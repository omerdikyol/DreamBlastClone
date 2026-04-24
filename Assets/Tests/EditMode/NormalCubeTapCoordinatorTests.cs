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
            Assert.That(result.ObstacleDamage.HasAnyDamage, Is.False);
            Assert.That(result.ObstacleDamage.HasAnyRemoval, Is.False);

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
        public void ValidBlastAppliesObstacleDamageBeforeGravityAndRefill()
        {
            var board = new BoardModel(3, 2);
            var vaseCoordinate = new BoardCoordinate(2, 0);
            var stone = new StoneObstacleModel();
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 1), CubeColor.Blue },
                { new BoardCoordinate(1, 1), CubeColor.Yellow },
                { new BoardCoordinate(2, 1), CubeColor.Red }
            });

            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 1));
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(1, 1), stone);

            var result = coordinator.Resolve(board, new BoardCoordinate(0, 0), refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.ObstacleDamage.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(result.ObstacleDamage.RemovedCoordinates, Is.EqualTo(new[]
            {
                vaseCoordinate
            }));
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(stone));
            AssertCube(board, new BoardCoordinate(0, 1), CubeColor.Blue);
            AssertCube(board, new BoardCoordinate(1, 1), CubeColor.Yellow);
            AssertCube(board, new BoardCoordinate(2, 1), CubeColor.Red);
        }

        [Test]
        public void SurvivingVaseFallsAfterBlastOpensSpaceBelow()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(0, 0);
            var vase = new VaseObstacleModel(remainingDurability: 2);

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(new BoardCoordinate(2, 1), vase);

            var result = coordinator.Resolve(board, tap, new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.ObstacleDamage.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(new BoardCoordinate(2, 1), 1)
            }));
            Assert.That(result.ObstacleDamage.RemovedCoordinates, Is.Empty);
            Assert.That(result.Gravity.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(2, 2), new BoardCoordinate(2, 1))
            }));
            Assert.That(result.Gravity.ObstacleMoves, Is.EqualTo(new[]
            {
                new ObstacleFallMove(new BoardCoordinate(2, 1), new BoardCoordinate(2, 0))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void RocketCreatingBlastStillDamagesObstacleAdjacentToTappedCoordinate()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 0);
            var vaseCoordinate = new BoardCoordinate(1, 1);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 1));

            var result = coordinator.Resolve(board, tap, new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Blast.CreatedSpecialCoordinate, Is.EqualTo(tap));
            Assert.That(result.Blast.RemovedCoordinates, Has.No.Member(tap));
            Assert.That(result.ObstacleDamage.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(result.ObstacleDamage.RemovedCoordinates, Is.EqualTo(new[]
            {
                vaseCoordinate
            }));
            Assert.That(board.GetCell(tap).Item, Is.TypeOf<RocketItemModel>());
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.Null);
        }

        [Test]
        public void TntCreatingBlastLetsCreatedTntFallBeforeAdjacentSlipClaimsColumn()
        {
            var board = new BoardModel(3, 6);
            var tap = new BoardCoordinate(1, 5);
            var sideCube = new CubeItemModel(CubeColor.Blue);

            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());
            board.PlaceItem(new BoardCoordinate(0, 1), sideCube);
            for (var y = 0; y < 6; y++)
            {
                board.PlaceItem(new BoardCoordinate(1, y), new CubeItemModel(CubeColor.Red));
            }

            var result = coordinator.Resolve(board, tap, new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Blast.CreatedSpecialCoordinate, Is.EqualTo(tap));
            Assert.That(result.Blast.CreatedSpecialItem, Is.TypeOf<TntItemModel>());
            Assert.That(result.Gravity.Moves, Has.Member(new ItemFallMove(tap, new BoardCoordinate(1, 0))));
            Assert.That(result.Gravity.Moves, Has.None.EqualTo(new ItemFallMove(new BoardCoordinate(0, 1), new BoardCoordinate(1, 0))));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.TypeOf<TntItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.SameAs(sideCube));
        }

        [Test]
        public void LShapedThreeCubeTapRemovesWholeGroupAndLeavesDiagonalCube()
        {
            var board = new BoardModel(3, 4);
            var tap = new BoardCoordinate(1, 1);
            var up = new BoardCoordinate(1, 2);
            var right = new BoardCoordinate(2, 1);
            var diagonal = new BoardCoordinate(2, 2);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(1, 3), CubeColor.Red },
                { new BoardCoordinate(2, 2), CubeColor.Blue },
                { new BoardCoordinate(2, 3), CubeColor.Green }
            });

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(up, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(right, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(diagonal, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 3), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 3), new CubeItemModel(CubeColor.Green));

            var result = coordinator.Resolve(board, tap, refillResolver);

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Blast.BlastedGroupSize, Is.EqualTo(3));
            Assert.That(result.Blast.RemovedCoordinates, Is.EqualTo(new[] { tap, up, right }));
            AssertCube(board, diagonal, CubeColor.Blue);
        }

        [Test]
        public void NormalBlastDamagesOnlyAdjacentEligibleObstacles()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);
            var adjacentVaseCoordinate = new BoardCoordinate(2, 1);
            var diagonalVaseCoordinate = new BoardCoordinate(2, 2);
            var stoneCoordinate = new BoardCoordinate(1, 2);

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceObstacle(adjacentVaseCoordinate, new VaseObstacleModel(remainingDurability: 1));
            board.PlaceObstacle(diagonalVaseCoordinate, new VaseObstacleModel(remainingDurability: 1));
            board.PlaceObstacle(stoneCoordinate, new StoneObstacleModel());

            var result = coordinator.Resolve(board, tap, new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.ObstacleDamage.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(adjacentVaseCoordinate, 1)
            }));
            Assert.That(result.ObstacleDamage.RemovedCoordinates, Is.EqualTo(new[]
            {
                adjacentVaseCoordinate
            }));
            Assert.That(board.GetCell(adjacentVaseCoordinate).Obstacle, Is.Null);
            Assert.That(board.GetCell(diagonalVaseCoordinate).Obstacle, Is.Not.Null);
            Assert.That(board.GetCell(stoneCoordinate).Obstacle, Is.Not.Null);
        }

        [Test]
        public void ValidBlastFillsReachableStoneCavityBeforeRefill()
        {
            var board = new BoardModel(3, 3);
            var stoneCoordinate = new BoardCoordinate(1, 1);
            var movingCube = new CubeItemModel(CubeColor.Blue);
            var blockingCube = new CubeItemModel(CubeColor.Green);

            board.PlaceObstacle(stoneCoordinate, new StoneObstacleModel());
            board.PlaceItem(new BoardCoordinate(0, 0), blockingCube);
            board.PlaceItem(new BoardCoordinate(0, 1), movingCube);
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Red));

            var result = coordinator.Resolve(board, new BoardCoordinate(1, 0), new FakeRefillCubeColorResolver());

            Assert.That(result.IsValidTap, Is.True);
            Assert.That(result.Gravity.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 1), new BoardCoordinate(1, 0))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(movingCube));
            Assert.That(board.GetCell(stoneCoordinate).Obstacle, Is.TypeOf<StoneObstacleModel>());
        }

        private static void AssertInvalidResult(NormalCubeTapPipelineResult result)
        {
            Assert.That(result.IsValidTap, Is.False);
            Assert.That(result.Blast.IsValidBlast, Is.False);
            Assert.That(result.ObstacleDamage.HasAnyDamage, Is.False);
            Assert.That(result.ObstacleDamage.HasAnyRemoval, Is.False);
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
