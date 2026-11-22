using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Recipe Catalog")]
        [SerializeField] private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();

        public IReadOnlyList<CraftingRecipe> AvailableRecipes => availableRecipes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultRecipesIfEmpty();
        }

        private void LoadDefaultRecipesIfEmpty()
        {
            if (availableRecipes.Count == 0)
            {
                CraftingRecipe[] loaded = Resources.LoadAll<CraftingRecipe>("Recipes");
                if (loaded != null && loaded.Length > 0)
                {
                    availableRecipes.AddRange(loaded);
                }
            }
        }

        public void RegisterRecipe(CraftingRecipe recipe)
        {
            if (recipe != null && !availableRecipes.Contains(recipe))
            {
                availableRecipes.Add(recipe);
            }
        }

        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null || recipe.resultItem == null) return false;
            if (InventorySystem.Instance == null) return false;

            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null || ing.count <= 0) continue;
                if (!InventorySystem.Instance.HasItem(ing.item.itemId, ing.count))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Craft(CraftingRecipe recipe)
        {
            if (!CanCraft(recipe))
            {
                Debug.LogWarning($"[CraftingSystem] Insufficient ingredients to craft {recipe.recipeName}.");
                return false;
            }

            // Consume ingredients
            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null || ing.count <= 0) continue;
                InventorySystem.Instance.RemoveItem(ing.item.itemId, ing.count);
            }

            // Grant result
            InventorySystem.Instance.AddItem(recipe.resultItem, recipe.resultCount);

            GameEvents.TriggerCraftingCompleted(recipe.recipeName);
            Debug.Log($"[CraftingSystem] Successfully crafted {recipe.resultCount}x {recipe.resultItem.itemName}!");
            return true;
        }
    }
}
