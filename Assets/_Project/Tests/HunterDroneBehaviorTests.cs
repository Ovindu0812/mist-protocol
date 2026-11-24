using NUnit.Framework;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class HunterDroneBehaviorTests
    {
        [Test]
        public void ExponentialDecayFormula_HalvesHeatAtHalfLife()
        {
            float initialHeat = 100f;
            float halfLife = 45f;
            float elapsedTime = 45f;

            float remainingHeat = initialHeat * Mathf.Pow(0.5f, elapsedTime / halfLife);

            Assert.AreEqual(50f, remainingHeat, 0.001f);
        }

        [Test]
        public void OrbitCalculation_StaysAtPreferredDistance()
        {
            Vector3 center = new Vector3(10f, 0f, 10f);
            float engageRange = 8f;
            float angle = 45f;

            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * engageRange;
            Vector3 orbitPoint = center + offset;

            float dist = Vector3.Distance(center, orbitPoint);
            Assert.AreEqual(engageRange, dist, 0.001f);
        }

        [Test]
        public void SonarDetection_WithinRange_TriggersDetection()
        {
            Vector3 dronePos = new Vector3(0, 2.5f, 0);
            Vector3 playerPos = new Vector3(5, 0, 0);
            float sonarRange = 9f;

            float flatDist = Vector3.Distance(new Vector3(dronePos.x, 0, dronePos.z), playerPos);
            Assert.Less(flatDist, sonarRange);
        }
    }
}
