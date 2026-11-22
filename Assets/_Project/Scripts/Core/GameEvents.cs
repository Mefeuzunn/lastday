using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Core
{
    public static class GameEvents
    {
        // --- Game State Events ---
        public static event Action<GameState> OnGameStateChanged;
        public static void TriggerGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        // --- Survival Stat Events ---
        public static event Action<float, float> OnHealthChanged;
        public static void TriggerHealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);

        public static event Action<float, float> OnStaminaChanged;
        public static void TriggerStaminaChanged(float current, float max) => OnStaminaChanged?.Invoke(current, max);

        public static event Action<float, float> OnHungerChanged;
        public static void TriggerHungerChanged(float current, float max) => OnHungerChanged?.Invoke(current, max);

        public static event Action<float, float> OnThirstChanged;
        public static void TriggerThirstChanged(float current, float max) => OnThirstChanged?.Invoke(current, max);

        public static event Action<float, float> OnInfectionChanged;
        public static void TriggerInfectionChanged(float current, float max) => OnInfectionChanged?.Invoke(current, max);

        public static event Action<float, float> OnArmorChanged;
        public static void TriggerArmorChanged(float current, float max) => OnArmorChanged?.Invoke(current, max);

        public static event Action OnPlayerDied;
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();

        // --- Player & Stealth & Noise Events ---
        public static event Action<Vector3, float> OnNoiseEmitted;
        public static void TriggerNoiseEmitted(Vector3 position, float radius) => OnNoiseEmitted?.Invoke(position, radius);

        public static event Action<bool> OnStealthStateChanged;
        public static void TriggerStealthStateChanged(bool isCrouching) => OnStealthStateChanged?.Invoke(isCrouching);

        public static event Action OnInteractPressed;
        public static void TriggerInteractPressed() => OnInteractPressed?.Invoke();

        // --- Inventory & Interaction Events ---
        public static event Action OnInventoryUpdated;
        public static void TriggerInventoryUpdated() => OnInventoryUpdated?.Invoke();

        public static event Action<bool, string> OnInteractionPromptChanged;
        public static void TriggerInteractionPromptChanged(bool isVisible, string message) => OnInteractionPromptChanged?.Invoke(isVisible, message);

        // --- Narrative Events ---
        public static event Action<string, string> OnStoryRequested;
        public static void TriggerStoryRequested(string storyResourceName, string startNodeId) => OnStoryRequested?.Invoke(storyResourceName, startNodeId);

        public static event Action<string, string> OnDialogueStarted;
        public static void TriggerDialogueStarted(string storyResource, string startNodeId) => OnDialogueStarted?.Invoke(storyResource, startNodeId);

        public static event Action OnDialogueEnded;
        public static void TriggerDialogueEnded() => OnDialogueEnded?.Invoke();

        public static event Action OnDialogueClosed;
        public static void TriggerDialogueClosed()
        {
            OnDialogueClosed?.Invoke();
            OnDialogueEnded?.Invoke();
        }

        public static event Action<string, int, int> OnNPCTrustChanged;
        public static void TriggerNPCTrustChanged(string npcId, int newTrust, int delta) => OnNPCTrustChanged?.Invoke(npcId, newTrust, delta);

        // --- Mission Events ---
        public static event Action<string, string, float, float> OnMissionProgressUpdated;
        public static void TriggerMissionProgressUpdated(string missionId, string description, float current, float required) => 
            OnMissionProgressUpdated?.Invoke(missionId, description, current, required);

        public static event Action<string, string> OnMissionCompleted;
        public static void TriggerMissionCompleted(string missionId, string nextSuccessNodeId) => 
            OnMissionCompleted?.Invoke(missionId, nextSuccessNodeId);

        public static event Action<string, string> OnMissionFailed;
        public static void TriggerMissionFailed(string missionId, string failNodeId) => 
            OnMissionFailed?.Invoke(missionId, failNodeId);

        // --- Atmosphere & Flashlight Events ---
        public static event Action<bool, float> OnDayNightChanged;
        public static void TriggerDayNightChanged(bool isNight, float hour) => OnDayNightChanged?.Invoke(isNight, hour);

        public static event Action<bool, float> OnFlashlightToggled;
        public static void TriggerFlashlightToggled(bool isOn, float batteryPercent) => OnFlashlightToggled?.Invoke(isOn, batteryPercent);

        public static event Action<float, float> OnFlashlightBatteryChanged;
        public static void TriggerFlashlightBatteryChanged(float current, float max) => OnFlashlightBatteryChanged?.Invoke(current, max);

        // --- Crafting Events ---
        public static event Action<string> OnCraftingCompleted;
        public static void TriggerCraftingCompleted(string recipeName) => OnCraftingCompleted?.Invoke(recipeName);

        // --- Phase 3: Combat, Feedback, Audio & Lore Events ---
        public static event Action<string, int> OnItemCollected;
        public static void TriggerItemCollected(string itemName, int count) => OnItemCollected?.Invoke(itemName, count);

        public static event Action<float> OnPlayerDamaged;
        public static void TriggerPlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);

        public static event Action<GameObject, string> OnZombieAlertStateChanged;
        public static void TriggerZombieAlertStateChanged(GameObject zombie, string alertState) => OnZombieAlertStateChanged?.Invoke(zombie, alertState);

        public static event Action<float, float> OnCameraShakeRequested;
        public static void TriggerCameraShakeRequested(float intensity, float duration) => OnCameraShakeRequested?.Invoke(intensity, duration);

        public static event Action<GameObject> OnStealthTakedownPerformed;
        public static void TriggerStealthTakedownPerformed(GameObject targetZombie) => OnStealthTakedownPerformed?.Invoke(targetZombie);

        public static event Action OnEmergencyPushPerformed;
        public static void TriggerEmergencyPushPerformed() => OnEmergencyPushPerformed?.Invoke();

        public static event Action<string, string> OnNoteCollected;
        public static void TriggerNoteCollected(string title, string content) => OnNoteCollected?.Invoke(title, content);

        public static event Action<bool> OnSafeHouseStateChanged;
        public static void TriggerSafeHouseStateChanged(bool isInside) => OnSafeHouseStateChanged?.Invoke(isInside);

        public static event Action OnOpenBackpackRequested;
        public static void TriggerOpenBackpackRequested() => OnOpenBackpackRequested?.Invoke();

        public static event Action OnOpenStashRequested;
        public static void TriggerOpenStashRequested() => OnOpenStashRequested?.Invoke();

        public static event Action OnCivilianRescued;
        public static void TriggerCivilianRescued() => OnCivilianRescued?.Invoke();

        // --- Faz 2: Companion & NPC Events ---
        public static event Action<string, GameObject> OnCompanionRecruited;
        public static void TriggerCompanionRecruited(string name, GameObject companionObj) => OnCompanionRecruited?.Invoke(name, companionObj);

        public static event Action<string> OnCompanionDismissed;
        public static void TriggerCompanionDismissed(string name) => OnCompanionDismissed?.Invoke(name);

        public static event Action<float, float> OnCompanionHealthChanged;
        public static void TriggerCompanionHealthChanged(float current, float max) => OnCompanionHealthChanged?.Invoke(current, max);

        public static event Action<bool> OnCompanionOrderChanged;
        public static void TriggerCompanionOrderChanged(bool isFollowing) => OnCompanionOrderChanged?.Invoke(isFollowing);

        public static event Action<string, string> OnScavengerRescued;
        public static void TriggerScavengerRescued(string scavengerName, string rewardMessage) => OnScavengerRescued?.Invoke(scavengerName, rewardMessage);

        // --- Faz 4: Chapter & Kill Events ---
        public static event Action<GameObject> OnZombieKilled;
        public static void TriggerZombieKilled(GameObject zombie) => OnZombieKilled?.Invoke(zombie);

        public static event Action<string> OnChapterCompleted;
        public static void TriggerChapterCompleted(string chapterId) => OnChapterCompleted?.Invoke(chapterId);

        // --- Phase 3: Progression & Boss Events ---
        public static event Action<int, string> OnXPGained;
        public static void TriggerXPGained(int amount, string reason) => OnXPGained?.Invoke(amount, reason);

        public static event Action<int, int> OnLevelUp;
        public static void TriggerLevelUp(int newLevel, int skillPoints) => OnLevelUp?.Invoke(newLevel, skillPoints);

        public static event Action<int> OnSkillPointsChanged;
        public static void TriggerSkillPointsChanged(int unspentPoints) => OnSkillPointsChanged?.Invoke(unspentPoints);

        public static event Action<string> OnPerkUnlocked;
        public static void TriggerPerkUnlocked(string perkId) => OnPerkUnlocked?.Invoke(perkId);

        public static event Action<string, string> OnBossDefeated;
        public static void TriggerBossDefeated(string bossId, string bossName) => OnBossDefeated?.Invoke(bossId, bossName);

        public static event Action<string> OnApexCacheUnlocked;
        public static void TriggerApexCacheUnlocked(string cacheId) => OnApexCacheUnlocked?.Invoke(cacheId);

        // --- Reset all events upon scene unload ---
        public static event Action<float> OnSearchProgressUpdated;
        public static void TriggerSearchProgressUpdated(float progress) => OnSearchProgressUpdated?.Invoke(progress);

        public static event Action OnMapToggleRequested;
        public static void TriggerMapToggleRequested() => OnMapToggleRequested?.Invoke();

        // --- Phase 8: Weapon, Combat & Hiding Events ---
        public static event Action<string, int, int> OnWeaponChanged;
        public static void TriggerWeaponChanged(string weaponName, int currentAmmo, int reserveAmmo) => OnWeaponChanged?.Invoke(weaponName, currentAmmo, reserveAmmo);

        public static event Action OnWeaponFired;
        public static void TriggerWeaponFired() => OnWeaponFired?.Invoke();

        public static event Action<bool, Vector3> OnHitConfirmed;
        public static void TriggerHitConfirmed(bool isHeadshot, Vector3 hitPos) => OnHitConfirmed?.Invoke(isHeadshot, hitPos);

        public static event Action<float> OnCrosshairSpreadChanged;
        public static void TriggerCrosshairSpreadChanged(float spreadMultiplier) => OnCrosshairSpreadChanged?.Invoke(spreadMultiplier);

        public static event Action<bool> OnPlayerHidingStateChanged;
        public static void TriggerPlayerHidingStateChanged(bool isHiding) => OnPlayerHidingStateChanged?.Invoke(isHiding);

        public static event Action<string, float> OnObjectiveToastRequested;
        public static void TriggerObjectiveToastRequested(string message, float duration = 5f) => OnObjectiveToastRequested?.Invoke(message, duration);

        public static event Action<Vector3, string> OnWaypointChanged;
        public static void TriggerWaypointChanged(Vector3 position, string label) => OnWaypointChanged?.Invoke(position, label);

        // --- Phase 2: Zombie Horde & Pack Alert Events ---
        public static event Action<Vector3, float> OnHordeAlertTriggered;
        public static void TriggerHordeAlertTriggered(Vector3 center, float radius) => OnHordeAlertTriggered?.Invoke(center, radius);

        // --- Stage 3: Community, Barter & Social Events ---
        public static event Action<float, float> OnCommunityMoraleChanged;
        public static void TriggerCommunityMoraleChanged(float currentMorale, float delta) => OnCommunityMoraleChanged?.Invoke(currentMorale, delta);

        public static event Action<string, string> OnBarterRequested;
        public static void TriggerBarterRequested(string npcId, string npcDisplayName) => OnBarterRequested?.Invoke(npcId, npcDisplayName);

        public static event Action<string> OnBarterCompleted;
        public static void TriggerBarterCompleted(string npcId) => OnBarterCompleted?.Invoke(npcId);

        public static event Action<Vector3, string, float> OnScavengerSquadDistress;
        public static void TriggerScavengerSquadDistress(Vector3 location, string message, float duration) => OnScavengerSquadDistress?.Invoke(location, message, duration);

        public static void ClearAll()
        {
            OnScavengerSquadDistress = null;
            OnBarterCompleted = null;
            OnBarterRequested = null;
            OnCommunityMoraleChanged = null;
            OnHordeAlertTriggered = null;
            OnWaypointChanged = null;
            OnWeaponChanged = null;
            OnWeaponFired = null;
            OnHitConfirmed = null;
            OnCrosshairSpreadChanged = null;
            OnPlayerHidingStateChanged = null;
            OnObjectiveToastRequested = null;
            OnSearchProgressUpdated = null;
            OnMapToggleRequested = null;
            OnGameStateChanged = null;
            OnHealthChanged = null;
            OnStaminaChanged = null;
            OnHungerChanged = null;
            OnThirstChanged = null;
            OnInfectionChanged = null;
            OnArmorChanged = null;
            OnPlayerDied = null;
            OnNoiseEmitted = null;
            OnStealthStateChanged = null;
            OnInteractPressed = null;
            OnInventoryUpdated = null;
            OnInteractionPromptChanged = null;
            OnStoryRequested = null;
            OnDialogueClosed = null;
            OnNPCTrustChanged = null;
            OnMissionProgressUpdated = null;
            OnMissionCompleted = null;
            OnMissionFailed = null;
            OnDayNightChanged = null;
            OnFlashlightToggled = null;
            OnFlashlightBatteryChanged = null;
            OnCraftingCompleted = null;
            OnItemCollected = null;
            OnPlayerDamaged = null;
            OnZombieAlertStateChanged = null;
            OnCameraShakeRequested = null;
            OnStealthTakedownPerformed = null;
            OnEmergencyPushPerformed = null;
            OnNoteCollected = null;
            OnSafeHouseStateChanged = null;
            OnOpenBackpackRequested = null;
            OnOpenStashRequested = null;
            OnCivilianRescued = null;
            OnCompanionRecruited = null;
            OnCompanionDismissed = null;
            OnCompanionHealthChanged = null;
            OnCompanionOrderChanged = null;
            OnScavengerRescued = null;
            OnZombieKilled = null;
            OnChapterCompleted = null;
            OnXPGained = null;
            OnLevelUp = null;
            OnSkillPointsChanged = null;
            OnPerkUnlocked = null;
            OnBossDefeated = null;
            OnApexCacheUnlocked = null;
        }
    }
}
