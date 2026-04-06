using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class CubeBlastResolver
    {
        private readonly CubeGroupDetector cubeGroupDetector = new CubeGroupDetector();

        public CubeBlastResolutionResult Resolve(BoardModel board, BoardCoordinate startCoordinate)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var group = cubeGroupDetector.FindGroup(board, startCoordinate);
            if (!group.IsValidStart || group.Count < 2)
            {
                return CubeBlastResolutionResult.Invalid();
            }

            // Base blast resolution only removes cubes from the item layer; cascades and obstacle effects come later.
            foreach (var coordinate in group.Coordinates)
            {
                board.ClearItem(coordinate);
            }

            return new CubeBlastResolutionResult(
                isValidBlast: true,
                removedCoordinates: group.Coordinates,
                blastedGroupSize: group.Count,
                blastedCubeColor: group.Color);
        }
    }
}
