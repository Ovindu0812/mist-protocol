using System.Collections.Generic;
using MistProtocol.Core;
using MistProtocol.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MistProtocol.UI
{
    /// <summary>Builds the whole uGUI HUD in code (no prefab/scene wiring needed) and listens to GameEvents.</summary>
    public class HUD : MonoBehaviour
    {
        private Font _font;
        private Text _health, _ammo, _objective, _prompt, _overseer, _zone, _center, _hitmarker;
        private Image _healthFill, _damage, _holdFill, _holdBack, _panel;
        private PlayerController _player;
        private PlayerWeapon _weapon;
        private PlayerInteractor _interactor;
        private float _overseerUntil, _damageAlpha;
        private readonly Queue<string> _overseerQueue = new();

        private static readonly Color Cyan = new Color(0.55f, 0.95f, 1f);
        private static readonly Color Amber = new Color(1f, 0.75f, 0.3f);

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            _damage = MakeImage(root, "Damage", new Color(0.8f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeText(root, "Crosshair", "+", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(60, 60), Vector2.zero);
            _hitmarker = MakeText(root, "Hit", "x", 30, new Color(1f, 0.3f, 0.2f, 0f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(60, 60), Vector2.zero);

            MakeImage(root, "HealthBack", new Color(0, 0, 0, 0.55f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(420, 70));
            _healthFill = MakeImage(root, "HealthFill", new Color(0.35f, 0.85f, 0.45f, 0.9f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 48), new Vector2(412, 62));
            _health = MakeText(root, "HealthText", "100", 30, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(360, 30), new Vector2(230, 55));
            _ammo = MakeText(root, "Ammo", "30 / 180", 44, Color.white, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(500, 70), new Vector2(-290, 70));

            _objective = MakeText(root, "Objective", "", 28, Amber, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1100, 80), new Vector2(590, -60));
            _zone = MakeText(root, "Zone", "", 52, new Color(1, 1, 1, 0), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.75f), new Vector2(1400, 80), Vector2.zero);
            _overseer = MakeText(root, "Overseer", "", 28, new Color(1f, 0.35f, 0.3f, 0f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(1500, 110), new Vector2(0, 190));
            _prompt = MakeText(root, "Prompt", "", 30, Cyan, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.42f), new Vector2(900, 50), Vector2.zero);
            _holdBack = MakeImage(root, "HoldBack", new Color(0, 0, 0, 0.6f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(-200, -8), new Vector2(200, 8));
            _holdFill = MakeImage(root, "HoldFill", Cyan, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(-198, -6), new Vector2(198, 6));

            _panel = MakeImage(root, "Panel", new Color(0.02f, 0.04f, 0.05f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _center = MakeText(_panel.transform, "PanelText", "", 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(1500, 950), Vector2.zero);

            GameEvents.ObjectiveChanged += t => _objective.text = "OBJECTIVE\n" + t;
            GameEvents.OverseerMessage += line => _overseerQueue.Enqueue(line);
            GameEvents.ZoneEntered += ShowZone;
            GameEvents.PlayerDamaged += amt => _damageAlpha = Mathf.Min(0.55f, _damageAlpha + amt / 50f);
        }

        public void ShowZone(string name)
        {
            _zone.text = name;
            _zone.color = new Color(1, 1, 1, 1.5f);
        }

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            if (_player != null)
            {
                _weapon = _player.GetComponentInChildren<PlayerWeapon>();
                _interactor = _player.GetComponentInChildren<PlayerInteractor>();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                _panel.enabled = gm.State != GameManager.Flow.Playing;
                _center.enabled = _panel.enabled;
                if (gm.State == GameManager.Flow.Intro) _center.text = GameManager.IntroText;
                else if (gm.State == GameManager.Flow.Won)
                    _center.text = "THE MIST CLEARS\n\nThe kill switch slams down. Every machine in Glenmist falls silent at once.\n" +
                                   "In the quiet, a radio crackles from the loft: \"...Nila? Is that you, duwa?\"\n\nAppa is alive.\n\n" +
                                   $"Robots destroyed: {gm.Kills}     Time: {Mathf.FloorToInt((gm.EndTime - gm.StartTime) / 60)}m {Mathf.FloorToInt((gm.EndTime - gm.StartTime) % 60)}s\n\nPress ENTER to play again";
                else if (gm.State == GameManager.Flow.Lost)
                    _center.text = "SIGNAL LOST\n\nThe mist swallows the factory floor. OVERSEER resumes production.\n\n" +
                                   $"Robots destroyed: {gm.Kills}\n\nPress ENTER to try again";
            }

            if (_player != null)
            {
                var h = _player.Health;
                _health.text = Mathf.CeilToInt(h.Current).ToString();
                var rt = _healthFill.rectTransform;
                rt.offsetMax = new Vector2(48 + 364 * h.Normalized, rt.offsetMax.y);
                _healthFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.15f, 0.9f), new Color(0.35f, 0.85f, 0.45f, 0.9f), h.Normalized);
            }
            if (_weapon != null)
                _ammo.text = _weapon.IsReloading ? "RELOADING" : $"{_weapon.InMagazine} / {_weapon.Reserve}";
            if (_weapon != null)
            {
                float since = Time.time - _weapon.LastHitTime;
                _hitmarker.color = new Color(1f, 0.3f, 0.2f, Mathf.Clamp01(1f - since * 6f));
            }

            string prompt = _interactor != null ? _interactor.Prompt : null;
            _prompt.text = prompt ?? "";
            bool hold = _interactor != null && _interactor.HoldProgress >= 0f;
            _holdBack.enabled = _holdFill.enabled = hold;
            if (hold)
            {
                var r = _holdFill.rectTransform;
                r.offsetMax = new Vector2(-198 + 396 * _interactor.HoldProgress, r.offsetMax.y);
            }

            _damageAlpha = Mathf.MoveTowards(_damageAlpha, 0f, dt * 0.8f);
            _damage.color = new Color(0.7f, 0f, 0f, _damageAlpha);
            var zc = _zone.color; zc.a = Mathf.MoveTowards(zc.a, 0f, dt * 0.4f); _zone.color = zc;

            if (Time.unscaledTime > _overseerUntil && _overseerQueue.Count > 0)
            {
                _overseer.text = _overseerQueue.Dequeue();
                _overseerUntil = Time.unscaledTime + 5.5f;
                ProceduralAudio.PlayAt("hum", _player != null ? _player.transform.position : Vector3.zero, 0.25f);
            }
            float a = Mathf.Clamp01((_overseerUntil - Time.unscaledTime) / 0.8f);
            _overseer.color = new Color(1f, 0.35f, 0.3f, a);
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

        private Text MakeText(Transform parent, string name, string text, int size, Color c, TextAnchor align, Vector2 anchor, Vector2 sizeDelta, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
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
