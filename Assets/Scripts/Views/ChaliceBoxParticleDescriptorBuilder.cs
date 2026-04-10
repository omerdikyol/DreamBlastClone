using System;
using System.Collections.Generic;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class ChaliceBoxParticleDescriptorBuilder
    {
        public ChaliceBoxParticleDescriptor Build(BoardModel preTapBoard, BoardTapDispatchResult tap)
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

            var removedAnchors = new HashSet<Core.BoardCoordinate>(obstacleDamage.RemovedCoordinates);
            var events = new List<ChaliceBoxParticleEvent>();
            var seenAnchors = new HashSet<Core.BoardCoordinate>();

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
                    damage.Amount));
            }

            return events.Count == 0
                ? ChaliceBoxParticleDescriptor.Empty()
                : new ChaliceBoxParticleDescriptor(events);
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
