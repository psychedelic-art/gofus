using System;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Combat;

namespace GOFUS.Entities
{
    public class Fighter : MonoBehaviour
    {
        [Header("Basic Stats")]
        public int Level = 1;
        public int Health;
        public int MaxHealth = 100;
        public int Mana;
        public int MaxMana = 50;

        [Header("Combat Stats")]
        public int ActionPoints;
        public int MaxActionPoints = 6;
        public int MovementPoints;
        public int MaxMovementPoints = 3;
        public int AttackDamage = 10;
        public float Speed = 100f;
        public float Initiative = 10f;
        public float SpellPower = 10f;
        public float CriticalChance = 5f;

        [Header("ATB System")]
        public float ATBGauge;

        [Header("Team")]
        public bool IsAI;
        public bool IsPlayerTeam;

        [Header("Spells")]
        public List<int> KnownSpells = new List<int>();

        [Header("Status")]
        private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();
        private bool isMoving;

        // Properties
        public bool IsAlive => Health > 0;
        public Vector3 Position => transform.position;
        public int ExperienceValue = 100;
        public bool IsMoving => isMoving;

        // Events
        public event Action<int> OnHealthChanged;
        public event Action<int> OnManaChanged;
        public event Action<StatusEffect> OnStatusEffectApplied;
        public event Action<StatusEffect> OnStatusEffectRemoved;
        public event Action OnDeath;

        private void Awake()
        {
            Health = MaxHealth;
            Mana = MaxMana;
            ActionPoints = MaxActionPoints;
            MovementPoints = MaxMovementPoints;
        }

        public void TakeDamage(int damage)
        {
            int actualDamage = Mathf.Max(0, damage - CalculateDefense());
            Health = Mathf.Max(0, Health - actualDamage);

            OnHealthChanged?.Invoke(Health);

            if (!IsAlive)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
            OnHealthChanged?.Invoke(Health);
        }

        public void RestoreMana(int amount)
        {
            Mana = Mathf.Min(MaxMana, Mana + amount);
            OnManaChanged?.Invoke(Mana);
        }

        public void UseMana(int amount)
        {
            Mana = Mathf.Max(0, Mana - amount);
            OnManaChanged?.Invoke(Mana);
        }

        public void MoveTo(int cellId)
        {
            StartCoroutine(MoveToCell(cellId));
        }

        private System.Collections.IEnumerator MoveToCell(int cellId)
        {
            isMoving = true;

            // Calculate target position (would use IsometricHelper in real implementation)
            Vector3 targetPosition = new Vector3(cellId % 14 * 2, cellId / 14 * 2, 0);

            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Speed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
            isMoving = false;
        }

        public void ApplyStatusEffect(StatusEffect effect)
        {
            var existingEffect = activeStatusEffects.Find(e => e.Id == effect.Id);

            if (existingEffect != null)
            {
                // Refresh duration
                existingEffect.Duration = effect.Duration;
            }
            else
            {
                activeStatusEffects.Add(effect);
                effect.Apply(this);
                OnStatusEffectApplied?.Invoke(effect);
            }
        }

        public void RemoveStatusEffect(StatusEffect effect)
        {
            if (activeStatusEffects.Remove(effect))
            {
                effect.Remove(this);
                OnStatusEffectRemoved?.Invoke(effect);
            }
        }

        public float GetResistance(ElementType element)
        {
            float baseResistance = 0f;

            // Calculate from equipment and buffs
            foreach (var effect in activeStatusEffects)
            {
                if (effect.ElementResistances.ContainsKey(element))
                {
                    baseResistance += effect.ElementResistances[element];
                }
            }

            return Mathf.Clamp(baseResistance, -100f, 95f);
        }

        private int CalculateDefense()
        {
            int defense = 0;

            // Base defense from level
            defense += Level * 2;

            // Defense from status effects
            foreach (var effect in activeStatusEffects)
            {
                defense += effect.DefenseModifier;
            }

            return defense;
        }

        private void Update()
        {
            // Update status effects
            for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeStatusEffects[i];
                effect.Duration -= Time.deltaTime;

                if (effect.Duration <= 0)
                {
                    RemoveStatusEffect(effect);
                }
            }
        }

        private void Die()
        {
            OnDeath?.Invoke();
            // Play death animation
            // Disable controls
            gameObject.SetActive(false);
        }

        public List<Item> GenerateLoot()
        {
            List<Item> loot = new List<Item>();

            // Random loot generation
            if (UnityEngine.Random.Range(0f, 1f) < 0.3f)
            {
                loot.Add(new Item { Name = "Health Potion", Value = 50 });
            }

            if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
            {
                loot.Add(new Item { Name = "Mana Potion", Value = 75 });
            }

            return loot;
        }
    }

    public class Enemy : Fighter
    {
        [Header("Enemy Specific")]
        public float AggroRange = 10f;
        public float AttackRange = 2f;
        public GameObject[] LootTable;
    }

    [Serializable]
    public class StatusEffect
    {
        public int Id;
        public string Name;
        public float Duration;
        public int DefenseModifier;
        public Dictionary<ElementType, float> ElementResistances = new Dictionary<ElementType, float>();

        public void Apply(Fighter target)
        {
            // Apply initial effect
        }

        public void Remove(Fighter target)
        {
            // Remove effect
        }
    }
}