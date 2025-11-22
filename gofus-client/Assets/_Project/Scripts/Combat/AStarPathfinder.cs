using System;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Map;

namespace GOFUS.Combat
{
    /// <summary>
    /// Optimized A* pathfinding implementation for isometric grid
    /// Features: Path caching, hierarchical pathfinding preparation, and optimized data structures
    /// </summary>
    public class AStarPathfinder
    {
        private readonly CellGrid grid;
        private readonly Dictionary<int, PathNode> nodeCache;
        private readonly Dictionary<string, List<int>> pathCache;
        private readonly int maxCacheSize = 100;

        // Performance optimization flags
        public bool IsCachingEnabled { get; set; } = true;
        public bool UseHierarchicalPathfinding { get; set; } = false;
        public int MaxSearchNodes { get; set; } = 1000;

        // Statistics for performance monitoring
        public int PathsCalculated { get; private set; }
        public int CacheHits { get; private set; }
        public float LastPathfindingTime { get; private set; }

        public CellGrid Grid => grid;

        private class PathNode : IComparable<PathNode>
        {
            public int CellId;
            public PathNode Parent;
            public float GCost; // Cost from start
            public float HCost; // Heuristic cost to end
            public float FCost => GCost + HCost; // Total cost

            public int CompareTo(PathNode other)
            {
                if (other == null) return -1;

                int compare = FCost.CompareTo(other.FCost);
                if (compare == 0)
                {
                    compare = HCost.CompareTo(other.HCost);
                }
                return compare;
            }
        }

        public AStarPathfinder(CellGrid grid)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.nodeCache = new Dictionary<int, PathNode>();
            this.pathCache = new Dictionary<string, List<int>>();
        }

        public List<int> FindPath(int startCell, int endCell)
        {
            Debug.Log($"[AStarPathfinder] ===== FindPath ENTERED: start={startCell}, end={endCell} =====");
            var startTime = Time.realtimeSinceStartup;

            try
            {
                // Validation
                Debug.Log($"[AStarPathfinder] Checking cell validity...");
                if (!IsValidCell(startCell) || !IsValidCell(endCell))
                {
                    Debug.Log($"[AStarPathfinder] Invalid cell: start={startCell} (valid={IsValidCell(startCell)}), end={endCell} (valid={IsValidCell(endCell)})");
                    return null;
                }

                // Same cell - return empty path
                if (startCell == endCell)
                {
                    Debug.Log($"[AStarPathfinder] Start and end are same cell ({startCell}), returning empty path");
                    return new List<int>();
                }

                // Check if start or end is unwalkable
                Debug.Log($"[AStarPathfinder] Checking walkability...");
                if (!IsWalkable(startCell))
                {
                    var startCellData = grid.Cells[startCell];
                    Debug.Log($"[AStarPathfinder] Start cell {startCell} is NOT WALKABLE! IsWalkable={startCellData.IsWalkable}, IsOccupied={startCellData.IsOccupied}");
                    return null;
                }

                if (!IsWalkable(endCell))
                {
                    var endCellData = grid.Cells[endCell];
                    Debug.Log($"[AStarPathfinder] End cell {endCell} is NOT WALKABLE! IsWalkable={endCellData.IsWalkable}, IsOccupied={endCellData.IsOccupied}");
                    return null;
                }

                Debug.Log($"[AStarPathfinder] Both cells are walkable, proceeding to A* search...");

                // Check cache
                if (IsCachingEnabled)
                {
                    string cacheKey = GetCacheKey(startCell, endCell);
                    if (pathCache.TryGetValue(cacheKey, out var cachedPath))
                    {
                        CacheHits++;
                        LastPathfindingTime = Time.realtimeSinceStartup - startTime;
                        return new List<int>(cachedPath); // Return copy
                    }
                }

                // Perform A* search
                var path = PerformAStarSearch(startCell, endCell);

                // Cache the result
                if (IsCachingEnabled && path != null)
                {
                    CachePathResult(startCell, endCell, path);
                }

                PathsCalculated++;
                LastPathfindingTime = Time.realtimeSinceStartup - startTime;
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AStarPathfinder] EXCEPTION in FindPath: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
            finally
            {
                // Clear node cache for next search
                nodeCache.Clear();
            }
        }

        private List<int> PerformAStarSearch(int startCell, int endCell)
        {
            var openSet = new SortedSet<PathNode>(new PathNodeComparer());
            var closedSet = new HashSet<int>();
            var openSetLookup = new Dictionary<int, PathNode>();

            // Create start node
            var startNode = GetOrCreateNode(startCell);
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(startCell, endCell);
            openSet.Add(startNode);
            openSetLookup[startCell] = startNode;

            int nodesSearched = 0;

            while (openSet.Count > 0 && nodesSearched < MaxSearchNodes)
            {
                // Get node with lowest FCost
                var currentNode = openSet.Min;
                openSet.Remove(currentNode);
                openSetLookup.Remove(currentNode.CellId);

                // Check if we reached the goal
                if (currentNode.CellId == endCell)
                {
                    return ReconstructPath(currentNode);
                }

                closedSet.Add(currentNode.CellId);
                nodesSearched++;

                // Check all neighbors
                var neighbors = IsometricHelper.GetNeighborCells(currentNode.CellId);
                foreach (int neighborCell in neighbors)
                {
                    // Skip if not walkable or already evaluated
                    if (!IsWalkable(neighborCell) || closedSet.Contains(neighborCell))
                        continue;

                    float tentativeGCost = currentNode.GCost + GetMovementCost(currentNode.CellId, neighborCell);

                    // Get or create neighbor node
                    PathNode neighborNode;
                    bool isInOpenSet = openSetLookup.TryGetValue(neighborCell, out neighborNode);

                    if (!isInOpenSet)
                    {
                        neighborNode = GetOrCreateNode(neighborCell);
                        neighborNode.HCost = CalculateHeuristic(neighborCell, endCell);
                    }

                    // Check if this path to neighbor is better
                    if (!isInOpenSet || tentativeGCost < neighborNode.GCost)
                    {
                        // Update the neighbor
                        if (isInOpenSet)
                        {
                            openSet.Remove(neighborNode);
                        }

                        neighborNode.Parent = currentNode;
                        neighborNode.GCost = tentativeGCost;

                        openSet.Add(neighborNode);
                        openSetLookup[neighborCell] = neighborNode;
                    }
                }
            }

            // No path found
            Debug.Log($"[AStarPathfinder] NO PATH FOUND from {startCell} to {endCell}! Searched {nodesSearched} nodes.");
            return null;
        }

