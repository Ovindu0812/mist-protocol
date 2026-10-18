using System.Collections;
using MistProtocol.AI.Perception;
using MistProtocol.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MistProtocol.Player
{
    /// <summary>
    /// "Mist Rifle": hitscan automatic rifle with spread, recoil, reload and physical
    /// impact impulses. Every shot is a loud event in the acoustic simulation.
    /// </summary>
    public class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private Transform viewModel;
        [SerializeField] private Transform muzzle;
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int reserveAmmo = 180;
        [SerializeField] private float fireInterval = 0.11f;
        [SerializeField] private float damage = 16f;
        [SerializeField] private float range = 80f;
        [SerializeField] private float impactImpulse = 6f;
        [SerializeField] private float reloadTime = 1.6f;
        [SerializeField] private float baseSpread = 0.5f;
        [SerializeField] private float loudness = 34f;

        private PlayerController _player;
        private PlayerInteractor _interactor;
        private Camera _cam;
        private float _nextFire;
        private float _heat;
        private float _kick;
        private float _bobT;
        private Vector3 _vmBasePos;
        private Quaternion _vmBaseRot;
        private float _reloadDip;

        public int InMagazine { get; private set; }
        public int Reserve => reserveAmmo;
        public bool IsReloading { get; private set; }
        public float LastHitTime { get; private set; } = -9f;

        public void Bind(Transform model, Transform muzzlePoint) { viewModel = model; muzzle = muzzlePoint; }

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            _interactor = GetComponent<PlayerInteractor>();
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null) _cam = GetComponentInParent<PlayerController>().GetComponentInChildren<Camera>();
            InMagazine = magazineSize;
            // salvaged ammo from every destroyed robot
            GameEvents.AgentKilled += (kind, pos) => reserveAmmo += kind == AgentKind.Sentinel ? 24 : 14;
            if (viewModel != null) { _vmBasePos = viewModel.localPosition; _vmBaseRot = viewModel.localRotation; }
        }

        public void AddAmmo(int amount) => reserveAmmo += amount;

        private void Update()
        {
            float dt = Time.deltaTime;
            _heat = Mathf.MoveTowards(_heat, 0f, dt * 3f);
            _kick = Mathf.MoveTowards(_kick, 0f, dt * 8f);
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool input = _player.InputEnabled && !_player.Health.IsDead && mouse != null && kb != null;
            bool holding = _interactor != null && _interactor.IsHolding;

            if (input && !holding && !IsReloading)
            {
                if (kb.rKey.wasPressedThisFrame && InMagazine < magazineSize && reserveAmmo > 0) StartCoroutine(Reload());
                else if (mouse.leftButton.isPressed && Time.time >= _nextFire)
                {
                    if (InMagazine > 0) Fire();
                    else if (reserveAmmo > 0) StartCoroutine(Reload());
                    else { _nextFire = Time.time + 0.3f; ProceduralAudio.PlayAt("beep", transform.position, 0.2f, 0.5f); }
                }
            }
            AnimateViewModel(dt, holding);
        }

        private void Fire()
        {
            _nextFire = Time.time + fireInterval;
            InMagazine--;
            float moving = Mathf.Clamp01(_player.Velocity.Flat().magnitude / 6f);
            float spread = baseSpread + _heat * 2.2f + moving * 1.6f - (_player.IsCrouching ? 0.3f : 0f);
            _heat = Mathf.Min(1f, _heat + 0.12f);
            _kick = 1f;
            _player.RecoilPitch += 0.9f + Random.value * 0.4f;

            Vector3 dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * _cam.transform.forward;
            Vector3 origin = _cam.transform.position;
            Vector3 end = origin + dir * range;
            bool hitAgent = false;
            if (Physics.Raycast(origin, dir, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null && !hit.collider.transform.IsChildOf(_player.transform))
                {
                    h.TakeDamage(damage, origin);
                    hitAgent = hit.collider.GetComponentInParent<AI.Agents.AgentBase>() != null;
                    if (hitAgent) LastHitTime = Time.time;
                }
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                    hit.rigidbody.AddForceAtPosition(dir * impactImpulse, hit.point, ForceMode.Impulse);
                Vfx.Impact(hit.point, hit.normal, hitAgent);
            }
            Vector3 mz = muzzle != null ? muzzle.position : origin;
            Vfx.Tracer(mz, end, new Color(1f, 0.85f, 0.5f, 0.9f));
            Vfx.Flash(mz, new Color(1f, 0.75f, 0.4f), 6f, 6f, 0.04f);
            ProceduralAudio.PlayAt("rifle", mz, 0.55f);
            SoundSystem.Emit(_player.transform.position, loudness, SoundType.Gunshot, true);
            GameEvents.RaiseShotFired();
            GameEvents.RaiseShotHit(hitAgent);
        }

        private IEnumerator Reload()
        {
            IsReloading = true;
            ProceduralAudio.PlayAt("reload", transform.position, 0.6f);
            SoundSystem.Emit(_player.transform.position, 6f, SoundType.Footstep, true);
            float t = 0f;
            while (t < reloadTime)
            {
                t += Time.deltaTime;
                _reloadDip = Mathf.Sin(t / reloadTime * Mathf.PI);
                yield return null;
            }
            _reloadDip = 0f;
            int need = magazineSize - InMagazine;
            int take = Mathf.Min(need, reserveAmmo);
            InMagazine += take;
            reserveAmmo -= take;
            IsReloading = false;
        }

        private void AnimateViewModel(float dt, bool holding)
        {
            if (viewModel == null) return;
            float speed = _player.Velocity.Flat().magnitude;
            _bobT += dt * speed * 1.6f;
            Vector3 bob = new Vector3(Mathf.Cos(_bobT) * 0.012f, Mathf.Abs(Mathf.Sin(_bobT)) * 0.014f, 0f) * Mathf.Clamp01(speed / 4f);
            Vector3 lower = holding ? new Vector3(0.05f, -0.25f, -0.1f) : Vector3.zero;
            Vector3 pos = _vmBasePos + bob + lower + new Vector3(0f, -_reloadDip * 0.12f, -_kick * 0.06f);
            viewModel.localPosition = Vector3.Lerp(viewModel.localPosition, pos, dt * 18f);
            viewModel.localRotation = _vmBaseRot * Quaternion.Euler(-_kick * 4f + _reloadDip * 30f, 0f, _reloadDip * -20f);
        }
    }
}
