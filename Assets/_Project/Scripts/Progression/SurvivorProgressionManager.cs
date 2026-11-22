using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.AI;

namespace ProjectSurvival.Progression
{
    public enum SkillBranch
    {
        Stealth = 0,
        Combat = 1,
        Survival = 2
    }

    [Serializable]
    public class SkillNodeDef
    {
        public string id;
        public SkillBranch branch;
        public int tier; // 1 to 4
        public string titleTR;
        public string descriptionTR;
        public string prerequisiteId;
    }

    /// <summary>
    /// Phase 3: 12-Perk, 3-Branch Tactical Progression & XP Architecture.
    /// Manages player leveling, skill point distribution, passive perk scaling,
    /// apex boss tracking, and permanent blueprint unlocks.
    /// </summary>
    public class SurvivorProgressionManager : MonoBehaviour
    {
        private static SurvivorProgressionManager _instance;
        public static SurvivorProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SurvivorProgressionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SurvivorProgressionManager");
                        _instance = go.AddComponent<SurvivorProgressionManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Progression State")]
        [SerializeField] private int playerLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int unspentSkillPoints = 0;

        // Unlocked Perk IDs
        private readonly HashSet<string> unlockedSkillIds = new HashSet<string>();
        // Unlocked Blueprint IDs
        private readonly HashSet<string> unlockedBlueprintIds = new HashSet<string>();
        // Defeated Boss IDs
        private readonly HashSet<string> defeatedBossIds = new HashSet<string>();
        // Opened Apex Cache IDs
        private readonly HashSet<string> openedApexCacheIds = new HashSet<string>();

