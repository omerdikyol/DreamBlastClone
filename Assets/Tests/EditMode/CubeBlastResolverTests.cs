using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class CubeBlastResolverTests
    {
        private readonly CubeBlastResolver resolver = new CubeBlastResolver();

        [Test]
        public void InvalidStartsReturnNoOpAndLeaveBoardUnchanged()
        {
            var board = new BoardModel(4, 1);
            var cubeCoordinate = new BoardCoordinate(3, 0);

            board.PlaceItem(new BoardCoordinate(1, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 0), new TntItemModel());
            board.PlaceItem(cubeCoordinate, new CubeItemModel(CubeColor.Red));

            var emptyResult = resolver.Resolve(board, new BoardCoordinate(0, 0));
            var rocketResult = resolver.Resolve(board, new BoardCoordinate(1, 0));
            var tntResult = resolver.Resolve(board, new BoardCoordinate(2, 0));
            var outOfBoundsResult = resolver.Resolve(board, new BoardCoordinate(-1, 0));

            AssertInvalidResult(emptyResult);
            AssertInvalidResult(rocketResult);
            AssertInvalidResult(tntResult);
            AssertInvalidResult(outOfBoundsResult);
            Assert.That(emptyResult.BlastCoordinates, Is.Empty);
            Assert.That(board.GetCell(cubeCoordinate).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.TypeOf<RocketItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Item, Is.TypeOf<TntItemModel>());
        }

        [Test]
        public void IsolatedCubeReturnsNoOpWithoutMutatingBoard()
        {
            var board = new BoardModel(2, 2);
            var coordinate = new BoardCoordinate(1, 1);

            board.PlaceItem(coordinate, new CubeItemModel(CubeColor.Blue));

            var result = resolver.Resolve(board, coordinate);

            AssertInvalidResult(result);
            Assert.That(board.GetCell(coordinate).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void ValidCubeGroupClearsOnlyDetectedItemCoordinates()
        {
            var board = new BoardModel(4, 4);
            var start = new BoardCoordinate(1, 1);
            var second = new BoardCoordinate(1, 2);
            var third = new BoardCoordinate(2, 2);
            var differentColor = new BoardCoordinate(2, 1);
            var diagonal = new BoardCoordinate(0, 0);

            board.PlaceItem(start, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(second, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(third, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(differentColor, new CubeItemModel(CubeColor.Red));
            board.PlaceItem(diagonal, new CubeItemModel(CubeColor.Green));

            var result = resolver.Resolve(board, start);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.BlastedGroupSize, Is.EqualTo(3));
            Assert.That(result.BlastedCubeColor, Is.EqualTo(CubeColor.Green));
            Assert.That(result.BlastCoordinates, Is.EqualTo(new[] { start, second, third }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[] { start, second, third }));
            Assert.That(result.CreatedSpecialCoordinate, Is.Null);
            Assert.That(result.CreatedSpecialItem, Is.Null);
            Assert.That(board.GetCell(start).Item, Is.Null);
            Assert.That(board.GetCell(second).Item, Is.Null);
            Assert.That(board.GetCell(third).Item, Is.Null);
            Assert.That(board.GetCell(differentColor).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(diagonal).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void TwoCubeOrthogonalGroupIsAValidBlast()
        {
            var board = new BoardModel(2, 2);
            var first = new BoardCoordinate(0, 0);
            var second = new BoardCoordinate(1, 0);

            board.PlaceItem(first, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(second, new CubeItemModel(CubeColor.Blue));

            var result = resolver.Resolve(board, first);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.BlastedGroupSize, Is.EqualTo(2));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[] { first, second }));
            Assert.That(board.GetCell(first).Item, Is.Null);
            Assert.That(board.GetCell(second).Item, Is.Null);
        }

        [Test]
        public void LShapedGroupClearsAllOrthogonallyConnectedSameColorCubes()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);
            var up = new BoardCoordinate(1, 2);
            var right = new BoardCoordinate(2, 1);
            var diagonal = new BoardCoordinate(2, 2);

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(up, new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(right, new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(diagonal, new CubeItemModel(CubeColor.Yellow));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.BlastedGroupSize, Is.EqualTo(3));
            Assert.That(result.BlastCoordinates, Is.EqualTo(new[] { tap, up, right }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[] { tap, up, right }));
            Assert.That(board.GetCell(tap).Item, Is.Null);
            Assert.That(board.GetCell(up).Item, Is.Null);
            Assert.That(board.GetCell(right).Item, Is.Null);
            Assert.That(board.GetCell(diagonal).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void BlastClearsOnlyItemLayerWhenObstaclesShareThoseCells()
        {
            var board = new BoardModel(3, 1);
            var first = new BoardCoordinate(0, 0);
            var second = new BoardCoordinate(1, 0);

            board.PlaceItem(first, new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(first, new VaseObstacleModel());
            board.PlaceItem(second, new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(second, new StoneObstacleModel());

            var firstObstacle = board.GetCell(first).Obstacle;
            var secondObstacle = board.GetCell(second).Obstacle;

            var result = resolver.Resolve(board, first);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.BlastCoordinates, Is.EqualTo(new[] { first, second }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[] { first, second }));
            Assert.That(board.GetCell(first).Item, Is.Null);
            Assert.That(board.GetCell(second).Item, Is.Null);
            Assert.That(board.GetCell(first).Obstacle, Is.SameAs(firstObstacle));
            Assert.That(board.GetCell(second).Obstacle, Is.SameAs(secondObstacle));
        }

        [Test]
        public void GroupOfFourCreatesHorizontalRocketAtTappedCoordinate()
        {
            var board = new BoardModel(4, 2);
            var tap = new BoardCoordinate(1, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(3, 0), new CubeItemModel(CubeColor.Red));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.BlastCoordinates, Is.EqualTo(new[]
            {
                tap,
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 0),
                new BoardCoordinate(3, 0)
            }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 0),
                new BoardCoordinate(3, 0)
            }));
            Assert.That(result.CreatedSpecialCoordinate, Is.EqualTo(tap));
            Assert.That(result.CreatedSpecialItem, Is.TypeOf<RocketItemModel>());
            Assert.That(((RocketItemModel)result.CreatedSpecialItem).Orientation, Is.EqualTo(RocketOrientation.Horizontal));
            Assert.That(board.GetCell(tap).Item, Is.TypeOf<RocketItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(3, 0)).Item, Is.Null);
        }

        [Test]
        public void GroupOfFourCreatesVerticalRocketWhenGroupIsTallerThanWide()
        {
            var board = new BoardModel(2, 4);
            var tap = new BoardCoordinate(0, 1);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 3), new CubeItemModel(CubeColor.Blue));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.CreatedSpecialItem, Is.TypeOf<RocketItemModel>());
            Assert.That(((RocketItemModel)result.CreatedSpecialItem).Orientation, Is.EqualTo(RocketOrientation.Vertical));
            Assert.That(board.GetCell(tap).Item, Is.TypeOf<RocketItemModel>());
        }

        [Test]
        public void GroupOfFiveDoesNotCreateSpecialItem()
        {
            var board = new BoardModel(5, 1);
            var tap = new BoardCoordinate(2, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(3, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(4, 0), new CubeItemModel(CubeColor.Green));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.CreatedSpecialCoordinate, Is.Null);
            Assert.That(result.CreatedSpecialItem, Is.Null);
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                tap,
                new BoardCoordinate(3, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(4, 0),
                new BoardCoordinate(0, 0)
            }));
            Assert.That(board.GetCell(tap).Item, Is.Null);
        }

        [Test]
        public void GroupOfSixOrMoreCreatesTntAtTappedCoordinate()
        {
            var board = new BoardModel(3, 2);
            var tap = new BoardCoordinate(1, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Yellow));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidBlast, Is.True);
            Assert.That(result.BlastedGroupSize, Is.EqualTo(6));
            Assert.That(result.CreatedSpecialCoordinate, Is.EqualTo(tap));
            Assert.That(result.CreatedSpecialItem, Is.TypeOf<TntItemModel>());
            Assert.That(result.RemovedCoordinates, Has.Count.EqualTo(5));
            Assert.That(board.GetCell(tap).Item, Is.TypeOf<TntItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Item, Is.Null);
        }

        private static void AssertInvalidResult(CubeBlastResolutionResult result)
        {
            Assert.That(result.IsValidBlast, Is.False);
            Assert.That(result.BlastedGroupSize, Is.EqualTo(0));
            Assert.That(result.BlastedCubeColor, Is.Null);
            Assert.That(result.BlastCoordinates, Is.Empty);
            Assert.That(result.RemovedCoordinates, Is.Empty);
            Assert.That(result.CreatedSpecialCoordinate, Is.Null);
            Assert.That(result.CreatedSpecialItem, Is.Null);
        }
    }
}
