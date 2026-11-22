using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    [Serializable]
    public class CraftingSlotUI
    {
        public GameObject rootObject;
        public Text recipeTitleText;
        public Text ingredientsText;
        public Button craftButton;
    }

    public class CraftingUI : MonoBehaviour
    {
        public static CraftingUI Instance { get; private set; }

        [Header("Panel Elements")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private List<CraftingSlotUI> recipeSlots = new List<CraftingSlotUI>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnInventoryUpdated += RefreshUI;
            GameEvents.OnCraftingCompleted += HandleCraftingCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryUpdated -= RefreshUI;
            GameEvents.OnCraftingCompleted -= HandleCraftingCompleted;
        }

        public void TogglePanel()
        {
            if (panelRoot != null)
            {
                bool newState = !panelRoot.activeSelf;
                panelRoot.SetActive(newState);
                if (newState)
                {
                    RefreshUI();
                }
            }
        }

        public void OpenPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                RefreshUI();
            }
        }

        public void ClosePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void HandleCraftingCompleted(string recipeName)
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            if (CraftingSystem.Instance == null) return;

            var recipes = CraftingSystem.Instance.AvailableRecipes;

            for (int i = 0; i < recipeSlots.Count; i++)
            {
                var slot = recipeSlots[i];
                if (slot.rootObject == null) continue;

                if (i < recipes.Count)
                {
                    slot.rootObject.SetActive(true);
                    CraftingRecipe r = recipes[i];

                    if (slot.recipeTitleText != null)
                    {
                        slot.recipeTitleText.text = $"{r.recipeName} (x{r.resultCount})";
                    }

                    // Build ingredients summary
                    string ingStr = "";
                    bool canCraft = CraftingSystem.Instance.CanCraft(r);

                    foreach (var ing in r.ingredients)
                    {
                        if (ing.item == null) continue;
                        int owned = GetItemCount(ing.item.itemId);
                        string colorTag = owned >= ing.count ? "<color=#55FF55>" : "<color=#FF5555>";
                        ingStr += $"{ing.item.itemName}: {colorTag}{owned}/{ing.count}</color>  ";
                    }

                    if (slot.ingredientsText != null)
                    {
                        slot.ingredientsText.text = ingStr;
                    }

                    if (slot.craftButton != null)
                    {
                        slot.craftButton.interactable = canCraft;
                        slot.craftButton.onClick.RemoveAllListeners();
                        slot.craftButton.onClick.AddListener(() =>
                        {
                            CraftingSystem.Instance.Craft(r);
                        });
                    }
                }
                else
                {
                    slot.rootObject.SetActive(false);
                }
            }
        }

        private int GetItemCount(string itemId)
        {
            if (InventorySystem.Instance == null) return 0;
            int total = 0;
            foreach (var slot in InventorySystem.Instance.Slots)
            {
                if (slot.item != null && slot.item.itemId == itemId)
                {
                    total += slot.quantity;
                }
            }
            return total;
        }
    }
}
