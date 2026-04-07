using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public readonly struct ItemSpawn
    {
        public ItemSpawn(BoardCoordinate coordinate, CubeColor color)
        {
            Coordinate = coordinate;
            Color = color;
        }

        public BoardCoordinate Coordinate { get; }

        public CubeColor Color { get; }
    }
}
