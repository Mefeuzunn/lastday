using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 2 (Step 2C): Compact SafeArea-aligned Siege Progress HUD.
    /// Displays a sleek, non-overlapping crimson/amber status pill at top-center:
    /// 'KARAKOL SAVUNMASI • DALGA X/Y' and 'KALAN DÜŞMAN: Z'.
    /// Strictly guarantees raycastTarget = false on all elements to prevent mobile touch interception.
    /// Automatically adjusts anchoredPosition to avoid overlapping CompassBarUI or BossHealthBarHUD.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SiegeWarningHUD : MonoBehaviour
    {
        public static SiegeWarningHUD Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Image pillBackground;
        [SerializeField] private Image accentBorder;
        [SerializeField] private Text waveTitleText;
        [SerializeField] private Text enemyCountText;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.35f;

        private bool isVisible = false;
        private Coroutine fadeRoutine;
        private Coroutine pulseRoutine;
        private int currentRemaining = 0;

        public bool IsVisible => isVisible;
        public int RemainingEnemies => currentRemaining;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUIHierarchyIfMissing();
        }

        private void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void Update()
        {
            if (!isVisible) return;
            AdjustSafeAreaPositioning();
        }

        private void AdjustSafeAreaPositioning()
        {
            if (rootRect == null) return;

            // Check if BossHealthBarHUD is currently active and visible
            bool bossBarActive = BossHealthBarHUD.Instance != null && BossHealthBarHUD.Instance.IsActive;

            // Position cleanly below CompassBar (-62f) or below BossHealthBarHUD (-158f)
            float targetY = bossBarActive ? -158f : -62f;

            // Calculate SafeArea top offset
            float safeTopOffset = 0f;
            if (Screen.safeArea.yMax < Screen.height)
            {
                safeTopOffset = (Screen.height - Screen.safeArea.yMax);
            }

            Vector2 desiredPos = new Vector2(0f, targetY - (safeTopOffset * 0.5f));
            if (rootRect.anchoredPosition != desiredPos)
            {
                rootRect.anchoredPosition = desiredPos;
            }
        }

        public void BuildUIHierarchyIfMissing()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (rootRect == null)
            {
                rootRect = GetComponent<RectTransform>();
                if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            }

            // Top-center alignment
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -62f);
            rootRect.sizeDelta = new Vector2(320f, 44f);

            // Background Pill
            if (pillBackground == null)
            {
                Transform bgT = transform.Find("Pill_BG");
                if (bgT != null) pillBackground = bgT.GetComponent<Image>();
                else
                {
                    GameObject bgObj = new GameObject("Pill_BG");
                    bgObj.transform.SetParent(transform, false);
                    pillBackground = bgObj.AddComponent<Image>();
                    pillBackground.color = new Color(0.10f, 0.08f, 0.12f, 0.92f); // Deep tactical dark purple-slate
                    pillBackground.raycastTarget = false;

                    RectTransform rt = bgObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                }
            }

            // Accent Border
            if (accentBorder == null)
            {
                Transform abT = transform.Find("Accent_Border");
                if (abT != null) accentBorder = abT.GetComponent<Image>();
                else
                {
                    GameObject abObj = new GameObject("Accent_Border");
                    abObj.transform.SetParent(transform, false);
                    accentBorder = abObj.AddComponent<Image>();
                    accentBorder.color = new Color(0.92f, 0.22f, 0.26f, 0.85f); // #EA3843 Crimson
                    accentBorder.raycastTarget = false;

                    RectTransform rt = abObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.sizeDelta = new Vector2(0f, 3.0f);
                }
            }

            // Wave Title Text
            if (waveTitleText == null)
            {
                Transform wtT = transform.Find("Wave_Title_Text");
                if (wtT != null) waveTitleText = wtT.GetComponent<Text>();
                else
                {
                    GameObject wtObj = new GameObject("Wave_Title_Text");
                    wtObj.transform.SetParent(transform, false);
                    waveTitleText = wtObj.AddComponent<Text>();
                    waveTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    waveTitleText.fontSize = 12;
                    waveTitleText.fontStyle = FontStyle.Bold;
                    waveTitleText.alignment = TextAnchor.MiddleCenter;
                    waveTitleText.color = new Color(0.98f, 0.88f, 0.55f, 1f); // Warm amber
                    waveTitleText.text = "KARAKOL SAVUNMASI • DALGA 1/2";
                    waveTitleText.raycastTarget = false;

                    RectTransform rt = wtObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0.45f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.offsetMin = new Vector2(10f, 0f);
                    rt.offsetMax = new Vector2(-10f, 0f);
                }
            }

            // Enemy Count Text
            if (enemyCountText == null)
            {
                Transform ecT = transform.Find("Enemy_Count_Text");
                if (ecT != null) enemyCountText = ecT.GetComponent<Text>();
                else
                {
                    GameObject ecObj = new GameObject("Enemy_Count_Text");
                    ecObj.transform.SetParent(transform, false);
                    enemyCountText = ecObj.AddComponent<Text>();
                    enemyCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    enemyCountText.fontSize = 11;
                    enemyCountText.fontStyle = FontStyle.Normal;
                    enemyCountText.alignment = TextAnchor.MiddleCenter;
                    enemyCountText.color = new Color(0.90f, 0.35f, 0.38f, 1f); // Soft crimson
                    enemyCountText.text = "KALAN DÜŞMAN: 6";
                    enemyCountText.raycastTarget = false;

                    RectTransform rt = ecObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0.05f);
                    rt.anchorMax = new Vector2(1f, 0.55f);
                    rt.offsetMin = new Vector2(10f, 0f);
                    rt.offsetMax = new Vector2(-10f, 0f);
                }
            }
        }

        public void ShowSiegeHUD(int currentWave, int totalWaves, int remainingEnemies)
        {
            BuildUIHierarchyIfMissing();
            isVisible = true;
            currentRemaining = remainingEnemies;

            UpdateContent(currentWave, totalWaves, remainingEnemies);
            AdjustSafeAreaPositioning();

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(1.0f));

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseBorderRoutine());
        }

        public void UpdateEnemyCount(int remainingEnemies)
        {
            currentRemaining = remainingEnemies;
            if (enemyCountText != null)
            {
                enemyCountText.text = $"KALAN DÜŞMAN: {remainingEnemies}";
            }
        }

        public void UpdateWave(int currentWave, int totalWaves, int remainingEnemies)
        {
            currentRemaining = remainingEnemies;
            UpdateContent(currentWave, totalWaves, remainingEnemies);
        }

        private void UpdateContent(int currentWave, int totalWaves, int remainingEnemies)
        {
            if (waveTitleText != null)
            {
                waveTitleText.text = $"KARAKOL SAVUNMASI • DALGA {currentWave}/{totalWaves}";
            }

            if (enemyCountText != null)
            {
                enemyCountText.text = $"KALAN DÜŞMAN: {remainingEnemies}";
            }
        }

        public void HideSiegeHUD()
        {
            isVisible = false;
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(0.0f));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private IEnumerator PulseBorderRoutine()
        {
            while (isVisible)
            {
                float t = Mathf.Sin(Time.time * 4.0f) * 0.5f + 0.5f;
                if (accentBorder != null)
                {
                    accentBorder.color = Color.Lerp(
                        new Color(0.92f, 0.22f, 0.26f, 0.65f),
                        new Color(1.0f, 0.42f, 0.20f, 1.0f),
                        t
                    );
                }
                yield return null;
            }
        }
    }
}
