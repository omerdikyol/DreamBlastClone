using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class CubeBlastResolutionResult
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public CubeBlastResolutionResult(
            bool isValidBlast,
            IReadOnlyList<BoardCoordinate> blastCoordinates,
            IReadOnlyList<BoardCoordinate> removedCoordinates,
            int blastedGroupSize,
            CubeColor? blastedCubeColor,
            BoardCoordinate? createdSpecialCoordinate,
            ItemModel createdSpecialItem)
        {
            if (blastedGroupSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blastedGroupSize), blastedGroupSize, "Blasted group size cannot be negative.");
            }

            if ((createdSpecialCoordinate.HasValue && createdSpecialItem is null)
                || (!createdSpecialCoordinate.HasValue && createdSpecialItem is not null))
            {
                throw new ArgumentException("Created special coordinate and item must either both be set or both be empty.");
            }

            IsValidBlast = isValidBlast;
            BlastCoordinates = blastCoordinates ?? throw new ArgumentNullException(nameof(blastCoordinates));
            RemovedCoordinates = removedCoordinates ?? throw new ArgumentNullException(nameof(removedCoordinates));
            BlastedGroupSize = blastedGroupSize;
            BlastedCubeColor = blastedCubeColor;
            CreatedSpecialCoordinate = createdSpecialCoordinate;
            CreatedSpecialItem = createdSpecialItem;
        }

        public bool IsValidBlast { get; }

        public IReadOnlyList<BoardCoordinate> BlastCoordinates { get; }

        public IReadOnlyList<BoardCoordinate> RemovedCoordinates { get; }

        public int BlastedGroupSize { get; }

        public CubeColor? BlastedCubeColor { get; }

        public BoardCoordinate? CreatedSpecialCoordinate { get; }

        public ItemModel CreatedSpecialItem { get; }

        public static CubeBlastResolutionResult Invalid()
        {
            return new CubeBlastResolutionResult(
                isValidBlast: false,
                blastCoordinates: EmptyCoordinates,
                removedCoordinates: EmptyCoordinates,
                blastedGroupSize: 0,
                blastedCubeColor: null,
                createdSpecialCoordinate: null,
                createdSpecialItem: null);
        }
    }
}
