using UnityEngine;

namespace ProjectSurvival.Survival
{
    /// <summary>
    /// Global facade and alias for InventorySystem to guarantee bidirectional compatibility.
    /// </summary>
    public static class InventoryManager
    {
        public static InventorySystem Instance => InventorySystem.Instance;

        public static bool HasItem(string itemId, int count = 1) => InventorySystem.Instance != null && InventorySystem.Instance.HasItem(itemId, count);

        public static int GetItemCount(string itemId) => InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(itemId) : 0;

        public static bool RemoveItem(string itemId, int count = 1) => InventorySystem.Instance != null && InventorySystem.Instance.RemoveItem(itemId, count);

        public static bool AddItem(ItemData item, int quantity = 1) => InventorySystem.Instance != null && InventorySystem.Instance.AddItem(item, quantity);

        public static bool AddItem(string itemId, int quantity = 1) => AddItemById(itemId, quantity);

        public static bool AddItemById(string itemId, int quantity = 1) => InventorySystem.Instance != null && InventorySystem.Instance.AddItemById(itemId, quantity);
    }
}
