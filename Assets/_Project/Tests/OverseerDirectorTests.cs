using NUnit.Framework;
using MistProtocol.AI.Director;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class OverseerDirectorTests
    {
        [Test]
        public void PlayerProfile_DefaultValues_AreSensible()
        {
            var profile = new PlayerProfile();

            Assert.AreEqual(0.3f, profile.Accuracy, 0.01f);
            Assert.AreEqual(0f, profile.Camping);
            Assert.AreEqual(0f, profile.Stealth);
            Assert.AreEqual(0f, profile.PhysicsUse);
            Assert.AreEqual(0f, profile.Aggression);
        }

        [Test]
        public void OverseerDirector_PhaseEnum_ContainsAllPacingStates()
        {
            Assert.AreEqual(0, (int)OverseerDirector.Phase.BuildUp);
            Assert.AreEqual(1, (int)OverseerDirector.Phase.Peak);
            Assert.AreEqual(2, (int)OverseerDirector.Phase.Relax);
            Assert.AreEqual(3, (int)OverseerDirector.Phase.Final);
        }

        [Test]
        public void PlayerProfile_ToString_ReturnsFormattedString()
        {
            var profile = new PlayerProfile();
            string desc = profile.ToString();

            Assert.IsTrue(desc.Contains("camp"));
            Assert.IsTrue(desc.Contains("stealth"));
            Assert.IsTrue(desc.Contains("acc"));
            Assert.IsTrue(desc.Contains("phys"));
            Assert.IsTrue(desc.Contains("aggr"));
        }
    }
}
