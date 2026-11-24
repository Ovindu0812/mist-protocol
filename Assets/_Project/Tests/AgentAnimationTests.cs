using NUnit.Framework;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class AgentAnimationTests
    {
        [Test]
        public void DroneBanking_Clamp_PreventsOverTilting()
        {
            float maxBank = 25f;
            float rawAccel = 15f; // extreme acceleration spike
            float calculatedPitch = Mathf.Clamp(rawAccel * 4f, -maxBank, maxBank);

            Assert.AreEqual(25f, calculatedPitch);
        }

        [Test]
        public void RotorRpm_CalculatesCorrectDegreesPerSecond()
        {
            float rotorRpm = 1800f;
            float dt = 0.0166f; // ~60 fps
            float speed01 = 1f;

            float spin = rotorRpm / 60f * 360f * dt * (1f + speed01 * 0.5f);
            Assert.Greater(spin, 0f);
            Assert.AreEqual(268.92f, spin, 5f);
        }

        [Test]
        public void WalkerGait_CadencePhaseAdvancesWithDisplacement()
        {
            float strideLength = 1.1f;
            float speed = 2.2f;
            float dt = 0.5f; // 1.1m travelled

            float phaseDelta = speed * dt / strideLength * Mathf.PI;
            Assert.AreEqual(Mathf.PI, phaseDelta, 0.001f);
        }
    }
}
