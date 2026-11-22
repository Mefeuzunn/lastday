using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Vehicles
{
    /// <summary>
    /// Micro-Step 1B: 12-Slot Portable Trunk Storage & Animated Tailgate Controller.
    /// Mounts on hero vehicles to provide localized mobile cargo capacity with
    /// smooth 75° downward tailgate hinge rotation and bidirectional inventory transfers.
    /// </summary>
    public class VehicleTrunkStorage : MonoBehaviour
    {
        [Header("Trunk Capacity")]
        [SerializeField] private int maxSlots = 12;
        [SerializeField] private List<InventorySlot> trunkItems = new List<InventorySlot>();

        [Header("Animated Tailgate")]
        [SerializeField] private Transform tailgateTransform;
        [SerializeField] private float openAngleDegrees = 75f;
        [SerializeField] private float animationDuration = 0.35f;

        private Quaternion closedRotation = Quaternion.identity;
        private Quaternion openRotation;
        private Coroutine tailgateCoroutine;
        private bool isTrunkOpen = false;

        public int MaxSlots => maxSlots;
        public int Capacity => maxSlots;
        public IReadOnlyList<InventorySlot> Items => trunkItems;
        public int OccupiedSlots => trunkItems.Count;
        public bool IsTrunkOpen => isTrunkOpen;

        public void SetCapacity(int slots)
        {
            maxSlots = Mathf.Max(1, slots);
        }

        private void Awake()
        {
            LocateTailgate();
            closedRotation = tailgateTransform != null ? tailgateTransform.localRotation : Quaternion.identity;
            openRotation = closedRotation * Quaternion.Euler(openAngleDegrees, 0f, 0f);
        }

        public void LocateTailgate()
        {
            if (tailgateTransform != null) return;
            tailgateTransform = transform.Find("Visual_Model/Tailgate") ?? transform.Find("Tailgate");
        }

        public void SetTailgateTransform(Transform t)
        {
            tailgateTransform = t;
            if (tailgateTransform != null)
            {
                closedRotation = tailgateTransform.localRotation;
                openRotation = closedRotation * Quaternion.Euler(openAngleDegrees, 0f, 0f);
            }
        }

        // =========================================================================
        // OPEN / CLOSE & TAILGATE HINGE ANIMATION
        // =========================================================================

        public void OpenTrunk()
        {
            isTrunkOpen = true;
            AnimateTailgate(openRotation);

            if (VehicleTrunkUI.Instance != null)
            {
                VehicleTrunkUI.Instance.OpenTrunkWithStorage(this);
            }

            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            MobileHapticManager.Instance?.TriggerLight();
        }

        public void CloseTrunk()
        {
            isTrunkOpen = false;
            AnimateTailgate(closedRotation);

            if (VehicleTrunkUI.Instance != null && VehicleTrunkUI.Instance.IsOpen)
            {
                VehicleTrunkUI.Instance.CloseTrunk();
            }

            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.Instance?.TriggerLight();
        }

        private void AnimateTailgate(Quaternion targetRot)
        {
            if (tailgateTransform == null) return;

            if (tailgateCoroutine != null) StopCoroutine(tailgateCoroutine);
            tailgateCoroutine = StartCoroutine(TailgateRoutine(targetRot));
        }

        private IEnumerator TailgateRoutine(Quaternion targetRot)
        {
            Quaternion startRot = tailgateTransform.localRotation;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
                tailgateTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            tailgateTransform.localRotation = targetRot;
            tailgateCoroutine = null;
        }

        // =========================================================================
        // BIDIRECTIONAL INVENTORY TRANSFERS
        // =========================================================================

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            // 1. Stack into existing slot if available
            for (int i = 0; i < trunkItems.Count; i++)
            {
                if (trunkItems[i].item != null && trunkItems[i].item.itemId == item.itemId && trunkItems[i].quantity < item.maxStack)
                {
                    int space = item.maxStack - trunkItems[i].quantity;
                    int toAdd = Mathf.Min(space, quantity);
                    trunkItems[i].quantity += toAdd;
                    quantity -= toAdd;

                    if (quantity <= 0) return true;
                }
            }

            // 2. Add into new slot if space exists
            if (trunkItems.Count < maxSlots && quantity > 0)
            {
                trunkItems.Add(new InventorySlot(item, quantity));
                return true;
            }

            return false;
        }

        public bool TransferToPlayer(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= trunkItems.Count) return false;

            InventorySlot slot = trunkItems[slotIndex];
            if (slot == null || slot.item == null || slot.quantity <= 0) return false;

            if (InventoryManager.AddItem(slot.item, slot.quantity))
            {
                trunkItems.RemoveAt(slotIndex);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                return true;
            }

            GameEvents.TriggerObjectiveToastRequested("Çantanız dolu!", 1.8f);
            return false;
        }

        public void TransferAllToPlayer()
        {
            int transferredCount = 0;
            for (int i = trunkItems.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = trunkItems[i];
                if (slot != null && slot.item != null && slot.quantity > 0)
                {
                    if (InventoryManager.AddItem(slot.item, slot.quantity))
                    {
                        trunkItems.RemoveAt(i);
                        transferredCount++;
                    }
                }
            }

            if (transferredCount > 0)
            {
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("Tüm eşyalar çantaya aktarıldı.", 2.0f);
            }
        }

        public bool StoreItem(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            if (AddItem(item, quantity))
            {
                InventoryManager.Instance?.RemoveItem(item.itemId, quantity);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                return true;
            }

            GameEvents.TriggerObjectiveToastRequested("Araç bagajı dolu!", 1.8f);
            return false;
        }

        public void RestoreItems(List<string> itemIds, List<int> counts)
        {
            trunkItems.Clear();
            if (itemIds != null && counts != null)
            {
                for (int i = 0; i < Mathf.Min(itemIds.Count, counts.Count); i++)
                {
                    ItemData it = InventorySystem.FindItemData(itemIds[i]);
                    if (it != null && counts[i] > 0)
                    {
                        trunkItems.Add(new InventorySlot(it, counts[i]));
                    }
                }
            }
        }

        public void StoreAllFromPlayer()
        {
            if (InventoryManager.Instance == null) return;

            var playerSlots = InventoryManager.Instance.Slots;
            int storedCount = 0;

            for (int i = playerSlots.Count - 1; i >= 0; i--)
            {
                InventorySlot pSlot = playerSlots[i];
                if (pSlot != null && pSlot.item != null && pSlot.quantity > 0)
                {
                    if (AddItem(pSlot.item, pSlot.quantity))
                    {
                        InventoryManager.Instance.RemoveItem(pSlot.item.itemId, pSlot.quantity);
                        storedCount++;
                    }
                }
            }

            if (storedCount > 0)
            {
                GameEvents.TriggerInventoryUpdated();
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.Instance?.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("Çantadaki eşyalar bagaja aktarıldı.", 2.0f);
            }
        }

        public void SetItemsDirectly(List<InventorySlot> items)
        {
            trunkItems = items ?? new List<InventorySlot>();
        }
    }
}
