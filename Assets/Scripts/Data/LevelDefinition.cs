using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Data
{
    public sealed class LevelDefinition
    {
        public LevelDefinition(int levelNumber, int gridWidth, int gridHeight, int moveCount, IEnumerable<LevelCellDefinition> cellDefinitions)
        {
            if (levelNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber, "Level number must be greater than zero.");
            }

            if (gridWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridWidth), gridWidth, "Grid width must be greater than zero.");
            }

            if (gridHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridHeight), gridHeight, "Grid height must be greater than zero.");
            }

            if (moveCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveCount), moveCount, "Move count cannot be negative.");
            }

            if (cellDefinitions is null)
            {
                throw new ArgumentNullException(nameof(cellDefinitions));
            }

            var coordinateSet = new HashSet<BoardCoordinate>();
            var collectedDefinitions = new List<LevelCellDefinition>();

            foreach (var cellDefinition in cellDefinitions)
            {
                if (cellDefinition is null)
                {
                    throw new ArgumentNullException(nameof(cellDefinitions), "Cell definitions cannot contain null entries.");
                }

                if (cellDefinition.Coordinate.X < 0
                    || cellDefinition.Coordinate.X >= gridWidth
                    || cellDefinition.Coordinate.Y < 0
                    || cellDefinition.Coordinate.Y >= gridHeight)
                {
                    throw new ArgumentOutOfRangeException(nameof(cellDefinitions), cellDefinition.Coordinate, "Level cell definition must be within the declared grid bounds.");
                }

                if (!coordinateSet.Add(cellDefinition.Coordinate))
                {
                    throw new ArgumentException($"Duplicate level cell coordinate {cellDefinition.Coordinate} is not allowed.", nameof(cellDefinitions));
                }

                collectedDefinitions.Add(cellDefinition);
            }

            LevelNumber = levelNumber;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            MoveCount = moveCount;
            CellDefinitions = collectedDefinitions.AsReadOnly();
        }

        public int LevelNumber { get; }

        public int GridWidth { get; }

        public int GridHeight { get; }

        public int MoveCount { get; }

        public IReadOnlyList<LevelCellDefinition> CellDefinitions { get; }
    }

    public sealed class LevelCellDefinition
    {
        public LevelCellDefinition(
            BoardCoordinate coordinate,
            LevelItemDefinition item = null,
            LevelObstacleDefinition obstacle = null)
        {
            Coordinate = coordinate;
            Item = item;
            Obstacle = obstacle;
        }

        public BoardCoordinate Coordinate { get; }

        // Authored level cells mirror runtime layering without becoming runtime board state themselves.
        public LevelItemDefinition Item { get; }

        public LevelObstacleDefinition Obstacle { get; }
    }
}
