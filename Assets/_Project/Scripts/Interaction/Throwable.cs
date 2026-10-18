using MistProtocol.AI.Perception;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Light physics prop the player can pick up (F) and throw (LMB while holding).
    /// Impacts make noise proportional to impact speed, which the Hunter Drone
    /// can hear, so a thrown crate is a deliberate distraction tool.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Throwable : MonoBehaviour
    {
        [SerializeField] private float minImpactSpeed = 2.5f;
        private Rigidbody _rb;
        private float _thrownAt = -99f;
        private float _nextSound;

        public Rigidbody Body => _rb;
        public bool IsHeld { get; private set; }

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void OnPickedUp()
        {
            IsHeld = true;
            _rb.useGravity = false;
            _rb.linearDamping = 10f;
            _rb.angularDamping = 10f;
        }

        public void OnReleased(Vector3 impulse)
        {
            IsHeld = false;
            _rb.useGravity = true;
            _rb.linearDamping = 0.05f;
            _rb.angularDamping = 0.1f;
            if (impulse.sqrMagnitude > 0.01f)
            {
                _rb.AddForce(impulse, ForceMode.Impulse);
                _rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
                _thrownAt = Time.time;
                GameEvents.RaisePhysicsInteraction();
            }
        }

        protected virtual void OnCollisionEnter(Collision c)
        {
            float v = c.relativeVelocity.magnitude;
            if (v < minImpactSpeed || Time.time < _nextSound) return;
            _nextSound = Time.time + 0.2f;
            ProceduralAudio.PlayAt("impact", transform.position, Mathf.Clamp01(v / 10f));
            bool byPlayer = Time.time - _thrownAt < 6f;
            SoundSystem.Emit(transform.position, Mathf.Clamp(v * 2.5f, 6f, 24f), SoundType.Impact, byPlayer);
            OnImpact(v, c);
        }

        protected virtual void OnImpact(float speed, Collision c) { }
    }
}
