using System.Collections;
using System.Collections.Generic;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Evaluates the Secret Laboratory switch sequence puzzle.
    /// Correct Sequence: Blue -> Yellow -> Green -> Red.
    /// (Glenmist Lore: Cold Mist -> Morning Sun -> Fresh Leaf -> Kiln Fire)
    /// If incorrect: plays alarm sound, flashes red warning lights, displays alert, resets switches.
    /// If correct: disables the energy barrier.
    /// </summary>
    public class LaboratoryPuzzleController : MonoBehaviour
    {
        public static LaboratoryPuzzleController Instance { get; private set; }

        [SerializeField] private LaboratorySwitch blueSwitch;
        [SerializeField] private LaboratorySwitch yellowSwitch;
        [SerializeField] private LaboratorySwitch greenSwitch;
        [SerializeField] private LaboratorySwitch redSwitch;
        [SerializeField] private EnergyBarrier energyBarrier;
        [SerializeField] private Light[] warningLights;

        [Header("Console Screen")]
        [SerializeField] private UnityEngine.UI.Text screenTitleText;
        [SerializeField] private UnityEngine.UI.Text screenSlotsText;
        [SerializeField] private UnityEngine.UI.Text screenStatusText;
        [SerializeField] private Light screenLight;

        private readonly List<SwitchColor> _correctSequence = new()
        {
            SwitchColor.Red,
            SwitchColor.Blue,
            SwitchColor.Yellow,
            SwitchColor.Green
        };

        private readonly List<SwitchColor> _currentSequence = new();
        private bool _isResetting;

        public bool IsSolved { get; private set; }

        public void Bind(LaboratorySwitch b, LaboratorySwitch y, LaboratorySwitch g, LaboratorySwitch r, EnergyBarrier barrier, Light[] lights = null)
        {
            blueSwitch = b;
            yellowSwitch = y;
            greenSwitch = g;
            redSwitch = r;
            energyBarrier = barrier;
            warningLights = lights;
        }

        public void BindScreen(UnityEngine.UI.Text title, UnityEngine.UI.Text slots, UnityEngine.UI.Text status, Light light)
        {
            screenTitleText = title;
            screenSlotsText = slots;
            screenStatusText = status;
            screenLight = light;
            UpdateScreenDisplay();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            UpdateScreenDisplay();
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

            UpdateScreenDisplay();

            // Correct step so far!
            if (_currentSequence.Count == _correctSequence.Count)
            {
                // All 4 entered in exact order: Fire (Red) -> Mist (Blue) -> Sun (Yellow) -> Leaf (Green)!
                StartCoroutine(SuccessSequence());
            }
            else
            {
                if (FinalRescueUI.Instance != null)
                {
                    FinalRescueUI.Instance.ShowHarvestPhaseAlert(stepIndex + 1, 4, activatedSwitch.LoreTitle);
                }
            }
        }

        public void UpdateScreenDisplay()
        {
            if (screenSlotsText == null) return;

            string s1 = _currentSequence.Count > 0 ? $"<color=#FF4444>[ {GetPhaseName(_currentSequence[0])} ]</color>" : "<color=#778899>[ 1: ??? ]</color>";
            string s2 = _currentSequence.Count > 1 ? $"<color=#44AAFF>[ {GetPhaseName(_currentSequence[1])} ]</color>" : "<color=#778899>[ 2: ??? ]</color>";
            string s3 = _currentSequence.Count > 2 ? $"<color=#FFAA22>[ {GetPhaseName(_currentSequence[2])} ]</color>" : "<color=#778899>[ 3: ??? ]</color>";
            string s4 = _currentSequence.Count > 3 ? $"<color=#55FF66>[ {GetPhaseName(_currentSequence[3])} ]</color>" : "<color=#778899>[ 4: ??? ]</color>";

            screenSlotsText.text = $"{s1}    {s2}    {s3}    {s4}";

            if (screenStatusText != null)
            {
                screenStatusText.text = _currentSequence.Count switch
                {
                    0 => "<color=#FF5555>STEP 1/4: KINDLE WITHERING KILN FURNACE (RED: FIRE)</color>",
                    1 => "<color=#44AAFF>STEP 2/4: CONDENSE HIGHLAND MOUNTAIN MIST (BLUE: MIST)</color>",
                    2 => "<color=#FFAA22>STEP 3/4: BATHE IN MORNING SUNRISE GOLD (YELLOW: SUN)</color>",
                    3 => "<color=#55FF66>STEP 4/4: HARVEST FRESH CEYLON LEAF (GREEN: LEAF)</color>",
                    _ => "<color=#55FF66>CIPHER VERIFIED: PRISON STASIS BARRIER DEACTIVATED</color>"
                };
            }
        }

        private string GetPhaseName(SwitchColor c) => c switch
        {
            SwitchColor.Red => "RED: FIRE",
            SwitchColor.Blue => "BLUE: MIST",
            SwitchColor.Yellow => "YELLOW: SUN",
            SwitchColor.Green => "GREEN: LEAF",
            _ => c.ToString()
        };

        private IEnumerator IncorrectSequenceSequence()
        {
            _isResetting = true;
            if (FinalRescueMission.Instance != null) FinalRescueMission.Instance.IsPuzzleLocked = true;

            // 1. Play alarm sound, electrical short circuit & UI message
            ProceduralAudio.PlayAt("alarm", transform.position, 1.2f, 1f);
            ProceduralAudio.PlayAt("shortCircuit", transform.position, 0.85f, 0.9f);

            // Announce Overseer Alarm Voice line
            GameEvents.Overseer("SECURITY LOCKDOWN: Unauthorized override in Secret Laboratory! Defense squad, terminate the intruder!");

            // 2. Alert and dispatch robot defense squad to attack player in Secret Laboratory
            Vector3 alertPos = transform.position + new Vector3(-2.8f, 0f, 0f);
            if (MistProtocol.AI.Director.OverseerDirector.Instance != null)
            {
                MistProtocol.AI.Director.OverseerDirector.Instance.DispatchSecurityReinforcements(alertPos, 2);
            }
            else
            {
                SpawnFallbackRobots(alertPos, 2);
            }

            if (screenSlotsText != null) screenSlotsText.text = "<color=#FF2222>[ ERROR ]   [ ERROR ]   [ ERROR ]   [ ERROR ]</color>";
            if (screenStatusText != null) screenStatusText.text = "<color=#FF3333>SEQUENCE MISMATCH: Circuit Overload! Robots Deployed!</color>";
            if (screenLight != null) screenLight.color = Color.red;

            // Emit electrical short-circuit spark bursts from switches
            if (blueSwitch != null) Vfx.Burst(blueSwitch.transform.position + Vector3.up * 0.9f, Vector3.up, new Color(0.2f, 0.7f, 1f), 14, 3f, 0.08f, 0.4f);
            if (yellowSwitch != null) Vfx.Burst(yellowSwitch.transform.position + Vector3.up * 0.9f, Vector3.up, new Color(1f, 0.8f, 0.2f), 14, 3f, 0.08f, 0.4f);
            if (greenSwitch != null) Vfx.Burst(greenSwitch.transform.position + Vector3.up * 0.9f, Vector3.up, new Color(0.2f, 1f, 0.4f), 14, 3f, 0.08f, 0.4f);
            if (redSwitch != null) Vfx.Burst(redSwitch.transform.position + Vector3.up * 0.9f, Vector3.up, new Color(1f, 0.3f, 0.2f), 14, 3f, 0.08f, 0.4f);

            // Circuit overload spark surge along floor power conduits connecting console to barrier
            Vector3 conduitStart1 = new Vector3(45.7f, 0.05f, 10.5f);
            Vector3 conduitStart2 = new Vector3(47.3f, 0.05f, 10.5f);
            for (int step = 0; step < 4; step++)
            {
                float t = step / 3.0f;
                Vector3 p1 = Vector3.Lerp(conduitStart1, new Vector3(45.7f, 0.05f, 12.8f), t);
                Vector3 p2 = Vector3.Lerp(conduitStart2, new Vector3(47.3f, 0.05f, 12.8f), t);
                Vfx.Burst(p1, Vector3.up, new Color(0.3f, 0.8f, 1f), 8, 2.5f, 0.06f, 0.3f);
                Vfx.Burst(p2, Vector3.up, new Color(1f, 0.4f, 0.2f), 8, 2.5f, 0.06f, 0.3f);
            }

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowPuzzleIncorrectAlert("INCORRECT CIPHER! Security alarm triggered - Defense robots attacking!");
            }

            // 2. Flash red warning lights with balanced intensity
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
                            l.intensity = 2.0f;
                        }
                    }
                    yield return new WaitForSeconds(0.2f);
                    foreach (var l in warningLights)
                    {
                        if (l != null) l.intensity = 0.35f;
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
            if (yellowSwitch != null) yellowSwitch.SetInactive();
            if (greenSwitch != null) greenSwitch.SetInactive();
            if (redSwitch != null) redSwitch.SetInactive();

            _currentSequence.Clear();
            if (screenLight != null) screenLight.color = new Color(0.2f, 0.75f, 1f);
            UpdateScreenDisplay();

            _isResetting = false;
            if (FinalRescueMission.Instance != null) FinalRescueMission.Instance.IsPuzzleLocked = false;
        }

        private IEnumerator SuccessSequence()
        {
            IsSolved = true;

            if (screenSlotsText != null) screenSlotsText.text = "<color=#55FF66>[ RED: FIRE ]   [ BLUE: MIST ]   [ YELLOW: SUN ]   [ GREEN: LEAF ]</color>";
            if (screenStatusText != null) screenStatusText.text = "<color=#55FF66>ACCESS GRANTED: ALL 4 PHASES VERIFIED. APPA IS FREE!</color>";
            if (screenLight != null) screenLight.color = new Color(0.2f, 1f, 0.5f);

            // 1. Play success fanfare / chime
            ProceduralAudio.PlayAt("success", transform.position, 1f, 1f);
            ProceduralAudio.PlayAt("pickup", transform.position, 1f, 1.4f);

            // 2. Set all switches to stable green glow
            if (blueSwitch != null) blueSwitch.SetSolved();
            if (yellowSwitch != null) yellowSwitch.SetSolved();
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

        private void SpawnFallbackRobots(Vector3 spawnCenter, int count)
        {
            var resPrefab = Resources.Load<GameObject>("Agent_Sentinel") ?? Resources.Load<GameObject>("Agent_HunterDrone");
            Vector3 target = PlayerLocator.Alive ? PlayerLocator.State.Root.position : transform.position;

            if (resPrefab != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = spawnCenter + new Vector3(-0.6f * i, 0f, (i % 2 == 0 ? -1f : 1f));
                    var bot = Instantiate(resPrefab, p, Quaternion.LookRotation((target - p).Flat().normalized));
                    bot.name = $"Alarm_Sentinel_{i + 1}";
                    var agent = bot.GetComponent<MistProtocol.AI.Agents.AgentBase>();
                    if (agent != null && agent.Sensor != null)
                    {
                        agent.Sensor.ReportPosition(target, 1.0f);
                    }
                }
            }

            // Alert any active agents across the facility
            foreach (var a in MistProtocol.AI.Agents.AgentBase.Active)
            {
                if (a != null && a.Sensor != null) a.Sensor.ReportPosition(target, 1f);
            }
        }
    }
}
