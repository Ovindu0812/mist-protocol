using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    public enum LabClueType
    {
        ComputerTerminal,
        HiddenDocument,
        WallMessage,
        ResearchNotes
    }

    /// <summary>
    /// Interactive clue placed inside the Secret Laboratory.
    /// Provides hints for the switch sequence puzzle and tracks collection progress.
    /// </summary>
    public class SecretLaboratoryClue : MonoBehaviour, IInteractable
    {
        [SerializeField] private LabClueType clueType;
        [SerializeField] private string title = "Clue";
        [SerializeField] private string message = "";

        private bool _hasBeenRead;
        private Light _glowLight;

        public bool CanInteract => true; // Can always be re-read if player wants to check again

        public string Prompt => clueType switch
        {
            LabClueType.ComputerTerminal => "[E] Access Security Terminal Diagnostic",
            LabClueType.HiddenDocument => "[E] Inspect Withering Loft Plantation Log",
            LabClueType.WallMessage => "[E] Read Appa's Scratched Wall Inscription",
            LabClueType.ResearchNotes => "[E] Read Appa's Secret Emergency Cipher",
            _ => "[E] Inspect Clue"
        };

        public void Configure(LabClueType type)
        {
            clueType = type;
            switch (type)
            {
                case LabClueType.ComputerTerminal:
                    title = "Clue 1: OVERSEER Terminal Diagnostic";
                    message = "[EMERGENCY OVERRIDE CONSOLE ACTIVATED]\n'Appa rewired the Master Console Workstation. The security cipher requires Phase 1 to ignite the thermal drying kiln (RED - Kiln Fire).' Verify slots on the workstation screen.";
                    break;
                case LabClueType.HiddenDocument:
                    title = "Clue 2: Highland Plantation Log";
                    message = "'In the Nuwara Eliya hills, after the kiln furnace roars (Phase 1: Red), the cold mountain mist blankets the rolling room at midnight (Phase 2: Midnight Mist / BLUE).'";
                    break;
                case LabClueType.WallMessage:
                    title = "Clue 3: Scratched Wall Message";
                    message = "'Nila, duwa... At dawn, the golden sunrise warms the dew (Phase 3: Morning Sun / YELLOW) before the harvesters take to the estate.'";
                    break;
                case LabClueType.ResearchNotes:
                    title = "Clue 4: Appa's Emergency Cipher";
                    message = "'TO MY DAUGHTER NILA:\nThe Master Console screen tracks the 4 sacred harvest phases:\n\n1. FIRE (Red) -> 2. MIST (Blue) -> 3. SUN (Yellow) -> 4. LEAF (Green)!\n\nInput the sequence using the 4 desk buttons to shut down the prison field!'";
                    break;
            }
        }

        private void Awake()
        {
            _glowLight = GetComponentInChildren<Light>();
        }

        public void Interact()
        {
            ProceduralAudio.PlayAt("pickup", transform.position, 0.9f, 1.2f);

            bool isFirstRead = !_hasBeenRead;
            _hasBeenRead = true;

            if (_glowLight != null)
            {
                _glowLight.color = new Color(0.3f, 0.95f, 0.6f);
                _glowLight.intensity = 0.75f;
                _glowLight.range = 2.2f;
            }

            // Notify mission coordinator of discovery
            if (FinalRescueMission.Instance != null)
            {
                FinalRescueMission.Instance.OnClueInspected(clueType, title, message, isFirstRead);
            }
            else if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowCluePopup(title, message, 1, 4);
            }
        }
    }
}
