using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    public enum LabClueType
    {
        ComputerTerminal,
        HiddenDocument,
        WallMessage
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
            LabClueType.ComputerTerminal => "[E] Read Computer Terminal",
            LabClueType.HiddenDocument => "[E] Inspect Hidden Research Document",
            LabClueType.WallMessage => "[E] Read Scratched Wall Inscription",
            _ => "[E] Inspect Clue"
        };

        public void Configure(LabClueType type)
        {
            clueType = type;
            switch (type)
            {
                case LabClueType.ComputerTerminal:
                    title = "Clue 1: Computer Terminal";
                    message = "Blue comes before Green.";
                    break;
                case LabClueType.HiddenDocument:
                    title = "Clue 2: Hidden Document";
                    message = "Red must be activated last.";
                    break;
                case LabClueType.WallMessage:
                    title = "Clue 3: Wall Message";
                    message = "The correct sequence will disable the prison barrier.";
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
                _glowLight.intensity = 2.5f;
            }

            // Notify mission coordinator of discovery
            if (FinalRescueMission.Instance != null)
            {
                FinalRescueMission.Instance.OnClueInspected(clueType, title, message, isFirstRead);
            }
            else if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowCluePopup(title, message, 1, 3);
            }
        }
    }
}
