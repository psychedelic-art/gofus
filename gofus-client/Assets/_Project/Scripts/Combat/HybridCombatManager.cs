using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GOFUS.Core;
using GOFUS.Entities;

namespace GOFUS.Combat
{
    public enum CombatMode
    {
        TurnBased,
        RealTime,
        Transitioning
    }

    public enum ActionType
    {
        BasicAttack,
        Spell,
        Move,
        Item,
        Defend
    }

    [Serializable]
    public class CombatState
    {
        public CombatMode Mode;
        public List<Fighter> Fighters;
        public Dictionary<Fighter, float> ATBStates;
        public Dictionary<int, float> SpellCooldowns;
    }

    [Serializable]
    public class PredictedAction
    {
        public Fighter Fighter;
        public float TimeUntilAction;
        public string ActionType;
        public int SpellId;
    }

    [Serializable]
    public class CombatAction
    {
        public ActionType Type;
        public Fighter Target;
        public int TargetCell;
        public int SpellId;
        public float ATBCost = 50f;
        public float CastTime = 0f;
    }

    [Serializable]
    public class SpellCast
    {
        public Fighter Caster;
        public int SpellId;
        public Fighter Target;
        public Vector3 TargetPosition;
        public float CastTime;
        public float ElapsedTime;
        public bool IsChanneling;
        public bool CanMove;
    }

    public class HybridCombatManager : Singleton<HybridCombatManager>
    {
        // Stack Machine for state management
        private Stack<CombatState> stateStack;
        private CombatMode currentMode;
        private bool isInCombat;

        // Fighters
        private List<Fighter> allFighters;
        private List<Fighter> playerTeam;
        private List<Fighter> enemyTeam;
        private Fighter currentTurnFighter;

        // Turn-based specific
        private Queue<Fighter> turnQueue;
        private float turnTimer;
        private const float TURN_TIME_LIMIT = 30f;

        // Real-time specific
        private Dictionary<Fighter, float> atbGauges;
        private float globalTimeScale = 1f;

        // Real-time spell casting
        private Dictionary<Fighter, SpellCast> activeSpellCasts;
        private Dictionary<Fighter, Dictionary<int, float>> spellCooldowns;
        private Queue<SpellCast> spellQueue;
        private const float SPELL_QUEUE_WINDOW = 0.5f;

        // Timeline prediction
        private List<PredictedAction> actionTimeline;

        // Hybrid features
        private float modeSwitchCooldown = 0f;
        private const float MODE_SWITCH_DELAY = 2f;
        private bool autoRealTimeForWeak = true;

        // Properties
        public CombatMode CurrentMode => currentMode;
        public bool IsInCombat => isInCombat;
        public Fighter CurrentTurn => currentTurnFighter;

        // Events
        public event Action<CombatMode> OnModeChanged;
        public event Action<Fighter> OnTurnStarted;
        public event Action<CombatResult> OnCombatEnded;
        public event Action<Fighter, float> OnATBFilled;
        public event Action<Fighter, SpellCast> OnSpellCastStarted;
        public event Action<Fighter, SpellCast> OnSpellCastCompleted;
        public event Action<Fighter, SpellCast> OnSpellCastInterrupted;

        protected override void Awake()
        {
            base.Awake();
            stateStack = new Stack<CombatState>();
            allFighters = new List<Fighter>();
            atbGauges = new Dictionary<Fighter, float>();
            actionTimeline = new List<PredictedAction>();

            // Initialize spell casting systems
            activeSpellCasts = new Dictionary<Fighter, SpellCast>();
            spellCooldowns = new Dictionary<Fighter, Dictionary<int, float>>();
            spellQueue = new Queue<SpellCast>();
        }

