using System.Collections.Generic;
using NUnit.Framework;
using MistProtocol.FinalRescue;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class PuzzleSequenceTests
    {
        private readonly List<SwitchColor> _correctSequence = new()
        {
            SwitchColor.Blue,
            SwitchColor.Yellow,
            SwitchColor.Green,
            SwitchColor.Red
        };

        private bool ValidateSequence(List<SwitchColor> input)
        {
            if (input == null || input.Count != _correctSequence.Count) return false;
            for (int i = 0; i < _correctSequence.Count; i++)
            {
                if (input[i] != _correctSequence[i]) return false;
            }
            return true;
        }

        [Test]
        public void Puzzle_CorrectSequence_Succeeds()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Blue,
                SwitchColor.Yellow,
                SwitchColor.Green,
                SwitchColor.Red
            };
            Assert.IsTrue(ValidateSequence(attempt));
        }

        [Test]
        public void Puzzle_IncorrectOrder_Fails()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Red,
                SwitchColor.Green,
                SwitchColor.Yellow,
                SwitchColor.Blue
            };
            Assert.IsFalse(ValidateSequence(attempt));
        }

        [Test]
        public void Puzzle_PartialSequence_Fails()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Blue,
                SwitchColor.Yellow
            };
            Assert.IsFalse(ValidateSequence(attempt));
        }
    }
}
