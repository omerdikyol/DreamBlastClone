using System;
using System.Collections.Generic;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class VaseParticleDescriptorBuilder
    {
        public VaseParticleDescriptor Build(BoardModel preTapBoard, BoardTapDispatchResult tap)
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
                return VaseParticleDescriptor.Empty();
            }

            var obstacleDamage = tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.ObstacleDamage,
                TapRouteType.SpecialItem => tap.SpecialItem.ObstacleDamage,
                _ => ObstacleDamageResolutionResult.Empty()
            };

            if (!obstacleDamage.HasAnyDamage)
            {
                return VaseParticleDescriptor.Empty();
            }

            var removedCoordinates = new HashSet<Core.BoardCoordinate>(obstacleDamage.RemovedCoordinates);
            var events = new List<VaseParticleEvent>();
            var seenCoordinates = new HashSet<Core.BoardCoordinate>();

            foreach (var damage in obstacleDamage.Damages)
            {
                if (!seenCoordinates.Add(damage.Coordinate))
                {
                    continue;
                }

                if (!preTapBoard.TryGetCell(damage.Coordinate, out var cell) || cell.Obstacle is not VaseObstacleModel)
                {
                    continue;
                }

                events.Add(new VaseParticleEvent(
                    damage.Coordinate,
                    removedCoordinates.Contains(damage.Coordinate)));
            }

            return events.Count == 0
                ? VaseParticleDescriptor.Empty()
                : new VaseParticleDescriptor(events);
        }
    }
}
