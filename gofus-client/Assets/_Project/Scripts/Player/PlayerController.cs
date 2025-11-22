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

        // Sprite positioning - Offset to render full character sprite above cell
        // This ensures the character body is visible, not just the head
        // CharacterLayerRenderer also handles internal layer offset based on sprite pivot
        private const float SPRITE_VERTICAL_OFFSET = 1.0f; // Offset to show full sprite

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

            // Try to get PlayerAnimator component
            playerAnimator = GetComponent<PlayerAnimator>();

            if (playerAnimator == null)
            {
                Debug.LogWarning("[PlayerController] PlayerAnimator component not found! Adding it automatically...");
                playerAnimator = gameObject.AddComponent<PlayerAnimator>();
                Debug.Log("[PlayerController] PlayerAnimator component added successfully!");
            }
            else
            {
                Debug.Log("[PlayerController] PlayerAnimator component found and assigned!");
            }

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
            transform.position = IsometricHelper.CellIdToWorldPosition(cellId) + Vector3.up * SPRITE_VERTICAL_OFFSET;
        }

        public bool RequestMove(int targetCellId)
        {
            Debug.Log($"[PlayerController] RequestMove called: target={targetCellId}, isMoving={isMoving}");

            if (isMoving)
            {
                Debug.LogWarning("[PlayerController] Already moving, ignoring request");
                return false;
            }

            // Validate target cell
            if (!IsValidMove(targetCellId))
            {
                Debug.LogWarning($"[PlayerController] Invalid move to cell {targetCellId}");
                return false;
            }

            Debug.Log($"[PlayerController] Calculating path from {currentCellId} to {targetCellId}");

            // Calculate path
            currentPath = CalculatePath(currentCellId, targetCellId);

            if (currentPath == null)
            {
                Debug.LogWarning($"[PlayerController] CalculatePath returned NULL for {currentCellId} -> {targetCellId}");
                return false;
            }

            if (currentPath.Count == 0)
            {
                Debug.LogWarning($"[PlayerController] CalculatePath returned EMPTY path for {currentCellId} -> {targetCellId}");
                return false;
            }

            Debug.Log($"[PlayerController] Path found with {currentPath.Count} cells: [{string.Join(", ", currentPath)}]");
            StartCoroutine(FollowPath());
            return true;
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
            Debug.Log($"[PlayerController] CalculatePath: start={startCell}, end={endCell}, mapRenderer={(mapRenderer == null ? "NULL" : "OK")}, Grid={(mapRenderer?.Grid == null ? "NULL" : "OK")}");

            if (mapRenderer == null || mapRenderer.Grid == null)
            {
                // Simple direct path if no map
                Debug.Log($"[PlayerController] No map/grid, returning simple path to {endCell}");
                return new List<int> { endCell };
            }

            Debug.Log($"[PlayerController] Creating AStarPathfinder with grid...");

            try
            {
                // Use A* pathfinding
                var pathfinder = new AStarPathfinder(mapRenderer.Grid);

                Debug.Log($"[PlayerController] Calling FindPath({startCell}, {endCell})...");
                var result = pathfinder.FindPath(startCell, endCell);

                Debug.Log($"[PlayerController] FindPath returned: {(result == null ? "NULL" : $"List with {result.Count} cells")}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerController] Exception in pathfinding: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private IEnumerator FollowPath()
        {
            Debug.Log($"[PlayerController] Starting FollowPath coroutine - playerAnimator={(playerAnimator != null ? "OK" : "NULL")}");
            isMoving = true;

            for (int i = 0; i < currentPath.Count; i++)
            {
                int targetCell = currentPath[i];
                Vector3 targetPos = IsometricHelper.CellIdToWorldPosition(targetCell) + Vector3.up * SPRITE_VERTICAL_OFFSET;
                Debug.Log($"[PlayerController] Moving to cell {targetCell} at position {targetPos} (step {i + 1}/{currentPath.Count})");

                // Calculate direction for animation
                if (playerAnimator != null)
                {
                    var direction = CalculateDirection(currentCellId, targetCell);
                    playerAnimator.SetDirection(direction);
                    playerAnimator.SetMoving(true);
                    Debug.Log($"[PlayerController] Animation set: direction={direction}, moving=true");
                }
                else
                {
                    Debug.LogWarning("[PlayerController] playerAnimator is NULL! Animation will not play. Assign PlayerAnimator component.");
                }

                // Move to cell
                float startTime = Time.time;
                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPos,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }
                float elapsed = Time.time - startTime;
                Debug.Log($"[PlayerController] Reached cell {targetCell} in {elapsed:F2} seconds");

                currentCellId = targetCell;
                OnCellReached?.Invoke(currentCellId);

                // Use movement points in combat
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Battle_TurnBased)
                {
                    currentMovementPoints--;
                    Debug.Log($"[PlayerController] MP used, remaining: {currentMovementPoints}");
                    if (currentMovementPoints <= 0)
                    {
                        Debug.Log("[PlayerController] Out of MP, stopping movement");
                        break;
                    }
                }
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetMoving(false);
            }

            Debug.Log("[PlayerController] Path complete");
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

            Debug.Log($"[PlayerController] Direction calc: from({from.x},{from.y}) to({to.x},{to.y}) = dx:{dx}, dy:{dy}");

            // Normalize for diamond grid where horizontal neighbors are ±2 apart
            // Diagonal neighbors are ±1 in both x and y
            if (Mathf.Abs(dx) > 1) dx = dx / Mathf.Abs(dx);  // Normalize: -2→-1, +2→+1, etc
            dy = Mathf.Clamp(dy, -1, 1);  // Vertical is already ±1

            Debug.Log($"[PlayerController] Normalized: dx:{dx}, dy:{dy}");

            // Map to 8 directions (even though diamond grid has only 6 neighbors)
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
            Debug.Log($"[PlayerController] Cell clicked: {cellId} (current: {currentCellId})");

            // Handle click-to-move with graceful degradation if GameManager doesn't exist
            bool canMove = true;

            // Check GameManager state if it exists
            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.CurrentState;
                Debug.Log($"[PlayerController] GameManager state: {state}");

                // Allow movement in InGame, Initializing (exploration), and Battle states
                if (state == GameState.InGame || state == GameState.Battle_RealTime || state == GameState.Initializing)
                {
                    canMove = true;
                    Debug.Log("[PlayerController] Movement allowed in current state");
                }
                else if (state == GameState.Battle_TurnBased)
                {
                    // Check if we have movement points
                    int distance = IsometricHelper.GetDistance(currentCellId, cellId);
                    Debug.Log($"[PlayerController] Turn-based mode: distance={distance}, MP={currentMovementPoints}");
                    canMove = distance <= currentMovementPoints;
                }
                else
                {
                    Debug.LogWarning($"[PlayerController] Cannot move in state: {state}");
                    canMove = false;
                }
            }
            else
            {
                // No GameManager - allow free movement (exploration mode)
                Debug.Log("[PlayerController] No GameManager found - allowing free movement");
                canMove = true;
            }

            if (canMove)
            {
                Debug.Log($"[PlayerController] canMove=true, calling RequestMove for cell {cellId}");
                bool success = RequestMove(cellId);
                Debug.Log($"[PlayerController] Move request result: {success}");
            }
            else
            {
                Debug.LogWarning($"[PlayerController] canMove=false, movement blocked");
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