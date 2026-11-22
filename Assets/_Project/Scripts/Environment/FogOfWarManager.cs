using System;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Persistent, dynamic Fog-of-War (Keşif Sisi) System upgraded for the 720x720m (3x3 Sector Grid) open world.
    /// Operates at 512x512 resolution (~1.4m per texel), providing smooth 28m radial reveals
    /// around the player and seamless backward-compatible save migration.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        public static FogOfWarManager Instance { get; private set; }

        public const int FogResolution = 512;
        public const float WorldSize = WorldMapConfig.TotalWorldSize; // 720m
        public const float WorldMin = WorldMapConfig.WorldMin;        // -360m
        public const float WorldMax = WorldMapConfig.WorldMax;        // +360m

        [Header("Vision Settings")]
        [SerializeField] private float playerVisionRadius = 28f;
        [SerializeField] private float updateInterval = 0.15f;
        [SerializeField] private float moveDistanceThreshold = 0.75f;

        [Header("Visual Feedback")]
        [SerializeField] private Color32 denseFogColor = new Color32(10, 14, 22, 238); // #0A0E16EE
        [SerializeField] private Color32 clearColor = new Color32(10, 14, 22, 0);

        private Texture2D fogTexture;
        private Color32[] fogPixels;
        private byte[] permanentAlpha;

        private Transform playerTransform;
        private Vector3 lastRevealedPos = new Vector3(-999f, -999f, -999f);
        private float nextUpdateTime = 0f;
        private bool isDirty = false;
        private float explorationPercent = 0f;

        public Texture2D FogTexture => fogTexture;
        public float ExplorationPercent => explorationPercent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeFogGrid();
        }

        private void Start()
        {
            FindPlayer();

            // Reveal initial spawn pocket
            Vector3 startPos = playerTransform != null ? playerTransform.position : new Vector3(0f, 0f, -25f);
            RevealRadius(startPos, 38f);

            // Reveal pockets around core landmarks
            RevealRadius(new Vector3(-36f, 0f, 32f), 22f); // Clinic
            RevealRadius(new Vector3(0f, 0f, 15f), 22f);   // Police checkpoint
            RevealRadius(new Vector3(0f, 0f, -8f), 22f);   // Safe House courtyard

            // Attempt to restore saved Fog of War state if available
            TryRestoreFromSave();
        }

        private void InitializeFogGrid()
        {
            fogTexture = new Texture2D(FogResolution, FogResolution, TextureFormat.RGBA32, false);
            fogTexture.filterMode = FilterMode.Bilinear;
            fogTexture.wrapMode = TextureWrapMode.Clamp;

            int total = FogResolution * FogResolution;
            fogPixels = new Color32[total];
            permanentAlpha = new byte[total];

            for (int i = 0; i < total; i++)
            {
                permanentAlpha[i] = denseFogColor.a;
                fogPixels[i] = denseFogColor;
            }

            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float now = Time.unscaledTime;
            Vector3 pPos = playerTransform.position;

            if (now >= nextUpdateTime || Vector3.Distance(pPos, lastRevealedPos) >= moveDistanceThreshold)
            {
                nextUpdateTime = now + updateInterval;
                lastRevealedPos = pPos;
                RevealRadius(pPos, playerVisionRadius);
            }

            if (isDirty)
            {
                ApplyFogTexture(pPos);
                isDirty = false;
            }
        }

        public void RevealRadius(Vector3 worldPos, float radiusMeters)
        {
            float texelsPerMeter = FogResolution / WorldSize; // 512 / 720 = ~0.7111 texels/m
            float radiusTexels = radiusMeters * texelsPerMeter;
            float innerRadiusTexels = radiusTexels * 0.75f;

            Vector2 uv = WorldMapConfig.WorldToNormalizedUV(worldPos);

            int centerTx = Mathf.RoundToInt(uv.x * (FogResolution - 1));
            int centerTy = Mathf.RoundToInt(uv.y * (FogResolution - 1));

            int minTx = Mathf.Max(0, Mathf.FloorToInt(centerTx - radiusTexels - 1));
            int maxTx = Mathf.Min(FogResolution - 1, Mathf.CeilToInt(centerTx + radiusTexels + 1));
            int minTy = Mathf.Max(0, Mathf.FloorToInt(centerTy - radiusTexels - 1));
            int maxTy = Mathf.Min(FogResolution - 1, Mathf.CeilToInt(centerTy + radiusTexels + 1));

            for (int ty = minTy; ty <= maxTy; ty++)
            {
                float dy = ty - centerTy;
                for (int tx = minTx; tx <= maxTx; tx++)
                {
                    float dx = tx - centerTx;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < radiusTexels)
                    {
                        // Smooth radial feather from inner radius to outer edge
                        float t = Mathf.Clamp01((dist - innerRadiusTexels) / (radiusTexels - innerRadiusTexels));
                        float feather = Mathf.SmoothStep(0f, 1f, t);
                        byte targetAlpha = (byte)Mathf.RoundToInt(feather * denseFogColor.a);

                        int idx = ty * FogResolution + tx;
                        if (targetAlpha < permanentAlpha[idx])
                        {
                            permanentAlpha[idx] = targetAlpha;
                            isDirty = true;
                        }
                    }
                }
            }
        }

        private void ApplyFogTexture(Vector3 livePlayerPos)
        {
            int total = FogResolution * FogResolution;
            int revealedCount = 0;

            // Live active scanner pulse around player
            Vector2 uv = WorldMapConfig.WorldToNormalizedUV(livePlayerPos);
            float centerTx = uv.x * (FogResolution - 1);
            float centerTy = uv.y * (FogResolution - 1);
            float texelsPerMeter = FogResolution / WorldSize;
            float visionRadiusTex = playerVisionRadius * texelsPerMeter;

            for (int ty = 0; ty < FogResolution; ty++)
            {
                float dy = ty - centerTy;
                for (int tx = 0; tx < FogResolution; tx++)
                {
                    int idx = ty * FogResolution + tx;
                    byte permA = permanentAlpha[idx];

                    if (permA < denseFogColor.a * 0.5f)
                    {
                        revealedCount++;
                    }

                    Color32 c = clearColor;
                    c.a = permA;

                    // Subtle cyan scanner ring within active vision
                    float dx = tx - centerTx;
                    float distFromPlayer = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distFromPlayer < visionRadiusTex && permA > 0)
                    {
                        float scanPulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f);
                        float ringDist = Mathf.Abs(distFromPlayer - (visionRadiusTex * 0.85f));
                        if (ringDist < 2.0f)
                        {
                            float ringAlpha = (1.0f - ringDist / 2.0f) * 0.25f * scanPulse;
                            c.r = (byte)Mathf.Clamp(c.r + ringAlpha * 40f, 0, 255);
                            c.g = (byte)Mathf.Clamp(c.g + ringAlpha * 140f, 0, 255);
                            c.b = (byte)Mathf.Clamp(c.b + ringAlpha * 200f, 0, 255);
                        }
                    }

                    fogPixels[idx] = c;
                }
            }

            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);

            explorationPercent = (revealedCount / (float)total) * 100f;
        }

        /// <summary>
        /// Checks whether a given world coordinate has been uncovered from Fog of War.
        /// </summary>
        public bool IsWorldPosExplored(Vector2 worldPos, float threshold = 0.5f)
        {
            if (permanentAlpha == null) return false;

            Vector2 uv = WorldMapConfig.WorldToNormalizedUV(new Vector3(worldPos.x, 0f, worldPos.y));

            int tx = Mathf.Clamp(Mathf.RoundToInt(uv.x * (FogResolution - 1)), 0, FogResolution - 1);
            int ty = Mathf.Clamp(Mathf.RoundToInt(uv.y * (FogResolution - 1)), 0, FogResolution - 1);

            int idx = ty * FogResolution + tx;
            return permanentAlpha[idx] <= (denseFogColor.a * threshold);
        }

        // =========================================================================
        // SAVESYSTEM INTEGRATION & 512x512 MIGRATION
        // =========================================================================
        public string GetSerializedFogData()
        {
            if (permanentAlpha == null) return string.Empty;

            // Pack 512x512 alphas into a 1-bit per texel bitmask (32,768 bytes)
            int totalBytes = (FogResolution * FogResolution) / 8;
            byte[] packed = new byte[totalBytes];

            for (int i = 0; i < permanentAlpha.Length; i++)
            {
                if (permanentAlpha[i] < 128)
                {
                    int byteIdx = i / 8;
                    int bitIdx = i % 8;
                    packed[byteIdx] |= (byte)(1 << bitIdx);
                }
            }

            return Convert.ToBase64String(packed);
        }

        public void LoadSerializedFogData(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return;

            try
            {
                byte[] packed = Convert.FromBase64String(base64);
                if (packed.Length == (256 * 256) / 8) // 8192 bytes = legacy 256x256
                {
                    LoadLegacy256FogData(base64);
                }
                else
                {
                    LoadSerializedFogDataV2(base64);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FogOfWarManager] Failed to detect fog data version: {ex.Message}");
            }
        }

        public void LoadSerializedFogDataV2(string base64)
        {
            if (string.IsNullOrEmpty(base64) || permanentAlpha == null) return;

            try
            {
                byte[] packed = Convert.FromBase64String(base64);
                int totalBits = packed.Length * 8;

                for (int i = 0; i < permanentAlpha.Length && i < totalBits; i++)
                {
                    int byteIdx = i / 8;
                    int bitIdx = i % 8;
                    bool isRevealed = (packed[byteIdx] & (1 << bitIdx)) != 0;

                    if (isRevealed)
                    {
                        permanentAlpha[i] = 0;
                    }
                }

                isDirty = true;
                Debug.Log("[FogOfWarManager] Successfully restored 512x512 persistent exploration progress from save.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FogOfWarManager] Failed to load 512x512 fog data: {ex.Message}");
            }
        }

        public void LoadLegacy256FogData(string base64)
        {
            if (string.IsNullOrEmpty(base64) || permanentAlpha == null) return;

            try
            {
                byte[] packed = Convert.FromBase64String(base64);
                int legacyRes = 256;
                int totalBits = packed.Length * 8;

                // Center sector is the central 1/3 of the 512x512 grid: texels [171..341]
                int minCenterTx = Mathf.RoundToInt((240f / 720f) * 512f); // ~171
                int spanCenterTx = Mathf.RoundToInt((240f / 720f) * 512f); // ~171

                for (int oy = 0; oy < legacyRes; oy++)
                {
                    int ny = minCenterTx + Mathf.RoundToInt((oy / (float)legacyRes) * spanCenterTx);
                    if (ny < 0 || ny >= FogResolution) continue;

                    for (int ox = 0; ox < legacyRes; ox++)
                    {
                        int bitIndex = oy * legacyRes + ox;
                        if (bitIndex >= totalBits) break;

                        int byteIdx = bitIndex / 8;
                        int bitIdx = bitIndex % 8;
                        bool isRevealed = (packed[byteIdx] & (1 << bitIdx)) != 0;

                        if (isRevealed)
                        {
                            int nx = minCenterTx + Mathf.RoundToInt((ox / (float)legacyRes) * spanCenterTx);
                            if (nx >= 0 && nx < FogResolution)
                            {
                                int newIdx = ny * FogResolution + nx;
                                permanentAlpha[newIdx] = 0;
                            }
                        }
                    }
                }

                isDirty = true;
                Debug.Log("[FogOfWarManager] Successfully migrated legacy 256x256 save into central sector of 512x512 grid!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FogOfWarManager] Failed to migrate legacy fog data: {ex.Message}");
            }
        }

        private void TryRestoreFromSave()
        {
            if (SaveSystem.HasSave())
            {
                SaveData data = SaveSystem.Load();
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.fogOfWarDataV2))
                    {
                        LoadSerializedFogDataV2(data.fogOfWarDataV2);
                    }
                    else if (!string.IsNullOrEmpty(data.fogOfWarData))
                    {
                        LoadLegacy256FogData(data.fogOfWarData);
                    }
                }
            }
        }
    }
}
