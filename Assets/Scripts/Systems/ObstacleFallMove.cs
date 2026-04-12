using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public readonly struct ObstacleFallMove
    {
        public ObstacleFallMove(BoardCoordinate from, BoardCoordinate to)
        {
            From = from;
            To = to;
        }

        public BoardCoordinate From { get; }

        public BoardCoordinate To { get; }
    }
}
