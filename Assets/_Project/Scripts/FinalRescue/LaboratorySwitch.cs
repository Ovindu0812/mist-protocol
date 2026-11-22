using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    public enum SwitchColor
    {
        Blue,
        Yellow,
        Green,
        Red
    }

    /// <summary>
    /// Interactive console switch inside the Secret Laboratory.
    /// Can be activated individually; lights up with high emission upon engagement.
    /// </summary>
    public class LaboratorySwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private SwitchColor switchColor;
        [SerializeField] private Renderer switchRenderer;
        [SerializeField] private Light statusLight;

        public SwitchColor Color => switchColor;
        public bool IsActive { get; private set; }

        public bool CanInteract => !IsActive && (FinalRescueMission.Instance == null || !FinalRescueMission.Instance.IsPuzzleLocked);

        public string Prompt => IsActive
            ? $"[{switchColor} Switch Active]"
            : $"[E] Activate {switchColor} Switch";

        public void Configure(SwitchColor color, Renderer rend = null, Light light = null)
        {
            switchColor = color;
            if (rend != null) switchRenderer = rend;
            if (light != null) statusLight = light;
            UpdateVisuals();
        }

        private void Awake()
        {
            if (switchRenderer == null) switchRenderer = GetComponentInChildren<Renderer>();
            if (statusLight == null) statusLight = GetComponentInChildren<Light>();
            UpdateVisuals();
        }

        public void Interact()
        {
            if (IsActive) return;
            if (FinalRescueMission.Instance != null && FinalRescueMission.Instance.IsPuzzleLocked) return;

            IsActive = true;
            UpdateVisuals();

            // Play sound based on color pitch
            float pitch = switchColor switch
            {
                SwitchColor.Blue => 1.0f,
                SwitchColor.Yellow => 1.15f,
                SwitchColor.Green => 1.30f,
                SwitchColor.Red => 1.50f,
                _ => 1.0f
            };
            ProceduralAudio.PlayAt("beep", transform.position, 0.85f, pitch);
            ProceduralAudio.PlayAt("zap", transform.position, 0.4f, pitch);

            // Notify puzzle controller
            if (LaboratoryPuzzleController.Instance != null)
            {
                LaboratoryPuzzleController.Instance.OnSwitchActivated(this);
            }
        }

        public void SetInactive()
        {
            IsActive = false;
            UpdateVisuals();
        }

        public void SetSolved()
        {
            IsActive = true;
            if (statusLight != null)
            {
                statusLight.color = new Color(0.2f, 1f, 0.4f);
                statusLight.intensity = 3f;
            }
            if (switchRenderer != null && switchRenderer.material != null)
            {
                switchRenderer.material.EnableKeyword("_EMISSION");
                switchRenderer.material.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 3f);
            }
        }

        private void UpdateVisuals()
        {
            UnityEngine.Color baseCol = switchColor switch
            {
                SwitchColor.Blue => new UnityEngine.Color(0.1f, 0.5f, 1f),
                SwitchColor.Yellow => new UnityEngine.Color(1f, 0.72f, 0.05f),
                SwitchColor.Green => new UnityEngine.Color(0.15f, 0.95f, 0.35f),
                SwitchColor.Red => new UnityEngine.Color(1f, 0.2f, 0.15f),
                _ => UnityEngine.Color.white
            };

            if (statusLight != null)
            {
                statusLight.color = baseCol;
                statusLight.intensity = IsActive ? 3.5f : 0.4f;
            }

            if (switchRenderer != null && switchRenderer.material != null)
            {
                var mat = switchRenderer.material;
                mat.EnableKeyword("_EMISSION");
                if (IsActive)
                {
                    mat.SetColor("_BaseColor", baseCol);
                    mat.SetColor("_EmissionColor", baseCol * 4.0f);
                }
                else
                {
                    mat.SetColor("_BaseColor", baseCol * 0.4f);
                    mat.SetColor("_EmissionColor", baseCol * 0.25f);
                }
            }
        }
    }
}
