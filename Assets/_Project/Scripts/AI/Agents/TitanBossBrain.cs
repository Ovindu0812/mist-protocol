using System.Collections.Generic;
using MistProtocol.AI.Navigation;
using MistProtocol.AI.Perception;
using MistProtocol.Animation;
using MistProtocol.Core;
using MistProtocol.Player;
using UnityEngine;

namespace MistProtocol.AI.Agents
{
    /// <summary>
    /// TITAN ENFORCER - Giant armored boss automaton guarding the Containment Vault.
    /// Features:
    /// - Heavy footsteps with screen rumble.
    /// - Sweeping crimson searchlight beam.
    /// - High armor resistance; shooting directly barely chips his health.
    /// - Stealth evasion: If Nila is crouched behind tea crate cover, the Titan cannot spot her.
    /// - Devastating heavy burst cannon when player is caught out in the open.
    /// </summary>
    public class TitanBossBrain : AgentBase
    {
        [Header("Patrol & Search")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float chaseSpeed = 2.7f;
        [SerializeField] private float stompInterval = 0.95f;

        [Header("Sensors & Weapon")]
        [SerializeField] private Light searchlight;
        [SerializeField] private float searchAngle = 65f;

        private WalkerAnimator _walker;
        private int _patrolIndex;
        private float _nextStomp;
        private float _nextGroundSlam;
        private float _staggerUntil;
        private float _searchYaw;
        private bool _isChasing;
        private bool _isEnraged;
        private Vector3 _lastKnownPlayerPos;
        private float _lostSightTime;

        public override AgentKind Kind => AgentKind.Sentinel;

        public void SetPatrolPoints(Transform[] points) => patrolPoints = points;

        protected override void Awake()
        {
            base.Awake();
            _walker = GetComponent<WalkerAnimator>();
            if (searchlight == null) searchlight = GetComponentInChildren<Light>();
        }

        protected override void Start()
        {
            base.Start();
            // Balance Titan stats so the fight is thrilling, fair, and fun
            var bossHealth = GetComponent<ArmoredBossHealth>();
            if (bossHealth != null)
            {
                bossHealth.BalanceBoss(650f, 0.45f);
                bossHealth.ArmorFractured += OnArmorFractured;
            }
            if (Weapon != null)
            {
                Weapon.Balance(7.5f, 0.22f, 3.2f);
            }
        }

        private void OnDestroy()
        {
            var bossHealth = GetComponent<ArmoredBossHealth>();
            if (bossHealth != null) bossHealth.ArmorFractured -= OnArmorFractured;
        }

        private void OnArmorFractured()
        {
            _isEnraged = true;
            patrolSpeed *= 1.35f;
            chaseSpeed *= 1.35f;
            if (searchlight != null)
            {
                searchlight.color = Color.red;
                searchlight.intensity = 4.5f;
            }
            GameEvents.Overseer("TITAN PROTOCOL: Core safety overrides offline. Enraged combat mode engaged!");
        }

        protected override void OnDamaged(float amount, Vector3 fromPos)
        {
            base.OnDamaged(amount, fromPos);
            // If hit by massive explosion (gas canister) or heavy damage
            if (amount >= 35f)
            {
                _staggerUntil = Time.time + 3.5f;
                Motor.Stop();
                if (_walker != null) _walker.AimPoint = null;
                GameEvents.Overseer("OVERSEER: Heavy containment armor breached! Hydraulic stabilizers recalibrating!");
            }
            else
            {
                // Turn towards damage source
                _lastKnownPlayerPos = fromPos;
                _isChasing = true;
            }
        }

        public override void OnHeard(in SoundStimulus s)
        {
            if (s.FromPlayer)
            {
                _lastKnownPlayerPos = s.EstimatedPosition;
                _isChasing = true;
            }
        }

        public override void Deactivate(float duration)
        {
            // Titan Boss operates on independent auxiliary nuclear reactor - immune to factory grid failure!
        }

        protected override void OnDied()
        {
            // Massive mechanical explosion on boss destruction
            Vfx.Burst(transform.position + Vector3.up * 1.5f, Vector3.up, new Color(1f, 0.45f, 0.15f), 45, 9f, 0.2f, 1.0f);
            ProceduralAudio.PlayAt("explosion", transform.position, 1.2f, 0.6f);

            // Screen shake for nearby player
            if (PlayerLocator.Alive)
            {
                var pc = PlayerLocator.State.Root.GetComponent<PlayerController>();
                if (pc != null) pc.RecoilPitch += 3.0f;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTitanDefeated();
            }

            // Spawn key only if BossChamberTrap hasn't already spawned it
            // (BossChamberTrap hooks Titan health.Died - check if a key already exists)
            bool keyAlreadySpawned = UnityEngine.Object.FindAnyObjectByType<Interaction.MasterCellKey>() != null;
            if (!keyAlreadySpawned)
            {
                Interaction.MasterCellKey.SpawnKey(transform.position);
                Interaction.FatherClue.SpawnMap(transform.position + Vector3.right * 0.8f);
            }

            base.OnDied();
        }

        protected override void Think()
        {
            if (Time.time < _staggerUntil) return;

            var player = PlayerLocator.State;
            bool seesPlayer = CanSeePlayerWithStealthCheck(player);

            if (seesPlayer)
            {
                _isChasing = true;
                _lastKnownPlayerPos = player.Root.position;
                _lostSightTime = 0f;

                // Aim and fire heavy bursts
                if (_walker != null) _walker.AimPoint = player.Root.position + Vector3.up * 1.2f;
                Motor.MaxSpeed = chaseSpeed;
                Motor.MoveTo(_lastKnownPlayerPos);

                float dist = Vector3.Distance(transform.position, player.Root.position);
                if (dist < 20f && Weapon != null)
                {
                    Weapon.TryFireAt(player.Root.position + Vector3.up * 1.1f, 1.2f);
                }

                TryExecuteGroundSlam(player.Root.position);
            }
            else if (_isChasing)
            {
                _lostSightTime += 1f / 8f;
                if (_walker != null) _walker.AimPoint = null;

                // Move toward last known point
                Motor.MaxSpeed = patrolSpeed * 1.3f;
                Motor.MoveTo(_lastKnownPlayerPos);

                // Give up chase if lost sight for more than 4 seconds
                if (_lostSightTime > 4.5f || Vector3.Distance(transform.position, _lastKnownPlayerPos) < 2f)
                {
                    _isChasing = false;
                }
            }
            else
            {
                // Patrol behavior
                if (_walker != null) _walker.AimPoint = null;
                Patrol();
            }

            // Stomp sound effect and floor vibration
            if (Motor.Velocity.magnitude > 0.4f && Time.time > _nextStomp)
            {
                _nextStomp = Time.time + stompInterval;
                PlayHeavyStomp();
            }
        }

        private bool CanSeePlayerWithStealthCheck(IPlayerState player)
        {
            if (player == null || !PlayerLocator.Alive) return false;

            Vector3 eyePos = transform.position + Vector3.up * 2.4f;
            Vector3 toPlayer = player.Root.position + Vector3.up * 1.0f - eyePos;
            float dist = toPlayer.magnitude;

            if (dist > 28f) return false;

            // Field of view check
            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
            if (angle > searchAngle * 0.5f) return false;

            // Line of sight raycast
            if (Physics.Raycast(eyePos, toPlayer.normalized, out var hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(player.Root))
                {
                    // Sightline blocked by crate, pillar, or wall!
                    return false;
                }
            }

            // If player is crouching behind lower barricades / dark spots
            var pc = player.Root.GetComponent<PlayerController>();
            if (pc != null && pc.IsCrouching && dist > 7f)
            {
                return false; // Safely crouching behind cover
            }

            return true;
        }

        private void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0 || patrolPoints[_patrolIndex] == null)
            {
                var p0 = GameObject.Find("PatrolTitan_0");
                var p1 = GameObject.Find("PatrolTitan_1");
                var p2 = GameObject.Find("PatrolTitan_2");
                var p3 = GameObject.Find("PatrolTitan_3");
                if (p0 != null && p1 != null && p2 != null && p3 != null)
                {
                    patrolPoints = new[] { p0.transform, p1.transform, p2.transform, p3.transform };
                    _patrolIndex = 0;
                }
                else
                {
                    Motor.MaxSpeed = patrolSpeed;
                    Motor.MoveTo(new Vector3(46f, 0f, 7f));
                    return;
                }
            }

