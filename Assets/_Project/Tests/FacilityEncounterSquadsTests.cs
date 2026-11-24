using NUnit.Framework;
using MistProtocol.World;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class FacilityEncounterSquadsTests
    {
        [Test]
        public void FacilityEncounterSquads_SingletonInitializes()
        {
            var go = new GameObject("TestFacilitySquads");
            var manager = go.AddComponent<FacilityEncounterSquads>();

            Assert.IsNotNull(FacilityEncounterSquads.Instance, "FacilityEncounterSquads instance should be set");
            Assert.AreSame(manager, FacilityEncounterSquads.Instance);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FacilityEncounterSquads_EnsureAllEncounterSquads_ExecutesSafely()
        {
            var go = new GameObject("TestFacilitySquads");
            var manager = go.AddComponent<FacilityEncounterSquads>();

            Assert.DoesNotThrow(() => manager.EnsureAllEncounterSquads());

            Object.DestroyImmediate(go);
        }
    }
}
