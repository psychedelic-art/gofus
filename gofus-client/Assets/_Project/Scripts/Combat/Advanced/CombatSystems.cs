using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GOFUS.Combat.Advanced
{
    /// <summary>
    /// Priority-based turn queue for combat order
    /// </summary>
    public class TurnQueue
    {
        private SortedList<float, CombatEntity> queue;
        private float timeCounter;

        public TurnQueue()
        {
            queue = new SortedList<float, CombatEntity>(new DescendingComparer());
            timeCounter = 0;
        }

        public void AddFighter(CombatEntity fighter)
        {
            if (fighter == null || !fighter.IsAlive) return;

            float priority = fighter.Initiative + UnityEngine.Random.Range(0f, 1f);

            // Ensure unique key
            while (queue.ContainsKey(priority))
            {
                priority += 0.001f;
            }

            queue.Add(priority, fighter);
        }

        public CombatEntity GetNextFighter()
        {
            if (queue.Count == 0) return null;

            var first = queue.First();
            queue.RemoveAt(0);
            return first.Value;
        }

        public void RemoveFighter(CombatEntity fighter)
        {
            var toRemove = queue.Where(kvp => kvp.Value == fighter).Select(kvp => kvp.Key).ToList();
            foreach (var key in toRemove)
            {
                queue.Remove(key);
            }
        }

        public void Clear()
        {
            queue.Clear();
        }

        private class DescendingComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                return y.CompareTo(x); // Descending order
            }
        }
    }

    /// <summary>
    /// Manages combo chains and timing
    /// </summary>
    public class ComboSystem
    {
        private Dictionary<CombatEntity, ComboChain> activeChains;
        private float comboWindow = 1f; // seconds

        public ComboSystem()
        {
            activeChains = new Dictionary<CombatEntity, ComboChain>();
        }

        public bool RegisterAttack(CombatEntity fighter, string attackName, float timestamp)
        {
            if (!activeChains.ContainsKey(fighter))
            {
                activeChains[fighter] = new ComboChain();
            }

            var chain = activeChains[fighter];

            // Check if within combo window
            if (chain.Attacks.Count > 0)
            {
                float timeSinceLastAttack = timestamp - chain.LastAttackTime;
                if (timeSinceLastAttack > comboWindow)
                {
                    // Reset combo
                    chain.Reset();
                    chain.AddAttack(attackName, timestamp);
                    return true;
                }
            }

            chain.AddAttack(attackName, timestamp);
            return chain.Attacks.Count > 1; // True if combo
        }

        public int GetComboCount(CombatEntity fighter)
        {
            return activeChains.ContainsKey(fighter) ?
                activeChains[fighter].Attacks.Count : 0;
        }

        public void ResetCombo(CombatEntity fighter)
        {
            if (activeChains.ContainsKey(fighter))
            {
                activeChains[fighter].Reset();
            }
        }

        private class ComboChain
        {
            public List<string> Attacks { get; private set; }
            public float LastAttackTime { get; private set; }

            public ComboChain()
            {
                Attacks = new List<string>();
            }

            public void AddAttack(string attack, float timestamp)
            {
                Attacks.Add(attack);
                LastAttackTime = timestamp;
            }

            public void Reset()
            {
                Attacks.Clear();
                LastAttackTime = 0;
            }
        }
    }

    /// <summary>
    /// Manages skills and abilities
    /// </summary>
    public class SkillSystem
    {
        private Dictionary<CombatEntity, List<Skill>> learnedSkills;
        private Dictionary<int, Skill> skillDatabase;
        private Dictionary<string, float> cooldowns; // Key: fighter_skillId

        public SkillSystem()
        {
            learnedSkills = new Dictionary<CombatEntity, List<Skill>>();
            skillDatabase = new Dictionary<int, Skill>();
            cooldowns = new Dictionary<string, float>();
            InitializeSkillDatabase();
        }

        private void InitializeSkillDatabase()
        {
            // Add basic skills
            AddSkillToDatabase(new Skill
            {
                Id = 1,
                Name = "Basic Attack",
                Damage = 10,
                ManaCost = 0,
                Cooldown = 0
            });

            AddSkillToDatabase(new Skill
            {
                Id = 2,
                Name = "Fireball",
                Damage = 25,
                ManaCost = 10,
                CastTime = 1.5f,
                Cooldown = 3f,
                Element = Element.Fire
            });

            AddSkillToDatabase(new Skill
            {
                Id = 3,
                Name = "Heal",
                Healing = 30,
                ManaCost = 15,
                CastTime = 2f,
                Cooldown = 5f
            });
        }

        public void AddSkillToDatabase(Skill skill)
        {
            skillDatabase[skill.Id] = skill;
        }

        public Skill GetSkill(int skillId)
        {
            return skillDatabase.ContainsKey(skillId) ? skillDatabase[skillId] : null;
        }

        public bool LearnSkill(CombatEntity fighter, Skill skill)
        {
            if (!learnedSkills.ContainsKey(fighter))
            {
                learnedSkills[fighter] = new List<Skill>();
            }

            if (!HasSkill(fighter, skill.Id))
            {
                learnedSkills[fighter].Add(skill);
                return true;
            }

            return false;
        }

        public bool HasSkill(CombatEntity fighter, int skillId)
        {
            return learnedSkills.ContainsKey(fighter) &&
                   learnedSkills[fighter].Any(s => s.Id == skillId);
        }

        public List<Skill> GetAvailableSkills(CombatEntity fighter)
        {
            return learnedSkills.ContainsKey(fighter) ?
                new List<Skill>(learnedSkills[fighter]) :
                new List<Skill>();
        }

        public bool CanUseSkill(CombatEntity fighter, int skillId)
        {
            if (!HasSkill(fighter, skillId))
                return false;

            var skill = GetSkill(skillId);
            if (skill == null)
                return false;

            // Check mana
            if (fighter.CurrentMana < skill.ManaCost)
                return false;

            // Check cooldown
            string cooldownKey = $"{fighter.Name}_{skillId}";
            if (cooldowns.ContainsKey(cooldownKey) && cooldowns[cooldownKey] > Time.time)
                return false;

            return true;
        }

        public bool UseSkill(CombatEntity fighter, int skillId)
        {
            if (!CanUseSkill(fighter, skillId))
                return false;

            var skill = GetSkill(skillId);
            fighter.UseMana(skill.ManaCost);

            // Set cooldown
            if (skill.Cooldown > 0)
            {
                string cooldownKey = $"{fighter.Name}_{skillId}";
                cooldowns[cooldownKey] = Time.time + skill.Cooldown;
            }

            return true;
        }
    }

    /// <summary>
    /// Calculates damage with modifiers
    /// </summary>
    public class DamageCalculator
    {
        public DamageResult CalculateDamage(CombatEntity attacker, CombatEntity target, Skill skill)
        {
            int baseDamage = skill.Damage;

            // Add attacker's strength
            baseDamage += attacker.Strength;

            // Check for critical hit
            bool isCritical = UnityEngine.Random.Range(0f, 100f) < attacker.CriticalChance;
            if (isCritical)
            {
                baseDamage *= 2;
            }

            // Apply defense
            baseDamage = Mathf.Max(1, baseDamage - target.Defense);

            // Apply elemental resistance
            if (skill.Element != Element.None && target.ElementalResistances.ContainsKey(skill.Element))
            {
                float resistance = target.ElementalResistances[skill.Element];
                baseDamage = Mathf.RoundToInt(baseDamage * (1f - resistance / 100f));
            }

            return new DamageResult
            {
                Amount = baseDamage,
                IsCritical = isCritical,
                Element = skill.Element
            };
        }
    }

    /// <summary>
    /// Manages threat/aggro for AI targeting
    /// </summary>
    public class ThreatSystem
    {
        private Dictionary<CombatEntity, Dictionary<CombatEntity, float>> threatTable;

        public ThreatSystem()
        {
            threatTable = new Dictionary<CombatEntity, Dictionary<CombatEntity, float>>();
        }

        public void AddThreat(CombatEntity target, CombatEntity source, float amount)
        {
            if (!threatTable.ContainsKey(target))
            {
                threatTable[target] = new Dictionary<CombatEntity, float>();
            }

            if (!threatTable[target].ContainsKey(source))
            {
                threatTable[target][source] = 0;
            }

            threatTable[target][source] += amount;
        }

        public CombatEntity GetHighestThreatTarget(CombatEntity attacker)
        {
            if (!threatTable.ContainsKey(attacker) || threatTable[attacker].Count == 0)
                return null;

            return threatTable[attacker]
                .Where(kvp => kvp.Key.IsAlive)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
        }

        public void ClearThreat(CombatEntity entity)
        {
            if (threatTable.ContainsKey(entity))
            {
                threatTable[entity].Clear();
            }
        }
    }

    /// <summary>
    /// Logs combat events for display
    /// </summary>
    public class CombatLog
    {
        private List<CombatEvent> events;
        private int maxEvents = 100;

        public CombatLog()
        {
            events = new List<CombatEvent>();
        }

        public void LogAttack(CombatEntity attacker, CombatEntity target, int damage)
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Attack,
                Message = $"{attacker.Name} attacks {target.Name} for {damage} damage",
                Timestamp = Time.time
            });
        }

        public void LogDamage(CombatEntity source, CombatEntity target, int damage, bool isCritical)
        {
            string critText = isCritical ? " (Critical!)" : "";
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Damage,
                Message = $"{target.Name} takes {damage} damage{critText}",
                Timestamp = Time.time
            });
        }

        public void LogHealing(CombatEntity source, CombatEntity target, int amount)
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Healing,
                Message = $"{source.Name} heals {target.Name} for {amount}",
                Timestamp = Time.time
            });
        }

        public void LogStatusEffect(CombatEntity target, string effect)
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.StatusApplied,
                Message = $"{target.Name} is affected by {effect}",
                Timestamp = Time.time
            });
        }

        public void LogCombo(CombatEntity fighter, int comboCount)
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Combo,
                Message = $"{fighter.Name} performs a {comboCount}-hit combo!",
                Timestamp = Time.time
            });
        }

        public void LogDefeat(CombatEntity defeated)
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Defeat,
                Message = $"{defeated.Name} has been defeated",
                Timestamp = Time.time
            });
        }

        public void LogVictory()
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Victory,
                Message = "Victory!",
                Timestamp = Time.time
            });
        }

        public void LogDefeat()
        {
            AddEvent(new CombatEvent
            {
                Type = CombatEventType.Defeat,
                Message = "Defeat...",
                Timestamp = Time.time
            });
        }

        private void AddEvent(CombatEvent evt)
        {
            events.Add(evt);

            // Limit log size
            if (events.Count > maxEvents)
            {
                events.RemoveAt(0);
            }
        }

        public List<CombatEvent> GetRecentEvents(int count)
        {
            int startIndex = Mathf.Max(0, events.Count - count);
            return events.GetRange(startIndex, Mathf.Min(count, events.Count));
        }
    }
}