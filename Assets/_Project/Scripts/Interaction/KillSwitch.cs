using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>OVERSEER's kill switch: hold E to drain the core. Starts the final wave.</summary>
    public class KillSwitch : MonoBehaviour, IHoldInteractable
    {
        [SerializeField] private float holdSeconds = 3.5f;
        private float _progress;
        private bool _started, _done;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                // Ensure a generous, easy-to-target interaction box around the console
                col.size = new Vector3(Mathf.Max(col.size.x, 2.2f), Mathf.Max(col.size.y, 1.6f), Mathf.Max(col.size.z, 1.8f));
                col.center = new Vector3(col.center.x, Mathf.Max(col.center.y, 0.75f), col.center.z);
            }
            else
            {
                col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(2.2f, 1.6f, 1.8f);
                col.center = new Vector3(0f, 0.75f, 0f);
            }
        }

        public float HoldProgress => Mathf.Clamp01(_progress / holdSeconds);
        public string Prompt
        {
            get
            {
                if (_done) return "OVERSEER offline";
                if (GameManager.Instance != null && !GameManager.Instance.CanUseKillSwitch)
                    return "[Locked] Awaiting containment clearance";
                return $"[Hold E] Pull the kill switch  {Mathf.RoundToInt(HoldProgress * 100)}%";
            }
        }
        public bool CanInteract => !_done && GameManager.Instance != null && GameManager.Instance.CanUseKillSwitch;

        public void Interact()
        {
            if (CanInteract)
            {
                Hold(0.15f);
            }
        }

        public void Hold(float dt)
        {
            if (!CanInteract) return;
            if (!_started)
            {
                _started = true;
                GameManager.Instance.OnKillSwitchStarted();
            }
            _progress += dt;
            if (Mathf.FloorToInt((_progress - dt) * 3f) != Mathf.FloorToInt(_progress * 3f))
                ProceduralAudio.PlayAt("beep", transform.position, 0.45f, 0.8f + HoldProgress * 0.8f);
            if (_progress >= holdSeconds)
            {
                _done = true;
                GameManager.Instance.OnKillSwitchCompleted();
            }
        }
    }
}
