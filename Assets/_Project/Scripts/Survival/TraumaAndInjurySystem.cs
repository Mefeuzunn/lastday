using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.AI;

namespace ProjectSurvival.Survival
{
    public enum TraumaType
    {
        Bleeding,
        LegFracture,
        AcuteFever
    }

    /// <summary>
    /// Phase 2 Atomic Micro-Step 2A: Anatomical Trauma & Injury State Engine.
    /// Manages 3 realistic trauma states:
    /// 1. Bleeding (Light: 1.2 HP/s, Heavy: 2.4 HP/s, crimson edge vignette, blood-drop particles).
    /// 2. Leg Fracture (Sprint locked, 42% walk speed reduction, camera limp roll, bone-snap SFX).
    /// 3. Acute Fever (Triggered at >= 65% Infection, +45% stamina drain, 50% slower stamina regen, heat haze).
    /// Provides context-smart first-aid item dispatching for Bandages, Splints, Painkillers, and Medkits.
    /// </summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class TraumaAndInjurySystem : MonoBehaviour
    {
        public static TraumaAndInjurySystem Instance { get; private set; }

        public static event Action OnTraumaStateChanged;

        [Header("Injury States")]
        [SerializeField] private bool isBleeding = false;
        [Range(1, 2)]
        [SerializeField] private int bleedSeverity = 1; // 1 = Light (1.2 HP/s), 2 = Heavy (2.4 HP/s)
        [SerializeField] private bool hasLegFracture = false;
        [SerializeField] private bool hasAcuteFever = false;
        [SerializeField] private float feverSuppressedTimer = 0f;

        [Header("Trauma Rates & Settings")]
        [SerializeField] private float lightBleedDps = 1.2f;
        [SerializeField] private float heavyBleedDps = 2.4f;
        [SerializeField] private float feverInfectionThreshold = 65f;
        [SerializeField] private float painkillersSuppressionDuration = 180f;

        [Header("Survival Alchemy Buffs (Phase 2B)")]
        [SerializeField] private float adrenalineBuffTimer = 0f;
        [SerializeField] private float herbalTeaBuffTimer = 0f;

        [Header("Hypothermia & Exposure State")]
        [SerializeField] private bool isHypothermic = false;
        [SerializeField] private float wetnessLevel = 0f;
        [SerializeField] private float thermalResistanceBuffTimer = 0f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem bloodDropParticles;
        [SerializeField] private Image vignetteOverlayImage;

        private SurvivalStats survivalStats;
        private PlayerController playerController;
        private float bloodDropTimer = 0f;
        private float limpBobTimer = 0f;
        private float currentLimpRoll = 0f;

        public bool IsBleeding => isBleeding;
        public int BleedSeverity => bleedSeverity;
        public bool HasLegFracture => hasLegFracture;
        public bool HasAcuteFever => hasAcuteFever && feverSuppressedTimer <= 0f;
        public bool IsFeverSuppressed => feverSuppressedTimer > 0f;
        public float FeverSuppressedRemaining => Mathf.Max(0f, feverSuppressedTimer);
        public float CurrentLimpRoll => currentLimpRoll;
        public bool IsAdrenalineActive => adrenalineBuffTimer > 0f;
        public float AdrenalineRemaining => Mathf.Max(0f, adrenalineBuffTimer);
        public bool IsHerbalTeaActive => herbalTeaBuffTimer > 0f;
        public float HerbalTeaRemaining => Mathf.Max(0f, herbalTeaBuffTimer);
        public bool IsHypothermic => isHypothermic;
        public float WetnessLevel => wetnessLevel;
        public float ThermalResistanceRemaining => Mathf.Max(0f, thermalResistanceBuffTimer);

        public void SetHypothermic(bool hypothermic) => isHypothermic = hypothermic;
        public void SetWetness(float wetness) => wetnessLevel = Mathf.Clamp01(wetness);
        public void ApplyThermalResistance(float duration) => thermalResistanceBuffTimer = Mathf.Max(thermalResistanceBuffTimer, duration);

        public void SerializeState(SaveData data)
        {
            if (data == null) return;
            data.isBleeding = isBleeding;
            data.bleedingSeverity = (float)bleedSeverity;
            data.hasLegFracture = hasLegFracture;
            data.hasAcuteFever = hasAcuteFever;
            data.isHypothermic = isHypothermic;
            data.wetnessLevel = wetnessLevel;
            data.painkillerBuffTimer = feverSuppressedTimer;
            data.adrenalineBuffTimer = adrenalineBuffTimer;
            data.herbalTeaBuffTimer = herbalTeaBuffTimer;
            data.thermalResistanceBuffTimer = thermalResistanceBuffTimer;
        }

        public void RestoreState(SaveData data)
        {
            if (data == null) return;
            isBleeding = data.isBleeding;
            bleedSeverity = Mathf.Clamp(Mathf.RoundToInt(data.bleedingSeverity), 1, 2);
            hasLegFracture = data.hasLegFracture;
            hasAcuteFever = data.hasAcuteFever;
            isHypothermic = data.isHypothermic;
            wetnessLevel = Mathf.Clamp01(data.wetnessLevel);
            feverSuppressedTimer = Mathf.Max(0f, data.painkillerBuffTimer);
            adrenalineBuffTimer = Mathf.Max(0f, data.adrenalineBuffTimer);
            herbalTeaBuffTimer = Mathf.Max(0f, data.herbalTeaBuffTimer);
            thermalResistanceBuffTimer = Mathf.Max(0f, data.thermalResistanceBuffTimer);

            OnTraumaStateChanged?.Invoke();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            survivalStats = GetComponent<SurvivalStats>();
            playerController = GetComponent<PlayerController>();

            EnsureBloodParticles();
            EnsureVignetteOverlay();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamaged += HandleGenericDamage;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamaged -= HandleGenericDamage;
        }

        private void Start()
        {
            EvaluateFeverThreshold();
            NotifyStateChanged();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Process Continuous Bleeding
            if (isBleeding)
            {
                float dps = (bleedSeverity == 2) ? heavyBleedDps : lightBleedDps;
                if (survivalStats != null && !survivalStats.Health.Equals(0f))
                {
                    // Internal hemorrhaging directly reduces health
                    survivalStats.TakeDamage(dps * dt);
                }

                // Periodic blood droplet splatter under boots
                bloodDropTimer += dt;
                float interval = (bleedSeverity == 2) ? 0.45f : 0.90f;
                if (bloodDropTimer >= interval)
                {
                    bloodDropTimer = 0f;
                    EmitBloodDrop();
                }
            }

            // 2. Process Fever & Painkiller Suppression
            if (feverSuppressedTimer > 0f)
            {
                feverSuppressedTimer -= dt;
                if (feverSuppressedTimer <= 0f)
                {
                    feverSuppressedTimer = 0f;
                    if (hasAcuteFever)
                    {
                        GameEvents.TriggerObjectiveToastRequested("AĞRI KESİCİ ETKİSİ GEÇTİ: ATEŞ NÜKSETTİ", 2.5f);
                    }
                    NotifyStateChanged();
                }
            }
            EvaluateFeverThreshold();

            // 2b. Process Survival Alchemy Buff Timers
            if (adrenalineBuffTimer > 0f)
            {
                adrenalineBuffTimer -= dt;
                if (adrenalineBuffTimer <= 0f)
                {
                    adrenalineBuffTimer = 0f;
                    GameEvents.TriggerObjectiveToastRequested("ADRENALİN ETKİSİ SONA ERDİ", 2.0f);
                    NotifyStateChanged();
                }
            }

            if (herbalTeaBuffTimer > 0f)
            {
                herbalTeaBuffTimer -= dt;
                if (herbalTeaBuffTimer <= 0f)
                {
                    herbalTeaBuffTimer = 0f;
                    NotifyStateChanged();
                }
            }

            // 3. Process Leg Fracture Limp Cadence
            if (hasLegFracture)
            {
                if (playerController != null && playerController.IsMoving)
                {
                    limpBobTimer += dt * 6.5f;
                    // Asymmetric limp roll cadence (-2.2° to +2.2°)
                    currentLimpRoll = Mathf.Sin(limpBobTimer) * 2.2f;
                }
                else
                {
                    currentLimpRoll = Mathf.MoveTowards(currentLimpRoll, 0f, dt * 8f);
                    if (Mathf.Abs(currentLimpRoll) < 0.001f) currentLimpRoll = 0f;
                }
            }
            else
            {
                currentLimpRoll = Mathf.MoveTowards(currentLimpRoll, 0f, dt * 8f);
                if (Mathf.Abs(currentLimpRoll) < 0.001f) currentLimpRoll = 0f;
            }

            // 4. Update Screen Edge Vignette Pulses
            UpdateVignetteVisuals();
        }

        private void EvaluateFeverThreshold()
        {
            if (survivalStats == null) return;

            bool shouldHaveFever = survivalStats.Infection >= feverInfectionThreshold;
            if (shouldHaveFever != hasAcuteFever)
            {
                hasAcuteFever = shouldHaveFever;
                if (hasAcuteFever)
                {
                    GameEvents.TriggerObjectiveToastRequested("YÜKSEK ATEŞ: ENFEKSİYON %65 ÜZERİNDE!", 3.0f);
                    MobileHapticManager.Instance?.TriggerHeavy();
                }
                NotifyStateChanged();
            }
        }

        // =========================================================================
        // INJURY INFLICTION TRIGGERS
        // =========================================================================

        public void InflictBleeding(int severity = 1)
        {
            if (isBleeding && bleedSeverity == 1 && severity >= 1)
            {
                bleedSeverity = 2; // Escalate to heavy bleeding
                GameEvents.TriggerObjectiveToastRequested("AĞIR KANAMA BAŞLADI! (2.4 HP/sn)", 3.0f);
            }
            else if (!isBleeding)
            {
                isBleeding = true;
                bleedSeverity = Mathf.Clamp(severity, 1, 2);
                string text = bleedSeverity == 2 ? "AĞIR KANAMA BAŞLADI! (SARGI BEZİ GEREKLİ)" : "KANAMA BAŞLADI! (SARGI BEZİ GEREKLİ)";
                GameEvents.TriggerObjectiveToastRequested(text, 2.5f);
            }

            MobileHapticManager.Instance?.TriggerHeavy();
            NotifyStateChanged();
        }

        public void InflictLegFracture(float fallHeight = 4f)
        {
            if (hasLegFracture) return;

            hasLegFracture = true;
            if (playerController != null)
            {
                playerController.SetSprint(false);
            }

            MobileHapticManager.Instance?.TriggerHeavy();
            ProceduralAudioManager.Instance?.PlayBoneFracture();
            GameEvents.TriggerCameraShakeRequested(0.40f, 0.35f);
            GameEvents.TriggerObjectiveToastRequested("BACAK KIRILDI / BURKULDU! (ATEL GEREKLİ)", 3.5f);

            NotifyStateChanged();
        }

        public void InflictAcuteFever()
        {
            if (hasAcuteFever) return;
            hasAcuteFever = true;
            NotifyStateChanged();
        }

        public void EvaluateZombieAttack(ZombieType zType, float rawDamage)
        {
            // 35% chance for Runner / Lurker to cause bleeding
            if (zType == ZombieType.Runner)
            {
                if (UnityEngine.Random.value <= 0.35f)
                {
                    InflictBleeding(1);
                }
            }
            else if (zType == ZombieType.Tank)
            {
                // Tank heavy slam causes leg fracture
                InflictLegFracture(4.5f);
            }
            else
            {
                if (UnityEngine.Random.value <= 0.15f)
                {
                    InflictBleeding(1);
                }
            }
        }

        public void EvaluateBossAttack(MutantBossType bType, float rawDamage, bool isSlam)
        {
            if (bType == MutantBossType.Juggernaut && isSlam)
            {
                InflictLegFracture(5.0f);
            }
            else if (bType == MutantBossType.BioRavager)
            {
                if (UnityEngine.Random.value <= 0.65f)
                {
                    InflictBleeding(2);
                }
            }
        }

        public void EvaluateLaceration(float lacerationChance)
        {
            if (UnityEngine.Random.value <= lacerationChance)
            {
                InflictBleeding(1);
            }
        }

        private void HandleGenericDamage(float amount)
        {
            // Fallback: very heavy single-hit damage (> 40 HP) can cause bleeding
            if (amount >= 40f && !isBleeding && UnityEngine.Random.value <= 0.30f)
            {
                InflictBleeding(1);
            }
        }

        // =========================================================================
        // FIRST-AID TREATMENTS & CURATIVE DISPATCHER
        // =========================================================================

        public bool TreatBleeding()
        {
            if (!isBleeding) return false;

            isBleeding = false;
            bleedSeverity = 1;
            ProceduralAudioManager.Instance?.PlayBandageWrap();
            MobileHapticManager.Instance?.TriggerLight();
            GameEvents.TriggerObjectiveToastRequested("KANAMA DURDURULDU", 2.0f);

            NotifyStateChanged();
            return true;
        }

        public bool TreatLegFracture()
        {
            if (!hasLegFracture) return false;

            hasLegFracture = false;
            currentLimpRoll = 0f;
            ProceduralAudioManager.Instance?.PlaySplintEquip();
            MobileHapticManager.Instance?.TriggerLight();
            GameEvents.TriggerObjectiveToastRequested("ATEL TAKILDI — KIRIK SABİTLENDİ", 2.5f);

            NotifyStateChanged();
            return true;
        }

        public bool TreatFever(bool temporary = false)
        {
            if (!hasAcuteFever && feverSuppressedTimer <= 0f) return false;

            if (temporary)
            {
                feverSuppressedTimer = painkillersSuppressionDuration;
                ProceduralAudioManager.Instance?.PlayPillSwallow();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("AĞRI KESİCİ ALINDI: ATEŞ 180 SN BASKILANDI", 2.5f);
            }
            else
            {
                hasAcuteFever = false;
                feverSuppressedTimer = 0f;
                ProceduralAudioManager.Instance?.PlayFirstAid();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("ANTİVİRAL TEDAVİ: ENFEKSİYON KRİZİ GEÇTİ", 2.5f);
            }

            NotifyStateChanged();
            return true;
        }

        public void ActivateAdrenaline(float duration = 25f)
        {
            adrenalineBuffTimer = duration;
            survivalStats?.RestoreStamina(100f);
            ProceduralAudioManager.Instance?.PlayAdrenalineHeartRush();
            MobileHapticManager.Instance?.TriggerMedium();
            GameEvents.TriggerObjectiveToastRequested("ADRENALİN ETKİSİ: 25 SN SINIRSIZ DAYANIKLILIK VE HIZ ARTIŞI!", 3.0f);
            NotifyStateChanged();
        }

        public void ActivateHerbalTea(float duration = 120f)
        {
            herbalTeaBuffTimer = duration;
            ProceduralAudioManager.Instance?.PlayDrink();
            MobileHapticManager.Instance?.TriggerLight();
            GameEvents.TriggerObjectiveToastRequested("BİTKİSEL ÇAY ETKİSİ: 120 SN DAYANIKLILIK DİRENCİ!", 2.5f);
            NotifyStateChanged();
        }

        /// <summary>
        /// Context-smart medical item consumption validator.
        /// Prevents wasting first-aid resources if the player is healthy and uninjured.
        /// </summary>
        public bool TryUseMedicalItem(string itemId)
        {
            bool hasInjury = isBleeding || hasLegFracture || HasAcuteFever;
            bool missingHealth = survivalStats != null && survivalStats.CurrentHealth < survivalStats.MaxHealth - 1.0f;
            bool hasInfection = survivalStats != null && survivalStats.CurrentInfection > 0.5f;

            if (string.Equals(itemId, "item_bandage", StringComparison.OrdinalIgnoreCase))
            {
                if (isBleeding)
                {
                    TreatBleeding();
                    survivalStats?.Heal(10f);
                    return true;
                }
                else if (missingHealth)
                {
                    survivalStats?.Heal(15f);
                    ProceduralAudioManager.Instance?.PlayBandageWrap();
                    MobileHapticManager.Instance?.TriggerLight();
                    GameEvents.TriggerObjectiveToastRequested("SARGI BEZİ UYGULANDI (+15 CAN)", 2.0f);
                    return true;
                }
                else
                {
                    ShowNoInjuryToast();
                    return false;
                }
            }
            else if (string.Equals(itemId, "item_splint", StringComparison.OrdinalIgnoreCase))
            {
                if (hasLegFracture)
                {
                    TreatLegFracture();
                    return true;
                }
                else
                {
                    ShowNoInjuryToast();
                    return false;
                }
            }
            else if (string.Equals(itemId, "item_painkillers", StringComparison.OrdinalIgnoreCase))
            {
                if (hasAcuteFever || hasLegFracture)
                {
                    TreatFever(temporary: true);
                    return true;
                }
                else if (missingHealth)
                {
                    survivalStats?.Heal(10f);
                    ProceduralAudioManager.Instance?.PlayPillSwallow();
                    MobileHapticManager.Instance?.TriggerLight();
                    GameEvents.TriggerObjectiveToastRequested("AĞRI KESİCİ ALINDI (+10 CAN)", 2.0f);
                    return true;
                }
                else
                {
                    ShowNoInjuryToast();
                    return false;
                }
            }
            else if (string.Equals(itemId, "item_medkit", StringComparison.OrdinalIgnoreCase))
            {
                if (isBleeding || hasLegFracture || missingHealth)
                {
                    if (isBleeding) isBleeding = false;
                    if (hasLegFracture) hasLegFracture = false;

                    survivalStats?.Heal(50f);
                    survivalStats?.ModifyInfection(-15f);
                    ProceduralAudioManager.Instance?.PlayFirstAid();
                    MobileHapticManager.Instance?.TriggerLight();
                    GameEvents.TriggerObjectiveToastRequested("İLK YARDIM KİTİ: TÜM YARALAR SARILDI (+50 CAN)", 3.0f);

                    NotifyStateChanged();
                    return true;
                }
                else
                {
                    ShowNoInjuryToast();
                    return false;
                }
            }
            else if (string.Equals(itemId, "item_antiviral", StringComparison.OrdinalIgnoreCase))
            {
                if (hasInfection || hasAcuteFever)
                {
                    survivalStats?.ModifyInfection(-45f);
                    TreatFever(temporary: false);
                    return true;
                }
                else
                {
                    ShowNoInjuryToast();
                    return false;
                }
            }
            else if (string.Equals(itemId, "item_adrenaline", StringComparison.OrdinalIgnoreCase))
            {
                ActivateAdrenaline(25f);
                return true;
            }
            else if (string.Equals(itemId, "item_herbal_tea", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Hydrate(30f);
                survivalStats?.ModifyInfection(-12f);
                ActivateHerbalTea(120f);
                return true;
            }
            else if (string.Equals(itemId, "item_purified_water", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Hydrate(50f);
                survivalStats?.RestoreStamina(10f);
                survivalStats?.ModifyInfection(-5f);
                ProceduralAudioManager.Instance?.PlayDrink();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("ARITILMIŞ SU İÇİLDİ (+50 SU, +10 ENERJİ, -%5 ENFEKSİYON)", 2.0f);
                return true;
            }
            else if (string.Equals(itemId, "item_hot_stew", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Feed(55f);
                survivalStats?.Hydrate(25f);
                survivalStats?.Heal(15f);
                ProceduralAudioManager.Instance?.PlayEat();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("SICAK YAHNİ TÜKETİLDİ (+55 TOKLUK, +25 SU, +15 CAN)", 2.5f);
                return true;
            }
            else if (string.Equals(itemId, "item_raw_meat", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Feed(15f);
                ProceduralAudioManager.Instance?.PlayEat();
                MobileHapticManager.Instance?.TriggerLight();
                bool nausea = UnityEngine.Random.value <= 0.40f;
                if (nausea)
                {
                    survivalStats?.ModifyInfection(12f);
                    GameEvents.TriggerObjectiveToastRequested("ÇİĞ ET TÜKETİLDİ (+15 TOKLUK, +%12 ENFEKSİYON BULANTISI)", 3.0f);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("ÇİĞ ET TÜKETİLDİ (+15 TOKLUK)", 2.5f);
                }
                return true;
            }
            else if (string.Equals(itemId, "item_cooked_meat", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Feed(65f);
                survivalStats?.Heal(22f);
                survivalStats?.RestoreStamina(15f);
                survivalStats?.ModifyInfection(-5f);
                ProceduralAudioManager.Instance?.PlayEat();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("IZGARA AV ETİ TÜKETİLDİ (+65 TOKLUK, +22 CAN, +15 ENERJİ, -%5 ENFEKSİYON)", 3.0f);
                return true;
            }
            else if (string.Equals(itemId, "item_raw_fish", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Feed(15f);
                ProceduralAudioManager.Instance?.PlayEat();
                MobileHapticManager.Instance?.TriggerLight();
                bool nausea = UnityEngine.Random.value <= 0.40f;
                if (nausea)
                {
                    survivalStats?.ModifyInfection(12f);
                    GameEvents.TriggerObjectiveToastRequested("ÇİĞ BALIK TÜKETİLDİ (+15 TOKLUK, +%12 PARAZİT RİSKİ)", 3.0f);
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("ÇİĞ BALIK TÜKETİLDİ (+15 TOKLUK)", 2.5f);
                }
                return true;
            }
            else if (string.Equals(itemId, "item_grilled_fish", StringComparison.OrdinalIgnoreCase))
            {
                survivalStats?.Feed(50f);
                survivalStats?.Hydrate(15f);
                survivalStats?.Heal(18f);
                survivalStats?.ApplyStaminaRegenBuff(120f, 1.6f);
                ProceduralAudioManager.Instance?.PlayEat();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("KÖZDE BALIK TÜKETİLDİ (+50 TOKLUK, +15 SU, +18 CAN, 120 SN DAYANIKLILIK ARTIŞI)", 3.0f);
                return true;
            }

            return false;
        }

        private void ShowNoInjuryToast()
        {
            GameEvents.TriggerObjectiveToastRequested("Tedavi Gerektiren Yaralanma Yok", 2.0f);
            ProceduralAudioManager.Instance?.PlaySoftError();
        }

        private void NotifyStateChanged()
        {
            OnTraumaStateChanged?.Invoke();
        }

        // =========================================================================
        // PROCEDURAL VISUALS & PARTICLES
        // =========================================================================

        private void EnsureBloodParticles()
        {
            if (bloodDropParticles != null) return;

            Transform existing = transform.Find("BloodDrop_Emitter");
            if (existing != null)
            {
                bloodDropParticles = existing.GetComponent<ParticleSystem>();
                return;
            }

            GameObject bGo = new GameObject("BloodDrop_Emitter");
            bGo.transform.SetParent(transform, false);
            bGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            bloodDropParticles = bGo.AddComponent<ParticleSystem>();
            var main = bloodDropParticles.main;
            main.startSize = 0.18f;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.5f;
            main.startColor = new Color(0.65f, 0.02f, 0.05f, 0.85f); // Crimson blood
            main.maxParticles = 40;
            main.gravityModifier = 1.5f;

            var emission = bloodDropParticles.emission;
            emission.rateOverTime = 0f; // Controlled via EmitBloodDrop

            var shape = bloodDropParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            ParticleSystemRenderer rend = bGo.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            rend.sharedMaterial = new Material(s);

            bloodDropParticles.Stop();
        }

        private void EmitBloodDrop()
        {
            if (bloodDropParticles != null)
            {
                bloodDropParticles.Emit(UnityEngine.Random.Range(2, 5));
            }
        }

        private void EnsureVignetteOverlay()
        {
            if (vignetteOverlayImage != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("Trauma_VignetteOverlay");
            if (existing != null)
            {
                vignetteOverlayImage = existing.GetComponent<Image>();
                return;
            }

            GameObject vGo = new GameObject("Trauma_VignetteOverlay", typeof(RectTransform), typeof(Image));
            vGo.transform.SetParent(canvas.transform, false);
            vGo.transform.SetAsFirstSibling(); // Render beneath HUD panels

            RectTransform rt = vGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            vignetteOverlayImage = vGo.GetComponent<Image>();
            vignetteOverlayImage.color = Color.clear;
            vignetteOverlayImage.raycastTarget = false;
        }

        public void ClearAllInjuries()
        {
            isBleeding = false;
            hasLegFracture = false;
            hasAcuteFever = false;
            isHypothermic = false;
            feverSuppressedTimer = 0f;
            wetnessLevel = 0f;
            OnTraumaStateChanged?.Invoke();
        }

        private void UpdateVignetteVisuals()
        {
            if (vignetteOverlayImage == null) return;

            if (IsAdrenalineActive)
            {
                // Golden-cyan combat adrenaline pulse
                float pulse = 0.20f + 0.16f * Mathf.Sin(Time.time * 8f);
                Color adrColor = Color.Lerp(new Color(1f, 0.82f, 0.20f), new Color(0.15f, 0.90f, 0.95f), Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f);
                adrColor.a = pulse;
                vignetteOverlayImage.color = adrColor;
            }
            else if (isBleeding)
            {
                // Rhythmic 0.8 Hz pulsing crimson edge vignette
                float pulse = 0.18f + 0.16f * Mathf.Sin(Time.time * (0.8f * Mathf.PI * 2f));
                if (bleedSeverity == 2) pulse *= 1.5f;
                vignetteOverlayImage.color = new Color(0.85f, 0.05f, 0.10f, Mathf.Clamp01(pulse));
            }
            else if (HasAcuteFever)
            {
                // Subtle heat-haze / peripheral warm violet pulse
                float pulse = 0.10f + 0.08f * Mathf.Sin(Time.time * 2.2f);
                vignetteOverlayImage.color = new Color(0.60f, 0.20f, 0.80f, Mathf.Clamp01(pulse));
            }
            else
            {
                vignetteOverlayImage.color = Color.Lerp(vignetteOverlayImage.color, Color.clear, Time.deltaTime * 3f);
            }
        }
    }
}
