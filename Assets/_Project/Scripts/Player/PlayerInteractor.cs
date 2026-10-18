using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MistProtocol.Player
{
    /// <summary>
    /// E = use (doors, card reader, kill switch), F = grab / drop physics props,
    /// LMB while holding = throw. Held props are moved by setting velocity toward
    /// a hold point (not parenting), so they still collide with the world and
    /// can't be pushed through walls.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 3f;
        [SerializeField] private float holdDistance = 1.8f;
        [SerializeField] private float throwSpeed = 15f;
        [SerializeField] private float maxCarryMass = 25f;

        private PlayerController _player;
        private Camera _cam;
        private Throwable _held;

        public bool IsHolding => _held != null || Time.time - _releasedAt < 0.25f;
        private float _releasedAt = -9f;
        public string Prompt { get; private set; }
        public float HoldProgress { get; private set; } = -1f;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            _cam = _player.GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            Prompt = null;
            HoldProgress = -1f;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (!_player.InputEnabled || _player.Health.IsDead || kb == null) { Drop(Vector3.zero); return; }

            if (_held != null && _held.Body == null) _held = null;
            if (_held != null)
            {
                Prompt = "[LMB] Throw   [F] Drop";
                if (mouse.leftButton.wasPressedThisFrame) Drop(_cam.transform.forward * throwSpeed * _held.Body.mass);
                else if (kb.fKey.wasPressedThisFrame) Drop(Vector3.zero);
                return;
            }

            if (!Physics.Raycast(_cam.transform.position, _cam.transform.forward, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
                return;

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                Prompt = interactable.Prompt;
                if (interactable is IHoldInteractable hold)
                {
                    HoldProgress = hold.HoldProgress;
                    if (kb.eKey.isPressed && hold.CanInteract) hold.Hold(Time.deltaTime);
                }
                else if (kb.eKey.wasPressedThisFrame) interactable.Interact();
                return;
            }

            var throwable = hit.collider.GetComponentInParent<Throwable>();
            if (throwable != null && throwable.Body.mass <= maxCarryMass)
            {
                Prompt = "[F] Pick up";
                if (kb.fKey.wasPressedThisFrame)
                {
                    _held = throwable;
                    _held.OnPickedUp();
                }
                return;
            }

            if (hit.collider.GetComponentInParent<Barricade>() != null) Prompt = "Walk into the crate to push it";
        }

        private void FixedUpdate()
        {
            if (_held == null) return;
            if (_held.Body == null) { _held = null; return; }
            Vector3 target = _cam.transform.position + _cam.transform.forward * holdDistance;
            Vector3 to = target - _held.Body.position;
            if (to.magnitude > 2.5f) { Drop(Vector3.zero); return; } // snagged on geometry
            _held.Body.linearVelocity = to * 14f;
            _held.Body.angularVelocity *= 0.8f;
        }

        private void Drop(Vector3 impulse)
        {
            if (_held == null) return;
            if (_held.Body != null) _held.OnReleased(impulse);
            _held = null;
            _releasedAt = Time.time;
        }
    }
}
