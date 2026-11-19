using System.IO;
using MistProtocol.AI.Agents;
using MistProtocol.AI.Director;
using MistProtocol.AI.HunterDrone;
using MistProtocol.AI.Perception;
using MistProtocol.AI.Saboteur;
using MistProtocol.AI.Sentinel;
using MistProtocol.Animation;
using MistProtocol.Core;
using MistProtocol.Interaction;
using MistProtocol.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MistProtocol.EditorTools
{
    /// <summary>Assembles gameplay prefabs from the Blender FBX models + runtime components.</summary>
    public static class PrefabFactory
    {
        public const string Dir = "Assets/_Project/Prefabs";
        private const string Models = "Assets/_Project/Art/Models";

        public static GameObject Player, Drone, Sentinel, Saboteur, Barricade, SmallCrate, Canister, Door, Flask, Keycard, Vent,
            AppaBoot, AppaPhone, AppaBadge, AppaMap, TitanBoss;

        public static void CreateAll()
        {
            Directory.CreateDirectory(Dir);
            Player = Save(BuildPlayer(), "Player");
            Drone = Save(BuildDrone(), "Agent_HunterDrone");
            Sentinel = Save(BuildSentinel(), "Agent_Sentinel");
            Saboteur = Save(BuildSaboteur(), "Agent_Saboteur");
            Barricade = Save(BuildBarricade(), "Barricade_TeaCrate");
            SmallCrate = Save(BuildSmallCrate(), "Throwable_SmallCrate");
            Canister = Save(BuildCanister(), "GasCanister");
            Door = Save(BuildDoor(), "PhysicsDoor");
            Flask = Save(BuildFlask(), "TeaFlask_Health");
            Keycard = Save(BuildKeycard(), "Keycard");
            Vent = Save(BuildVent(), "SpawnVent");
            AppaBoot = Save(BuildAppaBoot(), "Clue_AppaBoot");
            AppaPhone = Save(BuildAppaPhone(), "Clue_AppaPhone");
            AppaBadge = Save(BuildAppaBadge(), "Clue_AppaBadge");
            AppaMap = Save(BuildAppaMap(), "Clue_FactoryMap");
            TitanBoss = Save(BuildTitanBoss(), "Agent_TitanBoss");
        }

        // ------------------------------------------------------------------ helpers
        public static void Set(Object o, string field, object value)
        {
            var so = new SerializedObject(o);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[MistBuild] {o.GetType().Name}.{field} not found"); return; }
            switch (value)
            {
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                case string s: p.stringValue = s; break;
                case Object obj: p.objectReferenceValue = obj; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object o, string field, Object[] values)
        {
            var so = new SerializedObject(o);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Save(GameObject go, string name)
        {
            go.name = name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject Model(string fbx, Transform parent, Material glowFor = null, params string[] glowParts)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{fbx}.fbx");
            if (asset == null) { Debug.LogError($"[MistBuild] Missing model {fbx}"); return new GameObject(fbx); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = fbx;
            if (parent != null) go.transform.SetParent(parent, false);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                bool glow = System.Array.IndexOf(glowParts, r.name) >= 0;
                r.sharedMaterial = glow && glowFor != null ? glowFor : MaterialFactory.Palette;
            }
            return go;
        }

        private static void Ignore(GameObject go) => go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        private static Renderer[] Renderers(GameObject root, params string[] names)
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var n in names)
            {
                var t = root.transform.FindDeep(n);
                if (t != null && t.TryGetComponent<Renderer>(out var r)) list.Add(r);
            }
            return list.ToArray();
        }

        private static Transform Child(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private static void KinematicBody(GameObject go)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // ------------------------------------------------------------------ player
        private static GameObject BuildPlayer()
        {
            var root = new GameObject("Player") { tag = "Player" };
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.4f; cc.slopeLimit = 45f; cc.skinWidth = 0.04f;
            root.AddComponent<Health>();
            var pc = root.AddComponent<PlayerController>();
            Ignore(root);

            var pivot = Child(root.transform, "CameraPivot", new Vector3(0, 1.65f, 0));
            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 220f;
            cam.fieldOfView = 72f;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var lamp = new GameObject("Headlamp").AddComponent<Light>();
            lamp.transform.SetParent(camGo.transform, false);
            lamp.transform.localPosition = new Vector3(-0.12f, 0.12f, 0.05f);
            lamp.type = LightType.Spot;
            lamp.spotAngle = 60f; lamp.innerSpotAngle = 25f;
            lamp.range = 20f; lamp.intensity = 6f;
            lamp.color = new Color(1f, 0.95f, 0.85f);
            lamp.shadows = LightShadows.Soft;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;

            var rifle = Model("MistRifle", camGo.transform, MaterialFactory.Glow, "Rifle_Light");
            rifle.transform.localPosition = new Vector3(0.2f, -0.25f, 0.5f);
            foreach (var r in rifle.GetComponentsInChildren<MeshRenderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r.name == "Muzzle") r.enabled = false;
            }
            var muzzle = rifle.transform.FindDeep("Muzzle");

            var shotgun = Model("MistShotgun", camGo.transform, MaterialFactory.Glow, "Shotgun_Light");
            shotgun.transform.localPosition = new Vector3(0.2f, -0.26f, 0.48f);
            foreach (var r in shotgun.GetComponentsInChildren<MeshRenderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r.name == "Muzzle") r.enabled = false;
            }
            shotgun.SetActive(false);

            var revolver = Model("MistRevolver", camGo.transform, MaterialFactory.Glow, "Revolver_Light");
            revolver.transform.localPosition = new Vector3(0.18f, -0.22f, 0.42f);
            foreach (var r in revolver.GetComponentsInChildren<MeshRenderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r.name == "Muzzle") r.enabled = false;
            }
            revolver.SetActive(false);

            var smg = Model("MistSMG", camGo.transform, MaterialFactory.Glow, "SMG_PowerCell");
            smg.transform.localPosition = new Vector3(0.2f, -0.25f, 0.46f);
            foreach (var r in smg.GetComponentsInChildren<MeshRenderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r.name == "Muzzle") r.enabled = false;
            }
            smg.SetActive(false);

            var weapon = camGo.AddComponent<PlayerWeapon>();
            Set(weapon, "viewModel", rifle.transform);
            Set(weapon, "muzzle", muzzle);
            camGo.AddComponent<PlayerInteractor>();
            Set(pc, "cameraPivot", pivot);
            return root;
        }

        // ------------------------------------------------------------------ agents
        private static void AgentCore(GameObject root, float health, float speed, float accel, float hover)
        {
            var h = root.AddComponent<Health>();
            Set(h, "maxHealth", health);
            var motor = root.AddComponent<AgentMotor>();
            Set(motor, "maxSpeed", speed);
            Set(motor, "acceleration", accel);
            Set(motor, "hoverHeight", hover);
            root.AddComponent<VisionSensor>();
            KinematicBody(root);
            Ignore(root);
        }

        private static GameObject BuildDrone()
        {
            var root = new GameObject("Agent_Drone");
            var model = Model("HunterDrone", root.transform, MaterialFactory.Glow, "Eye");
            AgentCore(root, 55f, 5.5f, 10f, 2.4f);
            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.45f;
            var anim = root.AddComponent<DroneAnimator>();
            SetArray(anim, "glowRenderers", Renderers(model, "Eye"));
            var gun = model.transform.FindDeep("Gun");
            var muzzle = Child(gun != null ? gun : model.transform, "Muzzle", new Vector3(0, 0, 0.25f));
            var w = root.AddComponent<EnemyWeapon>();
            Set(w, "muzzle", muzzle);
            Set(w, "damage", 5f);
            Set(w, "fireInterval", 0.14f);
            Set(w, "baseSpread", 3f);
            Set(w, "magazine", 16);
            root.AddComponent<HunterDroneBrain>();
            var vs = root.GetComponent<VisionSensor>();
            Set(vs, "viewDistance", 26f);
            return root;
        }

        private static GameObject BuildSentinel()
        {
            var root = new GameObject("Agent_Sentinel");
            var model = Model("SentinelBot", root.transform, MaterialFactory.Glow, "Visor");
            AgentCore(root, 110f, 4.2f, 9f, 0f);
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.85f; col.radius = 0.4f; col.center = new Vector3(0, 0.93f, 0);
            var anim = root.AddComponent<WalkerAnimator>();
            SetArray(anim, "glowRenderers", Renderers(model, "Visor"));
            var gun = model.transform.FindDeep("Gun");
            var muzzle = Child(gun != null ? gun : model.transform, "Muzzle", new Vector3(0, 0.02f, 0.64f));
            var w = root.AddComponent<EnemyWeapon>();
            Set(w, "muzzle", muzzle);
            Set(w, "damage", 6f);
            Set(w, "magazine", 18);
            root.AddComponent<SentinelBrain>();
            return root;
        }

        private static GameObject BuildSaboteur()
        {
            var root = new GameObject("Agent_Saboteur");
            var model = Model("SaboteurBot", root.transform, MaterialFactory.Glow, "Eye");
            AgentCore(root, 70f, 3.4f, 12f, 0f);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.75f, 0.6f, 0.85f); col.center = new Vector3(0, 0.5f, 0);
            var anim = root.AddComponent<SpiderAnimator>();
            SetArray(anim, "glowRenderers", Renderers(model, "Eye"));
            root.AddComponent<SaboteurBrain>();
            return root;
        }

        // ------------------------------------------------------------------ props
        private static GameObject BuildBarricade()
        {
            var root = new GameObject("Barricade");
            Model("TeaCrate", root.transform);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.84f, 0.8f, 0.84f); col.center = new Vector3(0, 0.4f, 0);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 60f; rb.linearDamping = 0.5f; rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            root.AddComponent<Barricade>();
            Ignore(root);
            return root;
        }

        private static GameObject BuildSmallCrate()
        {
            var root = new GameObject("SmallCrate");
            var m = Model("TeaCrate", root.transform);
            m.transform.localScale = Vector3.one * 0.5f;
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.42f, 0.4f, 0.42f); col.center = new Vector3(0, 0.2f, 0);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<Throwable>();
            Ignore(root);
            return root;
        }

        private static GameObject BuildCanister()
        {
            var root = new GameObject("GasCanister");
            Model("GasCanister", root.transform);
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.18f; col.height = 0.74f; col.center = new Vector3(0, 0.36f, 0);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var h = root.AddComponent<Health>();
            Set(h, "maxHealth", 20f);
            root.AddComponent<GasCanister>();
            Ignore(root);
            return root;
        }

        private static GameObject BuildDoor()
        {
            const float w = 1.5f, ht = 2.4f;
            var root = new GameObject("Door");
            var leaf = new GameObject("Leaf");
            leaf.transform.SetParent(root.transform, false);
            leaf.transform.localPosition = new Vector3(w * 0.5f + 0.03f, ht * 0.5f + 0.02f, 0);
            leaf.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Box(new Vector3(w, ht, 0.08f), 1.2f);
            leaf.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Wood;
            leaf.AddComponent<BoxCollider>();
            var band = new GameObject("Band");
            band.transform.SetParent(leaf.transform, false);
            band.transform.localPosition = new Vector3(0, -0.6f, 0);
            band.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Box(new Vector3(w, 0.18f, 0.1f), 0.5f);
            band.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Hazard;
            var handle = new GameObject("Handle");
            handle.transform.SetParent(leaf.transform, false);
            handle.transform.localPosition = new Vector3(w * 0.5f - 0.15f, 0f, 0);
            handle.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Box(new Vector3(0.05f, 0.2f, 0.16f), 1f);
            handle.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.DarkMetal;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 30f; rb.angularDamping = 1.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var hinge = root.AddComponent<HingeJoint>();
            hinge.axis = Vector3.up;
            hinge.anchor = Vector3.zero;
            root.AddComponent<PhysicsDoor>();
            Ignore(root);
            return root;
        }

        private static GameObject BuildFlask()
        {
            var root = new GameObject("TeaFlask");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Flask;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0, 0.2f, 0);
            cap.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
            cap.GetComponent<Renderer>().sharedMaterial = MaterialFactory.DarkMetal;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true; trigger.radius = 0.7f;
            var light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.type = LightType.Point; light.range = 3f; light.intensity = 1.5f; light.color = new Color(0.3f, 1f, 0.4f);
            light.shadows = LightShadows.None;
            root.AddComponent<HealthPickup>();
            Ignore(root);
            return root;
        }

        private static GameObject BuildKeycard()
        {
            var root = new GameObject("Keycard");
            var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.transform.SetParent(root.transform, false);
            card.transform.localScale = new Vector3(0.3f, 0.02f, 0.2f);
            card.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Keycard;
            var col = card.GetComponent<BoxCollider>();
            col.size = new Vector3(2.5f, 15f, 2.5f); // generous hitbox for the use-ray
            var light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.type = LightType.Point; light.range = 2.5f; light.intensity = 2f; light.color = new Color(0.3f, 0.9f, 1f);
            light.shadows = LightShadows.None;
            root.AddComponent<KeycardPickup>();
            Ignore(root);
            return root;
        }

        private static GameObject BuildVent()
        {
            var root = new GameObject("Vent");
            var grate = new GameObject("Grate");
            grate.transform.SetParent(root.transform, false);
            grate.transform.localPosition = new Vector3(0, 0.6f, -0.85f);
            grate.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Box(new Vector3(1.4f, 1.2f, 0.1f), 0.4f);
            grate.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.DarkMetal;
            var stripe = new GameObject("Stripe");
            stripe.transform.SetParent(root.transform, false);
            stripe.transform.localPosition = new Vector3(0, 1.3f, -0.85f);
            stripe.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Box(new Vector3(1.5f, 0.15f, 0.12f), 0.5f);
            stripe.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Hazard;
            root.AddComponent<SpawnPoint>();
            return root;
        }

        private static GameObject BuildAppaBoot()
        {
            var root = new GameObject("Clue_AppaBoot");
            var sole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sole.name = "Sole";
            sole.transform.SetParent(root.transform, false);
            sole.transform.localScale = new Vector3(0.18f, 0.05f, 0.36f);
            sole.GetComponent<Renderer>().sharedMaterial = MaterialFactory.DarkMetal;
            Object.DestroyImmediate(sole.GetComponent<Collider>());

            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = "Upper";
            upper.transform.SetParent(root.transform, false);
            upper.transform.localPosition = new Vector3(0f, 0.08f, -0.04f);
            upper.transform.localScale = new Vector3(0.16f, 0.14f, 0.22f);
            upper.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Wood;
            Object.DestroyImmediate(upper.GetComponent<Collider>());

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.9f, 0.9f, 0.9f);
            col.center = new Vector3(0f, 0.1f, 0f);

            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = Vector3.up * 0.25f;
            glow.type = LightType.Point;
            glow.range = 2.5f;
            glow.intensity = 1.8f;
            glow.color = new Color(1f, 0.75f, 0.3f);
            glow.shadows = LightShadows.None;

            var clue = root.AddComponent<FatherClue>();
            clue.Configure(FatherClueType.DroppedBoot);
            Ignore(root);
            return root;
        }

        private static GameObject BuildAppaPhone()
        {
            var root = new GameObject("Clue_AppaPhone");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.12f, 0.03f, 0.22f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.DarkMetal;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(root.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.02f, 0.02f);
            screen.transform.localScale = new Vector3(0.09f, 0.015f, 0.14f);
            screen.GetComponent<Renderer>().sharedMaterial = MaterialFactory.LampCyan;
            Object.DestroyImmediate(screen.GetComponent<Collider>());

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.9f, 0.9f, 0.9f);
            col.center = new Vector3(0f, 0.1f, 0f);

            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = Vector3.up * 0.25f;
            glow.type = LightType.Point;
            glow.range = 2.5f;
            glow.intensity = 2f;
            glow.color = new Color(0.3f, 0.9f, 1f);
            glow.shadows = LightShadows.None;

            var clue = root.AddComponent<FatherClue>();
            clue.Configure(FatherClueType.BrokenPhone);
            Ignore(root);
            return root;
        }

        private static GameObject BuildAppaBadge()
        {
            var root = new GameObject("Clue_AppaBadge");
            var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "BadgeCard";
            card.transform.SetParent(root.transform, false);
            card.transform.localScale = new Vector3(0.16f, 0.015f, 0.24f);
            card.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Keycard;
            Object.DestroyImmediate(card.GetComponent<Collider>());

            var clip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clip.name = "Clip";
            clip.transform.SetParent(root.transform, false);
            clip.transform.localPosition = new Vector3(0f, 0.02f, 0.12f);
            clip.transform.localScale = new Vector3(0.06f, 0.02f, 0.04f);
            clip.GetComponent<Renderer>().sharedMaterial = MaterialFactory.DarkMetal;
            Object.DestroyImmediate(clip.GetComponent<Collider>());

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.9f, 0.9f, 0.9f);
            col.center = new Vector3(0f, 0.1f, 0f);

            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = Vector3.up * 0.25f;
            glow.type = LightType.Point;
            glow.range = 2.5f;
            glow.intensity = 2f;
            glow.color = new Color(1f, 0.85f, 0.4f);
            glow.shadows = LightShadows.None;

            var clue = root.AddComponent<FatherClue>();
            clue.Configure(FatherClueType.SupervisorID);
            Ignore(root);
            return root;
        }

        private static GameObject BuildAppaMap()
        {
            var root = new GameObject("Clue_FactoryMap");
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "BlueprintDoc";
            sheet.transform.SetParent(root.transform, false);
            sheet.transform.localScale = new Vector3(0.48f, 0.02f, 0.34f);
            sheet.GetComponent<Renderer>().sharedMaterial = MaterialFactory.LampCyan;
            Object.DestroyImmediate(sheet.GetComponent<Collider>());

            var clip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clip.name = "Clip";
            clip.transform.SetParent(root.transform, false);
            clip.transform.localPosition = new Vector3(-0.2f, 0.025f, 0f);
            clip.transform.localScale = new Vector3(0.08f, 0.035f, 0.36f);
            clip.GetComponent<Renderer>().sharedMaterial = MaterialFactory.DarkMetal;
            Object.DestroyImmediate(clip.GetComponent<Collider>());

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.2f, 1.4f);
            col.center = new Vector3(0f, 0.2f, 0f);

            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = Vector3.up * 0.35f;
            glow.type = LightType.Point;
            glow.range = 4.5f;
            glow.intensity = 3.5f;
            glow.color = new Color(0.2f, 0.85f, 1f);
            glow.shadows = LightShadows.None;

            var clue = root.AddComponent<FatherClue>();
            clue.Configure(FatherClueType.FactoryMap);
            Ignore(root);
            return root;
        }

        private static GameObject BuildTitanBoss()
        {
            var root = new GameObject("Agent_TitanBoss");
            var model = Model("TitanBoss", root.transform, MaterialFactory.Glow, "Searchlight", "Core_Glow", "Visor");

            // Heavy Armored Health (1400 HP - Main Boss Juggernaut)
            var h = root.AddComponent<ArmoredBossHealth>();
            Set(h, "maxHealth", 1400f);

            var motor = root.AddComponent<AgentMotor>();
            Set(motor, "maxSpeed", 3.8f);
            Set(motor, "acceleration", 8f);
            Set(motor, "hoverHeight", 0f);

            root.AddComponent<VisionSensor>();
            KinematicBody(root);
            Ignore(root);

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 3.2f; col.radius = 0.85f; col.center = new Vector3(0, 1.6f, 0);

            var anim = root.AddComponent<WalkerAnimator>();
            Set(anim, "strideLength", 1.8f);
            SetArray(anim, "glowRenderers", Renderers(model, "Searchlight", "Core_Glow", "Visor"));

            // Searchlight Spot Light
            var lightGo = new GameObject("Searchlight_Spot");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.6f, 0.5f);
            var sl = lightGo.AddComponent<Light>();
            sl.type = LightType.Spot;
            sl.spotAngle = 60f;
            sl.innerSpotAngle = 25f;
            sl.range = 32f;
            sl.intensity = 6f;
            sl.color = new Color(1f, 0.15f, 0.1f);
            sl.shadows = LightShadows.Soft;

            var gun = model.transform.FindDeep("Gun");
            var muzzle = model.transform.FindDeep("Muzzle");
            if (muzzle == null) muzzle = Child(gun != null ? gun : model.transform, "Muzzle", new Vector3(0, 0.02f, 0.9f));
            var w = root.AddComponent<EnemyWeapon>();
            Set(w, "muzzle", muzzle);
            Set(w, "damage", 15f);
            Set(w, "fireInterval", 0.15f);
            Set(w, "magazine", 40);
            Set(w, "reloadTime", 2.8f);
            Set(w, "loudness", 38f);
            Set(w, "tracerColor", new Color(1f, 0.2f, 0.1f, 1f));

            var brain = root.AddComponent<TitanBossBrain>();
            Set(brain, "searchlight", sl);

            return root;
        }
    }
}
