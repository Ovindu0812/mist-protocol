using NUnit.Framework;
using MistProtocol.FinalRescue;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class LaboratorySwitchTests
    {
        [Test]
        public void Switch_LoreTitles_MapAccuratelyToColors()
        {
            var goRed = new GameObject("SwitchRed");
            var swRed = goRed.AddComponent<LaboratorySwitch>();
            swRed.Configure(SwitchColor.Red);
            Assert.AreEqual("Kiln Flame (Red)", swRed.LoreTitle);

            var goBlue = new GameObject("SwitchBlue");
            var swBlue = goBlue.AddComponent<LaboratorySwitch>();
            swBlue.Configure(SwitchColor.Blue);
            Assert.AreEqual("Midnight Mist (Blue)", swBlue.LoreTitle);

            var goYellow = new GameObject("SwitchYellow");
            var swYellow = goYellow.AddComponent<LaboratorySwitch>();
            swYellow.Configure(SwitchColor.Yellow);
            Assert.AreEqual("Sunrise Sun (Yellow)", swYellow.LoreTitle);

            var goGreen = new GameObject("SwitchGreen");
            var swGreen = goGreen.AddComponent<LaboratorySwitch>();
            swGreen.Configure(SwitchColor.Green);
            Assert.AreEqual("Emerald Leaf (Green)", swGreen.LoreTitle);

            Object.DestroyImmediate(goRed);
            Object.DestroyImmediate(goBlue);
            Object.DestroyImmediate(goYellow);
            Object.DestroyImmediate(goGreen);
        }

        [Test]
        public void Switch_ActivationToggle_UpdatesCanInteractAndPrompt()
        {
            var go = new GameObject("SwitchTest");
            var sw = go.AddComponent<LaboratorySwitch>();
            sw.Configure(SwitchColor.Red);

            Assert.IsTrue(sw.CanInteract, "Inactive switch should be interactable");
            Assert.AreEqual("[E] Activate Kiln Flame (Red)", sw.Prompt);

            sw.Interact();
            Assert.IsTrue(sw.IsActive, "Switch should be active after interaction");
            Assert.IsFalse(sw.CanInteract, "Active switch cannot be re-engaged");
            Assert.AreEqual("[Kiln Flame (Red) Engaged]", sw.Prompt);

            sw.SetInactive();
            Assert.IsFalse(sw.IsActive, "Switch should be inactive after reset");
            Assert.IsTrue(sw.CanInteract, "Reset switch should be interactable again");

            Object.DestroyImmediate(go);
        }
    }
}
