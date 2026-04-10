using System;
using System.Collections.Generic;
using DreamBlastClone.Controllers;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelSessionTests
    {
        [Test]
        public void ConstructorThrowsWhenBoardIsNull()
        {
            Assert.That(
                () => new LevelSession(null, remainingMoves: 3, new FakeRefillCubeColorResolver()),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ConstructorThrowsWhenRefillResolverIsNull()
        {
            Assert.That(
                () => new LevelSession(new BoardModel(1, 1), remainingMoves: 3, null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ConstructorThrowsWhenStartingMovesAreNegative()
        {
            Assert.That(
                () => new LevelSession(new BoardModel(1, 1), remainingMoves: -1, new FakeRefillCubeColorResolver()),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SessionStartsInWinWhenBoardHasNoRemainingGoals()
        {
            var session = new LevelSession(new BoardModel(2, 2), remainingMoves: 5, new FakeRefillCubeColorResolver());

            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Win));
            Assert.That(session.RemainingMoves, Is.EqualTo(5));
        }

        [Test]
        public void SessionStartsInLoseWhenGoalsRemainAndMovesAreZero()
        {
            var board = new BoardModel(2, 2);
            board.PlaceObstacle(new BoardCoordinate(1, 1), new StoneObstacleModel());

            var session = new LevelSession(board, remainingMoves: 0, new FakeRefillCubeColorResolver());

            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Lose));
            Assert.That(session.RemainingMoves, Is.EqualTo(0));
        }

        [Test]
        public void InvalidTapReturnsNoOpWithoutChangingMovesOrState()
        {
            var board = new BoardModel(2, 2);
            board.PlaceObstacle(new BoardCoordinate(1, 1), new VaseObstacleModel());
            var session = new LevelSession(board, remainingMoves: 3, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.Tap.IsValidTap, Is.False);
            Assert.That(result.DidSpendMove, Is.False);
            Assert.That(result.RemainingMoves, Is.EqualTo(3));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Continue));
            Assert.That(session.RemainingMoves, Is.EqualTo(3));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Continue));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.Not.Null);
        }

        [Test]
        public void ValidNormalCubeTapSpendsMoveAndUpdatesState()
        {
            var board = new BoardModel(3, 3);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Yellow },
                { new BoardCoordinate(1, 2), CubeColor.Green },
                { new BoardCoordinate(2, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 2), new VaseObstacleModel());

            var session = new LevelSession(board, remainingMoves: 3, refillResolver);

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.Tap.RouteType, Is.EqualTo(TapRouteType.NormalCube));
            Assert.That(result.Tap.NormalCube.IsValidTap, Is.True);
            Assert.That(result.Tap.NormalCube.ObstacleDamage.HasAnyDamage, Is.False);
            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(result.RemainingMoves, Is.EqualTo(2));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Continue));
            Assert.That(session.RemainingMoves, Is.EqualTo(2));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void InvalidIsolatedCubeTapDoesNotSpendMove()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(1, 1), new VaseObstacleModel());

            var session = new LevelSession(board, remainingMoves: 3, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.Tap.RouteType, Is.EqualTo(TapRouteType.None));
            Assert.That(result.Tap.NormalCube.IsValidTap, Is.False);
            Assert.That(result.DidSpendMove, Is.False);
            Assert.That(result.RemainingMoves, Is.EqualTo(3));
            Assert.That(session.RemainingMoves, Is.EqualTo(3));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void ValidSingleSpecialTapSpendsMoveAndExposesSpecialPipeline()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Yellow },
                { new BoardCoordinate(1, 2), CubeColor.Green },
                { new BoardCoordinate(2, 2), CubeColor.Blue },
                { new BoardCoordinate(3, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(3, 1), new VaseObstacleModel());

            var session = new LevelSession(board, remainingMoves: 4, refillResolver);

            var result = session.ProcessTap(tap);

            Assert.That(result.Tap.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(result.Tap.SpecialItem.IsValidTap, Is.True);
            Assert.That(result.Tap.SpecialItem.Combo.IsComboActivated, Is.False);
            Assert.That(result.Tap.SpecialItem.Activation.IsValidActivation, Is.True);
            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(result.RemainingMoves, Is.EqualTo(3));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void ValidSpecialComboTapSpendsMoveAndExposesComboPipeline()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Red },
                { new BoardCoordinate(1, 0), CubeColor.Green },
                { new BoardCoordinate(1, 1), CubeColor.Blue },
                { new BoardCoordinate(1, 2), CubeColor.Yellow },
                { new BoardCoordinate(2, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 2), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Green));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new VaseObstacleModel());

            var session = new LevelSession(board, remainingMoves: 4, refillResolver);

            var result = session.ProcessTap(tap);

            Assert.That(result.Tap.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(result.Tap.SpecialItem.IsValidTap, Is.True);
            Assert.That(result.Tap.SpecialItem.Combo.IsComboActivated, Is.True);
            Assert.That(result.Tap.SpecialItem.Activation.IsValidActivation, Is.False);
            Assert.That(result.Tap.SpecialItem.TriggeredActivations, Is.Empty);
            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(result.RemainingMoves, Is.EqualTo(3));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Continue));
        }

        [Test]
        public void TntDamagingAdjacentRocketsUsesTriggeredSingleActivationsWithoutComboRouting()
        {
            var board = new BoardModel(7, 7);
            var tap = new BoardCoordinate(2, 2);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(4, 2), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(4, 3), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(6, 3), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(4, 6), new CubeItemModel(CubeColor.Green));

            var session = new LevelSession(board, remainingMoves: 4, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(tap);

            Assert.That(result.Tap.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(result.Tap.SpecialItem.IsValidTap, Is.True);
            Assert.That(result.Tap.SpecialItem.Combo.IsComboActivated, Is.False);
            Assert.That(result.Tap.SpecialItem.Activation.IsValidActivation, Is.True);
            Assert.That(result.Tap.SpecialItem.TriggeredActivations, Has.Count.EqualTo(2));
            Assert.That(result.Tap.SpecialItem.TriggeredActivations[0].OriginCoordinate, Is.EqualTo(new BoardCoordinate(4, 2)));
            Assert.That(result.Tap.SpecialItem.TriggeredActivations[1].OriginCoordinate, Is.EqualTo(new BoardCoordinate(4, 3)));
            Assert.That(result.Tap.SpecialItem.TriggeredActivations[1].Activation.RemovedItemCoordinates, Does.Contain(new BoardCoordinate(6, 3)));
            Assert.That(result.Tap.SpecialItem.TriggeredActivations[0].Activation.AffectedCoordinates, Has.None.EqualTo(new BoardCoordinate(4, 6)));
            Assert.That(result.Tap.SpecialItem.TriggeredActivations[1].Activation.AffectedCoordinates, Has.None.EqualTo(new BoardCoordinate(4, 6)));
            Assert.That(result.RemainingMoves, Is.EqualTo(3));
            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(board.GetCell(new BoardCoordinate(4, 6)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void FinalGoalClearedOnTapTransitionsSessionToWin()
        {
            var board = new BoardModel(3, 1);
            var vaseCoordinate = new BoardCoordinate(2, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 1));

            var session = new LevelSession(board, remainingMoves: 2, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(result.RemainingMoves, Is.EqualTo(1));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Win));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Win));
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.Null);
        }

        [Test]
        public void LastValidMoveWithoutClearingGoalsTransitionsSessionToLose()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 1), new StoneObstacleModel());

            var session = new LevelSession(board, remainingMoves: 1, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(result.RemainingMoves, Is.EqualTo(0));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Lose));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Lose));
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Obstacle, Is.Not.Null);
        }

        [Test]
        public void ClearingFinalGoalOnLastMoveStillReturnsWin()
        {
            var board = new BoardModel(3, 1);
            var vaseCoordinate = new BoardCoordinate(2, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 1));

            var session = new LevelSession(board, remainingMoves: 1, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.DidSpendMove, Is.True);
            Assert.That(result.RemainingMoves, Is.EqualTo(0));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Win));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Win));
        }

        [Test]
        public void TerminalSessionIgnoresFurtherTapsWithoutChangingState()
        {
            var board = new BoardModel(2, 2);
            var session = new LevelSession(board, remainingMoves: 0, new FakeRefillCubeColorResolver());

            var result = session.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(result.Tap.IsValidTap, Is.False);
            Assert.That(result.DidSpendMove, Is.False);
            Assert.That(result.RemainingMoves, Is.EqualTo(0));
            Assert.That(result.LevelState, Is.EqualTo(LevelState.Win));
            Assert.That(session.RemainingMoves, Is.EqualTo(0));
            Assert.That(session.CurrentLevelState, Is.EqualTo(LevelState.Win));
        }

        private sealed class FakeRefillCubeColorResolver : IRefillCubeColorResolver
        {
            private readonly IReadOnlyDictionary<BoardCoordinate, CubeColor> colorsByCoordinate;

            public FakeRefillCubeColorResolver(IReadOnlyDictionary<BoardCoordinate, CubeColor> colorsByCoordinate = null)
            {
                this.colorsByCoordinate = colorsByCoordinate;
            }

            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                if (colorsByCoordinate is not null && colorsByCoordinate.TryGetValue(coordinate, out var color))
                {
                    return color;
                }

                return CubeColor.Red;
            }
        }
    }
}
