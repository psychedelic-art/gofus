using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOFUS.Combat.Advanced
{
    /// <summary>
    /// Enhanced combat entity with advanced stats and capabilities
    /// </summary>
    public class CombatEntity : MonoBehaviour
    {
        [Header("Identity")]
        public string Name;
        public Team Team;
        public CombatRole Role;

        [Header("Core Stats")]
        public int MaxHealth = 100;
        public int CurrentHealth = 100;
        public int MaxMana = 50;
        public int CurrentMana = 50;

        [Header("Attributes")]
        public int Strength = 10;
        public int Intelligence = 10;
        public int Defense = 5;
        public int Speed = 10;
        public float CriticalChance = 10f;
        public int Initiative = 10;

        [Header("Resistances")]
        public Dictionary<Element, float> ElementalResistances;

        [Header("State")]
        public bool IsAlive => CurrentHealth > 0;
        public bool HasActed { get; set; }
        public bool IsStunned { get; set; }
        public bool IsSilenced { get; set; }

        // Events
        public event Action<int> OnHealthChanged;
        public event Action<int> OnManaChanged;
        public event Action OnDeath;

        private void Awake()
        {
            ElementalResistances = new Dictionary<Element, float>
            {
                { Element.Fire, 0 },
                { Element.Water, 0 },
                { Element.Earth, 0 },
                { Element.Air, 0 },
                { Element.Light, 0 },
                { Element.Dark, 0 }
            };
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            OnHealthChanged?.Invoke(CurrentHealth);

            if (!IsAlive)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        public void UseMana(int amount)
        {
            CurrentMana = Mathf.Max(0, CurrentMana - amount);
            OnManaChanged?.Invoke(CurrentMana);
        }

        public void RestoreMana(int amount)
        {
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
            OnManaChanged?.Invoke(CurrentMana);
        }

        public void ResetForCombat()
        {
            HasActed = false;
            IsStunned = false;
            IsSilenced = false;
        }
    }

    /// <summary>
    /// Team designation for combat
    /// </summary>
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }

    /// <summary>
    /// Combat role determines AI behavior and priorities
    /// </summary>
    public enum CombatRole
    {
        Tank,
        DPS,
        Healer,
        Support,
        Hybrid
    }

    /// <summary>
    /// Elemental types for damage and resistance
    /// </summary>
    public enum Element
    {
        None,
        Fire,
        Water,
        Earth,
        Air,
        Light,
        Dark
    }

    /// <summary>
    /// Skill/Ability definition
    /// </summary>
    [Serializable]
    public class Skill
    {
        public int Id;
        public string Name;
        public string Description;
        public int Damage;
        public int Healing;
        public int ManaCost;
        public float CastTime;
        public float Cooldown;
        public Element Element = Element.None;
        public StatusEffect StatusEffect;
        public TargetType TargetType = TargetType.Single;
        public int Range = 1;
    }

    /// <summary>
    /// Status effect that can be applied to entities
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public string Name;
        public StatusEffectType Type;
        public int Duration; // in turns
        public int DamagePerTurn;
        public int HealingPerTurn;
        public StatModifier StatModifier;
        public bool CanStack;

        public StatusEffect Clone()
        {
            return new StatusEffect
            {
                Name = Name,
                Type = Type,
                Duration = Duration,
                DamagePerTurn = DamagePerTurn,
                HealingPerTurn = HealingPerTurn,
                StatModifier = StatModifier?.Clone(),
                CanStack = CanStack
            };
        }
    }

    /// <summary>
    /// Type of status effect
    /// </summary>
    public enum StatusEffectType
    {
        Buff,
        Debuff,
        DoT, // Damage over Time
        HoT, // Heal over Time
        Control // Stun, Silence, etc.
    }

    /// <summary>
    /// Stat modifications from effects
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        public int Strength;
        public int Intelligence;
        public int Defense;
        public int Speed;

        public StatModifier Clone()
        {
            return new StatModifier
            {
                Strength = Strength,
                Intelligence = Intelligence,
                Defense = Defense,
                Speed = Speed
            };
        }
    }

    /// <summary>
    /// Target type for abilities
    /// </summary>
    public enum TargetType
    {
        Self,
        Single,
        AllEnemies,
        AllAllies,
        Area
    }

    /// <summary>
    /// Result of damage calculation
    /// </summary>
    public class DamageResult
    {
        public int Amount;
        public bool IsCritical;
        public Element Element;
    }

    /// <summary>
    /// Combat action to be executed
    /// </summary>
    public class CombatAction
    {
        public CombatEntity Performer;
        public CombatEntity Target;
        public int SkillId;
        public Vector3 TargetPosition;
    }

    /// <summary>
    /// Combat event for logging
    /// </summary>
    public class CombatEvent
    {
        public CombatEventType Type;
        public string Message;
        public float Timestamp;
    }

    /// <summary>
    /// Type of combat event
    /// </summary>
    public enum CombatEventType
    {
        Attack,
        Damage,
        Healing,
        StatusApplied,
        StatusRemoved,
        Combo,
        Defeat,
        Victory
    }
}