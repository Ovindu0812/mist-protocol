using System.Collections;
using System.Collections.Generic;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Multi-step environmental puzzle task required to retrieve Appa's Tier-1 Keycard.
    /// Instead of sitting unprotected on the desk, the keycard is secured inside Appa's
    /// magnetically-sealed personal desk safe. The player must explore the Withering Loft
    /// and flip 3 auxiliary power breaker levers to restore power and release the safe lock.
    /// </summary>
    public class WitheringLoftKeycardTask : MonoBehaviour
    {
        public static WitheringLoftKeycardTask Instance { get; private set; }

        [SerializeField] private AppaDeskSafe deskSafe;
        [SerializeField] private List<WitheringBreaker> breakers = new();

        public int TotalBreakers => 3;
        public int FlippedBreakersCount { get; private set; }
        public bool IsTaskCompleted { get; private set; }

        private bool _isInitialized;

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
            EnsureTaskEnvironment();
        }

        public void EnsureTaskEnvironment()
        {
            if (_isInitialized) return;

            var keycard = FindAnyObjectByType<KeycardPickup>();
            if (keycard != null)
            {
                keycard.IsLocked = true;
            }

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Build or Bind Appa's Personal Desk Safe on his north-west desk
            if (deskSafe == null)
            {
                deskSafe = FindAnyObjectByType<AppaDeskSafe>();
                if (deskSafe == null)
                {
                    Vector3 safePos = new Vector3(-27.0f, 0.85f, 33.5f);
                    deskSafe = CreateDeskSafe(safePos, litShader, keycard);
                }
            }

            // 2. Build 3 Auxiliary Power Breakers across Withering Loft
            if (breakers.Count == 0)
            {
                // Breaker 1: Trough Sector A (South)
                breakers.Add(CreateBreakerBox("Breaker_Loft_South", new Vector3(-15.2f, 1.25f, 7.5f), 90f,
                    "Breaker 1: South Withering Trough", litShader));

                // Breaker 2: Trough Sector B (Mid catwalk)
                breakers.Add(CreateBreakerBox("Breaker_Loft_Mid", new Vector3(-24.7f, 1.25f, 20.5f), -90f,
                    "Breaker 2: Central Leaf Platform", litShader));

                // Breaker 3: Substation Wall near Appa's desk (North-West)
                breakers.Add(CreateBreakerBox("Breaker_Loft_North", new Vector3(-29.2f, 1.35f, 31.8f), 0f,
                    "Breaker 3: Loft Primary Substation", litShader));
            }

            _isInitialized = true;
        }

        public void OnBreakerFlipped(WitheringBreaker breaker)
        {
            if (IsTaskCompleted) return;

            FlippedBreakersCount++;

            // Audio and HUD feedback
            ProceduralAudio.PlayAt("beep", breaker.transform.position, 0.85f, 1.0f + FlippedBreakersCount * 0.15f);

            if (FlippedBreakersCount < TotalBreakers)
            {
                string msg = $"AUXILIARY POWER: Breaker {FlippedBreakersCount}/{TotalBreakers} Restored!";
                GameEvents.Objective($"Restore Loft Auxiliary Breakers ({FlippedBreakersCount}/{TotalBreakers}) to open Appa's Safe!");
                GameEvents.Overseer($"OVERSEER: Power grid fluctuation in Withering Loft. Auxiliary line {FlippedBreakersCount} re-routed.");
            }
            else
            {
                // All 3 restored! Trigger safe unlock sequence
                StartCoroutine(UnlockSafeSequence());
            }
        }

        private IEnumerator UnlockSafeSequence()
        {
            IsTaskCompleted = true;

            // Deep power restoration hum & chime
            ProceduralAudio.PlayAt("success", deskSafe.transform.position, 1.0f, 1.1f);
            ProceduralAudio.PlayAt("zap", deskSafe.transform.position, 0.7f, 1.4f);

            GameEvents.Overseer("OVERSEER: Auxiliary power restored in Withering Loft. You are persistent, Nila.");
            GameEvents.Objective("Power restored! Open Appa's desk safe and retrieve the Keycard!");

            yield return new WaitForSeconds(0.4f);

            if (deskSafe != null)
            {
                deskSafe.UnlockSafe();
            }
        }

        private AppaDeskSafe CreateDeskSafe(Vector3 pos, Shader shader, KeycardPickup keycard)
        {
            var safeRoot = new GameObject("Appa_PersonalDeskSafe");
            safeRoot.transform.position = pos;
            safeRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Dark industrial chassis
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Safe_Body";
            body.transform.SetParent(safeRoot.transform, false);
            body.transform.localScale = new Vector3(0.55f, 0.42f, 0.48f);
            var bodyMat = new Material(shader) { color = new Color(0.12f, 0.14f, 0.16f) };
            bodyMat.SetFloat("_Metallic", 0.85f);
            bodyMat.SetFloat("_Smoothness", 0.45f);
            body.GetComponent<Renderer>().material = bodyMat;

            // Safe Door (hinged front)
            var doorPivot = new GameObject("Safe_Door_Pivot");
            doorPivot.transform.SetParent(safeRoot.transform, false);
            doorPivot.transform.localPosition = new Vector3(-0.25f, 0f, 0.24f);

            var doorMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorMesh.name = "Door_Plate";
            doorMesh.transform.SetParent(doorPivot.transform, false);
            doorMesh.transform.localPosition = new Vector3(0.24f, 0f, 0f);
            doorMesh.transform.localScale = new Vector3(0.48f, 0.38f, 0.04f);
            var doorMat = new Material(shader) { color = new Color(0.18f, 0.22f, 0.26f) };
            doorMat.SetFloat("_Metallic", 0.9f);
            doorMat.SetFloat("_Smoothness", 0.5f);
            doorMesh.GetComponent<Renderer>().material = doorMat;

            // Status Lock Light
            var lightGo = new GameObject("Lock_Light");
            lightGo.transform.SetParent(safeRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0.16f, 0.12f, 0.26f);
            var sl = lightGo.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.color = Color.red;
            sl.range = 1.8f;
            sl.intensity = 0.8f;

            // Safe interaction script
            var safeComp = safeRoot.AddComponent<AppaDeskSafe>();
            safeComp.Configure(doorPivot.transform, sl, keycard);

            // Position keycard securely inside safe
            if (keycard != null)
            {
                keycard.transform.position = pos + Vector3.up * 0.05f;
            }

            return safeComp;
        }

        private WitheringBreaker CreateBreakerBox(string name, Vector3 pos, float yaw, string title, Shader shader)
        {
            var root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Metal junction box casing
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Box_Casing";
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(0.28f, 0.44f, 0.14f);
            var mat = new Material(shader) { color = new Color(0.15f, 0.16f, 0.18f) };
            mat.SetFloat("_Metallic", 0.75f);
            mat.SetFloat("_Smoothness", 0.4f);
            box.GetComponent<Renderer>().material = mat;

            // Lever Switch Handle
            var leverPivot = new GameObject("Lever_Pivot");
            leverPivot.transform.SetParent(root.transform, false);
            leverPivot.transform.localPosition = new Vector3(0f, 0.02f, 0.08f);

            var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "Lever_Handle";
            lever.transform.SetParent(leverPivot.transform, false);
            lever.transform.localPosition = new Vector3(0f, 0.1f, 0.02f);
            lever.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
            var levMat = new Material(shader) { color = new Color(0.9f, 0.7f, 0.1f) };
            lever.GetComponent<Renderer>().material = levMat;

            // Status light
            var lightGo = new GameObject("Status_Light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.16f, 0.09f);
            var sl = lightGo.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.color = new Color(1f, 0.2f, 0.1f);
            sl.range = 2.0f;
            sl.intensity = 0.75f;

            var breakerComp = root.AddComponent<WitheringBreaker>();
            breakerComp.Configure(title, leverPivot.transform, sl);

            return breakerComp;
        }
    }

    /// <summary>
    /// Interactive Safe on Appa's desk enclosing his keycard.
    /// </summary>
    public class AppaDeskSafe : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform doorPivot;
        [SerializeField] private Light lockLight;
        [SerializeField] private KeycardPickup keycard;

        public bool IsUnlocked { get; private set; }

        public string Prompt => IsUnlocked
            ? "[Safe Unlocked - Take Keycard]"
            : $"[E] Appa's Personal Safe (LOCKED: {WitheringLoftKeycardTask.Instance?.FlippedBreakersCount ?? 0}/3 Breakers)";

        public bool CanInteract => !IsUnlocked;

        public void Configure(Transform pivot, Light light, KeycardPickup kc)
        {
            doorPivot = pivot;
            lockLight = light;
            keycard = kc;
        }

        public void Interact()
        {
            if (IsUnlocked) return;

            // Buzz and display task hint
            ProceduralAudio.PlayAt("shortCircuit", transform.position, 0.6f, 1.4f);
            int count = WitheringLoftKeycardTask.Instance != null ? WitheringLoftKeycardTask.Instance.FlippedBreakersCount : 0;
            GameEvents.Objective($"SAFE LOCKED: Auxiliary power offline! Flip 3 Loft Breakers ({count}/3) to release magnetic seal.");
        }

        public void UnlockSafe()
        {
            if (IsUnlocked) return;
            IsUnlocked = true;

            // Rotate door open
            if (doorPivot != null)
            {
                doorPivot.localRotation = Quaternion.Euler(0f, -105f, 0f);
            }

            // Green lock status
            if (lockLight != null)
            {
                lockLight.color = new Color(0.2f, 1f, 0.4f);
                lockLight.intensity = 1.4f;
                lockLight.range = 2.8f;
            }

            // Clank audio & steam burst
            ProceduralAudio.PlayAt("door", transform.position, 0.9f, 1.3f);
            ProceduralAudio.PlayAt("pickup", transform.position, 0.7f, 1.5f);
            Vfx.Burst(transform.position + Vector3.up * 0.2f, Vector3.up, new Color(0.2f, 0.85f, 1f), 16, 2.5f, 0.1f, 0.4f);

            // Release keycard
            if (keycard != null)
            {
                keycard.UnlockAndReveal();
            }
        }
    }

    /// <summary>
    /// Interactive auxiliary power breaker lever switch in the Withering Loft.
    /// </summary>
    public class WitheringBreaker : MonoBehaviour, IInteractable
    {
        [SerializeField] private string breakerTitle = "Auxiliary Breaker";
        [SerializeField] private Transform leverPivot;
        [SerializeField] private Light statusLight;

        public bool IsFlipped { get; private set; }

        public string Prompt => IsFlipped
            ? $"[{breakerTitle} - ONLINE]"
            : $"[E] Restore {breakerTitle}";

        public bool CanInteract => !IsFlipped;

        public void Configure(string title, Transform pivot, Light light)
        {
            breakerTitle = title;
            leverPivot = pivot;
            statusLight = light;
        }

        public void Interact()
        {
            if (IsFlipped) return;
            IsFlipped = true;

            // Rotate lever down 50 degrees
            if (leverPivot != null)
            {
                leverPivot.localRotation = Quaternion.Euler(50f, 0f, 0f);
            }

            // Status light to bright green
            if (statusLight != null)
            {
                statusLight.color = new Color(0.2f, 1f, 0.35f);
                statusLight.intensity = 1.2f;
            }

            // Click, zap, and electrical spark burst
            ProceduralAudio.PlayAt("click", transform.position, 0.8f, 0.9f);
            ProceduralAudio.PlayAt("zap", transform.position, 0.6f, 1.2f);
            Vfx.Burst(transform.position + transform.forward * 0.12f, transform.forward, new Color(0.3f, 0.9f, 1f), 12, 2.5f, 0.08f, 0.35f);

            // Notify task manager
            if (WitheringLoftKeycardTask.Instance != null)
            {
                WitheringLoftKeycardTask.Instance.OnBreakerFlipped(this);
            }
        }
    }
}
