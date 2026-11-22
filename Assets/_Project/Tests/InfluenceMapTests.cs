using NUnit.Framework;
using UnityEngine;
using MistProtocol.AI.Director;
using MistProtocol.AI.Navigation;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class InfluenceMapTests
    {
        private GameObject _gridGo;
        private NavGrid _grid;

        [SetUp]
        public void Setup()
        {
            _gridGo = new GameObject("Test_NavGrid");
            _grid = _gridGo.AddComponent<NavGrid>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_gridGo != null) Object.DestroyImmediate(_gridGo);
        }

        [Test]
        public void InfluenceMap_InitializesWithZeroInfluence()
        {
            var map = new InfluenceMap(_grid, 4);
            float val = map.Sample(Vector3.zero);
            Assert.AreEqual(0f, val, 0.001f);
        }
    }
}
