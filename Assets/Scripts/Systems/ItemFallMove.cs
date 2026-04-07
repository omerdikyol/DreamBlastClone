using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public readonly struct ItemFallMove
    {
        public ItemFallMove(BoardCoordinate from, BoardCoordinate to)
        {
            From = from;
            To = to;
        }

        public BoardCoordinate From { get; }

        public BoardCoordinate To { get; }
    }
}
