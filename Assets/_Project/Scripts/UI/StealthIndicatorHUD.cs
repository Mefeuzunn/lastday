using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    public enum StealthEyeState
    {
        Hidden = 0,      // < 35%: Closed / Dim Cyan Eye (Hidden in shadows/cover)
        Suspicious = 1,  // 35% - 70%: Half-Open Amber Eye (Partially visible)
        Exposed = 2      // > 70%: Wide Crimson Eye (Exposed / Spotted)
    }

    /// <summary>
    /// Active Phase 4: Minimalist Diegetic Stealth Eye Indicator.
    /// Positioned neatly within SafeArea between CompassBar and Reticle.
    /// Smoothly fades in when crouching or within 22m of unalerted enemies.
    /// Visualizes real-time visibility score via dynamic eye aperture and color states.
    /// </summary>
    public class StealthIndicatorHUD : MonoBehaviour
    {
        private static StealthIndicatorHUD _instance;
        public static StealthIndicatorHUD Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StealthIndicatorHUD>();
                    if (_instance == null)
                    {
                        Canvas canvas = FindFirstObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            GameObject go = new GameObject("UI_StealthIndicatorHUD");
                            go.transform.SetParent(canvas.transform, false);
                            _instance = go.AddComponent<StealthIndicatorHUD>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (_instance == null)
            {
                var inst = Instance;
            }
        }

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image eyeFrameImage;
        [SerializeField] private Image eyePupilImage;
        [SerializeField] private Text eyeStatusText;

        [Header("Color Palette")]
        [SerializeField] private Color hiddenCyan = new Color(0.02f, 0.84f, 0.63f, 0.85f);     // #06D6A0
        [SerializeField] private Color suspiciousAmber = new Color(1.0f, 0.72f, 0.01f, 0.90f); // #FFB703
        [SerializeField] private Color exposedCrimson = new Color(0.94f, 0.28f, 0.44f, 0.95f);  // #EF476F

        [Header("Animation Tuning")]
        [SerializeField] private float fadeSpeed = 4.5f;
        [SerializeField] private float pupilMorphSpeed = 7.0f;
        [SerializeField] private float enemyProximityThreshold = 22.0f;

        private StealthEyeState currentState = StealthEyeState.Hidden;
        private float targetAlpha = 0f;
        private float currentPupilScaleY = 0.25f;
        private Transform playerTransform;

        public StealthEyeState CurrentState => currentState;
        public float TargetAlpha => targetAlpha;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIHierarchyIfMissing();
        }

        private void Start()
        {
            LocatePlayer();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void LocatePlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            LocatePlayer();
            UpdateVisibilityAndState();
            AnimateUI();
        }

        private void UpdateVisibilityAndState()
        {
            // Determine if eye should be visible:
            // 1) Player is crouched, OR
            // 2) Player is within 22m of any unalerted enemy
            bool shouldBeVisible = false;

            if (playerTransform != null)
            {
                var stealth = playerTransform.GetComponent<StealthSystem>();
                if (stealth != null && stealth.IsCrouched)
                {
                    shouldBeVisible = true;
                }
                else
                {
                    var activeZombies = ZombieAI.ActiveZombies;
                    if (activeZombies != null)
                    {
                        Vector3 playerPos = playerTransform.position;
                        for (int i = 0; i < activeZombies.Count; i++)
                        {
                            ZombieAI z = activeZombies[i];
                            if (z != null && !z.IsDead)
                            {
                                if ((z.transform.position - playerPos).sqrMagnitude <= (enemyProximityThreshold * enemyProximityThreshold))
                                {
                                    shouldBeVisible = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Hide during Main Menu or Paused
            if (MainMenuController.Instance != null && MainMenuController.Instance.IsMenuShowing)
            {
                shouldBeVisible = false;
            }

            targetAlpha = shouldBeVisible ? 1.0f : 0.0f;

            // Fetch live visibility score
            float score = (StealthVisibilityController.Instance != null)
                ? StealthVisibilityController.Instance.CurrentVisibilityScore
                : 0.5f;

            if (score < 0.35f)
            {
                currentState = StealthEyeState.Hidden;
            }
            else if (score <= 0.70f)
            {
                currentState = StealthEyeState.Suspicious;
            }
            else
            {
                currentState = StealthEyeState.Exposed;
            }
        }

        private void AnimateUI()
        {
            if (canvasGroup == null) return;

            // Smooth alpha fade
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            if (canvasGroup.alpha <= 0.01f) return;

            Color targetColor = hiddenCyan;
            float targetScaleY = 0.22f; // Narrow / slit pupil
            string statusStr = "GİZLİ";

            switch (currentState)
            {
                case StealthEyeState.Hidden:
                    targetColor = hiddenCyan;
                    targetScaleY = 0.25f;
                    statusStr = "GİZLİ";
                    break;
                case StealthEyeState.Suspicious:
                    targetColor = suspiciousAmber;
                    targetScaleY = 0.60f;
                    statusStr = "ŞÜPHELİ";
                    break;
                case StealthEyeState.Exposed:
                    targetColor = exposedCrimson;
                    targetScaleY = 1.0f;
                    statusStr = "AÇIKTA!";
                    break;
            }

            // Color morph
            if (eyeFrameImage != null)
            {
                eyeFrameImage.color = Color.Lerp(eyeFrameImage.color, targetColor, Time.deltaTime * 8f);
            }

            // Pupil morph
            currentPupilScaleY = Mathf.Lerp(currentPupilScaleY, targetScaleY, Time.deltaTime * pupilMorphSpeed);
            if (eyePupilImage != null)
            {
                eyePupilImage.color = Color.Lerp(eyePupilImage.color, targetColor, Time.deltaTime * 8f);
                eyePupilImage.transform.localScale = new Vector3(1f, currentPupilScaleY, 1f);
            }

            if (eyeStatusText != null)
            {
                eyeStatusText.text = statusStr;
                eyeStatusText.color = targetColor;
            }
        }

        private void BuildUIHierarchyIfMissing()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();

            // Center upper placement: just beneath CompassBar / above reticle
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, -95f);
            rootRt.sizeDelta = new Vector2(100f, 45f);

            // Eye Frame Container
            GameObject frameGo = new GameObject("Eye_Frame");
            frameGo.transform.SetParent(transform, false);
            RectTransform fRt = frameGo.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0.5f, 0.5f);
            fRt.anchorMax = new Vector2(0.5f, 0.5f);
            fRt.pivot = new Vector2(0.5f, 0.5f);
            fRt.anchoredPosition = new Vector2(0f, 6f);
            fRt.sizeDelta = new Vector2(48f, 22f);

            eyeFrameImage = frameGo.AddComponent<Image>();
            eyeFrameImage.color = hiddenCyan;

            Outline frameOutline = frameGo.AddComponent<Outline>();
            frameOutline.effectDistance = new Vector2(1.5f, 1.5f);
            frameOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);

            // Inner Iris / Pupil
            GameObject pupilGo = new GameObject("Eye_Pupil");
            pupilGo.transform.SetParent(frameGo.transform, false);
            RectTransform pRt = pupilGo.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(18f, 18f);

            eyePupilImage = pupilGo.AddComponent<Image>();
            eyePupilImage.color = Color.white;

            // Status label below eye
            GameObject textGo = new GameObject("Text_Status");
            textGo.transform.SetParent(transform, false);
            RectTransform tRt = textGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 0f);
            tRt.sizeDelta = new Vector2(100f, 16f);

            eyeStatusText = textGo.AddComponent<Text>();
            eyeStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            eyeStatusText.text = "GİZLİ";
            eyeStatusText.fontSize = 11;
            eyeStatusText.fontStyle = FontStyle.Bold;
            eyeStatusText.alignment = TextAnchor.MiddleCenter;
            eyeStatusText.color = hiddenCyan;

            Outline textOutline = textGo.AddComponent<Outline>();
            textOutline.effectDistance = new Vector2(1f, 1f);
            textOutline.effectColor = Color.black;
        }
    }
}
