using System.Collections.Generic;
using UnityEngine;

namespace MistProtocol.World
{
    public enum ContainerType
    {
        Container20ft,
        Container40ft
    }

    public enum ContainerColor
    {
        Teal,
        Red
    }

    /// <summary>
    /// Spawns and manages industrial shipping cargo containers in the Drying Chamber (Zone 2).
    /// Fills the wide warehouse floor with realistic cover, verticality, and tactical corridors.
    /// Supports both Blender FBX models and procedural colliders.
    /// </summary>
    public class DryingChamberContainers : MonoBehaviour
    {
        public static DryingChamberContainers Instance { get; private set; }

        [SerializeField] private bool autoBuildOnStart = true;
        private readonly List<GameObject> _spawnedContainers = new();

        public IReadOnlyList<GameObject> SpawnedContainers => _spawnedContainers;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildContainersIfMissing();
            }
        }

        public void BuildContainersIfMissing()
        {
            // Remove any legacy door-blocking containers if they exist in the scene
            var existingSW = GameObject.Find("Container_SW_Gate");
            if (existingSW != null) Destroy(existingSW);
            var existingN = GameObject.Find("Container_N_ControlBarrier_40ft");
            if (existingN != null) Destroy(existingN);

            if (GameObject.Find("DryingChamber_Containers_Group") != null) return;

            var group = new GameObject("DryingChamber_Containers_Group");
            group.transform.SetParent(transform, false);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Materials matching the Blender container models
            var tealMat = new Material(litShader) { color = new Color(0.11f, 0.35f, 0.40f) };
            tealMat.SetFloat("_Metallic", 0.35f);
            tealMat.SetFloat("_Smoothness", 0.38f);

            var redMat = new Material(litShader) { color = new Color(0.49f, 0.20f, 0.18f) };
            redMat.SetFloat("_Metallic", 0.30f);
            redMat.SetFloat("_Smoothness", 0.35f);

            var metalMat = new Material(litShader) { color = new Color(0.68f, 0.68f, 0.66f) };
            metalMat.SetFloat("_Metallic", 0.85f);
            metalMat.SetFloat("_Smoothness", 0.65f);

            // Load Blender FBX container model
            GameObject fbxPrefab = Resources.Load<GameObject>("Models/CargoContainer");
#if UNITY_EDITOR
            if (fbxPrefab == null)
            {
                fbxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/CargoContainer.fbx");
            }
#endif

            // Layout specs for Drying Chamber (Zone 2: X=10 to 30, Z=0 to 24)
            // Doorways at SW Gate (X=10, Z=4) and North Control Room (X=20, Z=24) are kept 100% CLEAR!
            // 1. South Wall Heavy Freight Line (Long 40ft container against south wall)
            SpawnContainer(group.transform, "Container_S_Wall_40ft", new Vector3(22.0f, 0f, 1.8f), 0f, ContainerType.Container40ft, ContainerColor.Red, fbxPrefab, tealMat, redMat, metalMat);

            // 2. Stacked Upper 20ft Container on South Wall
            SpawnContainer(group.transform, "Container_S_Stacked_Upper", new Vector3(21.0f, 2.59f, 1.8f), 0f, ContainerType.Container20ft, ContainerColor.Teal, fbxPrefab, tealMat, redMat, metalMat);

            // 3. West Flank Corridor (Between Dryer 1 and West Wall, clear of door)
            SpawnContainer(group.transform, "Container_W_Flank", new Vector3(11.6f, 0f, 13.5f), 90f, ContainerType.Container20ft, ContainerColor.Teal, fbxPrefab, tealMat, redMat, metalMat);

            // 4. East Sector E Staging Area (Near Sector E Wall)
            SpawnContainer(group.transform, "Container_E_VaultStaging", new Vector3(28.4f, 0f, 8.5f), 90f, ContainerType.Container20ft, ContainerColor.Red, fbxPrefab, tealMat, redMat, metalMat);
        }

        private GameObject SpawnContainer(Transform parent, string name, Vector3 pos, float yaw, ContainerType type, ContainerColor col, GameObject fbxPrefab, Material tealMat, Material redMat, Material metalMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float len = type == ContainerType.Container20ft ? 6.06f : 12.19f;
            float wid = 2.44f;
            float hei = 2.59f;

            if (fbxPrefab != null)
            {
                string targetSubName = type == ContainerType.Container20ft ? "CargoContainer_20ft" : "CargoContainer_40ft";
                var inst = Instantiate(fbxPrefab, root.transform);
                inst.name = "Model";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;

                Transform subObj = inst.transform.Find(targetSubName);
                if (subObj != null)
                {
                    foreach (Transform child in inst.transform)
                    {
                        child.gameObject.SetActive(child == subObj);
                    }
                }

                Material targetMat = col == ContainerColor.Teal ? tealMat : redMat;
                foreach (var rend in inst.GetComponentsInChildren<MeshRenderer>())
                {
                    if (rend.name.Contains("Hardware") || rend.name.Contains("Rod") || rend.name.Contains("Casting"))
                        rend.material = metalMat;
                    else
                        rend.material = targetMat;
                }
            }
            else
            {
                // Procedural fallback cube if model not yet loaded
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Mesh_Fallback";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(0f, hei * 0.5f, 0f);
                cube.transform.localScale = new Vector3(len, hei, wid);
                cube.GetComponent<Renderer>().material = col == ContainerColor.Teal ? tealMat : redMat;
            }

            // Box Collider for bullet and character collision cover
            var boxCol = root.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = root.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(len, hei, wid);
            boxCol.center = new Vector3(0f, hei * 0.5f, 0f);

            _spawnedContainers.Add(root);
            return root;
        }
    }
}
