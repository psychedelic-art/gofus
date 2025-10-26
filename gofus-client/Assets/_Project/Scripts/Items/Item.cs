using UnityEngine;

namespace GOFUS.Items
{
    [System.Serializable]
    public class Item
    {
        public int Id;
        public string Name;
        public string Description;
        public ItemType Type;
        public int Level;
        public int Value;
        public Sprite Icon;
        public bool IsStackable;
        public int StackSize = 1;
        public ItemRarity Rarity;

        // Equipment specific
        public int AttackBonus;
        public int DefenseBonus;
        public int HealthBonus;
        public int ManaBonus;
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material,
        Quest,
        Currency
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}