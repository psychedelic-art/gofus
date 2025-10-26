using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GOFUS.Core;
using GOFUS.Entities;

namespace GOFUS.Combat.Advanced
{
    /// <summary>
    /// Enhanced combat manager with stack-based state machine
    /// Supports advanced features like combos, status effects, and AI
    /// </summary>
    public class AdvancedCombatManager : Singleton<AdvancedCombatManager>
    {
        [Header("State Management")]
        private Stack<ICombatState> stateStack;
        private ICombatState currentState;

        [Header("Combat Entities")]
        private TurnQueue turnQueue;
        private List<CombatEntity> allCombatants;
        private Dictionary<CombatEntity, List<StatusEffect>> statusEffects;

        [Header("Systems")]
        private ComboSystem comboSystem;
        public SkillSystem skillSystem { get; private set; }
        private DamageCalculator damageCalculator;
        private ThreatSystem threatSystem;
        private CombatLog combatLog;

        [Header("Combat Settings")]
        [SerializeField] private float turnTimeout = 30f;
        [SerializeField] private float comboWindowTime = 1f;
        [SerializeField] private bool allowInterrupts = true;

        // Properties
        public ICombatState CurrentState => currentState;
        public CombatLog CombatLog => combatLog;
        public bool IsInCombat { get; private set; }
        public int TurnNumber { get; private set; }

        // Events
        public event Action<CombatEntity> OnTurnStart;
        public event Action<CombatEntity> OnTurnEnd;
        public event Action<CombatEntity, CombatEntity, int> OnDamageDealt;
        public event Action<CombatEntity> OnCombatantDefeated;
        public event Action OnCombatStart;
        public event Action OnCombatEnd;

        public void Initialize()
        {
            stateStack = new Stack<ICombatState>();
            turnQueue = new TurnQueue();
            allCombatants = new List<CombatEntity>();
            statusEffects = new Dictionary<CombatEntity, List<StatusEffect>>();

            comboSystem = new ComboSystem();
            skillSystem = new SkillSystem();
            damageCalculator = new DamageCalculator();
            threatSystem = new ThreatSystem();
            combatLog = new CombatLog();

            // Push initial idle state
            PushState(new CombatIdleState());
        }

        public void PushState(ICombatState newState)
        {
            if (currentState != null)
            {
                currentState.OnExit();
            }

            stateStack.Push(newState);
            currentState = newState;
            currentState.OnEnter(this);
        }

        public void PopState()
        {
            if (stateStack.Count > 1)
            {
                currentState.OnExit();
                stateStack.Pop();
                currentState = stateStack.Peek();
                currentState.OnEnter(this);
            }
        }

        public void StartCombat(List<CombatEntity> playerTeam, List<CombatEntity> enemyTeam)
        {
            IsInCombat = true;
            TurnNumber = 0;

            // Add all combatants
            allCombatants.Clear();
            allCombatants.AddRange(playerTeam);
            allCombatants.AddRange(enemyTeam);

            // Initialize turn queue
            turnQueue.Clear();
            foreach (var combatant in allCombatants)
            {
                turnQueue.AddFighter(combatant);
                statusEffects[combatant] = new List<StatusEffect>();
            }

            OnCombatStart?.Invoke();
            StartCoroutine(CombatLoop());
        }

        private IEnumerator CombatLoop()
        {
            while (IsInCombat)
            {
                // Get next fighter
                var currentFighter = turnQueue.GetNextFighter();
                if (currentFighter == null || !currentFighter.IsAlive)
                {
                    CheckCombatEnd();
                    yield return null;
                    continue;
                }

                // Start turn
                TurnNumber++;
                OnTurnStart?.Invoke(currentFighter);

                // Process status effects
                ProcessStatusEffects(currentFighter);

                // Update state
                currentState?.OnUpdate();

                // Wait for action or timeout
                float turnTimer = 0;
                while (turnTimer < turnTimeout && !currentFighter.HasActed)
                {
                    yield return null;
                    turnTimer += Time.deltaTime;
                }

                // End turn
                currentFighter.HasActed = false;
                OnTurnEnd?.Invoke(currentFighter);

                // Re-add to queue
                turnQueue.AddFighter(currentFighter);

                yield return new WaitForSeconds(0.5f);
            }
        }

        public void ProcessTurn()
        {
            // Process one turn synchronously (for testing)
            var currentFighter = turnQueue.GetNextFighter();
            if (currentFighter != null && currentFighter.IsAlive)
            {
                ProcessStatusEffects(currentFighter);
                turnQueue.AddFighter(currentFighter);
            }
        }

        public void ApplyStatusEffect(CombatEntity target, StatusEffect effect)
        {
            if (!statusEffects.ContainsKey(target))
            {
                statusEffects[target] = new List<StatusEffect>();
            }

            // Check if effect already exists
            var existing = statusEffects[target].FirstOrDefault(e => e.Name == effect.Name);
            if (existing != null)
            {
                // Refresh duration
                existing.Duration = Mathf.Max(existing.Duration, effect.Duration);
            }
            else
            {
                statusEffects[target].Add(effect.Clone());
                ApplyStatModifiers(target, effect.StatModifier);
            }

            combatLog.LogStatusEffect(target, effect.Name);
        }

        public bool HasStatusEffect(CombatEntity target, string effectName)
        {
            if (!statusEffects.ContainsKey(target))
                return false;

            return statusEffects[target].Any(e => e.Name == effectName);
        }

