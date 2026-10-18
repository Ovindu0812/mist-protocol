using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>OVERSEER's kill switch: hold E to drain the core. Starts the final wave.</summary>
    public class KillSwitch : MonoBehaviour, IHoldInteractable
    {
        [SerializeField] private float holdSeconds = 8f;
        private float _progress;
        private bool _started, _done;

        public float HoldProgress => _progress / holdSeconds;
        public string Prompt => _done ? "OVERSEER offline" : $"[Hold E] Pull the kill switch  {Mathf.RoundToInt(HoldProgress * 100)}%";
        public bool CanInteract => !_done && GameManager.Instance != null && GameManager.Instance.CanUseKillSwitch;

        public void Interact() { }

        public void Hold(float dt)
        {
            if (!CanInteract) return;
            if (!_started) { _started = true; GameManager.Instance.OnKillSwitchStarted(); }
            _progress += dt;
            if (Mathf.FloorToInt((_progress - dt) * 2f) != Mathf.FloorToInt(_progress * 2f))
                ProceduralAudio.PlayAt("beep", transform.position, 0.4f, 0.8f + HoldProgress);
            if (_progress >= holdSeconds)
            {
                _done = true;
                GameManager.Instance.OnKillSwitchCompleted();
            }
        }
    }
}
