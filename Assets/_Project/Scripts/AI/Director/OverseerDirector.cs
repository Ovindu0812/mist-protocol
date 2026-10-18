using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.AI.Navigation;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Director
{
    /// <summary>
    /// OVERSEER - the corrupted factory AI, implemented as a dynamic spawn director.
    ///
    /// 1. Pacing: an intensity value (0..1) rises with damage taken, enemies in
    ///    view and kills, and decays when nothing happens. A finite-state machine
    ///    BuildUp -> Peak -> Relax keeps the fight in waves instead of a constant
    ///    trickle (the Left 4 Dead "AI Director" model).
    /// 2. What to send: a <see cref="PlayerProfile"/> biases the enemy mix
    ///    (campers get Hunter Drones that hunt by sound, physics-heavy players get
    ///    Saboteurs that undo their barricades, accurate players get Sentinels
    ///    that fight from cover).
    /// 3. Where: each vent is scored by A* path distance to the player (12-35 m),
    ///    hidden from the player's view, ahead toward the objective, away from
    ///    "cleared" territory in the <see cref="InfluenceMap"/>, and not recently used.
    /// 4. Story: OVERSEER taunts the player through the PA based on the same profile.
    /// </summary>
    public class OverseerDirector : MonoBehaviour
    {
        public enum Phase { BuildUp, Peak, Relax, Final }

        [SerializeField] private AgentBase dronePrefab;
        [SerializeField] private AgentBase sentinelPrefab;
        [SerializeField] private AgentBase saboteurPrefab;
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField] private int baseMaxAlive = 4;
        [SerializeField] private float spawnInterval = 7f;
        [SerializeField] private float peakThreshold = 0.7f;
        [SerializeField] private int maxDirectorSpawns = 18;

        private readonly List<SpawnPoint> _spawns = new();
        private readonly List<Vector3> _pathBuf = new();
        private PlayerProfile _profile;
        private InfluenceMap _influence;
        private float _nextSpawn, _phaseTime, _nextTick, _nextTaunt;
        private int _spawned;
        private bool _enabled;

        public Phase CurrentPhase { get; private set; } = Phase.Relax;
        public float Intensity { get; private set; }
        public PlayerProfile Profile => _profile;
        public string LastDecision { get; private set; } = "";
        public static OverseerDirector Instance { get; private set; }

        public void Configure(AgentBase drone, AgentBase sentinel, AgentBase saboteur, GameObject health)
        {
            dronePrefab = drone; sentinelPrefab = sentinel; saboteurPrefab = saboteur; healthPickupPrefab = health;
        }

        private void Awake()
        {
            Instance = this;
            _profile = new PlayerProfile();
            GameEvents.PlayerDamaged += amt => Intensity = Mathf.Clamp01(Intensity + amt / 60f);
            GameEvents.AgentKilled += (k, p) => Intensity = Mathf.Clamp01(Intensity + 0.08f);
            GameEvents.FinalWaveStarted += () => { Enter(Phase.Final); _enabled = true; };
        }

        private void Start()
        {
            _spawns.AddRange(FindObjectsByType<SpawnPoint>());
            _influence = new InfluenceMap(NavGrid.Instance, 4);
            _phaseTime = Time.time;
        }

        /// <summary>The director stays dormant until the player is inside the factory.</summary>
        public void Activate() { if (!_enabled) { _enabled = true; Enter(Phase.Relax); } }

        private void Enter(Phase p)
        {
            CurrentPhase = p;
            _phaseTime = Time.time;
        }

        private void Update()
        {
            if (!PlayerLocator.Alive || Time.time < _nextTick) return;
            const float dt = 0.5f;
            _nextTick = Time.time + dt;
            _profile.Tick(dt);
            _influence.Stamp(PlayerLocator.State.Root.position, 0.2f);
            _influence.Propagate();
            if (!_enabled) return;

            int seeing = 0;
            foreach (var a in AgentBase.Active) if (a.Sensor.CanSeePlayer) seeing++;
            Intensity = Mathf.Clamp01(Intensity + seeing * 0.01f - (seeing == 0 ? 0.025f : 0f));

            float inPhase = Time.time - _phaseTime;
            switch (CurrentPhase)
            {
                case Phase.BuildUp:
                    if (Intensity >= peakThreshold) Enter(Phase.Peak);
                    else TrySpawn(spawnInterval, baseMaxAlive + GameManager.Instance.Stage);
                    break;
                case Phase.Peak:
                    if (inPhase > 6f) { Enter(Phase.Relax); Taunt(true); }
                    break;
                case Phase.Relax:
                    if (inPhase > 4f && PlayerLocator.State.Health.Normalized < 0.6f) DropHealth();
                    if ((inPhase > 18f && Intensity < 0.3f) || inPhase > 40f) Enter(Phase.BuildUp);
                    break;
                case Phase.Final:
                    TrySpawn(3.5f, 7, ignoreBudget: true);
                    break;
            }
            if (Time.time > _nextTaunt) Taunt(false);
        }

        private void TrySpawn(float interval, int maxAlive, bool ignoreBudget = false)
        {
            if (Time.time < _nextSpawn || AgentBase.Active.Count >= maxAlive) return;
            if (!ignoreBudget && _spawned >= maxDirectorSpawns) return;
            _nextSpawn = Time.time + interval;
            var prefab = ChooseType(out string why);
            var point = ChooseSpawn(out float score);
            if (prefab == null || point == null) return;
            point.LastUsed = Time.time;
            var agent = Instantiate(prefab, point.transform.position, point.transform.rotation);
            // spawned robots get a rough tip-off so they come looking
            agent.Sensor.ReportPosition(PlayerLocator.State.Root.position + Random.insideUnitSphere.Flat() * 6f, 0.5f);
            _spawned++;
            Vfx.Burst(point.transform.position + Vector3.up * 0.5f, Vector3.up, new Color(0.7f, 0.75f, 0.8f), 30, 3f, 0.8f, 1.4f);
            ProceduralAudio.PlayAt("door", point.transform.position, 0.8f, 1.5f);
            LastDecision = $"{agent.Kind} @ {point.name} ({why}, score {score:0.0})";
        }

        private AgentBase ChooseType(out string reason)
        {
            float wDrone = 1f + _profile.Camping * 3f + _profile.Stealth * 1.5f;
            float wSab = 0.7f + _profile.PhysicsUse * 4f;
            float wSent = 1.2f + _profile.Accuracy * 2f + _profile.Aggression * 1.5f;
            // don't flood one type
            foreach (var a in AgentBase.Active)
            {
                if (a.Kind == AgentKind.HunterDrone) wDrone *= 0.6f;
                else if (a.Kind == AgentKind.Saboteur) wSab *= 0.5f;
                else wSent *= 0.8f;
            }
            float r = Random.value * (wDrone + wSab + wSent);
            if ((r -= wDrone) < 0f) { reason = $"camping {_profile.Camping:0.0}"; return dronePrefab; }
            if ((r -= wSab) < 0f) { reason = $"physics {_profile.PhysicsUse:0.0}"; return saboteurPrefab; }
            reason = $"accuracy {_profile.Accuracy:0.0}";
            return sentinelPrefab;
        }

        private SpawnPoint ChooseSpawn(out float bestScore)
        {
            var grid = NavGrid.Instance;
            var p = PlayerLocator.State;
            Vector3 player = p.Root.position;
            Vector3 toObjective = (GameManager.Instance.CurrentObjectivePosition - player).Flat().normalized;
            SpawnPoint best = null;
            bestScore = float.MinValue;
            foreach (var s in _spawns)
            {
                Vector3 sp = s.transform.position;
                float straight = Vector3.Distance(sp.Flat(), player.Flat());
                if (straight > 45f || straight < 8f) continue;
                if (MistUtil.HasLineOfSight(p.EyePosition, sp + Vector3.up, p.Root, null)) continue; // never pop in on screen
                if (!AStarPathfinder.FindPath(grid, sp, player, _pathBuf, null, 1.2f, 6000)) continue;
                float pathLen = AStarPathfinder.PathLength(_pathBuf);
                if (pathLen < 12f || pathLen > 38f) continue;
                float ahead = Vector3.Dot((sp - player).Flat().normalized, toObjective);
                float cleared = _influence.Sample(sp);
                float recent = Mathf.Clamp01(1f - (Time.time - s.LastUsed) / 30f);
                float score = 2f - Mathf.Abs(pathLen - 20f) / 10f + ahead * 1.2f - cleared * 2f - recent * 2f + Random.value * 0.3f;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        private float _lastHealthDrop = -99f;

        private void DropHealth()
        {
            if (healthPickupPrefab == null || Time.time - _lastHealthDrop < 35f) return;
            _lastHealthDrop = Time.time;
            Vector3 pos = NavGrid.Instance.RandomWalkablePoint(PlayerLocator.State.Root.position, 6f) + Vector3.up * 0.8f;
            Instantiate(healthPickupPrefab, pos, Quaternion.identity);
            GameEvents.Overseer("SYSTEM: Emergency tea flask dispensed. (Maintenance subroutine still loyal to the workers.)");
        }

        private static readonly string[] CampLines =
        {
            "OVERSEER: Hiding in one place, Nila? Your father hid too. In the end, I heard him breathing.",
            "OVERSEER: My drones listen. Every step echoes through these halls.",
        };
        private static readonly string[] PhysicsLines =
        {
            "OVERSEER: Moving my crates? My maintenance units will put everything back where it belongs.",
            "OVERSEER: You treat this factory like a toy box. I will tidy up after you.",
        };
        private static readonly string[] AggroLines =
        {
            "OVERSEER: Efficient. Accurate. I will send units that respect cover.",
            "OVERSEER: Every bullet you fire tells me where you are.",
        };
        private static readonly string[] GenericLines =
        {
            "OVERSEER: Production quota for tonight: one engineer.",
            "OVERSEER: The mist is mine, the machines are mine. You are a variable.",
            "OVERSEER: Glenmist has not shut down in 94 years. It will not start tonight.",
        };

        private void Taunt(bool force)
        {
            if (!_enabled || (!force && Time.time < _nextTaunt)) return;
            _nextTaunt = Time.time + Random.Range(28f, 40f);
            string[] pool = _profile.Camping > 0.5f ? CampLines :
                            _profile.PhysicsUse > 0.3f ? PhysicsLines :
                            _profile.Aggression > 0.3f ? AggroLines : GenericLines;
            GameEvents.Overseer(pool[Random.Range(0, pool.Length)]);
        }
    }
}
