using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class BoardDestructionFeedbackDescriptorBuilder
    {
        public BoardDestructionFeedbackDescriptor Build(BoardModel preTapBoard, BoardTapDispatchResult tap)
        {
            if (preTapBoard is null)
            {
                throw new ArgumentNullException(nameof(preTapBoard));
            }

            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            if (!tap.IsValidTap)
            {
                return BoardDestructionFeedbackDescriptor.Empty();
            }

            var removedItemCoordinates = CollectRemovedItems(tap);
            var removedObstacles = CollectRemovedObstacles(preTapBoard, tap);
            return new BoardDestructionFeedbackDescriptor(removedItemCoordinates, removedObstacles);
        }

        private static IReadOnlyList<BoardCoordinate> CollectRemovedItems(BoardTapDispatchResult tap)
        {
            var removedCoordinates = new List<BoardCoordinate>();
            var seenCoordinates = new HashSet<BoardCoordinate>();

            switch (tap.RouteType)
            {
                case TapRouteType.NormalCube:
                    var createdSpecialCoordinate = tap.NormalCube.Blast.CreatedSpecialCoordinate;
                    foreach (var coordinate in tap.NormalCube.Blast.RemovedCoordinates)
                    {
                        if (createdSpecialCoordinate.HasValue && coordinate == createdSpecialCoordinate.Value)
                        {
                            continue;
                        }

                        if (seenCoordinates.Add(coordinate))
                        {
                            removedCoordinates.Add(coordinate);
                        }
                    }

                    break;
                case TapRouteType.SpecialItem:
                    var removedItemCoordinates = tap.SpecialItem.Combo.IsComboActivated
                        ? tap.SpecialItem.Combo.RemovedItemCoordinates
                        : tap.SpecialItem.Activation.RemovedItemCoordinates;

                    foreach (var coordinate in removedItemCoordinates)
                    {
                        if (seenCoordinates.Add(coordinate))
                        {
                            removedCoordinates.Add(coordinate);
                        }
                    }

                    break;
            }

            return removedCoordinates;
        }

        private static IReadOnlyList<RemovedObstacleFeedback> CollectRemovedObstacles(BoardModel preTapBoard, BoardTapDispatchResult tap)
        {
            var obstacleDamage = tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.ObstacleDamage,
                TapRouteType.SpecialItem => tap.SpecialItem.ObstacleDamage,
                _ => ObstacleDamageResolutionResult.Empty()
            };

            if (!obstacleDamage.HasAnyRemoval)
            {
                return Array.Empty<RemovedObstacleFeedback>();
            }

            var removedObstacles = new List<RemovedObstacleFeedback>();
            var seenObstacles = new HashSet<Obstacles.ObstacleModel>();

            foreach (var coordinate in obstacleDamage.RemovedCoordinates)
            {
                if (!preTapBoard.TryGetCell(coordinate, out var cell) || cell.Obstacle is null || !seenObstacles.Add(cell.Obstacle))
                {
                    continue;
                }

                removedObstacles.Add(new RemovedObstacleFeedback(FindObstacleFootprint(preTapBoard, cell.Obstacle)));
            }

            return removedObstacles;
        }

        private static IReadOnlyList<BoardCoordinate> FindObstacleFootprint(BoardModel board, Obstacles.ObstacleModel obstacle)
        {
            var occupiedCoordinates = new List<BoardCoordinate>();

            foreach (var cell in board.GetAllCells())
            {
                if (ReferenceEquals(cell.Obstacle, obstacle))
                {
                    occupiedCoordinates.Add(cell.Coordinate);
                }
            }

            return occupiedCoordinates;
        }
    }
}
