using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GOFUS.Core;
using GOFUS.Map;

namespace GOFUS.Entities
{
    /// <summary>
    /// Manages all entities in the game world
    /// Provides efficient lookups and spatial queries
    /// </summary>
    public class EntityManager : Singleton<EntityManager>
    {
        private Dictionary<int, Entity> entities;
        private Dictionary<int, Entity> cellToEntity;
        private Dictionary<EntityType, List<Entity>> entitiesByType;
        private HashSet<int> dirtyEntities;
        private int nextEntityId = 1;

        // Properties
        public int EntityCount => entities?.Count ?? 0;

        protected override void Awake()
        {
            base.Awake();
            InitializeCollections();
        }

        private void InitializeCollections()
        {
            entities = new Dictionary<int, Entity>();
            cellToEntity = new Dictionary<int, Entity>();
            entitiesByType = new Dictionary<EntityType, List<Entity>>();
            dirtyEntities = new HashSet<int>();

            // Initialize type lists
            foreach (EntityType type in Enum.GetValues(typeof(EntityType)))
            {
                entitiesByType[type] = new List<Entity>();
            }
        }

        public Entity CreateEntity(EntityType type, int cellId)
        {
            // Create entity GameObject
            GameObject entityObject = new GameObject($"Entity_{type}_{nextEntityId}");
            entityObject.transform.parent = transform;

            // Add Entity component
            Entity entity = entityObject.AddComponent<Entity>();
            entity.Initialize(nextEntityId++, type, cellId);

            // Register entity
            RegisterEntity(entity);

            // Position in world
            Vector3 worldPos = IsometricHelper.CellIdToWorldPosition(cellId);
            entityObject.transform.position = worldPos;

            return entity;
        }

        private void RegisterEntity(Entity entity)
        {
            entities[entity.Id] = entity;

            if (entity.CellId >= 0)
            {
                cellToEntity[entity.CellId] = entity;
            }

            if (entitiesByType.ContainsKey(entity.Type))
            {
                entitiesByType[entity.Type].Add(entity);
            }
        }

        public bool RemoveEntity(int entityId)
        {
            if (!entities.TryGetValue(entityId, out Entity entity))
                return false;

            // Remove from cell mapping
            if (cellToEntity.ContainsKey(entity.CellId))
            {
                cellToEntity.Remove(entity.CellId);
            }

            // Remove from type list
            if (entitiesByType.ContainsKey(entity.Type))
            {
                entitiesByType[entity.Type].Remove(entity);
            }

            // Remove from dirty set
            dirtyEntities.Remove(entityId);

            // Remove from main registry
            entities.Remove(entityId);

            // Destroy GameObject
            if (entity.gameObject != null)
            {
                Destroy(entity.gameObject);
            }

            return true;
        }

        public Entity GetEntity(int entityId)
        {
            return entities.TryGetValue(entityId, out Entity entity) ? entity : null;
        }

        public Entity GetEntityAtCell(int cellId)
        {
            return cellToEntity.TryGetValue(cellId, out Entity entity) ? entity : null;
        }

        public List<Entity> GetAllEntities()
        {
            return new List<Entity>(entities.Values);
        }

        public List<Entity> GetEntitiesOfType(EntityType type)
        {
            return entitiesByType.ContainsKey(type) ?
                new List<Entity>(entitiesByType[type]) :
                new List<Entity>();
        }

        public List<Entity> GetEntitiesInRange(int centerCell, int range)
        {
            List<Entity> result = new List<Entity>();

            foreach (var entity in entities.Values)
            {
                int distance = IsometricHelper.GetDistance(centerCell, entity.CellId);
                if (distance <= range)
                {
                    result.Add(entity);
                }
            }

            return result;
        }

        public bool MoveEntity(int entityId, int newCellId)
        {
            if (!entities.TryGetValue(entityId, out Entity entity))
                return false;

            // Remove from old cell
            if (cellToEntity.ContainsKey(entity.CellId))
            {
                cellToEntity.Remove(entity.CellId);
            }

            // Update position
            entity.CellId = newCellId;

            // Add to new cell
            cellToEntity[newCellId] = entity;

            // Update world position
            Vector3 worldPos = IsometricHelper.CellIdToWorldPosition(newCellId);
            entity.transform.position = worldPos;

            // Mark as dirty for network sync
            MarkEntityDirty(entityId);

            return true;
        }

        public void MarkEntityDirty(int entityId)
        {
            dirtyEntities.Add(entityId);
        }

        public List<int> GetDirtyEntities()
        {
            return new List<int>(dirtyEntities);
        }

        public void ClearDirtyFlags()
        {
            dirtyEntities.Clear();
        }

