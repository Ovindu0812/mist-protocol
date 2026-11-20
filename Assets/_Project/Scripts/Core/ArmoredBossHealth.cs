using UnityEngine;

namespace MistProtocol.Core
{
    /// <summary>
    /// Heavy reinforced armor for the Titan Automaton.
    /// Standard bullets ping off the thick steel plating with heavy damage reduction.
    /// Explosions (canisters) bypass the armor and deal full damage.
    /// </summary>
    public class ArmoredBossHealth : Health
    {
        [SerializeField] private float bulletDamageMultiplier = 0.45f; // Absorbs ~55% of bullet damage
        [SerializeField] private float explosionThreshold = 40f;

        private float _lastRicochetTime;

        public void BalanceBoss(float maxHp, float bulletMult)
        {
            SetMax(maxHp, refill: true);
            bulletDamageMultiplier = bulletMult;
        }

        public override void TakeDamage(float amount, Vector3 fromPosition)
        {
            if (IsDead || amount <= 0f) return;

            float effectiveDamage = amount;
            bool isExplosive = amount >= explosionThreshold;

            if (!isExplosive)
            {
                // Heavy armor absorbs the bullet
                effectiveDamage = amount * bulletDamageMultiplier;

                if (Time.time - _lastRicochetTime > 0.12f)
                {
                    _lastRicochetTime = Time.time;
                    ProceduralAudio.PlayAt("impact", transform.position + Vector3.up * 1.5f, 0.7f, 1.6f);
                    Vfx.Burst(transform.position + Vector3.up * 1.5f, (fromPosition - transform.position).normalized,
                        new Color(1f, 0.85f, 0.3f), 14, 5f, 0.08f, 0.4f);
                }
            }
            else
            {
                // Heavy explosive impact staggers
                ProceduralAudio.PlayAt("explosion", transform.position, 0.8f, 0.9f);
            }

            base.TakeDamage(effectiveDamage, fromPosition);
        }
    }
}