        public void InitializeBattle(CombatMode mode, List<Fighter> players, List<Fighter> enemies)
        {
            isInCombat = true;
            currentMode = mode;

            playerTeam = players;
            enemyTeam = enemies;
            allFighters.Clear();
            allFighters.AddRange(players);
            allFighters.AddRange(enemies);

            // Initialize spell cooldowns for all fighters
            foreach (var fighter in allFighters)
            {
                spellCooldowns[fighter] = new Dictionary<int, float>();
            }

            // Check for auto real-time mode
            if (autoRealTimeForWeak && enemies.All(e => ShouldAutoRealTime(e)))
            {
                mode = CombatMode.RealTime;
                ShowNotification("Quick battle mode activated!");
            }

            // Initialize based on mode
            if (mode == CombatMode.TurnBased)
            {
                InitializeTurnBased();
            }
            else
            {
                InitializeRealTime();
            }

            // Push initial state
            stateStack.Clear();
            stateStack.Push(new CombatState
            {
                Mode = mode,
                Fighters = allFighters.ToList(),
                ATBStates = new Dictionary<Fighter, float>(atbGauges),
                SpellCooldowns = new Dictionary<int, float>()
            });

            OnModeChanged?.Invoke(mode);
        }

        public void SetMode(CombatMode mode)
        {
            currentMode = mode;
        }

        private void InitializeTurnBased()
        {
            turnQueue = new Queue<Fighter>(
                allFighters.OrderByDescending(f => f.Initiative + UnityEngine.Random.Range(0, 20))
            );

            if (turnQueue.Count > 0)
            {
                StartNextTurn();
            }
        }

        private void InitializeRealTime()
        {
            atbGauges.Clear();
            foreach (var fighter in allFighters)
            {
                atbGauges[fighter] = 0f;
            }
        }

        public void SwitchMode()
        {
            if (!isInCombat || currentMode == CombatMode.Transitioning) return;
            if (modeSwitchCooldown > 0) return;

            StartCoroutine(TransitionMode());
        }

        private IEnumerator TransitionMode()
        {
            var oldMode = currentMode;
            currentMode = CombatMode.Transitioning;

            // Cancel all active spell casts when switching modes
            CancelAllSpellCasts();

            // Visual transition
            ShowNotification("Switching combat mode...");
            yield return new WaitForSeconds(0.5f);

            var newMode = oldMode == CombatMode.TurnBased ? CombatMode.RealTime : CombatMode.TurnBased;

            // Save current state
            stateStack.Push(new CombatState
            {
                Mode = newMode,
                Fighters = allFighters.ToList(),
                ATBStates = new Dictionary<Fighter, float>(atbGauges),
                SpellCooldowns = SerializeSpellCooldowns()
            });

            // Switch to new mode
            if (newMode == CombatMode.TurnBased)
            {
                ConvertToTurnBased();
            }
            else
            {
                ConvertToRealTime();
            }

            currentMode = newMode;
            modeSwitchCooldown = MODE_SWITCH_DELAY;

            ShowNotification($"Combat mode: {newMode}");
            OnModeChanged?.Invoke(newMode);
        }

        // Real-Time Spell Casting System
        public bool TryStartSpellCast(Fighter caster, int spellId, Fighter target, Vector3 targetPosition)
        {
            if (currentMode != CombatMode.RealTime && currentMode != CombatMode.TurnBased)
                return false;

            // Check if caster is already casting
            if (activeSpellCasts.ContainsKey(caster))
            {
                // Queue the spell if within queue window
                if (GameManager.Instance.Config.EnableRealTimeSpellCasting)
                {
                    QueueSpellCast(caster, spellId, target, targetPosition);
                }
                return false;
            }

            // Check spell cooldown
            if (IsSpellOnCooldown(caster, spellId))
                return false;

            // Get spell data
            var spell = SpellDatabase.GetSpell(spellId);
            if (spell == null) return false;

            // Check ATB cost in real-time mode
            if (currentMode == CombatMode.RealTime)
            {
                if (atbGauges[caster] < spell.ATBCost)
                    return false;

                // Consume ATB
                atbGauges[caster] -= spell.ATBCost;
            }

            // Create spell cast
            var spellCast = new SpellCast
            {
                Caster = caster,
                SpellId = spellId,
                Target = target,
                TargetPosition = targetPosition,
                CastTime = spell.CastTime,
                ElapsedTime = 0f,
                IsChanneling = spell.IsChanneling,
                CanMove = spell.CanCastWhileMoving
            };

            activeSpellCasts[caster] = spellCast;
            OnSpellCastStarted?.Invoke(caster, spellCast);

            // If instant cast, complete immediately
            if (spell.CastTime <= 0)
            {
                CompleteSpellCast(caster);
            }

            return true;
        }

