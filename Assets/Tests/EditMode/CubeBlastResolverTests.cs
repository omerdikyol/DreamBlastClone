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
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[] { start, second, third }));
            Assert.That(board.GetCell(start).Item, Is.Null);
            Assert.That(board.GetCell(second).Item, Is.Null);
            Assert.That(board.GetCell(third).Item, Is.Null);
            Assert.That(board.GetCell(differentColor).Item, Is.TypeOf<CubeItemModel>());
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
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[] { first, second }));
            Assert.That(board.GetCell(first).Item, Is.Null);
            Assert.That(board.GetCell(second).Item, Is.Null);
            Assert.That(board.GetCell(first).Obstacle, Is.SameAs(firstObstacle));
            Assert.That(board.GetCell(second).Obstacle, Is.SameAs(secondObstacle));
        }

        private static void AssertInvalidResult(CubeBlastResolutionResult result)
        {
            Assert.That(result.IsValidBlast, Is.False);
            Assert.That(result.BlastedGroupSize, Is.EqualTo(0));
            Assert.That(result.BlastedCubeColor, Is.Null);
            Assert.That(result.RemovedCoordinates, Is.Empty);
        }
    }
}
