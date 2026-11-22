using MistProtocol.Core;
using MistProtocol.Player;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Extraction / Escape Point in the Glenmist Yard (next to Nila's arrival Jeep).
    /// Displays a visible glowing extraction beacon and completes the mission when reached.
    /// </summary>
    public class EscapePoint : MonoBehaviour
    {
        [SerializeField] private GameObject beaconVisual;
        [SerializeField] private Light beaconLight;

        private bool _escaped;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(6f, 3.5f, 6f);
                col.isTrigger = true;
            }
            BuildBeaconVisualIfMissing();
        }

        public void SetBeaconActive(bool active)
        {
            if (beaconVisual != null) beaconVisual.SetActive(active);
            if (beaconLight != null) beaconLight.enabled = active;
        }

        private void BuildBeaconVisualIfMissing()
        {
            if (beaconVisual != null) return;

            beaconVisual = new GameObject("Extraction_Beacon_Visual");
            beaconVisual.transform.SetParent(transform, false);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Upward vertical light pillar
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "LightPillar";
            pillar.transform.SetParent(beaconVisual.transform, false);
            pillar.transform.localScale = new Vector3(0.35f, 8f, 0.35f);
            pillar.transform.localPosition = new Vector3(0f, 8f, 0f);
            var pCol = pillar.GetComponent<Collider>();
            if (pCol != null) Destroy(pCol);

            var pMat = new Material(litShader);
            pMat.SetColor("_BaseColor", new Color(0.2f, 0.95f, 0.5f, 0.6f));
            pMat.EnableKeyword("_EMISSION");
            pMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.5f) * 4f);
            pillar.GetComponent<Renderer>().material = pMat;

            // Ground target ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "GroundRing";
            ring.transform.SetParent(beaconVisual.transform, false);
            ring.transform.localScale = new Vector3(4.5f, 0.04f, 4.5f);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var rCol = ring.GetComponent<Collider>();
            if (rCol != null) Destroy(rCol);

            var rMat = new Material(litShader);
            rMat.SetColor("_BaseColor", new Color(0.2f, 0.95f, 0.5f, 0.8f));
            rMat.EnableKeyword("_EMISSION");
            rMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.5f) * 2.5f);
            ring.GetComponent<Renderer>().material = rMat;

            // Dynamic Point Light
            beaconLight = beaconVisual.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.range = 14f;
            beaconLight.intensity = 4.5f;
            beaconLight.color = new Color(0.2f, 1f, 0.55f);

            beaconVisual.SetActive(false); // Only active during escape
        }

        private void Update()
        {
            if (beaconVisual != null && beaconVisual.activeSelf)
            {
                // Pulsing extraction beacon
                float pulse = 0.85f + 0.3f * Mathf.Sin(Time.time * 6f);
                if (beaconLight != null) beaconLight.intensity = 4f * pulse;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_escaped) return;
            if (FinalRescueMission.Instance == null || !FinalRescueMission.Instance.IsEscaping) return;

            bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
            if (!isPlayer) return;

            _escaped = true;
            FinalRescueMission.Instance.OnEscapedSuccessfully();
        }
    }
}
