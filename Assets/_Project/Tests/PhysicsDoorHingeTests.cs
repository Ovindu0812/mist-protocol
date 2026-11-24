using NUnit.Framework;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class PhysicsDoorHingeTests
    {
        [Test]
        public void PhysicsDoor_LockToggle_UpdatesStateAndKinematic()
        {
            var go = new GameObject("TestDoor");
            go.AddComponent<Rigidbody>();
            go.AddComponent<HingeJoint>();
            var door = go.AddComponent<PhysicsDoor>();

            Assert.IsFalse(door.Locked);
            Assert.IsTrue(door.CanInteract);

            door.SetLocked(true);
            Assert.IsTrue(door.Locked);
            Assert.IsFalse(door.CanInteract);
            Assert.AreEqual("Locked", door.Prompt);

            door.SetLocked(false);
            Assert.IsFalse(door.Locked);
            Assert.IsTrue(door.CanInteract);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PhysicsDoor_ContainmentDoor_CannotBeLocked()
        {
            var go = new GameObject("Door_Containment");
            go.AddComponent<Rigidbody>();
            go.AddComponent<HingeJoint>();
            var door = go.AddComponent<PhysicsDoor>();

            door.SetLocked(true);
            Assert.IsFalse(door.Locked);
            Assert.IsTrue(door.CanInteract);

            Object.DestroyImmediate(go);
        }
    }
}
