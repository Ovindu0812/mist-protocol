using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Animation
{
    /// <summary>Saboteur: diagonal-pair (trot) quadruped gait plus claw open/close and grab pose.</summary>
    public class SpiderAnimator : AgentAnimatorBase
    {
        [SerializeField] private float stride = 0.6f;
        [SerializeField] private float lift = 18f;
        [SerializeField] private float swing = 22f;

        private Transform _fl, _fr, _bl, _br, _claw, _fingerL, _fingerR, _antenna;
        private float _phase;
        private float _clawOpen;
        public bool Carrying { get; set; }
        public bool Attacking { get; set; }

        protected override void Awake()
        {
            base.Awake();
            _fl = transform.FindDeep("Leg_FL"); _fr = transform.FindDeep("Leg_FR");
            _bl = transform.FindDeep("Leg_BL"); _br = transform.FindDeep("Leg_BR");
            _claw = transform.FindDeep("Claw");
            _fingerL = transform.FindDeep("Claw_FingerL"); _fingerR = transform.FindDeep("Claw_FingerR");
            _antenna = transform.FindDeep("Antenna");
        }

        protected override void Animate(float dt)
        {
            float speed = Motor.Velocity.magnitude;
            _phase += speed * dt / stride * Mathf.PI;
            float a = Mathf.Clamp01(speed / 1.2f);
            float s = Mathf.Sin(_phase), c = Mathf.Cos(_phase);
            // trot: FL+BR in phase, FR+BL in anti-phase. Swing around Z (forward/back), lift around X.
            Leg(_fl, s, c, a, 1f); Leg(_br, s, c, a, 1f);
            Leg(_fr, -s, -c, a, -1f); Leg(_bl, -s, -c, a, -1f);

            float target = Attacking ? Mathf.PingPong(Time.time * 6f, 1f) : Carrying ? 0.1f : 0.6f;
            _clawOpen = Mathf.MoveTowards(_clawOpen, target, dt * 4f);
            if (_fingerL) _fingerL.localRotation = Quaternion.Euler(0, _clawOpen * 35f, 0);
            if (_fingerR) _fingerR.localRotation = Quaternion.Euler(0, -_clawOpen * 35f, 0);
            if (_claw) _claw.localRotation = Quaternion.Euler(Carrying ? -25f : Attacking ? 15f * Mathf.Sin(Time.time * 20f) : -Recoil * 30f, 0, 0);
            if (_antenna) _antenna.localRotation = Quaternion.Euler(-a * 20f + Mathf.Sin(Time.time * 7f) * 5f * (1f + Alert), 0, Flinch * 30f);
        }

        private void Leg(Transform leg, float s, float c, float amount, float side)
        {
            if (leg == null) return;
            float liftAmt = Mathf.Max(0f, c) * lift * amount;
            leg.localRotation = Quaternion.Euler(s * swing * amount, 0f, side * liftAmt * Mathf.Sign(leg.localPosition.x));
        }
    }
}
