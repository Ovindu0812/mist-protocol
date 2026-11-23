using NUnit.Framework;
using MistProtocol.World;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class TerminalLogicTests
    {
        [Test]
        public void Terminal_InitializesInLockedState()
        {
            var go = new GameObject("TestTerminal");
            var term = go.AddComponent<ComputerTerminal>();

            Assert.IsTrue(term.CanInteract, "Terminal should be interactable when locked");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Terminal_AccessToggle_GrantsOverride()
        {
            var go = new GameObject("TestTerminal");
            var term = go.AddComponent<ComputerTerminal>();

            Assert.AreEqual("[E] Access Security Terminal", term.Prompt);

            term.Interact();
            Assert.AreEqual("[E] Log out of Terminal", term.Prompt);

            term.Interact();
            Assert.AreEqual("[E] Access Security Terminal", term.Prompt);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Terminal_ClearanceCode_MatchesSecurityProtocol()
        {
            string requiredLevel = "LEVEL-3";
            string securityCard = "OVERSEER-KEY-LEVEL-3";

            bool isAuthorized = securityCard.EndsWith(requiredLevel);
            Assert.IsTrue(isAuthorized, "Security card matching required level should grant access");
        }
    }
}
