using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelStateEvaluatorTests
    {
        private readonly LevelStateEvaluator evaluator = new LevelStateEvaluator();

        [Test]
        public void NullBoardThrows()
        {
            Assert.That(
                () => evaluator.Evaluate(null, remainingMoves: 3),
                Throws.ArgumentNullException);
        }

        [Test]
        public void NegativeRemainingMovesThrows()
        {
            Assert.That(
                () => evaluator.Evaluate(new BoardModel(2, 2), remainingMoves: -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EmptyBoardReturnsWin()
        {
            var board = new BoardModel(3, 3);

            Assert.That(evaluator.Evaluate(board, remainingMoves: 5), Is.EqualTo(LevelState.Win));
        }

        [Test]
        public void AllGoalsClearedReturnsWin()
        {
            var board = new BoardModel(4, 4);
            var vase = new VaseObstacleModel();
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 2);

            board.PlaceObstacle(new BoardCoordinate(0, 0), vase);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            board.ClearObstacle(vase);
            board.ClearObstacle(chaliceBox);

            Assert.That(evaluator.Evaluate(board, remainingMoves: 2), Is.EqualTo(LevelState.Win));
        }

        [Test]
        public void GoalsNotClearedAndMovesRemainReturnsContinue()
        {
            var board = new BoardModel(2, 2);

            board.PlaceObstacle(new BoardCoordinate(1, 1), new StoneObstacleModel());

            Assert.That(evaluator.Evaluate(board, remainingMoves: 1), Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void GoalsNotClearedAndMovesExhaustedReturnsLose()
        {
            var board = new BoardModel(2, 2);

            board.PlaceObstacle(new BoardCoordinate(0, 1), new VaseObstacleModel());

            Assert.That(evaluator.Evaluate(board, remainingMoves: 0), Is.EqualTo(LevelState.Lose));
        }

        [Test]
        public void MixedPartiallyDamagedObstaclesStillPreventWin()
        {
            var board = new BoardModel(4, 4);
            var vase = new VaseObstacleModel(remainingDurability: 1);
            var stone = new StoneObstacleModel();
            var doorPhaseChaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(0, 2), remainingDoorDurability: 1);
            var chalicePhaseChaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 1);

            chalicePhaseChaliceBox.RemainingDoorDurability = 0;
            chalicePhaseChaliceBox.CollectedChaliceCount = 4;

            board.PlaceObstacle(new BoardCoordinate(0, 0), vase);
            board.PlaceObstacle(new BoardCoordinate(1, 0), stone);
            board.PlaceObstacle(doorPhaseChaliceBox.OccupiedCoordinates, doorPhaseChaliceBox);
            board.PlaceObstacle(chalicePhaseChaliceBox.OccupiedCoordinates, chalicePhaseChaliceBox);

            Assert.That(evaluator.Evaluate(board, remainingMoves: 3), Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void MultiCellObstacleCountsAsOneLogicalRemainingGoal()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(0, 0), remainingDoorDurability: 2);

            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            Assert.That(board.GetAllCells(), Has.Exactly(4).Matches<CellModel>(cell => ReferenceEquals(cell.Obstacle, chaliceBox)));
            Assert.That(evaluator.Evaluate(board, remainingMoves: 1), Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void ClearedBoardWithZeroMovesStillReturnsWin()
        {
            var board = new BoardModel(2, 2);
            var stone = new StoneObstacleModel();

            board.PlaceObstacle(new BoardCoordinate(1, 1), stone);
            board.ClearObstacle(stone);

            Assert.That(evaluator.Evaluate(board, remainingMoves: 0), Is.EqualTo(LevelState.Win));
        }
    }
}
