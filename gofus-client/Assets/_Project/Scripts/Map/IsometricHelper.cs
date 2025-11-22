using System.Collections.Generic;
using UnityEngine;

namespace GOFUS.Map
{
    /// <summary>
    /// Helper class for isometric grid calculations based on Dofus specifications
    /// </summary>
    public static class IsometricHelper
    {
        // Dofus grid constants
        public const int GRID_WIDTH = 14;
        public const int GRID_HEIGHT = 20;
        public const int GRID_HEIGHT_HALF = 20; // Double height for diamond shape
        public const float CELL_WIDTH = 86f;
        public const float CELL_HEIGHT = 43f;
        // Cell spacing for 200x100 pixel sprites at 50 PPU
        // Reduced from 2.0/1.0 to 1.6/0.8 (20% overlap) to eliminate gaps between cells
        public const float CELL_HALF_WIDTH = 1.6f;  // Was 2f - creates 20% overlap
        public const float CELL_HALF_HEIGHT = 0.8f; // Was 1f - creates 20% overlap
        public const int TOTAL_CELLS = 560; // 14 * 20 * 2

        /// <summary>
        /// Convert Cell ID to grid coordinates (Dofus style)
        /// </summary>
        public static Vector2Int CellIdToGridCoords(int cellId)
        {
            if (cellId < 0 || cellId >= TOTAL_CELLS)
                return new Vector2Int(-1, -1);

            // Dofus uses a specific cell numbering system for diamond grid
            int line = cellId / (GRID_WIDTH * 2 - 1);
            int column = cellId - line * (GRID_WIDTH * 2 - 1);

            // Adjust for diamond shape
            if (line % 2 == 1)
            {
                column = column * 2 + 1;
            }
            else
            {
                column = column * 2;
            }

            return new Vector2Int(column, line);
        }

        /// <summary>
        /// Convert grid coordinates to Cell ID
        /// </summary>
        public static int GridCoordsToCellId(int x, int y)
        {
            if (x < 0 || x >= GRID_WIDTH * 2 || y < 0 || y >= GRID_HEIGHT_HALF)
                return -1;

            int cellId = y * (GRID_WIDTH * 2 - 1);

            if (y % 2 == 1)
            {
                cellId += (x - 1) / 2;
            }
            else
            {
                cellId += x / 2;
            }

            return cellId >= 0 && cellId < TOTAL_CELLS ? cellId : -1;
        }

        /// <summary>
        /// Convert Cell ID to world position (isometric projection)
        /// </summary>
        public static Vector3 CellIdToWorldPosition(int cellId)
        {
            Vector2Int gridCoords = CellIdToGridCoords(cellId);

            if (gridCoords.x < 0 || gridCoords.y < 0)
                return Vector3.zero;

            // Isometric conversion
            float x = (gridCoords.x - gridCoords.y) * CELL_HALF_WIDTH;
            float y = (gridCoords.x + gridCoords.y) * CELL_HALF_HEIGHT;

            return new Vector3(x, y, 0);
        }

        /// <summary>
        /// Convert world position to Cell ID
        /// </summary>
        public static int WorldPositionToCellId(Vector3 worldPos)
        {
            // Reverse isometric projection
            float x = worldPos.x / CELL_HALF_WIDTH;
            float y = worldPos.y / CELL_HALF_HEIGHT;

            int gridX = Mathf.RoundToInt((x + y) / 2);
            int gridY = Mathf.RoundToInt((y - x) / 2);

            return GridCoordsToCellId(gridX, gridY);
        }

