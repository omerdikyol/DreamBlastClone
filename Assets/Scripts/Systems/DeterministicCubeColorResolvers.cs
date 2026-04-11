using DreamBlastClone.Core;
using DreamBlastClone.Data;

namespace DreamBlastClone.Systems
{
    public sealed class DeterministicInitialCubeColorResolver : IRandomCubeColorResolver
    {
        private readonly uint seed;

        public DeterministicInitialCubeColorResolver(uint seed)
        {
            this.seed = seed;
        }

        public CubeColor ResolveColor(BoardCoordinate coordinate)
        {
            var coordinateHash = BuildCoordinateHash(coordinate);
            return ResolveColorFromIndex((seed ^ coordinateHash) & 3u);
        }

        private static uint BuildCoordinateHash(BoardCoordinate coordinate)
        {
            return unchecked((uint)(coordinate.X * 73856093) ^ (uint)(coordinate.Y * 19349663));
        }

        private static CubeColor ResolveColorFromIndex(uint colorIndex)
        {
            return colorIndex switch
            {
                0u => CubeColor.Red,
                1u => CubeColor.Green,
                2u => CubeColor.Blue,
                _ => CubeColor.Yellow
            };
        }
    }

    public sealed class DeterministicRefillSequenceResolver : IRefillCubeColorResolver
    {
        private readonly uint seed;
        private uint nextSequenceIndex;

        public DeterministicRefillSequenceResolver(uint seed)
        {
            this.seed = seed;
            nextSequenceIndex = 0u;
        }

        public CubeColor ResolveColor(BoardCoordinate coordinate)
        {
            var coordinateHash = unchecked((uint)(coordinate.X * 73856093) ^ (uint)(coordinate.Y * 19349663));
            var colorIndex = unchecked(seed + (coordinateHash >> 2) + nextSequenceIndex * 3u) & 3u;
            nextSequenceIndex++;
            return colorIndex switch
            {
                0u => CubeColor.Red,
                1u => CubeColor.Green,
                2u => CubeColor.Blue,
                _ => CubeColor.Yellow
            };
        }
    }
}
