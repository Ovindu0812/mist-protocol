using NUnit.Framework;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class StaminaSystemTests
    {
        [Test]
        public void Sprinting_ConsumesStaminaLinearly()
        {
            float maxStamina = 100f;
            float drainRate = 20f;
            float dt = 2.0f;

            float currentStamina = maxStamina - (drainRate * dt);
            Assert.AreEqual(60f, currentStamina, 0.001f);
        }

        [Test]
        public void StaminaDepletion_TriggersExhaustionState()
        {
            float currentStamina = 10f;
            float drainRate = 25f;
            float dt = 1.0f;

            currentStamina = Mathf.Max(0f, currentStamina - (drainRate * dt));
            bool isExhausted = currentStamina <= 0f;

            Assert.AreEqual(0f, currentStamina, 0.001f);
            Assert.IsTrue(isExhausted);
        }

        [Test]
        public void Exhaustion_RequiresMinimumThresholdToRecover()
        {
            bool isExhausted = true;
            float recoveryThreshold = 25f;

            float staminaA = 15f;
            if (staminaA >= recoveryThreshold) isExhausted = false;
            Assert.IsTrue(isExhausted, "Should still be exhausted at 15 stamina");

            float staminaB = 26f;
            if (staminaB >= recoveryThreshold) isExhausted = false;
            Assert.IsFalse(isExhausted, "Should clear exhaustion state once past 25 threshold");
        }

        [Test]
        public void Regeneration_ClampsAtMaxStamina()
        {
            float maxStamina = 100f;
            float currentStamina = 95f;
            float regenRate = 20f;
            float dt = 1.0f;

            currentStamina = Mathf.Min(maxStamina, currentStamina + (regenRate * dt));
            Assert.AreEqual(100f, currentStamina, 0.001f);
        }
    }
}