        /// <summary>
        /// Get all neighbor cells (8-directional)
        /// </summary>
        public static List<int> GetNeighborCells(int cellId)
        {
            List<int> neighbors = new List<int>();
            Vector2Int coords = CellIdToGridCoords(cellId);

            if (coords.x < 0 || coords.y < 0)
                return neighbors;

            // Diamond grid has 6 neighbors (not 8)
            // In a diamond/staggered grid:
            // - Even rows have even x (0, 2, 4, 6...)
            // - Odd rows have odd x (1, 3, 5, 7...)
            // - Horizontal neighbors are ±2 in x
            // - Diagonal neighbors are ±1 in both x and y
            int[,] directions = new int[,]
            {
                {-2, 0},  {2, 0},     // left, right (horizontal)
                {-1, -1}, {1, -1},    // upper-left, upper-right
                {-1, 1},  {1, 1}      // lower-left, lower-right
            };

            for (int i = 0; i < 6; i++)
            {
                int newX = coords.x + directions[i, 0];
                int newY = coords.y + directions[i, 1];

                int neighborId = GridCoordsToCellId(newX, newY);
                if (neighborId >= 0 && neighborId < TOTAL_CELLS)
                {
                    neighbors.Add(neighborId);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Calculate Manhattan distance between cells
        /// </summary>
        public static int GetDistance(int cellId1, int cellId2)
        {
            Vector2Int coords1 = CellIdToGridCoords(cellId1);
            Vector2Int coords2 = CellIdToGridCoords(cellId2);

            if (coords1.x < 0 || coords2.x < 0)
                return int.MaxValue;

            return Mathf.Abs(coords1.x - coords2.x) + Mathf.Abs(coords1.y - coords2.y);
        }

        /// <summary>
        /// Get line of sight between two cells
        /// </summary>
        public static List<int> GetLineOfSight(int startCell, int endCell)
        {
            List<int> cells = new List<int>();

            Vector3 start = CellIdToWorldPosition(startCell);
            Vector3 end = CellIdToWorldPosition(endCell);

            if (start == Vector3.zero || end == Vector3.zero)
                return cells;

            float distance = Vector3.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / CELL_HALF_WIDTH);

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 point = Vector3.Lerp(start, end, t);
                int cellId = WorldPositionToCellId(point);

                if (cellId >= 0 && !cells.Contains(cellId))
                {
                    cells.Add(cellId);
                }
            }

            return cells;
        }

        /// <summary>
        /// Check if cell is walkable
        /// </summary>
        public static bool IsCellWalkable(int cellId, CellGrid grid)
        {
            if (cellId < 0 || cellId >= grid.Cells.Length)
                return false;

            return grid.Cells[cellId].IsWalkable && !grid.Cells[cellId].IsOccupied;
        }

        /// <summary>
        /// Convert screen position to world position
        /// </summary>
        public static Vector3 ScreenToWorldPosition(Vector2 screenPos, Camera camera)
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null)
                return Vector3.zero;

            Ray ray = camera.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Get cells in range
        /// </summary>
        public static List<int> GetCellsInRange(int centerCell, int range, bool includeDiagonals = true)
        {
            List<int> cellsInRange = new List<int>();

            for (int cellId = 0; cellId < TOTAL_CELLS; cellId++)
            {
                int distance = GetDistance(centerCell, cellId);
                if (distance <= range)
                {
                    cellsInRange.Add(cellId);
                }
            }

            return cellsInRange;
        }

        /// <summary>
        /// Get cells in area of effect
        /// </summary>
        public static List<int> GetAreaOfEffect(int targetCell, AreaShape shape, int size)
        {
            List<int> affectedCells = new List<int>();

            switch (shape)
            {
                case AreaShape.Cross:
                    affectedCells = GetCrossPattern(targetCell, size);
                    break;

                case AreaShape.Circle:
                    affectedCells = GetCellsInRange(targetCell, size);
                    break;

                case AreaShape.Square:
                    affectedCells = GetSquarePattern(targetCell, size);
                    break;

                case AreaShape.Line:
                    // Line requires direction, handled separately
                    break;

                case AreaShape.Diamond:
                    affectedCells = GetDiamondPattern(targetCell, size);
                    break;
            }

            if (!affectedCells.Contains(targetCell))
                affectedCells.Add(targetCell); // Include center

            return affectedCells;
        }

        private static List<int> GetCrossPattern(int centerCell, int size)
        {
            List<int> cells = new List<int>();
            Vector2Int center = CellIdToGridCoords(centerCell);

            if (center.x < 0 || center.y < 0)
                return cells;

            // Four cardinal directions
            int[,] directions = new int[,] { {0, -1}, {1, 0}, {0, 1}, {-1, 0} };

            for (int distance = 1; distance <= size; distance++)
            {
                for (int i = 0; i < 4; i++)
                {
                    int newX = center.x + directions[i, 0] * distance;
                    int newY = center.y + directions[i, 1] * distance;

                    int cellId = GridCoordsToCellId(newX, newY);
                    if (cellId >= 0 && !cells.Contains(cellId))
                    {
                        cells.Add(cellId);
                    }
                }
            }

            return cells;
        }

