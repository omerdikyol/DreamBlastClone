using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Grid
{
    public sealed class BoardModel
    {
        private readonly CellModel[,] cells;

        public BoardModel(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Board width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Board height must be greater than zero.");
            }

            Width = width;
            Height = height;
            cells = new CellModel[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    cells[x, y] = new CellModel(new BoardCoordinate(x, y));
                }
            }
        }

        public int Width { get; }

        public int Height { get; }

        public bool IsWithinBounds(BoardCoordinate coordinate)
        {
            return coordinate.X >= 0
                && coordinate.X < Width
                && coordinate.Y >= 0
                && coordinate.Y < Height;
        }

        public CellModel GetCell(BoardCoordinate coordinate)
        {
            if (!TryGetCell(coordinate, out var cell))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside the board bounds.");
            }

            return cell;
        }

        public bool TryGetCell(BoardCoordinate coordinate, out CellModel cell)
        {
            if (!IsWithinBounds(coordinate))
            {
                cell = null;
                return false;
            }

            cell = cells[coordinate.X, coordinate.Y];
            return true;
        }

        public IEnumerable<CellModel> GetAllCells()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    yield return cells[x, y];
                }
            }
        }

        public IEnumerable<BoardCoordinate> GetAllCoordinates()
        {
            foreach (var cell in GetAllCells())
            {
                yield return cell.Coordinate;
            }
        }

        public void PlaceItem(BoardCoordinate coordinate, ItemModel item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var cell = GetCell(coordinate);
            EnsureCellCanAcceptItem(cell);
            cell.PlaceItem(item);
        }

        public void PlaceObstacle(BoardCoordinate coordinate, ObstacleModel obstacle)
        {
            if (obstacle is null)
            {
                throw new ArgumentNullException(nameof(obstacle));
            }

            var cell = GetCell(coordinate);
            EnsureCellCanAcceptObstacle(cell, obstacle);
            cell.PlaceObstacle(obstacle);
        }

        public void PlaceObstacle(IEnumerable<BoardCoordinate> coordinates, ObstacleModel obstacle)
        {
            if (coordinates is null)
            {
                throw new ArgumentNullException(nameof(coordinates));
            }

            if (obstacle is null)
            {
                throw new ArgumentNullException(nameof(obstacle));
            }

            var distinctCoordinates = new HashSet<BoardCoordinate>();
            var targetCells = new List<CellModel>();

            foreach (var coordinate in coordinates)
            {
                if (!distinctCoordinates.Add(coordinate))
                {
                    throw new ArgumentException($"Duplicate coordinate {coordinate} is not allowed when placing content.", nameof(coordinates));
                }

                var cell = GetCell(coordinate);
                EnsureCellCanAcceptObstacle(cell, obstacle);
                targetCells.Add(cell);
            }

            if (targetCells.Count == 0)
            {
                throw new ArgumentException("At least one coordinate is required to place content.", nameof(coordinates));
            }

            foreach (var cell in targetCells)
            {
                // Multi-cell obstacles stay one logical object and occupy several cells by shared reference.
                cell.PlaceObstacle(obstacle);
            }
        }

        public void ClearItem(BoardCoordinate coordinate)
        {
            GetCell(coordinate).ClearItem();
        }

        public void ClearObstacle(BoardCoordinate coordinate)
        {
            GetCell(coordinate).ClearObstacle();
        }

        public void ClearObstacle(ObstacleModel obstacle)
        {
            if (obstacle is null)
            {
                throw new ArgumentNullException(nameof(obstacle));
            }

            foreach (var cell in GetAllCells())
            {
                if (ReferenceEquals(cell.Obstacle, obstacle))
                {
                    cell.ClearObstacle();
                }
            }
        }

        private static void EnsureCellCanAcceptItem(CellModel cell)
        {
            if (!cell.HasItem)
            {
                return;
            }

            throw new InvalidOperationException($"Cell {cell.Coordinate} already contains an item.");
        }

        private static void EnsureCellCanAcceptObstacle(CellModel cell, ObstacleModel obstacle)
        {
            // Reusing the same obstacle reference across cells is how multi-cell obstacles occupy a footprint.
            if (!cell.HasObstacle || ReferenceEquals(cell.Obstacle, obstacle))
            {
                return;
            }

            throw new InvalidOperationException($"Cell {cell.Coordinate} already contains a different obstacle.");
        }
    }
}
