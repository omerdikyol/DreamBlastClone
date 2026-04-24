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
            var removedObstacles = CollectRemovedObstacles(preTapBoard, tap, tapCoordinate);
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
                        AddRemovedItems(
                            tap.SpecialItem.Combo.RemovedItemCoordinates,
                            hitOrigin: null,
                            growCoordinate: null,
                            hitStepOffset: 0,
                            useSquareDistance: false,
                            removedItems,
                            seenCoordinates);
                    }
                    else
                    {
                        var hitOrigin = ShouldStaggerSpecialActivation(tap.SpecialItem.Activation) ? tapCoordinate : null;
                        AddRemovedItems(
                            tap.SpecialItem.Activation.RemovedItemCoordinates,
                            hitOrigin,
                            growCoordinate: IsTntActivation(tap.SpecialItem.Activation) ? tapCoordinate : null,
                            hitStepOffset: IsTntActivation(tap.SpecialItem.Activation) ? 1 : 0,
                            useSquareDistance: IsTntActivation(tap.SpecialItem.Activation),
                            removedItems,
                            seenCoordinates);
                    }

                    foreach (var triggeredActivation in tap.SpecialItem.TriggeredActivations)
                    {
                        var hitOrigin = ShouldStaggerSpecialActivation(triggeredActivation.Activation)
                            ? triggeredActivation.OriginCoordinate
                            : (BoardCoordinate?)null;
                        AddRemovedItems(
                            triggeredActivation.Activation.RemovedItemCoordinates,
                            hitOrigin,
                            growCoordinate: IsTntActivation(triggeredActivation.Activation) ? triggeredActivation.OriginCoordinate : null,
                            hitStepOffset: IsTntActivation(triggeredActivation.Activation) ? 1 : 0,
                            useSquareDistance: IsTntActivation(triggeredActivation.Activation),
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
            BoardCoordinate? growCoordinate,
            int hitStepOffset,
            bool useSquareDistance,
            List<RemovedItemFeedback> removedItems,
            HashSet<BoardCoordinate> seenCoordinates)
        {
            foreach (var coordinate in coordinates)
            {
                if (!seenCoordinates.Add(coordinate))
                {
                    continue;
                }

                removedItems.Add(new RemovedItemFeedback(
                    coordinate,
                    ResolveHitStep(coordinate, hitOrigin, useSquareDistance) + hitStepOffset,
                    growsBeforeRemoval: growCoordinate.HasValue && coordinate == growCoordinate.Value));
            }
        }

        private static int ResolveHitStep(BoardCoordinate coordinate, BoardCoordinate? hitOrigin, bool useSquareDistance)
        {
            if (!hitOrigin.HasValue)
            {
                return 0;
            }

            var deltaX = Math.Abs(coordinate.X - hitOrigin.Value.X);
            var deltaY = Math.Abs(coordinate.Y - hitOrigin.Value.Y);
            return useSquareDistance
                ? Math.Max(deltaX, deltaY)
                : deltaX + deltaY;
        }

        private static bool ShouldStaggerSpecialActivation(SpecialItemActivationResult activation)
        {
            return activation.IsValidActivation
                && (activation.ActivationType == SpecialActivationType.Rocket
                    || activation.ActivationType == SpecialActivationType.Tnt);
        }

        private static bool IsTntActivation(SpecialItemActivationResult activation)
        {
            return activation.IsValidActivation
                && activation.ActivationType == SpecialActivationType.Tnt;
        }

        private static IReadOnlyList<RemovedObstacleFeedback> CollectRemovedObstacles(
            BoardModel preTapBoard,
            BoardTapDispatchResult tap,
            BoardCoordinate? tapCoordinate)
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
            var obstacleFeedbackByObstacle = new Dictionary<Obstacles.ObstacleModel, ObstacleFeedbackDraft>();

            foreach (var coordinate in obstacleDamage.RemovedCoordinates)
            {
                if (!preTapBoard.TryGetCell(coordinate, out var cell) || cell.Obstacle is null)
                {
                    continue;
                }

                var hitStep = ResolveObstacleHitStep(tap, tapCoordinate, coordinate);
                if (obstacleFeedbackByObstacle.TryGetValue(cell.Obstacle, out var draft))
                {
                    draft.HitStep = Math.Min(draft.HitStep, hitStep);
                    obstacleFeedbackByObstacle[cell.Obstacle] = draft;
                    continue;
                }

                obstacleFeedbackByObstacle.Add(
                    cell.Obstacle,
                    new ObstacleFeedbackDraft(
                        FindObstacleFootprint(preTapBoard, cell.Obstacle),
                        hitStep));
            }

            foreach (var draft in obstacleFeedbackByObstacle.Values)
            {
                removedObstacles.Add(new RemovedObstacleFeedback(draft.OccupiedCoordinates, draft.HitStep));
            }

            return removedObstacles;
        }

        private static int ResolveObstacleHitStep(
            BoardTapDispatchResult tap,
            BoardCoordinate? tapCoordinate,
            BoardCoordinate obstacleCoordinate)
        {
            if (tap.RouteType != TapRouteType.SpecialItem || tap.SpecialItem.Combo.IsComboActivated)
            {
                return 0;
            }

            var bestHitStep = int.MaxValue;
            if (ShouldStaggerSpecialActivation(tap.SpecialItem.Activation) && tapCoordinate.HasValue)
            {
                bestHitStep = Math.Min(
                    bestHitStep,
                    ResolveHitStep(
                        obstacleCoordinate,
                        tapCoordinate,
                        IsTntActivation(tap.SpecialItem.Activation)) + (IsTntActivation(tap.SpecialItem.Activation) ? 1 : 0));
            }

            foreach (var triggeredActivation in tap.SpecialItem.TriggeredActivations)
            {
                if (!ShouldStaggerSpecialActivation(triggeredActivation.Activation))
                {
                    continue;
                }

                bestHitStep = Math.Min(
                    bestHitStep,
                    ResolveHitStep(
                        obstacleCoordinate,
                        triggeredActivation.OriginCoordinate,
                        IsTntActivation(triggeredActivation.Activation)) + (IsTntActivation(triggeredActivation.Activation) ? 1 : 0));
            }

            return bestHitStep == int.MaxValue ? 0 : bestHitStep;
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

        private struct ObstacleFeedbackDraft
        {
            public ObstacleFeedbackDraft(IReadOnlyList<BoardCoordinate> occupiedCoordinates, int hitStep)
            {
                OccupiedCoordinates = occupiedCoordinates;
                HitStep = hitStep;
            }

            public IReadOnlyList<BoardCoordinate> OccupiedCoordinates { get; }

            public int HitStep { get; set; }
        }
    }
}
