using System;
using System.Collections.Generic;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class LevelStateEvaluator
    {
        public LevelState Evaluate(BoardModel board, int remainingMoves)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (remainingMoves < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingMoves), remainingMoves, "Remaining moves cannot be negative.");
            }

            var remainingObstacles = new HashSet<ObstacleModel>();

            foreach (var cell in board.GetAllCells())
            {
                // Multi-cell obstacles share one instance across their footprint and still count as one logical goal.
                if (cell.HasObstacle)
                {
                    remainingObstacles.Add(cell.Obstacle);
                }
            }

            if (remainingObstacles.Count == 0)
            {
                return LevelState.Win;
            }

            if (remainingMoves == 0)
            {
                return LevelState.Lose;
            }

            return LevelState.Continue;
        }
    }
}
