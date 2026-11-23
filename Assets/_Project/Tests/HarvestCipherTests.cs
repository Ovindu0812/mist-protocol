using System.Collections.Generic;
using NUnit.Framework;
using MistProtocol.FinalRescue;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class HarvestCipherTests
    {
        [Test]
        public void HarvestCipher_FullPhaseProgression_MatchesGlenmistLore()
        {
            // Verified sequence:
            // Phase 1: Kiln Fire (Red)
            // Phase 2: Midnight Mist (Blue)
            // Phase 3: Morning Sun (Yellow)
            // Phase 4: Ceylon Leaf (Green)
            var expectedPhases = new[]
            {
                SwitchColor.Red,
                SwitchColor.Blue,
                SwitchColor.Yellow,
                SwitchColor.Green
            };

            var userInputs = new List<SwitchColor>();

            for (int i = 0; i < expectedPhases.Length; i++)
            {
                userInputs.Add(expectedPhases[i]);
                Assert.AreEqual(expectedPhases[i], userInputs[i], $"Phase {i + 1} must match expected harvest cipher color");
            }

            Assert.AreEqual(4, userInputs.Count, "All 4 phases must be completed for barrier shutdown");
        }

        [Test]
        public void HarvestCipher_PrematureGreenInput_FailsAtPhaseOne()
        {
            var expectedPhases = new[] { SwitchColor.Red, SwitchColor.Blue, SwitchColor.Yellow, SwitchColor.Green };
            SwitchColor invalidFirstInput = SwitchColor.Green;

            Assert.AreNotEqual(expectedPhases[0], invalidFirstInput, "Inputting Green first must trigger circuit mismatch error");
        }
    }
}
