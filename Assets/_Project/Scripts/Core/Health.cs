using System;
using UnityEngine;

namespace MistProtocol.Core
{
    /// <summary>Hit points for anything that can be damaged (player, agents, gas canisters).</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsDead => Current <= 0f;

        /// <summary>amount, world position the damage came from.</summary>
        public event Action<float, Vector3> Damaged;
        public event Action Died;

        private void Awake() => Current = maxHealth;

        public void SetMax(float value, bool refill = true)
        {
            maxHealth = value;
            if (refill) Current = value;
        }

        public void TakeDamage(float amount, Vector3 fromPosition)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, fromPosition);
            if (Current <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }
    }
}
