using System.Collections.Generic;
using MistProtocol.AI.Perception;
using MistProtocol.Animation;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Agents
{
    /// <summary>
    /// Common plumbing for every autonomous agent: health, perception, motor and
    /// animator references, hearing registration and death handling. The actual
    /// decision making lives in each subclass (FSM+sound map, GOAP, Utility AI),
    /// and talks to the body only through AgentMotor / AgentAnimatorBase / EnemyWeapon,
    /// keeping AI logic decoupled from visuals.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(AgentMotor), typeof(VisionSensor))]
    public abstract class AgentBase : MonoBehaviour, ISoundListener
    {
        public static readonly List<AgentBase> Active = new();

        [SerializeField] private float hearingThreshold = 3f;
        [SerializeField] private float thinkRate = 8f;

        protected Health Health;
        protected AgentMotor Motor;
        protected VisionSensor Vision;
        protected AgentAnimatorBase Anim;
        protected EnemyWeapon Weapon;
        private float _nextThink;

        public abstract AgentKind Kind { get; }
        /// <summary>Human-readable current decision, shown by the F1 debug overlay.</summary>
        public string DebugState { get; protected set; } = "-";
        public string DebugDetail { get; protected set; } = "";
        public VisionSensor Sensor => Vision;
        public AgentMotor Body => Motor;
        public Health HealthComponent => Health;

        public virtual Vector3 ListenPosition => transform.position;
        public float HearingThreshold => hearingThreshold;
        public virtual void OnHeard(in SoundStimulus s) { }

        protected static bool PlayerAlive => PlayerLocator.Alive;
        protected static Vector3 PlayerPos => PlayerLocator.State.Root.position;
        protected static IPlayerState PlayerState => PlayerLocator.State;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Motor = GetComponent<AgentMotor>();
            Vision = GetComponent<VisionSensor>();
            Anim = GetComponent<AgentAnimatorBase>();
            Weapon = GetComponent<EnemyWeapon>();
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        protected virtual void OnEnable()
        {
            Active.Add(this);
            SoundSystem.Register(this);
        }

        protected virtual void Start() => SoundSystem.Register(this);

        protected virtual void OnDisable()
        {
            Active.Remove(this);
            SoundSystem.Unregister(this);
        }

        protected virtual void Update()
        {
            if (Health.IsDead) return;
            if (Anim != null) Anim.SetAlert(Vision.Awareness);
            if (Time.time >= _nextThink)
            {
                _nextThink = Time.time + 1f / thinkRate;
                Think();
            }
        }

        /// <summary>Decision tick (runs at thinkRate Hz, not every frame, to save CPU).</summary>
        protected abstract void Think();

        protected virtual void OnDamaged(float amount, Vector3 from)
        {
            if (Anim != null) Anim.PlayHit();
            // getting shot reveals the shooter's rough position
            if (PlayerAlive && Vector3.Distance(from, PlayerState.EyePosition) < 2f)
                Vision.ReportPosition(PlayerPos, 1f);
        }

        protected virtual void OnDied()
        {
            GameEvents.RaiseAgentKilled(Kind, transform.position);
            Vfx.Burst(transform.position + Vector3.up, Vector3.up, new Color(1f, 0.7f, 0.3f), 25, 6f, 0.12f, 0.6f);
            ProceduralAudio.PlayAt("explosion", transform.position, 0.5f, 1.6f);
            Vector3 from = PlayerAlive ? PlayerState.EyePosition : transform.position - transform.forward;
            if (Anim != null) Anim.Shatter(from);
            Destroy(gameObject);
        }

        protected void Face(Vector3? target) => Motor.SetLookTarget(target);
    }
}
