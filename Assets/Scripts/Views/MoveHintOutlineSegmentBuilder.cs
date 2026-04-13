using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class MoveHintOutlineSegmentBuilder
    {
        public IReadOnlyList<MoveHintOutlineSegment> Build(IReadOnlyCollection<BoardCoordinate> highlightedCoordinates)
        {
            if (highlightedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(highlightedCoordinates));
            }

            var coordinateSet = new HashSet<BoardCoordinate>(highlightedCoordinates);
            var segments = new List<MoveHintOutlineSegment>();

            foreach (var coordinate in coordinateSet)
            {
                if (!coordinateSet.Contains(coordinate.Offset(0, -1)))
                {
                    segments.Add(new MoveHintOutlineSegment(coordinate.X, coordinate.Y, coordinate.X + 1, coordinate.Y));
                }

                if (!coordinateSet.Contains(coordinate.Offset(1, 0)))
                {
                    segments.Add(new MoveHintOutlineSegment(coordinate.X + 1, coordinate.Y, coordinate.X + 1, coordinate.Y + 1));
                }

                if (!coordinateSet.Contains(coordinate.Offset(0, 1)))
                {
                    segments.Add(new MoveHintOutlineSegment(coordinate.X, coordinate.Y + 1, coordinate.X + 1, coordinate.Y + 1));
                }

                if (!coordinateSet.Contains(coordinate.Offset(-1, 0)))
                {
                    segments.Add(new MoveHintOutlineSegment(coordinate.X, coordinate.Y, coordinate.X, coordinate.Y + 1));
                }
            }

            segments.Sort(static (left, right) =>
            {
                var startYComparison = left.StartY.CompareTo(right.StartY);
                if (startYComparison != 0)
                {
                    return startYComparison;
                }

                var startXComparison = left.StartX.CompareTo(right.StartX);
                if (startXComparison != 0)
                {
                    return startXComparison;
                }

                var endYComparison = left.EndY.CompareTo(right.EndY);
                if (endYComparison != 0)
                {
                    return endYComparison;
                }

                return left.EndX.CompareTo(right.EndX);
            });

            return segments;
        }
    }

    public readonly struct MoveHintOutlineSegment : IEquatable<MoveHintOutlineSegment>
    {
        public MoveHintOutlineSegment(int startX, int startY, int endX, int endY)
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
        }

        public int StartX { get; }

        public int StartY { get; }

        public int EndX { get; }

        public int EndY { get; }

        public bool Equals(MoveHintOutlineSegment other)
        {
            return StartX == other.StartX
                && StartY == other.StartY
                && EndX == other.EndX
                && EndY == other.EndY;
        }

        public override bool Equals(object obj)
        {
            return obj is MoveHintOutlineSegment other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartX, StartY, EndX, EndY);
        }
    }
}
