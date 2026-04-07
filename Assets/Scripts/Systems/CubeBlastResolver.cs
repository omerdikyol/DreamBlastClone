using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

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

            var createdSpecialItem = CreateSpecialItem(group);
            var removedCoordinates = new List<BoardCoordinate>(group.Count);

            // The blast footprint still includes the tap cell even when it becomes a spawned special afterward.
            foreach (var coordinate in group.Coordinates)
            {
                if (createdSpecialItem is not null && coordinate == startCoordinate)
                {
                    continue;
                }

                board.ClearItem(coordinate);
                removedCoordinates.Add(coordinate);
            }

            if (createdSpecialItem is not null)
            {
                board.ClearItem(startCoordinate);
                board.PlaceItem(startCoordinate, createdSpecialItem);
            }

            return new CubeBlastResolutionResult(
                isValidBlast: true,
                blastCoordinates: group.Coordinates,
                removedCoordinates: removedCoordinates,
                blastedGroupSize: group.Count,
                blastedCubeColor: group.Color,
                createdSpecialCoordinate: createdSpecialItem is null ? null : startCoordinate,
                createdSpecialItem: createdSpecialItem);
        }

        private static ItemModel CreateSpecialItem(CubeGroupDetectionResult group)
        {
            if (group.Count == 4)
            {
                return new RocketItemModel(ResolveRocketOrientation(group));
            }

            if (group.Count >= 6)
            {
                return new TntItemModel();
            }

            return null;
        }

        private static RocketOrientation ResolveRocketOrientation(CubeGroupDetectionResult group)
        {
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;

            foreach (var coordinate in group.Coordinates)
            {
                minX = Math.Min(minX, coordinate.X);
                maxX = Math.Max(maxX, coordinate.X);
                minY = Math.Min(minY, coordinate.Y);
                maxY = Math.Max(maxY, coordinate.Y);
            }

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            return height > width
                ? RocketOrientation.Vertical
                : RocketOrientation.Horizontal;
        }
    }
}
