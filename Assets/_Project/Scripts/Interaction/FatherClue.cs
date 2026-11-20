using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    public enum FatherClueType
    {
        DroppedBoot,
        BrokenPhone,
        SupervisorID,
        FactoryMap
    }

    /// <summary>
    /// Personal items dropped by Nila's father (Appa) marking the path to where he was taken.
    /// </summary>
    public class FatherClue : MonoBehaviour, IInteractable
    {
        [SerializeField] private FatherClueType clueType;
        [SerializeField] private Transform nextObjectiveTarget;
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float bobHeight = 0.04f;

        private Vector3 _startPos;
        private bool _inspected;
        private Light _glowLight;

        public bool CanInteract => !_inspected;

        public string Prompt => clueType switch
        {
            FatherClueType.DroppedBoot => "[E] Inspect Appa's dropped boot",
            FatherClueType.BrokenPhone => "[E] Inspect cracked radio-phone",
            FatherClueType.SupervisorID => "[E] Inspect Supervisor ID badge",
            FatherClueType.FactoryMap => "[E] Examine Appa's Factory Blueprint",
            _ => "[E] Inspect clue"
        };

        public void Configure(FatherClueType type, Transform nextTarget = null)
        {
            clueType = type;
            nextObjectiveTarget = nextTarget;
        }

        private void Awake()
        {
            _startPos = transform.position;
            _glowLight = GetComponentInChildren<Light>();
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true; // Clues must never physically block player walking
        }

        private void Update()
        {
            if (_inspected) return;

            // Gentle hover/pulse to draw attention in the dark
            float t = Time.time * bobSpeed;
            transform.position = _startPos + Vector3.up * (Mathf.Sin(t) * bobHeight);
            if (_glowLight != null)
            {
                _glowLight.intensity = 1.6f + Mathf.Sin(t * 1.8f) * 0.6f;
            }
        }

        public void Interact()
        {
            if (_inspected) return;
            _inspected = true;

            ProceduralAudio.PlayAt("pickup", transform.position, 0.7f, 1.2f);
            if (clueType == FatherClueType.BrokenPhone)
            {
                ProceduralAudio.PlayAt("zap", transform.position, 0.5f, 1.5f);
            }

            string dialogue = "";
            string objective = "";

            switch (clueType)
            {
                case FatherClueType.DroppedBoot:
                    dialogue = "NILA: Appa's work boot... It was torn off in a struggle. He was dragged further down this corridor. He's alive!";
                    objective = "Follow Appa's trail: Search the corridor for further signs";
                    break;

                case FatherClueType.BrokenPhone:
                    dialogue = "NILA: Appa's radio-phone! The screen is shattered, but the last outgoing call was to me. The trail continues north-east towards Sector 04!";
                    objective = "Follow Appa's trail: Head toward the heavy containment wing";
                    break;

                case FatherClueType.SupervisorID:
                    dialogue = "NILA: His Glenmist Supervisor ID badge! He deliberately threw it down here at this heavy door to mark where they dragged him. Appa must be in this room!";
                    objective = "Investigate the Heavy Containment Vault ahead";

                    // Ensure Sector E Containment door unlocks and swings open
                    var contDoor = GameObject.Find("Door_Containment");
                    if (contDoor != null)
                    {
                        var pd = contDoor.GetComponent<PhysicsDoor>();
                        if (pd != null)
                        {
                            pd.SetLocked(false);
                            pd.OpenAwayFrom(transform.position);
                        }
                    }
                    break;

                case FatherClueType.FactoryMap:
                    dialogue = "NILA: Appa's blueprint of Glenmist! He circled the Sub-Level Vault beneath Sector D with red ink: 'Nila, I'm locked in the Lower Reserve Vault. Flip the master kill switch in the Control Room to disengage the blast locks!' He's down there!";
                    objective = "PRIMARY OBJECTIVE: Proceed to Sector D Control Room and engage the kill switch to rescue Appa!";
                    break;
            }

            // Broadcast dialogue and objective
            GameEvents.Overseer(dialogue);
            GameEvents.Objective(objective);

            if (_glowLight != null)
                _glowLight.color = new Color(0.2f, 0.8f, 0.4f); // Turn soft green when inspected

            // Direct next objective if GameManager is listening
            if (nextObjectiveTarget != null && GameManager.Instance != null)
            {
                // We keep the player guided along the trail
            }
        }

        /// <summary>
        /// Spawns the interactive blueprint map dropped when the Titan Automaton is destroyed.
        /// </summary>
        public static GameObject SpawnMap(Vector3 position, Transform nextTarget = null)
        {
            var go = new GameObject("Clue_FactoryMap");
            go.transform.position = position;

            // Blueprint tablet
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "BlueprintDoc";
            sheet.transform.SetParent(go.transform, false);
            sheet.transform.localScale = new Vector3(0.48f, 0.02f, 0.34f);
            sheet.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var r = sheet.GetComponent<Renderer>();
            if (r != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader)
                {
                    color = new Color(0.12f, 0.42f, 0.78f)
                };
                r.material = mat;
            }
            Object.Destroy(sheet.GetComponent<Collider>());

            // Leather binder / metal clip
            var clip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clip.name = "Clip";
            clip.transform.SetParent(go.transform, false);
            clip.transform.localScale = new Vector3(0.08f, 0.035f, 0.36f);
            clip.transform.localPosition = new Vector3(-0.2f, 0.06f, 0f);
            Object.Destroy(clip.GetComponent<Collider>());

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.2f, 1.4f);
            col.center = new Vector3(0f, 0.2f, 0f);

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.35f;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 0.85f, 1f); // Vibrant cyan blueprint beacon
            l.intensity = 3.5f;
            l.range = 4.5f;
            l.shadows = LightShadows.None;

            var clue = go.AddComponent<FatherClue>();
            clue.Configure(FatherClueType.FactoryMap, nextTarget);
            return go;
        }
    }
}
