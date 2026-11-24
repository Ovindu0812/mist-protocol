using NUnit.Framework;
using MistProtocol.AI.Agents;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class TitanBossStateTests
    {
        [Test]
        public void ArmoredBossHealth_ArmorFractured_FiresAtThirtyPercentHealth()
        {
            var go = new GameObject("TestTitan");
            var health = go.AddComponent<ArmoredBossHealth>();
            health.BalanceBoss(100f, 0.5f);

            bool fractured = false;
            health.ArmorFractured += () => fractured = true;

            health.Damage(75f, Vector3.zero);

            Assert.IsTrue(fractured, "ArmorFractured event must fire when health drops below 30%");
            Assert.IsTrue(health.IsArmorFractured, "IsArmorFractured property must be true");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ArmoredBossHealth_ResetBoss_RestoresFullHealth()
        {
            var go = new GameObject("TestTitan");
            var health = go.AddComponent<ArmoredBossHealth>();
            health.BalanceBoss(500f, 0.45f);

            health.Damage(200f, Vector3.zero);
            Assert.AreEqual(300f, health.Current);

            health.ResetBoss();
            Assert.AreEqual(500f, health.Current);
            Assert.IsFalse(health.IsArmorFractured);

            Object.DestroyImmediate(go);
        }
    }
}
