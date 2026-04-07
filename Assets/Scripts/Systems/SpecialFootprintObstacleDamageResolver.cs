using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    internal static class SpecialFootprintObstacleDamageResolver
    {
        public static ObstacleDamageResolutionResult Resolve(BoardModel board, IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (affectedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(affectedCoordinates));
            }

            var vaseCoordinatesInOrder = new List<BoardCoordinate>();
            var stoneCoordinatesInOrder = new List<BoardCoordinate>();
            var chaliceBoxesInOrder = new List<ChaliceBoxObstacleModel>();
            var touchedChaliceCellsByBox = new Dictionary<ChaliceBoxObstacleModel, HashSet<BoardCoordinate>>();
            var seenVases = new HashSet<BoardCoordinate>();
            var seenStones = new HashSet<BoardCoordinate>();
            var damages = new List<ObstacleDamage>();
            var removedCoordinates = new List<BoardCoordinate>();

            foreach (var affectedCoordinate in affectedCoordinates)
            {
                if (!board.TryGetCell(affectedCoordinate, out var cell) || cell.Obstacle is null)
                {
                    continue;
                }

                switch (cell.Obstacle)
                {
                    case VaseObstacleModel:
                        if (seenVases.Add(affectedCoordinate))
                        {
                            vaseCoordinatesInOrder.Add(affectedCoordinate);
                        }

                        break;
                    case StoneObstacleModel:
                        if (seenStones.Add(affectedCoordinate))
                        {
                            stoneCoordinatesInOrder.Add(affectedCoordinate);
                        }

                        break;
                    case ChaliceBoxObstacleModel chaliceBox:
                        if (!touchedChaliceCellsByBox.TryGetValue(chaliceBox, out var touchedCells))
                        {
                            touchedCells = new HashSet<BoardCoordinate>();
                            touchedChaliceCellsByBox.Add(chaliceBox, touchedCells);
                            chaliceBoxesInOrder.Add(chaliceBox);
                        }

                        // Special-effect footprints aggregate shared obstacles once per event while still counting unique occupied cells in Chalice phase.
                        touchedCells.Add(affectedCoordinate);
                        break;
                }
            }

            foreach (var vaseCoordinate in vaseCoordinatesInOrder)
            {
                var vase = (VaseObstacleModel)board.GetCell(vaseCoordinate).Obstacle;
                vase.RemainingDurability -= 1;

                if (vase.RemainingDurability <= 0)
                {
                    board.ClearObstacle(vaseCoordinate);
                    removedCoordinates.Add(vaseCoordinate);
                }

                damages.Add(new ObstacleDamage(vaseCoordinate, amount: 1));
            }

            foreach (var stoneCoordinate in stoneCoordinatesInOrder)
            {
                var stone = (StoneObstacleModel)board.GetCell(stoneCoordinate).Obstacle;
                stone.RemainingDurability -= 1;

                if (stone.RemainingDurability <= 0)
                {
                    board.ClearObstacle(stoneCoordinate);
                    removedCoordinates.Add(stoneCoordinate);
                }

                damages.Add(new ObstacleDamage(stoneCoordinate, amount: 1));
            }

            foreach (var chaliceBox in chaliceBoxesInOrder)
            {
                if (chaliceBox.RemainingDoorDurability > 0)
                {
                    chaliceBox.RemainingDoorDurability = Math.Max(0, chaliceBox.RemainingDoorDurability - 1);
                    damages.Add(new ObstacleDamage(chaliceBox.Anchor, amount: 1));
                    continue;
                }

                var chaliceDamage = Math.Min(
                    touchedChaliceCellsByBox[chaliceBox].Count,
                    chaliceBox.RequiredChaliceCount - chaliceBox.CollectedChaliceCount);

                if (chaliceDamage <= 0)
                {
                    continue;
                }

                chaliceBox.CollectedChaliceCount += chaliceDamage;
                damages.Add(new ObstacleDamage(chaliceBox.Anchor, chaliceDamage));

                if (chaliceBox.CollectedChaliceCount >= chaliceBox.RequiredChaliceCount)
                {
                    board.ClearObstacle(chaliceBox);
                    removedCoordinates.Add(chaliceBox.Anchor);
                }
            }

            return damages.Count == 0
                ? ObstacleDamageResolutionResult.Empty()
                : new ObstacleDamageResolutionResult(damages, removedCoordinates);
        }
    }
}