        private void QueueSpellCast(Fighter caster, int spellId, Fighter target, Vector3 targetPosition)
        {
            var queuedCast = new SpellCast
            {
                Caster = caster,
                SpellId = spellId,
                Target = target,
                TargetPosition = targetPosition,
                CastTime = 0,
                ElapsedTime = 0,
                IsChanneling = false,
                CanMove = true
            };

            spellQueue.Enqueue(queuedCast);
        }

        private void UpdateSpellCasts()
        {
            var completedCasts = new List<Fighter>();

            foreach (var kvp in activeSpellCasts)
            {
                var fighter = kvp.Key;
                var spellCast = kvp.Value;

                spellCast.ElapsedTime += Time.deltaTime;

                // Check for movement interruption
                if (!spellCast.CanMove && fighter.IsMoving)
                {
                    InterruptSpellCast(fighter, "Movement interrupted the cast");
                    continue;
                }

                // Check if cast is complete
                if (spellCast.ElapsedTime >= spellCast.CastTime)
                {
                    completedCasts.Add(fighter);
                }
            }

            // Complete finished casts
            foreach (var fighter in completedCasts)
            {
                CompleteSpellCast(fighter);
            }

            // Process spell queue
            ProcessSpellQueue();
        }

        private void CompleteSpellCast(Fighter caster)
        {
            if (!activeSpellCasts.TryGetValue(caster, out SpellCast spellCast))
                return;

            var spell = SpellDatabase.GetSpell(spellCast.SpellId);
            if (spell != null)
            {
                // Execute the spell
                ExecuteSpell(caster, spell, spellCast.Target, spellCast.TargetPosition);

                // Start cooldown
                StartSpellCooldown(caster, spellCast.SpellId, spell.Cooldown);
            }

            OnSpellCastCompleted?.Invoke(caster, spellCast);
            activeSpellCasts.Remove(caster);

            // Check queued spells
            ProcessQueuedSpellForCaster(caster);
        }

        private void InterruptSpellCast(Fighter caster, string reason)
        {
            if (!activeSpellCasts.TryGetValue(caster, out SpellCast spellCast))
                return;

            Debug.Log($"Spell cast interrupted for {caster.name}: {reason}");
            OnSpellCastInterrupted?.Invoke(caster, spellCast);
            activeSpellCasts.Remove(caster);
        }

        private void CancelAllSpellCasts()
        {
            foreach (var caster in activeSpellCasts.Keys.ToList())
            {
                InterruptSpellCast(caster, "Combat mode changed");
            }
            spellQueue.Clear();
        }

        private void ExecuteSpell(Fighter caster, Spell spell, Fighter target, Vector3 targetPosition)
        {
            // Apply spell effects
            if (target != null && spell.TargetType == SpellTargetType.SingleTarget)
            {
                ApplySpellEffects(caster, spell, target);
            }
            else if (spell.TargetType == SpellTargetType.Area)
            {
                var targetsInArea = GetTargetsInArea(targetPosition, spell.AreaRadius);
                foreach (var areaTarget in targetsInArea)
                {
                    ApplySpellEffects(caster, spell, areaTarget);
                }
            }
            else if (spell.TargetType == SpellTargetType.Self)
            {
                ApplySpellEffects(caster, spell, caster);
            }

            // Visual effects
            PlaySpellEffects(spell, targetPosition);
        }

        private void ApplySpellEffects(Fighter caster, Spell spell, Fighter target)
        {
            // Damage/Heal
            if (spell.Damage > 0)
            {
                int finalDamage = CalculateSpellDamage(caster, spell, target);
                target.TakeDamage(finalDamage);
            }

            if (spell.Healing > 0)
            {
                target.Heal(spell.Healing);
            }

            // Apply buffs/debuffs
            foreach (var effect in spell.StatusEffects)
            {
                target.ApplyStatusEffect(effect);
            }
        }

        private int CalculateSpellDamage(Fighter caster, Spell spell, Fighter target)
        {
            float baseDamage = spell.Damage;
            float spellPower = caster.SpellPower;
            float resistance = target.GetResistance(spell.Element);

            float finalDamage = baseDamage * (1 + spellPower / 100) * (1 - resistance / 100);

            // Critical hit chance
            if (UnityEngine.Random.Range(0f, 100f) < caster.CriticalChance)
            {
                finalDamage *= 1.5f;
            }

            return Mathf.RoundToInt(finalDamage);
        }

