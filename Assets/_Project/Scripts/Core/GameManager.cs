using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State Settings")]
        [SerializeField] private GameState currentGameState = GameState.Gameplay;

        public GameState CurrentState => currentGameState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            Time.timeScale = 1f;

            var mmc = FindFirstObjectByType<ProjectSurvival.UI.MainMenuController>();
            if (mmc != null && mmc.AutoShowMenuOnStart)
            {
                currentGameState = GameState.MainMenu;
            }
            else
            {
                currentGameState = GameState.Gameplay;
            }

            // Phase 2 Micro-Step 2B: Ensure Cooking Stoves, Chemistry Labs & Alchemy UI
            Interaction.SurvivalStationPlacer.EnsureAllStationsPlaced();
            Interaction.OuterSectorLootPlacer.EnsureAllOuterSectorLootPlaced();
            if (FindFirstObjectByType<UI.SurvivalAlchemyUI>() == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null && canvas.GetComponent<UI.SurvivalAlchemyUI>() == null)
                {
                    canvas.gameObject.AddComponent<UI.SurvivalAlchemyUI>();
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDied += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
        }

        public void SetGameState(GameState newState)
        {
            currentGameState = newState;

            switch (newState)
            {
                case GameState.MainMenu:
                case GameState.Gameplay:
                case GameState.MissionActive:
                    Time.timeScale = 1f;
                    break;
                case GameState.Dialogue:
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0.2f;
                    break;
            }

            GameEvents.TriggerGameStateChanged(newState);
        }

        private void HandlePlayerDeath()
        {
            SetGameState(GameState.GameOver);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            GameEvents.ClearAll();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }

        public void SaveGame()
        {
            SaveData data = new SaveData();

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var stats = player.GetComponent<Survival.SurvivalStats>();
                if (stats != null)
                {
                    data.health = stats.CurrentHealth;
                    data.hunger = stats.CurrentHunger;
                    data.thirst = stats.CurrentThirst;
                    data.infection = stats.CurrentInfection;
                }
                data.playerPosX = player.transform.position.x;
                data.playerPosY = player.transform.position.y;
                data.playerPosZ = player.transform.position.z;

                var inv = player.GetComponent<Survival.InventorySystem>();
                if (inv != null)
                {
                    foreach (var slot in inv.Slots)
                    {
                        if (slot.item != null && slot.quantity > 0)
                        {
                            data.inventoryItemIds.Add(slot.item.itemId);
                            data.inventoryItemCounts.Add(slot.quantity);
                        }
                    }
                }
            }

            if (Narrative.ChoiceProcessor.Instance != null)
            {
                data.worldFlags = Narrative.ChoiceProcessor.Instance.GetActiveWorldFlags();
            }

            if (Narrative.StoryManager.Instance != null)
            {
                var trustDict = Narrative.StoryManager.Instance.GetAllTrust();
                foreach (var kvp in trustDict)
                {
                    data.npcTrustKeys.Add(kvp.Key);
                    data.npcTrustValues.Add(kvp.Value);
                }
            }

            if (ProjectSurvival.Environment.FogOfWarManager.Instance != null)
            {
                data.fogOfWarDataV2 = ProjectSurvival.Environment.FogOfWarManager.Instance.GetSerializedFogData();
                data.fogOfWarData = data.fogOfWarDataV2; // fallback
            }

            if (DayNightManager.Instance != null)
            {
                data.currentHour = DayNightManager.Instance.CurrentHour;
                data.dayCount = DayNightManager.Instance.DayCount;
            }

            var interactables = Object.FindObjectsByType<ProjectSurvival.Interaction.WorldInteractableObject>(FindObjectsSortMode.None);
            if (interactables != null)
            {
                for (int i = 0; i < interactables.Length; i++)
                {
                    var obj = interactables[i];
                    if (obj == null) continue;
                    if (obj.HasBeenSearched || obj.IsOpen || obj.IsBarricaded || obj.IsPowered || obj.IsDamaged)
                    {
                        data.interactableKeys.Add(obj.PersistentId);
                        data.interactableValues.Add(obj.GetSerializedState());
                    }
                }
            }

            if (AI.CommunityOutpostManager.Instance != null)
            {
                AI.CommunityOutpostManager.Instance.PopulateSaveData(data);
            }

            // Extended Audit Remediation: 5 Persistence Pillars
            if (Player.WeaponSystem.Instance != null)
            {
                data.equippedWeaponType = (int)Player.WeaponSystem.Instance.CurrentWeapon;
                data.isWeaponHolstered = Player.WeaponSystem.Instance.CurrentWeapon == Player.WeaponType.None;
            }

            if (UI.QuickSlotUI.Instance != null)
            {
                data.quickSlotItemIds = UI.QuickSlotUI.Instance.GetSlotItemIds();
            }

            if (AI.CompanionManager.Instance != null)
            {
                data.activeCompanionId = AI.CompanionManager.Instance.GetActiveCompanionId();
            }

            if (Environment.CustomWaypointManager.Instance != null && Environment.CustomWaypointManager.Instance.HasCustomWaypoint)
            {
                data.hasCustomWaypoint = true;
                data.customWaypointX = Environment.CustomWaypointManager.Instance.WaypointPosition.x;
                data.customWaypointZ = Environment.CustomWaypointManager.Instance.WaypointPosition.z;
            }
            else
            {
                data.hasCustomWaypoint = false;
            }

            var gridPanel = Object.FindAnyObjectByType<Interaction.RegionalMasterGridPanel>(FindObjectsInactive.Include);
            if (gridPanel != null)
            {
                data.isRegionalGridActive = gridPanel.IsPowered;
            }

            if (OpenWorld.OutpostLiberationManager.Instance != null)
            {
                data.liberatedOutpostIds = OpenWorld.OutpostLiberationManager.Instance.GetLiberatedOutpostIds();
            }

            // Phase 2: Weapon Attachments & Armor Persistence
            if (Player.WeaponAttachmentManager.Instance != null)
            {
                data.unlockedAttachmentIds = Player.WeaponAttachmentManager.Instance.GetUnlockedAttachmentIds();
                data.equippedAttachmentIds = Player.WeaponAttachmentManager.Instance.GetEquippedAttachmentIds();
            }

            if (Player.PlayerArmorController.Instance != null)
            {
                data.currentArmorHP = Player.PlayerArmorController.Instance.CurrentArmorHP;
                data.hasArmorEquipped = Player.PlayerArmorController.Instance.HasArmorEquipped;
            }

            // Phase 3: Progression, Bosses & Blueprint Persistence
            if (Progression.SurvivorProgressionManager.Instance != null)
            {
                Progression.SurvivorProgressionManager pm = Progression.SurvivorProgressionManager.Instance;
                data.playerLevel = pm.PlayerLevel;
                data.currentXP = pm.CurrentXP;
                data.unspentSkillPoints = pm.UnspentSkillPoints;
                data.unlockedSkillIds = pm.GetUnlockedSkillIds();
                data.unlockedBlueprintIds = pm.GetUnlockedBlueprintIds();
                data.defeatedBossIds = pm.GetDefeatedBossIds();
                data.openedApexCacheIds = pm.GetOpenedApexCacheIds();
            }

            // Phase 4: Weather Persistence
            if (Environment.DynamicWeatherManager.Instance != null)
            {
                data.currentWeatherState = (int)Environment.DynamicWeatherManager.Instance.CurrentWeather;
            }

            // Deep Expansion Phase 2: Trauma, Injury & Consumable Buff Persistence
            if (Survival.TraumaAndInjurySystem.Instance != null)
            {
                Survival.TraumaAndInjurySystem.Instance.SerializeState(data);
            }

            // Deep Expansion Phase 1: Vehicles Persistence
            var allVehicles = Object.FindObjectsByType<Vehicles.DrivableVehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allVehicles.Length; i++)
            {
                var v = allVehicles[i];
                if (v == null) continue;

                var rec = new VehicleSaveRecord
                {
                    vehicleId = v.VehicleId,
                    posX = v.transform.position.x,
                    posY = v.transform.position.y,
                    posZ = v.transform.position.z,
                    rotY = v.transform.eulerAngles.y,
                    engineHP = v.EngineHP,
                    fuel = v.Fuel,
                    headlightsOn = v.IsHeadlightsOn
                };

                foreach (var slot in v.TrunkInventory)
                {
                    if (slot != null && slot.item != null && slot.quantity > 0)
                    {
                        rec.trunkItemIds.Add(slot.item.itemId);
                        rec.trunkItemCounts.Add(slot.quantity);
                    }
                }

                data.vehicleRecords.Add(rec);
            }

            // Active Phase 3: Base Building & Fortifications Persistence
            data.builtStructures.Clear();
            var allStructures = Object.FindObjectsByType<Building.BuildableStructure>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allStructures.Length; i++)
            {
                if (allStructures[i] != null)
                {
                    data.builtStructures.Add(allStructures[i].ToSaveRecord());
                }
            }

            // Active Phase 5: Tactical Radio Scanner, Story Arc & Audio Logs Persistence
            if (Narrative.MainStoryDirector.Instance != null)
            {
                data.currentStoryChapter = Narrative.MainStoryDirector.Instance.CurrentChapter;
                data.currentObjectiveId = Narrative.MainStoryDirector.Instance.CurrentObjectiveId;
                data.isMainStoryCompleted = Narrative.MainStoryDirector.Instance.IsStoryCompleted;
                data.endgameMoralChoice = Narrative.MainStoryDirector.Instance.EndgameMoralChoice;
                data.collectedAudioLogIds = new List<string>(Narrative.MainStoryDirector.Instance.CollectedAudioLogIds);
            }
            if (Survival.TacticalRadioScanner.Instance != null)
            {
                data.decodedFrequencies = new List<float>(Survival.TacticalRadioScanner.Instance.DecodedFrequencies);
            }
            if (Progression.SurvivorProgressionManager.Instance != null)
            {
                data.hasGlobalCurePerk = Progression.SurvivorProgressionManager.Instance.HasGlobalCurePerk;
            }
            if (Narrative.StarterPrologueDirector.Instance != null)
            {
                data.isPrologueCompleted = Narrative.StarterPrologueDirector.Instance.IsPrologueCompleted;
                data.prologueStageIndex = Narrative.StarterPrologueDirector.Instance.CurrentStageIndex;
            }
            if (Narrative.ApartmentWakeUpSequence.Instance != null)
            {
                data.hasCheckedIntroPhone = Narrative.ApartmentWakeUpSequence.Instance.HasCheckedPhone;
            }

            // Step 3C: Fuel Sources (Gas Stations & Wrecks) Persistence
            var pumps = Object.FindObjectsByType<Interaction.GasStationPumpInteractable>(FindObjectsSortMode.None);
            if (pumps != null)
            {
                for (int i = 0; i < pumps.Length; i++)
                {
                    if (pumps[i] != null)
                    {
                        data.fuelSources.Add(new SavedFuelSourceState
                        {
                            sourceId = pumps[i].PersistentId,
                            remainingLiters = pumps[i].RemainingFuelLiters
                        });
                    }
                }
            }
            var wrecks = Object.FindObjectsByType<Interaction.WreckedCarSiphonInteractable>(FindObjectsSortMode.None);
            if (wrecks != null)
            {
                for (int i = 0; i < wrecks.Length; i++)
                {
                    if (wrecks[i] != null)
                    {
                        data.fuelSources.Add(new SavedFuelSourceState
                        {
                            sourceId = wrecks[i].PersistentId,
                            remainingLiters = wrecks[i].FuelReserveLiters
                        });
                    }
                }
            }

            // Phase 4 Step 4A: Underground Bunker Persistence
            if (Interaction.UndergroundBunkerDirector.Instance != null)
            {
                data.isBunkerGeneratorPowered = Interaction.UndergroundBunkerDirector.Instance.IsGeneratorPowered;
                data.isBunkerSecurityDoorUnlocked = Interaction.UndergroundBunkerDirector.Instance.IsSecurityDoorUnlocked;
                data.isBunkerVaultLooted = Interaction.UndergroundBunkerDirector.Instance.IsVaultLooted;
            }

            SaveSystem.Save(data);
            PlayerPrefs.SetInt("HasSaveData", 1);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] Game state and NPC trust saved successfully.");
        }

        public void StartNewGame()
        {
            SaveSystem.DeleteSave();
            PlayerPrefs.DeleteKey("HasSaveData");
            PlayerPrefs.Save();
            SetGameState(GameState.Gameplay);
        }

        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            if (data != null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    var cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    player.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
                    if (cc != null) cc.enabled = true;

                    var stats = player.GetComponent<Survival.SurvivalStats>();
                    if (stats != null)
                    {
                        stats.Heal(data.health - stats.CurrentHealth);
                        stats.Feed(data.hunger - stats.CurrentHunger);
                        stats.Hydrate(data.thirst - stats.CurrentThirst);
                        stats.ModifyInfection(data.infection - stats.CurrentInfection);
                    }
                }

                if (Narrative.ChoiceProcessor.Instance != null)
                {
                    Narrative.ChoiceProcessor.Instance.SetWorldFlags(data.worldFlags);
                }

                if (Narrative.StoryManager.Instance != null)
                {
                    Narrative.StoryManager.Instance.LoadTrustData(data.npcTrustKeys, data.npcTrustValues);
                }

                if (ProjectSurvival.Environment.FogOfWarManager.Instance != null)
                {
                    if (!string.IsNullOrEmpty(data.fogOfWarDataV2))
                    {
                        ProjectSurvival.Environment.FogOfWarManager.Instance.LoadSerializedFogDataV2(data.fogOfWarDataV2);
                    }
                    else if (!string.IsNullOrEmpty(data.fogOfWarData))
                    {
                        ProjectSurvival.Environment.FogOfWarManager.Instance.LoadLegacy256FogData(data.fogOfWarData);
                    }
                }

                if (DayNightManager.Instance != null && data.currentHour > 0f)
                {
                    DayNightManager.Instance.SetTime(data.currentHour);
                }

                if (data.interactableKeys != null && data.interactableValues != null && data.interactableKeys.Count == data.interactableValues.Count)
                {
                    var interactables = Object.FindObjectsByType<ProjectSurvival.Interaction.WorldInteractableObject>(FindObjectsSortMode.None);
                    if (interactables != null)
                    {
                        var lookup = new System.Collections.Generic.Dictionary<string, string>();
                        for (int i = 0; i < data.interactableKeys.Count; i++)
                        {
                            lookup[data.interactableKeys[i]] = data.interactableValues[i];
                        }

                        for (int i = 0; i < interactables.Length; i++)
                        {
                            var obj = interactables[i];
                            if (obj != null && lookup.TryGetValue(obj.PersistentId, out string state))
                            {
                                obj.LoadSerializedState(state);
                            }
                        }
                    }
                }

                if (AI.CommunityOutpostManager.Instance != null)
                {
                    AI.CommunityOutpostManager.Instance.LoadSaveData(data);
                }

                // Extended Audit Remediation: 5 Persistence Pillars Restore
                if (Player.WeaponSystem.Instance != null)
                {
                    Player.WeaponSystem.Instance.RestoreWeaponState(data.equippedWeaponType, data.isWeaponHolstered);
                }

                if (UI.QuickSlotUI.Instance != null && data.quickSlotItemIds != null && data.quickSlotItemIds.Length == 4)
                {
                    UI.QuickSlotUI.Instance.RestoreSlotItemIds(data.quickSlotItemIds);
                }

                if (AI.CompanionManager.Instance != null && !string.IsNullOrEmpty(data.activeCompanionId))
                {
                    AI.CompanionManager.Instance.RestoreCompanion(data.activeCompanionId);
                }

                if (Environment.CustomWaypointManager.Instance != null)
                {
                    if (data.hasCustomWaypoint)
                    {
                        Environment.CustomWaypointManager.Instance.SetWaypoint(new Vector3(data.customWaypointX, 0f, data.customWaypointZ));
                    }
                    else
                    {
                        Environment.CustomWaypointManager.Instance.ClearWaypoint();
                    }
                }

                var gridPanel = Object.FindAnyObjectByType<Interaction.RegionalMasterGridPanel>(FindObjectsInactive.Include);
                if (gridPanel != null && data.isRegionalGridActive)
                {
                    gridPanel.SetPoweredStateSilent(true);
                }

                if (OpenWorld.OutpostLiberationManager.Instance != null && data.liberatedOutpostIds != null)
                {
                    OpenWorld.OutpostLiberationManager.Instance.RestoreLiberatedOutposts(data.liberatedOutpostIds);
                }

                // Phase 2: Weapon Attachments & Armor Restore
                if (Player.WeaponAttachmentManager.Instance != null && data.unlockedAttachmentIds != null)
                {
                    Player.WeaponAttachmentManager.Instance.RestoreAttachments(data.unlockedAttachmentIds, data.equippedAttachmentIds);
                }

                if (Player.PlayerArmorController.Instance != null)
                {
                    Player.PlayerArmorController.Instance.RestoreArmor(data.currentArmorHP, data.hasArmorEquipped);
                }

                // Phase 3: Progression, Bosses & Blueprint Restore
                if (Progression.SurvivorProgressionManager.Instance != null)
                {
                    Progression.SurvivorProgressionManager.Instance.RestoreState(
                        data.playerLevel,
                        data.currentXP,
                        data.unspentSkillPoints,
                        data.unlockedSkillIds,
                        data.unlockedBlueprintIds,
                        data.defeatedBossIds,
                        data.openedApexCacheIds
                    );
                }

                // Phase 4: Weather Restore
                if (Environment.DynamicWeatherManager.Instance != null)
                {
                    Environment.DynamicWeatherManager.Instance.SetWeather((Environment.WeatherState)data.currentWeatherState, true);
                }

                // Deep Expansion Phase 2: Trauma, Injury & Consumable Buff Restore
                if (Survival.TraumaAndInjurySystem.Instance != null)
                {
                    Survival.TraumaAndInjurySystem.Instance.RestoreState(data);
                }

                // Deep Expansion Phase 1: Vehicles Restore
                if (data.vehicleRecords != null && data.vehicleRecords.Count > 0)
                {
                    var sceneVehicles = Object.FindObjectsByType<Vehicles.DrivableVehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    var dict = new Dictionary<string, Vehicles.DrivableVehicleController>();
                    for (int i = 0; i < sceneVehicles.Length; i++)
                    {
                        if (sceneVehicles[i] != null && !string.IsNullOrEmpty(sceneVehicles[i].VehicleId))
                        {
                            dict[sceneVehicles[i].VehicleId] = sceneVehicles[i];
                        }
                    }

                    for (int i = 0; i < data.vehicleRecords.Count; i++)
                    {
                        var rec = data.vehicleRecords[i];
                        if (rec != null && dict.TryGetValue(rec.vehicleId, out var vc))
                        {
                            vc.RestoreState(rec.posX, rec.posY, rec.posZ, rec.rotY, rec.engineHP, rec.fuel, rec.trunkItemIds, rec.trunkItemCounts);
                            vc.SetHeadlights(rec.headlightsOn);
                        }
                    }
                }

                // Active Phase 3: Base Building & Fortifications Restore
                if (Building.BaseBuildingManager.Instance != null && data.builtStructures != null)
                {
                    Building.BaseBuildingManager.Instance.RestoreSavedStructures(data.builtStructures);
                }

                // Active Phase 5: Tactical Radio Scanner, Story Arc & Audio Logs Restore
                if (Survival.TacticalRadioScanner.Instance != null && data.decodedFrequencies != null)
                {
                    Survival.TacticalRadioScanner.Instance.RestoreDecodedFrequencies(data.decodedFrequencies);
                }
                if (Progression.SurvivorProgressionManager.Instance != null)
                {
                    Progression.SurvivorProgressionManager.Instance.HasGlobalCurePerk = data.hasGlobalCurePerk;
                }
                if (Narrative.MainStoryDirector.Instance != null)
                {
                    Narrative.MainStoryDirector.Instance.RestoreStoryState(
                        data.currentStoryChapter,
                        data.currentObjectiveId,
                        data.isMainStoryCompleted,
                        data.endgameMoralChoice,
                        data.collectedAudioLogIds,
                        data.hasGlobalCurePerk
                    );
                }
                if (Narrative.StarterPrologueDirector.Instance != null)
                {
                    Narrative.StarterPrologueDirector.Instance.RestoreState(data.isPrologueCompleted, data.prologueStageIndex);
                }
                if (Narrative.ApartmentWakeUpSequence.Instance != null)
                {
                    Narrative.ApartmentWakeUpSequence.Instance.RestorePhoneChecked(data.hasCheckedIntroPhone);
                }

                // Step 3C: Fuel Sources (Gas Stations & Wrecks) Restore
                if (data.fuelSources != null && data.fuelSources.Count > 0)
                {
                    var fuelLookup = new System.Collections.Generic.Dictionary<string, float>();
                    for (int i = 0; i < data.fuelSources.Count; i++)
                    {
                        if (data.fuelSources[i] != null && !string.IsNullOrEmpty(data.fuelSources[i].sourceId))
                        {
                            fuelLookup[data.fuelSources[i].sourceId] = data.fuelSources[i].remainingLiters;
                        }
                    }

                    var loadedPumps = Object.FindObjectsByType<Interaction.GasStationPumpInteractable>(FindObjectsSortMode.None);
                    if (loadedPumps != null)
                    {
                        for (int i = 0; i < loadedPumps.Length; i++)
                        {
                            if (loadedPumps[i] != null && fuelLookup.TryGetValue(loadedPumps[i].PersistentId, out float liters))
                            {
                                loadedPumps[i].SetRemainingFuelLiters(liters);
                            }
                        }
                    }

                    var loadedWrecks = Object.FindObjectsByType<Interaction.WreckedCarSiphonInteractable>(FindObjectsSortMode.None);
                    if (loadedWrecks != null)
                    {
                        for (int i = 0; i < loadedWrecks.Length; i++)
                        {
                            if (loadedWrecks[i] != null && fuelLookup.TryGetValue(loadedWrecks[i].PersistentId, out float liters))
                            {
                                loadedWrecks[i].SetFuelReserveLiters(liters);
                            }
                        }
                    }
                }

                // Phase 4 Step 4A: Underground Bunker Restore
                if (Interaction.UndergroundBunkerDirector.Instance != null)
                {
                    Interaction.UndergroundBunkerDirector.Instance.RestoreBunkerState(
                        data.isBunkerGeneratorPowered,
                        data.isBunkerSecurityDoorUnlocked,
                        data.isBunkerVaultLooted
                    );
                }
            }

            SetGameState(GameState.Gameplay);
            Debug.Log("[GameManager] Game state and NPC trust loaded successfully.");
        }
    }
}
