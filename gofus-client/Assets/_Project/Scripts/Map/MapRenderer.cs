using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Core;
using GOFUS.Networking;

namespace GOFUS.Map
{
    [Serializable]
    public class MapData
    {
        public int Id;
        public string Name;
        public int Width = 14;
        public int Height = 20;
        public int[] Cells;
        public MapObject[] Objects;
        public string BackgroundMusic;
    }

    [Serializable]
    public class MapObject
    {
        public int CellId;
        public string Type;
        public string SpriteId;
        public bool IsInteractive;
    }

    public class MapRenderer : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private CellGrid grid;
        [SerializeField] private int currentMapId;

        [Header("Visuals")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject highlightPrefab;
        [SerializeField] private Material normalCellMaterial;
        [SerializeField] private Material obstacleCellMaterial;
        [SerializeField] private Color walkableColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color unwalkableColor = new Color(1, 0, 0, 0.3f);
        [SerializeField] private Color pathColor = new Color(0, 0, 1, 0.5f);
        [SerializeField] private Color rangeColor = new Color(1, 1, 0, 0.3f);

        [Header("Layers")]
        [SerializeField] private Transform groundLayer;
        [SerializeField] private Transform objectLayer;
        [SerializeField] private Transform highlightLayer;

        // Visual elements
        private Dictionary<int, GameObject> cellVisuals;
        private Dictionary<int, GameObject> highlightVisuals;
        private Dictionary<int, MapObject> mapObjects;

        // Properties
        public CellGrid Grid => grid;
        public int CurrentMapId => currentMapId;
        public bool IsInitialized { get; private set; }

        // Events
        public event Action<int> OnMapLoaded;
        public event Action<int> OnCellClicked;
        public event Action<int> OnCellHovered;

        private void Awake()
        {
            cellVisuals = new Dictionary<int, GameObject>();
            highlightVisuals = new Dictionary<int, GameObject>();
            mapObjects = new Dictionary<int, MapObject>();

            CreateLayers();
        }

        private void Start()
        {
            InitializeGrid();
        }

        private void CreateLayers()
        {
            if (groundLayer == null)
            {
                GameObject ground = new GameObject("GroundLayer");
                ground.transform.parent = transform;
                groundLayer = ground.transform;
            }

            if (objectLayer == null)
            {
                GameObject objects = new GameObject("ObjectLayer");
                objects.transform.parent = transform;
                objectLayer = objects.transform;
            }

            if (highlightLayer == null)
            {
                GameObject highlights = new GameObject("HighlightLayer");
                highlights.transform.parent = transform;
                highlightLayer = highlights.transform;
            }
        }

        public void InitializeGrid()
        {
            grid = new CellGrid();
            IsInitialized = true;
            Debug.Log($"Map grid initialized with {IsometricHelper.TOTAL_CELLS} cells");
        }

        public void InitializeForTests()
        {
            InitializeGrid();
        }

        public void LoadMap(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("Cannot load null map data");
                return;
            }

            currentMapId = mapData.Id;

            // Clear existing map
            ClearMap();

            // Ensure layers are set up (Awake might not have run if dynamically created)
            CreateLayers();

            // Initialize grid if needed
            if (grid == null)
                InitializeGrid();

            // Parse cell data
            for (int i = 0; i < mapData.Cells.Length && i < grid.Cells.Length; i++)
            {
                // Cell data format: walkability, movement cost, type
                int cellData = mapData.Cells[i];
                grid.Cells[i].IsWalkable = (cellData & 1) == 1;
                grid.Cells[i].MovementCost = ((cellData >> 1) & 7) + 1;
                grid.Cells[i].Type = (CellType)((cellData >> 4) & 15);
            }

            // Load map objects
            if (mapData.Objects != null)
            {
                foreach (var obj in mapData.Objects)
                {
                    mapObjects[obj.CellId] = obj;
                    if (!obj.IsInteractive)
                    {
                        grid.SetCellWalkable(obj.CellId, false);
                    }
                }
            }

            // Generate visual representation
            GenerateMapVisuals();

            Debug.Log($"Map {mapData.Name} (ID: {mapData.Id}) loaded successfully");
            OnMapLoaded?.Invoke(currentMapId);
        }

