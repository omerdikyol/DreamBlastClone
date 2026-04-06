using System;
using System.Collections.Generic;
using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Data
{
    public interface IRandomCubeColorResolver
    {
        CubeColor ResolveColor(BoardCoordinate coordinate);
    }

    public sealed class LevelBoardBuilder
    {
        public BoardModel Build(LevelDefinition levelDefinition, IRandomCubeColorResolver randomCubeColorResolver)
        {
            if (levelDefinition is null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            if (ContainsRandomCubeDefinition(levelDefinition) && randomCubeColorResolver is null)
            {
                throw new ArgumentNullException(nameof(randomCubeColorResolver), "A random cube resolver is required when the level contains random cube definitions.");
            }

            var board = new BoardModel(levelDefinition.GridWidth, levelDefinition.GridHeight);
            var chaliceBoxParts = new Dictionary<BoardCoordinate, ChaliceBoxPart>();

            foreach (var cellDefinition in levelDefinition.CellDefinitions)
            {
                ValidateCellIsWithinBoard(board, cellDefinition.Coordinate);

                if (cellDefinition.Item is not null)
                {
                    var item = CreateRuntimeItem(cellDefinition.Item, cellDefinition.Coordinate, randomCubeColorResolver);
                    board.PlaceItem(cellDefinition.Coordinate, item);
                }

                if (cellDefinition.Obstacle is null)
                {
                    continue;
                }

                if (cellDefinition.Obstacle is ChaliceBoxPartLevelObstacleDefinition chaliceBoxPartDefinition)
                {
                    chaliceBoxParts.Add(cellDefinition.Coordinate, chaliceBoxPartDefinition.Part);
                    continue;
                }

                var obstacle = CreateRuntimeObstacle(cellDefinition.Obstacle);
                board.PlaceObstacle(cellDefinition.Coordinate, obstacle);
            }

            PlaceChaliceBoxes(board, chaliceBoxParts);
            return board;
        }

        private static bool ContainsRandomCubeDefinition(LevelDefinition levelDefinition)
        {
            return levelDefinition.CellDefinitions.Any(cell => cell.Item is RandomCubeLevelItemDefinition);
        }

        private static void ValidateCellIsWithinBoard(BoardModel board, BoardCoordinate coordinate)
        {
            if (!board.IsWithinBounds(coordinate))
            {
                throw new InvalidOperationException($"Level cell {coordinate} falls outside the board dimensions.");
            }
        }

        private static ItemModel CreateRuntimeItem(
            LevelItemDefinition itemDefinition,
            BoardCoordinate coordinate,
            IRandomCubeColorResolver randomCubeColorResolver)
        {
            return itemDefinition switch
            {
                CubeLevelItemDefinition cubeDefinition => new CubeItemModel(cubeDefinition.Color),
                RandomCubeLevelItemDefinition => new CubeItemModel(ResolveRandomCubeColor(randomCubeColorResolver, coordinate)),
                RocketLevelItemDefinition rocketDefinition => new RocketItemModel(rocketDefinition.Orientation),
                TntLevelItemDefinition => new TntItemModel(),
                _ => throw new InvalidOperationException($"Unsupported level item definition '{itemDefinition.GetType().Name}' at {coordinate}.")
            };
        }

        private static CubeColor ResolveRandomCubeColor(IRandomCubeColorResolver randomCubeColorResolver, BoardCoordinate coordinate)
        {
            var color = randomCubeColorResolver.ResolveColor(coordinate);

            if (!Enum.IsDefined(typeof(CubeColor), color))
            {
                throw new InvalidOperationException($"Random cube resolver returned invalid color '{color}' for {coordinate}.");
            }

            return color;
        }

        private static ObstacleModel CreateRuntimeObstacle(LevelObstacleDefinition obstacleDefinition)
        {
            return obstacleDefinition switch
            {
                VaseLevelObstacleDefinition => new VaseObstacleModel(),
                StoneLevelObstacleDefinition => new StoneObstacleModel(),
                ChaliceBoxPartLevelObstacleDefinition => throw new InvalidOperationException("Chalice box parts must be grouped before runtime obstacle creation."),
                _ => throw new InvalidOperationException($"Unsupported level obstacle definition '{obstacleDefinition.GetType().Name}'.")
            };
        }

        private static void PlaceChaliceBoxes(BoardModel board, IReadOnlyDictionary<BoardCoordinate, ChaliceBoxPart> chaliceBoxParts)
        {
            var groupsByAnchor = new Dictionary<BoardCoordinate, Dictionary<ChaliceBoxPart, BoardCoordinate>>();

            foreach (var entry in chaliceBoxParts)
            {
                var anchor = GetAnchorFromPart(entry.Key, entry.Value);

                if (!groupsByAnchor.TryGetValue(anchor, out var group))
                {
                    group = new Dictionary<ChaliceBoxPart, BoardCoordinate>();
                    groupsByAnchor.Add(anchor, group);
                }

                if (!group.TryAdd(entry.Value, entry.Key))
                {
                    throw new InvalidOperationException($"Chalice box at {anchor} contains duplicate '{entry.Value}' parts.");
                }
            }

            foreach (var groupEntry in groupsByAnchor)
            {
                var anchor = groupEntry.Key;
                var partsByType = groupEntry.Value;
                var expectedCoordinates = new Dictionary<ChaliceBoxPart, BoardCoordinate>
                {
                    { ChaliceBoxPart.BottomLeft, anchor },
                    { ChaliceBoxPart.BottomRight, anchor.Offset(1, 0) },
                    { ChaliceBoxPart.TopLeft, anchor.Offset(0, 1) },
                    { ChaliceBoxPart.TopRight, anchor.Offset(1, 1) }
                };

                if (partsByType.Count != expectedCoordinates.Count)
                {
                    throw new InvalidOperationException($"Chalice box at {anchor} is incomplete and must contain exactly four parts.");
                }

                foreach (var expectedPart in expectedCoordinates)
                {
                    if (!partsByType.TryGetValue(expectedPart.Key, out var actualCoordinate))
                    {
                        throw new InvalidOperationException($"Chalice box at {anchor} is missing the '{expectedPart.Key}' part.");
                    }

                    if (actualCoordinate != expectedPart.Value)
                    {
                        throw new InvalidOperationException(
                            $"Chalice box at {anchor} has '{expectedPart.Key}' at {actualCoordinate}, expected {expectedPart.Value}.");
                    }

                    ValidateCellIsWithinBoard(board, actualCoordinate);
                }

                // Chalice box authored parts are normalized into one runtime obstacle anchored at the bottom-left cell.
                var chaliceBox = new ChaliceBoxObstacleModel(anchor, remainingDoorDurability: 4, requiredChaliceCount: 10, collectedChaliceCount: 0);
                board.PlaceObstacle(expectedCoordinates.Values, chaliceBox);
            }
        }

        private static BoardCoordinate GetAnchorFromPart(BoardCoordinate coordinate, ChaliceBoxPart part)
        {
            return part switch
            {
                ChaliceBoxPart.BottomLeft => coordinate,
                ChaliceBoxPart.BottomRight => coordinate.Offset(-1, 0),
                ChaliceBoxPart.TopLeft => coordinate.Offset(0, -1),
                ChaliceBoxPart.TopRight => coordinate.Offset(-1, -1),
                _ => throw new InvalidOperationException($"Unsupported chalice box part '{part}'.")
            };
        }
    }
}
