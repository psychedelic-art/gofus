using System;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Entities;

namespace GOFUS.Combat
{
    public enum SpellTargetType
    {
        Self,
        SingleTarget,
        Area,
        Line,
        Cone
    }

    [Serializable]
    public class Spell
    {
        public int Id;
        public string Name;
        public string Description;
        public int Level = 1;

        [Header("Targeting")]
        public SpellTargetType TargetType;
        public float Range = 5f;
        public float AreaRadius = 0f;
        public bool RequiresLineOfSight = true;

        [Header("Casting")]
        public float CastTime = 0f;
        public bool IsChanneling = false;
        public bool CanCastWhileMoving = false;
        public float ATBCost = 30f;
        public int ManaCost = 10;
        public int APCost = 3;

        [Header("Effects")]
        public int Damage = 0;
        public int Healing = 0;
        public ElementType Element = ElementType.Neutral;
        public List<StatusEffect> StatusEffects = new List<StatusEffect>();

        [Header("Cooldown")]
        public float Cooldown = 0f;
        public int MaxCharges = 1;

        [Header("Visual")]
        public GameObject EffectPrefab;
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 10f;
        public Color EffectColor = Color.white;
    }

    public static class SpellDatabase
    {
        private static Dictionary<int, Spell> spells;
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            spells = new Dictionary<int, Spell>();
            LoadSpells();
            isInitialized = true;
        }

        private static void LoadSpells()
        {
            // Basic Attack (ID: 1)
            spells[1] = new Spell
            {
                Id = 1,
                Name = "Basic Attack",
                Description = "A simple physical attack",
                TargetType = SpellTargetType.SingleTarget,
                Range = 1.5f,
                CastTime = 0f,
                ATBCost = 25f,
                APCost = 3,
                ManaCost = 0,
                Damage = 20,
                Element = ElementType.Neutral,
                Cooldown = 0f
            };

            // Fireball (ID: 2)
            spells[2] = new Spell
            {
                Id = 2,
                Name = "Fireball",
                Description = "Launches a fiery projectile at the target",
                TargetType = SpellTargetType.SingleTarget,
                Range = 8f,
                CastTime = 1.5f,
                CanCastWhileMoving = false,
                ATBCost = 40f,
                APCost = 4,
                ManaCost = 15,
                Damage = 35,
                Element = ElementType.Fire,
                Cooldown = 3f,
                RequiresLineOfSight = true
            };

            // Frost Nova (ID: 3)
            spells[3] = new Spell
            {
                Id = 3,
                Name = "Frost Nova",
                Description = "Freezes all enemies around you",
                TargetType = SpellTargetType.Area,
                Range = 0f,
                AreaRadius = 3f,
                CastTime = 0f,
                CanCastWhileMoving = true,
                ATBCost = 50f,
                APCost = 5,
                ManaCost = 20,
                Damage = 25,
                Element = ElementType.Water,
                Cooldown = 8f,
                StatusEffects = new List<StatusEffect>
                {
                    new StatusEffect
                    {
                        Id = 1,
                        Name = "Frozen",
                        Duration = 2f,
                        DefenseModifier = -10
                    }
                }
            };

            // Lightning Strike (ID: 4) - Instant cast spell for real-time mode
            spells[4] = new Spell
            {
                Id = 4,
                Name = "Lightning Strike",
                Description = "Instant lightning damage with chain potential",
                TargetType = SpellTargetType.SingleTarget,
                Range = 10f,
                CastTime = 0f, // Instant cast
                CanCastWhileMoving = true,
                ATBCost = 35f,
                APCost = 3,
                ManaCost = 12,
                Damage = 30,
                Element = ElementType.Air,
                Cooldown = 2f,
                RequiresLineOfSight = false
            };

            // Heal (ID: 5)
            spells[5] = new Spell
            {
                Id = 5,
                Name = "Heal",
                Description = "Restores health to target ally",
                TargetType = SpellTargetType.SingleTarget,
                Range = 6f,
                CastTime = 2f,
                CanCastWhileMoving = false,
                ATBCost = 30f,
                APCost = 3,
                ManaCost = 15,
                Healing = 40,
                Element = ElementType.Light,
                Cooldown = 1f
            };

            // Meteor Strike (ID: 6) - Area channeled spell
            spells[6] = new Spell
            {
                Id = 6,
                Name = "Meteor Strike",
                Description = "Calls down a meteor at target location",
                TargetType = SpellTargetType.Area,
                Range = 12f,
                AreaRadius = 4f,
                CastTime = 3f,
                IsChanneling = true,
                CanCastWhileMoving = false,
                ATBCost = 75f,
                APCost = 6,
                ManaCost = 40,
                Damage = 80,
                Element = ElementType.Fire,
                Cooldown = 20f,
                RequiresLineOfSight = false
            };

            // Arcane Missiles (ID: 7) - Channeled spell for real-time
            spells[7] = new Spell
            {
                Id = 7,
                Name = "Arcane Missiles",
                Description = "Channels missiles at the target",
                TargetType = SpellTargetType.SingleTarget,
                Range = 8f,
                CastTime = 3f,
                IsChanneling = true,
                CanCastWhileMoving = false,
                ATBCost = 20f,
                APCost = 4,
                ManaCost = 25,
                Damage = 50,
                Element = ElementType.Neutral,
                Cooldown = 0f
            };

            // Blink (ID: 8) - Instant movement spell
            spells[8] = new Spell
            {
                Id = 8,
                Name = "Blink",
                Description = "Instantly teleport forward",
                TargetType = SpellTargetType.Self,
                Range = 5f,
                CastTime = 0f,
                CanCastWhileMoving = true,
                ATBCost = 20f,
                APCost = 2,
                ManaCost = 10,
                Cooldown = 10f
            };

            // Shield (ID: 9)
            spells[9] = new Spell
            {
                Id = 9,
                Name = "Shield",
                Description = "Creates a protective barrier",
                TargetType = SpellTargetType.Self,
                CastTime = 0.5f,
                CanCastWhileMoving = true,
                ATBCost = 25f,
                APCost = 2,
                ManaCost = 15,
                Cooldown = 15f,
                StatusEffects = new List<StatusEffect>
                {
                    new StatusEffect
                    {
                        Id = 2,
                        Name = "Shielded",
                        Duration = 10f,
                        DefenseModifier = 20,
                        ElementResistances = new Dictionary<ElementType, float>
                        {
                            { ElementType.Fire, 25f },
                            { ElementType.Water, 25f },
                            { ElementType.Earth, 25f },
                            { ElementType.Air, 25f }
                        }
                    }
                }
            };

            // Poison Cloud (ID: 10) - Area DoT
            spells[10] = new Spell
            {
                Id = 10,
                Name = "Poison Cloud",
                Description = "Creates a toxic cloud at target location",
                TargetType = SpellTargetType.Area,
                Range = 8f,
                AreaRadius = 3f,
                CastTime = 1f,
                CanCastWhileMoving = false,
                ATBCost = 45f,
                APCost = 4,
                ManaCost = 20,
                Damage = 15,
                Element = ElementType.Earth,
                Cooldown = 12f,
                StatusEffects = new List<StatusEffect>
                {
                    new StatusEffect
                    {
                        Id = 3,
                        Name = "Poisoned",
                        Duration = 8f,
                        DefenseModifier = -5
                    }
                }
            };
        }

