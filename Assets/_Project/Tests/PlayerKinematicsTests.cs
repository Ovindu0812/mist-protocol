using NUnit.Framework;
using MistProtocol.Player;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class PlayerKinematicsTests
    {
        [Test]
        public void StaminaSystem_SprintDrainAndRecovery_RespectsLimits()
        {
            var go = new GameObject("TestStamina");
            var stamina = go.AddComponent<StaminaSystem>();

            Assert.AreEqual(100f, stamina.CurrentStamina);

            stamina.ConsumeStamina(40f);
            Assert.AreEqual(60f, stamina.CurrentStamina, 0.01f);

            stamina.ConsumeStamina(80f);
            Assert.AreEqual(0f, stamina.CurrentStamina, 0.01f);
            Assert.IsTrue(stamina.IsExhausted);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void StaminaSystem_Regenerate_RestoresFullCapacity()
        {
            var go = new GameObject("TestStamina");
            var stamina = go.AddComponent<StaminaSystem>();

            stamina.ConsumeStamina(50f);
            stamina.RestoreStamina(100f);

            Assert.AreEqual(100f, stamina.CurrentStamina, 0.01f);
            Assert.IsFalse(stamina.IsExhausted);

            Object.DestroyImmediate(go);
        }
    }
}
