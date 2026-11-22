using System.Collections;
using MistProtocol.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Reinforced High-Security Holding Cell in Sector E Containment Vault.
    /// Appa is imprisoned inside behind heavy steel bars and an electronic blast gate.
    /// Unlocked by the Master Cell Key dropped from Titan Automaton.
    /// </summary>
    public class HoldingCell : MonoBehaviour
    {
        public static HoldingCell Instance { get; private set; }

        [SerializeField] private Transform cellDoor;
        [SerializeField] private Transform lockTerminal;
        [SerializeField] private Transform appaTransform;
        [SerializeField] private Light terminalStatusLight;

        private bool _isUnlocked;
        private bool _isRescued;
        private Vector3 _doorClosedPos;
        private Vector3 _doorOpenPos;

        public Transform TerminalTransform => lockTerminal != null ? lockTerminal : transform;
        public Transform AppaTransform => appaTransform != null ? appaTransform : transform;
        public bool IsUnlocked => _isUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            // Superseded by The Final Rescue mission (Secret Laboratory).
            // Destroy immediately so the old prototype cell never interferes with the puzzle laboratory.
            Destroy(gameObject);
        }

        private void Start()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Procedurally generates the holding cell and Appa if not already placed in scene.
        /// </summary>
        public void BuildCellIfMissing()
        {
            var existingAppa = transform.Find("NPC_Appa");
            if (existingAppa != null)
            {
                appaTransform = existingAppa;
                // If old primitive model is present, upgrade to 3D FBX model
                if (existingAppa.Find("Appa_Mesh") == null)
                {
                    for (int i = existingAppa.childCount - 1; i >= 0; i--)
                    {
                        var child = existingAppa.GetChild(i);
                        if (Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                    AttachAppaModel(existingAppa.gameObject);
                }
            }

            if (transform.childCount > 0 && appaTransform != null) return;

            transform.position = new Vector3(51.5f, 0f, 12f);

            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            var darkMetal = new Material(litShader) { color = new Color(0.12f, 0.13f, 0.15f) };
            darkMetal.SetFloat("_Metallic", 0.7f);
            darkMetal.SetFloat("_Smoothness", 0.4f);

            var barMetal = new Material(litShader) { color = new Color(0.2f, 0.22f, 0.25f) };
            barMetal.SetFloat("_Metallic", 0.85f);
            barMetal.SetFloat("_Smoothness", 0.6f);

            var hazardMat = new Material(litShader) { color = new Color(0.95f, 0.8f, 0.1f) };

            // 1. Enclosure Walls & Ceiling
            // South Wall (z = 9.5f)
            CreateWall("Wall_South", new Vector3(0f, 1.75f, -2.5f), new Vector3(3.8f, 3.5f, 0.25f), darkMetal);
            // North Wall (z = 14.5f)
            CreateWall("Wall_North", new Vector3(0f, 1.75f, 2.5f), new Vector3(3.8f, 3.5f, 0.25f), darkMetal);
            // Ceiling
            CreateWall("Ceiling", new Vector3(0f, 3.5f, 0f), new Vector3(3.8f, 0.25f, 5.25f), darkMetal);
            // Hazard Floor Threshold
            CreateWall("Floor_Threshold", new Vector3(-1.85f, 0.04f, 0f), new Vector3(0.4f, 0.08f, 5.2f), hazardMat);

            // 2. Front Security Bars (Side Panels)
            // South side bars (-2.4m to -1.1m)
            CreateBars(new Vector3(-1.85f, 1.7f, -1.8f), 0.9f, barMetal);
            // North side bars (1.1m to 2.4m)
            CreateBars(new Vector3(-1.85f, 1.7f, 1.8f), 0.9f, barMetal);

            // Doorframe pillars
            CreateWall("Pillar_Door_S", new Vector3(-1.85f, 1.75f, -1.1f), new Vector3(0.25f, 3.5f, 0.25f), darkMetal);
            CreateWall("Pillar_Door_N", new Vector3(-1.85f, 1.75f, 1.1f), new Vector3(0.25f, 3.5f, 0.25f), darkMetal);
            CreateWall("Lintel_Door", new Vector3(-1.85f, 3.3f, 0f), new Vector3(0.25f, 0.4f, 2.2f), darkMetal);

            // 3. Sliding Blast Cell Door (at middle opening: z = -1.0f to 1.0f)
            var doorGo = new GameObject("Cell_SlidingDoor");
            doorGo.transform.SetParent(transform, false);
            doorGo.transform.localPosition = new Vector3(-1.82f, 1.6f, 0f);
            cellDoor = doorGo.transform;

            // Door frame & bars
            var doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = "Door_Frame";
            doorLeaf.transform.SetParent(doorGo.transform, false);
            doorLeaf.transform.localScale = new Vector3(0.12f, 3.1f, 2.15f);
            doorLeaf.GetComponent<Renderer>().material = darkMetal;

            // Vertical reinforced slits / bars inside door
            for (float bz = -0.7f; bz <= 0.7f; bz += 0.35f)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bar.name = "DoorBar";
                bar.transform.SetParent(doorGo.transform, false);
                bar.transform.localScale = new Vector3(0.06f, 1.45f, 0.06f);
                bar.transform.localPosition = new Vector3(0f, 0f, bz);
                bar.GetComponent<Renderer>().material = barMetal;
                var col = bar.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            // 4. Electronic Lock Terminal
            var termGo = new GameObject("Cell_LockTerminal");
            termGo.transform.SetParent(transform, false);
            termGo.transform.localPosition = new Vector3(-1.95f, 1.3f, -1.35f);
            lockTerminal = termGo.transform;

            var termBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            termBox.name = "Terminal_Console";
            termBox.transform.SetParent(termGo.transform, false);
            termBox.transform.localScale = new Vector3(0.18f, 0.45f, 0.32f);
            termBox.GetComponent<Renderer>().material = darkMetal;

            var termInteract = termGo.AddComponent<CellLockTerminalInteractable>();
            termInteract.Configure(this);

            var termLightGo = new GameObject("Status_LED");
            termLightGo.transform.SetParent(termGo.transform, false);
            termLightGo.transform.localPosition = new Vector3(-0.12f, 0.1f, 0f);
            terminalStatusLight = termLightGo.AddComponent<Light>();
            terminalStatusLight.type = LightType.Point;
            terminalStatusLight.range = 2.5f;
            terminalStatusLight.intensity = 2f;
            terminalStatusLight.color = new Color(1f, 0.15f, 0.1f); // Red when locked

            // 5. Interior Warm Lantern
            var lanternGo = new GameObject("Cell_InteriorLight");
            lanternGo.transform.SetParent(transform, false);
            lanternGo.transform.localPosition = new Vector3(0.5f, 3.1f, 0f);
            var lanternLight = lanternGo.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.range = 7f;
            lanternLight.intensity = 2.5f;
            lanternLight.color = new Color(1f, 0.78f, 0.45f);

            // 6. APPA (Nila's Father) NPC Character
            CreateAppaNPC(new Vector3(0.6f, 0f, 0f), darkMetal);
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

        private void CreateBars(Vector3 center, float width, Material barMat)
        {
            int count = Mathf.Max(3, Mathf.RoundToInt(width / 0.22f));
            float step = width / (count - 1);
            float startZ = center.z - width * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bar.name = "SecurityBar";
                bar.transform.SetParent(transform, false);
                bar.transform.localPosition = new Vector3(center.x, center.y, startZ + i * step);
                bar.transform.localScale = new Vector3(0.065f, 1.6f, 0.065f);
                bar.GetComponent<Renderer>().material = barMat;
            }
        }

        private void CreateAppaNPC(Vector3 localPos, Material baseMat)
        {
            var appaGo = new GameObject("NPC_Appa");
            appaGo.transform.SetParent(transform, false);
            appaGo.transform.localPosition = localPos;
            appaGo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Looking towards the cell door
            appaTransform = appaGo.transform;

            AttachAppaModel(appaGo);

            // Interactive component on Appa
            var appaInteract = appaGo.AddComponent<AppaNPCInteractable>();
            appaInteract.Configure(this);

            var col = appaGo.AddComponent<CapsuleCollider>();
            col.height = 1.9f;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 0.95f, 0f);
            col.isTrigger = true;
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

                // Add glowing Supervisor badge to chest
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
            else
            {
                CreateProceduralAppa(appaGo);
            }
        }

        private static void ApplyAppaMaterials(GameObject root)
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var skinMat = new Material(litShader) { color = new Color(0.56f, 0.37f, 0.25f) };
            skinMat.SetFloat("_Smoothness", 0.35f);

            var creamMat = new Material(litShader) { color = new Color(0.88f, 0.82f, 0.70f) };
            creamMat.SetFloat("_Smoothness", 0.25f);

            var vestMat = new Material(litShader) { color = new Color(0.12f, 0.08f, 0.04f) };
            vestMat.SetFloat("_Smoothness", 0.20f);

            var trouserMat = new Material(litShader) { color = new Color(0.16f, 0.10f, 0.06f) };
            trouserMat.SetFloat("_Smoothness", 0.25f);

            var shoeMat = new Material(litShader) { color = new Color(0.08f, 0.04f, 0.02f) };
            shoeMat.SetFloat("_Smoothness", 0.55f);

            var hairMat = new Material(litShader) { color = new Color(0.25f, 0.25f, 0.27f) };
            hairMat.SetFloat("_Smoothness", 0.25f);

            var glassMat = new Material(litShader) { color = new Color(0.08f, 0.08f, 0.10f) };
            glassMat.SetFloat("_Metallic", 0.9f);
            glassMat.SetFloat("_Smoothness", 0.8f);

            var lensMat = new Material(litShader) { color = new Color(0.85f, 0.92f, 0.98f, 0.35f) };
            lensMat.SetFloat("_Smoothness", 0.95f);

            var eyeWhiteMat = new Material(litShader) { color = new Color(0.92f, 0.92f, 0.90f) };
            eyeWhiteMat.SetFloat("_Smoothness", 0.8f);

            var eyeIrisMat = new Material(litShader) { color = new Color(0.10f, 0.06f, 0.03f) };
            eyeIrisMat.SetFloat("_Smoothness", 0.9f);

            var watchMat = new Material(litShader) { color = new Color(0.85f, 0.72f, 0.35f) };
            watchMat.SetFloat("_Metallic", 0.95f);
            watchMat.SetFloat("_Smoothness", 0.85f);

            var strapMat = new Material(litShader) { color = new Color(0.14f, 0.08f, 0.04f) };
            strapMat.SetFloat("_Smoothness", 0.4f);

            var btnMat = new Material(litShader) { color = new Color(0.28f, 0.18f, 0.10f) };
            btnMat.SetFloat("_Smoothness", 0.4f);

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("eyeball"))
                    r.material = eyeWhiteMat;
                else if (n.Contains("iris"))
                    r.material = eyeIrisMat;
                else if (n.Contains("head") || n.Contains("chin") || n.Contains("nose") || n.Contains("ear") ||
                         n.Contains("neck") || n.Contains("forearm") || n.Contains("palm") || n.Contains("thumb") ||
                         n.Contains("finger") || n.Contains("hand"))
                    r.material = skinMat;
                else if (n.Contains("collar") || n.Contains("cuff") || n.Contains("arm_upper") || n.Contains("v_shirt"))
                    r.material = creamMat;
                else if (n.Contains("vest") || n.Contains("pocket") || n.Contains("cinch"))
                    r.material = vestMat;
                else if (n.Contains("trouser") || n.Contains("pelvis") || n.Contains("crease") || n.Contains("leg"))
                    r.material = trouserMat;
                else if (n.Contains("shoe") || n.Contains("sole") || n.Contains("heel"))
                    r.material = shoeMat;
                else if (n.Contains("hair") || n.Contains("beard") || n.Contains("moust") || n.Contains("brow"))
                    r.material = hairMat;
                else if (n.Contains("lens"))
                    r.material = lensMat;
                else if (n.Contains("glass") || n.Contains("bridge") || n.Contains("temple"))
                    r.material = glassMat;
                else if (n.Contains("strap"))
                    r.material = strapMat;
                else if (n.Contains("watch") || n.Contains("case") || n.Contains("buckle"))
                    r.material = watchMat;
                else if (n.Contains("btn") || n.Contains("button"))
                    r.material = btnMat;
            }
        }

        private static void CreateProceduralAppa(GameObject appaGo)
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var shirtMat = new Material(litShader) { color = new Color(0.88f, 0.82f, 0.70f) };
            var skinMat = new Material(litShader) { color = new Color(0.68f, 0.48f, 0.35f) };
            var hairMat = new Material(litShader) { color = new Color(0.65f, 0.65f, 0.68f) };
            var pantsMat = new Material(litShader) { color = new Color(0.20f, 0.12f, 0.06f) };

            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(appaGo.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            torso.transform.localScale = new Vector3(0.48f, 0.65f, 0.28f);
            torso.GetComponent<Renderer>().material = shirtMat;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(appaGo.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            head.transform.localScale = new Vector3(0.32f, 0.35f, 0.32f);
            head.GetComponent<Renderer>().material = skinMat;

            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.18f, -0.05f);
            hair.transform.localScale = new Vector3(1.05f, 0.75f, 1.08f);
            hair.GetComponent<Renderer>().material = hairMat;

            for (float lx = -0.14f; lx <= 0.14f; lx += 0.28f)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "Leg";
                leg.transform.SetParent(appaGo.transform, false);
                leg.transform.localPosition = new Vector3(lx, 0.42f, 0f);
                leg.transform.localScale = new Vector3(0.18f, 0.85f, 0.2f);
                leg.GetComponent<Renderer>().material = pantsMat;
            }

            for (float ax = -0.32f; ax <= 0.32f; ax += 0.64f)
            {
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Arm";
                arm.transform.SetParent(appaGo.transform, false);
                arm.transform.localPosition = new Vector3(ax, 1.1f, 0f);
                arm.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);
                arm.GetComponent<Renderer>().material = shirtMat;
            }
        }

        public void TryUnlockCell()
        {
            if (_isUnlocked) return;

            bool hasKey = GameManager.Instance != null && GameManager.Instance.HasMasterCellKey;
            if (!hasKey)
            {
                ProceduralAudio.PlayAt("zap", TerminalTransform.position, 0.6f, 0.8f);
                GameEvents.Overseer("TERMINAL: ACCESS DENIED. Master Prison Cell Key required.");
                return;
            }

            _isUnlocked = true;
            StartCoroutine(UnlockSequence());
        }

        private IEnumerator UnlockSequence()
        {
            ProceduralAudio.PlayAt("pickup", TerminalTransform.position, 1f, 1.4f);
            ProceduralAudio.PlayAt("door", transform.position, 1f, 0.7f);

            // Turn LED green
            if (terminalStatusLight == null && lockTerminal != null)
                terminalStatusLight = lockTerminal.GetComponentInChildren<Light>();

            if (terminalStatusLight != null)
            {
                terminalStatusLight.color = new Color(0.2f, 1f, 0.4f);
                terminalStatusLight.intensity = 3.5f;
            }

            GameEvents.Overseer("TERMINAL: MASTER CLEARANCE GRANTED. Opening Sector E Holding Cell blast gate.");
            GameEvents.Objective("TALK TO APPA: Step into the holding cell to rescue your father!");

            // Point objective directly to Appa
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCustomObjective(AppaTransform, "TALK TO APPA: Step into the holding cell to rescue your father!");
            }

            // Ensure cellDoor reference exists
            if (cellDoor == null) cellDoor = transform.Find("Cell_SlidingDoor");

            if (cellDoor != null)
            {
                // Slide door open using localPosition so parent orientation/position is always respected
                float elapsed = 0f;
                Vector3 startLocal = cellDoor.localPosition;
                Vector3 endLocal = new Vector3(startLocal.x, startLocal.y, 2.2f);

                while (elapsed < 1.8f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / 1.8f);
                    if (cellDoor != null) cellDoor.localPosition = Vector3.Lerp(startLocal, endLocal, t);
                    yield return null;
                }

                if (cellDoor != null)
                {
                    cellDoor.localPosition = endLocal;
                    // Disable colliders on the sliding door so player can step into cell freely
                    foreach (var c in cellDoor.GetComponentsInChildren<Collider>())
                    {
                        c.enabled = false;
                    }
                }
            }

            // Appa greets Nila as door finishes opening
            GameEvents.Overseer("APPA: \"Nila! My duwa! You did it... You defeated the Titan! Come here!\"");
        }

        public void RescueAppa()
        {
            if (_isRescued) return;
            _isRescued = true;

            StartCoroutine(RescueSequence());
        }

        private IEnumerator RescueSequence()
        {
            ProceduralAudio.PlayAt("pickup", AppaTransform.position, 1f, 1.2f);
            ProceduralAudio.PlayAt("explosion", AppaTransform.position, 0.3f, 0.4f);

            GameEvents.Overseer("APPA: \"Nila... my brave duwa. You really did it. You destroyed the Overseer's machine. Let's go home.\"");
            GameEvents.Objective("MISSION ACCOMPLISHED: Appa has been rescued! Escaping Glenmist...");

            yield return new WaitForSeconds(1.8f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAppaRescued();
            }
        }
    }

    /// <summary>
    /// Interactive electronic terminal outside the holding cell.
    /// </summary>
    public class CellLockTerminalInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private HoldingCell _cell;

        public HoldingCell Cell
        {
            get
            {
                if (_cell == null)
                    _cell = GetComponentInParent<HoldingCell>() ?? HoldingCell.Instance ?? FindAnyObjectByType<HoldingCell>();
                return _cell;
            }
        }

        public void Configure(HoldingCell cell) { _cell = cell; }

        public bool CanInteract => Cell != null && !Cell.IsUnlocked;

        public string Prompt
        {
            get
            {
                var c = Cell;
                if (c != null && c.IsUnlocked) return "Holding Cell Unlocked";
                bool hasKey = GameManager.Instance != null && GameManager.Instance.HasMasterCellKey;
                return hasKey ? "[E] Swipe Master Key to unlock Holding Cell" : "[Locked] Requires Master Cell Key (dropped by Titan)";
            }
        }

        public void Interact()
        {
            var c = Cell;
            if (c != null)
            {
                c.TryUnlockCell();
            }
        }
    }

    /// <summary>
    /// Interactive father NPC inside the holding cell.
    /// </summary>
    public class AppaNPCInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private HoldingCell _cell;
        private bool _rescued;
        private Vector3 _basePos;

        public HoldingCell Cell
        {
            get
            {
                if (_cell == null)
                    _cell = GetComponentInParent<HoldingCell>() ?? HoldingCell.Instance ?? FindAnyObjectByType<HoldingCell>();
                return _cell;
            }
        }

        public void Configure(HoldingCell cell) { _cell = cell; }

        public bool CanInteract => Cell != null && Cell.IsUnlocked && !_rescued;

        public string Prompt
        {
            get
            {
                if (_rescued) return "Appa Rescued";
                var c = Cell;
                if (c != null && !c.IsUnlocked) return "[Locked behind security cell bars]";
                return "[E] Rescue Appa";
            }
        }

        public void Interact()
        {
            if (_rescued) return;
            var c = Cell;
            if (c != null)
            {
                _rescued = true;
                c.RescueAppa();
            }
        }

        private void Start()
        {
            _basePos = transform.localPosition;
        }

        private void Update()
        {
            // Gentle breathing / idle bobbing animation
            float t = Time.time * 2.2f;
            float bob = Mathf.Sin(t) * 0.015f;
            transform.localPosition = _basePos + new Vector3(0f, bob, 0f);
        }
    }
}
