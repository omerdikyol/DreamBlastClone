using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardTapDispatcherTests
    {
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();

        [Test]
        public void NullBoardThrows()
        {
            Assert.That(
                () => dispatcher.Resolve(null, new BoardCoordinate(0, 0), new FakeRefillCubeColorResolver()),
                Throws.ArgumentNullException);
        }

        [Test]
        public void NullRefillResolverThrows()
        {
            var board = new BoardModel(1, 1);

            Assert.That(
                () => dispatcher.Resolve(board, new BoardCoordinate(0, 0), null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void InvalidTapReturnsNoOpWithoutMutatingBoard()
        {
            var board = new BoardModel(3, 2);
            var unsupportedItem = new UnknownItemModel();

            board.PlaceItem(new BoardCoordinate(1, 0), unsupportedItem);

            var emptyTap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new FakeRefillCubeColorResolver());
            var unsupportedTap = dispatcher.Resolve(board, new BoardCoordinate(1, 0), new FakeRefillCubeColorResolver());
            var outOfBoundsTap = dispatcher.Resolve(board, new BoardCoordinate(-1, 0), new FakeRefillCubeColorResolver());

            AssertInvalidResult(emptyTap);
            AssertInvalidResult(unsupportedTap);
            AssertInvalidResult(outOfBoundsTap);
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(unsupportedItem));
        }

        [Test]
        public void CubeTapRoutesToNormalCubePipeline()
        {
            var board = new BoardModel(3, 3);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Yellow },
                { new BoardCoordinate(1, 2), CubeColor.Green },
                { new BoardCoordinate(2, 1), CubeColor.Blue },
                { new BoardCoordinate(2, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Blue));

            var result = dispatcher.Resolve(board, new BoardCoordinate(0, 1), refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.RouteType, Is.EqualTo(TapRouteType.NormalCube));
            Assert.That(result.NormalCube.IsValidTap, Is.True);
            Assert.That(result.NormalCube.Blast.RemovedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1)
            }));
            Assert.That(result.NormalCube.Gravity.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(2, 2), new BoardCoordinate(2, 1))
            }));
            Assert.That(result.NormalCube.Refill.Spawns, Is.EqualTo(new[]
            {
                new ItemSpawn(new BoardCoordinate(0, 2), CubeColor.Yellow),
                new ItemSpawn(new BoardCoordinate(1, 2), CubeColor.Green),
                new ItemSpawn(new BoardCoordinate(2, 2), CubeColor.Red)
            }));
            Assert.That(result.SpecialItem.IsValidActivation, Is.False);
        }

        [Test]
        public void RocketTapRoutesToSpecialItemResolver()
        {
            var board = new BoardModel(4, 2);
            var tap = new BoardCoordinate(1, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(3, 0), new TntItemModel());

            var result = dispatcher.Resolve(board, tap, new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(result.NormalCube.IsValidTap, Is.False);
            Assert.That(result.SpecialItem.IsValidActivation, Is.True);
            Assert.That(result.SpecialItem.ActivationType, Is.EqualTo(SpecialActivationType.Rocket));
            Assert.That(result.SpecialItem.AffectedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(2, 0),
                new BoardCoordinate(3, 0)
            }));
        }

        [Test]
        public void TntTapRoutesToSpecialItemResolverAndLeavesObstaclesUntouched()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);
            var vase = new VaseObstacleModel();

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceObstacle(new BoardCoordinate(0, 0), vase);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));

            var result = dispatcher.Resolve(board, tap, new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(result.NormalCube.IsValidTap, Is.False);
            Assert.That(result.SpecialItem.IsValidActivation, Is.True);
            Assert.That(result.SpecialItem.ActivationType, Is.EqualTo(SpecialActivationType.Tnt));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
        }

        private static void AssertInvalidResult(BoardTapDispatchResult result)
        {
            Assert.That(result.IsValidTap, Is.False);
            Assert.That(result.RouteType, Is.EqualTo(TapRouteType.None));
            Assert.That(result.NormalCube.IsValidTap, Is.False);
            Assert.That(result.SpecialItem.IsValidActivation, Is.False);
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

        private sealed class UnknownItemModel : ItemModel
        {
        }
    }
}
