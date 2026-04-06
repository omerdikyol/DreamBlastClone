using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class CubeGroupDetectorTests
    {
        private readonly CubeGroupDetector detector = new CubeGroupDetector();

        [Test]
        public void IsolatedCubeReturnsSingleCoordinateGroup()
        {
            var board = new BoardModel(3, 3);
            var coordinate = new BoardCoordinate(1, 1);

            board.PlaceItem(coordinate, new CubeItemModel(CubeColor.Red));

            var result = detector.FindGroup(board, coordinate);

            Assert.That(result.IsValidStart, Is.True);
            Assert.That(result.Color, Is.EqualTo(CubeColor.Red));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Coordinates, Is.EqualTo(new[] { coordinate }));
        }

        [Test]
        public void ConnectedOrthogonalSameColorCubesFormOneGroup()
        {
            var board = new BoardModel(4, 4);
            var start = new BoardCoordinate(1, 1);

            board.PlaceItem(start, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));

            var result = detector.FindGroup(board, start);

            Assert.That(result.IsValidStart, Is.True);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Coordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(1, 1),
                new BoardCoordinate(1, 2),
                new BoardCoordinate(2, 2)
            }));
            Assert.That(result.Coordinates, Has.No.Member(new BoardCoordinate(2, 1)));
            Assert.That(result.Coordinates, Has.No.Member(new BoardCoordinate(0, 0)));
        }

        [Test]
        public void NonCubeStartReturnsInvalidResult()
        {
            var board = new BoardModel(3, 1);

            board.PlaceItem(new BoardCoordinate(1, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 0), new TntItemModel());

            var emptyResult = detector.FindGroup(board, new BoardCoordinate(0, 0));
            var rocketResult = detector.FindGroup(board, new BoardCoordinate(1, 0));
            var tntResult = detector.FindGroup(board, new BoardCoordinate(2, 0));

            Assert.That(emptyResult.IsValidStart, Is.False);
            Assert.That(emptyResult.Count, Is.EqualTo(0));
            Assert.That(rocketResult.IsValidStart, Is.False);
            Assert.That(rocketResult.Count, Is.EqualTo(0));
            Assert.That(tntResult.IsValidStart, Is.False);
            Assert.That(tntResult.Count, Is.EqualTo(0));
        }

        [Test]
        public void ObstacleInSameCellDoesNotAffectCubeAdjacency()
        {
            var board = new BoardModel(3, 1);
            var start = new BoardCoordinate(0, 0);
            var neighbor = new BoardCoordinate(1, 0);

            board.PlaceItem(start, new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(start, new VaseObstacleModel());
            board.PlaceItem(neighbor, new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(neighbor, new StoneObstacleModel());

            var result = detector.FindGroup(board, start);

            Assert.That(result.IsValidStart, Is.True);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Coordinates, Is.EqualTo(new[] { start, neighbor }));
        }

        [Test]
        public void BoardEdgesAndOutOfBoundsAreHandledSafely()
        {
            var board = new BoardModel(2, 2);
            var corner = new BoardCoordinate(0, 0);
            var edgeNeighbor = new BoardCoordinate(0, 1);

            board.PlaceItem(corner, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(edgeNeighbor, new CubeItemModel(CubeColor.Green));

            var edgeResult = detector.FindGroup(board, corner);
            var outOfBoundsResult = detector.FindGroup(board, new BoardCoordinate(-1, 0));

            Assert.That(edgeResult.IsValidStart, Is.True);
            Assert.That(edgeResult.Coordinates, Is.EqualTo(new[] { corner, edgeNeighbor }));
            Assert.That(outOfBoundsResult.IsValidStart, Is.False);
            Assert.That(outOfBoundsResult.Color, Is.Null);
            Assert.That(outOfBoundsResult.Coordinates, Is.Empty);
        }
    }
}