        // Master Skill Definitions (12 Perks, 3 Branches, 4 Sequential Tiers)
        public static readonly List<SkillNodeDef> SkillTreeDefinitions = new List<SkillNodeDef>
        {
            // --- BRANCH A: GİZLİLİK (Stealth & Recon) ---
            new SkillNodeDef
            {
                id = "stealth_1_soft_step",
                branch = SkillBranch.Stealth,
                tier = 1,
                titleTR = "Hafif Adım",
                descriptionTR = "Çömelme ve yürüme ayak sesi yayılma mesafesini %30 daraltır.",
                prerequisiteId = null
            },
            new SkillNodeDef
            {
                id = "stealth_2_swift_scavenger",
                branch = SkillBranch.Stealth,
                tier = 2,
                titleTR = "Hızlı Arama & Kilit",
                descriptionTR = "Araç, dolap ve kapı arama/güçlendirme bekleme süresini %35 kısaltır.",
                prerequisiteId = "stealth_1_soft_step"
            },
            new SkillNodeDef
            {
                id = "stealth_3_shadow_stalker",
                branch = SkillBranch.Stealth,
                tier = 3,
                titleTR = "Gölge Ustası",
                descriptionTR = "Çömelirken zombi görüş mesafesini %25 azaltır ve gizlenme toleransını uzatır.",
                prerequisiteId = "stealth_2_swift_scavenger"
            },
            new SkillNodeDef
            {
                id = "stealth_4_silent_takedown",
                branch = SkillBranch.Stealth,
                tier = 4,
                titleTR = "Sessiz Suikastçı",
                descriptionTR = "Fark edilmemiş zombilere yakın dövüş saldırısı +%150 kritik hasar verir (tek vuruşta imha).",
                prerequisiteId = "stealth_3_shadow_stalker"
            },

            // --- BRANCH B: SAVAŞ (Combat & Ballistics) ---
            new SkillNodeDef
            {
                id = "combat_1_tactical_reload",
                branch = SkillBranch.Combat,
                tier = 1,
                titleTR = "Taktik Şarjör",
                descriptionTR = "Ateşli silah şarjör değiştirme hızını %25 artırır, tepme dağılımını %15 azaltır.",
                prerequisiteId = null
            },
            new SkillNodeDef
            {
                id = "combat_2_deadeye",
                branch = SkillBranch.Combat,
                tier = 2,
                titleTR = "Keskin Nişancı",
                descriptionTR = "Kritik kafa vuruşu çarpanını 2.5x'ten 3.25x'e çıkarır, nişangah açılmasını sınırlar.",
                prerequisiteId = "combat_1_tactical_reload"
            },
            new SkillNodeDef
            {
                id = "combat_3_heavy_impact",
                branch = SkillBranch.Combat,
                tier = 3,
                titleTR = "Ağır Darbe",
                descriptionTR = "Balta savurma yayını genişletir ve bacak vuruşlarına +%30 sakatlama travması ekler.",
                prerequisiteId = "combat_2_deadeye"
            },
            new SkillNodeDef
            {
                id = "combat_4_munitions_master",
                branch = SkillBranch.Combat,
                tier = 4,
                titleTR = "Cephane Uzmanı",
                descriptionTR = "Ateş ederken %25 ihtimalle mermi tüketmez ve patlayıcı tuzak yarıçapını +%25 artırır.",
                prerequisiteId = "combat_3_heavy_impact"
            },

            // --- BRANCH C: HAYATTA KALMA (Survival & Engineering) ---
            new SkillNodeDef
            {
                id = "survival_1_iron_constitution",
                branch = SkillBranch.Survival,
                tier = 1,
                titleTR = "Demir Bünye",
                descriptionTR = "Maksimum Canı 100'den 130 HP'ye, Maksimum Dayanıklılığı 125 puana yükseltir.",
                prerequisiteId = null
            },
            new SkillNodeDef
            {
                id = "survival_2_efficient_metabolism",
                branch = SkillBranch.Survival,
                tier = 2,
                titleTR = "Yavaş Metabolizma",
                descriptionTR = "Açlık ve Susuzluk tükenme hızını %30 yavaşlatır, yiyecek ve sudan alınan faydayı +%20 artırır.",
                prerequisiteId = "survival_1_iron_constitution"
            },
            new SkillNodeDef
            {
                id = "survival_3_pathogen_immunity",
                branch = SkillBranch.Survival,
                tier = 3,
                titleTR = "Biyolojik Direnç",
                descriptionTR = "Zombi darbelerinden gelen enfeksiyon artışını %50 keser, tıbbi malzeme iyileştirmesini ikiye katlar.",
                prerequisiteId = "survival_2_efficient_metabolism"
            },
            new SkillNodeDef
            {
                id = "survival_4_master_fabricator",
                branch = SkillBranch.Survival,
                tier = 4,
                titleTR = "Usta Mühendis",
                descriptionTR = "Tüm üretim ve jeneratör hurda maliyetlerini %25 düşürür, Taktik Zırh kapasitesini 140 HP'ye çıkarır.",
                prerequisiteId = "survival_3_pathogen_immunity"
            }
        };

        // Blueprint Constants
        public const string BLUEPRINT_AP_ROUNDS = "blueprint_ap_rounds";
        public const string BLUEPRINT_COMPENSATOR = "blueprint_compensator";
        public const string BLUEPRINT_COMPOSITE_ARMOR = "blueprint_composite_armor";

        // HUD Notification Elements
        private Canvas hudCanvas;
        private GameObject xpPopupContainer;
        private GameObject skillPointBadgeObj;
        private Text skillPointBadgeText;

        public int PlayerLevel => playerLevel;
        public int CurrentXP => currentXP;
        public int UnspentSkillPoints => unspentSkillPoints;
        public int XPForNextLevel => GetXPRequiredForLevel(playerLevel);

        [SerializeField] private bool hasGlobalCurePerk = false;
        public bool HasGlobalCurePerk
        {
            get => hasGlobalCurePerk;
            set => hasGlobalCurePerk = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SurvivorProgressionManager");
                Instance = go.AddComponent<SurvivorProgressionManager>();
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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureHUDWidgets();
            ApplyPerkEffects();
        }

        private void OnEnable()
        {
            GameEvents.OnZombieKilled += HandleZombieKilled;
            GameEvents.OnStealthTakedownPerformed += HandleStealthTakedown;
            GameEvents.OnCivilianRescued += HandleCivilianRescued;
            GameEvents.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            GameEvents.OnZombieKilled -= HandleZombieKilled;
            GameEvents.OnStealthTakedownPerformed -= HandleStealthTakedown;
            GameEvents.OnCivilianRescued -= HandleCivilianRescued;
            GameEvents.OnBossDefeated -= HandleBossDefeated;
        }

