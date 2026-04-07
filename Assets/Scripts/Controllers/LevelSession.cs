using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Controllers
{
    public sealed class LevelSession
    {
        private readonly IRefillCubeColorResolver refillColorResolver;
        private readonly BoardTapDispatcher boardTapDispatcher = new BoardTapDispatcher();
        private readonly MoveSpendEvaluator moveSpendEvaluator = new MoveSpendEvaluator();
        private readonly LevelStateEvaluator levelStateEvaluator = new LevelStateEvaluator();

        public LevelSession(BoardModel board, int remainingMoves, IRefillCubeColorResolver refillColorResolver)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));

            if (remainingMoves < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingMoves), remainingMoves, "Remaining moves cannot be negative.");
            }

            this.refillColorResolver = refillColorResolver ?? throw new ArgumentNullException(nameof(refillColorResolver));
            RemainingMoves = remainingMoves;
            CurrentLevelState = levelStateEvaluator.Evaluate(Board, RemainingMoves);
        }

        public BoardModel Board { get; }

        public int RemainingMoves { get; private set; }

        public LevelState CurrentLevelState { get; private set; }

        public LevelSessionTapResult ProcessTap(BoardCoordinate tapCoordinate)
        {
            // Terminal sessions stop mutating immediately and let higher layers react to the settled end state.
            if (CurrentLevelState != LevelState.Continue)
            {
                return CreateTapResult(BoardTapDispatchResult.Invalid(), didSpendMove: false);
            }

            var tap = boardTapDispatcher.Resolve(Board, tapCoordinate, refillColorResolver);
            var didSpendMove = moveSpendEvaluator.ShouldSpendMove(tap);

            if (didSpendMove)
            {
                RemainingMoves--;
            }

            CurrentLevelState = levelStateEvaluator.Evaluate(Board, RemainingMoves);
            return CreateTapResult(tap, didSpendMove);
        }

        private LevelSessionTapResult CreateTapResult(BoardTapDispatchResult tap, bool didSpendMove)
        {
            return new LevelSessionTapResult(
                tap,
                didSpendMove,
                RemainingMoves,
                CurrentLevelState);
        }
    }
}
