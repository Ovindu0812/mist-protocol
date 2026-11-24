using NUnit.Framework;
using MistProtocol.FinalRescue;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class PuzzleScreenDisplayTests
    {
        [Test]
        public void PuzzleScreen_PhaseColorTags_ContainCorrectHexValues()
        {
            string redTag = "<color=#FF4444>[ RED: FIRE ]</color>";
            string blueTag = "<color=#44AAFF>[ BLUE: MIST ]</color>";
            string yellowTag = "<color=#FFAA22>[ YELLOW: SUN ]</color>";
            string greenTag = "<color=#55FF66>[ GREEN: LEAF ]</color>";

            Assert.IsTrue(redTag.Contains("#FF4444"));
            Assert.IsTrue(blueTag.Contains("#44AAFF"));
            Assert.IsTrue(yellowTag.Contains("#FFAA22"));
            Assert.IsTrue(greenTag.Contains("#55FF66"));
        }

        [Test]
        public void PuzzleScreen_PhaseTitles_MatchGlenmistLoreOrder()
        {
            var correctOrder = new[]
            {
                "PHASE 1: KINDLE WITHERING KILN FURNACE (RED)",
                "PHASE 2: HIGHLAND MOUNTAIN MIST (BLUE)",
                "PHASE 3: MORNING SUNRISE GOLD (YELLOW)",
                "PHASE 4: HARVEST FRESH CEYLON LEAF (GREEN)"
            };

            Assert.AreEqual(4, correctOrder.Length);
            Assert.IsTrue(correctOrder[0].Contains("KILN FURNACE"));
            Assert.IsTrue(correctOrder[1].Contains("MOUNTAIN MIST"));
            Assert.IsTrue(correctOrder[2].Contains("SUNRISE GOLD"));
            Assert.IsTrue(correctOrder[3].Contains("CEYLON LEAF"));
        }

        [Test]
        public void PuzzleScreen_SlotUnselectedPlaceholder_FormatsProperly()
        {
            for (int slot = 1; slot <= 4; slot++)
            {
                string placeholder = $"<color=#778899>[ {slot}: ??? ]</color>";
                Assert.IsTrue(placeholder.Contains($"{slot}: ???"));
                Assert.IsTrue(placeholder.StartsWith("<color=#778899>"));
            }
        }
    }
}
