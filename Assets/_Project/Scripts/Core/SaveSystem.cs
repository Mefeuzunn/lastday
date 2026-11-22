using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectSurvival.Core
{
    [Serializable]
    public class SaveData
    {
        public float health;
        public float hunger;
        public float thirst;
        public float infection;
        public float playerPosX;
        public float playerPosY;
        public float playerPosZ;
        public List<string> worldFlags = new List<string>();
        public List<string> npcTrustKeys = new List<string>();
        public List<int> npcTrustValues = new List<int>();
        public List<string> inventoryItemIds = new List<string>();
        public List<int> inventoryItemCounts = new List<int>();
        public string fogOfWarData = string.Empty;
        public string fogOfWarDataV2 = string.Empty;
        public float currentHour = 10f;
        public int dayCount = 1;
        public List<string> interactableKeys = new List<string>();
        public List<string> interactableValues = new List<string>();
        public float communityMorale = 75f;
        public List<string> safeHouseResidents = new List<string>();
        public int barterStockSeed = 1234;
        public List<string> unlockedIntelDistricts = new List<string>();

        // Extended Persistence Fields (Audit Remediation)
        public int equippedWeaponType = 0;
        public bool isWeaponHolstered = true;
        public string[] quickSlotItemIds = new string[4]
        {
            "item_canned_food",
            "item_water_bottle",
            "item_medkit",
            "item_distraction_bottle"
        };
        public string activeCompanionId = string.Empty;
        public bool hasCustomWaypoint = false;
        public float customWaypointX = 0f;
        public float customWaypointZ = 0f;
        public bool isRegionalGridActive = false;
        public List<string> liberatedOutpostIds = new List<string>();

        // Phase 2 Attachment & Armor Persistence
        public List<string> unlockedAttachmentIds = new List<string>();
        public List<string> equippedAttachmentIds = new List<string>();
        public float currentArmorHP = 0f;
        public bool hasArmorEquipped = false;

        // Phase 3 Progression, Bosses & Blueprint Persistence
        public int playerLevel = 1;
        public int currentXP = 0;
        public int unspentSkillPoints = 0;
        public List<string> unlockedSkillIds = new List<string>();
        public List<string> defeatedBossIds = new List<string>();
        public List<string> openedApexCacheIds = new List<string>();
        public List<string> unlockedBlueprintIds = new List<string>();

        // Phase 4 Dynamic Weather Persistence
        public int currentWeatherState = 0;

        // Deep Expansion Phase 2: Trauma, Injury & Consumable Buff Persistence
        public bool isBleeding = false;
        public float bleedingSeverity = 1f;
        public bool hasLegFracture = false;
        public bool hasAcuteFever = false;
        public bool isHypothermic = false;
        public float wetnessLevel = 0f;
        public float painkillerBuffTimer = 0f;
        public float adrenalineBuffTimer = 0f;
        public float herbalTeaBuffTimer = 0f;
        public float thermalResistanceBuffTimer = 0f;

        // Deep Expansion Phase 1: Drivable Vehicles Persistence
        public List<VehicleSaveRecord> vehicleRecords = new List<VehicleSaveRecord>();

        // Active Phase 3: Modular Base Building & Fortifications Persistence
        public List<SavedBaseStructure> builtStructures = new List<SavedBaseStructure>();

        // Active Phase 5: Tactical Radio Scanner, 4-Chapter Branching Story & Audio Logs Persistence
        public int currentStoryChapter = 1;
        public string currentObjectiveId = "obj_1a_tune_radio";
        public bool isMainStoryCompleted = false;
        public string endgameMoralChoice = string.Empty;
        public List<float> decodedFrequencies = new List<float>();
        public List<string> collectedAudioLogIds = new List<string>();
        public bool hasGlobalCurePerk = false;

        // Step C: Starter Prologue Persistence
        public bool isPrologueCompleted = false;
        public int prologueStageIndex = 0;

        // Step B: Starter Intro & Phone Persistence
        public bool hasCheckedIntroPhone = false;

        // Step 3C: Gas Stations & Wrecked Car Fuel Reserves Persistence
        public List<SavedFuelSourceState> fuelSources = new List<SavedFuelSourceState>();

        // Phase 4 Step 4A: Underground Research Bunker Persistence
        public bool isBunkerGeneratorPowered = false;
        public bool isBunkerSecurityDoorUnlocked = false;
        public bool isBunkerVaultLooted = false;
    }

    [Serializable]
    public class SavedFuelSourceState
    {
        public string sourceId = string.Empty;
        public float remainingLiters = 0f;
    }

    [Serializable]
    public class SavedBaseStructure
    {
        public string structureId = string.Empty;
        public int structureType = 0;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float currentHP;
        public int accumulatedResources;
        public float resourceTimer;
    }

    [Serializable]
    public class VehicleSaveRecord
    {
        public string vehicleId = string.Empty;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float engineHP = 100f;
        public float fuel = 100f;
        public bool headlightsOn = false;
        public List<string> trunkItemIds = new List<string>();
        public List<int> trunkItemCounts = new List<int>();
    }

    public static class SaveSystem
    {
        private static readonly string SaveFileName = "survival_save.json";

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveSystem] Game saved successfully to: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {ex.Message}");
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!HasSave())
                {
                    Debug.LogWarning("[SaveSystem] No save file found.");
                    return null;
                }

                string json = File.ReadAllText(SaveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveSystem] Game loaded successfully.");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load game: {ex.Message}");
                return null;
            }
        }

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
        }
    }
}
