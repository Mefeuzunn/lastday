using UnityEngine;

namespace ProjectSurvival.Survival
{
    public enum ItemType
    {
        Food,
        Water,
        Medicine,
        Distraction,
        QuestItem,
        Resource,
        Weapon,
        Tool
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "ProjectSurvival/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public ItemType itemType;
        public int maxStack = 10;

        [Header("Survival Effects")]
        public float healthEffect = 0f;
        public float hungerEffect = 0f;
        public float thirstEffect = 0f;
        public float infectionEffect = 0f;

        [Header("Weapon & Tool Properties")]
        public float weaponDamage = 25f;
        public int maxDurability = 5;

        [Header("Distraction Properties")]
        public float distractionNoiseRadius = 14f;
        public GameObject throwPrefab;
    }
}