        // =========================================================================
        // XP & LEVELING CALCULATIONS
        // =========================================================================

        public static int GetXPRequiredForLevel(int level)
        {
            // Level 1 -> 2: 200 XP, then +120 XP per level
            return 200 + Mathf.Max(0, level - 1) * 120;
        }

        public void AddXP(int amount, string reason = "")
        {
            if (amount <= 0) return;

            currentXP += amount;
            ShowXPFloatingNotice(amount, reason);
            GameEvents.TriggerXPGained(amount, reason);

            int needed = GetXPRequiredForLevel(playerLevel);
            while (currentXP >= needed)
            {
                currentXP -= needed;
                playerLevel++;
                unspentSkillPoints++;

                ProceduralAudioManager.Instance?.PlayLevelUp();
                GameEvents.TriggerLevelUp(playerLevel, unspentSkillPoints);
                GameEvents.TriggerSkillPointsChanged(unspentSkillPoints);
                Debug.Log($"[SurvivorProgressionManager] LEVEL UP! New Level: {playerLevel}, Unspent Points: {unspentSkillPoints}");

                needed = GetXPRequiredForLevel(playerLevel);
            }

            UpdateSkillPointBadge();
        }

        private void HandleZombieKilled(GameObject zombieObj)
        {
            if (zombieObj == null) return;

            ZombieAI ai = zombieObj.GetComponent<ZombieAI>();
            if (ai != null)
            {
                int baseXP = 20;
                string typeName = "Zombi İmha";

                if (ai.Type == ZombieType.Runner)
                {
                    baseXP = 20;
                    typeName = "Koşucu Zombi";
                }
                else if (ai.Type == ZombieType.Screamer)
                {
                    baseXP = 60;
                    typeName = "Çığlıkçı Zombi";
                }
                else if (ai.Type == ZombieType.Tank)
                {
                    baseXP = 60;
                    typeName = "Tank Zombi";
                }

                AddXP(baseXP, typeName);
            }
            else
            {
                AddXP(20, "Zombi İmha");
            }
        }

        private void HandleStealthTakedown(GameObject target)
        {
            AddXP(25, "Gizli Suikast Bonusu");
        }

        private void HandleCivilianRescued()
        {
            AddXP(150, "Kazazede Kurtarıldı");
        }

        private void HandleBossDefeated(string bossId, string bossName)
        {
            AddXP(500, $"Bölge Lideri İmha: {bossName}");
            RegisterBossDefeated(bossId);
        }

        public void RegisterScavengeAction(string propName = "Ganimet")
        {
            AddXP(15, $"{propName} Yağmalandı");
        }

        public void RegisterOutpostLiberated(string outpostName)
        {
            AddXP(300, $"Karakol Kurtarıldı: {outpostName}");
        }

        public void AwardHeadshotBonus()
        {
            AddXP(10, "Kritik Kafa Vuruşu");
        }

        // =========================================================================
        // PERK UNLOCKING & STATE CHECKS
        // =========================================================================

        public bool HasPerk(string perkId)
        {
            return !string.IsNullOrEmpty(perkId) && unlockedSkillIds.Contains(perkId);
        }

        public bool CanUnlockPerk(string perkId)
        {
            if (unspentSkillPoints < 1) return false;
            if (HasPerk(perkId)) return false;

            SkillNodeDef def = SkillTreeDefinitions.Find(n => n.id == perkId);
            if (def == null) return false;

            // Sequential check: If prerequisite exists, it must be unlocked
            if (!string.IsNullOrEmpty(def.prerequisiteId) && !HasPerk(def.prerequisiteId))
            {
                return false;
            }

            return true;
        }