            Vector3 dest = patrolPoints[_patrolIndex].position;
            Motor.MaxSpeed = patrolSpeed;
            Motor.MoveTo(dest);

            if (Vector3.Distance(transform.position, dest) < 2.5f)
            {
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            }
        }

        private void PlayHeavyStomp()
        {
            ProceduralAudio.PlayAt("step", transform.position, 0.8f, 0.55f);

            // If player is nearby, cause mild screen rumble
            if (PlayerLocator.Alive)
            {
                float d = Vector3.Distance(transform.position, PlayerLocator.State.Root.position);
                if (d < 18f)
                {
                    var pc = PlayerLocator.State.Root.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        float intensity = (1f - d / 18f) * 0.25f;
                        pc.RecoilPitch += Random.Range(-intensity, intensity);
                    }
                }
            }
        }

        private void TryExecuteGroundSlam(Vector3 playerPos)
        {
            if (Time.time < _nextGroundSlam) return;
            float dist = Vector3.Distance(transform.position, playerPos);
            if (dist > 5.5f) return;

            _nextGroundSlam = Time.time + (_isEnraged ? 4.5f : 6.5f);
            _staggerUntil = Time.time + 1.2f;
            Motor.Stop();

            // Seismic impact sound & shockwave VFX
            ProceduralAudio.PlayAt("slam", transform.position, 1.4f, 0.85f);
            Vfx.Burst(transform.position + Vector3.up * 0.2f, Vector3.up, new Color(1f, 0.4f, 0.1f), 35, 8f, 0.15f, 0.7f);

            // Radial physics impulse & damage to player
            if (PlayerLocator.Alive && Vector3.Distance(transform.position, PlayerLocator.State.Root.position) <= 6.5f)
            {
                var pc = PlayerLocator.State.Root.GetComponent<PlayerController>();
                if (pc != null) pc.RecoilPitch += 4.5f;
                PlayerLocator.State.Health.TakeDamage(18f, transform.position);
            }

            // Push nearby loose rigidbodies
            var colliders = Physics.OverlapSphere(transform.position, 6.5f);
            foreach (var col in colliders)
            {
                var rb = col.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddExplosionForce(750f, transform.position, 6.5f, 1.0f);
                }
            }
        }

        private void LateUpdate()
        {
            // Sweep searchlight slowly when not chasing
            if (searchlight != null)
            {
                if (!_isChasing)
                {
                    _searchYaw = Mathf.Sin(Time.time * 1.5f) * 25f;
                    searchlight.transform.localRotation = Quaternion.Euler(15f, _searchYaw, 0f);
                    searchlight.color = Color.Lerp(searchlight.color, new Color(1f, 0.3f, 0.2f), Time.deltaTime * 3f);
                }
                else
                {
                    searchlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                    searchlight.color = new Color(1f, 0.05f, 0.02f); // Fiery red when locked on
                }
            }
        }
    }
}
