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
            SwitchColor.Red,
            SwitchColor.Blue,
            SwitchColor.Yellow,
            SwitchColor.Green
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
        public void Puzzle_CorrectHarvestRiddleSequence_Succeeds()
        {
            // Appa's Glenmist Harvest Riddle:
            // 1. Kiln Flame (Red)
            // 2. Midnight Mist (Blue)
            // 3. Sunrise Gold (Yellow)
            // 4. Emerald Harvest (Green)
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Red,
                SwitchColor.Blue,
                SwitchColor.Yellow,
                SwitchColor.Green
            };
            Assert.IsTrue(ValidateSequence(attempt));
        }

        [Test]
        public void Puzzle_PhysicalLeftToRightPedestalOrder_Fails()
        {
            // The physical pedestals in the room are scrambled from left to right:
            // Green -> Yellow -> Red -> Blue
            // Pressing them in physical line order must FAIL!
            var physicalLineAttempt = new List<SwitchColor>
            {
                SwitchColor.Green,
                SwitchColor.Yellow,
                SwitchColor.Red,
                SwitchColor.Blue
            };
            Assert.IsFalse(ValidateSequence(physicalLineAttempt), "Pressing pedestals in sequential line order must not solve the puzzle!");
        }

        [Test]
        public void Puzzle_IncorrectOrder_Fails()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Blue,
                SwitchColor.Yellow,
                SwitchColor.Green,
                SwitchColor.Red
            };
            Assert.IsFalse(ValidateSequence(attempt));
        }

        [Test]
        public void Puzzle_PartialSequence_Fails()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Red,
                SwitchColor.Blue
            };
            Assert.IsFalse(ValidateSequence(attempt));
        }

        [Test]
        public void Puzzle_DuplicateColorInputs_Fails()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Red,
                SwitchColor.Red,
                SwitchColor.Red,
                SwitchColor.Red
            };
            Assert.IsFalse(ValidateSequence(attempt), "Duplicate color spam must not unlock the puzzle");
        }

        [Test]
        public void Puzzle_ReverseOrder_Fails()
        {
            var attempt = new List<SwitchColor>
            {
                SwitchColor.Green,
                SwitchColor.Yellow,
                SwitchColor.Blue,
                SwitchColor.Red
            };
            Assert.IsFalse(ValidateSequence(attempt), "Reverse sequence must not unlock the puzzle");
        }
    }
}
