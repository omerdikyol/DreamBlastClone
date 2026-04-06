using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public sealed class CubeBlastResolutionResult
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public CubeBlastResolutionResult(bool isValidBlast, IReadOnlyList<BoardCoordinate> removedCoordinates, int blastedGroupSize, CubeColor? blastedCubeColor)
        {
            if (blastedGroupSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blastedGroupSize), blastedGroupSize, "Blasted group size cannot be negative.");
            }

            IsValidBlast = isValidBlast;
            RemovedCoordinates = removedCoordinates ?? throw new ArgumentNullException(nameof(removedCoordinates));
            BlastedGroupSize = blastedGroupSize;
            BlastedCubeColor = blastedCubeColor;
        }

        public bool IsValidBlast { get; }

        public IReadOnlyList<BoardCoordinate> RemovedCoordinates { get; }

        public int BlastedGroupSize { get; }

        public CubeColor? BlastedCubeColor { get; }

        public static CubeBlastResolutionResult Invalid()
        {
            return new CubeBlastResolutionResult(isValidBlast: false, EmptyCoordinates, blastedGroupSize: 0, blastedCubeColor: null);
        }
    }
}