        private PathNode GetOrCreateNode(int cellId)
        {
            if (!nodeCache.TryGetValue(cellId, out var node))
            {
                node = new PathNode { CellId = cellId };
                nodeCache[cellId] = node;
            }
            return node;
        }

        private float CalculateHeuristic(int fromCell, int toCell)
        {
            // Use Manhattan distance for isometric grid
            // Multiplied by minimum movement cost for admissible heuristic
            return IsometricHelper.GetDistance(fromCell, toCell);
        }

        private float GetMovementCost(int fromCell, int toCell)
        {
            // Base cost is the target cell's movement cost
            float cost = grid.Cells[toCell].MovementCost;

            // Add diagonal movement penalty if applicable
            var fromCoords = IsometricHelper.CellIdToGridCoords(fromCell);
            var toCoords = IsometricHelper.CellIdToGridCoords(toCell);

            bool isDiagonal = Math.Abs(fromCoords.x - toCoords.x) == 1 &&
                              Math.Abs(fromCoords.y - toCoords.y) == 1;

            if (isDiagonal)
            {
                cost *= 1.414f; // √2 for diagonal movement
            }

            return cost;
        }

        private List<int> ReconstructPath(PathNode endNode)
        {
            var path = new List<int>();
            var currentNode = endNode;

            while (currentNode.Parent != null)
            {
                path.Add(currentNode.CellId);
                currentNode = currentNode.Parent;
            }

            // Don't include start cell
            path.Reverse();
            return path;
        }

        private bool IsValidCell(int cellId)
        {
            return cellId >= 0 && cellId < IsometricHelper.TOTAL_CELLS;
        }

        private bool IsWalkable(int cellId)
        {
            if (!IsValidCell(cellId))
                return false;

            var cell = grid.Cells[cellId];
            return cell.IsWalkable && !cell.IsOccupied;
        }

        private string GetCacheKey(int startCell, int endCell)
        {
            return $"{startCell}_{endCell}";
        }

        private void CachePathResult(int startCell, int endCell, List<int> path)
        {
            // Limit cache size
            if (pathCache.Count >= maxCacheSize)
            {
                // Simple FIFO eviction - remove first entry
                var firstKey = pathCache.Keys.GetEnumerator();
                if (firstKey.MoveNext())
                {
                    pathCache.Remove(firstKey.Current);
                }
            }

            string key = GetCacheKey(startCell, endCell);
            pathCache[key] = new List<int>(path); // Store copy
        }

        public void ClearCache()
        {
            pathCache.Clear();
            CacheHits = 0;
            PathsCalculated = 0;
        }

        public void InvalidateCache()
        {
            // Called when the grid changes
            pathCache.Clear();
        }

        /// <summary>
        /// Find multiple paths efficiently by reusing calculations
        /// </summary>
        public Dictionary<int, List<int>> FindMultiplePaths(int startCell, List<int> targetCells)
        {
            var results = new Dictionary<int, List<int>>();

            foreach (int targetCell in targetCells)
            {
                var path = FindPath(startCell, targetCell);
                if (path != null)
                {
                    results[targetCell] = path;
                }
            }

            return results;
        }

        /// <summary>
        /// Check if a path exists without calculating the full path
        /// </summary>
        public bool PathExists(int startCell, int endCell)
        {
            // Quick validation
            if (!IsValidCell(startCell) || !IsValidCell(endCell))
                return false;

            if (!IsWalkable(startCell) || !IsWalkable(endCell))
                return false;

            // For now, calculate full path - could be optimized with bidirectional search
            var path = FindPath(startCell, endCell);
            return path != null && path.Count > 0;
        }

        /// <summary>
        /// Get the cost of a path without storing it
        /// </summary>
        public float GetPathCost(List<int> path)
        {
            if (path == null || path.Count == 0)
                return 0;

            float totalCost = 0;
            for (int i = 0; i < path.Count; i++)
            {
                totalCost += grid.Cells[path[i]].MovementCost;

                // Add diagonal cost if applicable
                if (i > 0)
                {
                    var prevCoords = IsometricHelper.CellIdToGridCoords(path[i - 1]);
                    var currCoords = IsometricHelper.CellIdToGridCoords(path[i]);

                    bool isDiagonal = Math.Abs(prevCoords.x - currCoords.x) == 1 &&
                                      Math.Abs(prevCoords.y - currCoords.y) == 1;

                    if (isDiagonal)
                    {
                        totalCost *= 0.414f; // Additional cost for diagonal
                    }
                }
            }

            return totalCost;
        }

        private class PathNodeComparer : IComparer<PathNode>
        {
            public int Compare(PathNode x, PathNode y)
            {
                if (x == null) return 1;
                if (y == null) return -1;

                int result = x.CompareTo(y);

                // If costs are equal, use cell ID as tiebreaker for consistent ordering
                if (result == 0)
                {
                    result = x.CellId.CompareTo(y.CellId);
                }

                return result;
            }
        }
    }
}