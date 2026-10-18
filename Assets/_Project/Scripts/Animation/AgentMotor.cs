using System.Collections.Generic;
using MistProtocol.AI.Navigation;
using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.Animation
{
    /// <summary>
    /// Agent Controller (G&V): turns the raw cell array produced by A* into smooth,
    /// believable 3D motion.
    ///   1. String-pulling removes redundant grid corners.
    ///   2. Catmull-Rom resampling rounds the remaining corners.
    ///   3. Pure-pursuit steering follows a look-ahead point on the curve.
    ///   4. Velocity is acceleration-limited and slows on arrival (no snapping).
    ///   5. Body yaw is slerped toward the travel direction, or toward an aim target
    ///      when the brain wants to strafe while shooting.
    ///   6. Light separation from other agents avoids stacking in doorways.
    /// </summary>
    public class AgentMotor : MonoBehaviour
    {
        private static readonly List<AgentMotor> All = new();

        [Header("Locomotion")]
        [SerializeField] private float maxSpeed = 3.2f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float turnSpeed = 360f;
        [SerializeField] private float lookAhead = 1.1f;
        [SerializeField] private float arriveRadius = 1.6f;
        [SerializeField] private float stoppingDistance = 0.3f;
        [Header("Height")]
        [SerializeField] private float hoverHeight;
        [SerializeField] private float separationRadius = 1.1f;
        [SerializeField] private bool canOpenDoors = true;
        [SerializeField] private float repathInterval = 0.5f;

        private readonly List<Vector3> _raw = new();
        private readonly List<Vector3> _smooth = new();
        private int _cursor;
        private Vector3 _velocity;
        private Vector3? _lookTarget;
        private Vector3 _goal;
        private bool _hasGoal;
        private float _nextRepath;
        private int _gridVersion;
        private AStarPathfinder.CellCost _costFn;

        public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }
        public float SpeedMultiplier { get; set; } = 1f;
        public float HoverHeight { get => hoverHeight; set => hoverHeight = value; }
        public Vector3 Velocity => _velocity;
        public float Speed01 => Mathf.Clamp01(_velocity.magnitude / Mathf.Max(0.01f, maxSpeed));
        public bool HasPath => _hasGoal;
        public bool PathFailed { get; private set; }
        public Vector3 Goal => _goal;
        public IReadOnlyList<Vector3> DebugPath => _smooth;
        public float RemainingDistance => _hasGoal ? Vector3.Distance(transform.position.Flat(), _goal.Flat()) : 0f;
        public bool Arrived => !_hasGoal || RemainingDistance <= stoppingDistance + 0.05f;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Plan (or re-plan) a path to <paramref name="goal"/>. Throttled; cheap to call every frame.</summary>
        public bool MoveTo(Vector3 goal, AStarPathfinder.CellCost costFn = null, bool force = false)
        {
            var grid = NavGrid.Instance;
            if (grid == null) return false;
            if (!force && (goal - transform.position).Flat().magnitude <= stoppingDistance + 0.1f) { Stop(); return true; }
            bool goalMoved = !_hasGoal || (goal - _goal).sqrMagnitude > 1f;
            bool stale = grid.Version != _gridVersion;
            if (!force && !goalMoved && !stale) return !PathFailed;
            // a moving goal (chasing) re-plans at most every repathInterval
            if (!force && !stale && _hasGoal && !PathFailed && Time.time < _nextRepath) return true;

            _nextRepath = Time.time + repathInterval;
            _gridVersion = grid.Version;
            _costFn = costFn;
            _goal = goal;
            _hasGoal = true;
            PathFailed = !AStarPathfinder.FindPath(grid, transform.position, goal, _raw, costFn);
            if (PathFailed) { _smooth.Clear(); return false; }
            _raw.Insert(0, transform.position.Flat());
            PathUtils.Simplify(grid, _raw);
            PathUtils.Smooth(grid, _raw, _smooth);
            _cursor = 0;
            return true;
        }

        public void Stop()
        {
            _hasGoal = false;
            _smooth.Clear();
        }

        /// <summary>Face a world point independent of movement direction (strafing). Null = face travel direction.</summary>
        public void SetLookTarget(Vector3? target) => _lookTarget = target;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = transform.position;
            Vector3 desired = Vector3.zero;

            if (_hasGoal && _smooth.Count > 0)
            {
                // advance cursor along the curve while points are inside the look-ahead circle
                while (_cursor < _smooth.Count - 1 && (_smooth[_cursor].Flat() - pos.Flat()).magnitude < lookAhead) _cursor++;
                Vector3 target = _smooth[_cursor];
                Vector3 to = (target - pos).Flat();
                float remaining = RemainingDistance;
                if (remaining <= stoppingDistance) { _hasGoal = false; }
                else
                {
                    float speed = maxSpeed * SpeedMultiplier * Mathf.Clamp01(remaining / arriveRadius + 0.15f);
                    desired = to.normalized * speed;
                }
                if (canOpenDoors) OpenDoorsAhead(pos, to);
            }

            // separation steering
            Vector3 sep = Vector3.zero;
            foreach (var other in All)
            {
                if (other == this) continue;
                Vector3 d = (pos - other.transform.position).Flat();
                float m = d.magnitude;
                if (m > 0.001f && m < separationRadius) sep += d / m * (separationRadius - m);
            }
            desired += sep * 2f;

            _velocity = Vector3.MoveTowards(_velocity, desired, acceleration * dt);
            Vector3 next = pos + _velocity * dt;
            var grid = NavGrid.Instance;
            if (grid != null && !grid.IsWalkableWorld(next) && grid.IsWalkableWorld(pos))
            {
                // slide along blocked axis instead of entering geometry
                Vector3 nx = new Vector3(next.x, pos.y, pos.z), nz = new Vector3(pos.x, pos.y, next.z);
                next = grid.IsWalkableWorld(nx) ? nx : grid.IsWalkableWorld(nz) ? nz : pos;
                _velocity = (next - pos) / dt;
            }
            next.y = hoverHeight;
            transform.position = next;

            Vector3 face = _lookTarget.HasValue ? (_lookTarget.Value - pos).Flat() : _velocity.Flat();
            if (face.sqrMagnitude > 0.0025f)
            {
                var rot = Quaternion.LookRotation(face.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * dt);
            }
        }

        private readonly Collider[] _doorHits = new Collider[4];

        private void OpenDoorsAhead(Vector3 pos, Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            int n = Physics.OverlapSphereNonAlloc(pos + dir.normalized * 1.0f + Vector3.up, 0.8f, _doorHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var door = _doorHits[i].GetComponentInParent<PhysicsDoor>();
                if (door != null) door.OpenAwayFrom(pos);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 1; i < _smooth.Count; i++) Gizmos.DrawLine(_smooth[i - 1] + Vector3.up * 0.1f, _smooth[i] + Vector3.up * 0.1f);
        }
    }
}