        private List<Fighter> GetTargetsInArea(Vector3 center, float radius)
        {
            return allFighters.Where(f =>
                f.IsAlive && Vector3.Distance(f.Position, center) <= radius
            ).ToList();
        }

        private void PlaySpellEffects(Spell spell, Vector3 position)
        {
            // Implement visual effects
            // This would instantiate particle effects, play sounds, etc.
        }

        private bool IsSpellOnCooldown(Fighter fighter, int spellId)
        {
            if (!spellCooldowns.ContainsKey(fighter))
                return false;

            return spellCooldowns[fighter].ContainsKey(spellId) &&
                   spellCooldowns[fighter][spellId] > 0;
        }

        private void StartSpellCooldown(Fighter fighter, int spellId, float cooldown)
        {
            if (!spellCooldowns.ContainsKey(fighter))
                spellCooldowns[fighter] = new Dictionary<int, float>();

            spellCooldowns[fighter][spellId] = cooldown;
        }

        private void UpdateSpellCooldowns()
        {
            foreach (var fighter in spellCooldowns.Keys.ToList())
            {
                var cooldowns = spellCooldowns[fighter];
                var expiredCooldowns = new List<int>();

                foreach (var spellId in cooldowns.Keys.ToList())
                {
                    cooldowns[spellId] -= Time.deltaTime;
                    if (cooldowns[spellId] <= 0)
                    {
                        expiredCooldowns.Add(spellId);
                    }
                }

                foreach (var spellId in expiredCooldowns)
                {
                    cooldowns.Remove(spellId);
                }
            }
        }

        private void ProcessSpellQueue()
        {
            if (spellQueue.Count == 0) return;

            var nextCast = spellQueue.Peek();
            if (!activeSpellCasts.ContainsKey(nextCast.Caster))
            {
                spellQueue.Dequeue();
                TryStartSpellCast(nextCast.Caster, nextCast.SpellId, nextCast.Target, nextCast.TargetPosition);
            }
        }

        private void ProcessQueuedSpellForCaster(Fighter caster)
        {
            var queuedSpell = spellQueue.FirstOrDefault(s => s.Caster == caster);
            if (queuedSpell != null)
            {
                spellQueue = new Queue<SpellCast>(spellQueue.Where(s => s != queuedSpell));
                TryStartSpellCast(queuedSpell.Caster, queuedSpell.SpellId, queuedSpell.Target, queuedSpell.TargetPosition);
            }
        }

        private Dictionary<int, float> SerializeSpellCooldowns()
        {
            var serialized = new Dictionary<int, float>();
            foreach (var kvp in spellCooldowns)
            {
                foreach (var cooldown in kvp.Value)
                {
                    serialized[cooldown.Key] = cooldown.Value;
                }
            }
            return serialized;
        }

        private void Update()
        {
            if (!isInCombat) return;

            // Update cooldowns
            if (modeSwitchCooldown > 0)
                modeSwitchCooldown -= Time.deltaTime;

            // Update spell casts and cooldowns
            UpdateSpellCasts();
            UpdateSpellCooldowns();

            if (currentMode == CombatMode.TurnBased)
            {
                UpdateTurnBased();
            }
            else if (currentMode == CombatMode.RealTime)
            {
                UpdateRealTime();
            }

            UpdateActionTimeline();
        }

        private void UpdateTurnBased()
        {
            if (currentTurnFighter == null) return;

            turnTimer += Time.deltaTime;

            if (turnTimer >= TURN_TIME_LIMIT)
            {
                EndTurn();
            }
        }

        private void UpdateRealTime()
        {
            foreach (var fighter in allFighters)
            {
                if (!fighter.IsAlive) continue;

                float fillRate = (fighter.Speed / 100f) * globalTimeScale;
                atbGauges[fighter] += fillRate * Time.deltaTime * 100f;

                if (atbGauges[fighter] >= 100f)
                {
                    atbGauges[fighter] = 100f;
                    OnATBFilled?.Invoke(fighter, 100f);

                    if (fighter.IsAI)
                    {
                        ExecuteAIAction(fighter);
                    }
                }
            }
        }

