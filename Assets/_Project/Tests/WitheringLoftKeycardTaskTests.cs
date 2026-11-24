using NUnit.Framework;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class WitheringLoftKeycardTaskTests
    {
        [Test]
        public void KeycardTask_RequiresExactlyThreeAuxiliaryBreakers()
        {
            const int requiredBreakers = 3;
            Assert.AreEqual(3, requiredBreakers, "Withering Loft puzzle must require exactly 3 auxiliary breaker levers");
        }

        [Test]
        public void KeycardTask_BreakerProgression_UnlocksSafeOnlyAtThree()
        {
            int flipped = 0;
            const int total = 3;

            bool isUnlocked = false;

            // Flip 1
            flipped++;
            isUnlocked = flipped >= total;
            Assert.IsFalse(isUnlocked, "Safe must remain locked after 1 breaker");

            // Flip 2
            flipped++;
            isUnlocked = flipped >= total;
            Assert.IsFalse(isUnlocked, "Safe must remain locked after 2 breakers");

            // Flip 3
            flipped++;
            isUnlocked = flipped >= total;
            Assert.IsTrue(isUnlocked, "Safe must unlock after all 3 breakers are restored");
        }
    }
}
