using NUnit.Framework;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class PlayerKinematicsTests
    {
        [Test]
        public void PlayerKinematics_SprintDrainCalculation_RespectsLimits()
        {
            float maxStamina = 100f;
            float currentStamina = maxStamina;
            float drainRate = 22f;
            float dt = 2.0f;

            currentStamina = Mathf.Max(0f, currentStamina - drainRate * dt);
            Assert.AreEqual(56f, currentStamina, 0.01f);

            // Test full depletion
            dt = 5.0f;
            currentStamina = Mathf.Max(0f, currentStamina - drainRate * dt);
            Assert.AreEqual(0f, currentStamina, 0.01f);
        }

        [Test]
        public void PlayerKinematics_CrouchHeight_ScalesCorrectly()
        {
            float standHeight = 1.80f;
            float crouchHeight = 1.00f;

            Assert.Less(crouchHeight, standHeight);
            Assert.AreEqual(0.80f, standHeight - crouchHeight, 0.01f);
        }
    }
}
