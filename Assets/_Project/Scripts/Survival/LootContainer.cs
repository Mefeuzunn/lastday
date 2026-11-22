using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    [Serializable]
    public class LootEntry
    {
        public ItemData item;
        public int count = 1;
        [Range(0f, 1f)] public float dropChance = 1f;
    }

    [RequireComponent(typeof(Collider))]
    public class LootContainer : MonoBehaviour
    {
        [Header("Container Properties")]
        [SerializeField] private string containerName = "Ganimet Sandığı";
        [SerializeField] private bool isLooted = false;
        [SerializeField] private List<LootEntry> possibleLoot = new List<LootEntry>();

        [Header("Feedback")]
        [SerializeField] private Transform visualModelTransform;

        private bool playerInRange = false;

        public bool IsLooted => isLooted;

        public void AddItem(ItemData item, int count = 1, float dropChance = 1f)
        {
            if (item == null) return;
            possibleLoot.Add(new LootEntry { item = item, count = count, dropChance = dropChance });
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            if (visualModelTransform == null)
            {
                Transform visualChild = transform.Find("Visual_Model");
                if (visualChild != null) visualModelTransform = visualChild;
            }
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
            if (other.CompareTag("Player") && !isLooted)
            {
                playerInRange = true;
                GameEvents.TriggerInteractionPromptChanged(true, $"{containerName} Aç");
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
            if (!playerInRange || isLooted) return;

            LootAll();
        }

        public void LootAll()
        {
            if (isLooted) return;

            int itemsGiven = 0;
            if (InventorySystem.Instance != null)
            {
                foreach (var entry in possibleLoot)
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

            isLooted = true;
            GameEvents.TriggerInteractionPromptChanged(false, string.Empty);
            Debug.Log($"[LootContainer] {containerName} opened! {itemsGiven} items looted.");

            // Visual feedback - slightly shrink or tilt open
            if (visualModelTransform != null)
            {
                visualModelTransform.localScale = Vector3.Scale(visualModelTransform.localScale, new Vector3(1f, 0.7f, 1f));
            }
        }
    }
}