        public bool UnlockPerk(string perkId)
        {
            if (!CanUnlockPerk(perkId)) return false;

            unspentSkillPoints--;
            unlockedSkillIds.Add(perkId);

            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerPerkUnlocked(perkId);
            GameEvents.TriggerSkillPointsChanged(unspentSkillPoints);

            ApplyPerkEffects();
            UpdateSkillPointBadge();

            Debug.Log($"[SurvivorProgressionManager] Unlocked Perk: '{perkId}'. Remaining Points: {unspentSkillPoints}");
            return true;
        }

        private void ApplyPerkEffects()
        {
            // Iron Constitution (survival_1_iron_constitution)
            if (HasPerk("survival_1_iron_constitution"))
            {
                if (SurvivalStats.Instance != null)
                {
                    SurvivalStats.Instance.SetMaxHealthBonus(30f);   // 100 -> 130
                    SurvivalStats.Instance.SetMaxStaminaBonus(25f);  // 100 -> 125
                }
            }

            // Master Fabricator (survival_4_master_fabricator)
            if (HasPerk("survival_4_master_fabricator"))
            {
                if (PlayerArmorController.Instance != null)
                {
                    PlayerArmorController.Instance.SetMaxCapacity(140f);
                }
            }
        }

        // =========================================================================
        // BLUEPRINTS & BOSS CACHES
        // =========================================================================

        public bool HasBlueprint(string blueprintId)
        {
            return !string.IsNullOrEmpty(blueprintId) && unlockedBlueprintIds.Contains(blueprintId);
        }

        public void UnlockBlueprint(string blueprintId, string displayName = "")
        {
            if (string.IsNullOrEmpty(blueprintId) || unlockedBlueprintIds.Contains(blueprintId)) return;

            unlockedBlueprintIds.Add(blueprintId);
            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            ShowXPFloatingNotice(100, $"Kalıcı Şema Açıldı: {displayName}");
            Debug.Log($"[SurvivorProgressionManager] Unlocked Permanent Blueprint: '{blueprintId}' ({displayName})");

            // Apply immediate blueprint modifications
            if (blueprintId == BLUEPRINT_COMPOSITE_ARMOR && PlayerArmorController.Instance != null)
            {
                PlayerArmorController.Instance.UpgradeToCompositeArmor();
            }
        }

        public bool IsBossDefeated(string bossId)
        {
            return !string.IsNullOrEmpty(bossId) && defeatedBossIds.Contains(bossId);
        }

        public void RegisterBossDefeated(string bossId)
        {
            if (!string.IsNullOrEmpty(bossId))
            {
                defeatedBossIds.Add(bossId);
            }
        }

        public bool IsApexCacheOpened(string cacheId)
        {
            return !string.IsNullOrEmpty(cacheId) && openedApexCacheIds.Contains(cacheId);
        }

        public void RegisterApexCacheOpened(string cacheId)
        {
            if (!string.IsNullOrEmpty(cacheId))
            {
                openedApexCacheIds.Add(cacheId);
                GameEvents.TriggerApexCacheUnlocked(cacheId);
            }
        }

        // =========================================================================
        // PASSIVE PERK SCALING QUERIES
        // =========================================================================

        public float GetFootstepNoiseMultiplier()
        {
            return HasPerk("stealth_1_soft_step") ? 0.70f : 1.0f;
        }

        public float GetScavengeHoldDurationMultiplier()
        {
            return HasPerk("stealth_2_swift_scavenger") ? 0.65f : 1.0f;
        }

        public float GetZombieVisionRangeMultiplier()
        {
            return HasPerk("stealth_3_shadow_stalker") ? 0.75f : 1.0f;
        }

        public float GetSneakMeleeDamageMultiplier()
        {
            return HasPerk("stealth_4_silent_takedown") ? 2.50f : 1.0f;
        }

        public float GetReloadSpeedMultiplier()
        {
            return HasPerk("combat_1_tactical_reload") ? 1.25f : 1.0f;
        }

        public float GetRecoilMultiplier()
        {
            float mult = 1.0f;
            if (HasPerk("combat_1_tactical_reload")) mult *= 0.85f;
            if (HasBlueprint(BLUEPRINT_COMPENSATOR)) mult *= 0.65f;
            return mult;
        }

        public float GetHeadshotMultiplier()
        {
            return HasPerk("combat_2_deadeye") ? 3.25f : 2.50f;
        }

