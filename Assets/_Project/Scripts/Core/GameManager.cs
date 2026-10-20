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
        public bool CanUseKillSwitch => Stage == 3;
        public int Kills { get; private set; }
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }

        public Vector3 CurrentObjectivePosition =>
            objectiveTargets != null && Stage < objectiveTargets.Length && objectiveTargets[Stage] != null
                ? objectiveTargets[Stage].position : transform.position;

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
                    if (kb.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
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
            SetStage(4);
            State = Flow.Won;
            EndTime = Time.time;
            if (_player != null) _player.InputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            foreach (var a in FindObjectsByType<AI.Agents.AgentBase>())
                a.HealthComponent.TakeDamage(9999f, a.transform.position + Vector3.up);
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
