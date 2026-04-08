using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Obstacles
{
    public sealed class ChaliceBoxObstacleModel : ObstacleModel
    {
        private readonly BoardCoordinate[] occupiedCoordinates;

        public ChaliceBoxObstacleModel(
            BoardCoordinate anchor,
            int remainingDoorDurability,
            int requiredChaliceCount = 10,
            int collectedChaliceCount = 0)
        {
            ValidateNonNegative(nameof(remainingDoorDurability), remainingDoorDurability);
            ValidatePositive(nameof(requiredChaliceCount), requiredChaliceCount);
            ValidateNonNegative(nameof(collectedChaliceCount), collectedChaliceCount);

            if (collectedChaliceCount > requiredChaliceCount)
            {
                throw new ArgumentOutOfRangeException(nameof(collectedChaliceCount), collectedChaliceCount, "Collected chalices cannot exceed the required chalice count.");
            }

            Anchor = anchor;
            RemainingDoorDurability = remainingDoorDurability;
            RequiredChaliceCount = requiredChaliceCount;
            CollectedChaliceCount = collectedChaliceCount;

            // Anchor is always the bottom-left cell of the fixed 2x2 footprint.
            occupiedCoordinates = new[]
            {
                anchor,
                anchor.Offset(1, 0),
                anchor.Offset(0, 1),
                anchor.Offset(1, 1)
            };
        }

        public override int FootprintWidth => 2;

        public override int FootprintHeight => 2;

        public BoardCoordinate Anchor { get; }

        public int RemainingDoorDurability { get; set; }

        public int RequiredChaliceCount { get; }

        public int CollectedChaliceCount { get; set; }

        public int RemainingChaliceCount => RequiredChaliceCount - CollectedChaliceCount;

        public IReadOnlyList<BoardCoordinate> OccupiedCoordinates => occupiedCoordinates;
    }
}
