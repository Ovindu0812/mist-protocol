using MistProtocol.AI.Agents;
using MistProtocol.AI.Navigation;
using MistProtocol.AI.Perception;
using MistProtocol.Animation;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.HunterDrone
{
    /// <summary>
    /// HUNTER DRONE - "hunts by ear".
    ///
    /// Architecture: hierarchical FSM driven by an acoustic memory.
    ///  * Sounds arrive from <see cref="SoundSystem"/> after Dijkstra propagation
    ///    through the level, so the drone hears gunfire *through doorways*, not walls.
    ///  * Every stimulus is written into a decaying <see cref="SuspicionMap"/>
    ///    (exponential decay, 45 s half-life). When idle, the drone patrols toward
    ///    the hottest region instead of random waypoints, so it "remembers" where
    ///    the player has been noisy.
    ///  * Stimulus priority = perceived intensity x type weight x recency, so a
    ///    loud explosion overrides a faint footstep, and a thrown crate can lure it.
    ///  * While searching it fires an active sonar ping: a standing/walking player
    ///    within range is revealed even without line of sight. Crouching defeats it.
    ///  * Engaging uses A* to an orbit point around the player at a preferred
    ///    range, so it circles and strafes rather than flying straight at you.
    /// </summary>
    public class HunterDroneBrain : AgentBase
    {
        private enum State { Patrol, Investigate, Search, Engage }

        [SerializeField] private float patrolSpeed = 3f;
        [SerializeField] private float investigateSpeed = 5.5f;
        [SerializeField] private float engageRange = 8f;
        [SerializeField] private float sonarRange = 9f;
        [SerializeField] private float searchDuration = 5f;

        private State _state = State.Patrol;
        private Vector3 _interestPoint;
        private float _interestScore;
        private float _stateTime;
        private float _orbitAngle;
        private float _orbitDir = 1f;
        private float _nextSonar;
        private int _burst;
        private float _burstPause;
        private DroneAnimator _droneAnim;
        private SuspicionMap _suspicion;
        private Vector3 _patrolTarget;
        private bool _hasPatrolTarget;

        public override AgentKind Kind => AgentKind.HunterDrone;
        public SuspicionMap Suspicion => _suspicion;

        protected override void Awake()
        {
            base.Awake();
            _droneAnim = GetComponent<DroneAnimator>();
            Motor.HoverHeight = 2.4f;
            Vision.EyeOffset = Vector3.zero;
            Vision.FieldOfView = 140f;
            _orbitDir = Random.value < 0.5f ? -1f : 1f;
        }

        protected override void Start()
        {
            base.Start();
            _suspicion = new SuspicionMap(NavGrid.Instance, 4, 45f);
        }

        public override void OnHeard(in SoundStimulus s)
        {
            if (Health.IsDead || _suspicion == null) return;
            float typeWeight = s.Type switch
            {
                SoundType.Gunshot => 1.0f,
                SoundType.Explosion => 1.2f,
                SoundType.Impact => 0.9f,     // thrown objects are a valid lure
                SoundType.EnemyGunshot => 0.6f, // converge on allies' fights
                SoundType.Footstep => 0.8f,
                SoundType.Door => 0.5f,
                _ => 0.5f,
            };
            float score = s.Intensity * typeWeight;
            _suspicion.Add(s.EstimatedPosition, score);

            if (_state == State.Engage && Vision.CanSeePlayer) return;
            // decay the current interest with time so newer sounds can override it
            float currentInterest = _interestScore * Mathf.Exp(-(Time.time - _stateTime) / 6f);
            if (score > currentInterest * 0.8f)
            {
                _interestPoint = s.EstimatedPosition;
                _interestScore = score;
                Enter(State.Investigate);
            }
        }

        private void Enter(State s)
        {
            if (_state == s && s != State.Investigate) return;
            _state = s;
            _stateTime = Time.time;
            if (_droneAnim) _droneAnim.Scanning = s == State.Search;
        }

        protected override void Think()
        {
            _suspicion?.Decay();
            bool engaged = Vision.CanSeePlayer && Vision.Awareness >= 1f;
            if (engaged) Enter(State.Engage);

            switch (_state)
            {
                case State.Patrol: TickPatrol(); break;
                case State.Investigate: TickInvestigate(); break;
                case State.Search: TickSearch(); break;
                case State.Engage: TickEngage(); break;
            }
            DebugState = _state.ToString();
            DebugDetail = $"aware {Vision.Awareness:0.00}  heat {(_suspicion != null ? _suspicion.MaxHeat : 0):0}  path {(Motor.PathFailed ? "FAIL" : Motor.HasPath ? "ok" : "-")}";
        }

        private void TickPatrol()
        {
            Motor.MaxSpeed = patrolSpeed;
            Face(null);
            if (!_hasPatrolTarget || Motor.Arrived || Motor.PathFailed)
            {
                // go where the acoustic memory is hottest; otherwise wander
                if (_suspicion != null && _suspicion.TryGetHottest(out var hot, 6f) && Vector3.Distance(hot.Flat(), transform.position.Flat()) > 4f)
                    _patrolTarget = hot;
                else
                    _patrolTarget = NavGrid.Instance.RandomWalkablePoint(transform.position, 14f);
                _hasPatrolTarget = true;
            }
            Motor.MoveTo(_patrolTarget);
        }

        private void TickInvestigate()
        {
            Motor.MaxSpeed = investigateSpeed;
            Face(null);
            if (!Motor.MoveTo(_interestPoint) || Motor.Arrived || Time.time - _stateTime > 15f)
                Enter(State.Search);
        }

        private void TickSearch()
        {
            Motor.Stop();
            if (Time.time >= _nextSonar) Sonar();
            if (Time.time - _stateTime > searchDuration)
            {
                _interestScore = 0f;
                _hasPatrolTarget = false;
                Enter(State.Patrol);
            }
        }

        private void Sonar()
        {
            _nextSonar = Time.time + 1.8f;
            ProceduralAudio.PlayAt("beep", transform.position, 0.5f, 1.4f);
            Vfx.Flash(transform.position, new Color(0.3f, 0.9f, 1f), 3f, sonarRange, 0.15f);
            if (!PlayerAlive || PlayerState.IsCrouching) return;
            if (Vector3.Distance(PlayerPos, transform.position.Flat()) < sonarRange)
            {
                Vision.ReportPosition(PlayerPos, 1f);
                _interestPoint = PlayerPos;
                Enter(State.Investigate);
            }
        }

        private void TickEngage()
        {
            if (!PlayerAlive) { Enter(State.Patrol); return; }
            Vector3 player = PlayerPos;
            if (!Vision.CanSeePlayer)
            {
                if (Vision.TimeSinceSeen > 1.5f)
                {
                    _interestPoint = Vision.LastKnownPosition;
                    _interestScore = 50f;
                    Enter(State.Investigate);
                }
                return;
            }

            // circle-strafe: orbit point at preferred range, A*-pathed so it never clips walls
            Motor.MaxSpeed = 4.5f;
            _orbitAngle += 30f * _orbitDir / 8f;
            Vector3 offset = Quaternion.Euler(0f, _orbitAngle, 0f) * Vector3.forward * engageRange;
            Vector3 orbit = player + offset;
            if (!NavGrid.Instance.IsWalkableWorld(orbit)) { _orbitDir = -_orbitDir; orbit = player + (transform.position - player).Flat().normalized * engageRange; }
            Motor.MoveTo(orbit);
            Face(player);

            if (_burstPause > 0f) { _burstPause -= 1f / 8f; return; }
            if (Weapon != null && Weapon.TryFireAt(PlayerState.EyePosition - Vector3.up * 0.3f, 0.9f))
            {
                if (++_burst >= 4) { _burst = 0; _burstPause = 1.1f; }
            }
        }
    }
}
