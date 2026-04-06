using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public sealed class CubeGroupDetectionResult
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public CubeGroupDetectionResult(CubeColor? color, IReadOnlyList<BoardCoordinate> coordinates, bool isValidStart)
        {
            Color = color;
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
            IsValidStart = isValidStart;
        }

        public bool IsValidStart { get; }

        public CubeColor? Color { get; }

        public IReadOnlyList<BoardCoordinate> Coordinates { get; }

        public int Count => Coordinates.Count;

        public static CubeGroupDetectionResult Invalid()
        {
            return new CubeGroupDetectionResult(color: null, EmptyCoordinates, isValidStart: false);
        }
    }
}
