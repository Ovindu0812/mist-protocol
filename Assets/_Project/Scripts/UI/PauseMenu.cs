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
            CreateText(_mainPanel.transform, "Title", "MISSION PAUSED", 36, new Color(0.35f, 0.85f, 1f), new Vector2(0f, 220f));

            // Buttons
            CreateButton(_mainPanel.transform, "ResumeBtn", "RESUME", new Vector2(0f, 120f), Resume);
            CreateButton(_mainPanel.transform, "RestartBtn", "RESTART MISSION", new Vector2(0f, 40f), RestartMission);
            CreateButton(_mainPanel.transform, "QuitBtn", "ABORT MISSION", new Vector2(0f, -40f), QuitGame);

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
    }
}
