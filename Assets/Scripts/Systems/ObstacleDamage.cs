using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public readonly struct ObstacleDamage
    {
        public ObstacleDamage(BoardCoordinate coordinate, int amount)
        {
            Coordinate = coordinate;
            Amount = amount;
        }

        public BoardCoordinate Coordinate { get; }

        public int Amount { get; }
    }
}
