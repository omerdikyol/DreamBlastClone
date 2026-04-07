using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialItemTapResolverTests
    {
        private readonly SpecialItemTapResolver resolver = new SpecialItemTapResolver();

        [Test]
        public void InvalidTapReturnsNoOpWithoutMutatingBoard()
        {
            var board = new BoardModel(3, 3);
            var normalCube = new CubeItemModel(CubeColor.Red);

            board.PlaceItem(new BoardCoordinate(1, 1), normalCube);

            var emptyResult = resolver.Resolve(board, new BoardCoordinate(0, 0));
            var cubeResult = resolver.Resolve(board, new BoardCoordinate(1, 1));
            var outOfBoundsResult = resolver.Resolve(board, new BoardCoordinate(-1, 0));

            AssertInvalidResult(emptyResult);
            AssertInvalidResult(cubeResult);
            AssertInvalidResult(outOfBoundsResult);
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Item, Is.SameAs(normalCube));
        }

        [Test]
        public void HorizontalRocketClearsEntireRowAndOnlyRemovesExistingItems()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);

            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(3, 1), new TntItemModel());
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Blue));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidActivation, Is.True);
            Assert.That(result.ActivationType, Is.EqualTo(SpecialActivationType.Rocket));
            Assert.That(result.AffectedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(3, 1)
            }));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(3, 1)
            }));
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(3, 1)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(1, 2)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void VerticalRocketClearsEntireColumnInBoardOrder()
        {
            var board = new BoardModel(3, 4);
            var tap = new BoardCoordinate(1, 2);

            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 1), new TntItemModel());
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(1, 3), new CubeItemModel(CubeColor.Yellow));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.ActivationType, Is.EqualTo(SpecialActivationType.Rocket));
            Assert.That(result.AffectedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(1, 0),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(1, 2),
                new BoardCoordinate(1, 3)
            }));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(result.AffectedCoordinates));
        }

        [Test]
        public void TntClearsCenteredFiveByFiveAreaClippedToBoardBounds()
        {
            var board = new BoardModel(4, 4);
            var tap = new BoardCoordinate(0, 0);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(3, 3), new CubeItemModel(CubeColor.Blue));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidActivation, Is.True);
            Assert.That(result.ActivationType, Is.EqualTo(SpecialActivationType.Tnt));
            Assert.That(result.AffectedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(0, 2),
                new BoardCoordinate(1, 2),
                new BoardCoordinate(2, 2)
            }));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(2, 1)
            }));
            Assert.That(board.GetCell(new BoardCoordinate(3, 3)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void SpecialActivationDoesNotChainIntoAffectedSpecialItems()
        {
            var board = new BoardModel(5, 1);
            var tap = new BoardCoordinate(1, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(3, 0), new TntItemModel());
            board.PlaceItem(new BoardCoordinate(4, 0), new CubeItemModel(CubeColor.Blue));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.ActivationType, Is.EqualTo(SpecialActivationType.Rocket));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(3, 0),
                new BoardCoordinate(4, 0)
            }));
            Assert.That(board.GetCell(new BoardCoordinate(3, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Item, Is.Null);
        }

        [Test]
        public void ObstacleLayerRemainsUntouchedDuringSpecialActivation()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceObstacle(new BoardCoordinate(0, 0), vase);
            board.PlaceObstacle(new BoardCoordinate(2, 2), stone);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Yellow));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsValidActivation, Is.True);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(new BoardCoordinate(2, 2)).Obstacle, Is.SameAs(stone));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 2)).Item, Is.Null);
        }

        private static void AssertInvalidResult(SpecialItemActivationResult result)
        {
            Assert.That(result.IsValidActivation, Is.False);
            Assert.That(result.ActivationType, Is.EqualTo(SpecialActivationType.None));
            Assert.That(result.AffectedCoordinates, Is.Empty);
            Assert.That(result.RemovedItemCoordinates, Is.Empty);
        }
    }
}