        public void LoadMapFromServer(int mapId)
        {
            StartCoroutine(LoadMapFromServerCoroutine(mapId));
        }

        // Public coroutine method that can be started by GameHUD (to avoid inactive GameObject issues)
        public IEnumerator LoadMapFromServerCoroutine(int mapId)
        {
            // Request map data from server
            string mapUrl = $"{NetworkManager.Instance.CurrentBackendUrl}/api/maps/{mapId}";

            Debug.Log($"[MapRenderer] Fetching map {mapId} from: {mapUrl}");

            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(mapUrl))
            {
                // Send the request
                yield return request.SendWebRequest();

                // Check for errors
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[MapRenderer] Failed to load map {mapId}: {request.error}");
                    Debug.LogWarning($"[MapRenderer] Falling back to test map for map {mapId}");

                    // Fallback to test map
                    var testMap = CreateTestMap(mapId);
                    LoadMap(testMap);
                }
                else
                {
                    // Successfully loaded from server
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log($"[MapRenderer] Received response: {json.Substring(0, Mathf.Min(200, json.Length))}...");

                        // Parse the backend response - backend returns { success, map }
                        GOFUS.Models.MapApiResponse apiResponse = JsonUtility.FromJson<GOFUS.Models.MapApiResponse>(json);

                        if (apiResponse != null && apiResponse.success && apiResponse.map != null && apiResponse.map.cells != null)
                        {
                            Debug.Log($"[MapRenderer] API response success: {apiResponse.success}, cells: {apiResponse.map.cells.Length}");

                            // Convert backend format to Unity format
                            MapData mapData = GOFUS.Models.MapDataConverter.ToUnityMapData(apiResponse.map);

                            if (mapData != null)
                            {
                                Debug.Log($"[MapRenderer] Successfully converted map {mapId} with {mapData.Cells.Length} cells");
                                LoadMap(mapData);
                            }
                            else
                            {
                                Debug.LogError("[MapRenderer] Conversion failed, using test map");
                                LoadMap(CreateTestMap(mapId));
                            }
                        }
                        else
                        {
                            Debug.LogError("[MapRenderer] Invalid response format, using test map");
                            LoadMap(CreateTestMap(mapId));
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[MapRenderer] Exception parsing map data: {e.Message}");
                        Debug.LogWarning($"[MapRenderer] Falling back to test map");
                        LoadMap(CreateTestMap(mapId));
                    }
                }
            }
        }

        private MapData CreateTestMap(int mapId)
        {
            var mapData = new MapData
            {
                Id = mapId,
                Name = $"Test Map {mapId}",
                Width = IsometricHelper.GRID_WIDTH,
                Height = IsometricHelper.GRID_HEIGHT,
                Cells = new int[IsometricHelper.TOTAL_CELLS]
            };

            // Initialize all cells as walkable
            for (int i = 0; i < mapData.Cells.Length; i++)
            {
                mapData.Cells[i] = 1; // Walkable
            }

            // Add some obstacles
            System.Random random = new System.Random(mapId);
            for (int i = 0; i < 20; i++)
            {
                int cellId = random.Next(IsometricHelper.TOTAL_CELLS);
                mapData.Cells[cellId] = 0; // Not walkable
            }

            return mapData;
        }

        private void GenerateMapVisuals()
        {
            Debug.Log($"[MapRenderer] Generating visuals for {grid.Cells.Length} cells");
            Debug.Log($"[MapRenderer] Ground Layer: {(groundLayer != null ? "EXISTS" : "NULL")}");
            Debug.Log($"[MapRenderer] Object Layer: {(objectLayer != null ? "EXISTS" : "NULL")}");
            Debug.Log($"[MapRenderer] Highlight Layer: {(highlightLayer != null ? "EXISTS" : "NULL")}");
            
            int walkableCount = 0;
            int obstacleCount = 0;
            
            for (int i = 0; i < grid.Cells.Length; i++)
            {
                CreateCellVisual(i);
                
                if (grid.Cells[i].IsWalkable)
                    walkableCount++;
                else
                    obstacleCount++;
                
                // Log first few cells for debugging
                if (i < 5)
                {
                    Vector3 pos = IsometricHelper.CellIdToWorldPosition(i);
                    Debug.Log($"[MapRenderer] Cell {i} at position {pos}, walkable: {grid.Cells[i].IsWalkable}");
                }
            }
            
            Debug.Log($"[MapRenderer] Created {cellVisuals.Count} cell visuals ({walkableCount} walkable, {obstacleCount} obstacles)");
            Debug.Log($"[MapRenderer] Map bounds approximately: X(-817 to 559), Y(0 to 688) world units");

            // Create map objects
            foreach (var kvp in mapObjects)
            {
                CreateMapObject(kvp.Key, kvp.Value);
            }
        }

        private void CreateCellVisual(int cellId)
        {
            Vector3 worldPos = IsometricHelper.CellIdToWorldPosition(cellId);

            // Create cell GameObject
            GameObject cellObject = CreateCellPrefab();
            cellObject.name = $"Cell_{cellId}";
            cellObject.transform.parent = groundLayer;
            cellObject.transform.position = worldPos;

            // Log creation details for debugging
            if (cellId < 10 || cellId % 100 == 0)
            {
                Debug.Log($"[MapRenderer] Creating cell {cellId} at {worldPos}");
            }

            // Set visual properties
            var renderer = cellObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // Don't set material if null - use default material
                if (normalCellMaterial != null && obstacleCellMaterial != null)
                {
                    renderer.material = grid.Cells[cellId].IsWalkable ? normalCellMaterial : obstacleCellMaterial;
                }
                
                renderer.sortingOrder = GetSortingOrder(cellId);
                renderer.sortingLayerName = "Default"; // Ensure on correct sorting layer

                // Set color based on cell type and walkability
                if (!grid.Cells[cellId].IsWalkable)
                {
                    renderer.color = new Color(0.5f, 0.5f, 0.5f, 0.9f); // Dark gray for obstacles
                }
                else
                {
                    switch (grid.Cells[cellId].Type)
                    {
                        case CellType.Water:
                            renderer.color = new Color(0.5f, 0.7f, 1f, 0.8f);
                            break;
                        case CellType.Lava:
                            renderer.color = new Color(1f, 0.3f, 0.1f, 0.8f);
                            break;
                        default:
                            renderer.color = new Color(0.9f, 0.9f, 0.9f, 0.8f); // Light gray for walkable
                            break;
                    }
                }
                
                // Log sprite details for first cell
                if (cellId == 0)
                {
                    Debug.Log($"[MapRenderer] Cell 0 sprite: {(renderer.sprite != null ? "EXISTS" : "NULL")}");
                    Debug.Log($"[MapRenderer] Cell 0 color: {renderer.color}");
                    Debug.Log($"[MapRenderer] Cell 0 sorting order: {renderer.sortingOrder}");
                    Debug.Log($"[MapRenderer] Cell 0 sorting layer: {renderer.sortingLayerName}");
                }
            }
            else
            {
                Debug.LogWarning($"[MapRenderer] No SpriteRenderer on cell {cellId}!");
            }

            // Add cell interaction
            var clickHandler = cellObject.AddComponent<CellClickHandler>();
            clickHandler.CellId = cellId;
            clickHandler.OnClick += HandleCellClick;
            clickHandler.OnHover += HandleCellHover;

            cellVisuals[cellId] = cellObject;

            // Create highlight visual
            CreateHighlightVisual(cellId, worldPos);
        }

        private GameObject CreateCellPrefab()
        {
            if (cellPrefab != null)
                return Instantiate(cellPrefab);

            // Create default cell if no prefab
            GameObject cell = new GameObject("Cell");
            var sr = cell.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDiamondSprite();

            var collider = cell.AddComponent<PolygonCollider2D>();

            return cell;
        }

        private void CreateHighlightVisual(int cellId, Vector3 position)
        {
            GameObject highlightObject = highlightPrefab != null ?
                Instantiate(highlightPrefab) :
                CreateDefaultHighlight();

            highlightObject.name = $"Highlight_{cellId}";
            highlightObject.transform.parent = highlightLayer;
            highlightObject.transform.position = position + Vector3.forward * -0.1f;
            highlightObject.SetActive(false);

            highlightVisuals[cellId] = highlightObject;
        }

        private GameObject CreateDefaultHighlight()
        {
            GameObject highlight = new GameObject("Highlight");
            var sr = highlight.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDiamondSprite();
            sr.color = new Color(1, 1, 1, 0.5f);
            sr.sortingLayerName = "Highlight";
            return highlight;
        }

        private void CreateMapObject(int cellId, MapObject mapObject)
        {
            Vector3 worldPos = IsometricHelper.CellIdToWorldPosition(cellId);

            GameObject obj = new GameObject($"Object_{cellId}");
            obj.transform.parent = objectLayer;
            obj.transform.position = worldPos + Vector3.up * 0.5f;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = GetSortingOrder(cellId) + 10;

            // In real implementation, load sprite from resources
            // sr.sprite = Resources.Load<Sprite>($"MapObjects/{mapObject.SpriteId}");
        }

        private int GetSortingOrder(int cellId)
        {
            Vector2Int coords = IsometricHelper.CellIdToGridCoords(cellId);
            return -(coords.x + coords.y);
        }

        public void HighlightCell(int cellId, Color color)
        {
            if (highlightVisuals.ContainsKey(cellId))
            {
                var highlight = highlightVisuals[cellId];
                highlight.SetActive(true);

                var sr = highlight.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = color;
                }
            }
        }

        public void ClearHighlight(int cellId)
        {
            if (highlightVisuals.ContainsKey(cellId))
            {
                highlightVisuals[cellId].SetActive(false);
            }
        }

        public bool IsHighlighted(int cellId)
        {
            return highlightVisuals.ContainsKey(cellId) && highlightVisuals[cellId].activeSelf;
        }

        public void HighlightPath(List<int> path, Color color)
        {
            foreach (int cellId in path)
            {
                HighlightCell(cellId, color);
            }
        }

        public void ClearAllHighlights()
        {
            foreach (var kvp in highlightVisuals)
            {
                kvp.Value.SetActive(false);
            }
        }

        public List<int> ShowRange(int centerCell, int range)
        {
            var cellsInRange = IsometricHelper.GetCellsInRange(centerCell, range);

            foreach (int cellId in cellsInRange)
            {
                if (grid.Cells[cellId].IsWalkable)
                {
                    HighlightCell(cellId, rangeColor);
                }
            }

            return cellsInRange;
        }

        public void ShowAreaOfEffect(int targetCell, AreaShape shape, int size, Color color)
        {
            var affectedCells = IsometricHelper.GetAreaOfEffect(targetCell, shape, size);

            foreach (int cellId in affectedCells)
            {
                HighlightCell(cellId, color);
            }
        }

        public void ShowMovementRange(int originCell, int movementPoints)
        {
            // Use pathfinding to determine actual reachable cells
            var reachableCells = CalculateReachableCells(originCell, movementPoints);

            foreach (int cellId in reachableCells)
            {
                HighlightCell(cellId, walkableColor);
            }
        }

        private List<int> CalculateReachableCells(int origin, int movementPoints)
        {
            var reachable = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<(int cell, int cost)>();

            queue.Enqueue((origin, 0));
            visited.Add(origin);

            while (queue.Count > 0)
            {
                var (currentCell, currentCost) = queue.Dequeue();

                if (currentCost <= movementPoints)
                {
                    reachable.Add(currentCell);

                    var neighbors = IsometricHelper.GetNeighborCells(currentCell);
                    foreach (int neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor) && IsometricHelper.IsCellWalkable(neighbor, grid))
                        {
                            int newCost = currentCost + grid.Cells[neighbor].MovementCost;
                            if (newCost <= movementPoints)
                            {
                                queue.Enqueue((neighbor, newCost));
                                visited.Add(neighbor);
                            }
                        }
                    }
                }
            }

            return reachable;
        }

        private void ClearMap()
        {
            // Clear visual objects
            if (cellVisuals != null)
            {
                foreach (var kvp in cellVisuals)
                {
                    if (kvp.Value != null)
                        Destroy(kvp.Value);
                }
                cellVisuals.Clear();
            }

            if (highlightVisuals != null)
            {
                foreach (var kvp in highlightVisuals)
                {
                    if (kvp.Value != null)
                        Destroy(kvp.Value);
                }
                highlightVisuals.Clear();
            }

            if (mapObjects != null)
            {
                mapObjects.Clear();
            }
        }

        private void HandleCellClick(int cellId)
        {
            Debug.Log($"[MapRenderer] HandleCellClick called for cell {cellId}");
            Debug.Log($"[MapRenderer] OnCellClicked has {(OnCellClicked != null ? OnCellClicked.GetInvocationList().Length : 0)} subscribers");
            OnCellClicked?.Invoke(cellId);
        }

        private void HandleCellHover(int cellId)
        {
            Debug.Log($"[MapRenderer] HandleCellHover called for cell {cellId}");
            OnCellHovered?.Invoke(cellId);
        }

        private Sprite CreateDiamondSprite()
        {
            // Create a diamond-shaped sprite programmatically with MUCH LARGER size
            // Increased from 86x43 to 200x100 for better visibility
            int width = 200;
            int height = 100;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[texture.width * texture.height];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw diamond shape
            int centerX = texture.width / 2;
            int centerY = texture.height / 2;

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distX = Mathf.Abs(x - centerX) / (float)centerX;
                    float distY = Mathf.Abs(y - centerY) / (float)centerY;

                    if (distX + distY * 2 <= 1)
                    {
                        int index = y * texture.width + x;
                        // Make cells VERY visible: bright and opaque
                        pixels[index] = new Color(1f, 1f, 1f, 1f); // Pure white, fully opaque
                        
                        // Add thick border for visibility
                        if (distX + distY * 2 >= 0.85f)
                        {
                            pixels[index] = new Color(0f, 0f, 0f, 1.0f); // Black border
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point; // Crisp edges

            // Pixels per unit determines the size in world space
            // Lower value = larger sprite in world
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 50f);
        }

        private void OnDestroy()
        {
            ClearMap();
        }
    }

    public class CellClickHandler : MonoBehaviour
    {
        public int CellId { get; set; }
        public event Action<int> OnClick;
        public event Action<int> OnHover;

        private void OnMouseDown()
        {
            Debug.Log($"[CellClickHandler] Cell {CellId} clicked via OnMouseDown!");
            TriggerClick();
        }

        private void OnMouseEnter()
        {
            Debug.Log($"[CellClickHandler] Mouse entered cell {CellId}");
            OnHover?.Invoke(CellId);
        }

        private void OnMouseExit()
        {
            Debug.Log($"[CellClickHandler] Mouse exited cell {CellId}");
        }

        /// <summary>
        /// Manually trigger a click event (for testing or programmatic clicks)
        /// </summary>
        public void TriggerClick()
        {
            Debug.Log($"[CellClickHandler] TriggerClick() called for cell {CellId}");
            OnClick?.Invoke(CellId);
        }
    }
}