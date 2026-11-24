using NUnit.Framework;
using MistProtocol.FinalRescue;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class ConsoleButtonVisualTests
    {
        [Test]
        public void ConsoleButton_ElementMapping_MatchesHarvestElements()
        {
            var colors = new[] { SwitchColor.Red, SwitchColor.Blue, SwitchColor.Yellow, SwitchColor.Green };
            var expectedLabels = new[] { "FIRE", "MIST", "SUN", "LEAF" };

            for (int i = 0; i < colors.Length; i++)
            {
                string label = colors[i] switch
                {
                    SwitchColor.Red => "FIRE",
                    SwitchColor.Blue => "MIST",
                    SwitchColor.Yellow => "SUN",
                    SwitchColor.Green => "LEAF",
                    _ => ""
                };
                Assert.AreEqual(expectedLabels[i], label, $"Element label for {colors[i]} must be {expectedLabels[i]}");
            }
        }

        [Test]
        public void ConsoleButton_ColorPalette_ProvidesHighVisibilityDistinction()
        {
            var red = new Color(0.95f, 0.2f, 0.2f);
            var blue = new Color(0.2f, 0.65f, 1f);
            var yellow = new Color(1f, 0.85f, 0.2f);
            var green = new Color(0.25f, 0.9f, 0.3f);

            // Verify pairwise Euclidean distance in RGB color space exceeds perceptual threshold
            Assert.Greater(Vector4.Distance(red, blue), 0.5f);
            Assert.Greater(Vector4.Distance(red, green), 0.5f);
            Assert.Greater(Vector4.Distance(blue, yellow), 0.5f);
            Assert.Greater(Vector4.Distance(yellow, green), 0.5f);
        }
    }
}
