using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 3: Dedicated Apex Boss Encounter HUD.
    /// Fades in top-center viewport (cleanly below CompassBar, within SafeArea)
    /// when within 38m of an active Apex Boss.
    /// Displays Turkish title, segmented Crimson Health Bar, Slate-Blue Armor Bar, and Phase ticks.
    /// </summary>
    public class BossHealthBarHUD : MonoBehaviour
    {
        private static BossHealthBarHUD _instance;
        public static BossHealthBarHUD Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BossHealthBarHUD>();
                    if (_instance == null)
                    {
                        Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                        if (hudCanvas != null)
                        {
                            GameObject uiObj = new GameObject("BossHealthBar_HUD");
                            uiObj.transform.SetParent(hudCanvas.transform, false);
                            _instance = uiObj.AddComponent<BossHealthBarHUD>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 38f;

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text bossTitleText;
        [SerializeField] private Text bossSubtitleText;
        [SerializeField] private Image healthBarPrimaryFill;
        [SerializeField] private Image healthBarGhostFill;
        [SerializeField] private Image armorBarFill;
        [SerializeField] private GameObject armorBarContainer;
        [SerializeField] private Text healthPercentText;

        private MutantBossAI activeBoss;
        private Transform playerTransform;
        private float ghostLagTimer = 0f;
        private float targetHealthNorm = 1.0f;
        private float currentGhostNorm = 1.0f;

        public bool IsActive => activeBoss != null && !activeBoss.IsDead && canvasGroup != null && canvasGroup.alpha > 0.05f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                if (hudCanvas != null)
                {
                    GameObject uiObj = new GameObject("BossHealthBar_HUD");
                    uiObj.transform.SetParent(hudCanvas.transform, false);
                    Instance = uiObj.AddComponent<BossHealthBarHUD>();
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildUIHierarchyIfEmpty();
        }

        private void Start()
        {
            FindPlayer();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            FindNearestActiveBoss();

            if (activeBoss != null && !activeBoss.IsDead)
            {
                // Fade In
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * 3.5f);
                }

                UpdateBossStatsDisplay();
            }
            else
            {
                // Fade Out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * 3.5f);
                }
            }
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void FindNearestActiveBoss()
        {
            if (activeBoss != null && !activeBoss.IsDead)
            {
                float curDist = Vector3.Distance(playerTransform.position, activeBoss.transform.position);
                if (curDist <= detectionRadius + 5f)
                {
                    return; // Retain currently tracked boss
                }
            }

            activeBoss = null;
            MutantBossAI[] allBosses = FindObjectsByType<MutantBossAI>(FindObjectsSortMode.None);
            float nearestDist = detectionRadius;

            for (int i = 0; i < allBosses.Length; i++)
            {
                MutantBossAI b = allBosses[i];
                if (b == null || b.IsDead) continue;

                float dist = Vector3.Distance(playerTransform.position, b.transform.position);
                if (dist <= nearestDist)
                {
                    nearestDist = dist;
                    activeBoss = b;
                }
            }
        }

        private void UpdateBossStatsDisplay()
        {
            if (activeBoss == null) return;

            if (bossTitleText != null)
            {
                bossTitleText.text = activeBoss.DisplayName;
            }
            if (bossSubtitleText != null)
            {
                bossSubtitleText.text = activeBoss.Subtitle;
            }

            // Health calculation
            targetHealthNorm = Mathf.Clamp01(activeBoss.CurrentHealth / activeBoss.MaxHealth);

            if (healthBarPrimaryFill != null)
            {
                healthBarPrimaryFill.fillAmount = targetHealthNorm;
            }

            // Ghost Lag fill
            if (currentGhostNorm > targetHealthNorm)
            {
                ghostLagTimer += Time.deltaTime;
                if (ghostLagTimer >= 0.25f)
                {
                    currentGhostNorm = Mathf.MoveTowards(currentGhostNorm, targetHealthNorm, Time.deltaTime * 0.45f);
                }
            }
            else
            {
                currentGhostNorm = targetHealthNorm;
                ghostLagTimer = 0f;
            }

            if (healthBarGhostFill != null)
            {
                healthBarGhostFill.fillAmount = currentGhostNorm;
            }

            if (healthPercentText != null)
            {
                healthPercentText.text = $"%{(int)(targetHealthNorm * 100f)}";
            }

            // Armor Shield calculation
            if (activeBoss.HasArmorShield)
            {
                if (armorBarContainer != null) armorBarContainer.SetActive(true);
                if (armorBarFill != null)
                {
                    armorBarFill.fillAmount = Mathf.Clamp01(activeBoss.CurrentArmorShield / activeBoss.MaxArmorShield);
                }
            }
            else
            {
                if (armorBarContainer != null) armorBarContainer.SetActive(false);
            }
        }

        // =========================================================================
        // PROCEDURAL SAFEAREA UI BUILDER
        // =========================================================================

        private void BuildUIHierarchyIfEmpty()
        {
            if (canvasGroup != null) return;

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RectTransform rtRoot = GetComponent<RectTransform>();
            if (rtRoot == null) rtRoot = gameObject.AddComponent<RectTransform>();

            // Positioned cleanly below CompassBar at top-center
            rtRoot.anchorMin = new Vector2(0.5f, 1f);
            rtRoot.anchorMax = new Vector2(0.5f, 1f);
            rtRoot.pivot = new Vector2(0.5f, 1f);
            rtRoot.anchoredPosition = new Vector2(0f, -82f);
            rtRoot.sizeDelta = new Vector2(480f, 68f);

            // Background Frame
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.13f, 0.88f);

            // Title & Subtitle Container
            GameObject titleObj = new GameObject("BossTitle");
            titleObj.transform.SetParent(transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0f, 0.62f);
            rtTitle.anchorMax = new Vector2(0.75f, 1f);
            rtTitle.offsetMin = new Vector2(16f, 0f);
            rtTitle.offsetMax = Vector2.zero;

            bossTitleText = titleObj.AddComponent<Text>();
            bossTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bossTitleText.fontSize = 14;
            bossTitleText.fontStyle = FontStyle.Bold;
            bossTitleText.alignment = TextAnchor.MiddleLeft;
            bossTitleText.color = new Color(0.96f, 0.28f, 0.32f); // #F54752 Crimson
            bossTitleText.text = "BÖLGE LİDERİ";

            GameObject subObj = new GameObject("BossSubtitle");
            subObj.transform.SetParent(transform, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.35f, 0.62f);
            rtSub.anchorMax = new Vector2(0.95f, 1f);
            rtSub.offsetMin = Vector2.zero;
            rtSub.offsetMax = new Vector2(-16f, 0f);

            bossSubtitleText = subObj.AddComponent<Text>();
            bossSubtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bossSubtitleText.fontSize = 11;
            bossSubtitleText.alignment = TextAnchor.MiddleRight;
            bossSubtitleText.color = new Color(0.70f, 0.74f, 0.78f);
            bossSubtitleText.text = "TEHLİKE SEVİYESİ: MAKSİMUM";

            // Health Bar Track
            GameObject hpTrack = new GameObject("HealthTrack");
            hpTrack.transform.SetParent(transform, false);
            RectTransform rtTrack = hpTrack.AddComponent<RectTransform>();
            rtTrack.anchorMin = new Vector2(0f, 0.22f);
            rtTrack.anchorMax = new Vector2(1f, 0.58f);
            rtTrack.offsetMin = new Vector2(16f, 0f);
            rtTrack.offsetMax = new Vector2(-16f, 0f);

            Image trackImg = hpTrack.AddComponent<Image>();
            trackImg.color = new Color(0.18f, 0.20f, 0.24f, 0.95f);

            // Ghost Lag Fill
            GameObject ghostObj = new GameObject("HealthGhostFill");
            ghostObj.transform.SetParent(hpTrack.transform, false);
            RectTransform rtGhost = ghostObj.AddComponent<RectTransform>();
            rtGhost.anchorMin = Vector2.zero;
            rtGhost.anchorMax = Vector2.one;
            rtGhost.sizeDelta = Vector2.zero;

            healthBarGhostFill = ghostObj.AddComponent<Image>();
            healthBarGhostFill.color = new Color(0.95f, 0.95f, 0.95f, 0.75f);
            healthBarGhostFill.type = Image.Type.Filled;
            healthBarGhostFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarGhostFill.fillOrigin = 0;

            // Primary Health Fill (Crimson)
            GameObject fillObj = new GameObject("HealthPrimaryFill");
            fillObj.transform.SetParent(hpTrack.transform, false);
            RectTransform rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.sizeDelta = Vector2.zero;

            healthBarPrimaryFill = fillObj.AddComponent<Image>();
            healthBarPrimaryFill.color = new Color(0.85f, 0.08f, 0.16f, 1f); // #D90429
            healthBarPrimaryFill.type = Image.Type.Filled;
            healthBarPrimaryFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarPrimaryFill.fillOrigin = 0;

            // Phase threshold tick marks (65%, 30%)
            CreatePhaseTick(hpTrack.transform, 0.65f);
            CreatePhaseTick(hpTrack.transform, 0.30f);

            // Armor Shield Container (Slate-Blue)
            armorBarContainer = new GameObject("ArmorTrack");
            armorBarContainer.transform.SetParent(transform, false);
            RectTransform rtArmor = armorBarContainer.AddComponent<RectTransform>();
            rtArmor.anchorMin = new Vector2(0f, 0.05f);
            rtArmor.anchorMax = new Vector2(1f, 0.18f);
            rtArmor.offsetMin = new Vector2(16f, 0f);
            rtArmor.offsetMax = new Vector2(-16f, 0f);

            Image armorTrackImg = armorBarContainer.AddComponent<Image>();
            armorTrackImg.color = new Color(0.12f, 0.15f, 0.20f, 0.85f);

            GameObject armorFillObj = new GameObject("ArmorFill");
            armorFillObj.transform.SetParent(armorBarContainer.transform, false);
            RectTransform rtArmorFill = armorFillObj.AddComponent<RectTransform>();
            rtArmorFill.anchorMin = Vector2.zero;
            rtArmorFill.anchorMax = Vector2.one;
            rtArmorFill.sizeDelta = Vector2.zero;

            armorBarFill = armorFillObj.AddComponent<Image>();
            armorBarFill.color = new Color(0.27f, 0.51f, 0.71f, 1f); // #4682B4 Slate-Blue
            armorBarFill.type = Image.Type.Filled;
            armorBarFill.fillMethod = Image.FillMethod.Horizontal;
            armorBarFill.fillOrigin = 0;
            armorBarContainer.SetActive(false);
        }

        private void CreatePhaseTick(Transform parent, float normPosition)
        {
            GameObject tick = new GameObject($"PhaseTick_{normPosition}");
            tick.transform.SetParent(parent, false);

            RectTransform rt = tick.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(normPosition, 0f);
            rt.anchorMax = new Vector2(normPosition, 1f);
            rt.sizeDelta = new Vector2(2f, 0f);

            Image img = tick.AddComponent<Image>();
            img.color = new Color(0.10f, 0.12f, 0.15f, 0.95f);
        }
    }
}
