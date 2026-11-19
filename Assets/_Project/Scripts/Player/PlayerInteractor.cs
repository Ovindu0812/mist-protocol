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
        [SerializeField] private float reach = 4.2f;
        [SerializeField] private float holdDistance = 1.8f;
        [SerializeField] private float throwSpeed = 15f;
        [SerializeField] private float maxCarryMass = 25f;

        private PlayerController _player;
        private Camera _cam;
        private Throwable _held;
        private IHoldInteractable _activeHold;

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
            if (!_player.InputEnabled || _player.Health.IsDead || kb == null)
            {
                Drop(Vector3.zero);
                _activeHold = null;
                return;
            }

            if (_held != null && _held.Body == null) _held = null;
            if (_held != null)
            {
                Prompt = "[T / LMB] Throw   [F] Drop";
                bool throwPressed = (kb.tKey != null && kb.tKey.wasPressedThisFrame) ||
                                    (mouse != null && mouse.leftButton.wasPressedThisFrame);
                if (throwPressed) Drop(_cam.transform.forward * throwSpeed * _held.Body.mass);
                else if (kb.fKey.wasPressedThisFrame) Drop(Vector3.zero);
                return;
            }

            // Continuous hold grace: Keep holding if E is pressed and player remains in range
            if (_activeHold != null && kb.eKey.isPressed && _activeHold.CanInteract)
            {
                var comp = _activeHold as Component;
                float dist = comp != null ? Vector3.Distance(_cam.transform.position, comp.transform.position) : 0f;
                if (dist <= reach * 1.5f)
                {
                    Prompt = _activeHold.Prompt;
                    HoldProgress = _activeHold.HoldProgress;
                    _activeHold.Hold(Time.deltaTime);
                    if (_activeHold.HoldProgress >= 1f)
                    {
                        _activeHold = null;
                    }
                    return;
                }
            }
            _activeHold = null;

            IInteractable interactable = null;
            RaycastHit hit;

            // 1. Precise direct raycast
            if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out hit, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            // 2. Thick SphereCast fallback if direct ray narrowly missed
            if (interactable == null && Physics.SphereCast(_cam.transform.position, 0.45f, _cam.transform.forward, out var sHit, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                interactable = sHit.collider.GetComponentInParent<IInteractable>();
                hit = sHit;
            }

            // 3. Proximity cone check: If player is standing right in front of the console/clue looking towards it
            if (interactable == null)
            {
                var nearby = Physics.OverlapSphere(_cam.transform.position, 3.2f, ~0, QueryTriggerInteraction.Collide);
                float bestDot = 0.4f; // wide cone ~66 degrees
                foreach (var c in nearby)
                {
                    var cand = c.GetComponentInParent<IInteractable>();
                    if (cand != null)
                    {
                        Vector3 dir = (c.bounds.center - _cam.transform.position).normalized;
                        float dot = Vector3.Dot(_cam.transform.forward, dir);
                        if (dot > bestDot)
                        {
                            bestDot = dot;
                            interactable = cand;
                        }
                    }
                }
            }

            if (interactable != null)
            {
                Prompt = interactable.Prompt;
                if (interactable is IHoldInteractable hold)
                {
                    HoldProgress = hold.HoldProgress;
                    if (kb.eKey.isPressed && hold.CanInteract)
                    {
                        _activeHold = hold;
                        hold.Hold(Time.deltaTime);
                    }
                }
                else if (kb.eKey.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
                return;
            }

            if (hit.collider != null)
            {
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
