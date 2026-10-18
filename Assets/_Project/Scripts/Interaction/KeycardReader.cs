using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>Card reader that unlocks a door leaf (the Drying Chamber gate).</summary>
    public class KeycardReader : MonoBehaviour, IInteractable
    {
        [SerializeField] private PhysicsDoor door;
        [SerializeField] private Renderer statusLight;
        private bool _used;

        public void Bind(PhysicsDoor target, Renderer lamp) { door = target; statusLight = lamp; }

        public string Prompt => _used ? "Unlocked" :
            GameManager.Instance != null && GameManager.Instance.HasKeycard ? "[E] Swipe keycard" : "Keycard required";
        public bool CanInteract => !_used && GameManager.Instance != null && GameManager.Instance.HasKeycard;

        public void Interact()
        {
            if (!CanInteract) { ProceduralAudio.PlayAt("beep", transform.position, 0.5f, 0.6f); return; }
            _used = true;
            ProceduralAudio.PlayAt("beep", transform.position);
            if (door != null)
            {
                door.SetLocked(false);
                door.OpenAwayFrom(PlayerLocator.State.Root.position);
            }
            if (statusLight != null) statusLight.material.SetColor("_EmissionColor", Color.green * 3f);
            GameManager.Instance.OnGateUnlocked();
        }
    }
}
