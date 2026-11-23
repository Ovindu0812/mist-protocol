using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace MistProtocol.World
{
    /// <summary>
    /// Interactive computer workstation terminal in Glenmist laboratories.
    /// Displays boot diagnostic data, security logs, and overrides security locks.
    /// </summary>
    public class ComputerTerminal : MonoBehaviour, IInteractable
    {
        public enum TerminalState { Locked, AccessGranted, Offline }

        [SerializeField] private string terminalId = "TERM-04";
        [SerializeField] private string securityLog = "FACILITY LOCKDOWN INITIATED BY OVERSEER\nALL SUB-LEVEL DOORS SEALED";
        [SerializeField] private TerminalState state = TerminalState.Locked;

        private Light _screenLight;
        private Text _screenText;
        private bool _isAccessed;

        public string Prompt => _isAccessed ? "[E] Log out of Terminal" : "[E] Access Security Terminal";
        public bool CanInteract => state != TerminalState.Offline;

        private void Start()
        {
            BuildTerminalUI();
        }

        public void Interact()
        {
            _isAccessed = !_isAccessed;
            if (_isAccessed)
            {
                state = TerminalState.AccessGranted;
                ProceduralAudio.PlayAt("terminalBoot", transform.position, 0.7f, 1.2f);
                if (_screenText != null)
                {
                    _screenText.text = $"[{terminalId}] OVERRIDE ACTIVE\n\n{securityLog}\n\nACCESS LEVEL: TOP SECRET";
                    _screenText.color = new Color(0.3f, 1f, 0.4f);
                }
                if (_screenLight != null)
                {
                    _screenLight.color = new Color(0.2f, 1f, 0.5f);
                }
            }
            else
            {
                ProceduralAudio.PlayAt("terminalBleep", transform.position, 0.5f, 0.9f);
                if (_screenText != null)
                {
                    _screenText.text = $"[{terminalId}] READY\nPRESS [E] TO LOGIN";
                    _screenText.color = new Color(0.2f, 0.8f, 1f);
                }
                if (_screenLight != null)
                {
                    _screenLight.color = new Color(0.2f, 0.75f, 1f);
                }
            }
        }

        private void BuildTerminalUI()
        {
            // CRT monitor housing
            var monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "CRT_Housing";
            monitor.transform.SetParent(transform, false);
            monitor.transform.localScale = new Vector3(0.8f, 0.6f, 0.5f);
            var mRen = monitor.GetComponent<MeshRenderer>();
            var mMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mMat.color = new Color(0.12f, 0.13f, 0.15f);
            mRen.sharedMaterial = mMat;

            // In-world Canvas
            var canvasGo = new GameObject("Terminal_Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.26f);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 240f);
            rt.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);

            // Screen Text
            var textGo = new GameObject("Terminal_Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = new Vector2(-20f, -20f);

            _screenText = textGo.GetComponent<Text>();
            _screenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _screenText.fontSize = 18;
            _screenText.alignment = TextAnchor.UpperLeft;
            _screenText.color = new Color(0.2f, 0.8f, 1f);
            _screenText.text = $"[{terminalId}] SYSTEM STANDBY\nPRESS [E] TO ACCESS";

            // Glow Light
            var lightGo = new GameObject("Screen_Glow", typeof(Light));
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            _screenLight = lightGo.GetComponent<Light>();
            _screenLight.type = LightType.Point;
            _screenLight.color = new Color(0.2f, 0.75f, 1f);
            _screenLight.range = 2.5f;
            _screenLight.intensity = 0.85f;
        }
    }
}
