using System;
using System.Collections.Generic;
using MistProtocol.Core;
using MistProtocol.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MistProtocol.UI
{
    /// <summary>
    /// Tactical In-Game HUD:
    /// - Top-Right: Glenmist Circular Tactical Minimap / Radar.
    /// - Top-Center: Titan Boss Health Bar with live draining animation (engages in combat).
    /// - Bottom-Left: Modern Military HP Gauge with live color transitions.
    /// - Bottom-Right: Tactical Ammo counter & Reload status.
    /// - Top-Left: Active Mission Objectives.
    /// - Center: Crosshair & dynamic hitmarkers.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        private Font _font;
        private Text _health, _ammo, _objective, _prompt, _overseer, _zone, _center, _hitmarker, _crosshairText;
        private Image _healthFill, _damage, _holdFill, _holdBack, _panel, _staminaFill;
        private CanvasGroup _staminaGroup;
        private PlayerController _player;
        private PlayerWeapon _weapon;
        private PlayerInteractor _interactor;
        private float _overseerUntil, _damageAlpha, _lastPlayedHitmarkerTime;
        private readonly Queue<string> _overseerQueue = new();

        // Titan Boss Health Bar elements
        private GameObject _bossContainerGo;
        private Image _bossBarFill;
        private Image _bossGhostFill;
        private Text _bossHpText;
        private Health _titanHealth;
        private float _displayedBossNorm = 1f;
        private float _displayedGhostNorm = 1f;
        private float _nextHeartbeat;

        private static readonly Color Cyan = new Color(0.45f, 0.9f, 1f);
        private static readonly Color Amber = new Color(1f, 0.78f, 0.25f);

        private void Awake()
        {
            try
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var canvasGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                var root = canvasGo.transform;

                // Damage vignette & Crosshairs
                _damage = MakeImage(root, "Damage", new Color(0.8f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _crosshairText = MakeText(root, "Crosshair", "+", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(80, 80), Vector2.zero, new Vector2(0.5f, 0.5f));
                _hitmarker = MakeText(root, "Hit", "x", 30, new Color(1f, 0.3f, 0.2f, 0f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(60, 60), Vector2.zero, new Vector2(0.5f, 0.5f));

                // =========================================================================
                // FULLSCREEN GAME STATE PANEL (Intro / Win / Loss) - Kept behind HUD items
                // =========================================================================
                _panel = MakeImage(root, "Panel", new Color(0.02f, 0.04f, 0.05f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _center = MakeText(_panel.transform, "PanelText", "", 32, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(1500, 950), Vector2.zero, new Vector2(0.5f, 0.5f));
                _panel.enabled = false;
                _center.enabled = false;

                // =========================================================================
                // TITAN BOSS HEALTH BAR (Top-Center) - Engages when near or attacking Titan
                // =========================================================================
                _bossContainerGo = new GameObject("Boss_Container", typeof(RectTransform));
                _bossContainerGo.transform.SetParent(root, false);
                var bossRt = _bossContainerGo.GetComponent<RectTransform>();
                bossRt.anchorMin = bossRt.anchorMax = new Vector2(0.5f, 1f);
                bossRt.pivot = new Vector2(0.5f, 1f);
                bossRt.anchoredPosition = new Vector2(0f, -28f);
                bossRt.sizeDelta = new Vector2(580f, 54f);

                // 1. Glowing crimson outer border
                MakeImage(_bossContainerGo.transform, "Boss_Border", new Color(1f, 0.35f, 0.15f, 0.9f), Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2));

                // 2. Dark armored metal backing
                MakeImage(_bossContainerGo.transform, "Boss_Back", new Color(0.04f, 0.05f, 0.08f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                // 3. Boss Title
                var bossTitleText = MakeText(_bossContainerGo.transform, "Boss_Title", "TITAN ENFORCER - UNIT 94", 17, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(560, 22), new Vector2(0, -10), new Vector2(0.5f, 1f));
                bossTitleText.fontStyle = FontStyle.Bold;

                // 4. Boss Bar Trough
                var bossTroughGo = new GameObject("Boss_Trough", typeof(RectTransform), typeof(Image));
                bossTroughGo.transform.SetParent(_bossContainerGo.transform, false);
                var bossTroughRt = bossTroughGo.GetComponent<RectTransform>();
                bossTroughRt.anchorMin = bossTroughRt.anchorMax = new Vector2(0.5f, 0f);
                bossTroughRt.pivot = new Vector2(0.5f, 0f);
                bossTroughRt.anchoredPosition = new Vector2(0f, 6f);
                bossTroughRt.sizeDelta = new Vector2(550f, 20f);
                bossTroughGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.98f);

                // 4.5 Ghost Damage-Lag Bar Fill (Amber trailing fill)
                var bossGhostGo = new GameObject("Boss_Ghost_Fill", typeof(RectTransform), typeof(Image));
                bossGhostGo.transform.SetParent(bossTroughGo.transform, false);
                var bossGhostRt = bossGhostGo.GetComponent<RectTransform>();
                bossGhostRt.anchorMin = bossGhostRt.anchorMax = new Vector2(0f, 0.5f);
                bossGhostRt.pivot = new Vector2(0f, 0.5f);
                bossGhostRt.anchoredPosition = new Vector2(2f, 0f);
                bossGhostRt.sizeDelta = new Vector2(546f, 16f);
                _bossGhostFill = bossGhostGo.GetComponent<Image>();
                _bossGhostFill.color = new Color(1f, 0.65f, 0.15f, 0.65f);
                _bossGhostFill.raycastTarget = false;

                // 5. Boss Bar Fill
                var bossFillGo = new GameObject("Boss_Fill", typeof(RectTransform), typeof(Image));
                bossFillGo.transform.SetParent(bossTroughGo.transform, false);
                var bossFillRt = bossFillGo.GetComponent<RectTransform>();
                bossFillRt.anchorMin = bossFillRt.anchorMax = new Vector2(0f, 0.5f);
                bossFillRt.pivot = new Vector2(0f, 0.5f);
                bossFillRt.anchoredPosition = new Vector2(2f, 0f);
                bossFillRt.sizeDelta = new Vector2(546f, 16f);
                _bossBarFill = bossFillGo.GetComponent<Image>();
                _bossBarFill.color = new Color(1f, 0.22f, 0.15f, 0.98f);
                _bossBarFill.raycastTarget = false;

                // 6. Boss HP Numeric Text
                _bossHpText = MakeText(bossTroughGo.transform, "Boss_HP_Number", "650 / 650 HP", 14, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(540, 20), Vector2.zero, new Vector2(0.5f, 0.5f));
                _bossHpText.fontStyle = FontStyle.Bold;

                _bossContainerGo.SetActive(false);

                // =========================================================================
                // MODERN TACTICAL HEALTH BAR (Bottom-Left: Positioned safely above bottom edge)
                // =========================================================================
                var hpContainerGo = new GameObject("HP_Container", typeof(RectTransform));
                hpContainerGo.transform.SetParent(root, false);
                var hpContainer = hpContainerGo.GetComponent<RectTransform>();
                hpContainer.anchorMin = hpContainer.anchorMax = new Vector2(0f, 0f);
                hpContainer.pivot = new Vector2(0f, 0f);
                hpContainer.anchoredPosition = new Vector2(50f, 85f);
                hpContainer.sizeDelta = new Vector2(400f, 62f);

                // 1. Glowing Cyan Outer Border
                MakeImage(hpContainer, "HP_Border", new Color(0.3f, 0.85f, 1f, 0.9f), Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2));

                // 2. Dark glass background
                MakeImage(hpContainer, "HP_Back", new Color(0.03f, 0.06f, 0.1f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                // 3. HP Badge
                var badgeGo = new GameObject("HP_Badge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(hpContainer, false);
                var badgeRt = badgeGo.GetComponent<RectTransform>();
                badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0f, 0.5f);
                badgeRt.pivot = new Vector2(0f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(8f, 0f);
                badgeRt.sizeDelta = new Vector2(50f, 44f);
                badgeGo.GetComponent<Image>().color = new Color(0.12f, 0.45f, 0.65f, 0.95f);
                var badgeText = MakeText(badgeGo.transform, "HP_Text", "HP", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(50, 44), Vector2.zero, new Vector2(0.5f, 0.5f));
                badgeText.fontStyle = FontStyle.Bold;

                // 4. Bar Trough (Dark groove)
                var troughGo = new GameObject("HP_Trough", typeof(RectTransform), typeof(Image));
                troughGo.transform.SetParent(hpContainer, false);
                var troughRt = troughGo.GetComponent<RectTransform>();
                troughRt.anchorMin = troughRt.anchorMax = new Vector2(0f, 0.5f);
                troughRt.pivot = new Vector2(0f, 0.5f);
                troughRt.anchoredPosition = new Vector2(66f, 0f);
                troughRt.sizeDelta = new Vector2(322f, 30f);
                troughGo.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.06f, 0.95f);

                // 5. Bar Fill (Vibrant green/amber/red indicator)
                var fillGo = new GameObject("HP_Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(troughGo.transform, false);
                var fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.anchorMin = fillRt.anchorMax = new Vector2(0f, 0.5f);
                fillRt.pivot = new Vector2(0f, 0.5f);
                fillRt.anchoredPosition = new Vector2(2f, 0f);
                fillRt.sizeDelta = new Vector2(318f, 26f);
                _healthFill = fillGo.GetComponent<Image>();
                _healthFill.color = new Color(0.25f, 0.95f, 0.45f, 0.95f);
                _healthFill.raycastTarget = false;

                // 6. Numeric HP Readout overlaid on the bar
                _health = MakeText(troughGo.transform, "HP_Number", "100 / 100", 18, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(318, 30), Vector2.zero, new Vector2(0.5f, 0.5f));
                _health.fontStyle = FontStyle.Bold;

                // 7. Stamina Bar Trough & Fill directly below HP
                var stamGo = new GameObject("Stamina_Container", typeof(RectTransform), typeof(CanvasGroup));
                stamGo.transform.SetParent(hpContainer, false);
                _staminaGroup = stamGo.GetComponent<CanvasGroup>();
                var sRt = stamGo.GetComponent<RectTransform>();
                sRt.anchorMin = sRt.anchorMax = new Vector2(0f, 0.5f);
                sRt.pivot = new Vector2(0f, 0.5f);
                sRt.anchoredPosition = new Vector2(66f, -22f);
                sRt.sizeDelta = new Vector2(322f, 6f);

                var stamBg = new GameObject("Stamina_Bg", typeof(RectTransform), typeof(Image));
                stamBg.transform.SetParent(stamGo.transform, false);
                var sBgRt = stamBg.GetComponent<RectTransform>();
                sBgRt.anchorMin = Vector2.zero;
                sBgRt.anchorMax = Vector2.one;
                sBgRt.sizeDelta = Vector2.zero;
                stamBg.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.08f, 0.85f);

                var stamFillGo = new GameObject("Stamina_Fill", typeof(RectTransform), typeof(Image));
                stamFillGo.transform.SetParent(stamGo.transform, false);
                var sFillRt = stamFillGo.GetComponent<RectTransform>();
                sFillRt.anchorMin = sFillRt.anchorMax = new Vector2(0f, 0.5f);
                sFillRt.pivot = new Vector2(0f, 0.5f);
                sFillRt.anchoredPosition = Vector2.zero;
                sFillRt.sizeDelta = new Vector2(322f, 6f);
                _staminaFill = stamFillGo.GetComponent<Image>();
                _staminaFill.color = new Color(0.2f, 0.85f, 1f, 0.95f);
                _staminaFill.raycastTarget = false;

                // =========================================================================
                // AMMO COUNTER (Bottom-Right: Positioned safely above bottom edge)
                // =========================================================================
                _ammo = MakeText(root, "Ammo", "30 / 180", 40, Color.white, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(500, 70), new Vector2(-50f, 85f), new Vector2(1f, 0f));

                // =========================================================================
                // OBJECTIVES & OVERSEER (Top-Left & Center)
                // =========================================================================
                _objective = MakeText(root, "Objective", "", 26, Amber, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(650, 90), new Vector2(40, -40), new Vector2(0f, 1f));
                _zone = MakeText(root, "Zone", "", 52, new Color(1, 1, 1, 0), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.75f), new Vector2(1400, 80), Vector2.zero, new Vector2(0.5f, 0.5f));
                _overseer = MakeText(root, "Overseer", "", 28, new Color(1f, 0.35f, 0.3f, 0f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(1500, 110), new Vector2(0, 200), new Vector2(0.5f, 0.5f));
                _prompt = MakeText(root, "Prompt", "", 30, Cyan, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.42f), new Vector2(900, 50), Vector2.zero, new Vector2(0.5f, 0.5f));
                _holdBack = MakeImage(root, "HoldBack", new Color(0, 0, 0, 0.6f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(-200, -8), new Vector2(200, 8));
                _holdFill = MakeImage(root, "HoldFill", Cyan, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(-198, -6), new Vector2(198, 6));

                // =========================================================================
                // TOP-RIGHT CIRCULAR TACTICAL MINIMAP
                // =========================================================================
                gameObject.AddComponent<Minimap>().Initialize(root);

                GameEvents.ObjectiveChanged += t => _objective.text = "OBJECTIVE\n" + t;
                GameEvents.OverseerMessage += line => _overseerQueue.Enqueue(line);
                GameEvents.ZoneEntered += ShowZone;
                GameEvents.PlayerDamaged += amt => _damageAlpha = Mathf.Min(0.55f, _damageAlpha + amt / 50f);

                Debug.Log("[HUD] Initialized successfully with Minimap and Health Bar.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[HUD Awake Exception] " + ex);
            }
        }

        public void ShowZone(string name)
        {
            _zone.text = name;
            _zone.color = new Color(1, 1, 1, 1.5f);
        }

        private void Start()
        {
            ResolvePlayerReferences();
        }

        private void ResolvePlayerReferences()
        {
            if (_player != null) return;

            if (PlayerLocator.State != null && PlayerLocator.State.Root != null)
            {
                _player = PlayerLocator.State.Root.GetComponent<PlayerController>();
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
            }

            if (_player != null)
            {
                _weapon = _player.GetComponentInChildren<PlayerWeapon>();
                _interactor = _player.GetComponentInChildren<PlayerInteractor>();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (_player == null)
            {
                ResolvePlayerReferences();
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                bool showPanel = gm.State != GameManager.Flow.Playing;
                _panel.enabled = showPanel;
                _center.enabled = showPanel;
                if (showPanel)
                {
                    if (gm.State == GameManager.Flow.Intro) _center.text = GameManager.IntroText;
                    else if (gm.State == GameManager.Flow.Won)
                    {
                        string title = gm.IsAppaRescued ? "FAMILY REUNITED" : "THE MIST CLEARS";
                        string story = gm.IsAppaRescued
                            ? "With the Master Key in hand, Nila unlocks the heavy containment cell.\nAppa embraces her with tears in his eyes: \"...Nila, my brave duwa. You saved me.\"\nTogether, hand in hand, they step out into the clearing morning mist."
                            : "The kill switch slams down. Every machine in Glenmist falls silent at once.\nIn the quiet, a radio crackles from the loft: \"...Nila? Is that you, duwa?\"\nAppa is alive.";

                        string rank = gm.CalculateMissionRank(out int score);
                        _center.text = $"{title}\n\n{story}\n\n" +
                                       $"TACTICAL EVALUATION: [ RANK {rank} ]    SCORE: {score}\n" +
                                       $"Robots destroyed: {gm.Kills}     Time: {Mathf.FloorToInt((gm.EndTime - gm.StartTime) / 60)}m {Mathf.FloorToInt((gm.EndTime - gm.StartTime) % 60)}s\n\nPress ENTER to play again";
                    }
                    else if (gm.State == GameManager.Flow.Lost)
                        _center.text = "SIGNAL LOST\n\nThe mist swallows the factory floor. OVERSEER resumes production.\n\n" +
                                       $"Robots destroyed: {gm.Kills}\n\nPress ENTER to try again";
                }
            }

            // Update Player Health Bar
            if (_player != null && _player.Health != null && _healthFill != null)
            {
                var h = _player.Health;
                float norm = Mathf.Clamp01(h.Normalized);
                if (_health != null) _health.text = $"{Mathf.CeilToInt(h.Current)} / {Mathf.CeilToInt(h.Max)}";

                // Responsive width: 318px max fill
                var rt = _healthFill.rectTransform;
                rt.sizeDelta = new Vector2(318f * norm, 26f);

                // Dynamic gradient color: Green (>50%) -> Amber (25-50%) -> Pulsing Red (<25%)
                if (norm > 0.5f)
                {
                    _healthFill.color = Color.Lerp(new Color(1f, 0.85f, 0.2f, 0.95f), new Color(0.25f, 0.95f, 0.45f, 0.95f), (norm - 0.5f) * 2f);
                }
                else if (norm > 0.25f)
                {
                    _healthFill.color = Color.Lerp(new Color(1f, 0.25f, 0.15f, 0.95f), new Color(1f, 0.85f, 0.2f, 0.95f), (norm - 0.25f) * 4f);
                }
                else
                {
                    float flash = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 12f);
                    _healthFill.color = new Color(1f, 0.15f, 0.1f, flash);
                }
            }

            // Update Stamina Bar
            if (_player != null && _staminaFill != null)
            {
                float stamNorm = Mathf.Clamp01(_player.CurrentStamina / Mathf.Max(1f, _player.MaxStamina));
                _staminaFill.rectTransform.sizeDelta = new Vector2(322f * stamNorm, 6f);
                if (_player.IsExhausted)
                {
                    float flash = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 14f);
                    _staminaFill.color = new Color(1f, 0.3f, 0.15f, flash);
                }
                else
                {
                    _staminaFill.color = new Color(0.2f, 0.85f, 1f, 0.95f);
                }

                if (_staminaGroup != null)
                {
                    float targetAlpha = (stamNorm < 0.98f || _player.IsExhausted) ? 1.0f : 0.0f;
                    _staminaGroup.alpha = Mathf.MoveTowards(_staminaGroup.alpha, targetAlpha, dt * 2.5f);
                }
            }

            // Update Titan Boss Health Bar
            UpdateBossHealthBar(dt);

            // Update Ammo Display
            if (_weapon != null && _ammo != null)
            {
                string status = _weapon.IsReloading ? "RELOADING..." : $"{_weapon.InMagazine} / {_weapon.Reserve}";
                _ammo.text = $"{_weapon.ActiveWeaponName}\n<size=26>{status}</size>";
                if (_weapon.IsLowAmmo && !_weapon.IsReloading)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
                    _ammo.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.2f), pulse);
                }
                else
                {
                    _ammo.color = Color.white;
                }
                float since = Time.time - _weapon.LastHitTime;
                if (_hitmarker != null) _hitmarker.color = new Color(1f, 0.3f, 0.2f, Mathf.Clamp01(1f - since * 6f));
                if (since < 0.08f && _lastPlayedHitmarkerTime < _weapon.LastHitTime)
                {
                    _lastPlayedHitmarkerTime = _weapon.LastHitTime;
                    ProceduralAudio.PlayAt("impact", _player != null ? _player.transform.position : Vector3.zero, 0.45f, 1.85f);
                }
                if (_crosshairText != null)
                {
                    float bloom = _weapon.CurrentBloom;
                    _crosshairText.fontSize = Mathf.RoundToInt(34 + bloom * 16f);
                }
            }

            string prompt = _interactor != null ? _interactor.Prompt : null;
            if (_prompt != null) _prompt.text = prompt ?? "";
            bool hold = _interactor != null && _interactor.HoldProgress >= 0f;
            if (_holdBack != null && _holdFill != null)
            {
                _holdBack.enabled = _holdFill.enabled = hold;
                if (hold)
                {
                    var r = _holdFill.rectTransform;
                    r.offsetMax = new Vector2(-198 + 396 * _interactor.HoldProgress, r.offsetMax.y);
                }
            }

            _damageAlpha = Mathf.MoveTowards(_damageAlpha, 0f, dt * 0.8f);
            if (_damage != null) _damage.color = new Color(0.7f, 0f, 0f, _damageAlpha);

            // Critical Low-HP Rhythmic Heartbeat & Vignette Pulse
            if (_player != null && _player.Health != null && !_player.Health.IsDead && _player.Health.Normalized <= 0.25f)
            {
                if (Time.unscaledTime > _nextHeartbeat)
                {
                    _nextHeartbeat = Time.unscaledTime + 0.95f;
                    ProceduralAudio.PlayAt("heartbeat", _player.transform.position, 1.2f, 1.0f);
                }
                float pulse = 0.15f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.35f);
                if (_damage != null) _damage.color = new Color(0.8f, 0f, 0f, Mathf.Max(_damageAlpha, pulse));
            }

            // Low Ammo Warning Flash
            if (_weapon != null && _ammo != null)
            {
                if (_weapon.IsLowAmmo)
                {
                    _ammo.color = Mathf.PingPong(Time.unscaledTime * 4f, 1f) > 0.5f ? new Color(1f, 0.25f, 0.2f) : Color.white;
                }
                else
                {
                    _ammo.color = Color.white;
                }
            }
            if (_zone != null)
            {
                var zc = _zone.color; zc.a = Mathf.MoveTowards(zc.a, 0f, dt * 0.4f); _zone.color = zc;
            }

            if (Time.unscaledTime > _overseerUntil && _overseerQueue.Count > 0)
            {
                if (_overseer != null) _overseer.text = _overseerQueue.Dequeue();
                _overseerUntil = Time.unscaledTime + 5.5f;
                Vector3 soundPos = _player != null ? _player.transform.position : Vector3.zero;
                ProceduralAudio.PlayAt("radioChirp", soundPos, 0.4f, 1.1f);
                ProceduralAudio.PlayAt("radioStatic", soundPos, 0.25f, 1.0f);
            }
            if (_overseer != null)
            {
                float a = Mathf.Clamp01((_overseerUntil - Time.unscaledTime) / 0.8f);
                _overseer.color = new Color(1f, 0.35f, 0.3f, a);
            }
        }

        private void UpdateBossHealthBar(float dt)
        {
            if (_titanHealth == null)
            {
                var titan = FindAnyObjectByType<AI.Agents.TitanBossBrain>(FindObjectsInactive.Exclude);
                if (titan != null)
                {
                    _titanHealth = titan.GetComponent<Health>();
                }
            }

            bool showBossBar = false;
            if (_titanHealth != null && !_titanHealth.IsDead && _titanHealth.gameObject.activeInHierarchy)
            {
                float dist = _player != null ? Vector3.Distance(_player.transform.position, _titanHealth.transform.position) : 999f;
                bool isEngaged = _titanHealth.Current < _titanHealth.Max || dist < 32f || (GameManager.Instance != null && GameManager.Instance.IsKillSwitchPulled);
                showBossBar = isEngaged;
            }

            if (_bossContainerGo != null && _bossContainerGo.activeSelf != showBossBar)
            {
                _bossContainerGo.SetActive(showBossBar);
            }

            if (showBossBar && _bossBarFill != null && _titanHealth != null)
            {
                float targetNorm = Mathf.Clamp01(_titanHealth.Normalized);
                _displayedBossNorm = targetNorm;
                _displayedGhostNorm = Mathf.MoveTowards(_displayedGhostNorm, targetNorm, dt * 0.45f);

                var rt = _bossBarFill.rectTransform;
                rt.sizeDelta = new Vector2(546f * _displayedBossNorm, 16f);

                if (_bossGhostFill != null)
                {
                    _bossGhostFill.rectTransform.sizeDelta = new Vector2(546f * _displayedGhostNorm, 16f);
                }

                // Color gradient: Vibrant orange-crimson -> Blood red when low
                if (_displayedBossNorm > 0.45f)
                    _bossBarFill.color = Color.Lerp(new Color(1f, 0.55f, 0.1f, 0.98f), new Color(1f, 0.22f, 0.15f, 0.98f), (1f - _displayedBossNorm) * 1.8f);
                else
                    _bossBarFill.color = Color.Lerp(new Color(0.85f, 0.1f, 0.1f, 0.98f), new Color(1f, 0.55f, 0.1f, 0.98f), _displayedBossNorm * 2.2f);

                if (_bossHpText != null)
                {
                    _bossHpText.text = $"{Mathf.CeilToInt(_titanHealth.Current)} / {Mathf.CeilToInt(_titanHealth.Max)} HP";
                }
            }
        }

        private Image MakeImage(Transform parent, string name, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        private Text MakeText(Transform parent, string name, string text, int size, Color c, TextAnchor align, Vector2 anchor, Vector2 sizeDelta, Vector2 pos, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = c; t.alignment = align; t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            go.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
            return t;
        }
    }
}
