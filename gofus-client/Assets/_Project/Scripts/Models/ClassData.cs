using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOFUS.Models
{
    /// <summary>
    /// Represents a character class in GOFUS (Feca, Iop, etc.)
    /// </summary>
    [Serializable]
    public class ClassData
    {
        public int id;
        public string name;
        public string description;
        public StatsPerLevel statsPerLevel;
        public List<SpellData> startingSpells;
        public List<SpellData> allSpells;

        // UI helpers
        public Color GetClassColor()
        {
            switch (id)
            {
                case 1: return new Color(0.6f, 0.4f, 0.2f); // Feca - Brown
                case 2: return new Color(0.2f, 0.6f, 0.3f); // Osamodas - Green
                case 3: return new Color(0.8f, 0.7f, 0.3f); // Enutrof - Gold
                case 4: return new Color(0.4f, 0.2f, 0.5f); // Sram - Purple
                case 5: return new Color(0.3f, 0.4f, 0.7f); // Xelor - Blue
                case 6: return new Color(0.4f, 0.6f, 0.3f); // Ecaflip - Light Green
                case 7: return new Color(0.7f, 0.3f, 0.5f); // Eniripsa - Pink
                case 8: return new Color(0.8f, 0.3f, 0.2f); // Iop - Red
                case 9: return new Color(0.3f, 0.6f, 0.7f); // Cra - Cyan
                case 10: return new Color(0.3f, 0.5f, 0.3f); // Sadida - Dark Green
                case 11: return new Color(0.5f, 0.2f, 0.2f); // Sacrieur - Dark Red
                case 12: return new Color(0.5f, 0.5f, 0.5f); // Pandawa - Gray
                default: return Color.gray;
            }
        }

        public string GetRoleDescription()
        {
            switch (id)
            {
                case 1: return "Tank/Support";
                case 2: return "Summoner";
                case 3: return "Support/Loot";
                case 4: return "Assassin";
                case 5: return "Control/AP Manipulation";
                case 6: return "Hybrid/Luck";
                case 7: return "Healer";
                case 8: return "Melee DPS";
                case 9: return "Ranged DPS";
                case 10: return "Summoner/Support";
                case 11: return "Berserker";
                case 12: return "Brawler/Support";
                default: return "Unknown";
            }
        }

        public string GetElementFocus()
        {
            switch (id)
            {
                case 1: return "All Elements";
                case 2: return "All Elements";
                case 3: return "Earth/Chance";
                case 4: return "Air/Strength";
                case 5: return "Fire/Intelligence";
                case 6: return "Chance";
                case 7: return "Water/Intelligence";
                case 8: return "Strength";
                case 9: return "Agility";
                case 10: return "Intelligence/Chance";
                case 11: return "Strength";
                case 12: return "Strength/Chance";
                default: return "Unknown";
            }
        }
    }

    /// <summary>
    /// Stats gained per level for a class
    /// </summary>
    [Serializable]
    public class StatsPerLevel
    {
        public int vitality;
        public int wisdom;
        public int strength;
        public int intelligence;
        public int chance;
        public int agility;

        public override string ToString()
        {
            return $"Vit: {vitality}, Wis: {wisdom}, Str: {strength}, Int: {intelligence}, Cha: {chance}, Agi: {agility}";
        }
    }

    /// <summary>
    /// Response from /api/classes endpoint
    /// </summary>
    [Serializable]
    public class ClassListResponse
    {
        public ClassData[] classes;
        public int totalClasses;
    }
}