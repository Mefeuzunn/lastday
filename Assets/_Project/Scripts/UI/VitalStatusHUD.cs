using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    public class VitalStatusHUD : MonoBehaviour
    {
        public static VitalStatusHUD Instance { get; private set; }

        [Header("Health Bar Elements")]
        [SerializeField] private Image healthTrack;
        [SerializeField] private Image healthGhostFill;
        [SerializeField] private Image healthPrimaryFill;
        [SerializeField] private Image healthShimmer;
        [SerializeField] private Image healthIcon;
        [SerializeField] private Text healthText;
        [SerializeField] private RectTransform healthContainer;

        [Header("Armor Bar Elements")]
        [SerializeField] private RectTransform armorContainer;
        [SerializeField] private Image armorTrack;
        [SerializeField] private Image armorPrimaryFill;
        [SerializeField] private Text armorText;
        [SerializeField] private Color armorColor = new Color(0.20f, 0.60f, 0.88f); // #3399E0 Tactical Cyan-Blue

        [Header("Energy / Stamina Bar Elements")]
        [SerializeField] private Image staminaTrack;
        [SerializeField] private Image staminaGhostFill;
        [SerializeField] private Image staminaPrimaryFill;
        [SerializeField] private Image staminaIcon;
        [SerializeField] private Text staminaText;

        [Header("Radial Survival Gauges (38x38)")]
        [SerializeField] private Image hungerRadialFill;
        [SerializeField] private Image hungerIcon;
        [SerializeField] private Text hungerText;

        [SerializeField] private Image thirstRadialFill;
        [SerializeField] private Image thirstIcon;
        [SerializeField] private Text thirstText;

        [SerializeField] private Image infectionRadialFill;
        [SerializeField] private Image infectionIcon;
        [SerializeField] private Text infectionText;

        [Header("Trauma Badges Elements (Diegetic Tactical Pills)")]
        [SerializeField] private RectTransform traumaBadgesContainer;
        [SerializeField] private GameObject badgeBleedingObj;
        [SerializeField] private Image badgeBleedingImg;
        [SerializeField] private Text badgeBleedingText;
        [SerializeField] private Button badgeBleedingBtn;

        [SerializeField] private GameObject badgeFractureObj;
        [SerializeField] private Image badgeFractureImg;
        [SerializeField] private Text badgeFractureText;
        [SerializeField] private Button badgeFractureBtn;

        [SerializeField] private GameObject badgeFeverObj;
        [SerializeField] private Image badgeFeverImg;
        [SerializeField] private Text badgeFeverText;
        [SerializeField] private Button badgeFeverBtn;

        private readonly Color colorBleeding = new Color(0.902f, 0.224f, 0.275f, 0.92f); // #E63946
        private readonly Color colorFracture = new Color(0.969f, 0.498f, 0.000f, 0.92f); // #F77F00
        private readonly Color colorFever    = new Color(0.608f, 0.365f, 0.898f, 0.92f); // #9B5DE5

        [Header("Color Palettes")]
        [SerializeField] private Color healthGradientStart = new Color(0.851f, 0.016f, 0.161f); // #D90429
        [SerializeField] private Color healthGradientEnd = new Color(0.937f, 0.137f, 0.235f);   // #EF233C
        [SerializeField] private Color healthGhostColor = new Color(0.973f, 0.976f, 0.980f);    // #F8F9FA
        [SerializeField] private Color healthShimmerColor = new Color(0.502f, 0.929f, 0.600f);  // #80ED99 emerald shimmer

        [SerializeField] private Color staminaNormalColor = new Color(0.0f, 0.706f, 0.847f);    // #00B4D8
        [SerializeField] private Color staminaWarningColor = new Color(0.969f, 0.498f, 0.0f);   // #F77F00
        [SerializeField] private Color staminaGhostColor = new Color(0.565f, 0.878f, 0.937f, 0.45f); // #90E0EF (Alpha 0.45)

        [SerializeField] private Color hungerNormalColor = new Color(0.957f, 0.635f, 0.380f);   // #F4A261
        [SerializeField] private Color hungerWarningColor = new Color(0.906f, 0.435f, 0.318f);  // #E76F51
        [SerializeField] private Color hungerCriticalColor = new Color(0.902f, 0.224f, 0.275f); // #E63946

        [SerializeField] private Color thirstNormalColor = new Color(0.298f, 0.788f, 0.941f);   // #4CC9F0
        [SerializeField] private Color thirstWarningColor = new Color(0.165f, 0.616f, 0.561f);  // #2A9D8F
        [SerializeField] private Color thirstCriticalColor = new Color(0.902f, 0.224f, 0.275f); // #E63946

        [SerializeField] private Color infectionDormantColor = new Color(0.322f, 0.475f, 0.435f, 0.55f); // #52796F
        [SerializeField] private Color infectionToxicColor = new Color(0.616f, 0.306f, 0.867f);   // #9D4EDD bio-purple
        [SerializeField] private Color infectionAcidColor = new Color(0.541f, 0.788f, 0.149f);    // #8AC926 acid-green

        // State Tracking
        private float targetHealthNorm = 1.0f;
        private float currentHealthNorm = 1.0f;
        private float ghostHealthNorm = 1.0f;
        private float ghostHealthDelayTimer = 0f;
        private float prevHealthVal = 100f;
        private float currentMaxHealth = 100f;

        private float targetStaminaNorm = 1.0f;
        private float currentStaminaNorm = 1.0f;
        private float ghostStaminaNorm = 1.0f;
        private float currentMaxStamina = 100f;
        private bool isStaminaExhausted = false;

        private float targetHungerNorm = 1.0f;
        private float currentHungerNorm = 1.0f;

        private float targetThirstNorm = 1.0f;
        private float currentThirstNorm = 1.0f;

        private float targetInfectionNorm = 0f;
        private float currentInfectionNorm = 0f;

        private Coroutine shimmerCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureZeroInputBlocking();
            EnsureArmorBar();
            EnsureTraumaBadges();
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnStaminaChanged += HandleStaminaChanged;
            GameEvents.OnHungerChanged += HandleHungerChanged;
            GameEvents.OnThirstChanged += HandleThirstChanged;
            GameEvents.OnInfectionChanged += HandleInfectionChanged;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnArmorChanged += HandleArmorChanged;
            TraumaAndInjurySystem.OnTraumaStateChanged += HandleTraumaStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnStaminaChanged -= HandleStaminaChanged;
            GameEvents.OnHungerChanged -= HandleHungerChanged;
            GameEvents.OnThirstChanged -= HandleThirstChanged;
            GameEvents.OnInfectionChanged -= HandleInfectionChanged;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnArmorChanged -= HandleArmorChanged;
            TraumaAndInjurySystem.OnTraumaStateChanged -= HandleTraumaStateChanged;
        }

        private void Start()
        {
            EnsureZeroInputBlocking();
            UpdateTraumaBadgeStates();

            // Try to find player's SurvivalStats immediately if available
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                SurvivalStats stats = player.GetComponent<SurvivalStats>();
                if (stats != null)
                {
                    ResetImmediate(stats.CurrentHealth, stats.MaxHealth, stats.CurrentStamina, stats.MaxStamina,
                        stats.CurrentHunger, stats.MaxHunger, stats.CurrentThirst, stats.MaxThirst,
                        stats.CurrentInfection, stats.MaxInfection);
                }
            }
        }

        private void EnsureZeroInputBlocking()
        {
            // Traverse all Graphic components and force raycastTarget = false,
            // EXCEPT for interactive trauma badge elements
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    if (traumaBadgesContainer != null && graphics[i].transform.IsChildOf(traumaBadgesContainer))
                    {
                        continue;
                    }
                    graphics[i].raycastTarget = false;
                }
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            UpdateHealthVisuals(dt);
            UpdateStaminaVisuals(dt);
            UpdateRadialGaugeVisuals(dt);
            UpdateTraumaBadges(dt);
        }

        // =========================================================================
        // HEALTH BAR: 4-LAYER STACKED FILL SYSTEM
        // =========================================================================
        private void UpdateHealthVisuals(float dt)
        {
            // Primary health fill slides smoothly
            currentHealthNorm = Mathf.MoveTowards(currentHealthNorm, targetHealthNorm, dt * 4.5f);
            if (healthPrimaryFill != null)
            {
                healthPrimaryFill.fillAmount = currentHealthNorm;
                healthPrimaryFill.color = Color.Lerp(healthGradientStart, healthGradientEnd, currentHealthNorm);
            }

            // Ghost damage trail: holds position for 0.35s, then lerps down at speed 2.5f
            if (ghostHealthDelayTimer > 0f)
            {
                ghostHealthDelayTimer -= dt;
            }
            else
            {
                ghostHealthNorm = Mathf.MoveTowards(ghostHealthNorm, currentHealthNorm, dt * 2.5f);
            }

            if (healthGhostFill != null)
            {
                healthGhostFill.fillAmount = ghostHealthNorm;
            }

            // Low health critical state (< 25% HP): rhythmic heartbeat pulse
            if (targetHealthNorm < 0.25f && targetHealthNorm > 0f)
            {
                float pulse = 1.0f + 0.045f * Mathf.Sin(Time.unscaledTime * 8f);
                if (healthContainer != null) healthContainer.localScale = new Vector3(pulse, pulse, 1f);
                if (healthIcon != null)
                {
                    float iconAlpha = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f);
                    healthIcon.color = new Color(1f, 0.15f, 0.15f, iconAlpha);
                }
            }
            else
            {
                if (healthContainer != null) healthContainer.localScale = Vector3.one;
                if (healthIcon != null) healthIcon.color = Color.white;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            currentMaxHealth = max > 0f ? max : 100f;
            float newNorm = Mathf.Clamp01(current / currentMaxHealth);

            // If healed, flash emerald-white shimmer and snap ghost bar up
            if (current > prevHealthVal + 0.5f)
            {
                TriggerHealShimmer();
                ghostHealthNorm = newNorm;
                ghostHealthDelayTimer = 0f;
            }
            else if (current < prevHealthVal - 0.5f)
            {
                // Taking damage: reset 0.35s hold delay
                ghostHealthDelayTimer = 0.35f;
            }

            prevHealthVal = current;
            targetHealthNorm = newNorm;

            if (healthText != null)
            {
                healthText.text = $"CAN  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(currentMaxHealth)}";
            }
        }

        private void HandlePlayerDamaged(float amount)
        {
            ghostHealthDelayTimer = 0.35f;
        }

        private void EnsureArmorBar()
        {
            if (armorContainer != null) return;
            if (healthContainer == null) return;

            GameObject armorObj = new GameObject("ArmorContainer");
            armorObj.transform.SetParent(healthContainer.parent, false);
            armorContainer = armorObj.AddComponent<RectTransform>();
            armorContainer.anchorMin = healthContainer.anchorMin;
            armorContainer.anchorMax = healthContainer.anchorMax;
            armorContainer.pivot = healthContainer.pivot;
            armorContainer.sizeDelta = new Vector2(healthContainer.sizeDelta.x, 14f);
            armorContainer.anchoredPosition = healthContainer.anchoredPosition + new Vector2(0f, healthContainer.sizeDelta.y + 6f);

            // Track
            GameObject trackObj = new GameObject("ArmorTrack");
            trackObj.transform.SetParent(armorContainer, false);
            RectTransform rtTrack = trackObj.AddComponent<RectTransform>();
            rtTrack.anchorMin = Vector2.zero;
            rtTrack.anchorMax = Vector2.one;
            rtTrack.sizeDelta = Vector2.zero;
            armorTrack = trackObj.AddComponent<Image>();
            armorTrack.color = new Color(0.08f, 0.12f, 0.16f, 0.85f);
            armorTrack.raycastTarget = false;

            // Fill
            GameObject fillObj = new GameObject("ArmorFill");
            fillObj.transform.SetParent(armorContainer, false);
            RectTransform rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.sizeDelta = Vector2.zero;
            armorPrimaryFill = fillObj.AddComponent<Image>();
            armorPrimaryFill.type = Image.Type.Filled;
            armorPrimaryFill.fillMethod = Image.FillMethod.Horizontal;
            armorPrimaryFill.color = armorColor;
            armorPrimaryFill.raycastTarget = false;

            // Text Label
            GameObject textObj = new GameObject("ArmorText");
            textObj.transform.SetParent(armorContainer, false);
            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;
            armorText = textObj.AddComponent<Text>();
            armorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            armorText.fontSize = 11;
            armorText.fontStyle = FontStyle.Bold;
            armorText.alignment = TextAnchor.MiddleCenter;
            armorText.color = Color.white;
            armorText.raycastTarget = false;

            armorContainer.gameObject.SetActive(false);
        }

        private void HandleArmorChanged(float current, float max)
        {
            if (armorContainer == null) EnsureArmorBar();
            if (armorContainer == null) return;

            bool hasArmor = current > 0f;
            armorContainer.gameObject.SetActive(hasArmor);

            if (hasArmor)
            {
                float norm = Mathf.Clamp01(current / (max > 0f ? max : 100f));
                if (armorPrimaryFill != null) armorPrimaryFill.fillAmount = norm;
                if (armorText != null) armorText.text = $"ZIRH  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void TriggerHealShimmer()
        {
            if (healthShimmer == null) return;
            if (shimmerCoroutine != null) StopCoroutine(shimmerCoroutine);
            shimmerCoroutine = StartCoroutine(ShimmerRoutine());
        }

        private IEnumerator ShimmerRoutine()
        {
            healthShimmer.gameObject.SetActive(true);
            float duration = 0.32f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Sin(t * Mathf.PI) * 0.75f;
                healthShimmer.color = new Color(healthShimmerColor.r, healthShimmerColor.g, healthShimmerColor.b, alpha);
                yield return null;
            }

            healthShimmer.gameObject.SetActive(false);
        }

        // =========================================================================
        // STAMINA BAR: 3-LAYER STACKED FILL SYSTEM
        // =========================================================================
        private void UpdateStaminaVisuals(float dt)
        {
            currentStaminaNorm = Mathf.MoveTowards(currentStaminaNorm, targetStaminaNorm, dt * 6.0f);
            if (staminaPrimaryFill != null)
            {
                staminaPrimaryFill.fillAmount = currentStaminaNorm;
            }

            ghostStaminaNorm = Mathf.MoveTowards(ghostStaminaNorm, currentStaminaNorm, dt * 3.5f);
            if (staminaGhostFill != null)
            {
                staminaGhostFill.fillAmount = ghostStaminaNorm;
            }

            // Exhaustion warning state (< 15% stamina)
            if (targetStaminaNorm < 0.15f)
            {
                isStaminaExhausted = true;
            }
            else if (targetStaminaNorm >= 0.25f)
            {
                isStaminaExhausted = false;
            }

            if (isStaminaExhausted)
            {
                if (staminaPrimaryFill != null) staminaPrimaryFill.color = staminaWarningColor;
                if (staminaIcon != null)
                {
                    float flashAlpha = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f));
                    staminaIcon.color = new Color(1f, 0.8f, 0.2f, flashAlpha);
                }
            }
            else
            {
                if (staminaPrimaryFill != null) staminaPrimaryFill.color = staminaNormalColor;
                if (staminaIcon != null) staminaIcon.color = Color.white;
            }
        }

        private void HandleStaminaChanged(float current, float max)
        {
            currentMaxStamina = max > 0f ? max : 100f;
            targetStaminaNorm = Mathf.Clamp01(current / currentMaxStamina);

            if (staminaText != null)
            {
                staminaText.text = $"ENERJİ  {Mathf.CeilToInt(targetStaminaNorm * 100f)}%";
            }
        }

        // =========================================================================
        // RADIAL SURVIVAL GAUGES: AÇLIK, SUSUZLUK, ENFEKSİYON
        // =========================================================================
        private void UpdateRadialGaugeVisuals(float dt)
        {
            // 1. Hunger
            currentHungerNorm = Mathf.MoveTowards(currentHungerNorm, targetHungerNorm, dt * 3f);
            if (hungerRadialFill != null)
            {
                hungerRadialFill.fillAmount = currentHungerNorm;
                if (currentHungerNorm > 0.40f)
                {
                    hungerRadialFill.color = hungerNormalColor;
                    if (hungerIcon != null) hungerIcon.color = Color.white;
                }
                else if (currentHungerNorm >= 0.20f)
                {
                    hungerRadialFill.color = hungerWarningColor;
                    if (hungerIcon != null) hungerIcon.color = hungerWarningColor;
                }
                else
                {
                    // Critical pulsing crimson
                    float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 7f);
                    Color crit = new Color(hungerCriticalColor.r, hungerCriticalColor.g, hungerCriticalColor.b, pulse);
                    hungerRadialFill.color = crit;
                    if (hungerIcon != null) hungerIcon.color = crit;
                }
            }

            // 2. Thirst
            currentThirstNorm = Mathf.MoveTowards(currentThirstNorm, targetThirstNorm, dt * 3f);
            if (thirstRadialFill != null)
            {
                thirstRadialFill.fillAmount = currentThirstNorm;
                if (currentThirstNorm > 0.40f)
                {
                    thirstRadialFill.color = thirstNormalColor;
                    if (thirstIcon != null) thirstIcon.color = Color.white;
                }
                else if (currentThirstNorm >= 0.20f)
                {
                    thirstRadialFill.color = thirstWarningColor;
                    if (thirstIcon != null) thirstIcon.color = thirstWarningColor;
                }
                else
                {
                    // Critical pulsing crimson
                    float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 7f);
                    Color crit = new Color(thirstCriticalColor.r, thirstCriticalColor.g, thirstCriticalColor.b, pulse);
                    thirstRadialFill.color = crit;
                    if (thirstIcon != null) thirstIcon.color = crit;
                }
            }

            // 3. Infection (Pathogen load: 0% = Clean, 100% = Critical)
            currentInfectionNorm = Mathf.MoveTowards(currentInfectionNorm, targetInfectionNorm, dt * 3f);
            if (infectionRadialFill != null)
            {
                infectionRadialFill.fillAmount = currentInfectionNorm;

                if (currentInfectionNorm <= 0.01f)
                {
                    infectionRadialFill.color = infectionDormantColor;
                    if (infectionIcon != null) infectionIcon.color = new Color(1f, 1f, 1f, 0.65f);
                }
                else
                {
                    // Toxic bio-purple shifting to acid-green, pulsing faster as infection rises
                    float pulseRate = Mathf.Lerp(4f, 12f, currentInfectionNorm);
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * pulseRate);
                    Color toxic = Color.Lerp(infectionToxicColor, infectionAcidColor, Mathf.Sin(Time.unscaledTime * 3f) * 0.5f + 0.5f);
                    toxic.a = pulse;

                    infectionRadialFill.color = toxic;
                    if (infectionIcon != null) infectionIcon.color = toxic;
                }
            }
        }

        private void HandleHungerChanged(float current, float max)
        {
            float m = max > 0f ? max : 100f;
            targetHungerNorm = Mathf.Clamp01(current / m);
            if (hungerText != null)
            {
                hungerText.text = $"{Mathf.CeilToInt(targetHungerNorm * 100f)}%";
            }
        }

        private void HandleThirstChanged(float current, float max)
        {
            float m = max > 0f ? max : 100f;
            targetThirstNorm = Mathf.Clamp01(current / m);
            if (thirstText != null)
            {
                thirstText.text = $"{Mathf.CeilToInt(targetThirstNorm * 100f)}%";
            }
        }

        private void HandleInfectionChanged(float current, float max)
        {
            float m = max > 0f ? max : 100f;
            targetInfectionNorm = Mathf.Clamp01(current / m);
            if (infectionText != null)
            {
                infectionText.text = $"{Mathf.CeilToInt(targetInfectionNorm * 100f)}%";
            }
        }

        // =========================================================================
        // IMMEDIATE SYNC FOR SAVE/LOAD SYSTEM
        // =========================================================================
        public void ResetImmediate(float hp, float maxHp, float sta, float maxSta, float hunger, float maxHunger, float thirst, float maxThirst, float inf, float maxInf)
        {
            currentMaxHealth = maxHp > 0f ? maxHp : 100f;
            currentMaxStamina = maxSta > 0f ? maxSta : 100f;

            targetHealthNorm = Mathf.Clamp01(hp / currentMaxHealth);
            currentHealthNorm = targetHealthNorm;
            ghostHealthNorm = targetHealthNorm;
            prevHealthVal = hp;
            ghostHealthDelayTimer = 0f;

            targetStaminaNorm = Mathf.Clamp01(sta / currentMaxStamina);
            currentStaminaNorm = targetStaminaNorm;
            ghostStaminaNorm = targetStaminaNorm;

            targetHungerNorm = Mathf.Clamp01(hunger / (maxHunger > 0f ? maxHunger : 100f));
            currentHungerNorm = targetHungerNorm;

            targetThirstNorm = Mathf.Clamp01(thirst / (maxThirst > 0f ? maxThirst : 100f));
            currentThirstNorm = targetThirstNorm;

            targetInfectionNorm = Mathf.Clamp01(inf / (maxInf > 0f ? maxInf : 100f));
            currentInfectionNorm = targetInfectionNorm;

            if (healthPrimaryFill != null) healthPrimaryFill.fillAmount = currentHealthNorm;
            if (healthGhostFill != null) healthGhostFill.fillAmount = ghostHealthNorm;
            if (healthText != null) healthText.text = $"CAN  {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(currentMaxHealth)}";

            if (staminaPrimaryFill != null) staminaPrimaryFill.fillAmount = currentStaminaNorm;
            if (staminaGhostFill != null) staminaGhostFill.fillAmount = ghostStaminaNorm;
            if (staminaText != null) staminaText.text = $"ENERJİ  {Mathf.CeilToInt(targetStaminaNorm * 100f)}%";

            if (hungerRadialFill != null) hungerRadialFill.fillAmount = currentHungerNorm;
            if (hungerText != null) hungerText.text = $"{Mathf.CeilToInt(targetHungerNorm * 100f)}%";

            if (thirstRadialFill != null) thirstRadialFill.fillAmount = currentThirstNorm;
            if (thirstText != null) thirstText.text = $"{Mathf.CeilToInt(targetThirstNorm * 100f)}%";

            if (infectionRadialFill != null) infectionRadialFill.fillAmount = currentInfectionNorm;
            if (infectionText != null) infectionText.text = $"{Mathf.CeilToInt(targetInfectionNorm * 100f)}%";
        }

        // =========================================================================
        // DIEGETIC ANATOMICAL TRAUMA BADGES
        // =========================================================================

        private void EnsureTraumaBadges()
        {
            if (traumaBadgesContainer != null) return;

            Transform existing = transform.Find("TraumaBadges_Container");
            if (existing != null)
            {
                traumaBadgesContainer = existing.GetComponent<RectTransform>();
                badgeBleedingObj = traumaBadgesContainer.Find("Badge_Bleeding")?.gameObject;
                badgeFractureObj = traumaBadgesContainer.Find("Badge_Fracture")?.gameObject;
                badgeFeverObj = traumaBadgesContainer.Find("Badge_Fever")?.gameObject;
                return;
            }

            GameObject containerObj = new GameObject("TraumaBadges_Container");
            containerObj.transform.SetParent(transform, false);
            traumaBadgesContainer = containerObj.AddComponent<RectTransform>();
            traumaBadgesContainer.anchorMin = new Vector2(0f, 0f);
            traumaBadgesContainer.anchorMax = new Vector2(1f, 0f);
            traumaBadgesContainer.pivot = new Vector2(0.5f, 1f);
            traumaBadgesContainer.anchoredPosition = new Vector2(0f, -6f);
            traumaBadgesContainer.sizeDelta = new Vector2(-28f, 26f);

            HorizontalLayoutGroup hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;

            // 1. Bleed Badge (#E63946)
            badgeBleedingObj = CreateBadgePill(traumaBadgesContainer, "Badge_Bleeding", "KANAMA", colorBleeding, uiFont,
                out badgeBleedingImg, out badgeBleedingText, out badgeBleedingBtn);
            badgeBleedingBtn.onClick.AddListener(OnBleedBadgeClicked);

            // 2. Fracture Badge (#F77F00)
            badgeFractureObj = CreateBadgePill(traumaBadgesContainer, "Badge_Fracture", "KIRIK", colorFracture, uiFont,
                out badgeFractureImg, out badgeFractureText, out badgeFractureBtn);
            badgeFractureBtn.onClick.AddListener(OnFractureBadgeClicked);

            // 3. Fever Badge (#9B5DE5)
            badgeFeverObj = CreateBadgePill(traumaBadgesContainer, "Badge_Fever", "ATEŞ", colorFever, uiFont,
                out badgeFeverImg, out badgeFeverText, out badgeFeverBtn);
            badgeFeverBtn.onClick.AddListener(OnFeverBadgeClicked);

            traumaBadgesContainer.gameObject.SetActive(false);
        }

        private GameObject CreateBadgePill(Transform parent, string name, string label, Color baseColor, Font font,
            out Image img, out Text text, out Button btn)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(94f, 26f);

            img = obj.AddComponent<Image>();
            img.color = baseColor;
            img.raycastTarget = true;

            btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            text = textObj.AddComponent<Text>();
            text.font = font;
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);

            obj.SetActive(false);
            return obj;
        }

        private void HandleTraumaStateChanged()
        {
            UpdateTraumaBadgeStates();
        }

        public void UpdateTraumaBadgeStates()
        {
            if (TraumaAndInjurySystem.Instance == null)
            {
                if (traumaBadgesContainer != null) traumaBadgesContainer.gameObject.SetActive(false);
                return;
            }

            bool bleeding = TraumaAndInjurySystem.Instance.IsBleeding;
            bool fracture = TraumaAndInjurySystem.Instance.HasLegFracture;
            bool fever = TraumaAndInjurySystem.Instance.HasAcuteFever;

            bool anyTrauma = bleeding || fracture || fever;
            if (traumaBadgesContainer != null)
            {
                traumaBadgesContainer.gameObject.SetActive(anyTrauma);
            }

            if (badgeBleedingObj != null)
            {
                badgeBleedingObj.SetActive(bleeding);
                if (bleeding && badgeBleedingText != null)
                {
                    badgeBleedingText.text = TraumaAndInjurySystem.Instance.BleedSeverity == 2 ? "AĞIR KANAMA" : "KANAMA";
                }
            }

            if (badgeFractureObj != null)
            {
                badgeFractureObj.SetActive(fracture);
            }

            if (badgeFeverObj != null)
            {
                badgeFeverObj.SetActive(fever);
            }
        }

        private void UpdateTraumaBadges(float dt)
        {
            if (TraumaAndInjurySystem.Instance == null) return;

            // Rhythmic pulsing alert for active badges
            float pulse = 0.78f + 0.22f * Mathf.Sin(Time.unscaledTime * 5.5f);

            if (badgeBleedingObj != null && badgeBleedingObj.activeSelf && badgeBleedingImg != null)
            {
                Color c = colorBleeding;
                c.a = pulse;
                badgeBleedingImg.color = c;
            }

            if (badgeFractureObj != null && badgeFractureObj.activeSelf && badgeFractureImg != null)
            {
                Color c = colorFracture;
                c.a = pulse;
                badgeFractureImg.color = c;
            }

            if (badgeFeverObj != null && badgeFeverObj.activeSelf && badgeFeverImg != null)
            {
                Color c = colorFever;
                c.a = pulse;
                badgeFeverImg.color = c;
            }
        }

        private void OnBleedBadgeClicked()
        {
            if (TraumaAndInjurySystem.Instance == null || !TraumaAndInjurySystem.Instance.IsBleeding) return;

            if (InventorySystem.Instance != null && InventorySystem.Instance.GetItemCount("item_bandage") > 0)
            {
                if (TraumaAndInjurySystem.Instance.TryUseMedicalItem("item_bandage"))
                {
                    InventorySystem.Instance.RemoveItem("item_bandage", 1);
                }
            }
            else if (InventorySystem.Instance != null && InventorySystem.Instance.GetItemCount("item_medkit") > 0)
            {
                if (TraumaAndInjurySystem.Instance.TryUseMedicalItem("item_medkit"))
                {
                    InventorySystem.Instance.RemoveItem("item_medkit", 1);
                }
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("Sargı Bezi Gerekli", 2.0f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                MobileHapticManager.Instance?.TriggerLight();
            }
        }

        private void OnFractureBadgeClicked()
        {
            if (TraumaAndInjurySystem.Instance == null || !TraumaAndInjurySystem.Instance.HasLegFracture) return;

            if (InventorySystem.Instance != null && InventorySystem.Instance.GetItemCount("item_splint") > 0)
            {
                if (TraumaAndInjurySystem.Instance.TryUseMedicalItem("item_splint"))
                {
                    InventorySystem.Instance.RemoveItem("item_splint", 1);
                }
            }
            else if (InventorySystem.Instance != null && InventorySystem.Instance.GetItemCount("item_medkit") > 0)
            {
                if (TraumaAndInjurySystem.Instance.TryUseMedicalItem("item_medkit"))
                {
                    InventorySystem.Instance.RemoveItem("item_medkit", 1);
                }
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("Atel Gerekli", 2.0f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                MobileHapticManager.Instance?.TriggerLight();
            }
        }

        private void OnFeverBadgeClicked()
        {
            if (TraumaAndInjurySystem.Instance == null || !TraumaAndInjurySystem.Instance.HasAcuteFever) return;

            if (InventorySystem.Instance != null && InventorySystem.Instance.GetItemCount("item_painkillers") > 0)
            {
                if (TraumaAndInjurySystem.Instance.TryUseMedicalItem("item_painkillers"))
                {
                    InventorySystem.Instance.RemoveItem("item_painkillers", 1);
                }
            }
            else if (InventorySystem.Instance != null && InventorySystem.Instance.GetItemCount("item_antiviral") > 0)
            {
                if (TraumaAndInjurySystem.Instance.TryUseMedicalItem("item_antiviral"))
                {
                    InventorySystem.Instance.RemoveItem("item_antiviral", 1);
                }
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("Ağrı Kesici Gerekli", 2.0f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                MobileHapticManager.Instance?.TriggerLight();
            }
        }
    }
}
