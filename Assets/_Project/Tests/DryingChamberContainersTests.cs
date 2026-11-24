using NUnit.Framework;
using MistProtocol.World;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class DryingChamberContainersTests
    {
        [Test]
        public void Containers_SpawnGroup_InitializesCorrectChildCount()
        {
            var go = new GameObject("TestContainers");
            var manager = go.AddComponent<DryingChamberContainers>();
            manager.BuildContainersIfMissing();

            var group = GameObject.Find("DryingChamber_Containers_Group");
            Assert.IsNotNull(group, "Drying Chamber containers group must be created");
            Assert.AreEqual(3, group.transform.childCount, "Drying Chamber should contain 3 perimeter cargo containers (all doorways clear)");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Containers_TacticalPositions_AreWithinDryingChamberBounds()
        {
            var go = new GameObject("TestContainers");
            var manager = go.AddComponent<DryingChamberContainers>();
            manager.BuildContainersIfMissing();

            var group = GameObject.Find("DryingChamber_Containers_Group");
            Assert.IsNotNull(group);

            // Drying Chamber bounds: X in [10, 30], Z in [0, 24]
            foreach (Transform child in group.transform)
            {
                Assert.IsTrue(child.position.x >= 10f && child.position.x <= 30f, 
                    $"Container {child.name} X={child.position.x} must be within Drying Chamber [10, 30]");
                Assert.IsTrue(child.position.z >= 0f && child.position.z <= 24f, 
                    $"Container {child.name} Z={child.position.z} must be within Drying Chamber [0, 24]");
            }

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Containers_HaveBoxColliders_ForBulletAndCharacterCover()
        {
            var go = new GameObject("TestContainers");
            var manager = go.AddComponent<DryingChamberContainers>();
            manager.BuildContainersIfMissing();

            var group = GameObject.Find("DryingChamber_Containers_Group");
            Assert.IsNotNull(group);

            foreach (Transform child in group.transform)
            {
                var col = child.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"Container {child.name} must have a BoxCollider for tactical cover");
                Assert.Greater(col.size.x, 2.0f, "Container collider length must be greater than 2m");
                Assert.Greater(col.size.y, 2.0f, "Container collider height must be greater than 2m");
            }

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(group);
        }
    }
}
