using System.Collections;
using MistProtocol.AI.Perception;
using MistProtocol.Animation;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Agents
{
    /// <summary>Hitscan weapon used by agents. Accuracy degrades with range and a moving target.</summary>
    public class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private float damage = 6f;
        [SerializeField] private float fireInterval = 0.16f;
        [SerializeField] private float baseSpread = 2.5f;
        [SerializeField] private int magazine = 18;
        [SerializeField] private float reloadTime = 2.2f;
        [SerializeField] private float loudness = 26f;
        [SerializeField] private Color tracerColor = new Color(1f, 0.25f, 0.15f, 0.9f);

        private float _next;
        private AgentAnimatorBase _anim;

        public int Ammo { get; private set; }
        public int Magazine => magazine;
        public bool IsReloading { get; private set; }
        public bool Loaded => Ammo > 0 && !IsReloading;

        public void Configure(Transform muzzlePoint, float dmg, float interval, float spread, int mag)
        {
            muzzle = muzzlePoint; damage = dmg; fireInterval = interval; baseSpread = spread; magazine = mag;
        }

        private void Awake()
        {
            Ammo = magazine;
            _anim = GetComponent<AgentAnimatorBase>();
        }

        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f;

        /// <returns>True if a shot was fired this call.</returns>
        public bool TryFireAt(Vector3 target, float accuracy = 1f)
        {
            if (IsReloading || Time.time < _next) return false;
            if (Ammo <= 0) { StartReload(); return false; }
            _next = Time.time + fireInterval;
            Ammo--;

            Vector3 origin = MuzzlePosition;
            Vector3 dir = (target - origin).normalized;
            float range = Vector3.Distance(origin, target);
            float spread = baseSpread * (1f + range / 25f) / Mathf.Max(0.2f, accuracy);
            if (PlayerLocator.Alive) spread += PlayerLocator.State.Velocity.Flat().magnitude * 0.35f;
            dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * dir;

            Vector3 end = origin + dir * 60f;
            if (Physics.Raycast(origin, dir, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    end = hit.point;
                    var h = hit.collider.GetComponentInParent<Health>();
                    if (h != null && hit.collider.GetComponentInParent<AgentBase>() == null) h.TakeDamage(damage, origin);
                    if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                        hit.rigidbody.AddForceAtPosition(dir * 3f, hit.point, ForceMode.Impulse);
                    Vfx.Impact(hit.point, hit.normal, false);
                }
            }
            Vfx.Tracer(origin, end, tracerColor, 0.03f);
            Vfx.Flash(origin, new Color(1f, 0.3f, 0.2f), 4f, 5f, 0.04f);
            ProceduralAudio.PlayAt("enemyShot", origin, 0.45f);
            SoundSystem.Emit(origin, loudness, SoundType.EnemyGunshot, false);
            if (_anim != null) _anim.PlayFire();
            return true;
        }

        public void StartReload()
        {
            if (!IsReloading && Ammo < magazine) StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            ProceduralAudio.PlayAt("reload", transform.position, 0.4f, 0.8f);
            yield return new WaitForSeconds(reloadTime);
            Ammo = magazine;
            IsReloading = false;
        }
    }
}