        public List<StatusEffect> GetStatusEffects(CombatEntity target)
        {
            return statusEffects.ContainsKey(target) ?
                new List<StatusEffect>(statusEffects[target]) :
                new List<StatusEffect>();
        }

        private void ProcessStatusEffects(CombatEntity entity)
        {
            if (!statusEffects.ContainsKey(entity))
                return;

            var effects = statusEffects[entity];
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];

                // Apply damage/healing
                if (effect.DamagePerTurn > 0)
                {
                    entity.TakeDamage(effect.DamagePerTurn);
                    combatLog.LogDamage(null, entity, effect.DamagePerTurn, false);
                }

                if (effect.HealingPerTurn > 0)
                {
                    entity.Heal(effect.HealingPerTurn);
                }

                // Reduce duration
                effect.Duration--;

                // Remove if expired
                if (effect.Duration <= 0)
                {
                    RemoveStatModifiers(entity, effect.StatModifier);
                    effects.RemoveAt(i);
                }
            }
        }

        private void ApplyStatModifiers(CombatEntity entity, StatModifier modifier)
        {
            if (modifier == null) return;

            entity.Strength += modifier.Strength;
            entity.Intelligence += modifier.Intelligence;
            entity.Defense += modifier.Defense;
            entity.Speed += modifier.Speed;
        }

        private void RemoveStatModifiers(CombatEntity entity, StatModifier modifier)
        {
            if (modifier == null) return;

            entity.Strength -= modifier.Strength;
            entity.Intelligence -= modifier.Intelligence;
            entity.Defense -= modifier.Defense;
            entity.Speed -= modifier.Speed;
        }

        public void ExecuteAction(CombatAction action)
        {
            if (action == null || action.Performer == null || action.Target == null)
                return;

            var skill = skillSystem.GetSkill(action.SkillId);
            if (skill == null)
                return;

            // Check if skill can be used
            if (!skillSystem.CanUseSkill(action.Performer, skill.Id))
                return;

            // Use skill
            skillSystem.UseSkill(action.Performer, skill.Id);

            // Calculate and apply damage
            if (skill.Damage > 0)
            {
                var damageResult = damageCalculator.CalculateDamage(
                    action.Performer,
                    action.Target,
                    skill
                );

                action.Target.TakeDamage(damageResult.Amount);
                OnDamageDealt?.Invoke(action.Performer, action.Target, damageResult.Amount);

                // Generate threat
                threatSystem.AddThreat(action.Target, action.Performer, damageResult.Amount);

                // Log
                combatLog.LogAttack(action.Performer, action.Target, damageResult.Amount);

                // Check for combo
                if (comboSystem.RegisterAttack(
                    action.Performer,
                    skill.Name,
                    Time.time))
                {
                    int comboCount = comboSystem.GetComboCount(action.Performer);
                    if (comboCount > 1)
                    {
                        combatLog.LogCombo(action.Performer, comboCount);
                    }
                }
            }

            // Apply healing
            if (skill.Healing > 0)
            {
                action.Target.Heal(skill.Healing);
                combatLog.LogHealing(action.Performer, action.Target, skill.Healing);
            }

            // Apply status effects
            if (skill.StatusEffect != null)
            {
                ApplyStatusEffect(action.Target, skill.StatusEffect);
            }

            // Check if target was defeated
            if (!action.Target.IsAlive)
            {
                OnCombatantDefeated?.Invoke(action.Target);
                combatLog.LogDefeat(action.Target);
                turnQueue.RemoveFighter(action.Target);
                CheckCombatEnd();
            }

            action.Performer.HasActed = true;
        }

        private void CheckCombatEnd()
        {
            var alivePlayers = allCombatants.Where(c => c.Team == Team.Player && c.IsAlive).ToList();
            var aliveEnemies = allCombatants.Where(c => c.Team == Team.Enemy && c.IsAlive).ToList();

            if (alivePlayers.Count == 0 || aliveEnemies.Count == 0)
            {
                EndCombat(alivePlayers.Count > 0);
            }
        }

        private void EndCombat(bool playerVictory)
        {
            IsInCombat = false;
            StopAllCoroutines();

            if (playerVictory)
            {
                combatLog.LogVictory();
            }
            else
            {
                combatLog.LogDefeat();
            }

            OnCombatEnd?.Invoke();

            // Clean up
            statusEffects.Clear();
            allCombatants.Clear();
            turnQueue.Clear();
        }

        public List<CombatEntity> GetAllFighters()
        {
            return new List<CombatEntity>(allCombatants);
        }
    }

    // Combat State Interface
    public interface ICombatState
    {
        void OnEnter(AdvancedCombatManager manager);
        void OnUpdate();
        void OnExit();
    }

    // Concrete States
    public class CombatIdleState : ICombatState
    {
        private AdvancedCombatManager manager;

        public void OnEnter(AdvancedCombatManager manager)
        {
            this.manager = manager;
        }

        public void OnUpdate() { }
        public void OnExit() { }
    }

    public class CombatAttackState : ICombatState
    {
        private AdvancedCombatManager manager;

        public void OnEnter(AdvancedCombatManager manager)
        {
            this.manager = manager;
        }

        public void OnUpdate() { }
        public void OnExit() { }
    }

    public class CombatCastState : ICombatState
    {
        private AdvancedCombatManager manager;

        public void OnEnter(AdvancedCombatManager manager)
        {
            this.manager = manager;
        }

        public void OnUpdate() { }
        public void OnExit() { }
    }
}