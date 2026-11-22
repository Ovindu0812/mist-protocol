using System.Collections;
using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Reinforced blast door & security terminal guarding the entrance to the Secret Laboratory.
    /// Can only be opened with the Master Access Key dropped by the Titan Robot.
    /// </summary>
    public class SecretLaboratoryDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform slidingDoor;
        [SerializeField] private Transform keyTerminal;
        [SerializeField] private Light terminalStatusLight;

        private bool _isUnlocked;
        private bool _isOpening;

        public bool IsUnlocked => _isUnlocked;
        public Transform TerminalTransform => keyTerminal != null ? keyTerminal : transform;

        public bool CanInteract => !_isUnlocked && !_isOpening;

        public string Prompt
        {
            get
            {
                if (_isUnlocked) return "[Secret Laboratory Unlocked]";
                bool hasKey = FinalRescueMission.Instance != null && FinalRescueMission.Instance.HasMasterAccessKey;
                if (!hasKey && GameManager.Instance != null)
                {
                    hasKey = GameManager.Instance.HasMasterCellKey;
                }
                return hasKey
                    ? "[E] Swipe Master Access Key to open Secret Laboratory"
                    : "[Locked] Requires Master Access Key (Defeat Titan Robot)";
            }
        }

        public void Bind(Transform door, Transform terminal, Light statusLight)
        {
            slidingDoor = door;
            keyTerminal = terminal;
            terminalStatusLight = statusLight;
        }

        private void Awake()
        {
            if (slidingDoor == null) slidingDoor = transform.Find("Lab_SlidingDoor");
            if (keyTerminal == null) keyTerminal = transform.Find("Lab_Terminal");
            if (terminalStatusLight == null && keyTerminal != null)
                terminalStatusLight = keyTerminal.GetComponentInChildren<Light>();
        }

        public void Interact()
        {
            if (_isUnlocked || _isOpening) return;

            bool hasKey = FinalRescueMission.Instance != null && FinalRescueMission.Instance.HasMasterAccessKey;
            if (!hasKey && GameManager.Instance != null)
            {
                hasKey = GameManager.Instance.HasMasterCellKey;
            }

            if (!hasKey)
            {
                ProceduralAudio.PlayAt("zap", transform.position, 0.6f, 0.5f);
                GameEvents.Overseer("SECURITY SCANNER: ACCESS DENIED. Master Access Key authorization required.");
                return;
            }

            _isUnlocked = true;
            StartCoroutine(OpenDoorSequence());
        }

        private IEnumerator OpenDoorSequence()
        {
            _isOpening = true;

            // Audio & lighting
            ProceduralAudio.PlayAt("door", transform.position, 1.2f, 0.75f);
            ProceduralAudio.PlayAt("pickup", transform.position, 0.8f, 1.4f);

            if (terminalStatusLight != null)
            {
                terminalStatusLight.color = new Color(0.2f, 1f, 0.4f);
                terminalStatusLight.intensity = 4f;
            }

            GameEvents.Overseer("SECURITY SCANNER: MASTER ACCESS KEY VERIFIED. Opening Secret Laboratory.");
            GameEvents.Objective("Investigate the Secret Laboratory and disable the prison barrier!");

            // Slide door open smoothly
            if (slidingDoor != null)
            {
                Vector3 startPos = slidingDoor.localPosition;
                Vector3 endPos = startPos + new Vector3(0f, 0f, 2.6f);

                float elapsed = 0f;
                while (elapsed < 1.8f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / 1.8f);
                    slidingDoor.localPosition = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                slidingDoor.localPosition = endPos;

                // Disable colliders so player can walk through freely
                foreach (var c in slidingDoor.GetComponentsInChildren<Collider>())
                {
                    c.enabled = false;
                }
            }

            _isOpening = false;

            if (FinalRescueMission.Instance != null)
            {
                FinalRescueMission.Instance.OnLaboratoryOpened();
            }
        }
    }
}
