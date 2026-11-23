using NUnit.Framework;
using MistProtocol.FinalRescue;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class SecretLaboratoryDoorTests
    {
        [Test]
        public void Door_InitiallyConcealed_CannotInteract()
        {
            var go = new GameObject("TestDoor");
            var door = go.AddComponent<SecretLaboratoryDoor>();

            Assert.IsFalse(door.IsRevealed, "Door should remain concealed behind wall plate before Titan defeat");
            Assert.IsFalse(door.CanInteract, "Player cannot interact with concealed wall plate");
            Assert.IsTrue(door.Prompt.Contains("Sealed Wall Mechanism"), "Prompt should indicate sealed mechanism");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Door_RevealTriggered_BecomesInteractable()
        {
            var go = new GameObject("TestDoor");
            var door = go.AddComponent<SecretLaboratoryDoor>();

            door.RevealDoor();

            Assert.IsTrue(door.IsRevealed, "Door should transition to revealed state");
            Assert.IsTrue(door.CanInteract, "Revealed door terminal should be interactable");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Door_WithoutMasterKey_RequiresTitanDefeatKey()
        {
            var go = new GameObject("TestDoor");
            var door = go.AddComponent<SecretLaboratoryDoor>();
            door.RevealDoor();

            Assert.IsTrue(door.Prompt.Contains("Requires Master Access Key"), "Prompt should require Master Access Key from Titan");

            Object.DestroyImmediate(go);
        }
    }
}
