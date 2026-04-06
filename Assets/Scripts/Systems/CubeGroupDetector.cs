using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class CubeGroupDetector
    {
        public CubeGroupDetectionResult FindGroup(BoardModel board, BoardCoordinate startCoordinate)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.TryGetCell(startCoordinate, out var startCell) || startCell.Item is not CubeItemModel startCube)
            {
                return CubeGroupDetectionResult.Invalid();
            }

            var visited = new HashSet<BoardCoordinate> { startCoordinate };
            var queue = new Queue<BoardCoordinate>();
            var coordinates = new List<BoardCoordinate>();

            queue.Enqueue(startCoordinate);

            while (queue.Count > 0)
            {
                var currentCoordinate = queue.Dequeue();
                coordinates.Add(currentCoordinate);

                foreach (var neighborCoordinate in currentCoordinate.GetOrthogonalNeighbors())
                {
                    if (visited.Contains(neighborCoordinate) || !board.TryGetCell(neighborCoordinate, out var neighborCell))
                    {
                        continue;
                    }

                    // Cube connectivity is defined only by the item layer; obstacle state does not participate here.
                    if (neighborCell.Item is not CubeItemModel neighborCube || neighborCube.Color != startCube.Color)
                    {
                        continue;
                    }

                    visited.Add(neighborCoordinate);
                    queue.Enqueue(neighborCoordinate);
                }
            }

            return new CubeGroupDetectionResult(startCube.Color, coordinates, isValidStart: true);
        }
    }
}