        public bool HasHeavyImpact()
        {
            return HasPerk("combat_3_heavy_impact");
        }

        public bool ShouldPreserveAmmo()
        {
            return HasPerk("combat_4_munitions_master") && (UnityEngine.Random.value < 0.25f);
        }

        public float GetExplosiveTrapRadiusMultiplier()
        {
            return HasPerk("combat_4_munitions_master") ? 1.25f : 1.0f;
        }

        public float GetHungerThirstDrainMultiplier()
        {
            return HasPerk("survival_2_efficient_metabolism") ? 0.70f : 1.0f;
        }

        public float GetFoodWaterRestoreMultiplier()
        {
            return HasPerk("survival_2_efficient_metabolism") ? 1.20f : 1.0f;
        }

        public float GetInfectionBuildupMultiplier()
        {
            if (hasGlobalCurePerk) return 0.50f;
            return HasPerk("survival_3_pathogen_immunity") ? 0.50f : 1.0f;
        }

        public float GetHealingEfficiencyMultiplier()
        {
            return HasPerk("survival_3_pathogen_immunity") ? 2.00f : 1.0f;
        }

        public float GetCraftingCostDiscountMultiplier()
        {
            return HasPerk("survival_4_master_fabricator") ? 0.75f : 1.0f;
        }

        public float GetFirearmDamagePenetrationMultiplier()
        {
            return HasBlueprint(BLUEPRINT_AP_ROUNDS) ? 1.25f : 1.0f;
        }

        // =========================================================================
        // HUD WIDGETS & NOTIFICATIONS
        // =========================================================================

        private void EnsureHUDWidgets()
        {
            if (hudCanvas == null)
            {
                hudCanvas = FindFirstObjectByType<Canvas>();
            }
            if (hudCanvas == null) return;

            // 1. Floating XP notification container
            if (xpPopupContainer == null)
            {
                Transform existing = hudCanvas.transform.Find("Progression_XPPopupContainer");
                if (existing != null)
                {
                    xpPopupContainer = existing.gameObject;
                }
                else
                {
                    xpPopupContainer = new GameObject("Progression_XPPopupContainer");
                    xpPopupContainer.transform.SetParent(hudCanvas.transform, false);

                    RectTransform rt = xpPopupContainer.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.72f);
                    rt.anchorMax = new Vector2(0.5f, 0.72f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(320f, 60f);
                }
            }

            // 2. Pulsing Skill Point Badge near top-left Vital/Mission HUD
            if (skillPointBadgeObj == null)
            {
                Transform existing = hudCanvas.transform.Find("Progression_SkillPointBadge");
                if (existing != null)
                {
                    skillPointBadgeObj = existing.gameObject;
                    skillPointBadgeText = skillPointBadgeObj.GetComponentInChildren<Text>();
                }
                else
                {
                    skillPointBadgeObj = new GameObject("Progression_SkillPointBadge");
                    skillPointBadgeObj.transform.SetParent(hudCanvas.transform, false);

                    RectTransform rt = skillPointBadgeObj.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(28f, -145f);
                    rt.sizeDelta = new Vector2(175f, 36f);

                    Image bg = skillPointBadgeObj.AddComponent<Image>();
                    bg.color = new Color(0.10f, 0.12f, 0.16f, 0.90f);

                    Button btn = skillPointBadgeObj.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        UI.SkillTreeUI.Instance?.ToggleSkillTree();
                    });

                    // Border indicator (gold accent)
                    GameObject border = new GameObject("GoldBorder");
                    border.transform.SetParent(skillPointBadgeObj.transform, false);
                    RectTransform brt = border.AddComponent<RectTransform>();
                    brt.anchorMin = Vector2.zero;
                    brt.anchorMax = Vector2.one;
                    brt.sizeDelta = Vector2.zero;
                    Image bImg = border.AddComponent<Image>();
                    bImg.color = new Color(0.96f, 0.76f, 0.26f, 0.85f);
                    bImg.raycastTarget = false;

                    GameObject txtObj = new GameObject("BadgeText");
                    txtObj.transform.SetParent(skillPointBadgeObj.transform, false);
                    RectTransform trt = txtObj.AddComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;

                    skillPointBadgeText = txtObj.AddComponent<Text>();
                    skillPointBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    skillPointBadgeText.fontSize = 12;
                    skillPointBadgeText.fontStyle = FontStyle.Bold;
                    skillPointBadgeText.alignment = TextAnchor.MiddleCenter;
                    skillPointBadgeText.color = new Color(1.0f, 0.88f, 0.45f);
                    skillPointBadgeText.text = "YETENEK PUANI (+0)";
                }
            }

