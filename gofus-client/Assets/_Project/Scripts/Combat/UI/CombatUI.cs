using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GOFUS.Combat.Advanced;
using TMPro;

namespace GOFUS.Combat.UI
{
    /// <summary>
    /// Main combat UI controller
    /// Manages all combat interface elements
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private GameObject turnOrderPanel;
        [SerializeField] private GameObject actionMenuPanel;
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private GameObject combatLogPanel;

        [Header("Turn Order Display")]
        [SerializeField] private Transform turnOrderContainer;
        [SerializeField] private GameObject turnOrderItemPrefab;
        private List<TurnOrderItem> turnOrderItems = new List<TurnOrderItem>();

        [Header("Action Menu")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillsButton;
        [SerializeField] private Button itemsButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button fleeButton;

        [Header("Skill Display")]
        [SerializeField] private Transform skillContainer;
        [SerializeField] private GameObject skillButtonPrefab;
        private List<SkillButton> skillButtons = new List<SkillButton>();

        [Header("Target Selection")]
        [SerializeField] private Transform targetContainer;
        [SerializeField] private GameObject targetButtonPrefab;
        private List<TargetButton> targetButtons = new List<TargetButton>();

        [Header("Combat Log")]
        [SerializeField] private TextMeshProUGUI combatLogText;
        [SerializeField] private ScrollRect combatLogScroll;
        [SerializeField] private int maxLogLines = 50;

        [Header("Status Display")]
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectIconPrefab;

        [Header("Damage Numbers")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private float damageNumberDuration = 1.5f;

        // References
        private AdvancedCombatManager combatManager;
        private CombatEntity currentActor;
        private Skill selectedSkill;
        private Action<CombatEntity> onTargetSelected;

        // Events
        public event Action<Skill> OnSkillSelected;
        public event Action<CombatEntity> OnTargetConfirmed;

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            combatManager = AdvancedCombatManager.Instance;

            // Hide all panels initially
            HideAllPanels();

            // Setup button listeners
            if (attackButton) attackButton.onClick.AddListener(OnAttackClicked);
            if (skillsButton) skillsButton.onClick.AddListener(OnSkillsClicked);
            if (itemsButton) itemsButton.onClick.AddListener(OnItemsClicked);
            if (defendButton) defendButton.onClick.AddListener(OnDefendClicked);
            if (fleeButton) fleeButton.onClick.AddListener(OnFleeClicked);
        }

        private void SubscribeToEvents()
        {
            if (combatManager != null)
            {
                combatManager.OnCombatStart += OnCombatStart;
                combatManager.OnCombatEnd += OnCombatEnd;
                combatManager.OnTurnStart += OnTurnStart;
                combatManager.OnTurnEnd += OnTurnEnd;
                combatManager.OnDamageDealt += OnDamageDealt;
                combatManager.OnCombatantDefeated += OnCombatantDefeated;
            }
        }

        private void OnCombatStart()
        {
            ShowCombatUI();
            UpdateTurnOrder();
            AddLogEntry("=== Combat Started ===");
        }

        private void OnCombatEnd()
        {
            HideCombatUI();
            AddLogEntry("=== Combat Ended ===");
        }

        private void OnTurnStart(CombatEntity entity)
        {
            currentActor = entity;
            UpdateTurnOrder();

            if (entity.Team == Team.Player)
            {
                ShowActionMenu();
                AddLogEntry($"{entity.Name}'s turn");
            }
            else
            {
                HideActionMenu();
                AddLogEntry($"{entity.Name} is thinking...");
            }
        }

        private void OnTurnEnd(CombatEntity entity)
        {
            HideActionMenu();
            HideSkillPanel();
            HideTargetPanel();
        }

        private void OnDamageDealt(CombatEntity attacker, CombatEntity target, int damage)
        {
            // Show damage number
            ShowDamageNumber(target.transform.position, damage, false);

            // Update log
            AddLogEntry($"{attacker.Name} deals {damage} damage to {target.Name}");
        }

        private void OnCombatantDefeated(CombatEntity defeated)
        {
            AddLogEntry($"{defeated.Name} has been defeated!");
        }

        public void ShowCombatUI()
        {
            if (combatPanel) combatPanel.SetActive(true);
            if (turnOrderPanel) turnOrderPanel.SetActive(true);
            if (combatLogPanel) combatLogPanel.SetActive(true);
        }

        public void HideCombatUI()
        {
            HideAllPanels();
        }

        private void HideAllPanels()
        {
            if (combatPanel) combatPanel.SetActive(false);
            if (turnOrderPanel) turnOrderPanel.SetActive(false);
            if (actionMenuPanel) actionMenuPanel.SetActive(false);
            if (skillPanel) skillPanel.SetActive(false);
            if (targetPanel) targetPanel.SetActive(false);
            if (combatLogPanel) combatLogPanel.SetActive(false);
        }

        private void ShowActionMenu()
        {
            if (actionMenuPanel) actionMenuPanel.SetActive(true);
        }

        private void HideActionMenu()
        {
            if (actionMenuPanel) actionMenuPanel.SetActive(false);
        }

        private void OnAttackClicked()
        {
            // Select basic attack
            selectedSkill = new Skill { Id = 1, Name = "Attack", Damage = 10 };
            ShowTargetSelection(SelectAttackTarget);
        }

        private void OnSkillsClicked()
        {
            ShowSkillPanel();
        }

        private void OnItemsClicked()
        {
            // TODO: Implement item panel
            AddLogEntry("Items not yet implemented");
        }

        private void OnDefendClicked()
        {
            // Execute defend action
            if (currentActor != null)
            {
                // Apply defense buff
                var defendBuff = new StatusEffect
                {
                    Name = "Defending",
                    Type = StatusEffectType.Buff,
                    Duration = 1,
                    StatModifier = new StatModifier { Defense = 5 }
                };

                combatManager.ApplyStatusEffect(currentActor, defendBuff);
                currentActor.HasActed = true;
                AddLogEntry($"{currentActor.Name} defends");
            }
        }

        private void OnFleeClicked()
        {
            AddLogEntry("Cannot flee from this battle!");
        }

        private void ShowSkillPanel()
        {
            if (skillPanel == null || currentActor == null) return;

            skillPanel.SetActive(true);
            PopulateSkills();
        }

        private void HideSkillPanel()
        {
            if (skillPanel) skillPanel.SetActive(false);
        }

        private void PopulateSkills()
        {
            // Clear existing buttons
            foreach (var button in skillButtons)
            {
                Destroy(button.gameObject);
            }
            skillButtons.Clear();

            // Get available skills
            var skills = combatManager?.skillSystem?.GetAvailableSkills(currentActor) ?? new List<Skill>();

            // Create skill buttons
            foreach (var skill in skills)
            {
                var buttonObj = Instantiate(skillButtonPrefab, skillContainer);
                var skillButton = buttonObj.GetComponent<SkillButton>() ?? buttonObj.AddComponent<SkillButton>();

                skillButton.Initialize(skill, () => HandleSkillSelection(skill));
                skillButtons.Add(skillButton);
            }
        }

        private void HandleSkillSelection(Skill skill)
        {
            selectedSkill = skill;
            HideSkillPanel();

            // Determine target type
            if (skill.TargetType == TargetType.Self)
            {
                ExecuteSkillOnSelf();
            }
            else
            {
                ShowTargetSelection(SelectSkillTarget);
            }
        }

        private void ShowTargetSelection(Action<CombatEntity> callback)
        {
            if (targetPanel == null) return;

            targetPanel.SetActive(true);
            onTargetSelected = callback;
            PopulateTargets();
        }

        private void HideTargetPanel()
        {
            if (targetPanel) targetPanel.SetActive(false);
        }

        private void PopulateTargets()
        {
            // Clear existing buttons
            foreach (var button in targetButtons)
            {
                Destroy(button.gameObject);
            }
            targetButtons.Clear();

            // Get all potential targets
            var allTargets = combatManager?.GetAllFighters() ?? new List<CombatEntity>();

            // Create target buttons
            foreach (var target in allTargets)
            {
                if (!target.IsAlive) continue;

                var buttonObj = Instantiate(targetButtonPrefab, targetContainer);
                var targetButton = buttonObj.GetComponent<TargetButton>() ?? buttonObj.AddComponent<TargetButton>();

                targetButton.Initialize(target, () => OnTargetSelected(target));
                targetButtons.Add(targetButton);
            }
        }

        private void OnTargetSelected(CombatEntity target)
        {
            HideTargetPanel();
            onTargetSelected?.Invoke(target);
        }

        private void SelectAttackTarget(CombatEntity target)
        {
            ExecuteAction(currentActor, target, selectedSkill);
        }

        private void SelectSkillTarget(CombatEntity target)
        {
            ExecuteAction(currentActor, target, selectedSkill);
        }

        private void ExecuteSkillOnSelf()
        {
            ExecuteAction(currentActor, currentActor, selectedSkill);
        }

        private void ExecuteAction(CombatEntity performer, CombatEntity target, Skill skill)
        {
            if (combatManager == null || performer == null || target == null || skill == null)
                return;

            var action = new GOFUS.Combat.Advanced.CombatAction
            {
                Performer = performer,
                Target = target,
                SkillId = skill.Id
            };

            combatManager.ExecuteAction(action);
        }

        public void UpdateTurnOrder()
        {
            // This would update the turn order display
            // Implementation depends on specific UI setup
        }

        public void ShowDamageNumber(Vector3 worldPosition, int damage, bool isCritical)
        {
            if (damageNumberPrefab == null || worldCanvas == null) return;

            var damageObj = Instantiate(damageNumberPrefab, worldCanvas.transform);
            var damageNumber = damageObj.GetComponent<DamageNumber>();

            if (damageNumber == null)
            {
                damageNumber = damageObj.AddComponent<DamageNumber>();
            }

            damageNumber.Initialize(worldPosition, damage, isCritical, damageNumberDuration);
        }

        public void ShowHealingNumber(Vector3 worldPosition, int healing)
        {
            if (damageNumberPrefab == null || worldCanvas == null) return;

            var healObj = Instantiate(damageNumberPrefab, worldCanvas.transform);
            var healNumber = healObj.GetComponent<DamageNumber>();

            if (healNumber == null)
            {
                healNumber = healObj.AddComponent<DamageNumber>();
            }

            healNumber.InitializeHealing(worldPosition, healing, damageNumberDuration);
        }

        public void AddLogEntry(string message)
        {
            if (combatLogText == null) return;

            // Add timestamp
            string timestampedMessage = $"[{Time.time:F1}] {message}\n";

            // Add to log
            combatLogText.text += timestampedMessage;

            // Limit log size
            string[] lines = combatLogText.text.Split('\n');
            if (lines.Length > maxLogLines)
            {
                var recentLines = new string[maxLogLines];
                Array.Copy(lines, lines.Length - maxLogLines, recentLines, 0, maxLogLines);
                combatLogText.text = string.Join("\n", recentLines);
            }

            // Scroll to bottom
            if (combatLogScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                combatLogScroll.verticalNormalizedPosition = 0;
            }
        }

        public void UpdateStatusEffects(CombatEntity entity)
        {
            if (statusEffectContainer == null || entity == null) return;

            // Clear existing icons
            foreach (Transform child in statusEffectContainer)
            {
                Destroy(child.gameObject);
            }

            // Create icons for current status effects
            var effects = combatManager?.GetStatusEffects(entity) ?? new List<StatusEffect>();

            foreach (var effect in effects)
            {
                var iconObj = Instantiate(statusEffectIconPrefab, statusEffectContainer);
                var icon = iconObj.GetComponent<StatusEffectIcon>();

                if (icon == null)
                {
                    icon = iconObj.AddComponent<StatusEffectIcon>();
                }

                icon.Initialize(effect);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (combatManager != null)
            {
                combatManager.OnCombatStart -= OnCombatStart;
                combatManager.OnCombatEnd -= OnCombatEnd;
                combatManager.OnTurnStart -= OnTurnStart;
                combatManager.OnTurnEnd -= OnTurnEnd;
                combatManager.OnDamageDealt -= OnDamageDealt;
                combatManager.OnCombatantDefeated -= OnCombatantDefeated;
            }
        }
    }

    // UI Component Classes
    public class TurnOrderItem : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public Image portrait;
        public Image highlight;

        public void SetData(CombatEntity entity, bool isActive)
        {
            if (nameText) nameText.text = entity.Name;
            if (highlight) highlight.enabled = isActive;
        }
    }

