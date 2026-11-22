using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Survival
{
    [Serializable]
    public class IngredientEntry
    {
        public ItemData item;
        public int count = 1;
    }

    [CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "ProjectSurvival/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Recipe Details")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 3)] public string description;

        [Header("Output")]
        public ItemData resultItem;
        public int resultCount = 1;

        [Header("Ingredients")]
        public List<IngredientEntry> ingredients = new List<IngredientEntry>();
    }
}
