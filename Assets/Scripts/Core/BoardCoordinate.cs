using System;
using System.Collections.Generic;

namespace DreamBlastClone.Core
{
    // Coordinates are board-space values only; (0, 0) is the bottom-left logical cell.
    public readonly struct BoardCoordinate : IEquatable<BoardCoordinate>
    {
        public BoardCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public BoardCoordinate Offset(int deltaX, int deltaY)
        {
            return new BoardCoordinate(X + deltaX, Y + deltaY);
        }

        // Orthogonal neighbors follow the same board-space convention as the rest of the domain model.
        public IEnumerable<BoardCoordinate> GetOrthogonalNeighbors()
        {
            yield return Offset(0, 1);
            yield return Offset(1, 0);
            yield return Offset(0, -1);
            yield return Offset(-1, 0);
        }

        public bool Equals(BoardCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(BoardCoordinate left, BoardCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardCoordinate left, BoardCoordinate right)
        {
            return !left.Equals(right);
        }
    }
}