        private static List<int> GetSquarePattern(int centerCell, int size)
        {
            List<int> cells = new List<int>();
            Vector2Int center = CellIdToGridCoords(centerCell);

            if (center.x < 0 || center.y < 0)
                return cells;

            for (int dx = -size; dx <= size; dx++)
            {
                for (int dy = -size; dy <= size; dy++)
                {
                    int newX = center.x + dx;
                    int newY = center.y + dy;

                    int cellId = GridCoordsToCellId(newX, newY);
                    if (cellId >= 0 && !cells.Contains(cellId))
                    {
                        cells.Add(cellId);
                    }
                }
            }

            return cells;
        }

        private static List<int> GetDiamondPattern(int centerCell, int size)
        {
            List<int> cells = new List<int>();
            Vector2Int center = CellIdToGridCoords(centerCell);

            if (center.x < 0 || center.y < 0)
                return cells;

            for (int dx = -size; dx <= size; dx++)
            {
                for (int dy = -size; dy <= size; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= size)
                    {
                        int newX = center.x + dx;
                        int newY = center.y + dy;

                        int cellId = GridCoordsToCellId(newX, newY);
                        if (cellId >= 0 && !cells.Contains(cellId))
                        {
                            cells.Add(cellId);
                        }
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// Get line of cells from origin in a direction
        /// </summary>
        public static List<int> GetLine(int originCell, Vector2Int direction, int length)
        {
            List<int> cells = new List<int>();
            Vector2Int origin = CellIdToGridCoords(originCell);

            if (origin.x < 0 || origin.y < 0)
                return cells;

            for (int i = 1; i <= length; i++)
            {
                int newX = origin.x + direction.x * i;
                int newY = origin.y + direction.y * i;

                int cellId = GridCoordsToCellId(newX, newY);
                if (cellId >= 0)
                {
                    cells.Add(cellId);
                }
                else
                {
                    break; // Stop if we hit an invalid cell
                }
            }

            return cells;
        }

        /// <summary>
        /// Check if two cells have line of sight
        /// </summary>
        public static bool HasLineOfSight(int fromCell, int toCell, CellGrid grid)
        {
            var cellsInLine = GetLineOfSight(fromCell, toCell);

            foreach (int cellId in cellsInLine)
            {
                if (cellId != fromCell && cellId != toCell)
                {
                    if (!IsCellWalkable(cellId, grid))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get the direction from one cell to another
        /// </summary>
        public static Vector2Int GetDirection(int fromCell, int toCell)
        {
            Vector2Int from = CellIdToGridCoords(fromCell);
            Vector2Int to = CellIdToGridCoords(toCell);

            if (from.x < 0 || to.x < 0)
                return Vector2Int.zero;

            int dx = Mathf.Clamp(to.x - from.x, -1, 1);
            int dy = Mathf.Clamp(to.y - from.y, -1, 1);

            return new Vector2Int(dx, dy);
        }
    }

    public enum AreaShape
    {
        Cross,
        Circle,
        Line,
        Square,
        Diamond,
        Cone
    }

    [System.Serializable]
    public class CellGrid
    {
        public Cell[] Cells { get; private set; }

        public CellGrid()
        {
            Cells = new Cell[IsometricHelper.TOTAL_CELLS];
            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i] = new Cell { Id = i, IsWalkable = true };
            }
        }

        public void SetCellWalkable(int cellId, bool walkable)
        {
            if (cellId >= 0 && cellId < Cells.Length)
            {
                Cells[cellId].IsWalkable = walkable;
            }
        }

        public void SetCellOccupied(int cellId, bool occupied)
        {
            if (cellId >= 0 && cellId < Cells.Length)
            {
                Cells[cellId].IsOccupied = occupied;
            }
        }
    }

    [System.Serializable]
    public class Cell
    {
        public int Id;
        public bool IsWalkable = true;
        public bool IsOccupied = false;
        public int MovementCost = 1;
        public CellType Type = CellType.Normal;
        public GameObject Occupant;
    }

    public enum CellType
    {
        Normal,
        Obstacle,
        Water,
        Lava,
        Teleporter,
        Interactive
    }
}