    public class SkillButton : MonoBehaviour
    {
        private Button button;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI costText;
        private Skill skill;

        public void Initialize(Skill skill, Action onClick)
        {
            this.skill = skill;

            button = GetComponent<Button>();
            nameText = GetComponentInChildren<TextMeshProUGUI>();

            if (button) button.onClick.AddListener(() => onClick());
            if (nameText) nameText.text = $"{skill.Name} ({skill.ManaCost} MP)";
        }
    }

    public class TargetButton : MonoBehaviour
    {
        private Button button;
        private TextMeshProUGUI nameText;
        private CombatEntity target;

        public void Initialize(CombatEntity target, Action onClick)
        {
            this.target = target;

            button = GetComponent<Button>();
            nameText = GetComponentInChildren<TextMeshProUGUI>();

            if (button) button.onClick.AddListener(() => onClick());
            if (nameText)
            {
                string teamColor = target.Team == Team.Player ? "green" : "red";
                nameText.text = $"<color={teamColor}>{target.Name}</color> ({target.CurrentHealth}/{target.MaxHealth})";
            }
        }
    }

    public class StatusEffectIcon : MonoBehaviour
    {
        public Image icon;
        public TextMeshProUGUI durationText;

        public void Initialize(StatusEffect effect)
        {
            if (durationText) durationText.text = effect.Duration.ToString();
            // Icon would be set based on effect type
        }
    }

    public class DamageNumber : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private float duration;
        private float elapsed;
        private Vector3 startPosition;

        public void Initialize(Vector3 worldPos, int damage, bool isCritical, float duration)
        {
            text = GetComponent<TextMeshProUGUI>();
            if (text == null) text = gameObject.AddComponent<TextMeshProUGUI>();

            text.text = damage.ToString();
            text.color = isCritical ? Color.yellow : Color.white;
            text.fontSize = isCritical ? 36 : 24;

            startPosition = worldPos;
            transform.position = worldPos;
            this.duration = duration;

            StartCoroutine(Animate());
        }

        public void InitializeHealing(Vector3 worldPos, int healing, float duration)
        {
            text = GetComponent<TextMeshProUGUI>();
            if (text == null) text = gameObject.AddComponent<TextMeshProUGUI>();

            text.text = $"+{healing}";
            text.color = Color.green;
            text.fontSize = 24;

            startPosition = worldPos;
            transform.position = worldPos;
            this.duration = duration;

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Move upward and fade out
                transform.position = startPosition + Vector3.up * progress * 2f;

                if (text != null)
                {
                    Color c = text.color;
                    c.a = 1f - progress;
                    text.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}