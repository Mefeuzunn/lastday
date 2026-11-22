using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 3: High-Security Apex Boss Vault Cache.
    /// Guarded by regional mutant bosses. Locked until the guardian boss is eliminated.
    /// Unlocks permanent weapon/crafting blueprints and high-tier military supplies upon interaction.
    /// </summary>
    public class ApexBossCacheInteractable : WorldInteractableObject
    {
        [Header("Cache Identity")]
        [SerializeField] private string cacheId = "apex_cache_north_highway";
        [SerializeField] private string requiredBossId = "boss_juggernaut";
        [SerializeField] private string rewardedBlueprintId = SurvivorProgressionManager.BLUEPRINT_AP_ROUNDS;
        [SerializeField] private string rewardedBlueprintName = "Delici Mermi Şeması (AP Rounds)";

        [Header("Visuals & Lighting")]
        [SerializeField] private Light statusLight;
        [SerializeField] private Renderer statusBezelRenderer;

        private bool isOpened = false;
        private Material statusMaterial;

        public string CacheId => cacheId;
        public bool IsOpened => isOpened;

        protected override void Awake()
        {
            base.Awake();
            persistentId = cacheId;
            objectTitle = "APEX ASKERİ KASA";
            objectSubtitle = "KİLİTLİ GÜVENLİK KASASI";

            BuildVisualVaultIfEmpty();
        }

        private void Start()
        {
            // Check if already opened in previous session
            if (SurvivorProgressionManager.Instance != null && SurvivorProgressionManager.Instance.IsApexCacheOpened(cacheId))
            {
                isOpened = true;
                UpdateVaultVisuals();
            }
        }

        private void Update()
        {
            UpdateVaultVisuals();
        }

        public void Configure(string id, string bossId, string bpId, string bpName)
        {
            cacheId = id;
            persistentId = id;
            requiredBossId = bossId;
            rewardedBlueprintId = bpId;
            rewardedBlueprintName = bpName;

            BuildVisualVaultIfEmpty();
            UpdateVaultVisuals();
        }

        private bool IsGuardianBossDefeated()
        {
            return SurvivorProgressionManager.Instance != null && SurvivorProgressionManager.Instance.IsBossDefeated(requiredBossId);
        }

        private void UpdateVaultVisuals()
        {
            bool bossDead = IsGuardianBossDefeated();

            if (isOpened)
            {
                objectTitle = "APEX KASASI (AÇILDI)";
                objectSubtitle = "GANİMETLER ALINDI";

                if (statusLight != null)
                {
                    statusLight.color = new Color(0.2f, 0.8f, 0.4f);
                    statusLight.intensity = 0.5f;
                }
                if (statusMaterial != null)
                {
                    statusMaterial.SetColor("_BaseColor", new Color(0.2f, 0.8f, 0.4f));
                    if (statusMaterial.HasProperty("_EmissionColor"))
                    {
                        statusMaterial.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 0.4f) * 0.8f);
                    }
                }
            }
            else if (bossDead)
            {
                // Unlocked cyan-gold state
                objectTitle = "APEX ASKERİ KASA (ERİŞİLEBİLİR)";
                objectSubtitle = $"{rewardedBlueprintName} BULUNDU";

                if (statusLight != null)
                {
                    statusLight.color = new Color(0.32f, 0.72f, 0.53f); // #52B788
                    statusLight.intensity = 2.5f;
                }
                if (statusMaterial != null)
                {
                    statusMaterial.SetColor("_BaseColor", new Color(0.32f, 0.72f, 0.53f));
                    if (statusMaterial.HasProperty("_EmissionColor"))
                    {
                        statusMaterial.SetColor("_EmissionColor", new Color(0.32f, 0.72f, 0.53f) * 2.2f);
                    }
                }
            }
            else
            {
                // Locked crimson state
                objectTitle = "APEX ASKERİ KASA";
                objectSubtitle = "KİLİTLİ: BÖLGE LİDERİNİ YOK ET";

                if (statusLight != null)
                {
                    statusLight.color = new Color(0.90f, 0.15f, 0.18f); // #E63946
                    statusLight.intensity = 1.8f;
                }
                if (statusMaterial != null)
                {
                    statusMaterial.SetColor("_BaseColor", new Color(0.90f, 0.15f, 0.18f));
                    if (statusMaterial.HasProperty("_EmissionColor"))
                    {
                        statusMaterial.SetColor("_EmissionColor", new Color(0.90f, 0.15f, 0.18f) * 1.5f);
                    }
                }
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isOpened)
            {
                return verbs; // No further actions
            }

            if (!IsGuardianBossDefeated())
            {
                verbs.Add(new InteractionVerb(
                    "locked_notice",
                    "KİLİTLİ: BÖLGE LİDERİNİ YOK ET",
                    0f,
                    null,
                    null,
                    0f
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "open_apex_cache",
                    $"APEX KASASINI AÇ ({rewardedBlueprintName})",
                    1.5f, // 1.5s Hold duration
                    null,
                    null,
                    0f
                ));
            }

            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (isOpened) return false;
            if (verbId == "locked_notice") return false;
            if (verbId == "open_apex_cache") return IsGuardianBossDefeated();
            return false;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "open_apex_cache" && !isOpened && IsGuardianBossDefeated())
            {
                OpenVaultCache();
            }
        }

        private void OpenVaultCache()
        {
            isOpened = true;

            // 1. Audio and Visual Feedback
            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            GameEvents.TriggerCameraShakeRequested(0.35f, 0.25f);

            // 2. Unlock Blueprint in Progression Manager
            if (SurvivorProgressionManager.Instance != null)
            {
                SurvivorProgressionManager.Instance.UnlockBlueprint(rewardedBlueprintId, rewardedBlueprintName);
                SurvivorProgressionManager.Instance.RegisterApexCacheOpened(cacheId);
            }

            // 3. Dispense Military Supplies directly into Inventory
            DispenseLootSupplies();

            UpdateVaultVisuals();
            Debug.Log($"[ApexBossCacheInteractable] Opened '{cacheId}'. Granted Blueprint '{rewardedBlueprintId}'.");
        }

        private void DispenseLootSupplies()
        {
            InventorySystem inv = InventorySystem.Instance;
            if (inv == null) return;

            switch (cacheId)
            {
                case "apex_cache_north_highway":
                    // AP Rounds + 36x 9mm, 18x Shotgun, 2x Medkits
                    inv.AddItemById("ammo_9mm", 36);
                    inv.AddItemById("ammo_shells", 18);
                    inv.AddItemById("item_medkit", 2);
                    GameEvents.TriggerObjectiveToastRequested("Alındı: 36x 9mm, 18x Fişek, 2x İlkyardım", 3.0f);
                    break;

                case "apex_cache_south_mall":
                    // Compensator + 2x Sound Bombs, 3x Antiviral, 5x Scrap
                    inv.AddItemById("trap_sound_bomb", 2);
                    inv.AddItemById("item_antiviral", 3);
                    inv.AddItemById("item_scrap", 5);
                    GameEvents.TriggerObjectiveToastRequested("Alındı: 2x Ses Bombası, 3x Antiviral, 5x Hurda", 3.0f);
                    break;

                case "apex_cache_ne_harbor":
                    // Composite Armor + 4x Medkits, 6x Scrap, 4x Alcohol
                    inv.AddItemById("item_medkit", 4);
                    inv.AddItemById("item_scrap", 6);
                    inv.AddItemById("item_alcohol", 4);
                    GameEvents.TriggerObjectiveToastRequested("Alındı: 4x İlkyardım, 6x Hurda, 4x Alkol", 3.0f);
                    break;
            }
        }

        private void BuildVisualVaultIfEmpty()
        {
            Transform existing = transform.Find("Vault_Visual_Model");
            if (existing != null) return;

            GameObject vaultModel = new GameObject("Vault_Visual_Model");
            vaultModel.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Vault Body (0.85m x 0.70m x 0.65m heavy military container)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Vault_Body";
            body.transform.SetParent(vaultModel.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(0.85f, 0.70f, 0.65f);
            Destroy(body.GetComponent<Collider>());

            Material darkMetal = new Material(litShader);
            SetMat(darkMetal, new Color(0.12f, 0.14f, 0.16f), 0.75f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = darkMetal;

            // Status Light Bezel
            GameObject bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bezel.name = "Status_Bezel";
            bezel.transform.SetParent(body.transform, false);
            bezel.transform.localPosition = new Vector3(0f, 0.25f, 0.52f);
            bezel.transform.localScale = new Vector3(0.35f, 0.18f, 0.12f);
            Destroy(bezel.GetComponent<Collider>());

            statusMaterial = new Material(litShader);
            SetMat(statusMaterial, new Color(0.90f, 0.15f, 0.18f), 0.85f, 0.2f);
            if (statusMaterial.HasProperty("_EmissionColor"))
            {
                statusMaterial.EnableKeyword("_EMISSION");
                statusMaterial.SetColor("_EmissionColor", new Color(0.90f, 0.15f, 0.18f) * 1.5f);
            }
            bezel.GetComponent<Renderer>().sharedMaterial = statusMaterial;

            // Point Light
            GameObject lightObj = new GameObject("Vault_StatusLight");
            lightObj.transform.SetParent(bezel.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            statusLight = lightObj.AddComponent<Light>();
            statusLight.type = LightType.Point;
            statusLight.color = new Color(0.90f, 0.15f, 0.18f);
            statusLight.range = 3.5f;
            statusLight.intensity = 1.8f;

            // Collider for interaction
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.1f, 0.9f, 0.9f);
            col.center = new Vector3(0f, 0.45f, 0f);
        }

        private void SetMat(Material mat, Color col, float smoothness, float metallic)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else mat.color = col;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        }

        public override string GetSerializedState()
        {
            return isOpened ? "opened" : "closed";
        }

        public override void LoadSerializedState(string state)
        {
            isOpened = (state == "opened");
            UpdateVaultVisuals();
        }

        public void RestoreSerializedState(string state)
        {
            LoadSerializedState(state);
        }
    }
}
