using System.Collections;
using System.Collections.Generic;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Evaluates the Secret Laboratory switch sequence puzzle.
    /// Correct Sequence: Blue -> Green -> Red.
    /// If incorrect: plays alarm sound, flashes red warning lights, displays alert, resets switches.
    /// If correct: disables the energy barrier.
    /// </summary>
    public class LaboratoryPuzzleController : MonoBehaviour
    {
        public static LaboratoryPuzzleController Instance { get; private set; }

        [SerializeField] private LaboratorySwitch blueSwitch;
        [SerializeField] private LaboratorySwitch greenSwitch;
        [SerializeField] private LaboratorySwitch redSwitch;
        [SerializeField] private EnergyBarrier energyBarrier;
        [SerializeField] private Light[] warningLights;

        private readonly List<SwitchColor> _correctSequence = new()
        {
            SwitchColor.Blue,
            SwitchColor.Green,
            SwitchColor.Red
        };

        private readonly List<SwitchColor> _currentSequence = new();
        private bool _isResetting;

        public bool IsSolved { get; private set; }

        public void Bind(LaboratorySwitch b, LaboratorySwitch g, LaboratorySwitch r, EnergyBarrier barrier, Light[] lights = null)
        {
            blueSwitch = b;
            greenSwitch = g;
            redSwitch = r;
            energyBarrier = barrier;
            warningLights = lights;
        }

        private void Awake()
        {
            Instance = this;
        }

        public void OnSwitchActivated(LaboratorySwitch activatedSwitch)
        {
            if (IsSolved || _isResetting) return;

            _currentSequence.Add(activatedSwitch.Color);
            int stepIndex = _currentSequence.Count - 1;

            // Check if this step matches the correct sequence
            if (_currentSequence[stepIndex] != _correctSequence[stepIndex])
            {
                // Mismatch! Trigger reset
                StartCoroutine(IncorrectSequenceSequence());
                return;
            }

            // Correct step so far!
            if (_currentSequence.Count == _correctSequence.Count)
            {
                // All 3 entered in exact order: Blue -> Green -> Red!
                StartCoroutine(SuccessSequence());
            }
        }

        private IEnumerator IncorrectSequenceSequence()
        {
            _isResetting = true;
            if (FinalRescueMission.Instance != null) FinalRescueMission.Instance.IsPuzzleLocked = true;

            // 1. Play alarm sound & UI message
            ProceduralAudio.PlayAt("alarm", transform.position, 1f, 1f);
            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowPuzzleIncorrectAlert("Incorrect sequence. Check the clues and try again.");
            }

            // 2. Flash red warning lights
            if (warningLights != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    foreach (var l in warningLights)
                    {
                        if (l != null)
                        {
                            l.enabled = true;
                            l.color = Color.red;
                            l.intensity = 4f;
                        }
                    }
                    yield return new WaitForSeconds(0.2f);
                    foreach (var l in warningLights)
                    {
                        if (l != null) l.intensity = 0.5f;
                    }
                    yield return new WaitForSeconds(0.2f);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            // 3. Reset switches to inactive
            if (blueSwitch != null) blueSwitch.SetInactive();
            if (greenSwitch != null) greenSwitch.SetInactive();
            if (redSwitch != null) redSwitch.SetInactive();

            _currentSequence.Clear();
            _isResetting = false;
            if (FinalRescueMission.Instance != null) FinalRescueMission.Instance.IsPuzzleLocked = false;
        }

        private IEnumerator SuccessSequence()
        {
            IsSolved = true;

            // 1. Play success fanfare / chime
            ProceduralAudio.PlayAt("success", transform.position, 1f, 1f);
            ProceduralAudio.PlayAt("pickup", transform.position, 1f, 1.4f);

            // 2. Set all switches to stable green glow
            if (blueSwitch != null) blueSwitch.SetSolved();
            if (greenSwitch != null) greenSwitch.SetSolved();
            if (redSwitch != null) redSwitch.SetSolved();

            if (warningLights != null)
            {
                foreach (var l in warningLights)
                {
                    if (l != null)
                    {
                        l.color = new Color(0.2f, 1f, 0.4f);
                        l.intensity = 2f;
                    }
                }
            }

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowPuzzleSuccessAlert("ENERGY BARRIER DEACTIVATED! Speak with your father!");
            }

            yield return new WaitForSeconds(0.5f);

            // 3. Disable the energy barrier
            if (energyBarrier != null)
            {
                energyBarrier.DisableBarrier();
            }

            // 4. Notify mission controller
            if (FinalRescueMission.Instance != null)
            {
                FinalRescueMission.Instance.OnPuzzleSolved();
            }
        }
    }
}
