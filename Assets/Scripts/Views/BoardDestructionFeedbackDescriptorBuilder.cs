using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class BoardDestructionFeedbackDescriptorBuilder
    {
        public BoardDestructionFeedbackDescriptor Build(
            BoardModel preTapBoard,
            BoardTapDispatchResult tap,
            BoardCoordinate? tapCoordinate = null)
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

            var removedItems = CollectRemovedItems(tap, tapCoordinate);
            var removedObstacles = CollectRemovedObstacles(preTapBoard, tap);
            return new BoardDestructionFeedbackDescriptor(removedItems, removedObstacles);
        }

        private static IReadOnlyList<RemovedItemFeedback> CollectRemovedItems(
            BoardTapDispatchResult tap,
            BoardCoordinate? tapCoordinate)
        {
            var removedItems = new List<RemovedItemFeedback>();
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
                            removedItems.Add(new RemovedItemFeedback(coordinate, hitStep: 0));
                        }
                    }

                    break;
                case TapRouteType.SpecialItem:
                    if (tap.SpecialItem.Combo.IsComboActivated)
                    {
                        AddRemovedItems(tap.SpecialItem.Combo.RemovedItemCoordinates, hitOrigin: null, removedItems, seenCoordinates);
                    }
                    else
                    {
                        var hitOrigin = ShouldStaggerRocketActivation(tap.SpecialItem.Activation) ? tapCoordinate : null;
                        AddRemovedItems(tap.SpecialItem.Activation.RemovedItemCoordinates, hitOrigin, removedItems, seenCoordinates);
                    }

                    foreach (var triggeredActivation in tap.SpecialItem.TriggeredActivations)
                    {
                        var hitOrigin = ShouldStaggerRocketActivation(triggeredActivation.Activation)
                            ? triggeredActivation.OriginCoordinate
                            : (BoardCoordinate?)null;
                        AddRemovedItems(
                            triggeredActivation.Activation.RemovedItemCoordinates,
                            hitOrigin,
                            removedItems,
                            seenCoordinates);
                    }

                    break;
            }

            return removedItems;
        }

        private static void AddRemovedItems(
            IReadOnlyList<BoardCoordinate> coordinates,
            BoardCoordinate? hitOrigin,
            List<RemovedItemFeedback> removedItems,
            HashSet<BoardCoordinate> seenCoordinates)
        {
            foreach (var coordinate in coordinates)
            {
                if (!seenCoordinates.Add(coordinate))
                {
                    continue;
                }

                removedItems.Add(new RemovedItemFeedback(coordinate, ResolveHitStep(coordinate, hitOrigin)));
            }
        }

        private static int ResolveHitStep(BoardCoordinate coordinate, BoardCoordinate? hitOrigin)
        {
            if (!hitOrigin.HasValue)
            {
                return 0;
            }

            return Math.Abs(coordinate.X - hitOrigin.Value.X)
                + Math.Abs(coordinate.Y - hitOrigin.Value.Y);
        }

        private static bool ShouldStaggerRocketActivation(SpecialItemActivationResult activation)
        {
            return activation.IsValidActivation
                && activation.ActivationType == SpecialActivationType.Rocket;
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
