using NUnit.Framework;
using MistProtocol.AI.Navigation;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class NavGridGeometryTests
    {
        [Test]
        public void NavGrid_WorldToCell_CalculatesCorrectIndices()
        {
            var go = new GameObject("TestNavGrid");
            var grid = go.AddComponent<NavGrid>();
            grid.Configure(new Vector3(-32f, 0f, -10f), 176, 96, 0.5f);

            bool inBounds = grid.WorldToCell(new Vector3(0f, 0f, 0f), out int x, out int z);

            Assert.IsTrue(inBounds, "World (0, 0) must be within grid bounds");
            Assert.AreEqual(64, x, "Cell X for world 0 with origin -32 and size 0.5 should be 64");
            Assert.AreEqual(20, z, "Cell Z for world 0 with origin -10 and size 0.5 should be 20");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NavGrid_CellCenter_MatchesDiscreteGridFormula()
        {
            var go = new GameObject("TestNavGrid");
            var grid = go.AddComponent<NavGrid>();
            grid.Configure(new Vector3(-32f, 0f, -10f), 176, 96, 0.5f);

            int index = grid.Index(64, 20);
            Vector3 center = grid.CellCenter(index);

            Assert.AreEqual(0.25f, center.x, 0.001f);
            Assert.AreEqual(0f, center.y, 0.001f);
            Assert.AreEqual(0.25f, center.z, 0.001f);

            Object.DestroyImmediate(go);
        }
    }
}
