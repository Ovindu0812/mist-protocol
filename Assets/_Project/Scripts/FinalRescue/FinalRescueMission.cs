using System.Collections;
using System.Collections.Generic;
using MistProtocol.Core;
using MistProtocol.Interaction;
using MistProtocol.Player;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Master Coordinator for "The Final Rescue" mission:
    /// 1. Titan Boss Reward (Master Access Key drop & objective: "Find the Secret Laboratory.")
    /// 2. Secret Laboratory (Entrance blast door, 4 switches, Energy Barrier)
    /// 3. Hidden Clue System (4 clues tracking "Clues Found: X/4")
    /// 4. Puzzle Mechanism (Blue -> Green -> Red with alarms, warning lights, reset)
    /// 5. Father Rescue (Dialogue: "You found me! But we must leave now. This facility is going to collapse!")
    /// 6. Emergency Escape (60s timer, alarms, red lights, Father following, extraction beacon)
    /// 7. Victory Screen ("MISSION COMPLETE", "Father Rescued. Facility Escaped.", Restart & Menu buttons)
    /// </summary>
    public class FinalRescueMission : MonoBehaviour
    {
        public static FinalRescueMission Instance { get; private set; }

        public bool HasMasterAccessKey { get; set; }
        public bool IsPuzzleLocked { get; set; }
        public bool IsEscaping { get; private set; }
        public float EscapeTimer { get; private set; } = 60f;
        public int CluesDiscoveredCount => _discoveredClues.Count;

        private readonly HashSet<LabClueType> _discoveredClues = new();

        [Header("Mission References")]
        [SerializeField] private SecretLaboratoryDoor labDoor;
        [SerializeField] private EnergyBarrier energyBarrier;
        [SerializeField] private FatherRescueNPC fatherNPC;
        [SerializeField] private EscapePoint escapePoint;
        [SerializeField] private Light[] emergencyAlarmLights;

        private bool _missionCompleted;
        private bool _missionFailed;
        private float _nextAlarmSoundTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnSceneLoad()
        {
            if (Instance == null && Object.FindAnyObjectByType<FinalRescueMission>() == null)
            {
                var missionGo = new GameObject("FinalRescue_Mission");
                missionGo.AddComponent<FinalRescueMission>();
            }
        }

        private void Awake()
        {
            Instance = this;
            EnsureUI();
            BuildSecretLaboratoryIfMissing();
        }

        private void Start()
        {
            EnsureEscapePoint();
        }

        private void EnsureUI()
        {
            if (FinalRescueUI.Instance == null && FindAnyObjectByType<FinalRescueUI>() == null)
            {
                var uiGo = new GameObject("FinalRescue_UI_Manager");
                uiGo.AddComponent<FinalRescueUI>();
            }
        }

        private void Update()
        {
            if (!IsEscaping || _missionCompleted || _missionFailed) return;

            EscapeTimer -= Time.deltaTime;

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.UpdateEmergencyTimer(EscapeTimer, 60f);
            }

            // Periodic emergency klaxon / siren audio
            if (Time.time > _nextAlarmSoundTime)
            {
                _nextAlarmSoundTime = Time.time + 3.2f;
                var player = FindAnyObjectByType<PlayerController>();
                Vector3 soundPos = player != null ? player.transform.position : transform.position;
                ProceduralAudio.PlayAt("alarm", soundPos, 0.75f, 0.95f);
            }

            // Timer run out -> Facility collapsed!
            if (EscapeTimer <= 0f)
            {
                EscapeTimer = 0f;
                OnEscapeTimeout();
            }
        }

        // =========================================================================
        // 1. TITAN BOSS REWARD & KEY COLLECTION
        // =========================================================================
        public void OnKeyCollected()
        {
            HasMasterAccessKey = true;

            // Direct objective to the Secret Laboratory
            string objective = "Find the Secret Laboratory.";
            GameEvents.Objective(objective);
            GameEvents.Overseer("SECURITY ALERT: Master Access Key acquired from Titan's core. Locating Secret Laboratory...");

            if (labDoor != null && GameManager.Instance != null)
            {
                GameManager.Instance.SetCustomObjective(labDoor.TerminalTransform, objective);
            }
        }

        // =========================================================================
        // 2. SECRET LABORATORY ENTRANCE
        // =========================================================================
        public void OnLaboratoryOpened()
        {
            string objective = "Investigate the Secret Laboratory and disable the prison barrier!";
            GameEvents.Objective(objective);

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.UpdateClueHud(_discoveredClues.Count, 4);
            }

            // Direct objective to the Energy Barrier / Father inside
            if (fatherNPC != null && GameManager.Instance != null)
            {
                GameManager.Instance.SetCustomObjective(fatherNPC.transform, objective);
            }
        }

        // =========================================================================
        // 3. HIDDEN CLUE SYSTEM
        // =========================================================================
        public void OnClueInspected(LabClueType type, string title, string message, bool isFirstRead)
        {
            if (isFirstRead)
            {
                _discoveredClues.Add(type);
            }

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowCluePopup(title, message, _discoveredClues.Count, 4);
            }
        }

        // =========================================================================
        // 4. PUZZLE SOLVED & BARRIER DISABLED
        // =========================================================================
        public void OnPuzzleSolved()
        {
            string objective = "RESCUE APPA: Approach and speak to your father!";
            GameEvents.Objective(objective);
            GameEvents.Overseer("LABORATORY SYSTEM: All safety relays disengaged. Prison energy barrier disabled.");

            if (fatherNPC != null && GameManager.Instance != null)
            {
                GameManager.Instance.SetCustomObjective(fatherNPC.transform, objective);
            }
        }

        // =========================================================================
        // 5 & 6. EMERGENCY ESCAPE SEQUENCE
        // =========================================================================
        public void StartEmergencyEscape()
        {
            if (IsEscaping) return;
            IsEscaping = true;
            EscapeTimer = 60f;

            // Audio & visual alarms
            ProceduralAudio.PlayAt("alarm", transform.position, 1.2f, 1f);
            ProceduralAudio.PlayAt("explosion", transform.position, 1f, 0.5f);

            ActivateEmergencyLights();
            UnlockAllCorridorDoors();

            string objective = "Escape the facility before it collapses!";
            GameEvents.Objective(objective);
            GameEvents.Overseer("CRITICAL WARNING: Structural integrity collapsing! Evacuate to the yard vehicle immediately!");

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.SetEmergencyTimerVisible(true);
            }

            // Activate visible extraction beacon at escape point
            if (escapePoint != null)
            {
                escapePoint.SetBeaconActive(true);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetCustomObjective(escapePoint.transform, objective);
                }
            }
        }

        private void ActivateEmergencyLights()
        {
            if (emergencyAlarmLights != null)
            {
                foreach (var l in emergencyAlarmLights)
                {
                    if (l != null)
                    {
                        l.enabled = true;
                        l.color = new Color(1f, 0.15f, 0.1f);
                        l.intensity = 4.5f;
                    }
                }
            }
        }

        private void UnlockAllCorridorDoors()
        {
            // Unlock and open all intermediate doors to ensure an unobstructed, adrenaline-fueled sprint
            foreach (var pd in FindObjectsByType<PhysicsDoor>(FindObjectsInactive.Exclude))
            {
                pd.SetLocked(false);
                pd.OpenAwayFrom(transform.position);
            }
        }

        public void OnEscapedSuccessfully()
        {
            if (_missionCompleted || _missionFailed) return;
            _missionCompleted = true;
            IsEscaping = false;

            ProceduralAudio.PlayAt("success", transform.position, 1.5f, 1f);
            ProceduralAudio.PlayAt("pickup", transform.position, 1.2f, 1.5f);

            var gm = GameManager.Instance;
            int kills = gm != null ? gm.Kills : 0;
            float timeElapsed = gm != null ? (Time.time - gm.StartTime) : (60f - EscapeTimer);

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowVictoryScreen(kills, timeElapsed, _discoveredClues.Count);
            }
        }

        private void OnEscapeTimeout()
        {
            if (_missionCompleted || _missionFailed) return;
            _missionFailed = true;
            IsEscaping = false;

            ProceduralAudio.PlayAt("explosion", transform.position, 1.5f, 0.3f);

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.InputEnabled = false;
                player.Health.TakeDamage(9999f, player.transform.position);
            }

            if (FinalRescueUI.Instance != null)
            {
                FinalRescueUI.Instance.ShowFailedScreen();
            }
        }

        // =========================================================================
        // PROCEDURAL LEVEL BUILDER FOR SECRET LABORATORY
        // =========================================================================
        public void BuildSecretLaboratoryIfMissing()
        {
            // If already built in scene, wire up references
            var existingDoor = GetComponentInChildren<SecretLaboratoryDoor>();
            if (existingDoor != null)
            {
                labDoor = existingDoor;
                energyBarrier = GetComponentInChildren<EnergyBarrier>();
                fatherNPC = GetComponentInChildren<FatherRescueNPC>();
                return;
            }

            // Create Laboratory at Sector E Vault (x=51.5f, y=0, z=12f)
            transform.position = new Vector3(51.5f, 0f, 12f);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var darkMetal = new Material(litShader) { color = new Color(0.1f, 0.12f, 0.14f) };
            darkMetal.SetFloat("_Metallic", 0.8f);
            darkMetal.SetFloat("_Smoothness", 0.5f);

            var highTechWall = new Material(litShader) { color = new Color(0.14f, 0.16f, 0.2f) };
            highTechWall.SetFloat("_Metallic", 0.6f);
            highTechWall.SetFloat("_Smoothness", 0.4f);

            var floorMat = new Material(litShader) { color = new Color(0.16f, 0.18f, 0.2f) };

            // 1. Secret Laboratory Entrance Blast Door
            var doorRoot = new GameObject("SecretLab_Entrance");
            doorRoot.transform.SetParent(transform, false);
            doorRoot.transform.localPosition = new Vector3(-2.2f, 0f, 0f);

            var slidingDoorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slidingDoorGo.name = "Lab_SlidingDoor";
            slidingDoorGo.transform.SetParent(doorRoot.transform, false);
            slidingDoorGo.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            slidingDoorGo.transform.localScale = new Vector3(0.2f, 3.5f, 2.4f);
            slidingDoorGo.GetComponent<Renderer>().material = darkMetal;

            var termGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            termGo.name = "Lab_Terminal";
            termGo.transform.SetParent(doorRoot.transform, false);
            termGo.transform.localPosition = new Vector3(-0.35f, 1.25f, -1.5f);
            termGo.transform.localScale = new Vector3(0.25f, 0.45f, 0.35f);
            var termMat = new Material(litShader) { color = new Color(0.08f, 0.1f, 0.12f) };
            termGo.GetComponent<Renderer>().material = termMat;

            var termLight = termGo.AddComponent<Light>();
            termLight.type = LightType.Point;
            termLight.range = 3.5f;
            termLight.intensity = 2.5f;
            termLight.color = new Color(1f, 0.2f, 0.15f); // Red locked indicator initially

            var termCol = termGo.GetComponent<BoxCollider>();
            termCol.size = new Vector3(2.5f, 2.5f, 2.5f);
            termCol.isTrigger = true;

            labDoor = doorRoot.AddComponent<SecretLaboratoryDoor>();
            labDoor.Bind(slidingDoorGo.transform, termGo.transform, termLight);

            // 2. Secret Laboratory Room Shell
            CreateWall("Wall_East", new Vector3(2.6f, 1.75f, 0f), new Vector3(0.25f, 3.5f, 7.5f), highTechWall);
            CreateWall("Wall_North", new Vector3(0f, 1.75f, 3.75f), new Vector3(5.4f, 3.5f, 0.25f), highTechWall);
            CreateWall("Wall_South", new Vector3(0f, 1.75f, -3.75f), new Vector3(5.4f, 3.5f, 0.25f), highTechWall);
            CreateWall("Floor", new Vector3(0.2f, 0.02f, 0f), new Vector3(5.2f, 0.05f, 7.5f), floorMat);
            CreateWall("Ceiling", new Vector3(0.2f, 3.5f, 0f), new Vector3(5.2f, 0.1f, 7.5f), darkMetal);

            // 3. Laboratory Atmospheric & Warning Lights
            var light1Go = new GameObject("LabLight_1");
            light1Go.transform.SetParent(transform, false);
            light1Go.transform.localPosition = new Vector3(0f, 3.2f, -1.8f);
            var l1 = light1Go.AddComponent<Light>();
            l1.type = LightType.Point;
            l1.range = 8f;
            l1.intensity = 2f;
            l1.color = new Color(0.6f, 0.8f, 1f);

            var light2Go = new GameObject("LabLight_2");
            light2Go.transform.SetParent(transform, false);
            light2Go.transform.localPosition = new Vector3(0f, 3.2f, 1.8f);
            var l2 = light2Go.AddComponent<Light>();
            l2.type = LightType.Point;
            l2.range = 8f;
            l2.intensity = 2f;
            l2.color = new Color(0.6f, 0.8f, 1f);

            emergencyAlarmLights = new[] { l1, l2 };

            // 4. Energy Barrier
            var barrierGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrierGo.name = "EnergyBarrier";
            barrierGo.transform.SetParent(transform, false);
            barrierGo.transform.localPosition = new Vector3(1.0f, 1.75f, 0f);
            barrierGo.transform.localScale = new Vector3(0.12f, 3.4f, 3.6f);

            var barrierMat = new Material(litShader);
            barrierMat.SetColor("_BaseColor", new Color(0.2f, 0.7f, 1f, 0.8f));
            barrierMat.EnableKeyword("_EMISSION");
            barrierMat.SetColor("_EmissionColor", new Color(0.15f, 0.65f, 1f) * 3f);
            barrierGo.GetComponent<Renderer>().material = barrierMat;

            var barrierLight = barrierGo.AddComponent<Light>();
            barrierLight.type = LightType.Point;
            barrierLight.range = 6f;
            barrierLight.intensity = 3.5f;
            barrierLight.color = new Color(0.2f, 0.7f, 1f);

            energyBarrier = barrierGo.AddComponent<EnergyBarrier>();
            energyBarrier.Configure(barrierGo.GetComponent<BoxCollider>(), barrierGo.GetComponent<Renderer>(), barrierLight);

            // 5. Father NPC inside Barrier
            var appaGo = new GameObject("NPC_Father_Appa");
            appaGo.transform.SetParent(transform, false);
            appaGo.transform.localPosition = new Vector3(1.9f, 0f, 0f);
            appaGo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Looking towards entrance

            AttachAppaModel(appaGo);

            var appaCol = appaGo.AddComponent<CapsuleCollider>();
            appaCol.height = 1.9f;
            appaCol.radius = 0.5f;
            appaCol.center = new Vector3(0f, 0.95f, 0f);
            appaCol.isTrigger = true;

            fatherNPC = appaGo.AddComponent<FatherRescueNPC>();
            fatherNPC.BindBarrier(energyBarrier);

            // 6. Four Control Switches: Blue, Yellow, Green, Red
            var blueSw = CreateSwitchStation("Switch_Blue", new Vector3(-1.8f, 0f, 2.5f), SwitchColor.Blue, darkMetal, litShader);
            var yellowSw = CreateSwitchStation("Switch_Yellow", new Vector3(-0.6f, 0f, 2.8f), SwitchColor.Yellow, darkMetal, litShader);
            var greenSw = CreateSwitchStation("Switch_Green", new Vector3(0.6f, 0f, 2.8f), SwitchColor.Green, darkMetal, litShader);
            var redSw = CreateSwitchStation("Switch_Red", new Vector3(1.8f, 0f, 2.5f), SwitchColor.Red, darkMetal, litShader);

            // 7. Puzzle Controller
            var puzzleGo = new GameObject("PuzzleController");
            puzzleGo.transform.SetParent(transform, false);
            var puzzle = puzzleGo.AddComponent<LaboratoryPuzzleController>();
            puzzle.Bind(blueSw, yellowSw, greenSw, redSw, energyBarrier, emergencyAlarmLights);

            // 8. Four Hidden Clues
            CreateClueStation("Clue1_Terminal", new Vector3(-1.6f, 0f, -2.4f), LabClueType.ComputerTerminal, darkMetal, litShader);
            CreateClueStation("Clue2_Document", new Vector3(-0.2f, 0f, -2.6f), LabClueType.HiddenDocument, darkMetal, litShader);
            CreateClueStation("Clue3_WallMessage", new Vector3(2.45f, 1.8f, -1.2f), LabClueType.WallMessage, darkMetal, litShader);
            CreateClueStation("Clue4_ResearchNotes", new Vector3(1.4f, 0f, -2.3f), LabClueType.ResearchNotes, darkMetal, litShader);
        }

        private void EnsureEscapePoint()
        {
            if (escapePoint != null) return;
            escapePoint = FindAnyObjectByType<EscapePoint>();
            if (escapePoint != null) return;

            var escGo = new GameObject("EscapePoint_Yard");
            escGo.transform.position = new Vector3(-20f, 0.1f, -4.5f);
            escapePoint = escGo.AddComponent<EscapePoint>();
        }

        private void CreateWall(string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material = mat;
        }

        private LaboratorySwitch CreateSwitchStation(string name, Vector3 pos, SwitchColor color, Material pedestalMat, Shader shader)
        {
            var stationGo = new GameObject(name);
            stationGo.transform.SetParent(transform, false);
            stationGo.transform.localPosition = pos;

            // Pedestal base
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Pedestal";
            baseGo.transform.SetParent(stationGo.transform, false);
            baseGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            baseGo.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            baseGo.GetComponent<Renderer>().material = pedestalMat;

            // Try loading 3D button box model
            GameObject buttonModelPrefab = Resources.Load<GameObject>("Models/PuzzleButtons");
#if UNITY_EDITOR
            if (buttonModelPrefab == null)
            {
                buttonModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/PuzzleButtons.fbx");
            }
#endif
            Renderer activeRend = null;
            Light lt = null;

            if (buttonModelPrefab != null)
            {
                // Instantiate the specific color box from the FBX hierarchy
                var fullModel = Instantiate(buttonModelPrefab, stationGo.transform);
                fullModel.name = $"Model_{color}";
                fullModel.transform.localPosition = new Vector3(0f, 0.96f, 0f);
                fullModel.transform.localScale = Vector3.one * 1.8f;
                fullModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // face player

                // Enable only the child object corresponding to this switch color
                string targetChildName = $"PuzzleBox_{color}";
                Transform matchedChild = null;
                foreach (Transform child in fullModel.transform)
                {
                    if (child.name.Equals(targetChildName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        child.gameObject.SetActive(true);
                        matchedChild = child;
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }

                if (matchedChild != null)
                {
                    activeRend = matchedChild.GetComponent<Renderer>();
                }
                else
                {
                    activeRend = fullModel.GetComponentInChildren<Renderer>();
                }

                var ltGo = new GameObject("StatusLight");
                ltGo.transform.SetParent(stationGo.transform, false);
                ltGo.transform.localPosition = new Vector3(0f, 1.15f, -0.2f);
                lt = ltGo.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 3.5f;
            }
            else
            {
                // Fallback procedural button
                var buttonGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                buttonGo.name = "ButtonMesh";
                buttonGo.transform.SetParent(stationGo.transform, false);
                buttonGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                buttonGo.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);

                var btnMat = new Material(shader);
                buttonGo.GetComponent<Renderer>().material = btnMat;
                activeRend = buttonGo.GetComponent<Renderer>();

                lt = buttonGo.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 3.5f;
            }

            var box = stationGo.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 1.8f, 1.2f);
            box.isTrigger = true;

            var sw = stationGo.AddComponent<LaboratorySwitch>();
            sw.Configure(color, activeRend, lt);
            return sw;
        }

        private void CreateClueStation(string name, Vector3 pos, LabClueType type, Material deskMat, Shader shader)
        {
            var clueGo = new GameObject(name);
            clueGo.transform.SetParent(transform, false);
            clueGo.transform.localPosition = pos;

            if (type == LabClueType.ComputerTerminal)
            {
                // Terminal stand & glowing monitor
                var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                desk.transform.SetParent(clueGo.transform, false);
                desk.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                desk.transform.localScale = new Vector3(0.9f, 0.9f, 0.55f);
                desk.GetComponent<Renderer>().material = deskMat;

                var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                screen.transform.SetParent(clueGo.transform, false);
                screen.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                screen.transform.localScale = new Vector3(0.6f, 0.45f, 0.1f);
                screen.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);

                var scrMat = new Material(shader) { color = new Color(0.1f, 0.5f, 0.85f) };
                scrMat.EnableKeyword("_EMISSION");
                scrMat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 0.95f) * 2.5f);
                screen.GetComponent<Renderer>().material = scrMat;

                var lt = screen.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 3.5f;
                lt.intensity = 2f;
                lt.color = new Color(0.2f, 0.7f, 1f);
            }
            else if (type == LabClueType.HiddenDocument)
            {
                // Crate table & paper clipboard
                var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crate.transform.SetParent(clueGo.transform, false);
                crate.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                crate.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                crate.GetComponent<Renderer>().material = deskMat;

                var doc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doc.transform.SetParent(clueGo.transform, false);
                doc.transform.localPosition = new Vector3(0f, 0.83f, 0f);
                doc.transform.localScale = new Vector3(0.4f, 0.04f, 0.55f);
                doc.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

                var docMat = new Material(shader) { color = new Color(0.9f, 0.85f, 0.65f) };
                docMat.EnableKeyword("_EMISSION");
                docMat.SetColor("_EmissionColor", new Color(0.9f, 0.8f, 0.5f) * 1.5f);
                doc.GetComponent<Renderer>().material = docMat;

                var lt = doc.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 3f;
                lt.intensity = 1.8f;
                lt.color = new Color(1f, 0.9f, 0.5f);
            }
            else if (type == LabClueType.WallMessage)
            {
                // Glowing inscribed plate on wall
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.transform.SetParent(clueGo.transform, false);
                plate.transform.localPosition = Vector3.zero;
                plate.transform.localScale = new Vector3(0.06f, 0.6f, 1.2f);

                var pMat = new Material(shader) { color = new Color(0.95f, 0.75f, 0.2f) };
                pMat.EnableKeyword("_EMISSION");
                pMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.25f) * 2.8f);
                plate.GetComponent<Renderer>().material = pMat;

                var lt = plate.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 4f;
                lt.intensity = 2.5f;
                lt.color = new Color(1f, 0.85f, 0.3f);
            }
            else if (type == LabClueType.ResearchNotes)
            {
                // Holo-pad / Chief Engineer's audio recorder station
                var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.transform.SetParent(clueGo.transform, false);
                table.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                table.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                table.GetComponent<Renderer>().material = deskMat;

                var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pad.transform.SetParent(clueGo.transform, false);
                pad.transform.localPosition = new Vector3(0f, 0.73f, 0f);
                pad.transform.localScale = new Vector3(0.45f, 0.05f, 0.32f);
                pad.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);

                var padMat = new Material(shader) { color = new Color(0.2f, 0.85f, 1f) };
                padMat.EnableKeyword("_EMISSION");
                padMat.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1f) * 2.5f);
                pad.GetComponent<Renderer>().material = padMat;

                var lt = pad.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 3.5f;
                lt.intensity = 2f;
                lt.color = new Color(0.2f, 0.85f, 1f);
            }

            var col = clueGo.AddComponent<BoxCollider>();
            col.size = new Vector3(1.6f, 2.0f, 1.6f);
            col.isTrigger = true;

            var clue = clueGo.AddComponent<SecretLaboratoryClue>();
            clue.Configure(type);
        }

        private static void AttachAppaModel(GameObject appaGo)
        {
            GameObject modelPrefab = Resources.Load<GameObject>("Appa");
#if UNITY_EDITOR
            if (modelPrefab == null)
            {
                modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Appa.fbx");
            }
#endif
            if (modelPrefab != null)
            {
                var modelInst = Instantiate(modelPrefab, appaGo.transform);
                modelInst.name = "Appa_Mesh";
                modelInst.transform.localPosition = Vector3.zero;
                modelInst.transform.localRotation = Quaternion.identity;
                modelInst.transform.localScale = Vector3.one;
                ApplyAppaMaterials(modelInst);

                // Add glowing supervisor badge
                var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                badge.name = "SupervisorBadge";
                badge.transform.SetParent(appaGo.transform, false);
                badge.transform.localPosition = new Vector3(-0.08f, 1.35f, 0.14f);
                badge.transform.localScale = new Vector3(0.06f, 0.08f, 0.02f);
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var badgeMat = new Material(litShader) { color = new Color(0.95f, 0.8f, 0.2f) };
                badgeMat.EnableKeyword("_EMISSION");
                badgeMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.25f) * 2.5f);
                badge.GetComponent<Renderer>().material = badgeMat;
                var badgeCol = badge.GetComponent<Collider>();
                if (badgeCol != null) Destroy(badgeCol);
            }
        }

        private static void ApplyAppaMaterials(GameObject root)
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var skinMat = new Material(litShader) { color = new Color(0.56f, 0.37f, 0.25f) };
            skinMat.SetFloat("_Smoothness", 0.35f);

            var creamMat = new Material(litShader) { color = new Color(0.88f, 0.82f, 0.70f) };
            creamMat.SetFloat("_Smoothness", 0.25f);

            var vestMat = new Material(litShader) { color = new Color(0.18f, 0.11f, 0.06f) };
            vestMat.SetFloat("_Smoothness", 0.2f);

            var trouserMat = new Material(litShader) { color = new Color(0.16f, 0.09f, 0.05f) };
            trouserMat.SetFloat("_Smoothness", 0.2f);

            var shoeMat = new Material(litShader) { color = new Color(0.10f, 0.05f, 0.03f) };
            shoeMat.SetFloat("_Smoothness", 0.55f);

            var hairMat = new Material(litShader) { color = new Color(0.25f, 0.25f, 0.26f) };
            hairMat.SetFloat("_Smoothness", 0.2f);

            var specMat = new Material(litShader) { color = new Color(0.12f, 0.12f, 0.14f) };
            specMat.SetFloat("_Metallic", 0.85f);
            specMat.SetFloat("_Smoothness", 0.75f);

            var goldMat = new Material(litShader) { color = new Color(0.92f, 0.75f, 0.25f) };
            goldMat.SetFloat("_Metallic", 0.9f);
            goldMat.SetFloat("_Smoothness", 0.85f);

            var eyeMat = new Material(litShader) { color = Color.white };
            eyeMat.SetFloat("_Smoothness", 0.8f);

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                string n = r.name.ToLowerInvariant();
                if (n.Contains("hair") || n.Contains("beard") || n.Contains("moustache")) r.material = hairMat;
                else if (n.Contains("spectacle") || n.Contains("glass")) r.material = specMat;
                else if (n.Contains("vest") || n.Contains("waistcoat")) r.material = vestMat;
                else if (n.Contains("pant") || n.Contains("trouser") || n.Contains("leg")) r.material = trouserMat;
                else if (n.Contains("shoe") || n.Contains("foot")) r.material = shoeMat;
                else if (n.Contains("shirt") || n.Contains("sleeve") || n.Contains("cuff") || n.Contains("collar")) r.material = creamMat;
                else if (n.Contains("head") || n.Contains("face") || n.Contains("neck") || n.Contains("hand") || n.Contains("arm")) r.material = skinMat;
                else if (n.Contains("watch") || n.Contains("buckle")) r.material = goldMat;
                else if (n.Contains("eye")) r.material = eyeMat;
            }
        }
    }
}
