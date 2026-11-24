using NUnit.Framework;
using MistProtocol.AI.Saboteur;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class SaboteurUtilityTests
    {
        [Test]
        public void SaboteurBrain_InitializesWithUtilityActions()
        {
            var go = new GameObject("TestSaboteur");
            var brain = go.AddComponent<SaboteurBrain>();

            Assert.IsNotNull(brain, "SaboteurBrain component should initialize");
            Assert.AreEqual(AgentKind.Saboteur, brain.Kind);

            Object.DestroyImmediate(go);
        }
    }
}
