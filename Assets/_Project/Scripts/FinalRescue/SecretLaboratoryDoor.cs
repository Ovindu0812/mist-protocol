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
        [SerializeField] private Transform wallCoverPlate;
        [SerializeField] private Light terminalStatusLight;

        private bool _isRevealed;
        private bool _isUnlocked;
        private bool _isOpening;

        public bool IsRevealed => _isRevealed;
        public bool IsUnlocked => _isUnlocked;
        public Transform TerminalTransform => keyTerminal != null ? keyTerminal : transform;

        public bool CanInteract => _isRevealed && !_isUnlocked && !_isOpening;

        public string Prompt
        {
            get
            {
                if (!_isRevealed) return "[Sealed Wall Mechanism - Defeat Titan]";
                if (_isUnlocked) return "[Secret Laboratory Unlocked]";
                bool hasKey = FinalRescueMission.Instance != null && FinalRescueMission.Instance.HasMasterAccessKey;
                if (!hasKey && GameManager.Instance != null)
                {
                    hasKey = GameManager.Instance.HasMasterCellKey;
                }
                return hasKey
                    ? "[E] Insert Master Access Key into revealed door terminal"
                    : "[Locked] Requires Master Access Key (Defeat Titan Automaton)";
            }
        }

        public void Bind(Transform door, Transform terminal, Light statusLight, Transform coverPlate = null)
        {
            slidingDoor = door;
            keyTerminal = terminal;
            terminalStatusLight = statusLight;
            wallCoverPlate = coverPlate;
        }

        private void Awake()
        {
            if (slidingDoor == null) slidingDoor = transform.Find("Lab_SlidingDoor");
            if (keyTerminal == null) keyTerminal = transform.Find("Lab_Terminal");
            if (wallCoverPlate == null) wallCoverPlate = transform.Find("Lab_WallCoverPlate");
            if (terminalStatusLight == null && keyTerminal != null)
                terminalStatusLight = keyTerminal.GetComponentInChildren<Light>();
        }

        public void RevealDoor()
        {
            if (_isRevealed) return;
            _isRevealed = true;
            StartCoroutine(RevealSequence());
        }

        private IEnumerator RevealSequence()
        {
            // Hydraulic sound, heavy steam burst & mechanical unlatching
            ProceduralAudio.PlayAt("door", transform.position, 1.4f, 0.5f);
            ProceduralAudio.PlayAt("steam", transform.position, 1.2f, 0.75f);
            ProceduralAudio.PlayAt("zap", transform.position, 0.8f, 1.1f);

            // Upper steam venting
            Vfx.Burst(transform.position + Vector3.up * 2.8f, Vector3.forward, new Color(0.85f, 0.9f, 0.95f), 40, 5.0f, 0.2f, 0.9f);
            // Ground perimeter dust discharge
            Vfx.Burst(transform.position + Vector3.up * 0.1f, Vector3.up, new Color(0.5f, 0.45f, 0.4f), 25, 2.5f, 0.25f, 0.7f);

            if (terminalStatusLight != null)
            {
                terminalStatusLight.enabled = true;
                terminalStatusLight.color = new Color(1f, 0.65f, 0.2f);
                terminalStatusLight.intensity = 1.4f;
                terminalStatusLight.range = 3.2f;
            }

            GameEvents.Overseer("SECURITY SENSORS: Massive hydraulic shift detected! A hidden blast door has emerged from the Sector E perimeter wall!");
            GameEvents.Objective("The Secret Laboratory door has revealed itself! Swipe the Master Access Key to enter!");

            if (wallCoverPlate != null)
            {
                Vector3 startPos = wallCoverPlate.localPosition;
                Vector3 endPos = startPos + new Vector3(0f, -3.8f, 0f); // Retract down into floor
                float elapsed = 0f;
                while (elapsed < 2.0f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / 2.0f);
                    wallCoverPlate.localPosition = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                wallCoverPlate.localPosition = endPos;
                wallCoverPlate.gameObject.SetActive(false);
            }
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
