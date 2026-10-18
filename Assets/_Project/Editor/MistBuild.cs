using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MistProtocol.EditorTools
{
    /// <summary>
    /// One-click (or headless) pipeline: textures -> materials -> prefabs -> level ->
    /// NavMesh -> lightmaps -> occlusion -> build settings.
    ///   Menu:  Mist Protocol / Build Everything
    ///   CLI:   Unity -batchmode -projectPath . -executeMethod MistProtocol.EditorTools.MistBuild.BuildAll -quit
    /// </summary>
    public static class MistBuild
    {
        public const string ScenePath = "Assets/_Project/Scenes/GlenmistFactory.unity";

        [MenuItem("Mist Protocol/Build Everything (no lightmap bake)")]
        public static void BuildAllNoBake() => Run(false);

        [MenuItem("Mist Protocol/Build Everything + Bake Lighting")]
        public static void BuildAll() => Run(!System.Environment.GetCommandLineArgs().Contains("-skipBake"));

        [MenuItem("Mist Protocol/Bake Lighting Only")]
        public static void BakeOnly()
        {
            EditorSceneManager.OpenScene(ScenePath);
            BakeLighting();
        }

        private static void Run(bool bake)
        {
            ConfigureUrp();
            Debug.Log("[MistBuild] textures");
            TextureFactory.GenerateAll();
            AssetDatabase.ImportAsset("Assets/_Project/Art/Textures", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[MistBuild] materials");
            MaterialFactory.CreateAll();
            MeshFactory.ClearCache();
            Debug.Log("[MistBuild] prefabs");
            PrefabFactory.CreateAll();
            Debug.Log("[MistBuild] level");
            Directory.CreateDirectory("Assets/_Project/Scenes");
            LevelBuilder.Build(ScenePath);
            Debug.Log("[MistBuild] navmesh");
            BakeNavMesh();
            if (bake) { Debug.Log("[MistBuild] lightmaps"); BakeLighting(); }
            Debug.Log("[MistBuild] occlusion");
            StaticOcclusionCulling.smallestOccluder = 2f;
            StaticOcclusionCulling.smallestHole = 0.3f;
            StaticOcclusionCulling.Compute();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Mist Protocol";
            PlayerSettings.companyName = "Team Glenmist";
            AssetDatabase.SaveAssets();
            ReportVerification();
            Debug.Log("[MistBuild] DONE");
        }

        /// <summary>Enable shadows from additional lights (the player's headlamp) on every URP asset in the project.</summary>
        private static void ConfigureUrp()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(asset);
                void B(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
                void I(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
                void F(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
                B("m_AdditionalLightShadowsSupported", true);
                B("m_MainLightShadowsSupported", true);
                B("m_SoftShadowsSupported", true);
                I("m_AdditionalLightsShadowmapResolution", 2048);
                F("m_ShadowDistance", 40f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BakeNavMesh()
        {
            // only level geometry + props contribute; gameplay triggers, lights and agents are excluded
            var level = GameObject.Find("Level");
            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            const string path = "Assets/_Project/Scenes/GlenmistFactory_NavMesh.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            EditorSceneManager.MarkSceneDirty(level.scene);
            EditorSceneManager.SaveScene(level.scene);
        }

        private static void BakeLighting()
        {
            const string path = "Assets/_Project/Settings/LS_Glenmist.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (ls == null) { ls = new LightingSettings { name = "LS_Glenmist" }; AssetDatabase.CreateAsset(ls, path); }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.Subtractive;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.lightmapResolution = 6f;
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.directSampleCount = 16;
            ls.indirectSampleCount = 96;
            ls.environmentSampleCount = 64;
            ls.maxBounces = 2;
            ls.ao = true;
            ls.aoMaxDistance = 1.2f;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            Lightmapping.lightingSettings = ls;
            var t = System.Diagnostics.Stopwatch.StartNew();
            bool ok = Lightmapping.Bake();
            Debug.Log($"[MistBuild] lightmap bake ok={ok} in {t.Elapsed.TotalSeconds:0}s, lightmaps={LightmapSettings.lightmaps.Length}");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        /// <summary>Sanity checks printed to the log (model orientation, navmesh, counts).</summary>
        private static void ReportVerification()
        {
            var sentinel = PrefabFactory.Sentinel;
            var visor = sentinel.transform.Find("SentinelBot")?.GetComponentsInChildren<Transform>();
            Transform v = null;
            if (visor != null) foreach (var t in visor) if (t.name == "Visor") v = t;
            if (v != null)
            {
                var r = v.GetComponent<Renderer>();
                Debug.Log($"[MistBuild] VERIFY sentinel visor centre (local to root) = {sentinel.transform.InverseTransformPoint(r.bounds.center)} (z>0 means model faces +Z)");
            }
            var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
            Debug.Log($"[MistBuild] VERIFY navmesh verts={tri.vertices.Length} tris={tri.indices.Length / 3}");
            Debug.Log($"[MistBuild] VERIFY renderers={Object.FindObjectsByType<MeshRenderer>().Length} lights={Object.FindObjectsByType<Light>().Length}");
        }
    }

    internal static class ArgsExt
    {
        public static bool Contains(this string[] a, string s) => System.Array.IndexOf(a, s) >= 0;
    }
}
