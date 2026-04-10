using System;
using System.Collections.Generic;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class StoneParticleDescriptorBuilder
    {
        public StoneParticleDescriptor Build(BoardModel preTapBoard, BoardTapDispatchResult tap)
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
                return StoneParticleDescriptor.Empty();
            }

            var obstacleDamage = tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.ObstacleDamage,
                TapRouteType.SpecialItem => tap.SpecialItem.ObstacleDamage,
                _ => ObstacleDamageResolutionResult.Empty()
            };

            if (!obstacleDamage.HasAnyRemoval)
            {
                return StoneParticleDescriptor.Empty();
            }

            var removedCoordinates = new List<Core.BoardCoordinate>();
            var seenCoordinates = new HashSet<Core.BoardCoordinate>();

            foreach (var coordinate in obstacleDamage.RemovedCoordinates)
            {
                if (!seenCoordinates.Add(coordinate))
                {
                    continue;
                }

                if (!preTapBoard.TryGetCell(coordinate, out var cell) || cell.Obstacle is not StoneObstacleModel)
                {
                    continue;
                }

                removedCoordinates.Add(coordinate);
            }

            return removedCoordinates.Count == 0
                ? StoneParticleDescriptor.Empty()
                : new StoneParticleDescriptor(removedCoordinates);
        }
    }
}
