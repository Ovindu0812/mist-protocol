using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.AI.HunterDrone;
using MistProtocol.AI.Sentinel;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.World
{
    /// <summary>
    /// Manages the intense combat encounter inside Zone B (Rolling Room).
    /// Ensures 3 additional Sentinel tactical robots and 3 Hunter Drones
    /// are active to defend the orthodox tea rolling machinery floor.
    /// </summary>
    public class RollingRoomEncounter : MonoBehaviour
    {
        public static RollingRoomEncounter Instance { get; private set; }

        private readonly List<GameObject> _spawnedSentinels = new();
        private readonly List<GameObject> _spawnedDrones = new();
        private bool _hasInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsureRollingRoomCombatSquad();
        }

        /// <summary>
        /// Instantiates and deploys the 3 Sentinels and 3 Hunter Drones
        /// specifically dedicated to the Rolling Room combat encounter if missing.
        /// </summary>
        public void EnsureRollingRoomCombatSquad()
        {
            if (_hasInitialized) return;

            // Check if already placed in scene by LevelBuilder or prior run
            if (GameObject.Find("RollingRoom_Sentinel_1") != null)
            {
                _hasInitialized = true;
                return;
            }

            var sentinelPrefab = Resources.Load<GameObject>("Agent_Sentinel");
            var dronePrefab = Resources.Load<GameObject>("Agent_HunterDrone");

            Transform parentGroup = transform;

            // 1. Deploy 3 Additional Sentinels with dedicated tactical patrol sectors
            if (sentinelPrefab != null)
            {
                // Sentinel 1: North flank along sorting conveyor
                SpawnSentinel(sentinelPrefab, "RollingRoom_Sentinel_1",
                    new Vector3(-4f, 0f, 28f), 0f, parentGroup,
                    new Vector3(-4f, 0f, 28f), new Vector3(4f, 0f, 28f), new Vector3(4f, 0f, 33f), new Vector3(-4f, 0f, 33f));

                // Sentinel 2: South flank across rolling machine bays
                SpawnSentinel(sentinelPrefab, "RollingRoom_Sentinel_2",
                    new Vector3(4f, 0f, 8f), 180f, parentGroup,
                    new Vector3(4f, 0f, 8f), new Vector3(-4f, 0f, 8f), new Vector3(-4f, 0f, 4f), new Vector3(4f, 0f, 4f));

                // Sentinel 3: East exit corridor chokepoint guarding Drying Chamber threshold
                SpawnSentinel(sentinelPrefab, "RollingRoom_Sentinel_3",
                    new Vector3(6f, 0f, 18f), -90f, parentGroup,
                    new Vector3(6f, 0f, 18f), new Vector3(6f, 0f, 12f), new Vector3(8f, 0f, 16f), new Vector3(8f, 0f, 24f));
            }

            // 2. Deploy 3 Hunter Drones hovering at tactical vantage heights
            if (dronePrefab != null)
            {
                // Drone 1: Hovering above North sorting conveyor
                SpawnDrone(dronePrefab, "RollingRoom_Drone_1", new Vector3(-2f, 2.4f, 29f), 180f, parentGroup);

                // Drone 2: Hovering above central rolling machine aisle
                SpawnDrone(dronePrefab, "RollingRoom_Drone_2", new Vector3(0f, 2.5f, 16f), -90f, parentGroup);

                // Drone 3: Hovering above South machinery entrance
                SpawnDrone(dronePrefab, "RollingRoom_Drone_3", new Vector3(3f, 2.4f, 7f), 0f, parentGroup);
            }

            _hasInitialized = true;
        }

        private void SpawnSentinel(GameObject prefab, string name, Vector3 pos, float yaw, Transform parent, params Vector3[] waypoints)
        {
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            go.name = name;

            var brain = go.GetComponent<SentinelBrain>();
            if (brain != null && waypoints != null && waypoints.Length > 0)
            {
                var ptList = new List<Transform>();
                for (int i = 0; i < waypoints.Length; i++)
                {
                    var wp = new GameObject($"{name}_WP_{i}");
                    wp.transform.position = waypoints[i];
                    wp.transform.SetParent(go.transform, true);
                    ptList.Add(wp.transform);
                }
                brain.SetPatrolPoints(ptList.ToArray());
            }

            _spawnedSentinels.Add(go);
        }

        private void SpawnDrone(GameObject prefab, string name, Vector3 pos, float yaw, Transform parent)
        {
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            go.name = name;
            _spawnedDrones.Add(go);
        }
    }
}
