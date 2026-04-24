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
    public sealed class ItemRefillResolverTests
    {
        private readonly ItemRefillResolver resolver = new ItemRefillResolver();

        [Test]
        public void NullBoardThrows()
        {
            var colorResolver = new FakeRefillCubeColorResolver();

            Assert.That(() => resolver.Resolve(null, colorResolver), Throws.ArgumentNullException);
        }

        [Test]
        public void NullColorResolverThrows()
        {
            var board = new BoardModel(1, 1);

            Assert.That(() => resolver.Resolve(board, null), Throws.ArgumentNullException);
        }

        [Test]
        public void FullBoardReturnsNoSpawns()
        {
            var board = new BoardModel(2, 2);
            var cube = new CubeItemModel(CubeColor.Red);
            var rocket = new RocketItemModel(RocketOrientation.Horizontal);
            var tnt = new TntItemModel();
            var secondCube = new CubeItemModel(CubeColor.Blue);

            board.PlaceItem(new BoardCoordinate(0, 0), cube);
            board.PlaceItem(new BoardCoordinate(0, 1), rocket);
            board.PlaceItem(new BoardCoordinate(1, 0), tnt);
            board.PlaceItem(new BoardCoordinate(1, 1), secondCube);

            var result = resolver.Resolve(board, new FakeRefillCubeColorResolver());

            Assert.That(result.HasAnySpawn, Is.False);
            Assert.That(result.SpawnCount, Is.EqualTo(0));
            Assert.That(result.Spawns, Is.Empty);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(cube));
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.SameAs(rocket));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(tnt));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Item, Is.SameAs(secondCube));
        }

        [Test]
        public void EmptyBoardFillsEveryCellWithNormalCubesInDeterministicOrder()
        {
            var board = new BoardModel(2, 2);
            var colorResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 0), CubeColor.Red },
                { new BoardCoordinate(0, 1), CubeColor.Green },
                { new BoardCoordinate(1, 0), CubeColor.Blue },
                { new BoardCoordinate(1, 1), CubeColor.Yellow }
            });

            var result = resolver.Resolve(board, colorResolver);

            Assert.That(result.HasAnySpawn, Is.True);
            Assert.That(result.SpawnCount, Is.EqualTo(4));
            Assert.That(result.Spawns, Is.EqualTo(new[]
            {
                new ItemSpawn(new BoardCoordinate(0, 0), CubeColor.Red),
                new ItemSpawn(new BoardCoordinate(0, 1), CubeColor.Green),
                new ItemSpawn(new BoardCoordinate(1, 0), CubeColor.Blue),
                new ItemSpawn(new BoardCoordinate(1, 1), CubeColor.Yellow)
            }));
            AssertSpawnedCube(board, new BoardCoordinate(0, 0), CubeColor.Red);
            AssertSpawnedCube(board, new BoardCoordinate(0, 1), CubeColor.Green);
            AssertSpawnedCube(board, new BoardCoordinate(1, 0), CubeColor.Blue);
            AssertSpawnedCube(board, new BoardCoordinate(1, 1), CubeColor.Yellow);
        }

        [Test]
        public void RefillOnlyTouchesEmptyItemSlots()
        {
            var board = new BoardModel(2, 2);
            var existingCube = new CubeItemModel(CubeColor.Red);
            var existingRocket = new RocketItemModel(RocketOrientation.Vertical);
            var colorResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 1), CubeColor.Blue },
                { new BoardCoordinate(1, 1), CubeColor.Yellow }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), existingCube);
            board.PlaceItem(new BoardCoordinate(1, 0), existingRocket);

            var result = resolver.Resolve(board, colorResolver);

            Assert.That(result.Spawns, Is.EqualTo(new[]
            {
                new ItemSpawn(new BoardCoordinate(0, 1), CubeColor.Blue),
                new ItemSpawn(new BoardCoordinate(1, 1), CubeColor.Yellow)
            }));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(existingCube));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(existingRocket));
            AssertSpawnedCube(board, new BoardCoordinate(0, 1), CubeColor.Blue);
            AssertSpawnedCube(board, new BoardCoordinate(1, 1), CubeColor.Yellow);
        }

        [Test]
        public void EmptyCellsWithObstaclesDoNotReceiveSpawnedCubes()
        {
            var board = new BoardModel(2, 1);
            var stone = new StoneObstacleModel();
            var vase = new VaseObstacleModel();
            var colorResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 0), CubeColor.Green },
                { new BoardCoordinate(1, 0), CubeColor.Blue }
            });

            board.PlaceObstacle(new BoardCoordinate(0, 0), stone);
            board.PlaceObstacle(new BoardCoordinate(1, 0), vase);

            var result = resolver.Resolve(board, colorResolver);

            Assert.That(result.Spawns, Is.Empty);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.SameAs(stone));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void RefillOnlySpawnsBelowReachableEdgeOfGroupedRigidBlockers()
        {
            var board = new BoardModel(3, 3);
            var leftStone = new StoneObstacleModel();
            var rightStone = new StoneObstacleModel();

            board.PlaceObstacle(new BoardCoordinate(1, 2), leftStone);
            board.PlaceObstacle(new BoardCoordinate(2, 2), rightStone);

            var result = resolver.Resolve(board, new FakeRefillCubeColorResolver());

            Assert.That(result.Spawns, Is.EqualTo(new[]
            {
                new ItemSpawn(new BoardCoordinate(0, 0), CubeColor.Red),
                new ItemSpawn(new BoardCoordinate(0, 1), CubeColor.Red),
                new ItemSpawn(new BoardCoordinate(0, 2), CubeColor.Red),
                new ItemSpawn(new BoardCoordinate(1, 0), CubeColor.Red),
                new ItemSpawn(new BoardCoordinate(1, 1), CubeColor.Red)
            }));
            AssertSpawnedCube(board, new BoardCoordinate(1, 0), CubeColor.Red);
            AssertSpawnedCube(board, new BoardCoordinate(1, 1), CubeColor.Red);
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(1, 2)).Obstacle, Is.SameAs(leftStone));
            Assert.That(board.GetCell(new BoardCoordinate(2, 2)).Obstacle, Is.SameAs(rightStone));
        }

        [Test]
        public void CellBelowIsolatedRigidBlockerCanStillReceiveSpawnedCube()
        {
            var board = new BoardModel(3, 2);
            var stone = new StoneObstacleModel();

            board.PlaceObstacle(new BoardCoordinate(1, 1), stone);

            var result = resolver.Resolve(board, new FakeRefillCubeColorResolver());

            Assert.That(result.Spawns, Does.Contain(new ItemSpawn(new BoardCoordinate(1, 0), CubeColor.Red)));
            AssertSpawnedCube(board, new BoardCoordinate(1, 0), CubeColor.Red);
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(stone));
        }

        [Test]
        public void InvalidResolverColorThrowsClearException()
        {
            var board = new BoardModel(1, 1);
            var colorResolver = new FakeRefillCubeColorResolver(invalidColor: (CubeColor)999);

            var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(board, colorResolver));

            Assert.That(exception.Message, Does.Contain("invalid color"));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
        }

        private static void AssertSpawnedCube(BoardModel board, BoardCoordinate coordinate, CubeColor expectedColor)
        {
            var cube = board.GetCell(coordinate).Item as CubeItemModel;
            Assert.That(cube, Is.Not.Null);
            Assert.That(cube.Color, Is.EqualTo(expectedColor));
        }

        private sealed class FakeRefillCubeColorResolver : IRefillCubeColorResolver
        {
            private readonly IReadOnlyDictionary<BoardCoordinate, CubeColor> colorsByCoordinate;
            private readonly CubeColor? invalidColor;

            public FakeRefillCubeColorResolver(
                IReadOnlyDictionary<BoardCoordinate, CubeColor> colorsByCoordinate = null,
                CubeColor? invalidColor = null)
            {
                this.colorsByCoordinate = colorsByCoordinate;
                this.invalidColor = invalidColor;
            }

            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                if (invalidColor.HasValue)
                {
                    return invalidColor.Value;
                }

                if (colorsByCoordinate is not null && colorsByCoordinate.TryGetValue(coordinate, out var color))
                {
                    return color;
                }

                return CubeColor.Red;
            }
        }
    }
}
