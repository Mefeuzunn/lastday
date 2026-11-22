using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.AI
{
    public class CommunityOutpostManager : MonoBehaviour
    {
        public static CommunityOutpostManager Instance { get; private set; }

        [Header("Community Morale (0 - 100%)")]
        [Range(0f, 100f)]
        [SerializeField] private float communityMorale = 75f;
        [SerializeField] private float minMoraleForStashGeneration = 35f;

        [Header("Passive Resource Generation")]
        [Tooltip("Seconds between automatic stash contributions (default: 240s = 4 minutes)")]
        [SerializeField] private float generationInterval = 240f;
        [SerializeField] private bool enablePassiveStash = true;

        [Header("Safe House Population Roster")]
        [SerializeField] private List<string> safeHouseResidents = new List<string>()
        {
            "Dr. Aras",
            "Kerem",
            "Zeynep",
            "Elif"
        };

        private float nextGenerationTime = 0f;

        public float CommunityMorale => communityMorale;
        public IReadOnlyList<string> SafeHouseResidents => safeHouseResidents;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            nextGenerationTime = Time.time + generationInterval;
        }

        private void OnEnable()
        {
            GameEvents.OnCivilianRescued += HandleCivilianRescued;
            GameEvents.OnScavengerRescued += HandleScavengerRescued;
            GameEvents.OnSafeHouseStateChanged += HandleSafeHouseStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCivilianRescued -= HandleCivilianRescued;
            GameEvents.OnScavengerRescued -= HandleScavengerRescued;
            GameEvents.OnSafeHouseStateChanged -= HandleSafeHouseStateChanged;
        }

        private void Update()
        {
            if (!enablePassiveStash) return;

            if (Time.time >= nextGenerationTime)
            {
                nextGenerationTime = Time.time + generationInterval;
                TryGeneratePassiveSupplies();
            }
        }

        public void ModifyMorale(float delta)
        {
            float old = communityMorale;
            communityMorale = Mathf.Clamp(communityMorale + delta, 0f, 100f);
            GameEvents.TriggerCommunityMoraleChanged(communityMorale, delta);

            Debug.Log($"[CommunityOutpostManager] Morale modified by {delta:+#;-#;0}%. New: {communityMorale:F0}%");
        }

        public void AddResident(string residentName)
        {
            if (string.IsNullOrEmpty(residentName)) return;

            if (!safeHouseResidents.Contains(residentName))
            {
                safeHouseResidents.Add(residentName);
                ModifyMorale(12f);
                GameEvents.TriggerObjectiveToastRequested($"[TOPLULUK BÜYÜYOR] {residentName} sığınağa yerleşti! (+%12 Moral)");
            }
        }

        private void HandleCivilianRescued()
        {
            AddResident("Mert (Sivil)");
        }

        private void HandleScavengerRescued(string name, string reward)
        {
            AddResident($"Keşifçi {name}");
            ModifyMorale(15f);
        }

        private void HandleSafeHouseStateChanged(bool isInside)
        {
            // Slight morale stabilization when player returns to base
            if (isInside && communityMorale < 60f)
            {
                ModifyMorale(2.5f);
            }
        }

        public void TryGeneratePassiveSupplies()
        {
            if (communityMorale < minMoraleForStashGeneration)
            {
                Debug.Log("[CommunityOutpostManager] Morale too low for community scavenging.");
                return;
            }

            StashContainer stash = StashContainer.Instance;
            if (stash == null) return;

            float yieldMultiplier = communityMorale >= 80f ? 1.5f : 1.0f;

            int waterCount = Mathf.RoundToInt(UnityEngine.Random.Range(1, 3) * yieldMultiplier);
            int bandageCount = Mathf.RoundToInt(UnityEngine.Random.Range(1, 2) * yieldMultiplier);
            int scrapCount = Mathf.RoundToInt(UnityEngine.Random.Range(2, 5) * yieldMultiplier);
            int ammoCount = Mathf.RoundToInt(UnityEngine.Random.Range(8, 16) * yieldMultiplier);

            DepositItem(stash, "item_water_bottle", waterCount);
            DepositItem(stash, "item_bandage", bandageCount);
            DepositItem(stash, "item_scrap", scrapCount);
            DepositItem(stash, "ammo_9mm", ammoCount);

            GameEvents.TriggerObjectiveToastRequested("Sığınak Telsizi: Ekip depoya yeni erzak bıraktı.", 4.5f);
            ProceduralAudioManager.Instance?.PlayItemPickup();

            Debug.Log($"[CommunityOutpostManager] Passive supplies deposited into Stash! (Water:{waterCount}, Bandage:{bandageCount}, Scrap:{scrapCount}, 9mm:{ammoCount})");
        }

        private void DepositItem(StashContainer stash, string itemId, int count)
        {
            ItemData data = InventorySystem.FindItemData(itemId);
            if (data != null && count > 0)
            {
                stash.StoreItem(data, count);
            }
        }

        public void PopulateSaveData(SaveData data)
        {
            if (data == null) return;
            data.communityMorale = communityMorale;
            data.safeHouseResidents = new List<string>(safeHouseResidents);
        }

        public void LoadSaveData(SaveData data)
        {
            if (data == null) return;
            if (data.communityMorale > 0f) communityMorale = data.communityMorale;
            if (data.safeHouseResidents != null && data.safeHouseResidents.Count > 0)
            {
                safeHouseResidents = new List<string>(data.safeHouseResidents);
            }
        }
    }
}
