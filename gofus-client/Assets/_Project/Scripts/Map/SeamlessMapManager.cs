using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GOFUS.Core;

namespace GOFUS.Map
{
    /// <summary>
    /// Manages seamless map transitions for unlimited world experience
    /// Automatically loads adjacent maps and transitions without loading screens
    /// </summary>
    public class SeamlessMapManager : Singleton<SeamlessMapManager>
    {
        #region Properties

        public int CurrentMapId { get; private set; }
        public MapData CurrentMapData { get; private set; }
        public Vector2 PlayerPosition { get; private set; }
        public bool IsTransitioning { get; private set; }
        public bool IsInputEnabled { get; private set; } = true;
        public bool IsShowingLoadingIndicator { get; private set; }

        private Dictionary<int, MapData> loadedMaps = new Dictionary<int, MapData>();
        private Dictionary<int, MapConnections> mapConnections = new Dictionary<int, MapConnections>();
        private Dictionary<string, GameObject> mapLayers = new Dictionary<string, GameObject>();
        private Dictionary<int, MapEntity> entities = new Dictionary<int, MapEntity>();
        private Queue<int> mapLoadQueue = new Queue<int>();
        private List<int> prioritizedMaps = new List<int>();

        private int cacheSize = 5;
        private int largeMapThreshold = 1000;
        private float edgeDetectionDistance = 10f;
        private TransitionEffect currentTransitionEffect = TransitionEffect.Fade;

        private bool objectPoolingEnabled = false;
        private Queue<GameObject> pooledObjects = new Queue<GameObject>();
        private int activeObjectCount = 0;
        private int drawCallCount = 0;

        #endregion

        #region Events

        public event Action<int> OnMapLoadStart;
        public event Action<int> OnMapLoadComplete;
        public event Action<int, int> OnMapTransitioned;
        public event Action OnTransitionStart;
        public event Action OnTransitionComplete;

        #endregion

        #region Initialization

        public void Initialize()
        {
            InitializeMapLayers();
            StartCoroutine(MapPreloadingCoroutine());
        }

        private void InitializeMapLayers()
        {
            mapLayers["Ground"] = new GameObject("Ground Layer");
            mapLayers["Obstacles"] = new GameObject("Obstacles Layer");
            mapLayers["Decorations"] = new GameObject("Decorations Layer");
            mapLayers["Overhead"] = new GameObject("Overhead Layer");

            foreach (var layer in mapLayers.Values)
            {
                layer.transform.SetParent(transform);
            }
        }

        #endregion

        #region Map Loading

        public void LoadMap(int mapId)
        {
            if (loadedMaps.ContainsKey(mapId))
            {
                SetCurrentMap(mapId);
                return;
            }

            OnMapLoadStart?.Invoke(mapId);

            MapData mapData = LoadMapData(mapId);
            loadedMaps[mapId] = mapData;
            SetCurrentMap(mapId);

            OnMapLoadComplete?.Invoke(mapId);

            // Preload adjacent maps
            PreloadAdjacentMaps(mapId);
        }

        public void LoadMapAsync(int mapId)
        {
            StartCoroutine(LoadMapAsyncCoroutine(mapId));
        }

        private IEnumerator LoadMapAsyncCoroutine(int mapId)
        {
            OnMapLoadStart?.Invoke(mapId);

            yield return new WaitForSeconds(0.5f); // Simulate loading

            MapData mapData = LoadMapData(mapId);
            loadedMaps[mapId] = mapData;

            OnMapLoadComplete?.Invoke(mapId);
        }

        public void LoadLargeMap(int mapId, int tileCount)
        {
            if (tileCount > largeMapThreshold)
            {
                IsShowingLoadingIndicator = true;
            }

            LoadMapAsync(mapId);
        }

        public void CompleteAsyncLoad(int mapId)
        {
            IsShowingLoadingIndicator = false;
            OnMapLoadComplete?.Invoke(mapId);
        }

        private MapData LoadMapData(int mapId)
        {
            // Create mock map data
            return new MapData
            {
                Id = mapId,
                Width = 100,
                Height = 100,
                Cells = new int[100 * 100],
                Name = $"Map_{mapId}"
            };
        }

