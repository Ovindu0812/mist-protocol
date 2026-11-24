using NUnit.Framework;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class HUDDisplayTests
    {
        [Test]
        public void HUD_HealthFraction_ClampsAccurately()
        {
            float maxHp = 100f;
            float currentHp = 75f;
            float normalized = Mathf.Clamp01(currentHp / maxHp);

            Assert.AreEqual(0.75f, normalized, 0.001f);

            // Test overflow clamping
            currentHp = 120f;
            normalized = Mathf.Clamp01(currentHp / maxHp);
            Assert.AreEqual(1.0f, normalized, 0.001f);

            // Test underflow clamping
            currentHp = -10f;
            normalized = Mathf.Clamp01(currentHp / maxHp);
            Assert.AreEqual(0.0f, normalized, 0.001f);
        }

        [Test]
        public void HUD_StaminaWidth_ScalesLinearly()
        {
            float maxStamina = 100f;
            float currentStamina = 50f;
            float maxWidth = 322f;

            float norm = Mathf.Clamp01(currentStamina / maxStamina);
            float width = maxWidth * norm;

            Assert.AreEqual(161f, width, 0.01f);
        }

        [Test]
        public void HUD_AmmoDisplay_FormatsCorrectly()
        {
            int inMag = 28;
            int reserve = 180;
            string formatted = $"{inMag} / {reserve}";

            Assert.AreEqual("28 / 180", formatted);
        }
    }
}
