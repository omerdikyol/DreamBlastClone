using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class ChaliceBoxParticleDescriptorBuilder
    {
        public ChaliceBoxParticleDescriptor Build(
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
                return ChaliceBoxParticleDescriptor.Empty();
            }

            var obstacleDamage = tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.ObstacleDamage,
                TapRouteType.SpecialItem => tap.SpecialItem.ObstacleDamage,
                _ => ObstacleDamageResolutionResult.Empty()
            };

            if (!obstacleDamage.HasAnyDamage)
            {
                return ChaliceBoxParticleDescriptor.Empty();
            }

            var removedAnchors = new HashSet<BoardCoordinate>(obstacleDamage.RemovedCoordinates);
            var events = new List<ChaliceBoxParticleEvent>();
            var seenAnchors = new HashSet<BoardCoordinate>();

            foreach (var damage in obstacleDamage.Damages)
            {
                if (!seenAnchors.Add(damage.Coordinate))
                {
                    continue;
                }

                if (!preTapBoard.TryGetCell(damage.Coordinate, out var cell) || cell.Obstacle is not ChaliceBoxObstacleModel chaliceBox)
                {
                    continue;
                }

                events.Add(new ChaliceBoxParticleEvent(
                    chaliceBox.Anchor,
                    ClassifyEventType(chaliceBox, damage.Amount, removedAnchors.Contains(chaliceBox.Anchor)),
                    damage.Amount,
                    ResolveHitStep(preTapBoard, tap, tapCoordinate, chaliceBox, damage.Coordinate)));
            }

            return events.Count == 0
                ? ChaliceBoxParticleDescriptor.Empty()
                : new ChaliceBoxParticleDescriptor(events);
        }

        private static int ResolveHitStep(
            BoardModel preTapBoard,
            BoardTapDispatchResult tap,
            BoardCoordinate? tapCoordinate,
            ChaliceBoxObstacleModel chaliceBox,
            BoardCoordinate obstacleCoordinate)
        {
            if (tap.RouteType != TapRouteType.SpecialItem)
            {
                return 0;
            }

            if (tap.SpecialItem.Combo.IsComboActivated)
            {
                return tapCoordinate.HasValue
                    ? ResolveBestHitStep(
                        preTapBoard,
                        chaliceBox,
                        tap.SpecialItem.Combo.AffectedCoordinates,
                        tapCoordinate.Value,
                        IsTntBasedCombo(tap.SpecialItem.Combo.ComboType),
                        IsTntBasedCombo(tap.SpecialItem.Combo.ComboType) ? 1 : 0,
                        obstacleCoordinate)
                    : 0;
            }

            var bestHitStep = int.MaxValue;
            if (ShouldStaggerSpecialActivation(tap.SpecialItem.Activation) && tapCoordinate.HasValue)
            {
                bestHitStep = Math.Min(
                    bestHitStep,
                    ResolveBestHitStep(
                        preTapBoard,
                        chaliceBox,
                        tap.SpecialItem.Activation.AffectedCoordinates,
                        tapCoordinate.Value,
                        IsTntActivation(tap.SpecialItem.Activation),
                        IsTntActivation(tap.SpecialItem.Activation) ? 1 : 0,
                        obstacleCoordinate));
            }

            foreach (var triggeredActivation in tap.SpecialItem.TriggeredActivations)
            {
                if (!ShouldStaggerSpecialActivation(triggeredActivation.Activation))
                {
                    continue;
                }

                bestHitStep = Math.Min(
                    bestHitStep,
                    ResolveBestHitStep(
                        preTapBoard,
                        chaliceBox,
                        triggeredActivation.Activation.AffectedCoordinates,
                        triggeredActivation.OriginCoordinate,
                        IsTntActivation(triggeredActivation.Activation),
                        IsTntActivation(triggeredActivation.Activation) ? 1 : 0,
                        obstacleCoordinate));
            }

            return bestHitStep == int.MaxValue ? 0 : bestHitStep;
        }

        private static int ResolveBestHitStep(
            BoardModel preTapBoard,
            ChaliceBoxObstacleModel chaliceBox,
            IReadOnlyList<BoardCoordinate> affectedCoordinates,
            BoardCoordinate origin,
            bool useSquareDistance,
            int hitStepOffset,
            BoardCoordinate fallbackCoordinate)
        {
            var bestDistance = int.MaxValue;

            foreach (var affectedCoordinate in affectedCoordinates)
            {
                if (!preTapBoard.TryGetCell(affectedCoordinate, out var cell)
                    || !ReferenceEquals(cell.Obstacle, chaliceBox))
                {
                    continue;
                }

                bestDistance = Math.Min(bestDistance, ResolveDistance(affectedCoordinate, origin, useSquareDistance));
            }

            if (bestDistance == int.MaxValue)
            {
                bestDistance = ResolveDistance(fallbackCoordinate, origin, useSquareDistance);
            }

            return bestDistance + hitStepOffset;
        }

        private static int ResolveDistance(BoardCoordinate coordinate, BoardCoordinate origin, bool useSquareDistance)
        {
            var deltaX = Math.Abs(coordinate.X - origin.X);
            var deltaY = Math.Abs(coordinate.Y - origin.Y);
            return useSquareDistance ? Math.Max(deltaX, deltaY) : deltaX + deltaY;
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

        private static bool IsTntBasedCombo(SpecialItemComboType comboType)
        {
            return comboType is SpecialItemComboType.TntTnt or SpecialItemComboType.TntRocket;
        }

        private static ChaliceBoxParticleEventType ClassifyEventType(
            ChaliceBoxObstacleModel chaliceBox,
            int amount,
            bool isRemoved)
        {
            if (chaliceBox.RemainingDoorDurability > 0)
            {
                return amount >= chaliceBox.RemainingDoorDurability
                    ? ChaliceBoxParticleEventType.DoorBreak
                    : ChaliceBoxParticleEventType.DoorDamage;
            }

            return isRemoved
                ? ChaliceBoxParticleEventType.ChaliceComplete
                : ChaliceBoxParticleEventType.ChaliceDamage;
        }
    }
}