        private void SetCurrentMap(int mapId)
        {
            CurrentMapId = mapId;
            CurrentMapData = loadedMaps[mapId];

            // Update map cache priority
            ManageMapCache();
        }

        #endregion

        #region Map Connections

        public void SetMapConnections(int mapId, MapConnections connections)
        {
            mapConnections[mapId] = connections;
        }

        public void SetMapSize(float width, float height)
        {
            if (CurrentMapData != null)
            {
                CurrentMapData.Width = Mathf.RoundToInt(width);
                CurrentMapData.Height = Mathf.RoundToInt(height);
            }
        }

        #endregion

        #region Player Position & Edge Detection

        public void UpdatePlayerPosition(Vector2 position)
        {
            Vector2 previousPosition = PlayerPosition;
            PlayerPosition = position;

            // Check for edge proximity
            var edgeCheck = IsPlayerNearEdge(position);
            if (edgeCheck.isNearEdge)
            {
                HandleEdgeApproach(edgeCheck.edge);
            }

            // Check for map transition
            CheckForMapTransition(position);
        }

        public (bool isNearEdge, MapEdge edge) IsPlayerNearEdge(Vector2 position)
        {
            if (CurrentMapData == null) return (false, MapEdge.None);

            float x = position.x;
            float y = position.y;
            float width = CurrentMapData.Width;
            float height = CurrentMapData.Height;

            // Check each edge
            if (x >= width - edgeDetectionDistance)
            {
                if (y >= height - edgeDetectionDistance)
                    return (true, MapEdge.TopRight);
                else if (y <= edgeDetectionDistance)
                    return (true, MapEdge.BottomRight);
                else
                    return (true, MapEdge.Right);
            }
            else if (x <= edgeDetectionDistance)
            {
                if (y >= height - edgeDetectionDistance)
                    return (true, MapEdge.TopLeft);
                else if (y <= edgeDetectionDistance)
                    return (true, MapEdge.BottomLeft);
                else
                    return (true, MapEdge.Left);
            }
            else if (y >= height - edgeDetectionDistance)
            {
                return (true, MapEdge.Top);
            }
            else if (y <= edgeDetectionDistance)
            {
                return (true, MapEdge.Bottom);
            }

            return (false, MapEdge.None);
        }

        private void HandleEdgeApproach(MapEdge edge)
        {
            if (!mapConnections.ContainsKey(CurrentMapId)) return;

            var connections = mapConnections[CurrentMapId];
            int adjacentMapId = GetAdjacentMapId(connections, edge);

            if (adjacentMapId != -1 && !IsMapInMemory(adjacentMapId))
            {
                // Preload the adjacent map
                LoadMapAsync(adjacentMapId);
            }
        }

        private int GetAdjacentMapId(MapConnections connections, MapEdge edge)
        {
            switch (edge)
            {
                case MapEdge.Top: return connections.North;
                case MapEdge.Bottom: return connections.South;
                case MapEdge.Left: return connections.West;
                case MapEdge.Right: return connections.East;
                case MapEdge.TopLeft: return connections.NorthWest;
                case MapEdge.TopRight: return connections.NorthEast;
                case MapEdge.BottomLeft: return connections.SouthWest;
                case MapEdge.BottomRight: return connections.SouthEast;
                default: return -1;
            }
        }

        private void CheckForMapTransition(Vector2 position)
        {
            if (IsTransitioning || CurrentMapData == null) return;

            float x = position.x;
            float y = position.y;
            float width = CurrentMapData.Width;
            float height = CurrentMapData.Height;

            int targetMapId = -1;
            Vector2 newPosition = position;

            // Detect edge crossing
            if (x >= width - 0.5f)
            {
                targetMapId = GetAdjacentMapId(MapEdge.Right);
                newPosition.x = 0.5f; // Appear on left side of new map
            }
            else if (x <= 0.5f)
            {
                targetMapId = GetAdjacentMapId(MapEdge.Left);
                newPosition.x = width - 0.5f; // Appear on right side of new map
            }
            else if (y >= height - 0.5f)
            {
                targetMapId = GetAdjacentMapId(MapEdge.Top);
                newPosition.y = 0.5f; // Appear on bottom of new map
            }
            else if (y <= 0.5f)
            {
                targetMapId = GetAdjacentMapId(MapEdge.Bottom);
                newPosition.y = height - 0.5f; // Appear on top of new map
            }

            if (targetMapId != -1)
            {
                TriggerTransition(targetMapId);
                PlayerPosition = newPosition; // Update position for seamless transition
            }
        }

