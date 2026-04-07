using System;
using System.Collections.Generic;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class LevelGoalProgress
    {
        public LevelGoalProgress(LevelGoalType goalType, int initialCount, int remainingCount)
        {
            if (initialCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount), initialCount, "Goal count must be greater than zero.");
            }

            if (remainingCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingCount), remainingCount, "Remaining goal count cannot be negative.");
            }

            GoalType = goalType;
            InitialCount = initialCount;
            RemainingCount = remainingCount;
        }

        public LevelGoalType GoalType { get; }

        public int InitialCount { get; }

        public int RemainingCount { get; }

        public bool IsCompleted => RemainingCount == 0;
    }

    public sealed class LevelGoalProgressEvaluator
    {
        public IReadOnlyList<LevelGoalProgress> Evaluate(BoardModel board, IReadOnlyList<LevelGoalDefinition> goals)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (goals is null)
            {
                throw new ArgumentNullException(nameof(goals));
            }

            var seenObstacles = new HashSet<ObstacleModel>();
            var remainingByType = new Dictionary<LevelGoalType, int>();

            foreach (var cell in board.GetAllCells())
            {
                if (!cell.HasObstacle || !seenObstacles.Add(cell.Obstacle))
                {
                    continue;
                }

                var goalType = ResolveGoalType(cell.Obstacle);

                if (!remainingByType.ContainsKey(goalType))
                {
                    remainingByType.Add(goalType, 0);
                }

                remainingByType[goalType]++;
            }

            var progress = new List<LevelGoalProgress>(goals.Count);

            foreach (var goal in goals)
            {
                if (goal is null)
                {
                    throw new ArgumentNullException(nameof(goals), "Goal definitions cannot contain null entries.");
                }

                remainingByType.TryGetValue(goal.GoalType, out var remainingCount);
                progress.Add(new LevelGoalProgress(goal.GoalType, goal.InitialCount, remainingCount));
            }

            return progress.AsReadOnly();
        }

        private static LevelGoalType ResolveGoalType(ObstacleModel obstacle)
        {
            return obstacle switch
            {
                StoneObstacleModel => LevelGoalType.Stone,
                VaseObstacleModel => LevelGoalType.Vase,
                ChaliceBoxObstacleModel => LevelGoalType.ChaliceBox,
                _ => throw new InvalidOperationException($"Unsupported runtime goal obstacle '{obstacle.GetType().Name}'.")
            };
        }
    }
}
