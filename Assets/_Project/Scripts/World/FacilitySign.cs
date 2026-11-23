using UnityEngine;
using UnityEngine.UI;

namespace MistProtocol.World
{
    /// <summary>
    /// Environmental signage for Glenmist Tea Research Facility.
    /// Procedurally creates backlit industrial signs with customizable warning labels,
    /// high-visibility colors, and optional subtle electrical hum / flicker.
    /// </summary>
    public class FacilitySign : MonoBehaviour
    {
        public enum SignTheme
        {
            LaboratoryCyan,
            BiohazardYellow,
            RestrictedRed,
            ExitGreen
        }

        [SerializeField] private string titleText = "RESEARCH SECTOR B";
        [SerializeField] private string subtitleText = "RESTRICTED ACCESS // AUTHORIZED PERSONNEL ONLY";
        [SerializeField] private SignTheme theme = SignTheme.LaboratoryCyan;
        [SerializeField] private bool enableBacklightFlicker = true;

        private Light _pointLight;
        private float _flickerOffset;

        private void Start()
        {
            _flickerOffset = Random.Range(0f, 100f);
            BuildSignBoard();
        }

        private void Update()
        {
            if (enableBacklightFlicker && _pointLight != null)
            {
                float noise = Mathf.PerlinNoise(_flickerOffset, Time.time * 6f);
                _pointLight.intensity = 1.0f + (noise > 0.8f ? 0.4f : (noise < 0.2f ? -0.3f : 0f));
            }
        }

        private void BuildSignBoard()
        {
            Color primaryColor = theme switch
            {
                SignTheme.LaboratoryCyan => new Color(0.2f, 0.8f, 1f),
                SignTheme.BiohazardYellow => new Color(1f, 0.8f, 0.1f),
                SignTheme.RestrictedRed => new Color(1f, 0.25f, 0.2f),
                SignTheme.ExitGreen => new Color(0.3f, 1f, 0.4f),
                _ => Color.white
            };

            // Backing plate
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Sign_Backing";
            plate.transform.SetParent(transform, false);
            plate.transform.localScale = new Vector3(2.4f, 0.9f, 0.08f);

            var plateRen = plate.GetComponent<MeshRenderer>();
            var plateMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            plateMat.color = new Color(0.08f, 0.1f, 0.12f);
            plateRen.sharedMaterial = plateMat;

            // In-world UI Canvas
            var canvasGo = new GameObject("Sign_Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480f, 180f);
            rt.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            // Title Text
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(Outline));
            titleGo.transform.SetParent(canvasGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 25f);
            titleRt.sizeDelta = new Vector2(460f, 60f);

            var title = titleGo.GetComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 32;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = primaryColor;
            title.text = titleText;

            var outline = titleGo.GetComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);

            // Subtitle Text
            var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(canvasGo.transform, false);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, -30f);
            subRt.sizeDelta = new Vector2(460f, 40f);

            var sub = subGo.GetComponent<Text>();
            sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sub.fontSize = 16;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            sub.text = subtitleText;

            // Optional glow light
            var lightGo = new GameObject("Sign_Backlight", typeof(Light));
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            _pointLight = lightGo.GetComponent<Light>();
            _pointLight.type = LightType.Point;
            _pointLight.color = primaryColor;
            _pointLight.range = 3.5f;
            _pointLight.intensity = 1.0f;
        }
    }
}