        public EntityData SerializeEntity(int entityId)
        {
            if (!entities.TryGetValue(entityId, out Entity entity))
                return null;

            var data = new EntityData
            {
                Id = entity.Id,
                Type = entity.Type,
                CellId = entity.CellId,
                Components = new List<ComponentData>()
            };

            // Serialize components
            var stats = entity.GetComponent<EntityStats>();
            if (stats != null)
            {
                data.Components.Add(new ComponentData
                {
                    Type = "EntityStats",
                    Data = JsonUtility.ToJson(stats)
                });
            }

            var combat = entity.GetComponent<EntityCombat>();
            if (combat != null)
            {
                data.Components.Add(new ComponentData
                {
                    Type = "EntityCombat",
                    Data = JsonUtility.ToJson(combat)
                });
            }

            return data;
        }

        public Entity DeserializeEntity(EntityData data)
        {
            if (data == null)
                return null;

            // Create entity
            var entity = CreateEntity(data.Type, data.CellId);

            // Restore components
            foreach (var compData in data.Components)
            {
                switch (compData.Type)
                {
                    case "EntityStats":
                        var stats = entity.gameObject.AddComponent<EntityStats>();
                        JsonUtility.FromJsonOverwrite(compData.Data, stats);
                        break;
                    case "EntityCombat":
                        var combat = entity.gameObject.AddComponent<EntityCombat>();
                        JsonUtility.FromJsonOverwrite(compData.Data, combat);
                        break;
                }
            }

            return entity;
        }

        // Cleanup
        protected override void OnDestroy()
        {
            foreach (var entity in entities.Values.ToList())
            {
                RemoveEntity(entity.Id);
            }
        }
    }

    /// <summary>
    /// Base entity component
    /// </summary>
    public class Entity : MonoBehaviour
    {
        public int Id { get; private set; }
        public EntityType Type { get; private set; }
        public int CellId { get; set; }

        public void Initialize(int id, EntityType type, int cellId)
        {
            Id = id;
            Type = type;
            CellId = cellId;
        }

        public T AddComponent<T>() where T : Component
        {
            return gameObject.AddComponent<T>();
        }

        public new T GetComponent<T>() where T : Component
        {
            return gameObject.GetComponent<T>();
        }
    }

    /// <summary>
    /// Entity stats component
    /// </summary>
    [Serializable]
    public class EntityStats : MonoBehaviour
    {
        public int MaxHealth = 100;
        public int CurrentHealth = 100;
        public int MaxMana = 50;
        public int CurrentMana = 50;
        public int Level = 1;
        public int Experience = 0;

        public bool IsAlive => CurrentHealth > 0;

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }

        public void UseMana(int amount)
        {
            CurrentMana = Mathf.Max(0, CurrentMana - amount);
        }

        public void RestoreMana(int amount)
        {
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        }
    }

    /// <summary>
    /// Entity combat component
    /// </summary>
    [Serializable]
    public class EntityCombat : MonoBehaviour
    {
        public int AttackPower = 10;
        public int Defense = 5;
        public float CriticalChance = 10f;
        public float AttackSpeed = 1f;
        public int Range = 1;

        public int CalculateDamage(bool isCritical)
        {
            int damage = AttackPower;

            if (isCritical)
            {
                damage *= 2;
            }

            return damage;
        }

        public bool RollCritical()
        {
            return UnityEngine.Random.Range(0f, 100f) < CriticalChance;
        }
    }

    /// <summary>
    /// Entity movement component
    /// </summary>
    [Serializable]
    public class EntityMovement : MonoBehaviour
    {
        public float Speed = 5f;
        public List<int> CurrentPath { get; private set; }
        public int PathIndex { get; private set; }
        public bool IsMoving { get; private set; }

        public bool HasPath => CurrentPath != null && CurrentPath.Count > 0;
        public int PathLength => CurrentPath?.Count ?? 0;

        public void SetPath(List<int> path)
        {
            CurrentPath = path;
            PathIndex = 0;
            IsMoving = path != null && path.Count > 0;
        }

        public int GetNextCell()
        {
            if (!HasPath || PathIndex >= CurrentPath.Count)
                return -1;

            return CurrentPath[PathIndex];
        }

        public void AdvancePath()
        {
            PathIndex++;

            if (PathIndex >= CurrentPath.Count)
            {
                CurrentPath = null;
                PathIndex = 0;
                IsMoving = false;
            }
        }

        public void ClearPath()
        {
            CurrentPath = null;
            PathIndex = 0;
            IsMoving = false;
        }
    }

    public enum EntityType
    {
        Player,
        Monster,
        NPC,
        Pet,
        Mount,
        Merchant,
        Interactive
    }

    [Serializable]
    public class EntityData
    {
        public int Id;
        public EntityType Type;
        public int CellId;
        public List<ComponentData> Components;
    }

    [Serializable]
    public class ComponentData
    {
        public string Type;
        public string Data;
    }
}