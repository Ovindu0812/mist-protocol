using System;
using MistProtocol.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// UI Manager for "The Final Rescue" mission:
    /// - Clue reading popup and progress tracker ("Clues Found: X/3")
    /// - Puzzle feedback and warning banners
    /// - Emergency evacuation 60-second countdown display
    /// - Mission Complete victory screen with Restart and Main Menu buttons
    /// - Mission Failed screen with Retry and Main Menu buttons
    /// </summary>
    public class FinalRescueUI : MonoBehaviour
    {
        public static FinalRescueUI Instance { get; private set; }

        private Canvas _canvas;
        private Font _font;

        // Clue popup elements
        private GameObject _clueModalGo;
        private Text _clueTitleText;
        private Text _clueBodyText;
        private Text _clueProgressText;
        private float _clueHideTime;

        // Clue HUD indicator
        private GameObject _clueHudGo;
        private Text _clueHudText;

        // Puzzle alert banner
        private GameObject _puzzleAlertGo;
        private Text _puzzleAlertText;
        private Image _puzzleAlertBack;
        private float _puzzleAlertHideTime;

        // Escape Timer HUD
        private GameObject _timerContainerGo;
        private Text _timerTitleText;
        private Text _timerDigitsText;
        private Image _timerBarFill;

        // Victory Screen
        private GameObject _victoryPanelGo;
        private Text _victoryTitleText;
        private Text _victorySubtitleText;
        private Text _victoryStatsText;
        private Button _victoryRestartBtn;
        private Button _victoryMenuBtn;

        // Mission Failed Screen
        private GameObject _failedPanelGo;
        private Text _failedTitleText;
        private Text _failedSubtitleText;
        private Button _failedRetryBtn;
        private Button _failedMenuBtn;

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("FinalRescue_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 110; // Overlay on top of gameplay HUD

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.transform;

            BuildClueModal(root);
            BuildClueHudTracker(root);
            BuildPuzzleAlertBanner(root);
            BuildEmergencyTimerHUD(root);
            BuildVictoryScreen(root);
            BuildFailedScreen(root);
        }

        // =========================================================================
        // 1. CLUE MODAL POPUP
        // =========================================================================
        private void BuildClueModal(Transform root)
        {
            _clueModalGo = new GameObject("Clue_Modal", typeof(RectTransform));
            _clueModalGo.transform.SetParent(root, false);
            var rt = _clueModalGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.65f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760f, 210f);

            // Border
            MakeImage(_clueModalGo.transform, "Border", new Color(0.2f, 0.75f, 1f, 0.95f), Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2));
            // Backing
            MakeImage(_clueModalGo.transform, "Backing", new Color(0.04f, 0.08f, 0.12f, 0.96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _clueTitleText = MakeText(_clueModalGo.transform, "Title", "CLUE DISCOVERED", 22, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(720, 36), new Vector2(0, -18), new Vector2(0.5f, 1f));
            _clueTitleText.fontStyle = FontStyle.Bold;

            _clueBodyText = MakeText(_clueModalGo.transform, "Message", "\"Blue comes before Green.\"", 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(700, 70), new Vector2(0, -6), new Vector2(0.5f, 0.5f));
            _clueBodyText.fontStyle = FontStyle.Italic;

            _clueProgressText = MakeText(_clueModalGo.transform, "Progress", "Clues Found: 1/3", 18, new Color(0.4f, 0.9f, 0.6f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(700, 30), new Vector2(0, 16), new Vector2(0.5f, 0f));

            _clueModalGo.SetActive(false);
        }

        // =========================================================================
        // 2. CLUE HUD TRACKER (Top-Left under objective)
        // =========================================================================
        private void BuildClueHudTracker(Transform root)
        {
            _clueHudGo = new GameObject("Clue_HUD_Tracker", typeof(RectTransform));
            _clueHudGo.transform.SetParent(root, false);
            var rt = _clueHudGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -140f);
            rt.sizeDelta = new Vector2(400f, 40f);

            _clueHudText = MakeText(_clueHudGo.transform, "ClueText", "LAB CLUES: 0 / 3", 22, new Color(0.35f, 0.95f, 0.7f), TextAnchor.MiddleLeft, Vector2.zero, new Vector2(400, 40), Vector2.zero, Vector2.zero);
            _clueHudText.fontStyle = FontStyle.Bold;

            _clueHudGo.SetActive(false);
        }

        // =========================================================================
        // 3. PUZZLE ALERT BANNER (Center Screen)
        // =========================================================================
        private void BuildPuzzleAlertBanner(Transform root)
        {
            _puzzleAlertGo = new GameObject("Puzzle_Alert_Banner", typeof(RectTransform));
            _puzzleAlertGo.transform.SetParent(root, false);
            var rt = _puzzleAlertGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.78f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(850f, 60f);

            _puzzleAlertBack = MakeImage(_puzzleAlertGo.transform, "Backing", new Color(0.7f, 0.05f, 0.05f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _puzzleAlertText = MakeText(_puzzleAlertGo.transform, "Text", "Incorrect sequence. Check the clues and try again.", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(820, 50), Vector2.zero, new Vector2(0.5f, 0.5f));
            _puzzleAlertText.fontStyle = FontStyle.Bold;

            _puzzleAlertGo.SetActive(false);
        }

        // =========================================================================
        // 4. EMERGENCY COUNTDOWN TIMER HUD (Top-Center)
        // =========================================================================
        private void BuildEmergencyTimerHUD(Transform root)
        {
            _timerContainerGo = new GameObject("Emergency_Timer_HUD", typeof(RectTransform));
            _timerContainerGo.transform.SetParent(root, false);
            var rt = _timerContainerGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(520f, 95f);

            // Crimson Glowing Backing
            MakeImage(_timerContainerGo.transform, "Border", new Color(1f, 0.2f, 0.15f, 0.95f), Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2));
            MakeImage(_timerContainerGo.transform, "Back", new Color(0.08f, 0.02f, 0.02f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _timerTitleText = MakeText(_timerContainerGo.transform, "Title", "CRITICAL WARNING: FACILITY COLLAPSE IMMINENT", 16, new Color(1f, 0.8f, 0.2f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(500, 24), new Vector2(0, -8), new Vector2(0.5f, 1f));
            _timerTitleText.fontStyle = FontStyle.Bold;

            _timerDigitsText = MakeText(_timerContainerGo.transform, "Digits", "01:00.00", 38, new Color(1f, 0.25f, 0.2f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(400, 44), new Vector2(0, -6), new Vector2(0.5f, 0.5f));
            _timerDigitsText.fontStyle = FontStyle.Bold;

            var troughGo = new GameObject("Trough", typeof(RectTransform), typeof(Image));
            troughGo.transform.SetParent(_timerContainerGo.transform, false);
            var troughRt = troughGo.GetComponent<RectTransform>();
            troughRt.anchorMin = troughRt.anchorMax = new Vector2(0.5f, 0f);
            troughRt.pivot = new Vector2(0.5f, 0f);
            troughRt.anchoredPosition = new Vector2(0f, 6f);
            troughRt.sizeDelta = new Vector2(490f, 10f);
            troughGo.GetComponent<Image>().color = new Color(0.2f, 0.05f, 0.05f, 0.85f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(troughGo.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(0f, 0f);
            fillRt.sizeDelta = new Vector2(490f, 10f);
            _timerBarFill = fillGo.GetComponent<Image>();
            _timerBarFill.color = new Color(1f, 0.25f, 0.15f);

            _timerContainerGo.SetActive(false);
        }

        // =========================================================================
        // 5. VICTORY SCREEN
        // =========================================================================
        private void BuildVictoryScreen(Transform root)
        {
            _victoryPanelGo = new GameObject("Victory_Panel", typeof(RectTransform));
            _victoryPanelGo.transform.SetParent(root, false);
            var rt = _victoryPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Fullscreen dark backdrop
            MakeImage(_victoryPanelGo.transform, "Backdrop", new Color(0.02f, 0.04f, 0.06f, 0.94f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Dialog Frame
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(_victoryPanelGo.transform, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(960f, 620f);

            MakeImage(frameGo.transform, "Border", new Color(0.95f, 0.78f, 0.25f, 0.9f), Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2));
            MakeImage(frameGo.transform, "Background", new Color(0.04f, 0.06f, 0.09f, 0.98f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _victoryTitleText = MakeText(frameGo.transform, "Title", "MISSION COMPLETE", 48, new Color(1f, 0.84f, 0.25f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(900, 60), new Vector2(0, -60), new Vector2(0.5f, 1f));
            _victoryTitleText.fontStyle = FontStyle.Bold;

            _victorySubtitleText = MakeText(frameGo.transform, "Subtitle", "Father Rescued. Facility Escaped.", 26, new Color(0.4f, 0.9f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(900, 40), new Vector2(0, -125), new Vector2(0.5f, 1f));
            _victorySubtitleText.fontStyle = FontStyle.Bold;

            string story = "With the energy barrier deactivated, Nila freed her father from the secret laboratory.\n" +
                           "Together, they outran the facility's self-destruct sequence and reached the jeep in the yard.\n" +
                           "As the Glenmist factory collapses behind them, father and daughter drive into the clearing morning mist.\n" +
                           "The tea hills are free.";

            MakeText(frameGo.transform, "Story", story, 20, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(840, 150), new Vector2(0, 20), new Vector2(0.5f, 0.5f));

            _victoryStatsText = MakeText(frameGo.transform, "Stats", "Robots Destroyed: --   Time: --", 18, new Color(0.7f, 0.8f, 0.85f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(800, 30), new Vector2(0, 130), new Vector2(0.5f, 0f));

            // Buttons
            _victoryRestartBtn = CreateButton(frameGo.transform, "RestartBtn", "PLAY AGAIN", new Vector2(-150f, 65f), () => RestartMission());
            _victoryMenuBtn = CreateButton(frameGo.transform, "MenuBtn", "MAIN MENU", new Vector2(150f, 65f), () => LoadMainMenu());

            _victoryPanelGo.SetActive(false);
        }

        // =========================================================================
        // 6. MISSION FAILED SCREEN
        // =========================================================================
        private void BuildFailedScreen(Transform root)
        {
            _failedPanelGo = new GameObject("Failed_Panel", typeof(RectTransform));
            _failedPanelGo.transform.SetParent(root, false);
            var rt = _failedPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            MakeImage(_failedPanelGo.transform, "Backdrop", new Color(0.12f, 0.02f, 0.02f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(_failedPanelGo.transform, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(880f, 480f);

            MakeImage(frameGo.transform, "Border", new Color(1f, 0.2f, 0.15f, 0.9f), Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2));
            MakeImage(frameGo.transform, "Background", new Color(0.06f, 0.03f, 0.03f, 0.98f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _failedTitleText = MakeText(frameGo.transform, "Title", "MISSION FAILED", 46, new Color(1f, 0.25f, 0.2f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(800, 60), new Vector2(0, -60), new Vector2(0.5f, 1f));
            _failedTitleText.fontStyle = FontStyle.Bold;

            _failedSubtitleText = MakeText(frameGo.transform, "Subtitle", "Facility Collapsed Before Evacuation", 24, new Color(1f, 0.7f, 0.6f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(800, 40), new Vector2(0, -125), new Vector2(0.5f, 1f));

            string failMsg = "The facility structural integrity reached 0%. You failed to reach the escape point before the blast locks sealed.\nTry again to rescue your father!";
            MakeText(frameGo.transform, "Desc", failMsg, 20, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(760, 100), new Vector2(0, 10), new Vector2(0.5f, 0.5f));

            _failedRetryBtn = CreateButton(frameGo.transform, "RetryBtn", "RETRY MISSION", new Vector2(-150f, 65f), () => RestartMission());
            _failedMenuBtn = CreateButton(frameGo.transform, "MenuBtn", "MAIN MENU", new Vector2(150f, 65f), () => LoadMainMenu());

            _failedPanelGo.SetActive(false);
        }

        // =========================================================================
        // PUBLIC API METHODS
        // =========================================================================
        public void ShowCluePopup(string clueTitle, string clueMessage, int foundCount, int totalCount = 3)
        {
            if (_clueModalGo == null) return;
            _clueTitleText.text = clueTitle.ToUpperInvariant();
            _clueBodyText.text = $"\"{clueMessage}\"";
            _clueProgressText.text = $"Clues Found: {foundCount}/{totalCount}";

            _clueModalGo.SetActive(true);
            _clueHideTime = Time.unscaledTime + 5.5f;

            UpdateClueHud(foundCount, totalCount);
        }

        public void UpdateClueHud(int foundCount, int totalCount = 3)
        {
            if (_clueHudGo != null)
            {
                _clueHudGo.SetActive(true);
                _clueHudText.text = $"LAB CLUES FOUND: {foundCount} / {totalCount}";
            }
        }

        public void HideClueHud()
        {
            if (_clueHudGo != null) _clueHudGo.SetActive(false);
        }

        public void ShowPuzzleIncorrectAlert(string message = "Incorrect sequence. Check the clues and try again.")
        {
            if (_puzzleAlertGo == null) return;
            _puzzleAlertBack.color = new Color(0.75f, 0.08f, 0.08f, 0.94f);
            _puzzleAlertText.text = message;
            _puzzleAlertGo.SetActive(true);
            _puzzleAlertHideTime = Time.unscaledTime + 3.5f;
        }

        public void ShowPuzzleSuccessAlert(string message = "Energy barrier deactivated! Rescue your father!")
        {
            if (_puzzleAlertGo == null) return;
            _puzzleAlertBack.color = new Color(0.08f, 0.65f, 0.25f, 0.94f);
            _puzzleAlertText.text = message;
            _puzzleAlertGo.SetActive(true);
            _puzzleAlertHideTime = Time.unscaledTime + 4.0f;
        }

        public void SetEmergencyTimerVisible(bool visible)
        {
            if (_timerContainerGo != null) _timerContainerGo.SetActive(visible);
        }

        public void UpdateEmergencyTimer(float remainingSeconds, float totalDuration = 60f)
        {
            if (_timerContainerGo == null) return;
            if (!_timerContainerGo.activeSelf) _timerContainerGo.SetActive(true);

            float clamped = Mathf.Max(0f, remainingSeconds);
            int mins = Mathf.FloorToInt(clamped / 60f);
            int secs = Mathf.FloorToInt(clamped % 60f);
            int ms = Mathf.FloorToInt((clamped * 100f) % 100f);

            _timerDigitsText.text = $"{mins:00}:{secs:00}.{ms:00}";

            float norm = Mathf.Clamp01(clamped / totalDuration);
            _timerBarFill.rectTransform.sizeDelta = new Vector2(490f * norm, 10f);

            if (clamped <= 15f)
            {
                float flash = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 14f);
                _timerDigitsText.color = new Color(1f, 0.1f, 0.1f, flash);
                _timerBarFill.color = new Color(1f, 0.1f, 0.1f);
            }
            else
            {
                _timerDigitsText.color = new Color(1f, 0.3f, 0.2f);
                _timerBarFill.color = new Color(1f, 0.4f, 0.2f);
            }
        }

        public void ShowVictoryScreen(int kills, float timeElapsed, int cluesFound)
        {
            if (_victoryPanelGo == null) return;
            _timerContainerGo?.SetActive(false);
            _clueHudGo?.SetActive(false);

            int mins = Mathf.FloorToInt(timeElapsed / 60f);
            int secs = Mathf.FloorToInt(timeElapsed % 60f);

            _victoryStatsText.text = $"Robots Destroyed: {kills}    Escape Time: {mins}m {secs}s    Clues Discovered: {cluesFound}/3";
            _victoryPanelGo.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ShowFailedScreen()
        {
            if (_failedPanelGo == null) return;
            _timerContainerGo?.SetActive(false);
            _clueHudGo?.SetActive(false);

            _failedPanelGo.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (_clueModalGo != null && _clueModalGo.activeSelf && Time.unscaledTime > _clueHideTime)
            {
                _clueModalGo.SetActive(false);
            }

            if (_puzzleAlertGo != null && _puzzleAlertGo.activeSelf && Time.unscaledTime > _puzzleAlertHideTime)
            {
                _puzzleAlertGo.SetActive(false);
            }

            // Keyboard navigation fallbacks for game over / win screens
            if (_victoryPanelGo != null && _victoryPanelGo.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) RestartMission();
            }
            else if (_failedPanelGo != null && _failedPanelGo.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) RestartMission();
            }
        }

        public static void RestartMission()
        {
            StaticReset.RunAll();
            AI.Agents.AgentBase.Active.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void LoadMainMenu()
        {
            StaticReset.RunAll();
            AI.Agents.AgentBase.Active.Clear();
            SceneManager.LoadScene(0);
        }

        // =========================================================================
        // UI HELPERS
        // =========================================================================
        private Image MakeImage(Transform parent, string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.offsetMin = oMin; r.offsetMax = oMax;
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        private Text MakeText(Transform parent, string name, string text, int size, Color c, TextAnchor align, Vector2 anchor, Vector2 sizeDelta, Vector2 pos, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.sizeDelta = sizeDelta;
            r.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Action onClick)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(240f, 54f);
            rt.anchoredPosition = pos;

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.12f, 0.45f, 0.75f, 0.95f);

            var btn = btnGo.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.45f, 0.75f, 0.95f);
            colors.highlightedColor = new Color(0.2f, 0.65f, 0.95f, 1f);
            colors.pressedColor = new Color(0.08f, 0.3f, 0.55f, 1f);
            btn.colors = colors;

            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = MakeText(btnGo.transform, "BtnText", label, 20, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(230, 48), Vector2.zero, new Vector2(0.5f, 0.5f));
            txt.fontStyle = FontStyle.Bold;

            return btn;
        }
    }
}
