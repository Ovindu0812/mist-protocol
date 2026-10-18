using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MistProtocol.AI.Agents;
using MistProtocol.AI.Perception;
using MistProtocol.Core;
using MistProtocol.Interaction;
using MistProtocol.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MistProtocol.EditorTools
{
    /// <summary>
    /// Headless play-mode smoke test: plays through the whole story with a scripted
    /// "player", logs every agent decision, counts runtime exceptions and saves
    /// screenshots. Run:
    ///   Unity -batchmode -projectPath . -executeMethod MistProtocol.EditorTools.SmokeTest.Run
    /// Results: Logs/smoke/report.txt + Logs/smoke/*.png
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeTest
    {
        private const string Key = "MistSmokeRunning";
        private static readonly string OutDir = Path.GetFullPath("Logs/smoke");
        private static double _t0;
        private static int _step;
        private static int _errors;
        private static readonly List<string> Report = new();
        private static float _nextLog;

        static SmokeTest()
        {
            if (!SessionState.GetBool(Key, false)) return;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        [MenuItem("Mist Protocol/Run Smoke Test")]
        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var f in Directory.GetFiles(OutDir)) File.Delete(f);
            EditorSceneManager.OpenScene(MistBuild.ScenePath);
            SessionState.SetBool(Key, true);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
            _step = 0;
            EditorApplication.isPlaying = true;
        }

        private static void OnLog(string msg, string stack, LogType type)
        {
            if (stack.Contains("UnityEditor.Search")) return; // editor search indexer noise, not game code
            if (type == LogType.Exception || type == LogType.Error)
            {
                _errors++;
                if (_errors < 30) Report.Add($"[{type}] {msg}\n{stack}");
            }
        }

        private static void Log(string s)
        {
            Report.Add($"[{Time.time:0.0}s] {s}");
            Debug.Log("[Smoke] " + s);
        }

        // game time, not wall time: batchmode doesn't advance the player loop on its own,
        // so the test pauses play mode and steps it one fixed 1/30 s frame per editor tick
        private static float T => Time.time - (float)_t0;

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (_step == 0)
            {
                _t0 = Time.time;
                _step = 1;
                Time.captureDeltaTime = 1f / 30f;
                EditorApplication.isPaused = true;
                return;
            }
            var pl = Player;
            if (pl != null && GameManager.Instance.State == GameManager.Flow.Playing) pl.Health.Heal(1000f); // scripted player is invulnerable
            try { Script(); }
            catch (Exception e) { Log("SMOKE SCRIPT EXCEPTION " + e); Finish(); return; }
            if (_step == 1) EditorApplication.Step();
        }

        private static PlayerController Player => UnityEngine.Object.FindAnyObjectByType<PlayerController>();

        private static void Teleport(Vector3 p, float yaw, float pitch = 0f)
        {
            var pc = Player;
            var cc = pc.GetComponent<CharacterController>();
            cc.enabled = false;
            pc.transform.position = p;
            cc.enabled = true;
            typeof(PlayerController).GetField("_yaw", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pc, yaw);
            typeof(PlayerController).GetField("_pitch", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pc, pitch);
        }

        private static void Shot(string name, Camera cam = null)
        {
            cam ??= Camera.main;
            var rt = new RenderTexture(1280, 720, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = prev;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            Log("screenshot " + name);
        }

        private static void ShotFrom(string name, Vector3 pos, Vector3 lookAt)
        {
            var go = new GameObject("SmokeCam");
            var cam = go.AddComponent<Camera>();
            var main = Camera.main;
            cam.CopyFrom(main);
            go.transform.position = pos;
            go.transform.LookAt(lookAt);
            cam.fieldOfView = 70f;
            Shot(name, cam);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void LogAgents()
        {
            foreach (var a in AgentBase.Active)
                Log($"  {a.Kind,-11} {a.name,-24} pos {a.transform.position.x,6:0.0},{a.transform.position.z,6:0.0}  {a.DebugState}  | {a.DebugDetail}");
        }

        private static bool At(float t)
        {
            if (T < t || _done.Contains(t)) return false;
            _done.Add(t);
            return true;
        }
        private static readonly HashSet<float> _done = new();

        private static void Script()
        {
            var gm = GameManager.Instance;
            if (At(1f))
            {
                Log($"scene started. agents={AgentBase.Active.Count}");
                var g = AI.Navigation.NavGrid.Instance;
                int walk = 0; for (int i = 0; i < g.Count; i++) if (g.IsWalkable(i)) walk++;
                var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
                bool hit = UnityEngine.AI.NavMesh.SamplePosition(new Vector3(-20, 0.1f, 18), out var nh, 2f, UnityEngine.AI.NavMesh.AllAreas);
                var path = new List<Vector3>();
                bool ok = AI.Navigation.AStarPathfinder.FindPath(g, new Vector3(-20, 0, 20), new Vector3(0, 0, 12), path);
                Log($"NAV walkable cells {walk}/{g.Count}, navmesh tris {tri.indices.Length / 3}, sample hit {hit} y={nh.position.y:0.00}, A* loft->rolling ok={ok} len={path.Count} expanded={AI.Navigation.AStarPathfinder.LastExpanded}");
                Shot("00_intro_panel");
                typeof(GameManager).GetMethod("Begin", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, null);
            }
            if (At(3f)) { Shot("01_yard_spawn"); Teleport(new Vector3(-20, 0.05f, -3), 0f, -5f); }
            if (At(5f)) Shot("02_front_gate");
            if (At(6f)) { Teleport(new Vector3(-20, 0.05f, 2f), 0f, 0f); }
            if (At(9f)) { Shot("03_withering_loft"); LogAgents(); }
            if (At(10f))
            {
                Log("EVENT: gunshot in loft (player)");
                SoundSystem.Emit(Player.transform.position, 34f, SoundType.Gunshot, true);
            }
            if (At(14f)) { LogAgents(); Shot("04_loft_after_gunshot"); }
            if (At(16f))
            {
                Log("EVENT: throw crate impact far away to lure");
                SoundSystem.Emit(new Vector3(-27, 0, 8), 20f, SoundType.Impact, true);
            }
            if (At(20f)) LogAgents();
            if (At(21f))
            {
                // keycard
                var kc = UnityEngine.Object.FindAnyObjectByType<KeycardPickup>();
                Teleport(new Vector3(-26.5f, 0.05f, 31.6f), -20f, 25f);
                Shot("05_appa_desk");
                kc.Interact();
                Log($"keycard collected: {gm.HasKeycard}, stage {gm.Stage}");
            }
            if (At(23f))
            {
                // push a barricade with physics and see the saboteur react
                var b = UnityEngine.Object.FindAnyObjectByType<Barricade>();
                b.Body.AddForce(new Vector3(1, 0, 0.5f) * 1800f, ForceMode.Impulse);
                Log($"EVENT: barricade {b.name} shoved");
            }
            if (At(25f))
            {
                var door = GameObject.Find("Door_Loft_Rolling").GetComponent<PhysicsDoor>();
                Teleport(new Vector3(-11.5f, 0.05f, 18f), 90f);
                door.Interact();
                Log("door AB opened");
            }
            if (At(27f)) { Shot("06_door_open"); }
            if (At(28f)) { Teleport(new Vector3(-7f, 0.05f, 18f), 90f, 0f); }
            if (At(31f)) { Shot("07_rolling_room"); LogAgents(); }
            if (At(32f))
            {
                Log("EVENT: player gunfire in rolling room");
                for (int i = 0; i < 3; i++) SoundSystem.Emit(Player.transform.position, 34f, SoundType.Gunshot, true);
            }
            if (At(38f)) { LogAgents(); Shot("08_rolling_combat"); Log($"player health {Player.Health.Current:0}"); }
            if (At(39f))
            {
                // damage test on a sentinel
                foreach (var a in AgentBase.Active)
                    if (a.Kind == AgentKind.Sentinel) { a.HealthComponent.TakeDamage(500f, Player.EyePosition); Log("EVENT: killed " + a.name); break; }
            }
            if (At(41f))
            {
                Player.Health.Heal(100f);
                var reader = UnityEngine.Object.FindAnyObjectByType<KeycardReader>();
                Teleport(new Vector3(8.5f, 0.05f, 7.7f), 90f);
                reader.Interact();
                Log($"gate unlocked, stage {gm.Stage}");
            }
            if (At(43f)) Shot("09_gate_open");
            if (At(44f)) { Teleport(new Vector3(12f, 0.05f, 4f), 45f); }
            if (At(47f)) { Shot("10_drying_chamber"); LogAgents(); }
            if (At(48f))
            {
                var c = UnityEngine.Object.FindAnyObjectByType<GasCanister>();
                Log("EVENT: explode canister " + c.transform.position);
                c.Explode();
            }
            if (At(50f)) { Shot("11_after_explosion"); LogAgents(); }
            if (At(52f))
            {
                Player.Health.Heal(100f);
                Teleport(new Vector3(20f, 0.05f, 26.5f), 0f, 10f);
            }
            if (At(54f)) { Shot("12_control_room"); Log($"stage {gm.Stage} (3 = kill switch)"); }
            if (At(55f)) { Teleport(new Vector3(20f, 0.05f, 31f), 0f, 20f); }
            if (T > 56f && T < 66f && gm.Stage == 3)
            {
                Player.Health.Heal(100f);
                UnityEngine.Object.FindAnyObjectByType<KillSwitch>().Hold(Time.deltaTime);
            }
            if (At(60f)) { LogAgents(); Shot("13_final_wave"); }
            if (T > 56f && Time.time > _nextLog && T < 70f) { _nextLog = Time.time + 2f; Log($"director: {AI.Director.OverseerDirector.Instance.CurrentPhase} {AI.Director.OverseerDirector.Instance.LastDecision}"); }
            if (At(70f))
            {
                Log($"FINAL state={gm.State} stage={gm.Stage} kills={gm.Kills}");
                Shot("14_end_screen");
                ShotFrom("15_overview_A", new Vector3(-29, 4.4f, 1), new Vector3(-16, 0, 20));
                ShotFrom("16_overview_B", new Vector3(9, 4.4f, 1), new Vector3(-3, 0, 20));
                ShotFrom("17_overview_C", new Vector3(29, 4.4f, 1), new Vector3(17, 0, 14));
                ShotFrom("18_exterior", new Vector3(-28, 3f, -12), new Vector3(-15, 2, 0));
                Finish();
            }
        }

        private static void Finish()
        {
            Report.Insert(0, $"SMOKE TEST errors/exceptions: {_errors}");
            File.WriteAllLines(Path.Combine(OutDir, "report.txt"), Report);
            SessionState.SetBool(Key, false);
            _step = 2;
            Time.captureDeltaTime = 0f;
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(_errors == 0 ? 0 : 3);
        }
    }
}
