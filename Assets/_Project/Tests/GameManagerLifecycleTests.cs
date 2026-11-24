using NUnit.Framework;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class GameManagerLifecycleTests
    {
        [Test]
        public void GameManager_FlowEnum_DefinesCorrectStates()
        {
            Assert.AreEqual(0, (int)GameManager.Flow.Intro);
            Assert.AreEqual(1, (int)GameManager.Flow.Playing);
            Assert.AreEqual(2, (int)GameManager.Flow.Won);
            Assert.AreEqual(3, (int)GameManager.Flow.Lost);
        }

        [Test]
        public void GameManager_ObjectiveText_CoversAllStages()
        {
            Assert.AreEqual(5, GameManager.ObjectiveText.Length);
            Assert.IsTrue(GameManager.ObjectiveText[0].Contains("keycard"));
            Assert.IsTrue(GameManager.ObjectiveText[1].Contains("gate"));
            Assert.IsTrue(GameManager.ObjectiveText[2].Contains("Control Room"));
            Assert.IsTrue(GameManager.ObjectiveText[3].Contains("kill switch"));
            Assert.IsTrue(GameManager.ObjectiveText[4].Contains("offline"));
        }
    }
}
