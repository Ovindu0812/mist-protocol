using NUnit.Framework;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class FatherClueProgressionTests
    {
        [Test]
        public void FatherClueType_EnumValues_MatchNarrativeChain()
        {
            Assert.AreEqual(0, (int)FatherClueType.DroppedBoot);
            Assert.AreEqual(1, (int)FatherClueType.BrokenPhone);
            Assert.AreEqual(2, (int)FatherClueType.SupervisorID);
            Assert.AreEqual(3, (int)FatherClueType.FactoryMap);
        }

        [Test]
        public void FatherClue_Prompts_AreContextualToType()
        {
            var go = new GameObject("TestClue");
            var clue = go.AddComponent<FatherClue>();

            clue.Configure(FatherClueType.DroppedBoot);
            Assert.AreEqual("[E] Inspect Appa's dropped boot", clue.Prompt);
            Assert.IsTrue(clue.CanInteract);

            clue.Configure(FatherClueType.BrokenPhone);
            Assert.AreEqual("[E] Inspect cracked radio-phone", clue.Prompt);

            clue.Configure(FatherClueType.SupervisorID);
            Assert.AreEqual("[E] Inspect Supervisor ID badge", clue.Prompt);

            clue.Configure(FatherClueType.FactoryMap);
            Assert.AreEqual("[E] Examine Appa's Factory Blueprint", clue.Prompt);

            Object.DestroyImmediate(go);
        }
    }
}
