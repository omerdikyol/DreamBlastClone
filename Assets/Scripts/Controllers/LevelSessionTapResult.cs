using System;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Controllers
{
    public sealed class LevelSessionTapResult
    {
        public LevelSessionTapResult(
            BoardTapDispatchResult tap,
            bool didSpendMove,
            int remainingMoves,
            LevelState levelState)
        {
            Tap = tap ?? throw new ArgumentNullException(nameof(tap));
            DidSpendMove = didSpendMove;
            RemainingMoves = remainingMoves;
            LevelState = levelState;
        }

        public BoardTapDispatchResult Tap { get; }

        public bool DidSpendMove { get; }

        public int RemainingMoves { get; }

        public LevelState LevelState { get; }
    }
}
