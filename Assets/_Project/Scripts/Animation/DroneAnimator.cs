using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Animation
{
    /// <summary>Hunter Drone: rotor spin, banking into acceleration, hover bob and a scanning sweep.</summary>
    public class DroneAnimator : AgentAnimatorBase
    {
        [SerializeField] private float rotorRpm = 1800f;
        [SerializeField] private float maxBank = 25f;

        private Transform _body;
        private readonly Transform[] _rotors = new Transform[4];
        private Vector3 _lastVel;
        private Vector3 _smoothedAccel;
        private float _t;
        public bool Scanning { get; set; }

        protected override void Awake()
        {
            base.Awake();
            _body = transform.FindDeep("HunterDrone");
            for (int i = 0; i < 4; i++) _rotors[i] = transform.FindDeep("Rotor_" + i);
        }

        protected override void Animate(float dt)
        {
            _t += dt;
            Vector3 accel = (Motor.Velocity - _lastVel) / Mathf.Max(dt, 0.0001f);
            _lastVel = Motor.Velocity;
            _smoothedAccel = Vector3.Lerp(_smoothedAccel, accel, dt * 5f);

            float spin = rotorRpm / 60f * 360f * dt * (1f + Motor.Speed01 * 0.5f);
            for (int i = 0; i < 4; i++)
                if (_rotors[i]) _rotors[i].Rotate(0f, i % 2 == 0 ? spin : -spin, 0f, Space.Self);

            if (_body == null) return;
            // bank: pitch forward when accelerating forward, roll into turns
            Vector3 localAcc = transform.InverseTransformDirection(_smoothedAccel + Motor.Velocity * 0.6f);
            float pitch = Mathf.Clamp(localAcc.z * 4f, -maxBank, maxBank);
            float roll = Mathf.Clamp(-localAcc.x * 4f, -maxBank, maxBank);
            float scanYaw = Scanning ? Mathf.Sin(_t * 1.6f) * 60f : 0f;
            _body.localRotation = Quaternion.Euler(pitch - Recoil * 8f + Flinch * 20f, scanYaw, roll);
            _body.localPosition = new Vector3(0f, Mathf.Sin(_t * 2.3f) * 0.08f, 0f);
        }
    }
}
