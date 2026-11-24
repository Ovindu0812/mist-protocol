using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.AI.HunterDrone;
using MistProtocol.AI.Saboteur;
using MistProtocol.AI.Sentinel;
using UnityEngine;

namespace MistProtocol.World
{
    /// <summary>
    /// Spawns and manages tactical combat squads in Sector A (Withering Loft - First Room)
    /// and Sector C (Drying Chamber - Keycard Room).
    /// Withering Loft: 3 Sentinels, 3 Hunter Drones, 2 Saboteurs (quadruped crawler robots).
    /// Drying Chamber: 3 Sentinels, 3 Hunter Drones, 2 Saboteurs.
    /// </summary>
    public class FacilityEncounterSquads : MonoBehaviour
    {
        public static FacilityEncounterSquads Instance { get; private set; }

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
            EnsureAllEncounterSquads();
        }

        public void EnsureAllEncounterSquads()
        {
            if (_hasInitialized) return;

            var sentinelPrefab = Resources.Load<GameObject>("Agent_Sentinel");
            var dronePrefab = Resources.Load<GameObject>("Agent_HunterDrone");
            var saboteurPrefab = Resources.Load<GameObject>("Agent_Saboteur");

#if UNITY_EDITOR
            if (sentinelPrefab == null)
                sentinelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Agent_Sentinel.prefab");
            if (dronePrefab == null)
                dronePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Agent_HunterDrone.prefab");
            if (saboteurPrefab == null)
                saboteurPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Agent_Saboteur.prefab");
#endif

            EnsureWitheringLoftSquad(sentinelPrefab, dronePrefab, saboteurPrefab);
            EnsureDryingChamberSquad(sentinelPrefab, dronePrefab, saboteurPrefab);

            _hasInitialized = true;
        }

        private void EnsureWitheringLoftSquad(GameObject sentinelPrefab, GameObject dronePrefab, GameObject saboteurPrefab)
        {
            if (GameObject.Find("WitheringLoft_Sentinel_1") != null) return;

            var group = new GameObject("WitheringLoft_Encounter_Squad");
            group.transform.SetParent(transform, false);

            // 1. Deploy 3 Sentinels
            if (sentinelPrefab != null)
            {
                SpawnSentinel(sentinelPrefab, "WitheringLoft_Sentinel_1",
                    new Vector3(-20f, 0f, 8f), 0f, group.transform,
                    new Vector3(-20f, 0f, 4f), new Vector3(-26f, 0f, 4f), new Vector3(-26f, 0f, 14f), new Vector3(-20f, 0f, 14f));

                SpawnSentinel(sentinelPrefab, "WitheringLoft_Sentinel_2",
                    new Vector3(-18f, 0f, 18f), 180f, group.transform,
                    new Vector3(-18f, 0f, 14f), new Vector3(-13f, 0f, 14f), new Vector3(-13f, 0f, 24f), new Vector3(-18f, 0f, 24f));

                SpawnSentinel(sentinelPrefab, "WitheringLoft_Sentinel_3",
                    new Vector3(-24f, 0f, 28f), -90f, group.transform,
                    new Vector3(-24f, 0f, 26f), new Vector3(-28f, 0f, 26f), new Vector3(-28f, 0f, 33f), new Vector3(-24f, 0f, 33f));
            }

            // 2. Deploy 3 Hunter Drones
            if (dronePrefab != null)
            {
                SpawnDrone(dronePrefab, "WitheringLoft_Drone_1", new Vector3(-20f, 2.4f, 10f), 0f, group.transform);
                SpawnDrone(dronePrefab, "WitheringLoft_Drone_2", new Vector3(-16f, 2.5f, 20f), 180f, group.transform);
                SpawnDrone(dronePrefab, "WitheringLoft_Drone_3", new Vector3(-24f, 2.4f, 30f), -90f, group.transform);
            }

            // 3. Deploy 2 Saboteurs (dog-like crawler robots)
            if (saboteurPrefab != null)
            {
                SpawnSaboteur(saboteurPrefab, "WitheringLoft_Saboteur_1", new Vector3(-27f, 0f, 12f), 90f, group.transform);
                SpawnSaboteur(saboteurPrefab, "WitheringLoft_Saboteur_2", new Vector3(-13f, 0f, 24f), -90f, group.transform);
            }
        }

        private void EnsureDryingChamberSquad(GameObject sentinelPrefab, GameObject dronePrefab, GameObject saboteurPrefab)
        {
            if (GameObject.Find("DryingChamber_Sentinel_1") != null) return;

            var group = new GameObject("DryingChamber_Encounter_Squad");
            group.transform.SetParent(transform, false);

            // 1. Deploy 3 Sentinels
            if (sentinelPrefab != null)
            {
                // Sentinel 1: Near the entrance gate opened with the key
                SpawnSentinel(sentinelPrefab, "DryingChamber_Sentinel_1",
                    new Vector3(14f, 0f, 6f), 0f, group.transform,
                    new Vector3(14f, 0f, 4f), new Vector3(18f, 0f, 4f), new Vector3(18f, 0f, 10f), new Vector3(14f, 0f, 10f));

                // Sentinel 2: Central aisle between ovens
                SpawnSentinel(sentinelPrefab, "DryingChamber_Sentinel_2",
                    new Vector3(20f, 0f, 14f), 180f, group.transform,
                    new Vector3(20f, 0f, 8f), new Vector3(20f, 0f, 20f), new Vector3(25f, 0f, 20f), new Vector3(25f, 0f, 8f));

                // Sentinel 3: East corridor leading to Titan entrance
                SpawnSentinel(sentinelPrefab, "DryingChamber_Sentinel_3",
                    new Vector3(26f, 0f, 16f), -90f, group.transform,
                    new Vector3(26f, 0f, 12f), new Vector3(28f, 0f, 12f), new Vector3(28f, 0f, 20f), new Vector3(26f, 0f, 20f));
            }

            // 2. Deploy 3 Hunter Drones
            if (dronePrefab != null)
            {
                SpawnDrone(dronePrefab, "DryingChamber_Drone_1", new Vector3(15f, 2.4f, 7f), 0f, group.transform);
                SpawnDrone(dronePrefab, "DryingChamber_Drone_2", new Vector3(20f, 2.5f, 14f), 180f, group.transform);
                SpawnDrone(dronePrefab, "DryingChamber_Drone_3", new Vector3(25f, 2.4f, 20f), -90f, group.transform);
            }

            // 3. Deploy 2 Saboteurs (dog-like crawler robots)
            if (saboteurPrefab != null)
            {
                SpawnSaboteur(saboteurPrefab, "DryingChamber_Saboteur_1", new Vector3(18f, 0f, 4f), 90f, group.transform);
                SpawnSaboteur(saboteurPrefab, "DryingChamber_Saboteur_2", new Vector3(26f, 0f, 8f), -90f, group.transform);
            }
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
        }

        private void SpawnDrone(GameObject prefab, string name, Vector3 pos, float yaw, Transform parent)
        {
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            go.name = name;
        }

        private void SpawnSaboteur(GameObject prefab, string name, Vector3 pos, float yaw, Transform parent)
        {
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            go.name = name;
        }
    }
}
