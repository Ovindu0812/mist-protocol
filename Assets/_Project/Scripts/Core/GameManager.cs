using MistProtocol.AI.Director;
using MistProtocol.Interaction;
using MistProtocol.Player;
using MistProtocol.World;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MistProtocol.Core
{
    /// <summary>Story flow: intro -> keycard -> gate -> control room -> kill switch -> ending.</summary>
    public class GameManager : MonoBehaviour
    {
        public enum Flow { Intro, Playing, Won, Lost }

        public static GameManager Instance { get; private set; }

        [SerializeField] private Transform[] objectiveTargets = new Transform[4];

        public Flow State { get; private set; } = Flow.Intro;
        /// <summary>0 find keycard, 1 unlock gate, 2 reach control room, 3 kill switch, 4 done.</summary>
        public int Stage { get; private set; }
        public bool HasKeycard { get; private set; }
        public bool HasMasterCellKey { get; private set; }
        public bool IsAppaRescued { get; private set; }
        public bool CanUseKillSwitch => Stage == 3;
        public bool IsKillSwitchPulled { get; private set; }
        public bool IsTitanDefeated { get; private set; }
        public int Kills { get; private set; }
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }

        private Transform _customObjectiveTarget;

        public void SetCustomObjective(Transform target, string text = null)
        {
            _customObjectiveTarget = target;
            if (!string.IsNullOrEmpty(text))
            {
                GameEvents.Objective(text);
            }
        }

        public Vector3 CurrentObjectivePosition =>
            _customObjectiveTarget != null ? _customObjectiveTarget.position :
            (objectiveTargets != null && Stage < objectiveTargets.Length && objectiveTargets[Stage] != null
                ? objectiveTargets[Stage].position : transform.position);

        public static readonly string[] ObjectiveText =
        {
            "Find Appa's keycard in the Withering Loft",
            "Swipe the keycard at the Drying Chamber gate (Rolling Room, east wall)",
            "Reach the Control Room at the north end of the Drying Chamber",
            "Hold the kill switch to shut OVERSEER down",
            "OVERSEER is offline",
        };

        public const string IntroText =
            "GLENMIST TEA FACTORY  -  NUWARA ELIYA HILLS  -  02:14 AM\n\n" +
            "For 94 years Glenmist rolled the finest Ceylon tea in the hills. Last year the company installed OVERSEER, " +
            "an AI meant to run the factory without workers.\n\n" +
            "Tonight a storm hit. OVERSEER locked the gates and turned the maintenance robots into weapons. " +
            "Your father, the night supervisor, sent one last radio message:\n\n" +
            "    \"Nila... the kill switch is in the control room. My keycard is on my desk in the loft. Don't come alone...\"\n\n" +
            "You came alone.\n\n" +
            "WASD move  -  Mouse look  -  LMB shoot  -  R reload  -  Shift sprint  -  Ctrl/C crouch (quiet)\n" +
            "E use  -  F pick up / drop  -  LMB while holding = throw  -  walk into big crates to push them\n" +
            "F1 AI debug overlay\n\n" +
            "Press ANY KEY or CLICK to begin";

        private PlayerController _player;
        private VehicleArrival _arrivalVehicle;

        public void SetObjectiveTargets(Transform[] targets) => objectiveTargets = targets;

        private void Awake()
        {
            Instance = this;
            GameEvents.AgentKilled += (k, p) => Kills++;
        }

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            _arrivalVehicle = FindAnyObjectByType<VehicleArrival>();
            if (_player != null)
            {
                _player.InputEnabled = false;
                _player.Health.Died += OnPlayerDied;
            }
            Cursor.lockState = CursorLockMode.None;

            if (FinalRescue.FinalRescueMission.Instance == null && FindAnyObjectByType<FinalRescue.FinalRescueMission>() == null)
            {
                var missionGo = new GameObject("FinalRescue_Mission");
                missionGo.AddComponent<FinalRescue.FinalRescueMission>();
            }

            if (UI.PauseMenu.Instance == null && FindAnyObjectByType<UI.PauseMenu>() == null)
            {
                var pauseGo = new GameObject("PauseMenu_Manager");
                pauseGo.AddComponent<UI.PauseMenu>();
            }

            if (World.DryingChamberContainers.Instance == null && FindAnyObjectByType<World.DryingChamberContainers>() == null)
            {
                var containerGo = new GameObject("DryingChamber_Containers_Manager");
                containerGo.AddComponent<World.DryingChamberContainers>();
            }

            if (World.RollingRoomEncounter.Instance == null && FindAnyObjectByType<World.RollingRoomEncounter>() == null)
            {
                var encounterGo = new GameObject("RollingRoom_Encounter_Manager");
                encounterGo.AddComponent<World.RollingRoomEncounter>();
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            switch (State)
            {
                case Flow.Intro:
                    // Start on any key so macOS Return/Enter layout differences cannot
                    // leave the title screen stuck. A Game-view click is a fallback.
                    bool startPressed = kb.anyKey.wasPressedThisFrame ||
                                        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                    if (startPressed) Begin();
                    break;
                case Flow.Playing:
                    if (UI.PauseMenu.Instance != null && UI.PauseMenu.Instance.IsPaused)
                    {
                        break;
                    }
                    if (kb.escapeKey.wasPressedThisFrame)
                    {
                        if (UI.PauseMenu.Instance != null)
                        {
                            UI.PauseMenu.Instance.TogglePause();
                        }
                        else
                        {
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                        }
                    }
                    else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                    break;
                case Flow.Won:
                case Flow.Lost:
                    if (kb.enterKey.wasPressedThisFrame) Restart();
                    break;
            }
        }

        private static void Restart()
        {
            StaticReset.RunAll();
            AI.Agents.AgentBase.Active.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Begin()
        {
            State = Flow.Playing;
            StartTime = Time.time;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_arrivalVehicle == null)
            {
                if (_player != null) _player.InputEnabled = true;
                GameEvents.Objective(ObjectiveText[0]);
                GameEvents.Overseer("OVERSEER: Unauthorised heat signature at the front gate. Welcome home, Nila.");
                return;
            }

            if (_player != null)
            {
                _player.InputEnabled = false;
                _player.SetCinematicPose(new Vector3(-20f, 0.05f, -3f), 180f);
            }
            _arrivalVehicle.DriveIn();
            StartCoroutine(FinishArrival());
        }

        private IEnumerator FinishArrival()
        {
            yield return new WaitForSecondsRealtime(5.15f);
            if (_player != null)
            {
                _player.SetCinematicPose(new Vector3(-20f, 0.05f, -5.5f), 0f);
                _player.InputEnabled = true;
            }
            GameEvents.Objective(ObjectiveText[0]);
            GameEvents.Overseer("OVERSEER: Unauthorised heat signature at the front gate. Welcome home, Nila.");
        }

        public void OnZoneEntered(ZoneTrigger zone)
        {
            GameEvents.RaiseZone(zone.zoneName);
            if (!string.IsNullOrEmpty(zone.overseerLine)) GameEvents.Overseer(zone.overseerLine);
            OverseerDirector.Instance?.Activate();
            if (zone.isControlRoom && Stage == 2) SetStage(3);
        }

        public void OnKeycardCollected()
        {
            HasKeycard = true;
            GameEvents.Overseer("OVERSEER: That card belongs to a dead shift. It will not save you.");
            SetStage(1);
        }

        public void OnGateUnlocked()
        {
            GameEvents.Overseer("OVERSEER: Drying Chamber breached. Venting withering gas lines.");
            SetStage(2);
        }

        public void OnKillSwitchStarted()
        {
            GameEvents.Overseer("OVERSEER: NO. All units to the control room. ALL UNITS.");
            GameEvents.RaiseFinalWave();
        }

        public void OnKillSwitchCompleted()
        {
            IsKillSwitchPulled = true;

            // Check if Titan is already defeated
            var titan = FindAnyObjectByType<AI.Agents.TitanBossBrain>(FindObjectsInactive.Exclude);
            bool titanDead = titan == null || titan.HealthComponent.IsDead || IsTitanDefeated;

            if (titanDead)
            {
                if (IsAppaRescued)
                {
                    TriggerWin();
                    return;
                }
                else
                {
                    // Deactivate all remaining robots - Titan is already gone
                    foreach (var a in FindObjectsByType<AI.Agents.AgentBase>(FindObjectsInactive.Exclude))
                        a.Deactivate(600f);
                    if (AI.Director.OverseerDirector.Instance != null)
                        AI.Director.OverseerDirector.Instance.PauseSpawns(600f);
                    ProceduralAudio.PlayAt("explosion", _player != null ? _player.transform.position : Vector3.zero, 1.2f, 0.4f);
                    GameEvents.Overseer("OVERSEER: POWER CORE DRAINED! All units offline. But she still hasn't found him...");
                    GameEvents.Objective("POWER CORE DRAINED! Go to Sector E, use the Master Key, and rescue Appa!");
                    return;
                }
            }

            // Broadcast emergency grid failure message
            GameEvents.Overseer("OVERSEER: POWER CORE COLLAPSED! All factory robots and drones deactivated for 5 minutes! WARNING: TITAN Unit-94 operates on auxiliary power and remains ACTIVE!");
            GameEvents.Objective("POWER GRID DRAINED: All standard robots deactivated for 5m! Destroy TITAN ENFORCER to save Appa!");

            // Deactivate all standard robots for 5 minutes (300 seconds)
            foreach (var a in FindObjectsByType<AI.Agents.AgentBase>(FindObjectsInactive.Exclude))
            {
                if (a is AI.Agents.TitanBossBrain) continue; // Titan is immune!
                a.Deactivate(300f);
            }

            // Pause director spawns for 5 minutes
            if (AI.Director.OverseerDirector.Instance != null)
            {
                AI.Director.OverseerDirector.Instance.PauseSpawns(300f);
            }

            // EMP audio feedback
            ProceduralAudio.PlayAt("explosion", _player != null ? _player.transform.position : Vector3.zero, 1.2f, 0.4f);
        }

        public void OnTitanDefeated()
        {
            IsTitanDefeated = true;
            GameEvents.Overseer("OVERSEER: Unit-94 DESTROYED?! Security mainframe breached... The Master Prison Cell Key has dropped from its core!");
            GameEvents.Objective("TITAN DESTROYED: Pick up the Master Cell Key from Titan's wreckage!");

            FinalRescue.FinalRescueMission.Instance?.OnTitanDefeated();
        }

        public void OnMasterCellKeyCollected()
        {
            HasMasterCellKey = true;
            if (FinalRescue.FinalRescueMission.Instance != null)
            {
                FinalRescue.FinalRescueMission.Instance.OnKeyCollected();
            }
            else
            {
                var cell = HoldingCell.Instance != null
                    ? HoldingCell.Instance
                    : UnityEngine.Object.FindAnyObjectByType<HoldingCell>();
                if (cell != null)
                    SetCustomObjective(cell.TerminalTransform, "Find the Secret Laboratory.");
                else
                    GameEvents.Objective("Find the Secret Laboratory.");
            }
        }

        public void OnAppaRescued()
        {
            IsAppaRescued = true;
            TriggerWin();
        }

        private void TriggerWin()
        {
            SetStage(4);
            State = Flow.Won;
            EndTime = Time.time;
            if (_player != null) _player.InputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            foreach (var a in FindObjectsByType<AI.Agents.AgentBase>(FindObjectsInactive.Exclude))
                a.HealthComponent.TakeDamage(9999f, a.transform.position + Vector3.up);
        }

        public string CalculateMissionRank(out int totalScore)
        {
            float elapsed = EndTime - StartTime;
            int baseScore = 5000;
            int timeBonus = Mathf.Max(0, Mathf.RoundToInt((300f - elapsed) * 10f));
            int killBonus = Kills * 150;
            int rescueBonus = IsAppaRescued ? 2000 : 500;

            totalScore = baseScore + timeBonus + killBonus + rescueBonus;

            if (totalScore >= 9000) return "S";
            if (totalScore >= 7500) return "A";
            if (totalScore >= 6000) return "B";
            return "C";
        }

        private void SetStage(int s)
        {
            Stage = s;
            GameEvents.Objective(ObjectiveText[Mathf.Clamp(s, 0, ObjectiveText.Length - 1)]);
        }

        private void OnPlayerDied()
        {
            State = Flow.Lost;
            EndTime = Time.time;
            _player.InputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameEvents.Overseer("OVERSEER: Variable removed. Resuming production.");
        }
    }
}
