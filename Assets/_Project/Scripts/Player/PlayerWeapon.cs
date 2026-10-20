using System.Collections;
using System.Collections.Generic;
using MistProtocol.AI.Perception;
using MistProtocol.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MistProtocol.Player
{
    [System.Serializable]
    public class WeaponData
    {
        public string name = "Mist Rifle";
        public Transform viewModel;
        public Transform muzzle;
        public int magazineSize = 30;
        public int inMagazine = 30;
        public int reserveAmmo = 180;
        public float fireInterval = 0.11f;
        public float damage = 16f;
        public float range = 80f;
        public float impactImpulse = 6f;
        public float reloadTime = 1.6f;
        public float baseSpread = 0.5f;
        public float heatGain = 0.12f;
        public float loudness = 34f;
        public int pellets = 1;
        public bool isAutomatic = true;
        public string soundName = "rifle";
        public Color tracerColor = new Color(1f, 0.85f, 0.5f, 0.9f);
        public Color flashColor = new Color(1f, 0.75f, 0.4f);
        public float recoilPitch = 0.9f;

        [System.NonSerialized] public Vector3 vmBasePos;
        [System.NonSerialized] public Quaternion vmBaseRot;
    }

    /// <summary>
    /// Multi-weapon system: Mist Rifle, Mist Shotgun, Mist Revolver, and Mist SMG.
    /// Handles hitscan ballistics, pellet spread, recoil, procedural reload animation,
    /// and weapon switching via 1-4 number keys or scroll wheel.
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

        [SerializeField] private List<WeaponData> weapons = new();
        [SerializeField] private int activeIndex = 0;

        private PlayerController _player;
        private PlayerInteractor _interactor;
        private Camera _cam;
        private float _nextFire;
        private float _heat;
        private float _kick;
        private float _bobT;
        private float _reloadDip;
        private Coroutine _reloadRoutine;

        public int ActiveIndex => activeIndex;
        public WeaponData ActiveWeapon => (weapons != null && weapons.Count > activeIndex) ? weapons[activeIndex] : null;
        public string ActiveWeaponName => ActiveWeapon != null ? ActiveWeapon.name : "Mist Rifle";
        public int InMagazine => ActiveWeapon != null ? ActiveWeapon.inMagazine : magazineSize;
        public int Reserve => ActiveWeapon != null ? ActiveWeapon.reserveAmmo : reserveAmmo;
        public bool IsReloading { get; private set; }
        public float LastHitTime { get; private set; } = -9f;

        public void Bind(Transform model, Transform muzzlePoint)
        {
            viewModel = model;
            muzzle = muzzlePoint;
        }

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            _interactor = GetComponent<PlayerInteractor>();
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null && _player != null)
                _cam = _player.GetComponentInChildren<Camera>();

            InitializeWeapons();

            // Salvaged ammo from destroyed robots distributes to all weapons
            GameEvents.AgentKilled += (kind, pos) =>
            {
                bool isSentinel = kind == AgentKind.Sentinel;
                AddAmmoToWeapon(0, isSentinel ? 24 : 14); // Mist Rifle
                AddAmmoToWeapon(1, isSentinel ? 6 : 4);   // Mist Shotgun
                AddAmmoToWeapon(2, isSentinel ? 6 : 3);   // Mist Revolver
                AddAmmoToWeapon(3, isSentinel ? 35 : 20); // Mist SMG
            };
        }

        private void InitializeWeapons()
        {
            // Always rebuild if the list doesn't have all 4 weapons.
            // This handles the case where the prefab was serialized before the new weapons were added.
            if (weapons == null || weapons.Count < 4)
            {
                weapons = new List<WeaponData>
                {
                    // Slot 0: Mist Rifle
                    new()
                    {
                        name = "Mist Rifle",
                        viewModel = viewModel,
                        muzzle = muzzle,
                        magazineSize = magazineSize,
                        inMagazine = magazineSize,
                        reserveAmmo = reserveAmmo,
                        fireInterval = fireInterval,
                        damage = damage,
                        range = range,
                        impactImpulse = impactImpulse,
                        reloadTime = reloadTime,
                        baseSpread = baseSpread,
                        heatGain = 0.12f,
                        loudness = loudness,
                        pellets = 1,
                        isAutomatic = true,
                        soundName = "rifle",
                        tracerColor = new Color(1f, 0.85f, 0.5f, 0.9f),
                        flashColor = new Color(1f, 0.75f, 0.4f),
                        recoilPitch = 0.9f
                    },
                    // Slot 1: Mist Shotgun (Ceylon Tea Factory Riot Spreader)
                    new()
                    {
                        name = "Mist Shotgun",
                        magazineSize = 6,
                        inMagazine = 6,
                        reserveAmmo = 36,
                        fireInterval = 0.68f,
                        damage = 13f,         // 8 pellets x 13 = 104 total point-blank damage
                        range = 45f,
                        impactImpulse = 14f,
                        reloadTime = 2.2f,
                        baseSpread = 3.2f,
                        heatGain = 0.35f,
                        loudness = 42f,
                        pellets = 8,
                        isAutomatic = false,
                        soundName = "shotgun",
                        tracerColor = new Color(1f, 0.65f, 0.3f, 0.95f),
                        flashColor = new Color(1f, 0.6f, 0.2f),
                        recoilPitch = 3.2f
                    },
                    // Slot 2: Mist Revolver (Heavy Industrial Handgun)
                    new()
                    {
                        name = "Mist Revolver",
                        magazineSize = 6,
                        inMagazine = 6,
                        reserveAmmo = 42,
                        fireInterval = 0.36f,
                        damage = 56f,
                        range = 95f,
                        impactImpulse = 10f,
                        reloadTime = 1.7f,
                        baseSpread = 0.25f,
                        heatGain = 0.22f,
                        loudness = 36f,
                        pellets = 1,
                        isAutomatic = false,
                        soundName = "revolver",
                        tracerColor = new Color(1f, 0.95f, 0.7f, 1f),
                        flashColor = new Color(1f, 0.85f, 0.5f),
                        recoilPitch = 2.4f
                    },
                    // Slot 3: Mist SMG (Compact High-ROF Security Machine Pistol)
                    new()
                    {
                        name = "Mist SMG",
                        magazineSize = 35,
                        inMagazine = 35,
                        reserveAmmo = 210,
                        fireInterval = 0.072f, // rapid fire
                        damage = 9.5f,
                        range = 65f,
                        impactImpulse = 4f,
                        reloadTime = 1.35f,
                        baseSpread = 0.9f,
                        heatGain = 0.08f,
                        loudness = 28f,
                        pellets = 1,
                        isAutomatic = true,
                        soundName = "smg",
                        tracerColor = new Color(0.4f, 0.85f, 1f, 0.9f),
                        flashColor = new Color(0.3f, 0.7f, 1f),
                        recoilPitch = 0.55f
                    }
                };
            }

            // Bind transforms if child models exist under the camera, or load them at runtime
            Transform camT = _cam != null ? _cam.transform : transform;
            Material palMat = viewModel != null ? viewModel.GetComponentInChildren<MeshRenderer>()?.sharedMaterial : null;

            for (int i = 0; i < weapons.Count; i++)
            {
                var w = weapons[i];
                string cleanName = w.name.Replace(" ", "");
                if (w.viewModel == null)
                {
                    Transform found = camT.Find(cleanName);
                    if (found == null)
                    {
                        foreach (Transform child in camT)
                        {
                            if (child.name.Equals(cleanName, System.StringComparison.OrdinalIgnoreCase))
                            {
                                found = child;
                                break;
                            }
                        }
                    }

#if UNITY_EDITOR
                    if (found == null)
                    {
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Models/{cleanName}.fbx");
                        if (asset != null)
                        {
                            var inst = Instantiate(asset, camT);
                            inst.name = cleanName;
                            found = inst.transform;
                            found.localPosition = i == 1 ? new Vector3(0.2f, -0.26f, 0.48f) :
                                                  i == 2 ? new Vector3(0.18f, -0.22f, 0.42f) :
                                                  i == 3 ? new Vector3(0.2f, -0.25f, 0.46f) :
                                                  new Vector3(0.2f, -0.25f, 0.5f);
                            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
                            {
                                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                if (r.name == "Muzzle") r.enabled = false;
                                if (palMat != null) r.sharedMaterial = palMat;
                            }
                        }
                    }
#endif
                    if (found == null && viewModel != null)
                    {
                        var copy = Instantiate(viewModel.gameObject, camT);
                        copy.name = cleanName;
                        found = copy.transform;
                    }

                    if (found != null)
                    {
                        w.viewModel = found;
                        w.muzzle = found.Find("Muzzle");
                        if (w.muzzle == null) w.muzzle = found;
                    }
                }

                if (w.viewModel != null)
                {
                    w.vmBasePos = w.viewModel.localPosition;
                    w.vmBaseRot = w.viewModel.localRotation;
                }
            }

            ApplyWeaponVisibility();
        }

        public void AddAmmo(int amount)
        {
            if (ActiveWeapon != null)
                ActiveWeapon.reserveAmmo += amount;
            else
                reserveAmmo += amount;
        }

        public void AddAmmoToWeapon(int slot, int amount)
        {
            if (weapons != null && slot >= 0 && slot < weapons.Count)
                weapons[slot].reserveAmmo += amount;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _heat = Mathf.MoveTowards(_heat, 0f, dt * 3f);
            _kick = Mathf.MoveTowards(_kick, 0f, dt * 8f);

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool holding = _interactor != null && _interactor.IsHolding;

            if (kb != null && !holding)
            {
                HandleWeaponSwitching(kb, mouse);
            }

            bool canShoot = _player != null && _player.InputEnabled && !_player.Health.IsDead && mouse != null && !holding;

            if (canShoot)
            {
                var cur = ActiveWeapon;
                if (cur != null && !IsReloading)
                {
                    if (kb != null && kb.rKey.wasPressedThisFrame && cur.inMagazine < cur.magazineSize && cur.reserveAmmo > 0)
                    {
                        StartReload(cur);
                    }
                    else
                    {
                        bool shootRequested = cur.isAutomatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
                        if (shootRequested && Time.time >= _nextFire)
                        {
                            if (cur.inMagazine > 0)
                                Fire(cur);
                            else if (cur.reserveAmmo > 0)
                                StartReload(cur);
                            else
                            {
                                _nextFire = Time.time + 0.3f;
                                ProceduralAudio.PlayAt("beep", transform.position, 0.2f, 0.5f);
                            }
                        }
                    }
                }
            }

            AnimateViewModel(dt, holding);
        }

        private void HandleWeaponSwitching(Keyboard kb, Mouse mouse)
        {
            if (IsReloading) return;

            int targetIndex = activeIndex;

            if (kb.qKey.wasPressedThisFrame) targetIndex = (activeIndex + 1) % weapons.Count;
            else if (kb.digit1Key.wasPressedThisFrame) targetIndex = 0;
            else if (kb.digit2Key.wasPressedThisFrame) targetIndex = 1;
            else if (kb.digit3Key.wasPressedThisFrame) targetIndex = 2;
            else if (kb.digit4Key.wasPressedThisFrame) targetIndex = 3;

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f) targetIndex = (activeIndex - 1 + weapons.Count) % weapons.Count;
                else if (scroll < -0.1f) targetIndex = (activeIndex + 1) % weapons.Count;
            }

            if (targetIndex != activeIndex && targetIndex >= 0 && targetIndex < weapons.Count)
                SwitchWeapon(targetIndex);
        }

        public void SwitchWeapon(int newIndex)
        {
            if (weapons == null || weapons.Count == 0) return;
            if (newIndex < 0 || newIndex >= weapons.Count) return;
            if (newIndex == activeIndex && weapons[activeIndex].viewModel != null && weapons[activeIndex].viewModel.gameObject.activeSelf) return;

            if (_reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
                IsReloading = false;
                _reloadDip = 0f;
            }

            activeIndex = newIndex;
            _nextFire = Time.time + 0.2f;
            _heat = 0f;
            _kick = 0.5f;

            ApplyWeaponVisibility();
            ProceduralAudio.PlayAt("pickup", transform.position, 0.45f, 1.4f);
            Debug.Log($"[PlayerWeapon] Switched to weapon {activeIndex}: {ActiveWeaponName}");
        }

        private void ApplyWeaponVisibility()
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].viewModel != null)
                    weapons[i].viewModel.gameObject.SetActive(i == activeIndex);
            }
        }

        private void Fire(WeaponData w)
        {
            _nextFire = Time.time + w.fireInterval;
            w.inMagazine--;

            float moving = _player != null ? Mathf.Clamp01(_player.Velocity.Flat().magnitude / 6f) : 0f;
            bool isCrouching = _player != null && _player.IsCrouching;
            float spread = w.baseSpread + _heat * 2.2f + moving * 1.6f - (isCrouching ? 0.3f : 0f);
            _heat = Mathf.Min(1f, _heat + w.heatGain);
            _kick = 1f;

            if (_player != null)
                _player.RecoilPitch += w.recoilPitch + Random.value * 0.35f;

            Vector3 origin = _cam != null ? _cam.transform.position : transform.position;
            Vector3 mz = w.muzzle != null ? w.muzzle.position : origin;
            bool anyHitAgent = false;

            for (int i = 0; i < w.pellets; i++)
            {
                Vector3 dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * (_cam != null ? _cam.transform.forward : transform.forward);
                Vector3 end = origin + dir * w.range;
                bool hitAgent = false;

                if (Physics.Raycast(origin, dir, out var hit, w.range, ~0, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var h = hit.collider.GetComponentInParent<Health>();
                    if (h != null && (_player == null || !hit.collider.transform.IsChildOf(_player.transform)))
                    {
                        h.TakeDamage(w.damage, origin);
                        hitAgent = hit.collider.GetComponentInParent<AI.Agents.AgentBase>() != null;
                        if (hitAgent)
                        {
                            LastHitTime = Time.time;
                            anyHitAgent = true;
                        }
                    }
                    if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                        hit.rigidbody.AddForceAtPosition(dir * w.impactImpulse, hit.point, ForceMode.Impulse);

                    Vfx.Impact(hit.point, hit.normal, hitAgent);
                }

                Vfx.Tracer(mz, end, w.tracerColor);
            }

            Vfx.Flash(mz, w.flashColor, 6f, 6f, 0.04f);
            ProceduralAudio.PlayAt(w.soundName, mz, 0.6f);
            SoundSystem.Emit(transform.position, w.loudness, SoundType.Gunshot, true);
            GameEvents.RaiseShotFired();
            GameEvents.RaiseShotHit(anyHitAgent);
        }

        private void StartReload(WeaponData w)
        {
            if (_reloadRoutine != null) StopCoroutine(_reloadRoutine);
            _reloadRoutine = StartCoroutine(ReloadRoutine(w));
        }

        private IEnumerator ReloadRoutine(WeaponData w)
        {
            IsReloading = true;
            ProceduralAudio.PlayAt("reload", transform.position, 0.6f);
            SoundSystem.Emit(transform.position, 6f, SoundType.Footstep, true);

            float t = 0f;
            while (t < w.reloadTime)
            {
                t += Time.deltaTime;
                _reloadDip = Mathf.Sin(t / w.reloadTime * Mathf.PI);
                yield return null;
            }
            _reloadDip = 0f;

            int need = w.magazineSize - w.inMagazine;
            int take = Mathf.Min(need, w.reserveAmmo);
            w.inMagazine += take;
            w.reserveAmmo -= take;
            IsReloading = false;
            _reloadRoutine = null;
        }

        private void AnimateViewModel(float dt, bool holding)
        {
            var cur = ActiveWeapon;
            if (cur == null || cur.viewModel == null) return;

            Transform vm = cur.viewModel;
            float speed = _player != null ? _player.Velocity.Flat().magnitude : 0f;
            _bobT += dt * speed * 1.6f;
            Vector3 bob = new Vector3(Mathf.Cos(_bobT) * 0.012f, Mathf.Abs(Mathf.Sin(_bobT)) * 0.014f, 0f) * Mathf.Clamp01(speed / 4f);
            Vector3 lower = holding ? new Vector3(0.05f, -0.25f, -0.1f) : Vector3.zero;
            Vector3 pos = cur.vmBasePos + bob + lower + new Vector3(0f, -_reloadDip * 0.12f, -_kick * 0.06f);

            vm.localPosition = Vector3.Lerp(vm.localPosition, pos, dt * 18f);
            vm.localRotation = cur.vmBaseRot * Quaternion.Euler(-_kick * 4f + _reloadDip * 30f, 0f, _reloadDip * -20f);
        }
    }
}
