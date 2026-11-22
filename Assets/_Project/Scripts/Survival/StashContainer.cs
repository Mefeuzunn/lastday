using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    [RequireComponent(typeof(Collider))]
    public class StashContainer : MonoBehaviour
    {
        public static StashContainer Instance { get; private set; }

        [Header("Stash Setup")]
        [SerializeField] private string stashTitle = "Sığınak Sandığı";
        [SerializeField] private int maxSlots = 24;
        [SerializeField] private List<InventorySlot> storedSlots = new List<InventorySlot>();

        private bool playerInRange = false;

        public string StashTitle => stashTitle;
        public IReadOnlyList<InventorySlot> StoredSlots => storedSlots;

        public event Action OnStashUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            GameEvents.OnInteractPressed += HandleInteract;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractPressed -= HandleInteract;
            if (playerInRange)
            {
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;
                GameEvents.TriggerInteractionPromptChanged(true, "SANDIĞI AÇ (Erzak Deposu)");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            }
        }

        private void HandleInteract()
        {
            if (!playerInRange) return;
            GameEvents.TriggerOpenStashRequested();
        }

        public bool StoreItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            // Try stack
            foreach (var slot in storedSlots)
            {
                if (slot.item != null && slot.item.itemId == item.itemId && slot.quantity < item.maxStack)
                {
                    int space = item.maxStack - slot.quantity;
                    int toAdd = Mathf.Min(space, quantity);
                    slot.quantity += toAdd;
                    quantity -= toAdd;

                    if (quantity <= 0)
                    {
                        OnStashUpdated?.Invoke();
                        return true;
                    }
                }
            }

            // New slot
            if (storedSlots.Count < maxSlots)
            {
                storedSlots.Add(new InventorySlot(item, quantity));
                OnStashUpdated?.Invoke();
                return true;
            }

            return false;
        }

        public bool WithdrawItem(string itemId, int quantity = 1)
        {
            for (int i = 0; i < storedSlots.Count; i++)
            {
                if (storedSlots[i].item != null && storedSlots[i].item.itemId == itemId)
                {
                    int take = Mathf.Min(storedSlots[i].quantity, quantity);
                    if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(storedSlots[i].item, take))
                    {
                        storedSlots[i].quantity -= take;
                        if (storedSlots[i].quantity <= 0)
                        {
                            storedSlots.RemoveAt(i);
                        }
                        OnStashUpdated?.Invoke();
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        public void LoadFromSave(List<string> itemIds, List<int> itemCounts)
        {
            storedSlots.Clear();
            if (itemIds == null || itemCounts == null) return;

            for (int i = 0; i < itemIds.Count && i < itemCounts.Count; i++)
            {
                ItemData item = Resources.Load<ItemData>($"Data/Items/{itemIds[i]}");
                if (item != null)
                {
                    storedSlots.Add(new InventorySlot(item, itemCounts[i]));
                }
            }
            OnStashUpdated?.Invoke();
        }
    }
}
