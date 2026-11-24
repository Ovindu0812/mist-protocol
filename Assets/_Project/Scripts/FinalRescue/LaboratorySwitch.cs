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

        public string LoreTitle => switchColor switch
        {
            SwitchColor.Red => "Phase 1: Kiln Fire (RED)",
            SwitchColor.Blue => "Phase 2: Mountain Mist (BLUE)",
            SwitchColor.Yellow => "Phase 3: Morning Sun (YELLOW)",
            SwitchColor.Green => "Phase 4: Ceylon Leaf (GREEN)",
            _ => switchColor.ToString()
        };

        public string Prompt => IsActive
            ? $"[{LoreTitle} - ENGAGED]"
            : $"[E] Activate {LoreTitle}";

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

            // Calibrated tactile mechanical relay feedback & resonant harmonic acoustics
            float pitch = switchColor switch
            {
                SwitchColor.Blue => 1.00f,   // Highland Mountain Mist: deep foundational bass resonance
                SwitchColor.Yellow => 1.15f, // Morning Sunrise Gold: mid-high harmonic chime
                SwitchColor.Green => 1.30f,  // Emerald Ceylon Leaf: vibrant mid-frequency click
                SwitchColor.Red => 1.50f,    // Kiln Flame: high-frequency crisp energization
                _ => 1.0f
            };
            ProceduralAudio.PlayAt("beep", transform.position, 0.85f, pitch);
            ProceduralAudio.PlayAt("pickup", transform.position, 0.70f, 1.65f);
            ProceduralAudio.PlayAt("zap", transform.position, 0.40f, pitch * 1.1f);

            // Notify puzzle controller
            if (LaboratoryPuzzleController.Instance != null)
            {
                LaboratoryPuzzleController.Instance.OnSwitchActivated(this);
            }
        }

        public void SetInactive()
        {
            if (IsActive)
            {
                // Mechanical relay de-energize release sound and circuit fizzle cue
                ProceduralAudio.PlayAt("click", transform.position, 0.60f, 0.75f);
                ProceduralAudio.PlayAt("shortCircuit", transform.position, 0.35f, 1.25f);
            }
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