        public static Spell GetSpell(int id)
        {
            if (!isInitialized)
                Initialize();

            return spells.ContainsKey(id) ? spells[id] : null;
        }

        public static List<Spell> GetAllSpells()
        {
            if (!isInitialized)
                Initialize();

            return new List<Spell>(spells.Values);
        }

        public static List<Spell> GetSpellsForClass(string className)
        {
            // Return class-specific spells
            List<Spell> classSpells = new List<Spell>();

            switch (className.ToLower())
            {
                case "mage":
                    classSpells.Add(GetSpell(2)); // Fireball
                    classSpells.Add(GetSpell(3)); // Frost Nova
                    classSpells.Add(GetSpell(6)); // Meteor Strike
                    classSpells.Add(GetSpell(7)); // Arcane Missiles
                    classSpells.Add(GetSpell(8)); // Blink
                    break;

                case "cleric":
                    classSpells.Add(GetSpell(5)); // Heal
                    classSpells.Add(GetSpell(9)); // Shield
                    classSpells.Add(GetSpell(4)); // Lightning Strike
                    break;

                case "rogue":
                    classSpells.Add(GetSpell(1)); // Basic Attack
                    classSpells.Add(GetSpell(10)); // Poison Cloud
                    classSpells.Add(GetSpell(8)); // Blink
                    break;

                default:
                    classSpells.Add(GetSpell(1)); // Basic Attack
                    break;
            }

            return classSpells;
        }
    }

    public class SpellCaster : MonoBehaviour
    {
        private Fighter fighter;
        private HybridCombatManager combatManager;

        private void Awake()
        {
            fighter = GetComponent<Fighter>();
            combatManager = HybridCombatManager.Instance;
        }

        public bool CastSpell(int spellId, Fighter target = null, Vector3? targetPosition = null)
        {
            if (combatManager == null) return false;

            var spell = SpellDatabase.GetSpell(spellId);
            if (spell == null) return false;

            // Check if fighter knows the spell
            if (!fighter.KnownSpells.Contains(spellId)) return false;

            // Check mana
            if (fighter.Mana < spell.ManaCost) return false;

            // Determine target position
            Vector3 castTargetPos = targetPosition ?? (target != null ? target.Position : fighter.Position);

            // Check range
            if (spell.Range > 0 && Vector3.Distance(fighter.Position, castTargetPos) > spell.Range)
                return false;

            // Start cast
            bool success = combatManager.TryStartSpellCast(fighter, spellId, target, castTargetPos);

            if (success)
            {
                fighter.UseMana(spell.ManaCost);
            }

            return success;
        }

        public void CancelCast()
        {
            // Implement cast cancellation
        }
    }
}