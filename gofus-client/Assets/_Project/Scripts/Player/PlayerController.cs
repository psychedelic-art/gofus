using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Core;
using GOFUS.Map;
using GOFUS.Entities;
using GOFUS.Combat;

namespace GOFUS.Player
{
    public enum PlayerDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public class PlayerController : MonoBehaviour
    {
        [Header("Position")]
        [SerializeField] private int currentCellId;
        [SerializeField] private bool isMoving;
        [SerializeField] private List<int> currentPath;
        [SerializeField] private float moveSpeed = 5f;

        [Header("Stats")]
        [SerializeField] private int level = 1;
        [SerializeField] private int experience;
        [SerializeField] private int experienceToNextLevel = 100;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int maxMana = 50;
        [SerializeField] private int currentMana = 50;

        [Header("Combat")]
        [SerializeField] private int maxActionPoints = 6;
        [SerializeField] private int currentActionPoints = 6;
        [SerializeField] private int maxMovementPoints = 3;
        [SerializeField] private int currentMovementPoints = 3;

        [Header("Attributes")]
        [SerializeField] private int strength = 10;
        [SerializeField] private int intelligence = 10;
        [SerializeField] private int agility = 10;
        [SerializeField] private int vitality = 10;
        [SerializeField] private int wisdom = 10;
        [SerializeField] private int chance = 10;

        [Header("References")]
        private MapRenderer mapRenderer;
        private PlayerAnimator playerAnimator;
        private Dictionary<int, Item> inventory;

        // Properties
        public int CurrentCellId => currentCellId;
        public bool IsMoving => isMoving;
        public List<int> CurrentPath => currentPath;
        public int Level => level;
        public int Experience => experience;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int MaxMana => maxMana;
        public int CurrentMana => currentMana;
        public bool IsAlive => currentHealth > 0;
        public int CurrentActionPoints
        {
            get => currentActionPoints;
            set => currentActionPoints = Mathf.Clamp(value, 0, maxActionPoints);
        }
        public int CurrentMovementPoints
        {
            get => currentMovementPoints;
            set => currentMovementPoints = Mathf.Clamp(value, 0, maxMovementPoints);
        }

        // Events
        public event Action<int> OnCellReached;
        public event Action OnPathComplete;
        public event Action<int> OnHealthChanged;
        public event Action<int> OnManaChanged;
        public event Action<int> OnLevelUp;
        public event Action OnDeath;
        public event Action<Item> OnItemAdded;
        public event Action<Item> OnItemRemoved;

        private void Awake()
        {
            inventory = new Dictionary<int, Item>();
            playerAnimator = GetComponent<PlayerAnimator>();
            currentHealth = maxHealth;
            currentMana = maxMana;
        }

        public void Initialize(MapRenderer map)
        {
            mapRenderer = map;

            // Subscribe to map events
            if (mapRenderer != null)
            {
                mapRenderer.OnCellClicked += OnCellClicked;
            }
        }

        public void SetPosition(int cellId)
        {
            currentCellId = cellId;
            transform.position = IsometricHelper.CellIdToWorldPosition(cellId);
        }

        public bool RequestMove(int targetCellId)
        {
            if (isMoving) return false;

            // Validate target cell
            if (!IsValidMove(targetCellId))
                return false;

            // Calculate path
            currentPath = CalculatePath(currentCellId, targetCellId);

            if (currentPath != null && currentPath.Count > 0)
            {
                StartCoroutine(FollowPath());
                return true;
            }

            return false;
        }

        private bool IsValidMove(int cellId)
        {
            if (mapRenderer == null || mapRenderer.Grid == null)
                return true; // Allow movement if no map loaded

            if (cellId < 0 || cellId >= IsometricHelper.TOTAL_CELLS)
                return false;

            return mapRenderer.Grid.Cells[cellId].IsWalkable;
        }

        public List<int> CalculatePath(int startCell, int endCell)
        {
            if (mapRenderer == null || mapRenderer.Grid == null)
            {
                // Simple direct path if no map
                return new List<int> { endCell };
            }

            // Use A* pathfinding
            var pathfinder = new AStarPathfinder(mapRenderer.Grid);
            return pathfinder.FindPath(startCell, endCell);
        }

        private IEnumerator FollowPath()
        {
            isMoving = true;

            for (int i = 0; i < currentPath.Count; i++)
            {
                int targetCell = currentPath[i];
                Vector3 targetPos = IsometricHelper.CellIdToWorldPosition(targetCell);

                // Calculate direction for animation
                if (playerAnimator != null)
                {
                    var direction = CalculateDirection(currentCellId, targetCell);
                    playerAnimator.SetDirection(direction);
                    playerAnimator.SetMoving(true);
                }

                // Move to cell
                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPos,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                currentCellId = targetCell;
                OnCellReached?.Invoke(currentCellId);

                // Use movement points in combat
                if (GameManager.Instance.CurrentState == GameState.Battle_TurnBased)
                {
                    currentMovementPoints--;
                    if (currentMovementPoints <= 0)
                        break;
                }
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetMoving(false);
            }

            currentPath = null;
            isMoving = false;
            OnPathComplete?.Invoke();
        }

