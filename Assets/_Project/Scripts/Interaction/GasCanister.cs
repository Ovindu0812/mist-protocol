using MistProtocol.AI.Perception;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Pressurised withering-gas canister. Shoot it, throw it hard, or let the
    /// Saboteur plant it as a trap. Explosion applies falloff damage and a real
    /// AddExplosionForce to every rigidbody in range (doors, crates, debris).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class GasCanister : Throwable
    {
        [SerializeField] private float radius = 6f;
        [SerializeField] private float maxDamage = 90f;
        [SerializeField] private float force = 900f;
        [SerializeField] private float impactExplodeSpeed = 12f;

        private Health _health;
        private bool _exploded;
        public bool IsTrap { get; set; }
        public bool Exploded => _exploded;

        private void Start()
        {
            _health = GetComponent<Health>();
            _health.Died += Explode;
        }

        protected override void OnImpact(float speed, Collision c)
        {
            if (speed >= impactExplodeSpeed) Explode();
        }

        public void Explode()
        {
            if (_exploded) return;
            _exploded = true;
            Vector3 p = transform.position;
            Vfx.Explosion(p);
            ProceduralAudio.PlayAt("explosion", p, 1f);
            SoundSystem.Emit(p, 45f, SoundType.Explosion, false);

            foreach (var col in Physics.OverlapSphere(p, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (col.transform.IsChildOf(transform)) continue;
                float falloff = 1f - Mathf.Clamp01(Vector3.Distance(p, col.ClosestPoint(p)) / radius);
                var h = col.GetComponentInParent<Health>();
                if (h != null && h != _health) h.TakeDamage(maxDamage * falloff, p);
                if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic)
                    col.attachedRigidbody.AddExplosionForce(force, p, radius, 1.5f);
            }
            Destroy(gameObject);
        }
    }
}
