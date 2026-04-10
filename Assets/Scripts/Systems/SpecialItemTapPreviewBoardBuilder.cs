using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemTapPreviewBoardBuilder
    {
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();

        public BoardModel Build(BoardModel preTapBoard, SpecialItemTapPipelineResult tap)
        {
            if (preTapBoard is null)
            {
                throw new ArgumentNullException(nameof(preTapBoard));
            }

            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            var previewBoard = boardModelCloner.Clone(preTapBoard);
            if (!tap.IsValidTap)
            {
                return previewBoard;
            }

            var removedItemCoordinates = CollectRemovedItemCoordinates(tap);

            foreach (var removedCoordinate in removedItemCoordinates)
            {
                previewBoard.ClearItem(removedCoordinate);
            }

            ApplyObstacleDamagePreview(previewBoard, tap.ObstacleDamage);
            return previewBoard;
        }

        private static void ApplyObstacleDamagePreview(BoardModel previewBoard, ObstacleDamageResolutionResult obstacleDamage)
        {
            foreach (var damage in obstacleDamage.Damages)
            {
                if (!previewBoard.TryGetCell(damage.Coordinate, out var cell) || cell.Obstacle is null)
                {
                    continue;
                }

                switch (cell.Obstacle)
                {
                    case VaseObstacleModel vase:
                        vase.RemainingDurability = Math.Max(0, vase.RemainingDurability - damage.Amount);
                        break;
                    case StoneObstacleModel stone:
                        stone.RemainingDurability = Math.Max(0, stone.RemainingDurability - damage.Amount);
                        break;
                    case ChaliceBoxObstacleModel chaliceBox:
                        if (chaliceBox.RemainingDoorDurability > 0)
                        {
                            chaliceBox.RemainingDoorDurability = Math.Max(0, chaliceBox.RemainingDoorDurability - damage.Amount);
                        }
                        else
                        {
                            chaliceBox.CollectedChaliceCount = Math.Min(
                                chaliceBox.RequiredChaliceCount,
                                chaliceBox.CollectedChaliceCount + damage.Amount);
                        }

                        break;
                }
            }

            foreach (var removedCoordinate in obstacleDamage.RemovedCoordinates)
            {
                if (!previewBoard.TryGetCell(removedCoordinate, out var cell) || cell.Obstacle is null)
                {
                    continue;
                }

                previewBoard.ClearObstacle(cell.Obstacle);
            }
        }

        private static IReadOnlyList<BoardCoordinate> CollectRemovedItemCoordinates(SpecialItemTapPipelineResult tap)
        {
            var removedCoordinates = new List<BoardCoordinate>();
            var seenCoordinates = new HashSet<BoardCoordinate>();
            var initialRemovedCoordinates = tap.Combo.IsComboActivated
                ? tap.Combo.RemovedItemCoordinates
                : tap.Activation.RemovedItemCoordinates;

            foreach (var coordinate in initialRemovedCoordinates)
            {
                if (seenCoordinates.Add(coordinate))
                {
                    removedCoordinates.Add(coordinate);
                }
            }

            foreach (var triggeredActivation in tap.TriggeredActivations)
            {
                foreach (var coordinate in triggeredActivation.Activation.RemovedItemCoordinates)
                {
                    if (seenCoordinates.Add(coordinate))
                    {
                        removedCoordinates.Add(coordinate);
                    }
                }
            }

            return removedCoordinates;
        }
    }
}