        public PlayerDirection CalculateDirection(int fromCell, int toCell)
        {
            Vector2Int from = IsometricHelper.CellIdToGridCoords(fromCell);
            Vector2Int to = IsometricHelper.CellIdToGridCoords(toCell);

            int dx = to.x - from.x;
            int dy = to.y - from.y;

            // Normalize to -1, 0, 1
            dx = Mathf.Clamp(dx, -1, 1);
            dy = Mathf.Clamp(dy, -1, 1);

            // Map to 8 directions
            if (dx == 0 && dy == -1) return PlayerDirection.North;
            if (dx == 1 && dy == -1) return PlayerDirection.NorthEast;
            if (dx == 1 && dy == 0) return PlayerDirection.East;
            if (dx == 1 && dy == 1) return PlayerDirection.SouthEast;
            if (dx == 0 && dy == 1) return PlayerDirection.South;
            if (dx == -1 && dy == 1) return PlayerDirection.SouthWest;
            if (dx == -1 && dy == 0) return PlayerDirection.West;
            if (dx == -1 && dy == -1) return PlayerDirection.NorthWest;

            return PlayerDirection.South; // Default
        }

        public void TakeDamage(int damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth);

            if (!IsAlive)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth);
        }

        public void UseMana(int amount)
        {
            currentMana = Mathf.Max(0, currentMana - amount);
            OnManaChanged?.Invoke(currentMana);
        }

        public void RestoreMana(int amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            OnManaChanged?.Invoke(currentMana);
        }

        private void Die()
        {
            OnDeath?.Invoke();

            if (playerAnimator != null)
            {
                playerAnimator.PlayDeath();
            }

            // Disable movement
            isMoving = false;
            currentPath = null;
        }

        public void Respawn(int cellId)
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            SetPosition(cellId);
            OnHealthChanged?.Invoke(currentHealth);
            OnManaChanged?.Invoke(currentMana);
        }

        public bool UseActionPoints(int amount)
        {
            if (currentActionPoints >= amount)
            {
                currentActionPoints -= amount;
                return true;
            }
            return false;
        }

        public bool UseMovementPoints(int amount)
        {
            if (currentMovementPoints >= amount)
            {
                currentMovementPoints -= amount;
                return true;
            }
            return false;
        }

        public void ResetActionPoints()
        {
            currentActionPoints = maxActionPoints;
            currentMovementPoints = maxMovementPoints;
        }

        public void AddExperience(int amount)
        {
            experience += amount;

            while (experience >= experienceToNextLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            level++;
            experience -= experienceToNextLevel;
            experienceToNextLevel = CalculateExperienceToNextLevel(level);

            // Increase stats
            maxHealth += 10;
            maxMana += 5;
            currentHealth = maxHealth;
            currentMana = maxMana;

            // Grant stat points
            OnLevelUp?.Invoke(level);
        }

        private int CalculateExperienceToNextLevel(int currentLevel)
        {
            return currentLevel * currentLevel * 100;
        }

        // Inventory Management
        public bool AddItem(Item item)
        {
            if (item == null) return false;

            if (inventory.ContainsKey(item.Id))
            {
                inventory[item.Id].Quantity += item.Quantity;
            }
            else
            {
                inventory[item.Id] = item;
            }

            OnItemAdded?.Invoke(item);
            return true;
        }

        public bool RemoveItem(int itemId, int quantity = 1)
        {
            if (!inventory.ContainsKey(itemId))
                return false;

            var item = inventory[itemId];

            if (item.Quantity >= quantity)
            {
                item.Quantity -= quantity;

                if (item.Quantity <= 0)
                {
                    inventory.Remove(itemId);
                }

                OnItemRemoved?.Invoke(item);
                return true;
            }

            return false;
        }

        public bool HasItem(int itemId)
        {
            return inventory.ContainsKey(itemId) && inventory[itemId].Quantity > 0;
        }

        public int GetItemCount(int itemId)
        {
            return inventory.ContainsKey(itemId) ? inventory[itemId].Quantity : 0;
        }

        public Item GetItem(int itemId)
        {
            return inventory.ContainsKey(itemId) ? inventory[itemId] : null;
        }

        public List<Item> GetAllItems()
        {
            return new List<Item>(inventory.Values);
        }

        private void OnCellClicked(int cellId)
        {
            // Handle click-to-move
            if (GameManager.Instance.CurrentState == GameState.InGame ||
                GameManager.Instance.CurrentState == GameState.Battle_RealTime)
            {
                RequestMove(cellId);
            }
            else if (GameManager.Instance.CurrentState == GameState.Battle_TurnBased)
            {
                // Check if we have movement points
                int distance = IsometricHelper.GetDistance(currentCellId, cellId);
                if (distance <= currentMovementPoints)
                {
                    RequestMove(cellId);
                }
            }
        }

        public void SetStats(int str, int intel, int agi, int vit, int wis, int cha)
        {
            strength = str;
            intelligence = intel;
            agility = agi;
            vitality = vit;
            wisdom = wis;
            chance = cha;

            // Recalculate derived stats
            maxHealth = 50 + (level * 10) + (vitality * 5);
            maxMana = 20 + (level * 5) + (wisdom * 3);
        }

        public int GetAttackDamage()
        {
            return 5 + strength + (level * 2);
        }

        public int GetSpellPower()
        {
            return intelligence + (level * 2);
        }

        public float GetCriticalChance()
        {
            return 5f + (agility * 0.5f);
        }

        public int GetInitiative()
        {
            return agility + wisdom;
        }

        private void OnDestroy()
        {
            if (mapRenderer != null)
            {
                mapRenderer.OnCellClicked -= OnCellClicked;
            }
        }
    }

    [Serializable]
    public class Item
    {
        public int Id;
        public string Name;
        public string Description;
        public int Quantity;
        public ItemType Type;
        public int Value;
        public Sprite Icon;
    }

    public enum ItemType
    {
        Consumable,
        Equipment,
        Resource,
        Quest,
        Currency
    }
}