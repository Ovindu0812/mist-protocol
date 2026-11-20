using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Master Key dropped by the Titan Automaton upon its destruction.
    /// Unlocks the high-security Holding Cell in Sector E where Appa is trapped.
    /// </summary>
    public class MasterCellKey : MonoBehaviour, IInteractable
    {
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 _startPos;
        private bool _collected;
        private Light _glowLight;

        public bool CanInteract => !_collected;
        public string Prompt => "[E] Pick up Master Prison Cell Key";

        public static GameObject SpawnKey(Vector3 position)
        {
            var go = new GameObject("Clue_MasterCellKey");
            go.transform.position = position + Vector3.up * 0.25f;

            // Physical keycard tablet device
            var keyBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyBody.name = "KeyMesh";
            keyBody.transform.SetParent(go.transform, false);
            keyBody.transform.localScale = new Vector3(0.28f, 0.04f, 0.42f);
            keyBody.transform.localRotation = Quaternion.Euler(20f, 45f, 0f);

            var r = keyBody.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", new Color(0.95f, 0.75f, 0.2f));
                mat.SetFloat("_Metallic", 0.9f);
                mat.SetFloat("_Smoothness", 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 3f);
                r.material = mat;
            }

            // Upward glowing light beam beacon
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "LightBeam";
            beam.transform.SetParent(go.transform, false);
            beam.transform.localScale = new Vector3(0.08f, 2.5f, 0.08f);
            beam.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var beamCol = beam.GetComponent<Collider>();
            if (beamCol != null) Object.Destroy(beamCol);

            var beamR = beam.GetComponent<Renderer>();
            if (beamR != null)
            {
                var bMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                bMat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f, 0.6f));
                bMat.EnableKeyword("_EMISSION");
                bMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.3f) * 4f);
                beamR.material = bMat;
            }

            // Dynamic Point Light
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8f;
            l.intensity = 3.5f;
            l.color = new Color(1f, 0.82f, 0.3f);

            var key = go.AddComponent<MasterCellKey>();

            // Ensure collider for raycast interaction
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.8f, 1.2f, 0.8f);
            box.isTrigger = true;

            // Direct objective immediately to the key
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCustomObjective(go.transform, "TITAN DESTROYED: Pick up the Master Cell Key from Titan's wreckage!");
            }

            return go;
        }

        private void Awake()
        {
            _startPos = transform.position;
            _glowLight = GetComponent<Light>();
        }

        private void Update()
        {
            if (_collected) return;

            float t = Time.time * bobSpeed;
            transform.position = _startPos + Vector3.up * (Mathf.Sin(t) * bobHeight);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            if (_glowLight != null)
            {
                _glowLight.intensity = 3f + Mathf.Sin(t * 1.5f) * 1.2f;
            }
        }

        public void Interact()
        {
            if (_collected) return;
            _collected = true;

            ProceduralAudio.PlayAt("pickup", transform.position, 1f, 1.3f);
            ProceduralAudio.PlayAt("zap", transform.position, 0.6f, 1.8f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMasterCellKeyCollected();
            }

            // Radio transmission from Appa
            GameEvents.Overseer("APPA (Radio): \"...Nila?! Duwa, is that you?! I heard the Titan collapse! I'm locked inside the holding cell against the east wall! Use the Master Key on the terminal!\"");

            // Direct objective to the holding cell terminal
            var cell = HoldingCell.Instance != null ? HoldingCell.Instance : FindAnyObjectByType<HoldingCell>();
            if (cell != null && GameManager.Instance != null)
            {
                GameManager.Instance.SetCustomObjective(cell.TerminalTransform, "RESCUE APPA: Swipe Master Key on Holding Cell Console (East Wall)!");
            }
            else
            {
                GameEvents.Objective("RESCUE APPA: Swipe Master Key on Holding Cell Console at the East Wall!");
            }

            Destroy(gameObject, 0.15f);
        }
    }
}
