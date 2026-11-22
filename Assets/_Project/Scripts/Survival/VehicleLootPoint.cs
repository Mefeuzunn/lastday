using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    public enum VehicleType
    {
        Civilian,
        Police,
        Ambulance,
        Taxi,
        Van
    }

    [RequireComponent(typeof(Collider))]
    public class VehicleLootPoint : MonoBehaviour
    {
        [Header("Vehicle Identity")]
        [SerializeField] private string vehicleName = "Terk Edilmiş Araç";
        [SerializeField] private VehicleType vehicleType = VehicleType.Civilian;
        [SerializeField] private bool isSearched = false;

        [Header("Search Mechanics")]
        [SerializeField] private float searchDuration = 2.5f;
        [SerializeField] private float noiseRadius = 14f;
        [SerializeField] private Transform trunkTransform;

        [Header("Contextual Loot")]
        [SerializeField] private List<LootEntry> lootTable = new List<LootEntry>();

        private bool playerInRange = false;
        private bool isSearching = false;
        private float searchTimer = 0f;
        private float noiseTickTimer = 0f;

        public bool IsSearched => isSearched;
        public VehicleType Type => vehicleType;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            if (trunkTransform == null)
            {
                trunkTransform = transform.Find("Trunk_Black") 
                    ?? transform.Find("RearDoors") 
                    ?? transform.Find("Trunk")
                    ?? transform.Find("Visual_Model");
            }

            if (lootTable == null || lootTable.Count == 0)
            {
                SetupDefaultLootTable();
            }
        }

        private void SetupDefaultLootTable()
        {
            lootTable = new List<LootEntry>();
            ItemData medkit = Resources.Load<ItemData>("Data/Items/item_medkit") ?? LoadItem("item_medkit");
            ItemData water = Resources.Load<ItemData>("Data/Items/item_water_bottle") ?? LoadItem("item_water_bottle");
            ItemData canned = Resources.Load<ItemData>("Data/Items/item_canned_food") ?? LoadItem("item_canned_food");
            ItemData scrap = Resources.Load<ItemData>("Data/Items/item_scrap_metal") ?? LoadItem("item_scrap_metal");
            ItemData cloth = Resources.Load<ItemData>("Data/Items/item_cloth") ?? LoadItem("item_cloth");
            ItemData battery = Resources.Load<ItemData>("Data/Items/item_battery") ?? LoadItem("item_battery");

            switch (vehicleType)
            {
                case VehicleType.Ambulance:
                    if (medkit != null) lootTable.Add(new LootEntry { item = medkit, count = 1, dropChance = 1f });
                    if (cloth != null) lootTable.Add(new LootEntry { item = cloth, count = 2, dropChance = 0.9f });
                    break;

                case VehicleType.Police:
                    if (battery != null) lootTable.Add(new LootEntry { item = battery, count = 2, dropChance = 0.9f });
                    if (medkit != null) lootTable.Add(new LootEntry { item = medkit, count = 1, dropChance = 0.6f });
                    if (scrap != null) lootTable.Add(new LootEntry { item = scrap, count = 2, dropChance = 0.8f });
                    break;

                case VehicleType.Van:
                    if (canned != null) lootTable.Add(new LootEntry { item = canned, count = 2, dropChance = 1f });
                    if (water != null) lootTable.Add(new LootEntry { item = water, count = 2, dropChance = 0.9f });
                    if (scrap != null) lootTable.Add(new LootEntry { item = scrap, count = 3, dropChance = 0.75f });
                    break;

                default: // Civilian & Taxi
                    if (water != null) lootTable.Add(new LootEntry { item = water, count = 1, dropChance = 0.85f });
                    if (canned != null) lootTable.Add(new LootEntry { item = canned, count = 1, dropChance = 0.75f });
                    if (cloth != null) lootTable.Add(new LootEntry { item = cloth, count = 1, dropChance = 0.8f });
                    if (scrap != null) lootTable.Add(new LootEntry { item = scrap, count = 1, dropChance = 0.6f });
                    break;
            }
        }

        private ItemData LoadItem(string itemId)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{itemId} t:ItemData");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            }
#endif
            return null;
        }

        private void OnEnable()
        {
            GameEvents.OnInteractPressed += StartOrContinueSearch;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractPressed -= StartOrContinueSearch;
            CancelSearch();
            if (playerInRange)
            {
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isSearched)
            {
                playerInRange = true;
                UpdatePromptText();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                CancelSearch();
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void UpdatePromptText()
        {
            if (isSearched)
            {
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
            else if (isSearching)
            {
                int pct = Mathf.RoundToInt((searchTimer / searchDuration) * 100f);
                GameEvents.TriggerInteractionPromptChanged(true, $"Aranıyor... %{pct}");
            }
            else
            {
                GameEvents.TriggerInteractionPromptChanged(true, $"{vehicleName} Bagajını Ara");
            }
        }

        private void StartOrContinueSearch()
        {
            if (!playerInRange || isSearched) return;

            if (!isSearching)
            {
                isSearching = true;
                searchTimer = 0f;
                noiseTickTimer = 0f;
                UpdatePromptText();
            }
        }

        private void Update()
        {
            if (!isSearching || !playerInRange || isSearched) return;

            searchTimer += Time.deltaTime;
            noiseTickTimer += Time.deltaTime;

            // Emit noise every 0.75s while rustling through trunk
            if (noiseTickTimer >= 0.75f)
            {
                noiseTickTimer = 0f;
                GameEvents.TriggerNoiseEmitted(transform.position, noiseRadius);
            }

            float progress = Mathf.Clamp01(searchTimer / searchDuration);
            GameEvents.TriggerSearchProgressUpdated(progress);
            UpdatePromptText();

            if (searchTimer >= searchDuration)
            {
                CompleteSearch();
            }
        }

        private void CancelSearch()
        {
            if (isSearching)
            {
                isSearching = false;
                searchTimer = 0f;
                GameEvents.TriggerSearchProgressUpdated(-1f);
                if (playerInRange && !isSearched)
                {
                    UpdatePromptText();
                }
            }
        }

        private void CompleteSearch()
        {
            isSearching = false;
            isSearched = true;
            GameEvents.TriggerSearchProgressUpdated(-1f);
            GameEvents.TriggerInteractionPromptChanged(false, string.Empty);

            // Pop / animate trunk open
            if (trunkTransform != null)
            {
                trunkTransform.localPosition += new Vector3(0f, 0.25f, -0.15f);
                trunkTransform.localRotation *= Quaternion.Euler(-32f, 0f, 0f);
            }

            // Distribute loot
            int itemsGiven = 0;
            if (InventorySystem.Instance != null)
            {
                foreach (var entry in lootTable)
                {
                    if (entry.item != null && UnityEngine.Random.value <= entry.dropChance)
                    {
                        bool added = InventorySystem.Instance.AddItem(entry.item, entry.count);
                        if (added)
                        {
                            itemsGiven++;
                            GameEvents.TriggerItemCollected(entry.item.itemName, entry.count);
                        }
                    }
                }
            }

            Debug.Log($"[VehicleLootPoint] {vehicleName} arandı. {itemsGiven} eşya bulundu!");
        }

        public void Configure(string name, VehicleType type, float duration = 2.5f, float noise = 14f)
        {
            vehicleName = name;
            vehicleType = type;
            searchDuration = duration;
            noiseRadius = noise;
            SetupDefaultLootTable();
        }
    }
}
