using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Animation
{
    /// <summary>Biped gait for the Sentinel: leg/arm swing phase-locked to distance travelled.</summary>
    public class WalkerAnimator : AgentAnimatorBase
    {
        [SerializeField] private float strideLength = 1.1f;
        [SerializeField] private float legSwing = 32f;
        [SerializeField] private float armSwing = 22f;

        private Transform _legL, _legR, _armL, _armR, _torso, _head;
        private Vector3 _torsoBase;
        private float _phase;
        public Vector3? AimPoint { get; set; }
        public bool Crouched { get; set; }
        private float _crouch;

        protected override void Awake()
        {
            base.Awake();
            _legL = transform.FindDeep("Leg_L"); _legR = transform.FindDeep("Leg_R");
            _armL = transform.FindDeep("Arm_L"); _armR = transform.FindDeep("Arm_R");
            _torso = transform.FindDeep("Torso"); _head = transform.FindDeep("Head");
            if (_torso != null) _torsoBase = _torso.localPosition;
        }

        protected override void Animate(float dt)
        {
            float speed = Motor.Velocity.magnitude;
            // phase advances by distance, so the feet plant at the correct cadence for any speed
            _phase += speed * dt / strideLength * Mathf.PI;
            float s = Mathf.Sin(_phase);
            float amount = Mathf.Clamp01(speed / 1.5f);
            _crouch = Mathf.MoveTowards(_crouch, Crouched ? 1f : 0f, dt * 3f);

            // local "forward" swing: rotate around the X axis of each limb
            float crouchKnee = _crouch * 25f;
            if (_legL) _legL.localRotation = Quaternion.Euler(s * legSwing * amount - crouchKnee, 0, 0);
            if (_legR) _legR.localRotation = Quaternion.Euler(-s * legSwing * amount - crouchKnee, 0, 0);

            bool aiming = AimPoint.HasValue;
            if (_armL) _armL.localRotation = Quaternion.Euler(aiming ? -70f : -s * armSwing * amount, 0, aiming ? -15f : 0);
            if (_armR)
            {
                float aimPitch = -85f + Recoil * 25f;
                _armR.localRotation = aiming ? Quaternion.Euler(aimPitch, 0, 0) : Quaternion.Euler(s * armSwing * amount, 0, 0);
            }

            if (_torso)
            {
                float bob = Mathf.Abs(Mathf.Cos(_phase)) * 0.04f * amount;
                _torso.localPosition = _torsoBase + new Vector3(0, bob - _crouch * 0.25f, 0);
                _torso.localRotation = Quaternion.Euler(-Flinch * 18f + amount * 6f + _crouch * 10f, 0, Mathf.Sin(_phase * 0.5f) * 3f * amount);
            }
            if (_head && aiming)
            {
                Vector3 local = _torso.InverseTransformPoint(AimPoint.Value);
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -50f, 50f);
                _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.Euler(0, yaw, 0), dt * 8f);
            }
        }
    }
}