        private void UpdateActionTimeline()
        {
            actionTimeline.Clear();

            if (currentMode == CombatMode.TurnBased)
            {
                var tempQueue = new Queue<Fighter>(turnQueue);
                float time = 0f;

                for (int i = 0; i < 5 && tempQueue.Count > 0; i++)
                {
                    var fighter = tempQueue.Dequeue();
                    actionTimeline.Add(new PredictedAction
                    {
                        Fighter = fighter,
                        TimeUntilAction = time,
                        ActionType = "Turn"
                    });
                    time += TURN_TIME_LIMIT;
                }
            }
            else if (currentMode == CombatMode.RealTime)
            {
                // Include spell casts in timeline
                foreach (var kvp in activeSpellCasts)
                {
                    var remainingTime = kvp.Value.CastTime - kvp.Value.ElapsedTime;
                    actionTimeline.Add(new PredictedAction
                    {
                        Fighter = kvp.Key,
                        TimeUntilAction = remainingTime,
                        ActionType = "Spell Cast",
                        SpellId = kvp.Value.SpellId
                    });
                }

                // ATB predictions
                foreach (var kvp in atbGauges.OrderByDescending(x => x.Value))
                {
                    var fighter = kvp.Key;
                    var currentATB = kvp.Value;
                    var fillRate = (fighter.Speed / 100f) * globalTimeScale;
                    var timeToFill = (100f - currentATB) / (fillRate * 100f);

                    actionTimeline.Add(new PredictedAction
                    {
                        Fighter = fighter,
                        TimeUntilAction = timeToFill,
                        ActionType = "ATB Ready"
                    });
                }

                actionTimeline = actionTimeline.OrderBy(a => a.TimeUntilAction).Take(5).ToList();
            }
        }

        public List<PredictedAction> GetActionTimeline(int count)
        {
            return actionTimeline.Take(count).ToList();
        }

        public void EndTurn()
        {
            if (currentMode != CombatMode.TurnBased) return;

            if (currentTurnFighter != null)
            {
                turnQueue.Enqueue(currentTurnFighter);
            }

            StartNextTurn();
        }

        private void StartNextTurn()
        {
            if (turnQueue.Count == 0)
            {
                InitializeTurnBased();
                return;
            }

            currentTurnFighter = turnQueue.Dequeue();
            turnTimer = 0f;

            currentTurnFighter.ActionPoints = currentTurnFighter.MaxActionPoints;
            currentTurnFighter.MovementPoints = currentTurnFighter.MaxMovementPoints;

            OnTurnStarted?.Invoke(currentTurnFighter);
        }

        public bool TryExecuteRealTimeAction(Fighter fighter, CombatAction action)
        {
            if (currentMode != CombatMode.RealTime) return false;
            if (atbGauges[fighter] < action.ATBCost) return false;

            ExecuteAction(fighter, action);
            atbGauges[fighter] -= action.ATBCost;

            return true;
        }

        private void ExecuteAIAction(Fighter fighter)
        {
            var target = GetNearestEnemy(fighter);
            if (target != null)
            {
                // AI can cast spells in real-time
                if (UnityEngine.Random.Range(0f, 1f) < 0.3f && fighter.KnownSpells.Count > 0)
                {
                    var spell = fighter.KnownSpells[UnityEngine.Random.Range(0, fighter.KnownSpells.Count)];
                    TryStartSpellCast(fighter, spell, target, target.Position);
                }
                else
                {
                    var action = new CombatAction
                    {
                        Type = ActionType.BasicAttack,
                        Target = target,
                        ATBCost = 50f
                    };
                    TryExecuteRealTimeAction(fighter, action);
                }
            }
        }

        private void ExecuteAction(Fighter source, CombatAction action)
        {
            switch (action.Type)
            {
                case ActionType.BasicAttack:
                    DealDamage(source, action.Target, source.AttackDamage);
                    break;
                case ActionType.Spell:
                    var spell = SpellDatabase.GetSpell(action.SpellId);
                    if (spell != null)
                    {
                        TryStartSpellCast(source, action.SpellId, action.Target, action.Target?.Position ?? source.Position);
                    }
                    break;
                case ActionType.Move:
                    MoveFighter(source, action.TargetCell);
                    break;
            }
        }

        private Fighter GetNearestEnemy(Fighter source)
        {
            var enemies = source.IsPlayerTeam ? enemyTeam : playerTeam;
            return enemies.Where(e => e.IsAlive).OrderBy(e =>
                Vector3.Distance(source.Position, e.Position)
            ).FirstOrDefault();
        }

