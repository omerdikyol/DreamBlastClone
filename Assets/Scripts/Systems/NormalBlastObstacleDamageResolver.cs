using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class NormalBlastObstacleDamageResolver
    {
        public ObstacleDamageResolutionResult Resolve(BoardModel board, CubeBlastResolutionResult blast)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (blast is null)
            {
                throw new ArgumentNullException(nameof(blast));
            }

            if (!blast.IsValidBlast)
            {
                return ObstacleDamageResolutionResult.Empty();
            }

            var damagedVaseCoordinates = new HashSet<BoardCoordinate>();
            var vaseCoordinatesInOrder = new List<BoardCoordinate>();
            var touchedChaliceCellsByBox = new Dictionary<ChaliceBoxObstacleModel, HashSet<BoardCoordinate>>();
            var chaliceBoxesInOrder = new List<ChaliceBoxObstacleModel>();
            var damages = new List<ObstacleDamage>();
            var removedCoordinates = new List<BoardCoordinate>();

            foreach (var blastedCoordinate in blast.BlastCoordinates)
            {
                foreach (var neighborCoordinate in blastedCoordinate.GetOrthogonalNeighbors())
                {
                    if (!board.TryGetCell(neighborCoordinate, out var neighborCell) || neighborCell.Obstacle is null)
                    {
                        continue;
                    }

                    switch (neighborCell.Obstacle)
                    {
                        case VaseObstacleModel:
                            // Multi-cell obstacles are aggregated by shared reference, while vases still damage once per adjacent cell.
                            if (damagedVaseCoordinates.Add(neighborCoordinate))
                            {
                                vaseCoordinatesInOrder.Add(neighborCoordinate);
                            }

                            break;
                        case ChaliceBoxObstacleModel chaliceBox:
                            if (!touchedChaliceCellsByBox.TryGetValue(chaliceBox, out var touchedCells))
                            {
                                touchedCells = new HashSet<BoardCoordinate>();
                                touchedChaliceCellsByBox.Add(chaliceBox, touchedCells);
                                chaliceBoxesInOrder.Add(chaliceBox);
                            }

                            touchedCells.Add(neighborCoordinate);
                            break;
                    }
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

            foreach (var chaliceBox in chaliceBoxesInOrder)
            {
                var touchedCells = touchedChaliceCellsByBox[chaliceBox];

                if (chaliceBox.RemainingDoorDurability > 0)
                {
                    chaliceBox.RemainingDoorDurability = Math.Max(0, chaliceBox.RemainingDoorDurability - 1);
                    damages.Add(new ObstacleDamage(chaliceBox.Anchor, amount: 1));
                    continue;
                }

                var chaliceDamage = Math.Min(
                    touchedCells.Count,
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
