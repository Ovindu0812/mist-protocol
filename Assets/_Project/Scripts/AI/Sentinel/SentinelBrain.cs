using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.AI.Navigation;
using MistProtocol.AI.Perception;
using MistProtocol.Animation;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Sentinel
{
    /// <summary>
    /// SENTINEL - tactical rifle robot driven by Goal-Oriented Action Planning.
    ///
    /// Each think tick the brain (1) senses the world into a bitmask state,
    /// (2) picks the highest-priority goal, and (3) re-plans with forward A* over
    /// the action set only when something salient changed. The same 9 actions
    /// combine into different tactics depending on context costs:
    ///   * no cover nearby     -> AlertSquad, Flank, Shoot
    ///   * good cover          -> AlertSquad, TakeCover, ShootFromCover
    ///   * empty mag & exposed -> TakeCover, Reload, ShootFromCover
    ///   * low health          -> Survive goal: TakeCover, Reload
    ///   * squadmate suppressing -> Flank becomes cheap -> pincer emerges.
    /// Flank paths are themselves an A* search with a tactical cost layer that
    /// penalises cells inside the player's line of sight.
    /// </summary>
    public class SentinelBrain : AgentBase
    {
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float runSpeed = 4.2f;

        private readonly List<GoapAction> _actions = new();
        private readonly List<GoapAction> _plan = new();
        private GoapAction _current;
        private int _planIndex;
        private GoapGoal _goal;
        [System.NonSerialized] private Fact _state;
        [System.NonSerialized] private Fact _lastSalient;
        private int _patrolIndex;
        private bool _alerted;
        private float _alertedAt;

        public override AgentKind Kind => AgentKind.Sentinel;
        public WalkerAnimator Walker { get; private set; }
        public EnemyWeapon Gun => Weapon;
        public AgentMotor Mover => Motor;
        public VisionSensor Eyes => Vision;
        public Fact State => _state;
        public string CurrentActionName => _current?.Name;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public bool InCover { get; set; }
        public Vector3 CoverPoint { get; set; }
        public bool HasLead { get; private set; }
        public Vector3 LeadPoint { get; private set; }
        public string PlanText { get; private set; } = "";

        public void SetPatrolPoints(Transform[] points) => patrolPoints = points;

        protected override void Awake()
        {
            base.Awake();
            Walker = GetComponent<WalkerAnimator>();
            _actions.AddRange(new GoapAction[]
            {
                new PatrolAction(), new InvestigateAction(), new AlertSquadAction(), new ReloadAction(),
                new TakeCoverAction(), new ShootFromCoverAction(), new ShootAction(), new FlankAction(), new ApproachAction(),
            });
        }

        protected override void OnEnable() { base.OnEnable(); SentinelSquad.Join(this); }
        protected override void OnDisable() { base.OnDisable(); SentinelSquad.Leave(this); }

        public override void OnHeard(in SoundStimulus s)
        {
            if (Vision.CanSeePlayer) return;
            if (s.FromPlayer || s.Type == SoundType.EnemyGunshot || s.Type == SoundType.Explosion)
            {
                LeadPoint = s.EstimatedPosition;
                HasLead = true;
                if (s.FromPlayer && s.Type == SoundType.Gunshot && s.Intensity > 18f)
                    Vision.ReportPosition(s.EstimatedPosition, 0.6f);
            }
        }

        public void ReceiveAlert(Vector3 playerPos)
        {
            Vision.ReportPosition(playerPos, 1f);
            _alerted = true;
            _alertedAt = Time.time;
        }

        public void MarkSquadAlerted() { _alerted = true; _alertedAt = Time.time; }
        public void ClearLead() => HasLead = false;

        // ------------------------------------------------------------------ think
        protected override void Think()
        {
            const float dt = 1f / 8f;
            Sense();
            ChooseGoal(out var goal);

            Fact salient = _state & (Fact.KnowsPlayer | Fact.LowHealth | Fact.HasLead);
            Fact soft = _state & (Fact.PlayerVisible | Fact.WeaponLoaded);
            bool committed = _current is ICommitted;
            bool changed = salient != (_lastSalient & (Fact.KnowsPlayer | Fact.LowHealth | Fact.HasLead)) ||
                           (!committed && soft != (_lastSalient & (Fact.PlayerVisible | Fact.WeaponLoaded)));
            _lastSalient = salient | soft;

            if (goal.Name != _goal.Name || _current == null || changed)
            {
                _goal = goal;
                Replan();
            }

            if (_current != null)
            {
                var status = _current.Tick(this, dt);
                if (status == ActionStatus.Success) Advance();
                else if (status == ActionStatus.Failure) { SwitchTo(null); }
            }
            DebugState = $"{_goal.Name}: {CurrentActionName ?? "idle"}";
            DebugDetail = PlanText;
        }

        private void Sense()
        {
            Fact s = Fact.None;
            if (Vision.HasTarget) s |= Fact.KnowsPlayer;
            if (Vision.CanSeePlayer && Vision.Awareness >= 1f) s |= Fact.PlayerVisible;
            if (InCover && (transform.position - CoverPoint).Flat().magnitude < 1.2f && IsCoverSafe(CoverPoint)) s |= Fact.InCover;
            else InCover = false;
            if (Weapon != null && Weapon.Ammo > 0) s |= Fact.WeaponLoaded;
            if (_alerted && Time.time - _alertedAt < 40f) s |= Fact.SquadAlerted;
            if (Health.Normalized < 0.4f) s |= Fact.LowHealth;
            if (HasLead) s |= Fact.HasLead;
            _state = s;
        }

        private void ChooseGoal(out GoapGoal goal)
        {
            bool knows = (_state & Fact.KnowsPlayer) != 0;
            if (knows && (_state & Fact.LowHealth) != 0 && (_state & Fact.InCover) == 0)
                goal = new GoapGoal { Name = "Survive", Want = Fact.InCover | Fact.WeaponLoaded };
            else if (knows)
                goal = new GoapGoal { Name = "Eliminate", Want = Fact.PlayerDamaged | Fact.SquadAlerted };
            else if (HasLead)
                goal = new GoapGoal { Name = "Investigate", Want = Fact.AreaSearched };
            else
                goal = new GoapGoal { Name = "Patrol", Want = Fact.Patrolled };
        }

        private void Replan()
        {
            if (GoapPlanner.Plan(_state, _goal, _actions, this, _plan))
            {
                _planIndex = 0;
                PlanText = string.Join(" > ", _plan.ConvertAll(a => a.Name));
                SwitchTo(_plan.Count > 0 ? _plan[0] : null);
            }
            else
            {
                PlanText = "(no plan)";
                SwitchTo(null);
                Motor.Stop();
            }
        }

        private void Advance()
        {
            _planIndex++;
            if (_planIndex < _plan.Count && _plan[_planIndex].IsValid(this)) SwitchTo(_plan[_planIndex]);
            else SwitchTo(null); // replans next tick
        }

        private void SwitchTo(GoapAction next)
        {
            if (_current == next) return;
            _current?.Exit(this);
            _current = next;
            _current?.Enter(this);
        }

        // ------------------------------------------------------------------ helpers used by actions
        public Vector3 NextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return NavGrid.Instance.RandomWalkablePoint(transform.position, 10f);
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            return patrolPoints[_patrolIndex].position;
        }

        public Vector3 ThreatEye => (Vision.CanSeePlayer && PlayerAlive ? PlayerPos : Vision.LastKnownPosition) + Vector3.up * 1.6f;
        public Vector3 PlayerPosition => PlayerAlive ? PlayerPos : Vision.LastKnownPosition;
        public bool PlayerReloading => PlayerAlive && PlayerState.IsReloading;

        /// <summary>A point is safe cover if the threat can't see a crouched body (0.9 m) standing on it.</summary>
        public bool IsCoverSafe(Vector3 p) =>
            !MistUtil.HasLineOfSight(ThreatEye, p + Vector3.up * 0.9f, null, null);

        private Vector3 _cachedCover;
        private float _cachedCoverTime = -9f;

        public bool FindCover(out Vector3 best)
        {
            if (Time.time - _cachedCoverTime < 0.5f) { best = _cachedCover; return best != Vector3.zero; }
            _cachedCoverTime = Time.time;
            best = Vector3.zero;
            var grid = NavGrid.Instance;
            Vector3 me = transform.position.Flat();
            Vector3 threat = ThreatEye;
            var candidates = new List<(int cell, float d)>();
            foreach (int c in grid.CoverCells)
            {
                float d = (grid.CellCenter(c) - me).sqrMagnitude;
                if (d < 18f * 18f) candidates.Add((c, d));
            }
            candidates.Sort((a, b) => a.d.CompareTo(b.d));
            float bestScore = float.MaxValue;
            int tested = 0;
            foreach (var (cell, d) in candidates)
            {
                if (tested++ > 45) break;
                Vector3 p = grid.CellCenter(cell);
                float toThreat = Vector3.Distance(p, threat.Flat());
                if (toThreat < 5f || toThreat > 24f) continue;
                if (SentinelSquad.IsCoverClaimed(p, this)) continue;
                if (!IsCoverSafe(p)) continue;
                bool canPeek = MistUtil.HasLineOfSight(threat, p + Vector3.up * 1.75f, null, null);
                float score = Mathf.Sqrt(d) + Mathf.Abs(toThreat - 12f) * 0.4f + (canPeek ? 0f : 6f);
                if (score < bestScore) { bestScore = score; best = p; }
            }
            _cachedCover = best;
            return best != Vector3.zero;
        }

        // tactical cost layer for flanking searches
        private float[] _danger;
        private int[] _dangerStamp;
        private int _stamp;
        private Vector3 _dangerEye;

        public void BeginDangerQuery()
        {
            var g = NavGrid.Instance;
            if (_danger == null || _danger.Length != g.Count) { _danger = new float[g.Count]; _dangerStamp = new int[g.Count]; }
            _stamp++;
            _dangerEye = ThreatEye;
        }

        /// <summary>+6 cost for cells the player can see: A* then prefers routes through blind spots.</summary>
        public float DangerCost(int cell)
        {
            if (_dangerStamp[cell] == _stamp) return _danger[cell];
            _dangerStamp[cell] = _stamp;
            Vector3 p = NavGrid.Instance.CellCenter(cell) + Vector3.up * 1.2f;
            float d = (p - _dangerEye).sqrMagnitude > 30f * 30f ? 0f :
                MistUtil.HasLineOfSight(_dangerEye, p, null, null) ? 6f : 0f;
            _danger[cell] = d;
            return d;
        }

        public bool FindFlankPoint(out Vector3 point)
        {
            point = Vector3.zero;
            if (!PlayerAlive && !Vision.HasLastKnown) return false;
            Vector3 player = PlayerPosition;
            Vector3 fromPlayer = (transform.position - player).Flat().normalized;
            if (fromPlayer == Vector3.zero) fromPlayer = transform.forward;
            var grid = NavGrid.Instance;
            foreach (float ang in new[] { 75f, -75f, 105f, -105f, 50f, -50f })
            foreach (float dist in new[] { 9f, 12f, 7f })
            {
                Vector3 p = player + Quaternion.Euler(0f, ang, 0f) * fromPlayer * dist;
                if (!grid.IsWalkableWorld(p)) continue;
                if (!MistUtil.HasLineOfSight(p + Vector3.up * 1.6f, player + Vector3.up * 1.2f, null, PlayerAlive ? PlayerState.Root : null)) continue;
                point = p;
                return true;
            }
            return false;
        }

        public void Shoot(float accuracy)
        {
            if (Weapon == null || !PlayerAlive) return;
            Weapon.TryFireAt(PlayerState.EyePosition - Vector3.up * 0.35f, accuracy);
        }

        protected override void OnDamaged(float amount, Vector3 from)
        {
            base.OnDamaged(amount, from);
            // being hit while "in cover" means the cover is compromised
            InCover = false;
        }
    }

    /// <summary>Marker: action shouldn't be interrupted by soft fact flicker (visibility while peeking etc.).</summary>
    public interface ICommitted { }
}
