using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
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
            : this(board, remainingMoves, refillColorResolver, goals: null)
        {
        }

        public LevelSession(
            BoardModel board,
            int remainingMoves,
            IRefillCubeColorResolver refillColorResolver,
            IEnumerable<LevelGoalDefinition> goals)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));

            if (remainingMoves < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingMoves), remainingMoves, "Remaining moves cannot be negative.");
            }

            this.refillColorResolver = refillColorResolver ?? throw new ArgumentNullException(nameof(refillColorResolver));
            RemainingMoves = remainingMoves;
            Goals = CollectGoals(goals);
            CurrentLevelState = levelStateEvaluator.Evaluate(Board, RemainingMoves);
        }

        public BoardModel Board { get; }

        public int RemainingMoves { get; private set; }

        public LevelState CurrentLevelState { get; private set; }

        public IReadOnlyList<LevelGoalDefinition> Goals { get; }

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

        private static IReadOnlyList<LevelGoalDefinition> CollectGoals(IEnumerable<LevelGoalDefinition> goals)
        {
            if (goals is null)
            {
                return Array.Empty<LevelGoalDefinition>();
            }

            var collectedGoals = new List<LevelGoalDefinition>();

            foreach (var goal in goals)
            {
                if (goal is null)
                {
                    throw new ArgumentNullException(nameof(goals), "Goal definitions cannot contain null entries.");
                }

                collectedGoals.Add(goal);
            }

            return collectedGoals.AsReadOnly();
        }
    }
}
