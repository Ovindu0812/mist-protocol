using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.AI.Navigation;
using MistProtocol.AI.Perception;
using MistProtocol.Animation;
using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.AI.Saboteur
{
    /// <summary>
    /// SABOTEUR - maintenance bot that fights the player's *use of the environment*.
    ///
    /// Architecture: Utility AI. Seven actions are scored from considerations
    /// every decision tick:
    ///   RestoreBarricade  - drags crates the player moved back to their original spot
    ///   CloseDoor         - shuts doors the player opened ahead of their objective
    ///   PlantTrap         - carries a gas canister into the player's route
    ///   DetonateTrap      - zaps its own trap when the player walks past it
    ///   Evade             - sidesteps when the player aims at it / is about to throw
    ///   Zap               - rushes in for a melee shock when the player is reloading
    ///   Wander            - low constant fallback
    /// Threat is estimated from the player's *weapon state*: aiming directly at
    /// the bot, reloading, or holding a throwable each shift the scores, so the
    /// same bot is cowardly against a ready rifle and aggressive against a reload.
    /// </summary>
    public class SaboteurBrain : AgentBase
    {
        [SerializeField] private float speed = 3.4f;
        [SerializeField] private float momentum = 0.15f;

        private readonly List<UtilityAction> _actions = new();
        private UtilityAction _current;
        private float _decideTimer;
        private SpiderAnimator _spider;
        private Transform _claw;

        public override AgentKind Kind => AgentKind.Saboteur;
        public AgentMotor Mover => Motor;
        public VisionSensor Eyes => Vision;
        public SpiderAnimator Spider => _spider;
        public Transform ClawPoint => _claw != null ? _claw : transform;
        public float BaseSpeed => speed;
        public IReadOnlyList<UtilityAction> Actions => _actions;
        public GasCanister ActiveTrap { get; set; }
        public Barricade Carrying { get; set; }
        public GasCanister CarryingCanister { get; set; }

        protected override void Awake()
        {
            base.Awake();
            _spider = GetComponent<SpiderAnimator>();
            _claw = transform.FindDeep("Claw");
            Vision.EyeOffset = new Vector3(0f, 0.55f, 0.3f);
            Vision.FieldOfView = 160f;
            _actions.AddRange(new UtilityAction[]
            {
                new RestoreBarricadeAction(), new CloseDoorAction(), new PlantTrapAction(), new DetonateTrapAction(),
                new EvadeAction(), new ZapAction(), new WanderAction(),
            });
        }

        // -------------------------------------------------------------- shared considerations
        /// <summary>0..1: how dangerous the player is to *me* right now, from their weapon state.</summary>
        public float Threat()
        {
            if (!PlayerAlive || !Vision.HasTarget) return 0f;
            var p = PlayerState;
            Vector3 toMe = (transform.position + Vector3.up * 0.5f) - p.EyePosition;
            float dist = toMe.magnitude;
            float aimDot = Vector3.Dot(p.AimDirection, toMe.normalized);
            float aimed = Curves.Logistic(aimDot, 0.96f, 80f);
            float range = Curves.Inverse(dist / 25f);
            float weaponReady = p.IsReloading ? 0.15f : p.IsHoldingObject ? 0.7f : 1f;
            return Mathf.Clamp01((0.35f + 0.65f * aimed) * range * weaponReady * (Vision.CanSeePlayer ? 1f : 0.4f));
        }

        public float DistanceToPlayer => PlayerAlive ? Vector3.Distance(transform.position.Flat(), PlayerPos.Flat()) : 99f;
        public bool PlayerIsAlive => PlayerAlive;
        public Vector3 PlayerPosition => PlayerAlive ? PlayerPos : transform.position;
        public IPlayerState Player => PlayerState;

        /// <summary>Where the player must go next (from the game manager), used to place traps / pick doors.</summary>
        public Vector3 PlayerObjective => GameManager.Instance != null ? GameManager.Instance.CurrentObjectivePosition : transform.position;

        public override void OnHeard(in SoundStimulus s)
        {
            if (s.FromPlayer && s.Intensity > 10f) Vision.ReportPosition(s.EstimatedPosition, 0.5f);
        }

        // -------------------------------------------------------------- decision
        protected override void Think()
        {
            const float dt = 1f / 8f;
            _decideTimer -= dt;
            if (_decideTimer <= 0f)
            {
                _decideTimer = 0.3f;
                UtilityAction best = null;
                float bestScore = 0.01f;
                foreach (var a in _actions)
                {
                    float s = a.Score(this);
                    if (a == _current) s += momentum;
                    a.LastScore = s;
                    if (s > bestScore) { bestScore = s; best = a; }
                }
                if (best != _current)
                {
                    _current?.Exit(this);
                    _current = best;
                    _current?.Enter(this);
                }
            }
            if (_current != null && _current.Tick(this, dt))
            {
                _current.Exit(this);
                _current = null;
                _decideTimer = 0f;
            }
            DebugState = _current?.Name ?? "idle";
            DebugDetail = $"threat {Threat():0.00}";
        }

        // -------------------------------------------------------------- carrying helpers
        public void Grab(Barricade crate)
        {
            Carrying = crate;
            crate.BeginCarry(ClawPoint, new Vector3(0f, 0.15f, 0.55f));
            if (_spider) _spider.Carrying = true;
            Motor.SpeedMultiplier = 0.6f;
        }

        public void DropCarried(Vector3 at)
        {
            if (Carrying != null) Carrying.EndCarry(at);
            Carrying = null;
            if (CarryingCanister != null)
            {
                CarryingCanister.transform.SetParent(null, true);
                CarryingCanister.transform.position = at + Vector3.up * 0.05f;
                CarryingCanister.transform.rotation = Quaternion.identity;
                CarryingCanister.Body.isKinematic = false;
                CarryingCanister = null;
            }
            if (_spider) _spider.Carrying = false;
            Motor.SpeedMultiplier = 1f;
        }

        public void GrabCanister(GasCanister c)
        {
            CarryingCanister = c;
            c.Body.isKinematic = true;
            c.transform.SetParent(ClawPoint, true);
            c.transform.localPosition = new Vector3(0f, -0.2f, 0.45f);
            if (_spider) _spider.Carrying = true;
            Motor.SpeedMultiplier = 0.75f;
        }

        protected override void OnDamaged(float amount, Vector3 from)
        {
            base.OnDamaged(amount, from);
            // heavy hit makes it let go of whatever it's dragging
            if (amount > 10f && (Carrying != null || CarryingCanister != null)) DropCarried(transform.position + transform.forward);
        }

        protected override void OnDied()
        {
            if (Carrying != null || CarryingCanister != null) DropCarried(transform.position + transform.forward * 0.8f);
            base.OnDied();
        }
    }
}
