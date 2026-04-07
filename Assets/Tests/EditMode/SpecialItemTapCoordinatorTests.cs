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
    public sealed class SpecialItemTapCoordinatorTests
    {
        private readonly SpecialItemTapCoordinator coordinator = new SpecialItemTapCoordinator();

        [Test]
        public void NullBoardThrows()
        {
            Assert.That(
                () => coordinator.Resolve(null, new BoardCoordinate(0, 0), new FakeRefillCubeColorResolver()),
                Throws.ArgumentNullException);
        }

        [Test]
        public void NullRefillResolverThrows()
        {
            var board = new BoardModel(1, 1);

            Assert.That(
                () => coordinator.Resolve(board, new BoardCoordinate(0, 0), null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void InvalidTapReturnsEmptySubresultsWithoutMutatingBoard()
        {
            var board = new BoardModel(3, 2);
            var cube = new CubeItemModel(CubeColor.Red);

            board.PlaceItem(new BoardCoordinate(1, 0), cube);

            var emptyTap = coordinator.Resolve(board, new BoardCoordinate(0, 0), new FakeRefillCubeColorResolver());
            var cubeTap = coordinator.Resolve(board, new BoardCoordinate(1, 0), new FakeRefillCubeColorResolver());
            var outOfBoundsTap = coordinator.Resolve(board, new BoardCoordinate(-1, 0), new FakeRefillCubeColorResolver());

            AssertInvalidResult(emptyTap);
            AssertInvalidResult(cubeTap);
            AssertInvalidResult(outOfBoundsTap);
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(cube));
        }

        [Test]
        public void ValidRocketTapRunsActivationObstacleDamageGravityAndRefill()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);
            var vaseCoordinate = new BoardCoordinate(3, 1);
            var stoneCoordinate = new BoardCoordinate(0, 0);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Yellow },
                { new BoardCoordinate(1, 2), CubeColor.Green },
                { new BoardCoordinate(2, 1), CubeColor.Blue },
                { new BoardCoordinate(2, 2), CubeColor.Red },
                { new BoardCoordinate(3, 1), CubeColor.Yellow },
                { new BoardCoordinate(3, 2), CubeColor.Green }
            });

            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(3, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(vaseCoordinate, vase);
            board.PlaceObstacle(stoneCoordinate, stone);

            var result = coordinator.Resolve(board, tap, refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Activation.IsValidActivation, Is.True);
            Assert.That(result.Activation.ActivationType, Is.EqualTo(SpecialActivationType.Rocket));
            Assert.That(result.Activation.AffectedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(3, 1)
            }));
            Assert.That(result.Activation.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(3, 1)
            }));

            Assert.That(result.ObstacleDamage.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(result.ObstacleDamage.RemovedCoordinates, Is.Empty);
            Assert.That(vase.RemainingDurability, Is.EqualTo(1));
            Assert.That(board.GetCell(stoneCoordinate).Obstacle, Is.SameAs(stone));

            Assert.That(result.Gravity.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 2), new BoardCoordinate(0, 1)),
                new ItemFallMove(new BoardCoordinate(1, 2), new BoardCoordinate(1, 1)),
                new ItemFallMove(new BoardCoordinate(2, 2), new BoardCoordinate(2, 1)),
                new ItemFallMove(new BoardCoordinate(3, 2), new BoardCoordinate(3, 1))
            }));

            Assert.That(result.Refill.Spawns, Is.EqualTo(new[]
            {
                new ItemSpawn(new BoardCoordinate(0, 2), CubeColor.Yellow),
                new ItemSpawn(new BoardCoordinate(1, 2), CubeColor.Green),
                new ItemSpawn(new BoardCoordinate(2, 2), CubeColor.Red),
                new ItemSpawn(new BoardCoordinate(3, 2), CubeColor.Green)
            }));

            AssertCube(board, new BoardCoordinate(0, 0), CubeColor.Yellow);
            AssertCube(board, new BoardCoordinate(0, 1), CubeColor.Yellow);
            AssertCube(board, new BoardCoordinate(0, 2), CubeColor.Yellow);
            AssertCube(board, new BoardCoordinate(1, 1), CubeColor.Green);
            AssertCube(board, new BoardCoordinate(1, 2), CubeColor.Green);
            AssertCube(board, new BoardCoordinate(2, 1), CubeColor.Red);
            AssertCube(board, new BoardCoordinate(2, 2), CubeColor.Red);
            AssertCube(board, new BoardCoordinate(3, 1), CubeColor.Blue);
            AssertCube(board, new BoardCoordinate(3, 2), CubeColor.Green);
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void ValidTntTapUsesCurrentSpecialObstacleDamageSemanticsThenRefillsBoard()
        {
            var board = new BoardModel(4, 4);
            var tap = new BoardCoordinate(1, 1);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 1, requiredChaliceCount: 10);
            var stoneCoordinate = new BoardCoordinate(0, 0);
            var stone = new StoneObstacleModel();
            var refillResolver = new FakeRefillCubeColorResolver();

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(stoneCoordinate, stone);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var result = coordinator.Resolve(board, tap, refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Activation.IsValidActivation, Is.True);
            Assert.That(result.Activation.ActivationType, Is.EqualTo(SpecialActivationType.Tnt));
            Assert.That(result.ObstacleDamage.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(stoneCoordinate, 1),
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(result.ObstacleDamage.RemovedCoordinates, Is.EqualTo(new[]
            {
                stoneCoordinate
            }));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(0));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
            Assert.That(board.GetCell(chaliceBox.Anchor).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(result.Gravity.HasAnyMovement, Is.False);
            Assert.That(result.Refill.SpawnCount, Is.EqualTo(board.Width * board.Height));
            Assert.That(board.GetCell(stoneCoordinate).Obstacle, Is.Null);

            foreach (var coordinate in result.Refill.Spawns)
            {
                Assert.That(board.GetCell(coordinate.Coordinate).Item, Is.TypeOf<CubeItemModel>());
            }
        }

        private static void AssertInvalidResult(SpecialItemTapPipelineResult result)
        {
            Assert.That(result.IsValidTap, Is.False);
            Assert.That(result.Activation.IsValidActivation, Is.False);
            Assert.That(result.ObstacleDamage.HasAnyDamage, Is.False);
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

                return CubeColor.Yellow;
            }
        }
    }
}
