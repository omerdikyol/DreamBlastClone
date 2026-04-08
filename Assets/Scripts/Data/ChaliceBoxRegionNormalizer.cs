using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Data
{
    internal readonly struct ChaliceBoxRegionDefinition
    {
        public ChaliceBoxRegionDefinition(BoardCoordinate anchor, int width, int height)
        {
            if (width < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Chalice box width must be at least 2.");
            }

            if (height < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Chalice box height must be at least 2.");
            }

            Anchor = anchor;
            Width = width;
            Height = height;

            var occupiedCoordinates = new List<BoardCoordinate>(width * height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    occupiedCoordinates.Add(anchor.Offset(x, y));
                }
            }

            OccupiedCoordinates = occupiedCoordinates.AsReadOnly();
        }

        public BoardCoordinate Anchor { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<BoardCoordinate> OccupiedCoordinates { get; }
    }

    internal static class ChaliceBoxRegionNormalizer
    {
        public static IReadOnlyList<ChaliceBoxRegionDefinition> Normalize(
            IReadOnlyDictionary<BoardCoordinate, ChaliceBoxPart> chaliceParts,
            int gridWidth,
            int gridHeight,
            int levelNumber)
        {
            if (chaliceParts is null)
            {
                throw new ArgumentNullException(nameof(chaliceParts));
            }

            if (chaliceParts.Count == 0)
            {
                return Array.Empty<ChaliceBoxRegionDefinition>();
            }

            var remainingCoordinates = new HashSet<BoardCoordinate>(chaliceParts.Keys);
            var regions = new List<ChaliceBoxRegionDefinition>();

            while (remainingCoordinates.Count > 0)
            {
                var start = GetLowestCoordinate(remainingCoordinates);
                var connectedRegion = CollectConnectedRegion(start, remainingCoordinates);
                var normalizedBoxes = ValidateAndSplitRegion(chaliceParts, connectedRegion, gridWidth, gridHeight, levelNumber);
                regions.AddRange(normalizedBoxes);
            }

            return regions.AsReadOnly();
        }

        private static IReadOnlyList<ChaliceBoxRegionDefinition> ValidateAndSplitRegion(
            IReadOnlyDictionary<BoardCoordinate, ChaliceBoxPart> chaliceParts,
            IReadOnlyCollection<BoardCoordinate> connectedRegion,
            int gridWidth,
            int gridHeight,
            int levelNumber)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var coordinate in connectedRegion)
            {
                minX = Math.Min(minX, coordinate.X);
                minY = Math.Min(minY, coordinate.Y);
                maxX = Math.Max(maxX, coordinate.X);
                maxY = Math.Max(maxY, coordinate.Y);
            }

            var anchor = new BoardCoordinate(minX, minY);
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            if (width < 2 || height < 2)
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains a chalice box region smaller than 2x2 anchored at {anchor}.");
            }

            if (width % 2 != 0 || height % 2 != 0)
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains a chalice box region with non-2x2-compatible dimensions {width}x{height} anchored at {anchor}.");
            }

            if (connectedRegion.Count != width * height)
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains an incomplete chalice box region anchored at {anchor}.");
            }

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var coordinate = new BoardCoordinate(x, y);
                    if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                    {
                        throw new InvalidOperationException(
                            $"Level {levelNumber} contains a chalice box region that extends beyond the board bounds.");
                    }

                    if (!chaliceParts.TryGetValue(coordinate, out var actualPart))
                    {
                        throw new InvalidOperationException(
                            $"Level {levelNumber} contains an incomplete chalice box region anchored at {anchor}.");
                    }

                    var expectedPart = ResolveExpectedPart(coordinate, minX, maxX, maxY);
                    if (actualPart != expectedPart)
                    {
                        throw new InvalidOperationException(
                            $"Level {levelNumber} contains a malformed chalice box region anchored at {anchor}: expected '{expectedPart}' at {coordinate}, found '{actualPart}'.");
                    }
                }
            }

            var normalizedBoxes = new List<ChaliceBoxRegionDefinition>((width / 2) * (height / 2));

            // Authored cb* regions can span multiple box footprints, but gameplay still treats each Chalice Box as 2x2.
            for (var y = minY; y <= maxY; y += 2)
            {
                for (var x = minX; x <= maxX; x += 2)
                {
                    normalizedBoxes.Add(new ChaliceBoxRegionDefinition(new BoardCoordinate(x, y), 2, 2));
                }
            }

            return normalizedBoxes.AsReadOnly();
        }

        private static ChaliceBoxPart ResolveExpectedPart(BoardCoordinate coordinate, int minX, int maxX, int maxY)
        {
            if (coordinate.Y == maxY)
            {
                return coordinate.X == maxX ? ChaliceBoxPart.TopRight : ChaliceBoxPart.TopLeft;
            }

            return coordinate.X == maxX ? ChaliceBoxPart.BottomRight : ChaliceBoxPart.BottomLeft;
        }

        private static HashSet<BoardCoordinate> CollectConnectedRegion(
            BoardCoordinate start,
            HashSet<BoardCoordinate> remainingCoordinates)
        {
            var visited = new HashSet<BoardCoordinate>();
            var queue = new Queue<BoardCoordinate>();
            queue.Enqueue(start);
            remainingCoordinates.Remove(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                {
                    continue;
                }

                foreach (var neighbor in current.GetOrthogonalNeighbors())
                {
                    if (!remainingCoordinates.Remove(neighbor))
                    {
                        continue;
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return visited;
        }

        private static BoardCoordinate GetLowestCoordinate(IEnumerable<BoardCoordinate> coordinates)
        {
            var foundAny = false;
            var best = default(BoardCoordinate);

            foreach (var coordinate in coordinates)
            {
                if (!foundAny
                    || coordinate.Y < best.Y
                    || (coordinate.Y == best.Y && coordinate.X < best.X))
                {
                    best = coordinate;
                    foundAny = true;
                }
            }

            return best;
        }
    }
}
