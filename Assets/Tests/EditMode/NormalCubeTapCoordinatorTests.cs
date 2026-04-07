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
    public sealed class NormalCubeTapCoordinatorTests
    {
        private readonly NormalCubeTapCoordinator coordinator = new NormalCubeTapCoordinator();

        [Test]
        public void NullBoardThrows()
        {
            var refillResolver = new FakeRefillCubeColorResolver();

            Assert.That(() => coordinator.Resolve(null, new BoardCoordinate(0, 0), refillResolver), Throws.ArgumentNullException);
        }

        [Test]
        public void NullRefillResolverThrows()
        {
            var board = new BoardModel(1, 1);

            Assert.That(() => coordinator.Resolve(board, new BoardCoordinate(0, 0), null), Throws.ArgumentNullException);
        }

        [Test]
        public void InvalidTapReturnsEmptySubresultsWithoutMutatingBoard()
        {
            var board = new BoardModel(3, 2);
            var isolatedCube = new CubeItemModel(CubeColor.Red);
            var rocket = new RocketItemModel(RocketOrientation.Horizontal);

            board.PlaceItem(new BoardCoordinate(0, 0), isolatedCube);
            board.PlaceItem(new BoardCoordinate(1, 0), rocket);

            var emptyTap = coordinator.Resolve(board, new BoardCoordinate(2, 1), new FakeRefillCubeColorResolver());
            var isolatedTap = coordinator.Resolve(board, new BoardCoordinate(0, 0), new FakeRefillCubeColorResolver());
            var rocketTap = coordinator.Resolve(board, new BoardCoordinate(1, 0), new FakeRefillCubeColorResolver());

            AssertInvalidResult(emptyTap);
            AssertInvalidResult(isolatedTap);
            AssertInvalidResult(rocketTap);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(isolatedCube));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(rocket));
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Item, Is.Null);
        }

        [Test]
        public void ValidTapRunsBlastThenGravityThenRefill()
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

            var result = coordinator.Resolve(board, new BoardCoordinate(0, 1), refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Blast.IsValidBlast, Is.True);
            Assert.That(result.Blast.BlastedGroupSize, Is.EqualTo(2));
            Assert.That(result.Blast.BlastedCubeColor, Is.EqualTo(CubeColor.Red));
            Assert.That(result.Blast.RemovedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1)
            }));

            Assert.That(result.Gravity.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(2, 2), new BoardCoordinate(2, 1))
            }));

            Assert.That(result.Refill.Spawns, Is.EqualTo(new[]
            {
                new ItemSpawn(new BoardCoordinate(0, 2), CubeColor.Yellow),
                new ItemSpawn(new BoardCoordinate(1, 2), CubeColor.Green),
                new ItemSpawn(new BoardCoordinate(2, 2), CubeColor.Red)
            }));

            AssertCube(board, new BoardCoordinate(0, 0), CubeColor.Blue);
            AssertCube(board, new BoardCoordinate(0, 2), CubeColor.Yellow);
            AssertCube(board, new BoardCoordinate(1, 0), CubeColor.Blue);
            AssertCube(board, new BoardCoordinate(1, 2), CubeColor.Green);
            AssertCube(board, new BoardCoordinate(2, 0), CubeColor.Green);
            AssertCube(board, new BoardCoordinate(2, 1), CubeColor.Blue);
            AssertCube(board, new BoardCoordinate(2, 2), CubeColor.Red);
        }

        [Test]
        public void ObstacleLayerRemainsUntouchedThroughPipeline()
        {
            var board = new BoardModel(2, 2);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 1), CubeColor.Blue },
                { new BoardCoordinate(1, 1), CubeColor.Yellow }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 0), vase);
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(1, 1), stone);

            var result = coordinator.Resolve(board, new BoardCoordinate(0, 0), refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(stone));
            AssertCube(board, new BoardCoordinate(0, 1), CubeColor.Blue);
            AssertCube(board, new BoardCoordinate(1, 1), CubeColor.Yellow);
        }

        private static void AssertInvalidResult(NormalCubeTapPipelineResult result)
        {
            Assert.That(result.IsValidTap, Is.False);
            Assert.That(result.Blast.IsValidBlast, Is.False);
            Assert.That(result.Gravity.HasAnyMovement, Is.False);
            Assert.That(result.Refill.HasAnySpawn, Is.False);
        }

        private static void AssertCube(BoardModel board, BoardCoordinate coordinate, CubeColor expectedColor)
        {
            var cube = board.GetCell(coordinate).Item as CubeItemModel;
            Assert.That(cube, Is.Not.Null);
            Assert.That(cube.Color, Is.EqualTo(expectedColor));
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
