using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GOFUS.Combat.Advanced
{
    /// <summary>
    /// AI system for NPC combat behavior
    /// Uses different strategies based on behavior type
    /// </summary>
    public class CombatAI
    {
        private AIBehavior behavior;
        private float decisionDelay = 0.5f;
        private Dictionary<string, float> decisionWeights;

        public CombatAI(AIBehavior behavior)
        {
            this.behavior = behavior;
            InitializeWeights();
        }

        private void InitializeWeights()
        {
            decisionWeights = new Dictionary<string, float>();

            switch (behavior)
            {
                case AIBehavior.Aggressive:
                    decisionWeights["attack"] = 0.8f;
                    decisionWeights["defend"] = 0.1f;
                    decisionWeights["heal"] = 0.1f;
                    break;

                case AIBehavior.Defensive:
                    decisionWeights["attack"] = 0.3f;
                    decisionWeights["defend"] = 0.5f;
                    decisionWeights["heal"] = 0.2f;
                    break;

                case AIBehavior.Support:
                    decisionWeights["attack"] = 0.2f;
                    decisionWeights["defend"] = 0.3f;
                    decisionWeights["heal"] = 0.5f;
                    break;

                case AIBehavior.Balanced:
                default:
                    decisionWeights["attack"] = 0.4f;
                    decisionWeights["defend"] = 0.3f;
                    decisionWeights["heal"] = 0.3f;
                    break;
            }
        }

        public CombatAction SelectAction(CombatContext context)
        {
            if (context == null || context.Self == null)
                return null;

            // Evaluate situation
            var evaluation = EvaluateSituation(context);

            // Select action based on evaluation
            CombatAction action = null;

            switch (evaluation.RecommendedAction)
            {
                case AIActionType.Attack:
                    action = SelectAttackAction(context, evaluation);
                    break;

                case AIActionType.Heal:
                    action = SelectHealAction(context, evaluation);
                    break;

                case AIActionType.Defend:
                    action = SelectDefendAction(context, evaluation);
                    break;

                case AIActionType.Support:
                    action = SelectSupportAction(context, evaluation);
                    break;
            }

            return action ?? GetDefaultAction(context);
        }

        private SituationEvaluation EvaluateSituation(CombatContext context)
        {
            var eval = new SituationEvaluation();

            // Check health status
            float healthPercent = (float)context.Self.CurrentHealth / context.Self.MaxHealth;
            eval.HealthStatus = healthPercent;

            // Check mana status
            float manaPercent = (float)context.Self.CurrentMana / context.Self.MaxMana;
            eval.ManaStatus = manaPercent;

            // Count threats
            eval.ThreatLevel = CalculateThreatLevel(context);

            // Determine recommended action
            if (healthPercent < 0.3f && behavior == AIBehavior.Defensive)
            {
                eval.RecommendedAction = AIActionType.Heal;
            }
            else if (eval.ThreatLevel > 0.7f)
            {
                eval.RecommendedAction = behavior == AIBehavior.Aggressive ?
                    AIActionType.Attack : AIActionType.Defend;
            }
            else
            {
                // Use weighted random selection
                eval.RecommendedAction = SelectWeightedAction();
            }

            // Find priority target
            eval.PriorityTarget = FindPriorityTarget(context);

            return eval;
        }

        private float CalculateThreatLevel(CombatContext context)
        {
            if (context.Enemies == null || context.Enemies.Count == 0)
                return 0f;

            float totalThreat = 0;
            foreach (var enemy in context.Enemies)
            {
                // Base threat on enemy stats and distance
                float enemyThreat = (float)enemy.Strength / 20f;

                // Increase threat for low health enemies (finish them)
                float enemyHealthPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;
                if (enemyHealthPercent < 0.3f)
                {
                    enemyThreat *= 1.5f;
                }

                totalThreat += enemyThreat;
            }

            return Mathf.Clamp01(totalThreat / context.Enemies.Count);
        }

        private CombatEntity FindPriorityTarget(CombatContext context)
        {
            if (context.Enemies == null || context.Enemies.Count == 0)
                return null;

            // Different targeting based on behavior
            switch (behavior)
            {
                case AIBehavior.Aggressive:
                    // Target lowest health enemy
                    return context.Enemies
                        .Where(e => e.IsAlive)
                        .OrderBy(e => e.CurrentHealth)
                        .FirstOrDefault();

                case AIBehavior.Defensive:
                    // Target highest damage dealer
                    return context.Enemies
                        .Where(e => e.IsAlive)
                        .OrderByDescending(e => e.Strength)
                        .FirstOrDefault();

                case AIBehavior.Support:
                    // Target enemy attacking allies
                    return context.Enemies
                        .Where(e => e.IsAlive)
                        .FirstOrDefault();

                default:
                    // Target closest enemy
                    return context.Enemies
                        .Where(e => e.IsAlive)
                        .FirstOrDefault();
            }
        }

        private AIActionType SelectWeightedAction()
        {
            float random = UnityEngine.Random.value;
            float cumulative = 0;

            if (random < (cumulative += decisionWeights["attack"]))
                return AIActionType.Attack;

            if (random < (cumulative += decisionWeights["defend"]))
                return AIActionType.Defend;

            if (random < (cumulative += decisionWeights["heal"]))
                return AIActionType.Heal;

            return AIActionType.Attack; // Default
        }

        private CombatAction SelectAttackAction(CombatContext context, SituationEvaluation eval)
        {
            if (eval.PriorityTarget == null)
                return null;

            // Select best offensive skill
            var offensiveSkills = context.AvailableSkills
                .Where(s => s.Damage > 0 && context.Self.CurrentMana >= s.ManaCost)
                .OrderByDescending(s => CalculateSkillPriority(s, eval))
                .ToList();

            if (offensiveSkills.Count == 0)
                return GetBasicAttack(context, eval.PriorityTarget);

            var selectedSkill = offensiveSkills.First();

            return new CombatAction
            {
                Performer = context.Self,
                Target = eval.PriorityTarget,
                SkillId = selectedSkill.Id
            };
        }

        private CombatAction SelectHealAction(CombatContext context, SituationEvaluation eval)
        {
            // Find ally that needs healing (including self)
            CombatEntity healTarget = null;

            if (context.Self.CurrentHealth < context.Self.MaxHealth * 0.5f)
            {
                healTarget = context.Self;
            }
            else if (context.Allies != null)
            {
                healTarget = context.Allies
                    .Where(a => a.IsAlive && a.CurrentHealth < a.MaxHealth * 0.7f)
                    .OrderBy(a => a.CurrentHealth)
                    .FirstOrDefault();
            }

            if (healTarget == null)
                return SelectAttackAction(context, eval); // Fall back to attack

            // Select healing skill
            var healSkills = context.AvailableSkills
                .Where(s => s.Healing > 0 && context.Self.CurrentMana >= s.ManaCost)
                .OrderByDescending(s => s.Healing)
                .ToList();

            if (healSkills.Count == 0)
                return SelectAttackAction(context, eval);

            return new CombatAction
            {
                Performer = context.Self,
                Target = healTarget,
                SkillId = healSkills.First().Id
            };
        }

        private CombatAction SelectDefendAction(CombatContext context, SituationEvaluation eval)
        {
            // Look for defensive buffs
            var defensiveSkills = context.AvailableSkills
                .Where(s => s.StatusEffect != null &&
                           s.StatusEffect.Type == StatusEffectType.Buff &&
                           context.Self.CurrentMana >= s.ManaCost)
                .ToList();

            if (defensiveSkills.Count > 0)
            {
                return new CombatAction
                {
                    Performer = context.Self,
                    Target = context.Self,
                    SkillId = defensiveSkills.First().Id
                };
            }

            // Fall back to attack weakest enemy
            return SelectAttackAction(context, eval);
        }

        private CombatAction SelectSupportAction(CombatContext context, SituationEvaluation eval)
        {
            // Buff allies or debuff enemies
            var supportSkills = context.AvailableSkills
                .Where(s => s.StatusEffect != null && context.Self.CurrentMana >= s.ManaCost)
                .ToList();

            if (supportSkills.Count > 0)
            {
                var skill = supportSkills.First();
                CombatEntity target = skill.StatusEffect.Type == StatusEffectType.Buff ?
                    SelectAllyForBuff(context) :
                    eval.PriorityTarget;

                if (target != null)
                {
                    return new CombatAction
                    {
                        Performer = context.Self,
                        Target = target,
                        SkillId = skill.Id
                    };
                }
            }

            return SelectHealAction(context, eval);
        }

        private CombatEntity SelectAllyForBuff(CombatContext context)
        {
            if (context.Allies == null || context.Allies.Count == 0)
                return context.Self;

            // Buff strongest ally
            return context.Allies
                .Where(a => a.IsAlive)
                .OrderByDescending(a => a.Strength + a.Intelligence)
                .FirstOrDefault() ?? context.Self;
        }

        private float CalculateSkillPriority(Skill skill, SituationEvaluation eval)
        {
            float priority = skill.Damage;

            // Adjust for mana efficiency
            if (eval.ManaStatus < 0.3f)
            {
                priority /= (skill.ManaCost + 1);
            }

            // Prefer elemental damage if target has weakness
            if (skill.Element != Element.None)
            {
                priority *= 1.2f;
            }

            // Prefer skills with additional effects
            if (skill.StatusEffect != null)
            {
                priority *= 1.3f;
            }

            return priority;
        }

        private CombatAction GetBasicAttack(CombatContext context, CombatEntity target)
        {
            // Find basic attack skill (usually ID 1)
            var basicAttack = context.AvailableSkills.FirstOrDefault(s => s.ManaCost == 0);

            if (basicAttack == null)
                return null;

            return new CombatAction
            {
                Performer = context.Self,
                Target = target,
                SkillId = basicAttack.Id
            };
        }

        private CombatAction GetDefaultAction(CombatContext context)
        {
            // Last resort - basic attack on first enemy
            var target = context.Enemies?.FirstOrDefault(e => e.IsAlive);
            if (target == null)
                return null;

            return GetBasicAttack(context, target);
        }

        // Helper classes
        private class SituationEvaluation
        {
            public float HealthStatus;
            public float ManaStatus;
            public float ThreatLevel;
            public AIActionType RecommendedAction;
            public CombatEntity PriorityTarget;
        }

        private enum AIActionType
        {
            Attack,
            Defend,
            Heal,
            Support
        }
    }

    /// <summary>
    /// AI behavior patterns
    /// </summary>
    public enum AIBehavior
    {
        Aggressive,
        Defensive,
        Support,
        Balanced,
        Berserker,
        Tactical
    }

    /// <summary>
    /// Context for AI decision making
    /// </summary>
    public class CombatContext
    {
        public CombatEntity Self;
        public List<CombatEntity> Allies;
        public List<CombatEntity> Enemies;
        public List<Skill> AvailableSkills;
        public int TurnNumber;
        public float CombatDuration;
    }
}