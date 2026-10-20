using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.AI.Director;
using MistProtocol.AI.Navigation;
using MistProtocol.AI.Perception;
using MistProtocol.AI.Sentinel;
using MistProtocol.Core;
using MistProtocol.Interaction;
using MistProtocol.UI;
using MistProtocol.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MistProtocol.EditorTools
{
    /// <summary>
    /// World Builder: constructs the Glenmist factory level.
    ///
    ///   z=36 +-------------------+-------------------+-------------------+
    ///        |                   |                   |  D  CONTROL ROOM  |
    ///        |  A  WITHERING     |  B  ROLLING ROOM  |   (kill switch)   |
    ///        |     LOFT          |                   +--------door-------+ z=24
    ///        |   (keycard desk)  door                |                   |
    ///        |                   |                  gate  C  DRYING      |
    ///        |                   |                   |     CHAMBER       |
    ///   z=0  +------gap----------+-------------------+-------------------+
    ///        |  YARD (spawn)     |
    ///   z=-8 +-------------------+
    ///      x=-30               x=-10               x=10                x=30
    /// </summary>
    public static class LevelBuilder
    {
        private const float H = 5f;      // wall height
        private const float T = 0.4f;    // wall thickness
        private const float DoorGap = 1.7f;

        private static Transform _static, _props, _lights, _gameplay, _waypoints;

        private const StaticEditorFlags StaticFlags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                                                      StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                                                      StaticEditorFlags.ReflectionProbeStatic;

        public static readonly Color FogColor = new Color(0.33f, 0.38f, 0.42f);

        public static void Build(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("Level").transform;
            _static = new GameObject("Level_Static").transform;
            _props = new GameObject("Level_Props").transform;
            _static.SetParent(level);
            _props.SetParent(level);
            _lights = new GameObject("Lighting").transform;
            _gameplay = new GameObject("Gameplay").transform;
            _waypoints = new GameObject("Waypoints").transform;

            Environment();
            Shell();
            ZoneWitheringLoft();
            ZoneRollingRoom();
            ZoneDryingChamber();
            ZoneControlRoom();
            Outside();
            var doors = Doors();
            Lamps();
            Mist();
            PostProcessing();
            Gameplay(doors);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // ================================================================== environment
        private static void Environment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.26f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.11f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.032f;
            RenderSettings.fogColor = FogColor;

            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.transform.SetParent(_lights);
            moon.type = LightType.Directional;
            moon.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            moon.color = new Color(0.62f, 0.72f, 0.95f);
            moon.intensity = 0.45f;
            moon.shadows = LightShadows.Soft;
            moon.lightmapBakeType = LightmapBakeType.Mixed;
            RenderSettings.sun = moon;
        }

        // ================================================================== geometry helpers
        public static GameObject Block(string name, Vector3 center, Vector3 size, Material mat, float tile = 2f,
            Transform parent = null, bool collider = true, bool isStatic = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _static, false);
            go.transform.position = center;
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Box(size, tile);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticFlags);
            return go;
        }

        /// <summary>Wall running along X at depth z, from x0 to x1, with door gaps (centre, width) and lintels.</summary>
        private static void WallX(string name, float z, float x0, float x1, Material mat, params (float c, float w)[] gaps)
        {
            float cur = x0;
            var sorted = new List<(float c, float w)>(gaps);
            sorted.Sort((a, b) => a.c.CompareTo(b.c));
            foreach (var g in sorted)
            {
                float a = g.c - g.w * 0.5f, b = g.c + g.w * 0.5f;
                if (a > cur) Block($"{name}_{cur:0}", new Vector3((cur + a) * 0.5f, H * 0.5f, z), new Vector3(a - cur, H, T), mat, 2f);
                Block($"{name}_Lintel_{g.c:0}", new Vector3(g.c, (H + 2.6f) * 0.5f, z), new Vector3(g.w, H - 2.6f, T), mat, 2f);
                cur = b;
            }
            if (x1 > cur) Block($"{name}_{cur:0}", new Vector3((cur + x1) * 0.5f, H * 0.5f, z), new Vector3(x1 - cur, H, T), mat, 2f);
        }

        /// <summary>Wall running along Z at x, from z0 to z1.</summary>
        private static void WallZ(string name, float x, float z0, float z1, Material mat, params (float c, float w)[] gaps)
        {
            float cur = z0;
            var sorted = new List<(float c, float w)>(gaps);
            sorted.Sort((a, b) => a.c.CompareTo(b.c));
            foreach (var g in sorted)
            {
                float a = g.c - g.w * 0.5f, b = g.c + g.w * 0.5f;
                if (a > cur) Block($"{name}_{cur:0}", new Vector3(x, H * 0.5f, (cur + a) * 0.5f), new Vector3(T, H, a - cur), mat, 2f);
                Block($"{name}_Lintel_{g.c:0}", new Vector3(x, (H + 2.6f) * 0.5f, g.c), new Vector3(T, H - 2.6f, g.w), mat, 2f);
                cur = b;
            }
            if (z1 > cur) Block($"{name}_{cur:0}", new Vector3(x, H * 0.5f, (cur + z1) * 0.5f), new Vector3(T, H, z1 - cur), mat, 2f);
        }

        public static GameObject Prop(string fbx, Vector3 pos, float yaw = 0f, bool isStatic = true, Vector3? scale = null)
        {
            var go = PrefabFactory.Model(fbx, _props, MaterialFactory.Glow, "Console_Screen");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (scale.HasValue) go.transform.localScale = scale.Value;
            FitCollider(go);
            if (isStatic) foreach (var t in go.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticFlags);
            return go;
        }

        /// <summary>One box collider around all child renderers, in the root's local space.</summary>
        public static void FitCollider(GameObject go)
        {
            var rs = go.GetComponentsInChildren<MeshRenderer>();
            if (rs.Length == 0) return;
            var inv = go.transform.worldToLocalMatrix;
            var b = new Bounds();
            bool first = true;
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = inv * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            var col = go.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = b.size;
        }

        private static GameObject Place(GameObject prefab, Vector3 pos, float yaw = 0f, Transform parent = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent != null ? parent : _props, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        // ================================================================== shell
        private static void Shell()
        {
            // floor slab for the whole factory + outer ground plane
            Block("Floor_Factory", new Vector3(0f, -0.1f, 18f), new Vector3(60.4f, 0.2f, 36.4f), MaterialFactory.Floor, 4f);
            Block("Floor_Yard", new Vector3(-20f, -0.1f, -4.1f), new Vector3(20.4f, 0.2f, 8.2f), MaterialFactory.Concrete, 3f);

            // exterior walls: brick plinth + green corrugated cladding (classic hill-country tea factory)
            void Exterior(string n, Vector3 c, Vector3 size)
            {
                Block(n + "_Brick", new Vector3(c.x, 0.75f, c.z), new Vector3(size.x, 1.5f, size.z), MaterialFactory.Brick, 2f);
                Block(n + "_Clad", new Vector3(c.x, 1.5f + (H - 1.5f) * 0.5f, c.z), new Vector3(size.x, H - 1.5f, size.z), MaterialFactory.Corrugated, 2f);
            }
            // south wall with the broken front gate at x = -20
            Exterior("Wall_S_W", new Vector3(-26.1f, 0, 0), new Vector3(8.2f, H, T));
            Exterior("Wall_S_E", new Vector3(5.9f, 0, 0), new Vector3(48.2f, H, T));
            Block("Wall_S_GateLintel", new Vector3(-20f, (H + 3.2f) * 0.5f, 0), new Vector3(3.8f, H - 3.2f, T), MaterialFactory.Corrugated, 2f);
            Exterior("Wall_N", new Vector3(0, 0, 36f), new Vector3(60.4f, H, T));
            Exterior("Wall_W", new Vector3(-30f, 0, 18f), new Vector3(T, H, 36.4f));
            Exterior("Wall_E", new Vector3(30f, 0, 18f), new Vector3(T, H, 36.4f));

            // interior partitions (lime-washed)
            WallZ("Wall_AB", -10f, 0f, 36f, MaterialFactory.Wall, (18f, DoorGap));
            WallZ("Wall_BC", 10f, 0f, 36f, MaterialFactory.Wall, (6f, DoorGap));
            WallX("Wall_CD", 24f, 10f, 30f, MaterialFactory.Wall, (20f, DoorGap));

            // roof: corrugated sheets, loft roof has two skylight strips for moon shafts through the mist
            Block("Roof_A1", new Vector3(-20f, H + 0.1f, 4.5f), new Vector3(20.4f, 0.2f, 9.4f), MaterialFactory.Corrugated, 2f);
            Block("Roof_A2", new Vector3(-20f, H + 0.1f, 17.75f), new Vector3(20.4f, 0.2f, 13.5f), MaterialFactory.Corrugated, 2f);
            Block("Roof_A3", new Vector3(-20f, H + 0.1f, 31.25f), new Vector3(20.4f, 0.2f, 9.5f), MaterialFactory.Corrugated, 2f);
            Block("Roof_BCD", new Vector3(10f, H + 0.1f, 18f), new Vector3(40.4f, 0.2f, 36.4f), MaterialFactory.Corrugated, 2f);
            foreach (var r in new[] { "Roof_A1", "Roof_A2", "Roof_A3", "Roof_BCD" })
                GameObject.Find(r).AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            // roof trusses
            for (float x = -28f; x <= 28f; x += 4f)
                Block($"Truss_{x:0}", new Vector3(x, H - 0.35f, 18f), new Vector3(0.18f, 0.35f, 36f), MaterialFactory.DarkMetal, 1f, null, false);
        }

        // ================================================================== zones
        private static void ZoneWitheringLoft()
        {
            foreach (float x in new[] { -24.5f, -15.5f })
            foreach (float z in new[] { 6f, 13f, 20f, 27f })
                Prop("WitheringTrough", new Vector3(x, 0, z));

            // Appa's desk (keycard) in the north-west corner
            Block("Desk", new Vector3(-27.2f, 0.4f, 33.6f), new Vector3(1.8f, 0.8f, 0.9f), MaterialFactory.Wood, 1f, _props);
            Block("Desk_Shelf", new Vector3(-29.5f, 1.2f, 33.6f), new Vector3(0.4f, 2.4f, 2.2f), MaterialFactory.Wood, 1f, _props);
            Block("Desk_Ledger", new Vector3(-26.7f, 0.83f, 33.5f), new Vector3(0.35f, 0.05f, 0.25f), MaterialFactory.Hazard, 1f, _props, false);

            foreach (var p in new[] { new Vector3(-28.8f, 0, 2f), new Vector3(-28.8f, 0, 3f), new Vector3(-27.8f, 0, 2f), new Vector3(-11.2f, 0, 30f) })
                Prop("TeaCrate", p, 0f);
        }

        private static void ZoneRollingRoom()
        {
            foreach (float x in new[] { -5f, 5f })
            foreach (float z in new[] { 7f, 16f, 25f })
                Prop("TeaRollingMachine", new Vector3(x, 0, z), x < 0 ? 0f : 180f);
            foreach (float z in new[] { 3f, 11.5f, 20.5f, 29.5f })
                Block($"Pillar_B_{z:0}", new Vector3(0f, H * 0.5f, z), new Vector3(0.6f, H, 0.6f), MaterialFactory.Concrete, 1.5f);
            // sorting conveyor along the north wall
            Block("Conveyor", new Vector3(0f, 0.45f, 33.5f), new Vector3(14f, 0.9f, 1.2f), MaterialFactory.DarkMetal, 1f);
            Block("Conveyor_Belt", new Vector3(0f, 0.92f, 33.5f), new Vector3(14f, 0.05f, 1.0f), MaterialFactory.Concrete, 1f, null, false);
        }

        private static void ZoneDryingChamber()
        {
            // two ECP dryers: long brick ovens with hazard strips
            foreach (float x in new[] { 15.5f, 24.5f })
            {
                Block($"Dryer_{x:0}", new Vector3(x, 1.5f, 13.5f), new Vector3(3f, 3f, 9f), MaterialFactory.Brick, 2f);
                Block($"Dryer_Top_{x:0}", new Vector3(x, 3.1f, 13.5f), new Vector3(3.2f, 0.2f, 9.2f), MaterialFactory.DarkMetal, 1f);
                Block($"Dryer_Hazard_{x:0}", new Vector3(x, 0.15f, 13.5f), new Vector3(3.05f, 0.3f, 9.05f), MaterialFactory.Hazard, 1f, null, false);
                Block($"Dryer_Chimney_{x:0}", new Vector3(x, 4.1f, 16.5f), new Vector3(0.7f, 1.8f, 0.7f), MaterialFactory.DarkMetal, 1f, null, false);
            }
            foreach (var z in new[] { 4f, 21f })
                Block($"Pillar_C_{z:0}", new Vector3(20f, H * 0.5f, z), new Vector3(0.6f, H, 0.6f), MaterialFactory.Concrete, 1.5f);
        }

        private static void ZoneControlRoom()
        {
            var console = Prop("KillSwitchConsole", new Vector3(20f, 0, 32.5f), 180f, isStatic: false);
            console.name = "KillSwitchConsole";
            console.AddComponent<KillSwitch>();
            foreach (float x in new[] { 12f, 14f, 26f, 28f })
                Block($"ServerRack_{x:0}", new Vector3(x, 1.1f, 35.2f), new Vector3(1.4f, 2.2f, 0.9f), MaterialFactory.DarkMetal, 1f);
            foreach (float x in new[] { 12f, 14f, 26f, 28f })
                Block($"ServerRack_LED_{x:0}", new Vector3(x, 1.6f, 34.73f), new Vector3(1.0f, 0.06f, 0.02f), MaterialFactory.LampCyan, 1f, null, false);
            Block("Desk_Control", new Vector3(13.5f, 0.4f, 28f), new Vector3(2.4f, 0.8f, 1f), MaterialFactory.Wood, 1f);
        }

        private static void Outside()
        {
            Block("Ground", new Vector3(0f, -0.15f, 10f), new Vector3(160f, 0.2f, 140f), MaterialFactory.Ground, 4f);
            // yard fence
            Block("Fence_S", new Vector3(-20f, 0.8f, -8f), new Vector3(20.4f, 1.6f, 0.3f), MaterialFactory.Brick, 2f);
            Block("Fence_W", new Vector3(-30f, 0.8f, -4f), new Vector3(0.3f, 1.6f, 8f), MaterialFactory.Brick, 2f);
            Block("Fence_E", new Vector3(-10f, 0.8f, -4f), new Vector3(0.3f, 1.6f, 8f), MaterialFactory.Brick, 2f);
            Block("Fence_S_Rail", new Vector3(-20f, 2.0f, -8f), new Vector3(20.4f, 0.08f, 0.08f), MaterialFactory.DarkMetal, 1f, null, false);
            // gate pillars + fallen gate leaf + sign
            Block("GatePillar_L", new Vector3(-22.2f, 1.8f, -0.4f), new Vector3(0.6f, 3.6f, 0.6f), MaterialFactory.Brick, 1f);
            Block("GatePillar_R", new Vector3(-17.8f, 1.8f, -0.4f), new Vector3(0.6f, 3.6f, 0.6f), MaterialFactory.Brick, 1f);
            var fallen = Block("FallenGate", new Vector3(-24f, 0.12f, -3.5f), new Vector3(2.6f, 0.15f, 1.4f), MaterialFactory.Corrugated, 1f);
            fallen.transform.rotation = Quaternion.Euler(0f, 23f, 4f);
            var sign = new GameObject("Sign_Glenmist");
            sign.transform.SetParent(_static);
            sign.transform.position = new Vector3(-20f, 4.2f, -0.3f);
            sign.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var tm = sign.AddComponent<TextMesh>();
            tm.text = "GLENMIST TEA FACTORY  -  EST. 1932";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.characterSize = 0.12f;
            tm.fontSize = 48;
            tm.color = new Color(0.95f, 0.85f, 0.6f);
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            Block("Sign_Board", new Vector3(-20f, 4.2f, -0.22f), new Vector3(6.2f, 0.7f, 0.06f), MaterialFactory.DarkMetal, 1f, null, false);

            // tea bushes terraced on the hills around the factory (pure decoration, outside the play space)
            var bushes = new GameObject("TeaEstate").transform;
            bushes.SetParent(_static);
            var rng = new System.Random(7);
            for (int row = 0; row < 14; row++)
            for (int i = 0; i < 26; i++)
            {
                float x = -60f + i * 4.6f + (float)rng.NextDouble();
                float z = -14f - row * 3.2f;
                if (x > -32f && x < -8f && z > -10f) continue;
                float y = row * 0.55f;
                var b = Block("Bush", new Vector3(x, y + 0.4f, z), new Vector3(3.4f, 0.9f + (float)rng.NextDouble() * 0.3f, 1.8f), MaterialFactory.TeaBush, 2f, bushes, false);
                Block("Terrace", new Vector3(x, y - 0.2f, z), new Vector3(4.8f, 0.6f, 3.2f), MaterialFactory.Ground, 3f, bushes, false);
            }
            // misty hill silhouettes
            for (int i = 0; i < 7; i++)
            {
                var hill = Block($"Hill_{i}", new Vector3(-70f + i * 26f, 4f, 95f + (i % 2) * 15f), new Vector3(40f, 30f, 30f), MaterialFactory.Hill, 10f, _static, false);
                hill.transform.rotation = Quaternion.Euler(0f, 45f + i * 13f, 45f);
            }
        }

        // ================================================================== doors
        private static Dictionary<string, PhysicsDoor> Doors()
        {
            var d = new Dictionary<string, PhysicsDoor>();
            PhysicsDoor Make(string name, Vector3 hinge, float yaw, bool locked)
            {
                var go = Place(PrefabFactory.Door, hinge, yaw);
                go.name = name;
                var door = go.GetComponent<PhysicsDoor>();
                PrefabFactory.Set(door, "locked", locked);
                return door;
            }
            float off = DoorGap * 0.5f - 0.04f;
            d["AB"] = Make("Door_Loft_Rolling", new Vector3(-10f, 0f, 18f - off), -90f, false);
            d["BC"] = Make("Gate_DryingChamber", new Vector3(10f, 0f, 6f - off), -90f, true);
            d["CD"] = Make("Door_ControlRoom", new Vector3(20f - off, 0f, 24f), 0f, false);
            return d;
        }

        // ================================================================== lighting
        private static void Lamp(Vector3 pos, Color color, float intensity, float range, Material bulbMat, bool flicker = false)
        {
            var root = new GameObject("Lamp");
            root.transform.SetParent(_lights);
            root.transform.position = pos;
            Block("Cable", pos + Vector3.up * ((H - pos.y) * 0.5f), new Vector3(0.03f, H - pos.y, 0.03f), MaterialFactory.DarkMetal, 1f, root.transform, false);
            Block("Shade", pos + Vector3.up * 0.12f, new Vector3(0.6f, 0.12f, 0.6f), MaterialFactory.DarkMetal, 1f, root.transform, false);
            var bulb = Block("Bulb", pos, new Vector3(0.22f, 0.12f, 0.22f), bulbMat, 1f, root.transform, false);
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(root.transform, false);
            l.transform.localPosition = Vector3.down * 0.2f;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = flicker ? LightShadows.None : LightShadows.Soft;
            l.lightmapBakeType = flicker ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
            if (flicker)
            {
                var f = l.gameObject.AddComponent<LampFlicker>();
                PrefabFactory.Set(f, "bulb", bulb.GetComponent<Renderer>());
            }
        }

        private static void Lamps()
        {
            Color warm = new Color(1f, 0.72f, 0.42f), orange = new Color(1f, 0.42f, 0.16f), cyan = new Color(0.35f, 0.85f, 1f);
            foreach (float z in new[] { 6f, 16f, 27f })
            foreach (float x in new[] { -24.5f, -15.5f })
                Lamp(new Vector3(x, 3.9f, z + 3.5f), warm, 7f, 13f, MaterialFactory.LampWarm, x < -20f && z > 15f);
            Lamp(new Vector3(-27.2f, 2.6f, 33.6f), warm, 6f, 8f, MaterialFactory.LampWarm);
            foreach (float z in new[] { 4f, 12f, 21f, 30f })
            foreach (float x in new[] { -5f, 5f })
                Lamp(new Vector3(x, 3.9f, z), warm, 7f, 13f, MaterialFactory.LampWarm, x > 0 && z == 21f);
            foreach (float z in new[] { 4f, 13.5f, 21f })
            foreach (float x in new[] { 20f })
                Lamp(new Vector3(x, 3.6f, z), orange, 9f, 14f, MaterialFactory.LampOrange, z == 13.5f);
            Lamp(new Vector3(12.5f, 3.6f, 8f), orange, 6f, 11f, MaterialFactory.LampOrange);
            Lamp(new Vector3(27.5f, 3.6f, 18f), orange, 6f, 11f, MaterialFactory.LampOrange);
            foreach (float x in new[] { 14f, 20f, 26f })
                Lamp(new Vector3(x, 3.8f, 28f), cyan, 9f, 12f, MaterialFactory.LampCyan);
            Lamp(new Vector3(20f, 3.4f, 33.5f), cyan, 8f, 9f, MaterialFactory.LampCyan);
            // yard sodium lamps on poles
            foreach (float x in new[] { -27f, -13f })
            {
                Block($"Pole_{x:0}", new Vector3(x, 2.5f, -6.8f), new Vector3(0.15f, 5f, 0.15f), MaterialFactory.DarkMetal, 1f);
                var l = new GameObject("YardLight").AddComponent<Light>();
                l.transform.SetParent(_lights);
                l.transform.position = new Vector3(x, 4.8f, -6.3f);
                l.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
                l.type = LightType.Spot; l.spotAngle = 95f; l.range = 14f; l.intensity = 6f;
                l.color = new Color(1f, 0.6f, 0.25f);
                l.lightmapBakeType = LightmapBakeType.Baked;
                l.shadows = LightShadows.Soft;
                Block("Bulb", new Vector3(x, 4.95f, -6.4f), new Vector3(0.4f, 0.12f, 0.6f), MaterialFactory.LampOrange, 1f, null, false);
            }

            // reflection probes, one per zone
            void Probe(Vector3 c, Vector3 size)
            {
                var p = new GameObject("ReflectionProbe").AddComponent<ReflectionProbe>();
                p.transform.SetParent(_lights);
                p.transform.position = c;
                p.size = size;
                p.mode = ReflectionProbeMode.Baked;
                p.resolution = 64;
            }
            Probe(new Vector3(-20, 2.5f, 18), new Vector3(20, 5, 36));
            Probe(new Vector3(0, 2.5f, 18), new Vector3(20, 5, 36));
            Probe(new Vector3(20, 2.5f, 12), new Vector3(20, 5, 24));
            Probe(new Vector3(20, 2.5f, 30), new Vector3(20, 5, 12));
        }

        private static void Mist()
        {
            void Cloud(string name, Vector3 c, Vector3 size, float alpha, int max)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_lights);
                go.transform.position = c;
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = 20f; main.loop = true; main.prewarm = true;
                main.startLifetime = 22f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startColor = new Color(0.78f, 0.84f, 0.88f, alpha);
                main.maxParticles = max;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = max / 22f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
                var col = ps.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                          new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
                col.color = g;
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = MaterialFactory.Mist;
                r.maxParticleSize = 3f;
                r.sortMode = ParticleSystemSortMode.Distance;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            Cloud("Mist_Yard", new Vector3(-20, 0.8f, -4), new Vector3(20, 1.2f, 8), 0.16f, 30);
            Cloud("Mist_Estate", new Vector3(-10, 1.5f, -25), new Vector3(90, 3f, 25), 0.2f, 60);
            Cloud("Mist_A", new Vector3(-20, 0.6f, 18), new Vector3(19, 1.0f, 35), 0.08f, 40);
            Cloud("Mist_B", new Vector3(0, 0.6f, 18), new Vector3(19, 1.0f, 35), 0.06f, 35);
            Cloud("Mist_C", new Vector3(20, 0.8f, 12), new Vector3(19, 1.4f, 23), 0.1f, 35);
        }

        private static void PostProcessing()
        {
            const string path = "Assets/_Project/Settings/PP_MistProtocol.asset";
            System.IO.Directory.CreateDirectory("Assets/_Project/Settings");
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(0.95f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.7f);
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.34f); vig.smoothness.Override(0.45f);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.7f); ca.contrast.Override(14f); ca.saturation.Override(-6f);
            ca.colorFilter.Override(new Color(0.94f, 0.98f, 1f));
            var grain = profile.Add<FilmGrain>(true); grain.intensity.Override(0.18f);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(-8f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            AssetDatabase.SaveAssets();

            var vol = new GameObject("PostProcessVolume").AddComponent<Volume>();
            vol.transform.SetParent(_lights);
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        // ================================================================== gameplay
        private static Transform Waypoint(string name, Vector3 p)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_waypoints);
            t.position = p;
            return t;
        }

        private static void Zone(string name, Vector3 c, Vector3 size, string line, bool control = false)
        {
            var go = new GameObject("Zone_" + name);
            go.transform.SetParent(_gameplay);
            go.transform.position = c;
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;
            var z = go.AddComponent<ZoneTrigger>();
            z.zoneName = name;
            z.overseerLine = line;
            z.isControlRoom = control;
        }

        private static void Gameplay(Dictionary<string, PhysicsDoor> doors)
        {
            // ---- systems
            var sys = new GameObject("Systems").transform;
            sys.SetParent(_gameplay);
            var grid = new GameObject("NavGrid").AddComponent<NavGrid>();
            grid.transform.SetParent(sys);
            grid.Configure(new Vector3(-32f, 0f, -10f), 128, 96, 0.5f);
            EditorUtility.SetDirty(grid);
            new GameObject("SoundSystem").AddComponent<SoundSystem>().transform.SetParent(sys);
            var vfx = new GameObject("VfxLibrary").AddComponent<VfxLibrary>();
            vfx.transform.SetParent(sys);
            vfx.particleMaterial = MaterialFactory.Particles;
            vfx.tracerMaterial = MaterialFactory.Tracer;
            var director = new GameObject("OVERSEER_Director").AddComponent<OverseerDirector>();
            director.transform.SetParent(sys);
            director.Configure(PrefabFactory.Drone.GetComponent<AgentBase>(), PrefabFactory.Sentinel.GetComponent<AgentBase>(),
                PrefabFactory.Saboteur.GetComponent<AgentBase>(), PrefabFactory.Flask);
            EditorUtility.SetDirty(director);
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.transform.SetParent(sys);
            var ui = new GameObject("UI");
            ui.transform.SetParent(sys);
            ui.AddComponent<HUD>();
            ui.AddComponent<MistProtocol.UI.DebugOverlay>();

            // ---- player
            var player = Place(PrefabFactory.Player, new Vector3(-20f, 0.05f, -5.5f), 0f, _gameplay);
            var cam = player.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam);

            // Opening vehicle: starts beyond the broken south gate and drives up to the yard.
            var jeep = PrefabFactory.Model("GlenmistJeep", _gameplay, MaterialFactory.Glow);
            jeep.name = "GlenmistJeep";
            jeep.transform.SetPositionAndRotation(new Vector3(-20f, 0.02f, -25f), Quaternion.identity);
            var wheels = new System.Collections.Generic.List<Transform>();
            foreach (var name in new[] { "Jeep_Wheel_FL", "Jeep_Wheel_FR", "Jeep_Wheel_RL", "Jeep_Wheel_RR" })
            {
                var wheel = jeep.transform.FindDeep(name);
                if (wheel != null) wheels.Add(wheel);
            }
            jeep.AddComponent<VehicleArrival>().Configure(new Vector3(-20f, 0.02f, -8.5f), wheels.ToArray());
            foreach (var x in new[] { -0.56f, 0.56f })
            {
                var headlamp = new GameObject("JeepHeadlamp").AddComponent<Light>();
                headlamp.transform.SetParent(jeep.transform, false);
                headlamp.transform.localPosition = new Vector3(x, 0.82f, 2.05f);
                headlamp.type = LightType.Spot;
                headlamp.color = new Color(1f, 0.82f, 0.5f);
                headlamp.intensity = 7f;
                headlamp.range = 26f;
                headlamp.spotAngle = 38f;
                headlamp.shadows = LightShadows.None;
            }

            // ---- objectives
            var keycard = Place(PrefabFactory.Keycard, new Vector3(-27.0f, 0.9f, 33.4f), 20f, _gameplay);
            var reader = Block("KeycardReader", new Vector3(9.72f, 1.3f, 7.7f), new Vector3(0.12f, 0.35f, 0.25f), MaterialFactory.DarkMetal, 1f, _gameplay, true, false);
            reader.GetComponent<BoxCollider>().size = new Vector3(0.6f, 0.8f, 0.6f);
            var lamp = Block("ReaderLamp", new Vector3(9.64f, 1.42f, 7.7f), new Vector3(0.04f, 0.06f, 0.12f), MaterialFactory.LampOrange, 1f, reader.transform, false, false);
            var kr = reader.AddComponent<KeycardReader>();
            kr.Bind(doors["BC"], lamp.GetComponent<Renderer>());
            EditorUtility.SetDirty(kr);
            var killSwitch = GameObject.Find("KillSwitchConsole").transform;
            gm.SetObjectiveTargets(new[] { keycard.transform, reader.transform, doors["CD"].transform, killSwitch });
            EditorUtility.SetDirty(gm);

            // ---- zones
            Zone("THE FRONT GATE", new Vector3(-20, 1.5f, -4), new Vector3(20, 3, 8), "");
            Zone("WITHERING LOFT", new Vector3(-20, 1.5f, 18), new Vector3(19.5f, 3, 35.5f),
                "OVERSEER: The withering loft. Your father's desk is exactly where he left it. So is he.");
            Zone("ROLLING ROOM", new Vector3(0, 1.5f, 18), new Vector3(19.5f, 3, 35.5f),
                "OVERSEER: Rolling Room online. Sentinel squad, the engineer is armed.");
            Zone("DRYING CHAMBER", new Vector3(20, 1.5f, 12), new Vector3(19.5f, 3, 23.5f),
                "OVERSEER: Careful. The dryers run on pressurised gas. One spark...");
            Zone("CONTROL ROOM", new Vector3(20, 1.5f, 30), new Vector3(19.5f, 3, 11.5f),
                "OVERSEER: You should not be in here.", true);

            // ---- physics props
            foreach (var p in new[] { new Vector3(-12.2f, 0, 15.5f), new Vector3(-12.2f, 0, 20.5f), new Vector3(-20f, 0, 9.6f), new Vector3(-20f, 0, 23.5f) })
                Place(PrefabFactory.Barricade, p, Random.Range(-10f, 10f));
            foreach (var p in new[] { new Vector3(-8f, 0, 18.5f), new Vector3(7.8f, 0, 4f), new Vector3(7.8f, 0, 9f), new Vector3(0f, 0, 7.5f) })
                Place(PrefabFactory.Barricade, p, Random.Range(-10f, 10f));
            foreach (var p in new[] { new Vector3(12.5f, 0, 21f), new Vector3(27.5f, 0, 6f), new Vector3(20f, 0, 26.5f) })
                Place(PrefabFactory.Barricade, p, Random.Range(-10f, 10f));
            foreach (var p in new[] { new Vector3(-22f, 0, 2.5f), new Vector3(-18f, 0, 16.4f), new Vector3(-26f, 0, 30f), new Vector3(-13f, 0, 33f),
                                      new Vector3(-2f, 0, 3f), new Vector3(3f, 0, 30.5f), new Vector3(-7.5f, 0, 12f), new Vector3(14f, 0, 2f), new Vector3(26f, 0, 22f) })
                Place(PrefabFactory.SmallCrate, p + Vector3.up * 0.02f, Random.Range(0f, 90f));
            foreach (var p in new[] { new Vector3(-28.5f, 0, 16f), new Vector3(-11f, 0, 8f), new Vector3(-3f, 0, 21f), new Vector3(8.5f, 0, 28f),
                                      new Vector3(13f, 0, 9f), new Vector3(13f, 0, 10f), new Vector3(20f, 0, 9.5f), new Vector3(27.8f, 0, 13f),
                                      new Vector3(27.8f, 0, 14f), new Vector3(20.5f, 0, 18.5f) })
                Place(PrefabFactory.Canister, p + Vector3.up * 0.02f, 0f);
            foreach (var p in new[] { new Vector3(-28f, 0.8f, 8f), new Vector3(8.5f, 0.8f, 33f), new Vector3(28f, 0.8f, 2f), new Vector3(12f, 0.8f, 18f) })
                Place(PrefabFactory.Flask, p, 0f);

            // ---- spawn vents (face into the room)
            void Vent(string name, Vector3 p, float yaw) { Place(PrefabFactory.Vent, p, yaw, _gameplay).name = "Vent_" + name; }
            Vent("A_W", new Vector3(-29.0f, 0, 10f), 90f);
            Vent("A_N", new Vector3(-17f, 0, 35.0f), 180f);
            Vent("B_N", new Vector3(-3f, 0, 35.0f), 180f);
            Vent("B_S", new Vector3(4f, 0, 1.0f), 0f);
            Vent("B_E", new Vector3(9.0f, 0, 32f), -90f);
            Vent("C_E", new Vector3(29.0f, 0, 3f), -90f);
            Vent("C_E2", new Vector3(29.0f, 0, 20f), -90f);
            Vent("C_S", new Vector3(20f, 0, 1.0f), 0f);
            Vent("D_W", new Vector3(11.0f, 0, 31f), 90f);
            Vent("D_E", new Vector3(29.0f, 0, 31f), -90f);

            // ---- initial agents
            Transform[] Patrol(string n, params Vector3[] pts)
            {
                var list = new List<Transform>();
                for (int i = 0; i < pts.Length; i++) list.Add(Waypoint($"{n}_{i}", pts[i]));
                return list.ToArray();
            }
            void Sentinel(Vector3 p, float yaw, Transform[] patrol)
            {
                var s = Place(PrefabFactory.Sentinel, p, yaw, _gameplay).GetComponent<SentinelBrain>();
                PrefabFactory.SetArray(s, "patrolPoints", patrol);
            }
            Sentinel(new Vector3(-20f, 0, 20f), 0f, Patrol("PatrolA", new Vector3(-20, 0, 4), new Vector3(-20, 0, 31), new Vector3(-28, 0, 24), new Vector3(-12, 0, 24)));
            Sentinel(new Vector3(-2f, 0, 12f), 90f, Patrol("PatrolB1", new Vector3(-2, 0, 3), new Vector3(-2, 0, 31), new Vector3(8, 0, 31)));
            Sentinel(new Vector3(2f, 0, 22f), -90f, Patrol("PatrolB2", new Vector3(8, 0, 20), new Vector3(2, 0, 4), new Vector3(-8, 0, 28)));
            Sentinel(new Vector3(20f, 0, 6f), 0f, Patrol("PatrolC", new Vector3(20, 0, 3), new Vector3(20, 0, 22), new Vector3(28, 0, 22), new Vector3(12, 0, 3)));
            Place(PrefabFactory.Drone, new Vector3(-20f, 2.4f, 30f), 180f, _gameplay);
            Place(PrefabFactory.Drone, new Vector3(20f, 2.4f, 20f), 180f, _gameplay);
            Place(PrefabFactory.Saboteur, new Vector3(-6f, 0, 32f), 180f, _gameplay);
            Place(PrefabFactory.Saboteur, new Vector3(26f, 0, 9f), -90f, _gameplay);
        }
    }
}
