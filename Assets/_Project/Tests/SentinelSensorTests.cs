using NUnit.Framework;
using MistProtocol.AI.Sentinel;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class SentinelSensorTests
    {
        [Test]
        public void SentinelBrain_NextPatrolPoint_LoopsContinuously()
        {
            var go = new GameObject("TestSentinel");
            var brain = go.AddComponent<SentinelBrain>();

            var wp0 = new GameObject("WP0").transform; wp0.position = new Vector3(0, 0, 0);
            var wp1 = new GameObject("WP1").transform; wp1.position = new Vector3(10, 0, 0);
            var wp2 = new GameObject("WP2").transform; wp2.position = new Vector3(20, 0, 0);

            brain.SetPatrolPoints(new[] { wp0, wp1, wp2 });

            Vector3 p1 = brain.NextPatrolPoint();
            Vector3 p2 = brain.NextPatrolPoint();
            Vector3 p3 = brain.NextPatrolPoint();
            Vector3 p4 = brain.NextPatrolPoint();

            Assert.AreEqual(wp1.position, p1);
            Assert.AreEqual(wp2.position, p2);
            Assert.AreEqual(wp0.position, p3);
            Assert.AreEqual(wp1.position, p4);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(wp0.gameObject);
            Object.DestroyImmediate(wp1.gameObject);
            Object.DestroyImmediate(wp2.gameObject);
        }
    }
}
