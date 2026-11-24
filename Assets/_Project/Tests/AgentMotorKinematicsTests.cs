using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MistProtocol.AI.Navigation;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class AgentMotorKinematicsTests
    {
        [Test]
        public void CatmullRom_EndpointsMatchP1AndP2()
        {
            Vector3 p0 = new Vector3(0, 0, 0);
            Vector3 p1 = new Vector3(10, 0, 0);
            Vector3 p2 = new Vector3(20, 5, 0);
            Vector3 p3 = new Vector3(30, 5, 0);

            Vector3 start = PathUtils.CatmullRom(p0, p1, p2, p3, 0f);
            Vector3 end = PathUtils.CatmullRom(p0, p1, p2, p3, 1f);

            Assert.AreEqual(p1.x, start.x, 0.001f);
            Assert.AreEqual(p1.y, start.y, 0.001f);
            Assert.AreEqual(p1.z, start.z, 0.001f);

            Assert.AreEqual(p2.x, end.x, 0.001f);
            Assert.AreEqual(p2.y, end.y, 0.001f);
            Assert.AreEqual(p2.z, end.z, 0.001f);
        }

        [Test]
        public void CatmullRom_MidpointIsSmoothBetweenPoints()
        {
            Vector3 p0 = new Vector3(0, 0, 0);
            Vector3 p1 = new Vector3(5, 0, 0);
            Vector3 p2 = new Vector3(15, 0, 0);
            Vector3 p3 = new Vector3(20, 0, 0);

            Vector3 mid = PathUtils.CatmullRom(p0, p1, p2, p3, 0.5f);
            Assert.AreEqual(10f, mid.x, 0.01f);
            Assert.AreEqual(0f, mid.y, 0.01f);
            Assert.AreEqual(0f, mid.z, 0.01f);
        }

        [Test]
        public void PathUtils_Smooth_GeneratesSubdividedWaypoints()
        {
            var corners = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(10, 0, 0),
                new Vector3(10, 0, 10)
            };
            var output = new List<Vector3>();

            PathUtils.Smooth(null, corners, output, spacing: 1.0f);

            Assert.Greater(output.Count, corners.Count);
            Assert.AreEqual(corners[0], output[0]);
        }
    }
}
