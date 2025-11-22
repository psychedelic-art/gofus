using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOFUS.Models
{
    /// <summary>
    /// Represents a spell in GOFUS
    /// </summary>
    [Serializable]
    public class SpellData
    {
        public int id;
        public string name;
        public string description;
        public int level;
        public int apCost;
        public string range;
        public List<SpellEffect> effects;
        public int criticalHitBonus;
        public int criticalHitChance;
        public int cooldown;

        // UI helpers
        public string GetFormattedRange()
        {
            if (string.IsNullOrEmpty(range)) return "Melee";
            if (range == "0") return "Self";
            if (range.Contains("-")) return $"Range: {range}";
            return $"Range: {range}";
        }

        public string GetFormattedAPCost()
        {
            return $"{apCost} AP";
        }

        public string GetFormattedCooldown()
        {
            if (cooldown == 0) return "No Cooldown";
            return $"{cooldown} Turn{(cooldown > 1 ? "s" : "")} Cooldown";
        }

        public Color GetElementColor()
        {
            if (effects == null || effects.Count == 0) return Color.gray;

            var firstEffect = effects[0];
            if (firstEffect.element != null)
            {
                switch (firstEffect.element.ToLower())
                {
                    case "fire": return new Color(1f, 0.4f, 0.2f);
                    case "water": return new Color(0.2f, 0.6f, 1f);
                    case "earth": return new Color(0.6f, 0.4f, 0.2f);
                    case "air": return new Color(0.8f, 0.8f, 0.4f);
                    case "neutral": return Color.gray;
                    default: return Color.white;
                }
            }

            return Color.white;
        }
    }

    /// <summary>
    /// Represents an effect of a spell
    /// </summary>
    [Serializable]
    public class SpellEffect
    {
        public string type; // damage, heal, buff, debuff, summon, etc.
        public string element; // fire, water, earth, air, neutral
        public int min;
        public int max;
        public int value;
        public int duration;
        public bool random;
        public string target; // self, enemy, ally, summons
        public string creature; // for summon effects
        public int hpPercent; // for resurrection
        public bool invisible; // for invisibility
        public int mpReduction;
        public int apReduction;
        public int apSteal;
        public int armorPierce;
        public int resistanceReduction;
        public int damageAmplification;
        public bool backstabOnly;
        public bool teleport;
        public int pull;
        public int push;
        public int pushbackSelf;
        public string area; // circle_2, circle_3, cross
        public bool block;
        public int lootBonus;
        public int prospecting;
        public bool cleanse;
        public bool revive;
        public bool freeze;
        public bool rewind;
        public bool swapPositions;
        public bool carry;
        public bool drunk;
        public int accuracyReduction;

        public string GetEffectDescription()
        {
            switch (type)
            {
                case "damage":
                    return $"Deals {min}-{max} {element ?? "neutral"} damage";
                case "heal":
                    return $"Heals {min}-{max} HP";
                case "shield":
                    return $"Shields for {value} damage";
                case "buff":
                    return $"Buffs {target ?? "self"} for {duration} turns";
                case "debuff":
                    return $"Debuffs enemy for {duration} turns";
                case "summon":
                    return $"Summons a {creature}";
                case "teleport":
                    return "Teleports to target location";
                case "steal":
                    return $"Steals {apSteal} AP";
                case "trap":
                    return $"Places a trap dealing {value} damage";
                case "glyph":
                    return $"Places a glyph dealing {value} damage";
                default:
                    return type;
            }
        }
    }
}