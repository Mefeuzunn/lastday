using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 1 (Step 1B): 360° Directional Damage Indicator Arc.
    /// Provides screen-space spatial awareness when attacked from behind or the flanks.
    /// Displays a sleek translucent crimson crescent indicator arc at radius 165px,
    /// rotating to point toward the attacker's world position and fading smoothly over 1.1s.
    /// Pool of 4 indicators, raycastTarget = false, strictly inside SafeArea.
    /// </summary>
    public class DirectionalDamageHUD : MonoBehaviour
    {
        public static DirectionalDamageHUD Instance { get; private set; }

        [Header("Arc Layout Tuning")]
        [SerializeField] private float arcRadius = 165f;
        [SerializeField] private float fadeDuration = 1.1f;
        [SerializeField] private Color arcColor = new Color(0.902f, 0.224f, 0.275f, 0.85f); // #E63946

        [Header("Pool Setup")]
        [SerializeField] private int poolSize = 4;
        [SerializeField] private RectTransform container;

        private class IndicatorSlot
        {
            public GameObject RootObj;
            public RectTransform Pivot;
            public RectTransform ArcRect;
            public Image ArcImage;
            public Coroutine FadeRoutine;
            public float LastTriggerTime;
        }

        private IndicatorSlot[] pool;
        private Camera mainCamera;
        private Transform playerTransform;
        private Sprite crescentSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCrescentSprite();
            InitializeContainer();
            InitializePool();
        }

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void BuildCrescentSprite()
        {
            if (crescentSprite != null) return;

            int width = 160;
            int height = 54;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.name = "Tex_DirectionalDamageArc";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            float cx = width * 0.5f;
            float cy = -height * 0.65f; // Curve center slightly below base
            float targetR = height * 1.55f;
            float halfThickness = height * 0.22f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float diff = Mathf.Abs(dist - targetR);

                    // Radial curve falloff
                    float radialAlpha = 1.0f - Mathf.Clamp01(diff / halfThickness);
                    radialAlpha = Mathf.SmoothStep(0f, 1f, radialAlpha);

                    // Angular taper falloff towards left & right tips
                    float normX = Mathf.Abs(dx) / (width * 0.5f);
                    float tipTaper = 1.0f - Mathf.SmoothStep(0.65f, 1.0f, normX);

                    float alpha = radialAlpha * tipTaper;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            crescentSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private void InitializeContainer()
        {
            if (container != null) return;

            // Find SafeArea or HUD_Canvas
            Transform parent = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)
            {
                Transform safeArea = canvas.transform.Find("SafeArea") ?? canvas.transform.Find("Safe_Area") ?? canvas.transform.Find("SafeAreaContainer");
                if (safeArea != null) parent = safeArea;
                else parent = canvas.transform;
            }

            GameObject cObj = new GameObject("DirectionalDamage_Container", typeof(RectTransform));
            cObj.transform.SetParent(parent, false);
            container = cObj.GetComponent<RectTransform>();
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = new Vector2(arcRadius * 2.5f, arcRadius * 2.5f);
        }

        private void InitializePool()
        {
            pool = new IndicatorSlot[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                GameObject root = new GameObject($"ThreatArc_{i + 1}");
                root.transform.SetParent(container, false);

                RectTransform pivot = root.AddComponent<RectTransform>();
                pivot.anchorMin = new Vector2(0.5f, 0.5f);
                pivot.anchorMax = new Vector2(0.5f, 0.5f);
                pivot.pivot = new Vector2(0.5f, 0.5f);
                pivot.anchoredPosition = Vector2.zero;
                pivot.sizeDelta = Vector2.zero;

                GameObject arcChild = new GameObject("ArcVisual");
                arcChild.transform.SetParent(pivot, false);

                RectTransform arcRt = arcChild.AddComponent<RectTransform>();
                arcRt.anchorMin = new Vector2(0.5f, 0.5f);
                arcRt.anchorMax = new Vector2(0.5f, 0.5f);
                arcRt.pivot = new Vector2(0.5f, 0.5f);
                arcRt.anchoredPosition = new Vector2(0f, arcRadius);
                arcRt.sizeDelta = new Vector2(140f, 42f);

                Image img = arcChild.AddComponent<Image>();
                img.sprite = crescentSprite;
                img.color = new Color(arcColor.r, arcColor.g, arcColor.b, 0f);
                img.raycastTarget = false;

                pool[i] = new IndicatorSlot
                {
                    RootObj = root,
                    Pivot = pivot,
                    ArcRect = arcRt,
                    ArcImage = img,
                    FadeRoutine = null,
                    LastTriggerTime = -999f
                };

                root.SetActive(false);
            }
        }

        /// <summary>
        /// Displays a directional damage arc pointing directly towards attacker world position.
        /// </summary>
        public void ShowThreat(Vector3 attackerWorldPosition)
        {
            FindReferences();
            if (mainCamera == null || playerTransform == null) return;

            Vector3 camFwd = mainCamera.transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 0.001f) camFwd = playerTransform.forward;
            camFwd.Normalize();

            Vector3 toAttacker = attackerWorldPosition - playerTransform.position;
            toAttacker.y = 0f;
            if (toAttacker.sqrMagnitude < 0.01f)
            {
                // Self damage / explosion right at player feet: point behind
                toAttacker = -camFwd;
            }
            toAttacker.Normalize();

            // Calculate signed horizontal angle
            float angle = Vector3.SignedAngle(camFwd, toAttacker, Vector3.up);

            // Get available or oldest slot
            IndicatorSlot slot = GetBestSlot();
            if (slot == null) return;

            slot.LastTriggerTime = Time.time;
            slot.RootObj.SetActive(true);

            // Rotate pivot so arc rests on circumference pointing towards threat
            slot.Pivot.localRotation = Quaternion.Euler(0f, 0f, -angle);

            if (slot.FadeRoutine != null)
            {
                StopCoroutine(slot.FadeRoutine);
            }
            slot.FadeRoutine = StartCoroutine(FadeIndicatorRoutine(slot));
        }

        private IndicatorSlot GetBestSlot()
        {
            // 1. Try to find an inactive slot
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].RootObj.activeSelf) return pool[i];
            }

            // 2. Otherwise pick the oldest slot
            IndicatorSlot oldest = pool[0];
            for (int i = 1; i < pool.Length; i++)
            {
                if (pool[i].LastTriggerTime < oldest.LastTriggerTime)
                {
                    oldest = pool[i];
                }
            }
            return oldest;
        }

        private IEnumerator FadeIndicatorRoutine(IndicatorSlot slot)
        {
            Color c = arcColor;
            slot.ArcImage.color = c;

            // Subtle punch pop
            slot.ArcRect.localScale = Vector3.one * 1.15f;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);

                // Smooth scale recovery
                slot.ArcRect.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t * 3.5f);

                // Smooth alpha fade
                c.a = Mathf.Lerp(arcColor.a, 0f, t * t);
                slot.ArcImage.color = c;

                yield return null;
            }

            c.a = 0f;
            slot.ArcImage.color = c;
            slot.RootObj.SetActive(false);
            slot.FadeRoutine = null;
        }
    }
}
