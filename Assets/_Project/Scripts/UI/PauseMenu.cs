using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MistProtocol.Core;

namespace MistProtocol.UI
{
    /// <summary>
    /// Modal in-game Pause Menu triggered by pressing the Escape key.
    /// Freezes simulation time, displays options, and provides audio controls.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        private GameObject _pauseRoot;
        private GameObject _mainPanel;
        private bool _isPaused;

        public bool IsPaused => _isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                // Don't pause if player is on game over / win screen
                var gm = GameManager.Instance;
                if (gm != null && gm.State != GameManager.Flow.Playing) return;

                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_isPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_pauseRoot != null) _pauseRoot.SetActive(true);
            ProceduralAudio.PlayAt("beep", Vector3.zero, 0.4f, 1.2f);
        }

        public void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_pauseRoot != null) _pauseRoot.SetActive(false);
            ProceduralAudio.PlayAt("beep", Vector3.zero, 0.4f, 0.9f);
        }

        public void RestartMission()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _pauseRoot = new GameObject("Pause_Root", typeof(RectTransform));
            _pauseRoot.transform.SetParent(canvasGo.transform, false);
            var rt = _pauseRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Translucent dark background overlay
            var bgGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_pauseRoot.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

            // Centered Main Panel
            _mainPanel = new GameObject("Main_Panel", typeof(RectTransform));
            _mainPanel.transform.SetParent(_pauseRoot.transform, false);
            var pRt = _mainPanel.GetComponent<RectTransform>();
            pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(500f, 600f);

            // Title
            CreateText(_mainPanel.transform, "Title", "MISSION PAUSED", 36, new Color(0.35f, 0.85f, 1f), new Vector2(0f, 240f));

            // Buttons
            CreateButton(_mainPanel.transform, "ResumeBtn", "RESUME", new Vector2(0f, 150f), Resume);
            CreateButton(_mainPanel.transform, "RestartBtn", "RESTART MISSION", new Vector2(0f, 80f), RestartMission);
            CreateButton(_mainPanel.transform, "QuitBtn", "ABORT MISSION", new Vector2(0f, 10f), QuitGame);

            // Audio Volume Sliders
            CreateVolumeSlider(_mainPanel.transform, "MasterVol", "Master Volume", AudioListener.volume, new Vector2(0f, -80f), val =>
            {
                AudioListener.volume = val;
                PlayerPrefs.SetFloat("MasterVolume", val);
            });

            // Mouse Look Controls
            CreateValueSlider(_mainPanel.transform, "LookSens", "Look Sensitivity", PlayerPrefs.GetFloat("MouseSensitivity", 1f), 0.2f, 3.0f, new Vector2(0f, -130f), val =>
            {
                PlayerPrefs.SetFloat("MouseSensitivity", val);
            });

            CreateToggleOption(_mainPanel.transform, "InvertY", "Invert Y Axis", PlayerPrefs.GetInt("InvertY", 0) == 1, new Vector2(0f, -180f), val =>
            {
                PlayerPrefs.SetInt("InvertY", val ? 1 : 0);
            });

            _pauseRoot.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string text, int size, Color color, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(450f, 50f);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
            go.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 55f);
            go.GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.3f, 0.95f);

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(action);

            var tGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tGo.transform.SetParent(go.transform, false);
            var tRt = tGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
            var t = tGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = label;

            return btn;
        }

        private void CreateVolumeSlider(Transform parent, string name, string label, float initialValue, Vector2 pos, UnityEngine.Events.UnityAction<float> onChange)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 40f);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lblGo.transform.SetParent(row.transform, false);
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.5f, 1f);
            lRt.sizeDelta = Vector2.zero;
            var t = lblGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 15;
            t.color = new Color(0.8f, 0.9f, 1f);
            t.alignment = TextAnchor.MiddleLeft;
            t.text = label;

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.52f, 0.2f);
            sRt.anchorMax = new Vector2(1f, 0.8f);
            sRt.sizeDelta = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 0.8f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.2f, 0.7f, 1f, 1f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;
            slider.onValueChanged.AddListener(onChange);
        }

        private void CreateValueSlider(Transform parent, string name, string label, float initialValue, float min, float max, Vector2 pos, UnityEngine.Events.UnityAction<float> onChange)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 40f);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lblGo.transform.SetParent(row.transform, false);
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.5f, 1f);
            lRt.sizeDelta = Vector2.zero;
            var t = lblGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 15;
            t.color = new Color(0.8f, 0.9f, 1f);
            t.alignment = TextAnchor.MiddleLeft;
            t.text = label;

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.52f, 0.2f);
            sRt.anchorMax = new Vector2(1f, 0.8f);
            sRt.sizeDelta = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 0.8f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.3f, 0.85f, 0.6f, 1f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fRt;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initialValue;
            slider.onValueChanged.AddListener(onChange);
        }

        private void CreateToggleOption(Transform parent, string name, string label, bool initialValue, Vector2 pos, UnityEngine.Events.UnityAction<bool> onChange)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360f, 40f);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lblGo.transform.SetParent(row.transform, false);
            var lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.7f, 1f);
            lRt.sizeDelta = Vector2.zero;
            var t = lblGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 15;
            t.color = new Color(0.8f, 0.9f, 1f);
            t.alignment = TextAnchor.MiddleLeft;
            t.text = label;

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(row.transform, false);
            var tRt = toggleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.75f, 0.1f);
            tRt.anchorMax = new Vector2(0.95f, 0.9f);
            tRt.sizeDelta = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(toggleGo.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 0.8f);

            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(bg.transform, false);
            var cRt = checkmark.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.2f, 0.2f);
            cRt.anchorMax = new Vector2(0.8f, 0.8f);
            cRt.sizeDelta = Vector2.zero;
            checkmark.GetComponent<Image>().color = new Color(0.35f, 0.85f, 1f, 1f);

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = initialValue;
            toggle.onValueChanged.AddListener(onChange);
        }
    }
}
