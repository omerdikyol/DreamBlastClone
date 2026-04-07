using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class NormalBlastObstacleDamageResolverTests
    {
        private readonly CubeBlastResolver cubeBlastResolver = new CubeBlastResolver();
        private readonly NormalBlastObstacleDamageResolver damageResolver = new NormalBlastObstacleDamageResolver();

        [Test]
        public void InvalidBlastReturnsNoDamageAndLeavesBoardUnchanged()
        {
            var board = new BoardModel(2, 2);
            var vase = new VaseObstacleModel();

            board.PlaceObstacle(new BoardCoordinate(1, 1), vase);

            var result = damageResolver.Resolve(board, CubeBlastResolutionResult.Invalid());

            Assert.That(result.HasAnyDamage, Is.False);
            Assert.That(result.Damages, Is.Empty);
            Assert.That(vase.RemainingDurability, Is.EqualTo(2));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void AdjacentVaseTakesOneDamageFromBlast()
        {
            var board = new BoardModel(3, 2);
            var vaseCoordinate = new BoardCoordinate(2, 0);
            var vase = new VaseObstacleModel();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(vaseCoordinate, vase);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(vase.RemainingDurability, Is.EqualTo(1));
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void VaseAdjacentToMultipleBlastedCubesTakesOnlyOneDamage()
        {
            var board = new BoardModel(3, 2);
            var vaseCoordinate = new BoardCoordinate(1, 1);
            var vase = new VaseObstacleModel();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(vaseCoordinate, vase);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(vase.RemainingDurability, Is.EqualTo(1));
        }

        [Test]
        public void MultipleAdjacentVasesEachTakeOneDamage()
        {
            var board = new BoardModel(3, 2);
            var leftVaseCoordinate = new BoardCoordinate(0, 1);
            var rightVaseCoordinate = new BoardCoordinate(2, 1);
            var leftVase = new VaseObstacleModel();
            var rightVase = new VaseObstacleModel();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceObstacle(leftVaseCoordinate, leftVase);
            board.PlaceObstacle(rightVaseCoordinate, rightVase);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(1, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(leftVaseCoordinate, 1),
                new ObstacleDamage(rightVaseCoordinate, 1)
            }));
            Assert.That(leftVase.RemainingDurability, Is.EqualTo(1));
            Assert.That(rightVase.RemainingDurability, Is.EqualTo(1));
        }

        [Test]
        public void DiagonalVaseAndAdjacentStoneAreNotDamaged()
        {
            var board = new BoardModel(3, 2);
            var diagonalVaseCoordinate = new BoardCoordinate(2, 1);
            var stoneCoordinate = new BoardCoordinate(2, 0);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(diagonalVaseCoordinate, vase);
            board.PlaceObstacle(stoneCoordinate, stone);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.HasAnyDamage, Is.False);
            Assert.That(vase.RemainingDurability, Is.EqualTo(2));
            Assert.That(stone.RemainingDurability, Is.EqualTo(1));
            Assert.That(board.GetCell(diagonalVaseCoordinate).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(stoneCoordinate).Obstacle, Is.SameAs(stone));
        }

        [Test]
        public void VaseWithOneDurabilityIsClearedWhenDamaged()
        {
            var board = new BoardModel(3, 1);
            var vaseCoordinate = new BoardCoordinate(2, 0);
            var vase = new VaseObstacleModel(remainingDurability: 1);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(vaseCoordinate, vase);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.Null);
        }

        [Test]
        public void DoorPhaseChaliceBoxTakesOneDamageTotalPerBlastEvent()
        {
            var board = new BoardModel(3, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 0), remainingDoorDurability: 4);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(3));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
            Assert.That(board.GetCell(chaliceBox.Anchor).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(board.GetCell(chaliceBox.Anchor.Offset(1, 1)).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void BreakingDoorDoesNotCollectChalicesInSameBlastEvent()
        {
            var board = new BoardModel(3, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 0), remainingDoorDurability: 1);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(0));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
            Assert.That(board.GetCell(chaliceBox.Anchor).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void ChalicePhaseCollectsOnePerUniqueTouchedCell()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 0), remainingDoorDurability: 1);
            chaliceBox.RemainingDoorDurability = 0;

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 2)
            }));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(2));
            Assert.That(board.GetCell(chaliceBox.Anchor).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void ChalicePhaseCountsTouchedCellOnlyOnceEvenIfMultipleBlastedCubesTouchIt()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 1);
            chaliceBox.RemainingDoorDurability = 0;

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(1));
        }

        [Test]
        public void CompletedChaliceBoxIsClearedFromAllOccupiedCells()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(1, 0),
                remainingDoorDurability: 1,
                requiredChaliceCount: 3,
                collectedChaliceCount: 2);
            chaliceBox.RemainingDoorDurability = 0;

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Green));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(3));
            foreach (var coordinate in chaliceBox.OccupiedCoordinates)
            {
                Assert.That(board.GetCell(coordinate).Obstacle, Is.Null);
            }
        }

        [Test]
        public void BlastCanDamageVaseAndChaliceBoxInSameEvent()
        {
            var board = new BoardModel(4, 2);
            var vaseCoordinate = new BoardCoordinate(0, 1);
            var vase = new VaseObstacleModel();
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 1);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(vaseCoordinate, vase);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(0, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1),
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(vase.RemainingDurability, Is.EqualTo(1));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(0));
        }

        [Test]
        public void RocketCreatingBlastStillDamagesObstacleAdjacentToTappedCell()
        {
            var board = new BoardModel(5, 2);
            var vaseCoordinate = new BoardCoordinate(2, 1);
            var vase = new VaseObstacleModel();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(3, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(vaseCoordinate, vase);

            var blast = cubeBlastResolver.Resolve(board, new BoardCoordinate(2, 0));
            var result = damageResolver.Resolve(board, blast);

            Assert.That(blast.CreatedSpecialItem, Is.TypeOf<RocketItemModel>());
            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1)
            }));
            Assert.That(vase.RemainingDurability, Is.EqualTo(1));
        }
    }
}
