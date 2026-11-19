using System.Collections.Generic;
using MistProtocol.AI.Agents;
using MistProtocol.Core;
using MistProtocol.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MistProtocol.UI
{
    /// <summary>
    /// Tactical Circular Minimap / Radar HUD for Glenmist Tea Factory (Top-Right).
    /// Displays:
    /// - Glowing circular frame with compass North indicator.
    /// - Live scrolling tactical blueprint of the Glenmist Factory floor plan.
    /// - Central Player chevron with real-time look orientation.
    /// - Real-time radar blips for active robots, drones, and the Titan Boss.
    /// - Objective waypoint marker pointing toward the active mission target.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        private const float RadarRadius = 95f;        // UI pixels radius
        private const float WorldViewRange = 36f;      // World meters visible from center to edge
        private const float WorldToUIRatio = RadarRadius / WorldViewRange; // ~2.64 px per meter

        // Factory bounds in world coordinates (from LevelBuilder)
        private const float MapMinX = -32f;
        private const float MapMaxX = 56f;
        private const float MapMinZ = -10f;
        private const float MapMaxZ = 38f;
        private const float MapWorldWidth = MapMaxX - MapMinX;   // 88m
        private const float MapWorldHeight = MapMaxZ - MapMinZ;  // 48m

        private RectTransform _rootRect;
        private RawImage _mapImage;
        private RectTransform _mapRect;
        private RectTransform _playerMarker;
        private RectTransform _objectiveMarker;
        private Text _objectiveDistText;
        private Text _northText;

        private Texture2D _mapTexture;
        private Texture2D _radarRingTexture;
        private Texture2D _playerArrowTexture;
        private Texture2D _enemyBlipTexture;
        private Texture2D _bossBlipTexture;
        private Texture2D _objectiveIconTexture;

        private readonly List<Image> _blipPool = new();
        private PlayerController _player;

        public static Minimap Instance { get; private set; }

        public void Initialize(Transform canvasRoot)
        {
            try
            {
                Instance = this;

                // Generate procedural textures
                BuildTextures();

                // Root container at Top-Right
                var rootGo = new GameObject("Minimap_Root", typeof(RectTransform));
                rootGo.transform.SetParent(canvasRoot, false);
                _rootRect = rootGo.GetComponent<RectTransform>();
                _rootRect.anchorMin = _rootRect.anchorMax = new Vector2(1f, 1f);
                _rootRect.pivot = new Vector2(1f, 1f);
                _rootRect.anchoredPosition = new Vector2(-35f, -35f);
                _rootRect.sizeDelta = new Vector2(RadarRadius * 2f + 20f, RadarRadius * 2f + 20f);

                // Tactical Dark Background
                var bgGo = new GameObject("Radar_BG", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(_rootRect, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
                bgRt.sizeDelta = new Vector2(RadarRadius * 2f, RadarRadius * 2f);
                bgGo.GetComponent<Image>().color = new Color(0.03f, 0.07f, 0.12f, 0.92f);

                // Mask Container (Clips map and blips) using RectMask2D (100% URP compatible)
                var maskGo = new GameObject("Radar_Mask", typeof(RectTransform), typeof(RectMask2D));
                maskGo.transform.SetParent(_rootRect, false);
                var maskRt = maskGo.GetComponent<RectTransform>();
                maskRt.anchorMin = maskRt.anchorMax = new Vector2(0.5f, 0.5f);
                maskRt.pivot = new Vector2(0.5f, 0.5f);
                maskRt.sizeDelta = new Vector2(RadarRadius * 2f, RadarRadius * 2f);

            // Scrolling Factory Floor Blueprint Image inside the mask
            var mapGo = new GameObject("Factory_Blueprint", typeof(RectTransform), typeof(RawImage));
            mapGo.transform.SetParent(maskGo.transform, false);
            _mapRect = mapGo.GetComponent<RectTransform>();
            _mapRect.anchorMin = _mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            float uiMapW = MapWorldWidth * WorldToUIRatio;
            float uiMapH = MapWorldHeight * WorldToUIRatio;
            _mapRect.sizeDelta = new Vector2(uiMapW, uiMapH);
            _mapImage = mapGo.GetComponent<RawImage>();
            _mapImage.texture = _mapTexture;
            _mapImage.color = new Color(0.9f, 0.95f, 1f, 0.85f);

            // Radar Grid overlay inside mask
            var gridGo = new GameObject("Radar_Grid", typeof(RectTransform), typeof(Image));
            gridGo.transform.SetParent(maskGo.transform, false);
            var gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.sizeDelta = new Vector2(RadarRadius * 2f, RadarRadius * 2f);
            var gridImg = gridGo.GetComponent<Image>();
            gridImg.sprite = Sprite.Create(_radarRingTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            gridImg.color = new Color(0.2f, 0.8f, 1f, 0.15f);

            // Glowing Outer Cyan Ring Border
            var ringGo = new GameObject("Outer_Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(_rootRect, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(RadarRadius * 2f + 8f, RadarRadius * 2f + 8f);
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = Sprite.Create(_radarRingTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            ringImg.color = new Color(0.35f, 0.85f, 1f, 0.95f);

            // North "N" Indicator at Top of the Radar Ring
            var northGo = new GameObject("North_Indicator", typeof(RectTransform), typeof(Text), typeof(Shadow));
            northGo.transform.SetParent(_rootRect, false);
            var northRt = northGo.GetComponent<RectTransform>();
            northRt.anchorMin = northRt.anchorMax = new Vector2(0.5f, 1f);
            northRt.anchoredPosition = new Vector2(0f, -8f);
            northRt.sizeDelta = new Vector2(40f, 24f);
            _northText = northGo.GetComponent<Text>();
            _northText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _northText.text = "▲ N";
            _northText.fontSize = 13;
            _northText.color = new Color(0.4f, 0.9f, 1f, 0.95f);
            _northText.alignment = TextAnchor.MiddleCenter;

            // Player Marker at Center (White chevron)
            var playerMarkerGo = new GameObject("Player_Chevron", typeof(RectTransform), typeof(Image));
            playerMarkerGo.transform.SetParent(_rootRect, false);
            _playerMarker = playerMarkerGo.GetComponent<RectTransform>();
            _playerMarker.anchorMin = _playerMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _playerMarker.sizeDelta = new Vector2(22f, 22f);
            var playerImg = playerMarkerGo.GetComponent<Image>();
            playerImg.sprite = Sprite.Create(_playerArrowTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            playerImg.color = Color.white;

            // Objective Marker (Amber Diamond)
            var objGo = new GameObject("Objective_Marker", typeof(RectTransform), typeof(Image));
            objGo.transform.SetParent(_rootRect, false);
            _objectiveMarker = objGo.GetComponent<RectTransform>();
            _objectiveMarker.anchorMin = _objectiveMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _objectiveMarker.sizeDelta = new Vector2(16f, 16f);
            var objImg = objGo.GetComponent<Image>();
            objImg.sprite = Sprite.Create(_objectiveIconTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            objImg.color = new Color(1f, 0.78f, 0.2f, 0.95f);

            // Objective Distance Readout below Minimap
            var distGo = new GameObject("Objective_Distance", typeof(RectTransform), typeof(Text), typeof(Shadow));
            distGo.transform.SetParent(_rootRect, false);
            var distRt = distGo.GetComponent<RectTransform>();
            distRt.anchorMin = distRt.anchorMax = new Vector2(0.5f, 0f);
            distRt.anchoredPosition = new Vector2(0f, -14f);
            distRt.sizeDelta = new Vector2(160f, 22f);
            _objectiveDistText = distGo.GetComponent<Text>();
            _objectiveDistText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _objectiveDistText.text = "OBJ: 0m";
            _objectiveDistText.fontSize = 14;
            _objectiveDistText.color = new Color(1f, 0.82f, 0.35f, 0.9f);
            _objectiveDistText.alignment = TextAnchor.MiddleCenter;

            // Pre-allocate 16 enemy blip icons
            for (int i = 0; i < 16; i++)
            {
                var bGo = new GameObject($"EnemyBlip_{i}", typeof(RectTransform), typeof(Image));
                bGo.transform.SetParent(maskGo.transform, false);
                var rt = bGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(12f, 12f);
                var img = bGo.GetComponent<Image>();
                img.sprite = Sprite.Create(_enemyBlipTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
                img.color = new Color(1f, 0.25f, 0.2f, 0.95f);
                bGo.SetActive(false);
                _blipPool.Add(img);
            }

            Debug.Log("[Minimap] Initialized successfully at top-right.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Minimap Initialize Exception] " + ex);
        }
    }

        private void Update()
        {
            if (_player == null)
            {
                _player = PlayerLocator.State != null
                    ? PlayerLocator.State.Root.GetComponent<PlayerController>()
                    : FindAnyObjectByType<PlayerController>();
                if (_player == null) return;
            }

            Vector3 playerPos = _player.transform.position;
            float playerYaw = _player.transform.eulerAngles.y;

            // 1. Update Player Chevron Rotation (0 deg is forward/North in map texture)
            if (_playerMarker != null)
            {
                _playerMarker.localRotation = Quaternion.Euler(0f, 0f, -playerYaw);
            }

            // 2. Scroll Factory Map relative to Player Position
            if (_mapRect != null)
            {
                // Map center in world space is (MapMinX + MapMaxX)/2, (MapMinZ + MapMaxZ)/2
                float mapWorldCenterX = (MapMinX + MapMaxX) * 0.5f; // (-32 + 56)/2 = 12m
                float mapWorldCenterZ = (MapMinZ + MapMaxZ) * 0.5f; // (-10 + 38)/2 = 14m

                float dx = (mapWorldCenterX - playerPos.x) * WorldToUIRatio;
                float dz = (mapWorldCenterZ - playerPos.z) * WorldToUIRatio;
                _mapRect.anchoredPosition = new Vector2(dx, dz);
            }

            // 3. Update Objective Marker
            UpdateObjective(playerPos);

            // 4. Update Enemy Robot Radar Blips
            UpdateEnemyBlips(playerPos);
        }

        private void UpdateObjective(Vector3 playerPos)
        {
            if (_objectiveMarker == null || GameManager.Instance == null) return;

            Vector3 targetPos = GameManager.Instance.CurrentObjectivePosition;
            Vector3 diff = targetPos - playerPos;
            float dist = diff.magnitude;

            if (_objectiveDistText != null)
            {
                _objectiveDistText.text = $"OBJ: {Mathf.RoundToInt(dist)}m";
            }

            // Convert world displacement to UI radar coordinates (X -> X, Z -> Y)
            Vector2 uiDiff = new Vector2(diff.x, diff.z) * WorldToUIRatio;
            float len = uiDiff.magnitude;

            // Clamp inside circular radar boundary with 14px padding
            float maxR = RadarRadius - 14f;
            if (len > maxR)
            {
                uiDiff = uiDiff.normalized * maxR;
            }

            _objectiveMarker.anchoredPosition = uiDiff;

            // Pulse effect
            float pulse = 0.85f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.35f);
            _objectiveMarker.localScale = Vector3.one * pulse;
        }

        private void UpdateEnemyBlips(Vector3 playerPos)
        {
            var activeAgents = AgentBase.Active;
            int blipIndex = 0;

            if (activeAgents != null)
            {
                for (int i = 0; i < activeAgents.Count; i++)
                {
                    var agent = activeAgents[i];
                    if (agent == null || !agent.gameObject.activeInHierarchy || agent.HealthComponent.IsDead) continue;

                    Vector3 diff = agent.transform.position - playerPos;
                    Vector2 uiDiff = new Vector2(diff.x, diff.z) * WorldToUIRatio;
                    float distFromCenter = uiDiff.magnitude;

                    // Only show agents within ~42 meters (or clamp to rim)
                    if (diff.magnitude > 42f) continue;

                    if (blipIndex < _blipPool.Count)
                    {
                        var blip = _blipPool[blipIndex];
                        blip.gameObject.SetActive(true);

                        // Clamp inside circular radar boundary
                        float maxR = RadarRadius - 8f;
                        if (distFromCenter > maxR)
                        {
                            uiDiff = uiDiff.normalized * maxR;
                        }

                        blip.rectTransform.anchoredPosition = uiDiff;

                        // Titan Boss has a larger pulsing crimson marker
                        bool isBoss = agent is TitanBossBrain;
                        if (isBoss)
                        {
                            blip.rectTransform.sizeDelta = new Vector2(18f, 18f);
                            blip.color = new Color(1f, 0.15f, 0.05f, 0.95f);
                            blip.rectTransform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.unscaledTime * 3f, 0.4f));
                        }
                        else
                        {
                            blip.rectTransform.sizeDelta = new Vector2(10f, 10f);
                            blip.color = new Color(1f, 0.35f, 0.2f, 0.9f);
                            blip.rectTransform.localScale = Vector3.one;
                        }

                        blipIndex++;
                    }
                }
            }

            // Hide unused blips
            for (int i = blipIndex; i < _blipPool.Count; i++)
            {
                _blipPool[i].gameObject.SetActive(false);
            }
        }

        // ==============================================================================
        // PROCEDURAL BLUEPRINT & RADAR TEXTURE GENERATOR
        // ==============================================================================
        private void BuildTextures()
        {
            // 1. Radar Circular Border & Dark Mask Texture (128x128)
            _radarRingTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            _radarRingTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] ringPx = new Color[128 * 128];
            Vector2 ringCenter = new Vector2(63.5f, 63.5f);
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), ringCenter);
                    if (d <= 58f)
                    {
                        // Inside circle: dark tactical navy glass background
                        ringPx[y * 128 + x] = new Color(0.03f, 0.07f, 0.12f, 0.88f);
                    }
                    else if (d <= 62f)
                    {
                        // Glowing cyan ring rim
                        float edgeAlpha = Mathf.SmoothStep(0f, 1f, (d - 58f) / 2f);
                        ringPx[y * 128 + x] = new Color(0.4f, 0.9f, 1f, edgeAlpha);
                    }
                    else if (d <= 64f)
                    {
                        float outerAlpha = Mathf.SmoothStep(1f, 0f, (d - 62f) / 2f);
                        ringPx[y * 128 + x] = new Color(0.4f, 0.9f, 1f, outerAlpha);
                    }
                    else
                    {
                        ringPx[y * 128 + x] = Color.clear;
                    }
                }
            }
            _radarRingTexture.SetPixels(ringPx);
            _radarRingTexture.Apply();

            // 2. Player Chevron Arrow Texture (32x32)
            _playerArrowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] arrowPx = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Sharp triangle pointing UP (+Y)
                    float normX = (x - 15.5f) / 14f;
                    float normY = y / 28f;
                    bool inTriangle = normY >= 0.15f && normY <= 0.95f && Mathf.Abs(normX) <= (1f - normY) * 0.95f;
                    // Recessed rear chevron notch
                    if (inTriangle && normY < 0.35f && Mathf.Abs(normX) < 0.45f * (1f - normY / 0.35f))
                    {
                        inTriangle = false;
                    }
                    arrowPx[y * 32 + x] = inTriangle ? new Color(0.95f, 0.98f, 1f, 0.98f) : Color.clear;
                }
            }
            _playerArrowTexture.SetPixels(arrowPx);
            _playerArrowTexture.Apply();

            // 3. Enemy Blip Texture (32x32 circle)
            _enemyBlipTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] blipPx = new Color[32 * 32];
            Vector2 bc = new Vector2(15.5f, 15.5f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), bc);
                    if (d <= 11f)
                    {
                        blipPx[y * 32 + x] = new Color(1f, 0.2f, 0.15f, 1f);
                    }
                    else if (d <= 14f)
                    {
                        blipPx[y * 32 + x] = new Color(1f, 0.4f, 0.2f, (14f - d) / 3f);
                    }
                    else
                    {
                        blipPx[y * 32 + x] = Color.clear;
                    }
                }
            }
            _enemyBlipTexture.SetPixels(blipPx);
            _enemyBlipTexture.Apply();

            // 4. Objective Diamond Texture (32x32)
            _objectiveIconTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] objPx = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = Mathf.Abs(x - 15.5f) / 13f;
                    float dy = Mathf.Abs(y - 15.5f) / 13f;
                    bool inDiamond = (dx + dy) <= 1.0f;
                    objPx[y * 32 + x] = inDiamond ? new Color(1f, 0.82f, 0.2f, 1f) : Color.clear;
                }
            }
            _objectiveIconTexture.SetPixels(objPx);
            _objectiveIconTexture.Apply();

            // 5. Factory Blueprint Layout Texture (256 x 140)
            // Maps world X [-32, 56] to texture X [0, 256], world Z [-10, 38] to texture Y [0, 140]
            const int texW = 256;
            const int texH = 140;
            _mapTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            Color[] mapPx = new Color[texW * texH];

            // Dark blue grid background
            Color bgTint = new Color(0.04f, 0.08f, 0.14f, 0.75f);
            Color gridLine = new Color(0.10f, 0.22f, 0.35f, 0.55f);
            Color wallColor = new Color(0.35f, 0.85f, 1f, 0.95f);
            Color roomFillLoft = new Color(0.08f, 0.18f, 0.28f, 0.6f);
            Color roomFillRolling = new Color(0.09f, 0.20f, 0.26f, 0.6f);
            Color roomFillDrying = new Color(0.18f, 0.14f, 0.08f, 0.6f);
            Color roomFillControl = new Color(0.06f, 0.22f, 0.20f, 0.6f);
            Color roomFillVault = new Color(0.24f, 0.08f, 0.08f, 0.65f); // Reddish containment boss room
            Color yardFill = new Color(0.06f, 0.10f, 0.14f, 0.5f);

            for (int y = 0; y < texH; y++)
            {
                float worldZ = Mathf.Lerp(MapMinZ, MapMaxZ, (float)y / texH);
                for (int x = 0; x < texW; x++)
                {
                    float worldX = Mathf.Lerp(MapMinX, MapMaxX, (float)x / texW);
                    int idx = y * texW + x;

                    // Base background with subtle 5m grid lines
                    bool isGrid = (Mathf.Abs(worldX % 5f) < 0.25f) || (Mathf.Abs(worldZ % 5f) < 0.25f);
                    Color pixelColor = isGrid ? gridLine : bgTint;

                    // Tint rooms
                    if (worldZ >= -8f && worldZ <= 0f && worldX >= -30f && worldX <= -10f)
                    {
                        pixelColor = yardFill; // Front Yard
                    }
                    else if (worldZ >= 0f && worldZ <= 36f)
                    {
                        if (worldX >= -30f && worldX <= -10f) pixelColor = roomFillLoft; // Withering Loft
                        else if (worldX >= -10f && worldX <= 10f) pixelColor = roomFillRolling; // Rolling Room
                        else if (worldX >= 10f && worldX <= 30f && worldZ <= 24f) pixelColor = roomFillDrying; // Drying Chamber
                        else if (worldX >= 10f && worldX <= 30f && worldZ > 24f) pixelColor = roomFillControl; // Control Room
                        else if (worldX >= 30f && worldX <= 54f && worldZ <= 24f) pixelColor = roomFillVault; // Containment Vault
                    }

                    mapPx[idx] = pixelColor;
                }
            }

            // Draw Blueprint Walls onto mapPx
            void DrawLine(Vector2 p1, Vector2 p2, Color col, float thickness = 1.6f)
            {
                float len = Vector2.Distance(p1, p2);
                Vector2 dir = (p2 - p1).normalized;
                int steps = Mathf.CeilToInt(len * 2f);
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 pt = p1 + dir * (s * 0.5f);
                    int px = Mathf.Clamp(Mathf.RoundToInt(((pt.x - MapMinX) / MapWorldWidth) * texW), 0, texW - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(((pt.y - MapMinZ) / MapWorldHeight) * texH), 0, texH - 1);
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int cx = Mathf.Clamp(px + dx, 0, texW - 1);
                            int cy = Mathf.Clamp(py + dy, 0, texH - 1);
                            mapPx[cy * texW + cx] = col;
                        }
                    }
                }
            }

            // Exterior Factory Walls
            DrawLine(new Vector2(-30f, 0f), new Vector2(-22f, 0f), wallColor); // South wall left
            DrawLine(new Vector2(-18f, 0f), new Vector2(30f, 0f), wallColor);  // South wall gate gap at -20
            DrawLine(new Vector2(30f, 0f), new Vector2(54f, 0f), wallColor);   // Vault South wall
            DrawLine(new Vector2(-30f, 36f), new Vector2(30f, 36f), wallColor); // North wall
            DrawLine(new Vector2(-30f, 0f), new Vector2(-30f, 36f), wallColor); // West wall
            DrawLine(new Vector2(30f, 24f), new Vector2(30f, 36f), wallColor); // East Control Room wall
            DrawLine(new Vector2(54f, 0f), new Vector2(54f, 24f), wallColor);  // Vault East wall
            DrawLine(new Vector2(30f, 24f), new Vector2(54f, 24f), wallColor); // Vault North wall

            // Interior Partitions with Doors
            // Wall AB (x = -10, door at z = 18)
            DrawLine(new Vector2(-10f, 0f), new Vector2(-10f, 17f), wallColor);
            DrawLine(new Vector2(-10f, 19f), new Vector2(-10f, 36f), wallColor);

            // Wall BC (x = 10, gate at z = 6)
            DrawLine(new Vector2(10f, 0f), new Vector2(10f, 5f), wallColor);
            DrawLine(new Vector2(10f, 7f), new Vector2(10f, 36f), wallColor);

            // Wall CD (z = 24, door at x = 20)
            DrawLine(new Vector2(10f, 24f), new Vector2(19f, 24f), wallColor);
            DrawLine(new Vector2(21f, 24f), new Vector2(30f, 24f), wallColor);

            // Wall CE (x = 30, door to Titan Boss vault at z = 14)
            DrawLine(new Vector2(30f, 0f), new Vector2(30f, 13f), wallColor);
            DrawLine(new Vector2(30f, 15f), new Vector2(30f, 24f), wallColor);

            // Yard Fence (South at z = -8, x = -30 to -10)
            Color fenceColor = new Color(0.2f, 0.5f, 0.7f, 0.7f);
            DrawLine(new Vector2(-30f, -8f), new Vector2(-10f, -8f), fenceColor);
            DrawLine(new Vector2(-30f, -8f), new Vector2(-30f, 0f), fenceColor);
            DrawLine(new Vector2(-10f, -8f), new Vector2(-10f, 0f), fenceColor);

            _mapTexture.SetPixels(mapPx);
            _mapTexture.Apply();
        }
    }
}
