using MistProtocol.AI.Perception;
using MistProtocol.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MistProtocol.Player
{
    /// <summary>
    /// First-person movement for Nila. Walk / sprint / crouch / jump on a
    /// CharacterController, pushes rigidbodies it walks into, and emits
    /// footstep noise into the acoustic simulation (crouching = near silent).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour, IPlayerState
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 6.8f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float mouseSensitivity = 0.11f;
        [Header("Physics push")]
        [SerializeField] private float pushForce = 1100f;
        [Header("Rig")]
        [SerializeField] private Transform cameraPivot;

        private CharacterController _cc;
        private Health _health;
        private PlayerWeapon _weapon;
        private PlayerInteractor _interactor;
        private float _yaw, _pitch, _verticalVel;
        private float _stepDistance;
        private float _standHeight;
        private float _shake;
        private float _headBobTimer;
        private bool _wasGrounded;
        private Vector3 _velocity;
        private float _pivotHeight;

        public bool InputEnabled { get; set; } = true;
        public float RecoilPitch { get; set; }

        // IPlayerState
        public Transform Root => transform;
        public Vector3 EyePosition => cameraPivot.position;
        public Vector3 AimDirection => cameraPivot.forward;
        public Vector3 Velocity => _velocity;
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsReloading => _weapon != null && _weapon.IsReloading;
        public bool IsHoldingObject => _interactor != null && _interactor.IsHolding;
        public Health Health => _health;
        public Transform CameraPivot => cameraPivot;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            _weapon = GetComponentInChildren<PlayerWeapon>();
            _interactor = GetComponentInChildren<PlayerInteractor>();
            if (cameraPivot == null) cameraPivot = transform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }
            if (cameraPivot == null)
            {
                var pivGo = new GameObject("CameraPivot");
                pivGo.transform.SetParent(transform, false);
                pivGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                cameraPivot = pivGo.transform;
            }
            _standHeight = _cc != null ? _cc.height : 1.8f;
            _pivotHeight = cameraPivot.localPosition.y;
            _yaw = transform.eulerAngles.y;
            PlayerLocator.Register(this);
            _health.Damaged += (amt, from) => { _shake = Mathf.Min(1f, _shake + amt / 30f); GameEvents.RaisePlayerDamaged(amt); };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            // Movement/action requires keyboard only; mouse look requires mouse + locked cursor.
            bool input = InputEnabled && !_health.IsDead && kb != null;

            // ---- look (requires mouse device present AND cursor locked)
            if (input && mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 md = mouse.delta.ReadValue() * mouseSensitivity;
                _yaw += md.x;
                _pitch = Mathf.Clamp(_pitch - md.y, -85f, 85f);
            }
            RecoilPitch = Mathf.Lerp(RecoilPitch, 0f, dt * 10f);
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 2.5f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 shake = Random.insideUnitSphere * _shake * 1.5f;
            cameraPivot.localRotation = Quaternion.Euler(_pitch - RecoilPitch + shake.x, shake.y, shake.z);

            // ---- move
            Vector2 move = Vector2.zero;
            if (input)
            {
                if (kb.wKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed) move.y -= 1;
                if (kb.dKey.isPressed) move.x += 1;
                if (kb.aKey.isPressed) move.x -= 1;
                move = Vector2.ClampMagnitude(move, 1f);
            }
            IsCrouching = input && (kb.leftCtrlKey.isPressed || kb.cKey.isPressed);
            IsSprinting = input && !IsCrouching && kb.leftShiftKey.isPressed && move.y > 0.1f && !IsHoldingObject;
            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;

            float targetH = IsCrouching ? 1.1f : _standHeight;
            _cc.height = Mathf.MoveTowards(_cc.height, targetH, dt * 5f);
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
            var lp = cameraPivot.localPosition;
            float targetBaseY = _pivotHeight - (_standHeight - targetH);

            Vector3 wish = (transform.forward * move.y + transform.right * move.x) * speed;
            if (_cc.isGrounded)
            {
                _verticalVel = -2f;
                if (input && kb.spaceKey.wasPressedThisFrame && !IsCrouching)
                    _verticalVel = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            _verticalVel += gravity * dt;
            Vector3 delta = (wish + Vector3.up * _verticalVel) * dt;
            Vector3 before = transform.position;
            _cc.Move(delta);
            _velocity = (transform.position - before) / Mathf.Max(dt, 0.0001f);

            // Landing impact thud
            if (!_wasGrounded && _cc.isGrounded)
            {
                if (_verticalVel < -4f)
                {
                    ProceduralAudio.PlayAt("step", transform.position, 0.85f, 0.7f);
                    _shake = Mathf.Max(_shake, 0.15f);
                }
            }
            _wasGrounded = _cc.isGrounded;

            // Procedural head-bobbing & breathing sway
            float horizSpeed = _velocity.Flat().magnitude;
            if (_cc.isGrounded && horizSpeed > 0.5f)
            {
                float bobFreq = IsSprinting ? 12.5f : 8.5f;
                float bobAmp = IsSprinting ? 0.045f : 0.024f;
                _headBobTimer += dt * bobFreq;
                lp.y = Mathf.MoveTowards(lp.y, targetBaseY + Mathf.Sin(_headBobTimer) * bobAmp, dt * 8f);
                lp.x = Mathf.Cos(_headBobTimer * 0.5f) * (bobAmp * 0.6f);
            }
            else
            {
                float breath = Mathf.Sin(Time.time * 2.0f) * 0.008f;
                lp.y = Mathf.MoveTowards(lp.y, targetBaseY + breath, dt * 5f);
                lp.x = Mathf.MoveTowards(lp.x, 0f, dt * 2.5f);
            }
            cameraPivot.localPosition = lp;

            // ---- footsteps into the acoustic sim
            if (_cc.isGrounded)
            {
                float horiz = _velocity.Flat().magnitude;
                _stepDistance += horiz * dt;
                float stride = IsSprinting ? 2.4f : 1.9f;
                if (_stepDistance > stride && horiz > 0.5f)
                {
                    _stepDistance = 0f;
                    float loud = IsCrouching ? 3f : IsSprinting ? 17f : 8f;
                    SoundSystem.Emit(transform.position, loud, SoundType.Footstep, true);
                    string stepCue = GetSurfaceFootstepSound();
                    ProceduralAudio.PlayAt(stepCue, transform.position, IsCrouching ? 0.15f : 0.4f);
                }
            }
        }

        private string GetSurfaceFootstepSound()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                string n = hit.collider.gameObject.name.ToLower();
                if (n.Contains("metal") || n.Contains("grate") || n.Contains("pipe") || n.Contains("catwalk")) return "step_metal";
                if (n.Contains("wood") || n.Contains("plank") || n.Contains("crate") || n.Contains("floor_wood")) return "step_wood";
            }
            return "step";
        }

        public void AddShake(float amount) => _shake = Mathf.Min(1f, _shake + amount);

        public void SetCinematicPose(Vector3 position, float yaw)
        {
            transform.position = position;
            _yaw = yaw;
            _pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.identity;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            rb.AddForceAtPosition(dir * pushForce * Time.deltaTime * (IsSprinting ? 1.6f : 1f), hit.point, ForceMode.Impulse);
        }
    }
}
