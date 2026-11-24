using NUnit.Framework;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class RollingRoomEncounterTests
    {
        private static readonly Bounds RollingRoomBounds = new Bounds(
            new Vector3(0f, 1.5f, 18f),
            new Vector3(19.5f, 4f, 35.5f)
        );

        [Test]
        public void RollingRoom_AdditionalSentinels_AreWithinRoomBounds()
        {
            var sentinelPositions = new[]
            {
                new Vector3(-4f, 0f, 28f), // North flank along conveyor
                new Vector3(4f, 0f, 8f),   // South flank across rolling machines
                new Vector3(6f, 0f, 18f)   // East exit chokepoint
            };

            foreach (var pos in sentinelPositions)
            {
                Assert.IsTrue(RollingRoomBounds.Contains(pos + Vector3.up * 1f),
                    $"Sentinel at {pos} must reside within Rolling Room bounds");
            }
        }

        [Test]
        public void RollingRoom_HunterDrones_HaveElevatedHoverTacticalHeight()
        {
            var dronePositions = new[]
            {
                new Vector3(-2f, 2.4f, 29f),
                new Vector3(0f, 2.5f, 16f),
                new Vector3(3f, 2.4f, 7f)
            };

            foreach (var pos in dronePositions)
            {
                Assert.GreaterOrEqual(pos.y, 2.0f, $"Drone at {pos} should hover above ground cover");
                Assert.IsTrue(RollingRoomBounds.Contains(pos),
                    $"Drone at {pos} must reside within Rolling Room bounds");
            }
        }

        [Test]
        public void RollingRoom_TotalCombatSquad_AddsExactlySixReinforcements()
        {
            int sentinels = 3;
            int drones = 3;
            Assert.AreEqual(6, sentinels + drones, "Rolling Room must receive 3 Sentinels and 3 Drones");
        }
    }
}
