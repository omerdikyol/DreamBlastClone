using System;
using System.Collections.Generic;
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

            var damagedCoordinates = new HashSet<Core.BoardCoordinate>();
            var damages = new List<ObstacleDamage>();

            foreach (var blastedCoordinate in blast.RemovedCoordinates)
            {
                foreach (var neighborCoordinate in blastedCoordinate.GetOrthogonalNeighbors())
                {
                    if (!damagedCoordinates.Add(neighborCoordinate) || !board.TryGetCell(neighborCoordinate, out var neighborCell))
                    {
                        continue;
                    }

                    // A vase can be adjacent to multiple blasted cubes, but one blast event should damage it only once.
                    if (neighborCell.Obstacle is not VaseObstacleModel vase)
                    {
                        continue;
                    }

                    vase.RemainingDurability -= 1;
                    if (vase.RemainingDurability <= 0)
                    {
                        board.ClearObstacle(neighborCoordinate);
                    }

                    damages.Add(new ObstacleDamage(neighborCoordinate, amount: 1));
                }
            }

            return damages.Count == 0
                ? ObstacleDamageResolutionResult.Empty()
                : new ObstacleDamageResolutionResult(damages);
        }
    }
}
