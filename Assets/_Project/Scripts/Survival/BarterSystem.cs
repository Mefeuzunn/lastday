using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Survival
{
    [Serializable]
    public class BarterItemEntry
    {
        public string itemId;
        public int availableQuantity;
        public int baseValue;
        public int minTrustRequired;

        public BarterItemEntry(string id, int qty, int val, int trust = 0)
        {
            itemId = id;
            availableQuantity = qty;
            baseValue = val;
            minTrustRequired = trust;
        }
    }

    public class BarterSystem : MonoBehaviour
    {
        public static BarterSystem Instance { get; private set; }

        private readonly Dictionary<string, List<BarterItemEntry>> merchantStocks = new Dictionary<string, List<BarterItemEntry>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultMerchantStocks();
        }

        private void InitializeDefaultMerchantStocks()
        {
            // Kerem: Weapons, Ammo, Tools
            merchantStocks["kerem"] = new List<BarterItemEntry>
            {
                new BarterItemEntry("ammo_9mm", 30, 1, 0),          // 1 value per bullet
                new BarterItemEntry("ammo_shells", 12, 3, 20),      // 3 value per shell
                new BarterItemEntry("item_crowbar", 1, 35, 10),
                new BarterItemEntry("weapon_axe", 1, 65, 30),
                new BarterItemEntry("item_scrap", 15, 4, 0)
            };

            // Dr. Aras: Medical Supplies
            merchantStocks["aras"] = new List<BarterItemEntry>
            {
                new BarterItemEntry("item_bandage", 5, 12, 0),
                new BarterItemEntry("item_splint", 3, 18, 10),
                new BarterItemEntry("item_painkillers", 4, 15, 0),
                new BarterItemEntry("item_medkit", 2, 35, 20),
                new BarterItemEntry("item_antiviral", 1, 60, 45),
                new BarterItemEntry("item_water_bottle", 4, 10, 0)
            };

            // Elif: Tactical Intelligence & Throwables
            merchantStocks["elif"] = new List<BarterItemEntry>
            {
                new BarterItemEntry("item_distraction_bottle", 4, 10, 0),
                new BarterItemEntry("item_sound_bomb", 2, 28, 25),
                new BarterItemEntry("item_battery", 3, 14, 0),
                new BarterItemEntry("item_map_intel", 1, 55, 35)    // Intel map unlocks Fog of War!
            };

            // Zeynep: Crafting & Defense Materials
            merchantStocks["zeynep"] = new List<BarterItemEntry>
            {
                new BarterItemEntry("item_scrap", 20, 4, 0),
                new BarterItemEntry("item_crowbar", 1, 35, 0),
                new BarterItemEntry("item_shiv", 2, 18, 0),
                new BarterItemEntry("item_battery", 2, 14, 15)
            };
        }

        public List<BarterItemEntry> GetMerchantStock(string npcId)
        {
            if (merchantStocks.Count == 0)
            {
                InitializeDefaultMerchantStocks();
            }

            string key = npcId?.ToLowerInvariant() ?? "";
            foreach (var kvp in merchantStocks)
            {
                if (key.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            return new List<BarterItemEntry>();
        }

        public int GetItemValue(string itemId)
        {
            return itemId switch
            {
                "weapon_axe" => 65,
                "weapon_pistol" => 75,
                "weapon_shotgun" => 90,
                "ammo_9mm" => 1,
                "ammo_shells" => 3,
                "item_medkit" => 35,
                "item_antiviral" => 60,
                "item_bandage" => 12,
                "item_map_intel" => 55,
                "item_sound_bomb" => 28,
                "item_distraction_bottle" => 10,
                "item_canned_food" => 14,
                "item_water_bottle" => 10,
                "item_alcohol" => 16,
                "item_battery" => 14,
                "item_crowbar" => 35,
                "item_shiv" => 18,
                "item_scrap" => 4,
                "item_cloth" => 6,
                _ => 8
            };
        }

        public float GetTrustDiscountMultiplier(string npcId)
        {
            int trust = GetTrust(npcId);
            if (trust >= 75) return 0.75f; // 25% discount
            if (trust >= 50) return 0.82f; // 18% discount
            if (trust >= 25) return 0.90f; // 10% discount
            return 1.0f;                   // Standard price
        }

        public int GetTrust(string npcId)
        {
            if (StoryManager.Instance != null)
            {
                int val = StoryManager.Instance.GetTrust(npcId);
                return val > 0 ? val : 50;
            }
            return 50;
        }

        public bool ExecuteBarter(string npcId, Dictionary<string, int> playerOfferedItems, Dictionary<string, int> requestedMerchantItems)
        {
            if (playerOfferedItems == null || requestedMerchantItems == null || requestedMerchantItems.Count == 0)
            {
                return false;
            }

            InventorySystem playerInv = InventorySystem.Instance;
            if (playerInv == null) return false;

            // Verify player has all offered items
            foreach (var kvp in playerOfferedItems)
            {
                if (!playerInv.HasItem(kvp.Key, kvp.Value))
                {
                    Debug.LogWarning($"[BarterSystem] Player missing required item {kvp.Key} x{kvp.Value}");
                    return false;
                }
            }

            // Calculate values
            int playerTotalValue = 0;
            foreach (var kvp in playerOfferedItems)
            {
                playerTotalValue += GetItemValue(kvp.Key) * kvp.Value;
            }

            float discount = GetTrustDiscountMultiplier(npcId);
            int merchantTotalCost = 0;
            foreach (var kvp in requestedMerchantItems)
            {
                int rawCost = GetItemValue(kvp.Key) * kvp.Value;
                merchantTotalCost += Mathf.RoundToInt(rawCost * discount);
            }

            if (playerTotalValue < merchantTotalCost)
            {
                Debug.LogWarning($"[BarterSystem] Trade value insufficient: Player offers {playerTotalValue}, Merchant requests {merchantTotalCost}");
                return false;
            }

            // Deduct offered items from player
            foreach (var kvp in playerOfferedItems)
            {
                playerInv.RemoveItem(kvp.Key, kvp.Value);
            }

            // Grant merchant items to player
            var stock = GetMerchantStock(npcId);
            foreach (var kvp in requestedMerchantItems)
            {
                playerInv.AddItemById(kvp.Key, kvp.Value);

                // Deduct from merchant stock
                for (int i = 0; i < stock.Count; i++)
                {
                    if (stock[i].itemId == kvp.Key)
                    {
                        stock[i].availableQuantity = Mathf.Max(0, stock[i].availableQuantity - kvp.Value);
                        break;
                    }
                }
            }

            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerObjectiveToastRequested("Takas başarıyla tamamlandı!", 3.5f);
            GameEvents.TriggerBarterCompleted(npcId);

            Debug.Log($"[BarterSystem] Barter with {npcId} successful! Offered: {playerTotalValue} val, Acquired: {merchantTotalCost} val.");
            return true;
        }
    }
}
