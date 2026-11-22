using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;
using ProjectSurvival.UI;

namespace ProjectSurvival.Narrative
{
    public enum PrologueStage
    {
        Stage1_CheckPhone = 0,
        Stage2_ScavengeFoodWater = 1,
        Stage3_ExitAndFindWeapon = 2,
        Completed = 3
    }

    /// <summary>
    /// Step C: Starter Prologue & Survival Onboarding Director.
    /// Drives the opening player flow:
    /// 1. Waking up in bedroom & checking smartphone alert.
    /// 2. Scavenging food and water rations from StarterHouse_Safehome cabinets.
    /// 3. Exiting front door onto porch to claim first outdoor weapon (Fire Axe or 9mm Pistol).
    /// 4. Seamless narrative handoff to MainStoryDirector (Chapter 1: 92.4 MHz Radio Signal).
    /// </summary>
    public class StarterPrologueDirector : MonoBehaviour
    {
        public static StarterPrologueDirector Instance { get; private set; }

        [Header("Prologue State")]
        [SerializeField] private bool isPrologueCompleted = false;
        [SerializeField] private PrologueStage currentStage = PrologueStage.Stage1_CheckPhone;

        [Header("Stage 2 Scavenge Tracking")]
        [SerializeField] private bool hasLootedFood = false;
        [SerializeField] private bool hasLootedWater = false;

        [Header("Stage 3 Weapon Tracking")]
        [SerializeField] private bool hasEquippedWeapon = false;

        public bool IsPrologueCompleted => isPrologueCompleted;
        public PrologueStage CurrentStage => currentStage;
        public int CurrentStageIndex => (int)currentStage;
        public bool HasLootedFood => hasLootedFood;
        public bool HasLootedWater => hasLootedWater;
        public bool HasEquippedWeapon => hasEquippedWeapon;

        public event Action<PrologueStage> OnStageChanged;
        public event Action OnPrologueFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (isPrologueCompleted)
            {
                Debug.Log("[StarterPrologueDirector] Prologue already completed. Skipping onboarding flow.");
                return;
            }

            // Dispatch initial Stage 1 objective
            DispatchCurrentPrologueObjective();

            // Auto-prompt smartphone after brief wake-up orientation
            StartCoroutine(WakeUpSequenceRoutine());
        }

        private IEnumerator WakeUpSequenceRoutine()
        {
            // Allow player to experience bedroom wake-up sequence and naturally inspect the phone
            yield return new WaitForSeconds(15.0f);

            if (!isPrologueCompleted && currentStage == PrologueStage.Stage1_CheckPhone)
            {
                if (ApartmentWakeUpSequence.Instance == null || !ApartmentWakeUpSequence.Instance.HasCheckedPhone)
                {
                    GameEvents.TriggerObjectiveToastRequested("Yatağın yanındaki akıllı telefona bak.", 3.0f);
                }
            }

            yield return new WaitForSeconds(15.0f);

            if (!isPrologueCompleted && currentStage == PrologueStage.Stage1_CheckPhone)
            {
                if (ApartmentWakeUpSequence.Instance == null || !ApartmentWakeUpSequence.Instance.HasCheckedPhone)
                {
                    SmartphoneScreenUI.Instance?.ShowSmartphone();
                }
            }
        }

        public void OnSmartphoneClosed()
        {
            if (isPrologueCompleted) return;

            if (currentStage == PrologueStage.Stage1_CheckPhone)
            {
                Debug.Log("[StarterPrologueDirector] Stage 1 Completed: Smartphone checked.");
                SurvivorProgressionManager.Instance?.AddXP(10);
                GameEvents.TriggerObjectiveToastRequested("Acil Durum Uyarısı Okundu (+10 XP)", 2.5f);

                AdvanceToStage(PrologueStage.Stage2_ScavengeFoodWater);
            }
        }

        public void OnItemLooted(string itemId, int quantity)
        {
            if (isPrologueCompleted || currentStage != PrologueStage.Stage2_ScavengeFoodWater) return;

            if (itemId == "item_canned_food" || itemId == "item_hot_stew")
            {
                hasLootedFood = true;
            }
            else if (itemId == "item_water_bottle" || itemId == "item_purified_water")
            {
                hasLootedWater = true;
            }

            // Update Quest Tracker with dynamic count
            DispatchCurrentPrologueObjective();

            // Check completion of Stage 2
            if (hasLootedFood && hasLootedWater)
            {
                Debug.Log("[StarterPrologueDirector] Stage 2 Completed: Food & Water scavenged.");
                SurvivorProgressionManager.Instance?.AddXP(25);
                GameEvents.TriggerObjectiveToastRequested("Erzaklar Toplandı (+25 XP) — Ön Kapıdan Dışarı Çık", 3.0f);

                AdvanceToStage(PrologueStage.Stage3_ExitAndFindWeapon);
            }
        }