        private int GetAdjacentMapId(MapEdge edge)
        {
            if (!mapConnections.ContainsKey(CurrentMapId)) return -1;
            return GetAdjacentMapId(mapConnections[CurrentMapId], edge);
        }

        #endregion

        #region Map Transitions

        public void TriggerTransition(int targetMapId)
        {
            if (IsTransitioning) return;

            StartCoroutine(TransitionToMap(targetMapId));
        }

        public void StartTransition(int targetMapId)
        {
            IsTransitioning = true;
            IsInputEnabled = false;
            OnTransitionStart?.Invoke();
        }

        public void CompleteTransition()
        {
            IsTransitioning = false;
            IsInputEnabled = true;
            OnTransitionComplete?.Invoke();
        }

        private IEnumerator TransitionToMap(int targetMapId)
        {
            StartTransition(targetMapId);

            // Apply transition effect
            yield return StartCoroutine(ApplyTransitionEffect());

            // Load new map if not already loaded
            if (!IsMapLoaded(targetMapId))
            {
                LoadMap(targetMapId);
            }
            else
            {
                SetCurrentMap(targetMapId);
            }

            // Transfer entities
            TransferEntitiesToNewMap(targetMapId);

            // Notify transition complete
            OnMapTransitioned?.Invoke(CurrentMapId, targetMapId);

            CompleteTransition();
        }

        private IEnumerator ApplyTransitionEffect()
        {
            switch (currentTransitionEffect)
            {
                case TransitionEffect.Fade:
                    // Implement fade effect
                    yield return new WaitForSeconds(0.3f);
                    break;

                case TransitionEffect.Slide:
                    // Implement slide effect
                    yield return new WaitForSeconds(0.5f);
                    break;

                default:
                    yield return null;
                    break;
            }
        }

        public void SetTransitionEffect(TransitionEffect effect)
        {
            currentTransitionEffect = effect;
        }

        #endregion

        #region Map Preloading

        private void PreloadAdjacentMaps(int mapId)
        {
            if (!mapConnections.ContainsKey(mapId)) return;

            var connections = mapConnections[mapId];

            // Add all adjacent maps to preload queue
            if (connections.North > 0) mapLoadQueue.Enqueue(connections.North);
            if (connections.South > 0) mapLoadQueue.Enqueue(connections.South);
            if (connections.East > 0) mapLoadQueue.Enqueue(connections.East);
            if (connections.West > 0) mapLoadQueue.Enqueue(connections.West);
        }

