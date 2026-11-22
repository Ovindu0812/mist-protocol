using NUnit.Framework;
using MistProtocol.AI.Navigation;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class PathfindingTests
    {
        [Test]
        public void MinHeap_PushesAndPopsInAscendingPriorityOrder()
        {
            var heap = new MinHeap();
            heap.Push(101, 5.5f);
            heap.Push(102, 1.2f);
            heap.Push(103, 8.9f);
            heap.Push(104, 3.4f);

            Assert.AreEqual(4, heap.Count);

            int first = heap.Pop(out float k1);
            Assert.AreEqual(102, first);
            Assert.AreEqual(1.2f, k1, 0.001f);

            int second = heap.Pop(out float k2);
            Assert.AreEqual(104, second);
            Assert.AreEqual(3.4f, k2, 0.001f);

            int third = heap.Pop(out float k3);
            Assert.AreEqual(101, third);
            Assert.AreEqual(5.5f, k3, 0.001f);

            int fourth = heap.Pop(out float k4);
            Assert.AreEqual(103, fourth);
            Assert.AreEqual(8.9f, k4, 0.001f);

            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void MinHeap_Clear_ResetsCountToZero()
        {
            var heap = new MinHeap();
            heap.Push(1, 10f);
            heap.Push(2, 20f);
            Assert.AreEqual(2, heap.Count);

            heap.Clear();
            Assert.AreEqual(0, heap.Count);
        }
    }
}