        public void OnWeaponPickedUp(WeaponType wType, string weaponId)
        {
            if (isPrologueCompleted || currentStage != PrologueStage.Stage3_ExitAndFindWeapon) return;

            if (wType == WeaponType.Axe || wType == WeaponType.Pistol || wType == WeaponType.Shotgun || wType == WeaponType.Crossbow)
            {
                hasEquippedWeapon = true;
                Debug.Log($"[StarterPrologueDirector] Stage 3 Completed: Weapon {wType} equipped.");

                SurvivorProgressionManager.Instance?.AddXP(50);
                GameEvents.TriggerObjectiveToastRequested("Artık Kendini Savunabilirsin — Hayatta Kalma Başladı (+50 XP)", 4.0f);

                CompletePrologueAndHandoff();
            }
        }

        public void AdvanceToStage(PrologueStage newStage)
        {
            currentStage = newStage;
            DispatchCurrentPrologueObjective();
            OnStageChanged?.Invoke(currentStage);
        }

        public void CompletePrologueAndHandoff()
        {
            if (isPrologueCompleted) return;

            isPrologueCompleted = true;
            currentStage = PrologueStage.Completed;

            OnPrologueFinished?.Invoke();

            // Seamless handoff to Chapter 1
            if (MainStoryDirector.Instance != null)
            {
                MainStoryDirector.Instance.DispatchCurrentObjective();
            }

            // Save completed state
            GameManager.Instance?.SaveGame();
            Debug.Log("[StarterPrologueDirector] Prologue fully completed. Seamless handoff to Chapter 1 radio signal.");
        }

        public void DispatchCurrentPrologueObjective()
        {
            if (isPrologueCompleted) return;

            StoryObjective obj = GetCurrentPrologueObjective();
            if (obj != null)
            {
                QuestTrackerHUD.Instance?.RefreshObjectiveDisplay(obj);
            }
        }

        public StoryObjective GetCurrentPrologueObjective()
        {
            switch (currentStage)
            {
                case PrologueStage.Stage1_CheckPhone:
                    return new StoryObjective
                    {
                        id = "prologue_1_phone",
                        chapter = 0,
                        chapterTitle = "BÖLÜM 0: UYANIŞ",
                        title = "Başucundaki akıllı telefona bak",
                        description = "Yatağının yanındaki akıllı telefonu aç ve acil durum uyarısını oku.",
                        targetPosition = new Vector3(-1.6f, 0.72f, -1.8f),
                        requiredFrequency = 0f,
                        isTerminalInteraction = false,
                        xpReward = 10
                    };

                case PrologueStage.Stage2_ScavengeFoodWater:
                    int count = (hasLootedFood ? 1 : 0) + (hasLootedWater ? 1 : 0);
                    return new StoryObjective
                    {
                        id = "prologue_2_scavenge",
                        chapter = 0,
                        chapterTitle = "BÖLÜM 0: İLK ERZAK",
                        title = $"Mutfak dolabı ve buzdolabından yiyecek ve su bul ({count}/2)",
                        description = "Mutfak dolaplarından veya buzdolabından en az 1 Konserve ve 1 Su şişesi topla.",
                        targetPosition = new Vector3(-2.2f, 0.85f, 2.5f),
                        requiredFrequency = 0f,
                        isTerminalInteraction = false,
                        xpReward = 25
                    };

                case PrologueStage.Stage3_ExitAndFindWeapon:
                    return new StoryObjective
                    {
                        id = "prologue_3_weapon",
                        chapter = 0,
                        chapterTitle = "BÖLÜM 0: DIŞARI ÇIK",
                        title = "Ön kapıdan çık ve ilk silahını bul",
                        description = "Ön kapıdan dışarı çık ve sokaktaki barikatta saplı ilk silahını kuşan.",
                        targetPosition = new Vector3(0f, 0.85f, 6.5f), // Fire axe location
                        requiredFrequency = 0f,
                        isTerminalInteraction = false,
                        xpReward = 50
                    };

                default:
                    return null;
            }
        }

        public void RestoreState(bool completed, int stageIndex)
        {
            isPrologueCompleted = completed;
            currentStage = (PrologueStage)Mathf.Clamp(stageIndex, 0, 3);

            if (isPrologueCompleted)
            {
                currentStage = PrologueStage.Completed;
            }
            else
            {
                DispatchCurrentPrologueObjective();
            }
            Debug.Log($"[StarterPrologueDirector] Restored state: Completed={isPrologueCompleted}, Stage={currentStage}");
        }

        // Test Helper for Automated Audits
        public void ForceSetStage(PrologueStage stage)
        {
            currentStage = stage;
            isPrologueCompleted = (stage == PrologueStage.Completed);
            DispatchCurrentPrologueObjective();
        }
    }
}