        private IEnumerator MapPreloadingCoroutine()
        {
            while (true)
            {
                if (mapLoadQueue.Count > 0)
                {
                    int mapId = mapLoadQueue.Dequeue();
                    if (!IsMapInMemory(mapId))
                    {
                        yield return StartCoroutine(LoadMapAsyncCoroutine(mapId));
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion

        #region Map Cache Management

        public void SetCacheSize(int size)
        {
            cacheSize = size;
            ManageMapCache();
        }

        private void ManageMapCache()
        {
            if (loadedMaps.Count <= cacheSize) return;

            // Get maps sorted by priority (current map and adjacent maps have highest priority)
            var sortedMaps = loadedMaps.Keys.ToList();

            // Remove current and adjacent maps from removal candidates
            sortedMaps.Remove(CurrentMapId);

            if (mapConnections.ContainsKey(CurrentMapId))
            {
                var connections = mapConnections[CurrentMapId];
                sortedMaps.Remove(connections.North);
                sortedMaps.Remove(connections.South);
                sortedMaps.Remove(connections.East);
                sortedMaps.Remove(connections.West);
            }

            // Remove excess maps
            while (loadedMaps.Count > cacheSize && sortedMaps.Count > 0)
            {
                int mapToRemove = sortedMaps[0];
                loadedMaps.Remove(mapToRemove);
                sortedMaps.RemoveAt(0);
            }
        }

        public bool IsPrioritized(int mapId)
        {
            if (mapId == CurrentMapId) return true;

            if (mapConnections.ContainsKey(CurrentMapId))
            {
                var connections = mapConnections[CurrentMapId];
                return mapId == connections.North ||
                       mapId == connections.South ||
                       mapId == connections.East ||
                       mapId == connections.West;
            }

            return false;
        }

        #endregion

        #region Map State Queries

        public bool IsMapLoaded(int mapId)
        {
            return loadedMaps.ContainsKey(mapId);
        }

        public bool IsMapInMemory(int mapId)
        {
            return loadedMaps.ContainsKey(mapId);
        }

        public bool IsMapPreloading(int mapId)
        {
            return mapLoadQueue.Contains(mapId);
        }

        public Dictionary<string, GameObject> GetMapLayers()
        {
            return new Dictionary<string, GameObject>(mapLayers);
        }

        public void SetLargeMapThreshold(int threshold)
        {
            largeMapThreshold = threshold;
        }

        #endregion

        #region Entity Management

        public void AddEntity(int id, EntityType type, Vector2 position)
        {
            entities[id] = new MapEntity
            {
                Id = id,
                Type = type,
                Position = position,
                MapId = CurrentMapId
            };
        }

        public void AddDetailedEntity(MapEntity entity)
        {
            entity.MapId = CurrentMapId;
            entities[entity.Id] = entity;
        }

        public MapEntity GetEntity(int id)
        {
            return entities.ContainsKey(id) ? entities[id] : null;
        }

        public bool IsEntityOnMap(int entityId, int mapId)
        {
            if (!entities.ContainsKey(entityId)) return false;
            return entities[entityId].MapId == mapId;
        }

        private void TransferEntitiesToNewMap(int newMapId)
        {
            foreach (var entity in entities.Values)
            {
                // Transfer entities that are near the edge
                var edgeCheck = IsPlayerNearEdge(entity.Position);
                if (edgeCheck.isNearEdge || entity.Type == EntityType.Player)
                {
                    entity.MapId = newMapId;

                    // Adjust position for new map
                    AdjustEntityPositionForNewMap(entity, newMapId);
                }
            }
        }

        private void AdjustEntityPositionForNewMap(MapEntity entity, int newMapId)
        {
            // Adjust entity position based on transition direction
            // This ensures continuity across map boundaries
            if (entity.Position.x >= CurrentMapData.Width - 1)
            {
                entity.Position = new Vector2(1, entity.Position.y);
            }
            else if (entity.Position.x <= 1)
            {
                entity.Position = new Vector2(CurrentMapData.Width - 1, entity.Position.y);
            }
            else if (entity.Position.y >= CurrentMapData.Height - 1)
            {
                entity.Position = new Vector2(entity.Position.x, 1);
            }
            else if (entity.Position.y <= 1)
            {
                entity.Position = new Vector2(entity.Position.x, CurrentMapData.Height - 1);
            }
        }

        #endregion

        #region Performance Optimization

        public void EnableObjectPooling(bool enable)
        {
            objectPoolingEnabled = enable;
        }

        public int GetDrawCallCount()
        {
            // Simulate draw call counting
            return drawCallCount;
        }

        public int GetActiveObjectCount()
        {
            return activeObjectCount;
        }

        private void RecycleMapObjects()
        {
            if (!objectPoolingEnabled) return;

            // Implement object recycling logic
            foreach (var obj in pooledObjects)
            {
                obj.SetActive(false);
            }
        }

        private GameObject GetPooledObject()
        {
            if (pooledObjects.Count > 0)
            {
                var obj = pooledObjects.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return new GameObject("PooledObject");
        }

        #endregion
    }

    #region Supporting Classes

    public class MapConnections
    {
        public int North = -1;
        public int South = -1;
        public int East = -1;
        public int West = -1;
        public int NorthEast = -1;
        public int NorthWest = -1;
        public int SouthEast = -1;
        public int SouthWest = -1;
    }

    public enum MapEdge
    {
        None, Top, Bottom, Left, Right,
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    public enum TransitionEffect
    {
        None, Fade, Slide, Zoom
    }

    public enum EntityType
    {
        Player, NPC, Monster, Item
    }

    public class MapEntity
    {
        public int Id;
        public EntityType Type;
        public Vector2 Position;
        public int Health;
        public string State;
        public int MapId;
    }

    #endregion
}