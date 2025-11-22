using System;
using UnityEngine;

namespace GOFUS.Models
{
    /// <summary>
    /// Response models for map data from backend API
    /// Matches the production database schema
    /// IMPORTANT: Field names must match backend JSON exactly for JsonUtility
    /// </summary>

    /// <summary>
    /// Top-level API response wrapper
    /// </summary>
    [Serializable]
    public class MapApiResponse
    {
        public bool success;
        public MapDataResponse map;
    }

    /// <summary>
    /// Map data from backend - matches Drizzle schema
    /// </summary>
    [Serializable]
    public class MapDataResponse
    {
        public int id;
        public int x;                   // World X coordinate
        public int y;                   // World Y coordinate
        public int width;               // Grid width (14)
        public int height;              // Grid height (20)
        public int subAreaId;           // camelCase to match backend JSON
        public int musicId;             // camelCase to match backend JSON
        public int capabilities;
        public int outdoor;             // int not bool (backend stores as 0/1)
        public int backgroundNum;       // camelCase to match backend JSON
        public CellDataDTO[] cells;     // 560 cells
        // interactives and fightPositions not used yet
    }

    /// <summary>
    /// <summary>
    /// Individual cell data - matches actual API response
    /// API returns: id, level, walkable, movementCost
    /// Missing: cellId, coordX, coordY, lineOfSight, interactive
    /// </summary>
    [Serializable]
    public class CellDataDTO
    {
        // What the API actually returns
        public int id;                  // Cell ID (0-279 currently, should be 0-559)
        public bool walkable;
        public int level;               // Height level (0-15)
        public int movementCost;        // AP cost (1-10)
        
        // Optional fields (may not be in API response)
        public bool lineOfSight;        // Default to true if missing
        public bool interactive;        // Default to false if missing
        
        // Computed fields (calculated from id if missing)
        public int cellId => id;        // Use id as cellId
        public int coordX => id % 14;   // Calculate X from ID
        public int coordY => id / 14;   // Calculate Y from ID
    }

    [Serializable]
    public class Interactive
    {
        public int cellId;
        public string type;
        public int id;
    }

    /// <summary>
    /// Conversion utilities for backend → Unity format
    /// </summary>
    public static class MapDataConverter
    {
        /// <summary>
        /// Convert backend MapDataResponse to Unity MapData format
        /// </summary>
        public static Map.MapData ToUnityMapData(MapDataResponse response)
        {
            if (response == null)
            {
                Debug.LogError("[MapDataConverter] Response is null");
                return null;
            }

            int expectedCells = 560; // 14x20 grid
            int actualCells = response.cells != null ? response.cells.Length : 0;
            
            Debug.Log($"[MapDataConverter] API returned {actualCells} cells, expected {expectedCells}");

            var mapData = new Map.MapData
            {
                Id = response.id,
                Name = $"Map {response.id} [{response.x},{response.y}]",
                Width = response.width,
                Height = response.height,
                Cells = new int[expectedCells] // Always create 560 cells
            };

            // Initialize all cells as walkable by default
            for (int i = 0; i < expectedCells; i++)
            {
                mapData.Cells[i] = 1; // Default: walkable
            }

            if (response.cells == null || response.cells.Length == 0)
            {
                Debug.LogWarning("[MapDataConverter] No cell data in response, using defaults");
                return mapData;
            }

            Debug.Log($"[MapDataConverter] Converting {response.cells.Length} cells from backend format");

            // Convert cell data to Unity format
            // Unity MapData.Cells is int[] where each int encodes cell properties
            // Format: bit 0 = walkable, bits 1-3 = movementCost, bits 4-7 = type
            for (int i = 0; i < response.cells.Length && i < expectedCells; i++)
            {
                var cell = response.cells[i];
                int cellValue = 0;

                // Bit 0: walkable
                if (cell.walkable)
                    cellValue |= 1;

                // Bits 1-3: movement cost (0-7)
                int cost = Mathf.Clamp(cell.movementCost - 1, 0, 7);
                cellValue |= (cost << 1);

                // Bits 4-7: cell type (0 = normal, 1 = obstacle, 2 = interactive)
                int cellType = cell.interactive ? 2 : (cell.walkable ? 0 : 1);
                cellValue |= (cellType << 4);

                // Use cell.id as the index (which matches cellId)
                int index = cell.id;
                if (index >= 0 && index < expectedCells)
                {
                    mapData.Cells[index] = cellValue;
                }
                else
                {
                    Debug.LogWarning($"[MapDataConverter] Cell {i} has invalid id {cell.id}, skipping");
                }
            }

            // If we have fewer cells than expected, duplicate the pattern to fill the map
            if (actualCells < expectedCells && actualCells > 0)
            {
                Debug.LogWarning($"[MapDataConverter] Only {actualCells} cells provided, filling remaining with pattern");
                for (int i = actualCells; i < expectedCells; i++)
                {
                    // Copy from earlier cells in a repeating pattern
                    mapData.Cells[i] = mapData.Cells[i % actualCells];
                }
            }

            // No map objects for now (will be added later if needed)
            mapData.Objects = new Map.MapObject[0];

            Debug.Log($"[MapDataConverter] Conversion complete: {mapData.Name}");
            return mapData;
        }

        /// <summary>
        /// Calculate adjacent map ID from coordinates
        /// In production, use backend adjacentMaps field instead
        /// </summary>
        public static int? GetAdjacentMapId(int currentX, int currentY, Map.MapEdge edge, MapDataResponse[] allMaps)
        {
            int targetX = currentX;
            int targetY = currentY;

            switch (edge)
            {
                case Map.MapEdge.Right:
                    targetX++;
                    break;
                case Map.MapEdge.Left:
                    targetX--;
                    break;
                case Map.MapEdge.Top:
                    targetY--;  // Y decreases going north
                    break;
                case Map.MapEdge.Bottom:
                    targetY++;  // Y increases going south
                    break;
                default:
                    return null;
            }

            // Find map at target coordinates
            if (allMaps != null)
            {
                foreach (var map in allMaps)
                {
                    if (map.x == targetX && map.y == targetY)
                    {
                        return map.id;
                    }
                }
            }

            return null;
        }
    }
}