        private void DealDamage(Fighter source, Fighter target, int damage)
        {
            target.TakeDamage(damage);

            if (!target.IsAlive)
            {
                RemoveFighter(target);
                CheckBattleEnd();
            }
        }

        private void MoveFighter(Fighter fighter, int cellId)
        {
            fighter.MoveTo(cellId);
        }

        private void RemoveFighter(Fighter fighter)
        {
            allFighters.Remove(fighter);
            if (playerTeam.Contains(fighter)) playerTeam.Remove(fighter);
            if (enemyTeam.Contains(fighter)) enemyTeam.Remove(fighter);
            if (atbGauges.ContainsKey(fighter)) atbGauges.Remove(fighter);
            if (activeSpellCasts.ContainsKey(fighter))
            {
                InterruptSpellCast(fighter, "Fighter removed from battle");
            }
        }

        private void CheckBattleEnd()
        {
            bool playersAlive = playerTeam.Any(f => f.IsAlive);
            bool enemiesAlive = enemyTeam.Any(f => f.IsAlive);

            if (!playersAlive || !enemiesAlive)
            {
                EndBattle(playersAlive);
            }
        }

        private void EndBattle(bool victory)
        {
            isInCombat = false;
            CancelAllSpellCasts();

            var result = new CombatResult
            {
                Victory = victory,
                Experience = CalculateExperience(),
                Loot = GenerateLoot()
            };

            OnCombatEnded?.Invoke(result);
            GameManager.Instance.ChangeState(GameState.InGame);
        }

        private int CalculateExperience()
        {
            return enemyTeam.Where(e => !e.IsAlive).Sum(e => e.ExperienceValue);
        }

        private List<Item> GenerateLoot()
        {
            var loot = new List<Item>();
            foreach (var enemy in enemyTeam.Where(e => !e.IsAlive))
            {
                loot.AddRange(enemy.GenerateLoot());
            }
            return loot;
        }

        public bool ShouldAutoRealTime(Fighter fighter)
        {
            var playerLevel = playerTeam.FirstOrDefault()?.Level ?? 1;
            return fighter.Level < playerLevel - 5 || fighter.Health < 50;
        }

        private void ConvertToTurnBased()
        {
            CancelAllSpellCasts();

            var fightersByATB = allFighters.OrderByDescending(f =>
                atbGauges.ContainsKey(f) ? atbGauges[f] : 0f
            ).ToList();

            turnQueue = new Queue<Fighter>(fightersByATB);
            StartNextTurn();
        }

        private void ConvertToRealTime()
        {
            float baseATB = 50f;

            for (int i = 0; i < allFighters.Count; i++)
            {
                var fighter = allFighters[i];
                if (fighter == currentTurnFighter)
                {
                    atbGauges[fighter] = 100f;
                }
                else
                {
                    int position = turnQueue.ToList().IndexOf(fighter);
                    atbGauges[fighter] = baseATB - (position * 10f);
                }
            }
        }

        private void ShowNotification(string message)
        {
            Debug.Log($"[Combat] {message}");
        }

        // Alias method for InitializeBattle for compatibility
        public void InitializeCombat(List<Fighter> players, List<Fighter> enemies)
        {
            InitializeBattle(currentMode, players, enemies);
        }

        // Get all fighters in the battle
        public List<Fighter> GetAllFighters()
        {
            var allFightersList = new List<Fighter>();
            allFightersList.AddRange(playerTeam);
            allFightersList.AddRange(enemyTeam);
            return allFightersList;
        }

        // For testing
        public void InitializeBattle(CombatMode mode)
        {
            InitializeBattle(mode, new List<Fighter>(), new List<Fighter>());
        }

        public Fighter AddFighter(Fighter fighter)
        {
            allFighters.Add(fighter);
            if (currentMode == CombatMode.RealTime)
            {
                atbGauges[fighter] = 0f;
            }
            spellCooldowns[fighter] = new Dictionary<int, float>();
            return fighter;
        }
    }

    public class CombatResult
    {
        public bool Victory;
        public int Experience;
        public List<Item> Loot;
    }

    public class Item
    {
        public string Name;
        public int Value;
    }
}