            UpdateSkillPointBadge();
        }

        public void UpdateSkillPointBadge()
        {
            if (skillPointBadgeObj == null) EnsureHUDWidgets();
            if (skillPointBadgeObj == null) return;

            if (unspentSkillPoints > 0)
            {
                skillPointBadgeObj.SetActive(true);
                if (skillPointBadgeText != null)
                {
                    skillPointBadgeText.text = $"YETENEK PUANI (+{unspentSkillPoints})";
                }
            }
            else
            {
                skillPointBadgeObj.SetActive(false);
            }
        }

        private void ShowXPFloatingNotice(int amount, string reason)
        {
            if (xpPopupContainer == null) EnsureHUDWidgets();
            if (xpPopupContainer == null) return;

            StartCoroutine(AnimateXPNotice(amount, reason));
        }

        private IEnumerator AnimateXPNotice(int amount, string reason)
        {
            GameObject notice = new GameObject("XP_Notice");
            notice.transform.SetParent(xpPopupContainer.transform, false);

            RectTransform rt = notice.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 32f);
            rt.anchoredPosition = new Vector2(0f, -10f);

            Text txt = notice.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.35f, 0.95f, 0.55f, 1f);
            txt.text = string.IsNullOrEmpty(reason) ? $"+{amount} XP" : $"+{amount} XP  <color=#CCCCCC>({reason})</color>";

            float elapsed = 0f;
            float duration = 1.8f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + new Vector2(0f, t * 45f);
                float alpha = Mathf.Clamp01((1f - t) * 1.5f);
                txt.color = new Color(0.35f, 0.95f, 0.55f, alpha);
                yield return null;
            }

            Destroy(notice);
        }

        // =========================================================================
        // SAVE & RESTORE
        // =========================================================================

        public List<string> GetUnlockedSkillIds() => new List<string>(unlockedSkillIds);
        public List<string> GetUnlockedBlueprintIds() => new List<string>(unlockedBlueprintIds);
        public List<string> GetDefeatedBossIds() => new List<string>(defeatedBossIds);
        public List<string> GetOpenedApexCacheIds() => new List<string>(openedApexCacheIds);

        public void RestoreState(int level, int xp, int skillPoints, List<string> skills, List<string> blueprints, List<string> bosses, List<string> caches)
        {
            playerLevel = Mathf.Max(1, level);
            currentXP = Mathf.Max(0, xp);
            unspentSkillPoints = Mathf.Max(0, skillPoints);

            unlockedSkillIds.Clear();
            if (skills != null)
            {
                for (int i = 0; i < skills.Count; i++) unlockedSkillIds.Add(skills[i]);
            }

            unlockedBlueprintIds.Clear();
            if (blueprints != null)
            {
                for (int i = 0; i < blueprints.Count; i++) unlockedBlueprintIds.Add(blueprints[i]);
            }

            defeatedBossIds.Clear();
            if (bosses != null)
            {
                for (int i = 0; i < bosses.Count; i++) defeatedBossIds.Add(bosses[i]);
            }

            openedApexCacheIds.Clear();
            if (caches != null)
            {
                for (int i = 0; i < caches.Count; i++) openedApexCacheIds.Add(caches[i]);
            }

            ApplyPerkEffects();
            UpdateSkillPointBadge();
            Debug.Log($"[SurvivorProgressionManager] Restored Level {playerLevel}, {unlockedSkillIds.Count} Perks, {unlockedBlueprintIds.Count} Blueprints.");
        }
    }
}
