using NUnit.Framework;
using GOFUS.Map;
using System.Collections.Generic;
using System.Linq;

namespace GOFUS.Tests
{
    /// <summary>
    /// TDD Tests for IsometricHelper neighbor calculation
    /// </summary>
    public class IsometricHelperTests
    {
        [Test]
        public void CellIdToGridCoords_Cell311_ReturnsCorrectCoordinates()
        {
            // Arrange
            int cellId = 311;

            // Act
            var coords = IsometricHelper.CellIdToGridCoords(cellId);

            // Assert
            Assert.AreEqual(29, coords.x, "Cell 311 should be at x=29");
            Assert.AreEqual(11, coords.y, "Cell 311 should be at y=11");
        }

        [Test]
        public void CellIdToGridCoords_Cell310_ReturnsCorrectCoordinates()
        {
            // Arrange
            int cellId = 310;

            // Act
            var coords = IsometricHelper.CellIdToGridCoords(cellId);

            // Assert
            Assert.AreEqual(27, coords.x, "Cell 310 should be at x=27");
            Assert.AreEqual(11, coords.y, "Cell 310 should be at y=11");
        }

        [Test]
        public void GridCoordsToCellId_27_11_Returns310()
        {
            // Arrange
            int x = 27, y = 11;

            // Act
            int cellId = IsometricHelper.GridCoordsToCellId(x, y);

            // Assert
            Assert.AreEqual(310, cellId);
        }

        [Test]
        public void GridCoordsToCellId_29_11_Returns311()
        {
            // Arrange
            int x = 29, y = 11;

            // Act
            int cellId = IsometricHelper.GridCoordsToCellId(x, y);

            // Assert
            Assert.AreEqual(311, cellId);
        }

        [Test]
        public void GetNeighborCells_Cell311_ContainsCell310()
        {
            // Arrange
            int cellId = 311;

            // Act
            var neighbors = IsometricHelper.GetNeighborCells(cellId);

            // Assert
            Assert.IsTrue(neighbors.Contains(310),
                $"Cell 311 neighbors should include cell 310. Found neighbors: [{string.Join(", ", neighbors)}]");
        }

        [Test]
        public void GetNeighborCells_Cell311_ContainsCell312()
        {
            // Arrange
            int cellId = 311;

            // Act
            var neighbors = IsometricHelper.GetNeighborCells(cellId);

            // Assert
            Assert.IsTrue(neighbors.Contains(312),
                $"Cell 311 neighbors should include cell 312. Found neighbors: [{string.Join(", ", neighbors)}]");
        }

        [Test]
        public void GetNeighborCells_Cell311_Returns6To8Neighbors()
        {
            // Arrange
            int cellId = 311;

            // Act
            var neighbors = IsometricHelper.GetNeighborCells(cellId);

            // Assert
            Assert.GreaterOrEqual(neighbors.Count, 6, "Should have at least 6 neighbors");
            Assert.LessOrEqual(neighbors.Count, 8, "Should have at most 8 neighbors");
        }

        [Test]
        public void GetNeighborCells_AllNeighborsAreValid()
        {
            // Arrange
            int cellId = 311;

            // Act
            var neighbors = IsometricHelper.GetNeighborCells(cellId);

            // Assert
            foreach (var neighbor in neighbors)
            {
                Assert.GreaterOrEqual(neighbor, 0, $"Neighbor {neighbor} should be >= 0");
                Assert.Less(neighbor, IsometricHelper.TOTAL_CELLS,
                    $"Neighbor {neighbor} should be < {IsometricHelper.TOTAL_CELLS}");
            }
        }

        [Test]
        public void GetNeighborCells_Cell0_ReturnsValidNeighbors()
        {
            // Arrange - test edge case
            int cellId = 0;

            // Act
            var neighbors = IsometricHelper.GetNeighborCells(cellId);

            // Assert
            Assert.Greater(neighbors.Count, 0, "Cell 0 should have at least some neighbors");
            foreach (var neighbor in neighbors)
            {
                Assert.GreaterOrEqual(neighbor, 0);
                Assert.Less(neighbor, IsometricHelper.TOTAL_CELLS);
            }
        }

        [Test]
        public void GetNeighborCells_Cell559_ReturnsValidNeighbors()
        {
            // Arrange - test edge case at end of grid
            int cellId = 559;

            // Act
            var neighbors = IsometricHelper.GetNeighborCells(cellId);

            // Assert
            Assert.Greater(neighbors.Count, 0, "Cell 559 should have at least some neighbors");
            foreach (var neighbor in neighbors)
            {
                Assert.GreaterOrEqual(neighbor, 0);
                Assert.Less(neighbor, IsometricHelper.TOTAL_CELLS);
            }
        }

        [Test]
        public void CellIdToGridCoords_RoundTrip_ReturnsOriginalCellId()
        {
            // Test that converting cellId → coords → cellId returns original
            for (int cellId = 0; cellId < IsometricHelper.TOTAL_CELLS; cellId++)
            {
                var coords = IsometricHelper.CellIdToGridCoords(cellId);
                int backToCellId = IsometricHelper.GridCoordsToCellId(coords.x, coords.y);

                Assert.AreEqual(cellId, backToCellId,
                    $"Round trip failed for cell {cellId}: coords=({coords.x},{coords.y}) returned cell {backToCellId}");
            }
        }

        [Test]
        public void WorldPosition_RoundTrip_ReturnsNearbyCell()
        {
            // Test cells near center of map
            int[] testCells = { 0, 13, 27, 280, 310, 311, 559 };

            foreach (int cellId in testCells)
            {
                var worldPos = IsometricHelper.CellIdToWorldPosition(cellId);
                int backToCellId = IsometricHelper.WorldPositionToCellId(worldPos);

                // Should return same cell or immediate neighbor (due to rounding)
                var neighbors = IsometricHelper.GetNeighborCells(cellId);
                neighbors.Add(cellId);

                Assert.IsTrue(neighbors.Contains(backToCellId),
                    $"Round trip world position failed for cell {cellId}: worldPos={worldPos} returned cell {backToCellId}");
            }
        }
    }
}
