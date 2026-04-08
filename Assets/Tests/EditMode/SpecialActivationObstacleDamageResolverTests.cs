using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialActivationObstacleDamageResolverTests
    {
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();
        private readonly SpecialActivationObstacleDamageResolver damageResolver = new SpecialActivationObstacleDamageResolver();

        [Test]
        public void InvalidActivationReturnsNoDamageAndLeavesBoardUnchanged()
        {
            var board = new BoardModel(2, 2);
            var vase = new VaseObstacleModel();

            board.PlaceObstacle(new BoardCoordinate(1, 1), vase);

            var result = damageResolver.Resolve(board, SpecialItemActivationResult.Invalid());

            Assert.That(result.HasAnyDamage, Is.False);
            Assert.That(result.Damages, Is.Empty);
            Assert.That(result.RemovedCoordinates, Is.Empty);
            Assert.That(vase.RemainingDurability, Is.EqualTo(2));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void RocketDamagesVaseAndStoneAlongItsLine()
        {
            var board = new BoardModel(5, 3);
            var vaseCoordinate = new BoardCoordinate(0, 1);
            var stoneCoordinate = new BoardCoordinate(3, 1);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();

            board.PlaceItem(new BoardCoordinate(2, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(vaseCoordinate, vase);
            board.PlaceObstacle(stoneCoordinate, stone);
            board.PlaceObstacle(new BoardCoordinate(2, 2), new VaseObstacleModel());

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(2, 1));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1),
                new ObstacleDamage(stoneCoordinate, 1)
            }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                stoneCoordinate
            }));
            Assert.That(vase.RemainingDurability, Is.EqualTo(1));
            Assert.That(board.GetCell(vaseCoordinate).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(stoneCoordinate).Obstacle, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(2, 2)).Obstacle, Is.TypeOf<VaseObstacleModel>());
        }

        [Test]
        public void TntDamagesObstaclesInsideItsAreaOnly()
        {
            var board = new BoardModel(6, 6);
            var insideVaseCoordinate = new BoardCoordinate(1, 1);
            var insideStoneCoordinate = new BoardCoordinate(3, 3);
            var outsideVaseCoordinate = new BoardCoordinate(5, 5);
            var insideVase = new VaseObstacleModel(remainingDurability: 1);
            var insideStone = new StoneObstacleModel(remainingDurability: 2);
            var outsideVase = new VaseObstacleModel();

            board.PlaceItem(new BoardCoordinate(2, 2), new TntItemModel());
            board.PlaceObstacle(insideVaseCoordinate, insideVase);
            board.PlaceObstacle(insideStoneCoordinate, insideStone);
            board.PlaceObstacle(outsideVaseCoordinate, outsideVase);

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(2, 2));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(insideVaseCoordinate, 1),
                new ObstacleDamage(insideStoneCoordinate, 1)
            }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                insideVaseCoordinate
            }));
            Assert.That(board.GetCell(insideVaseCoordinate).Obstacle, Is.Null);
            Assert.That(insideStone.RemainingDurability, Is.EqualTo(1));
            Assert.That(board.GetCell(outsideVaseCoordinate).Obstacle, Is.SameAs(outsideVase));
        }

        [Test]
        public void DoorPhaseChaliceBoxTakesExactlyOneTotalDamagePerSpecialEvent()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 0), remainingDoorDurability: 4);

            board.PlaceItem(new BoardCoordinate(0, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(0, 1));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(result.RemovedCoordinates, Is.Empty);
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(3));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
        }

        [Test]
        public void DoorPhaseEachTouchedChaliceBoxTakesOneDamagePerSpecialEvent()
        {
            var board = new BoardModel(3, 6);
            var bottomChaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 0), remainingDoorDurability: 4);
            var topChaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 2), remainingDoorDurability: 4);

            board.PlaceItem(new BoardCoordinate(1, 5), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceObstacle(bottomChaliceBox.OccupiedCoordinates, bottomChaliceBox);
            board.PlaceObstacle(topChaliceBox.OccupiedCoordinates, topChaliceBox);

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(1, 5));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EquivalentTo(new[]
            {
                new ObstacleDamage(bottomChaliceBox.Anchor, 1),
                new ObstacleDamage(topChaliceBox.Anchor, 1)
            }));
            Assert.That(bottomChaliceBox.RemainingDoorDurability, Is.EqualTo(3));
            Assert.That(topChaliceBox.RemainingDoorDurability, Is.EqualTo(3));
        }

        [Test]
        public void BreakingDoorDoesNotCollectChalicesUntilNextDistinctEvent()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 0), remainingDoorDurability: 1, requiredChaliceCount: 10);

            board.PlaceItem(new BoardCoordinate(0, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var firstActivation = specialItemTapResolver.Resolve(board, new BoardCoordinate(0, 1));
            var firstResult = damageResolver.Resolve(board, firstActivation);

            Assert.That(firstResult.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(0));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
            Assert.That(board.GetCell(chaliceBox.Anchor).Obstacle, Is.SameAs(chaliceBox));

            board.PlaceItem(new BoardCoordinate(0, 1), new RocketItemModel(RocketOrientation.Horizontal));

            var secondActivation = specialItemTapResolver.Resolve(board, new BoardCoordinate(0, 1));
            var secondResult = damageResolver.Resolve(board, secondActivation);

            Assert.That(secondResult.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 2)
            }));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(2));
        }

        [Test]
        public void ChalicePhaseCollectsOnePerAffectedCellAndClearsWhenGoalIsReached()
        {
            var board = new BoardModel(3, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(1, 0),
                remainingDoorDurability: 1,
                requiredChaliceCount: 3,
                collectedChaliceCount: 1);
            chaliceBox.RemainingDoorDurability = 0;

            board.PlaceItem(new BoardCoordinate(0, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(0, 1));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 2)
            }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                chaliceBox.Anchor
            }));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(3));
            foreach (var coordinate in chaliceBox.OccupiedCoordinates)
            {
                Assert.That(board.GetCell(coordinate).Obstacle, Is.Null);
            }
        }

        [Test]
        public void ChalicePhaseCountsUniqueAffectedCellsPerTouchedChaliceBox()
        {
            var board = new BoardModel(3, 6);
            var bottomChaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(1, 0),
                remainingDoorDurability: 0,
                requiredChaliceCount: 10,
                collectedChaliceCount: 1);
            var topChaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(1, 2),
                remainingDoorDurability: 0,
                requiredChaliceCount: 10,
                collectedChaliceCount: 1);

            board.PlaceItem(new BoardCoordinate(1, 5), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceObstacle(bottomChaliceBox.OccupiedCoordinates, bottomChaliceBox);
            board.PlaceObstacle(topChaliceBox.OccupiedCoordinates, topChaliceBox);

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(1, 5));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EquivalentTo(new[]
            {
                new ObstacleDamage(bottomChaliceBox.Anchor, 2),
                new ObstacleDamage(topChaliceBox.Anchor, 2)
            }));
            Assert.That(bottomChaliceBox.CollectedChaliceCount, Is.EqualTo(3));
            Assert.That(topChaliceBox.CollectedChaliceCount, Is.EqualTo(3));
        }

        [Test]
        public void OneSpecialActivationCanDamageVaseStoneAndChaliceBoxTogether()
        {
            var board = new BoardModel(4, 4);
            var vaseCoordinate = new BoardCoordinate(0, 0);
            var stoneCoordinate = new BoardCoordinate(1, 0);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(2, 0),
                remainingDoorDurability: 2,
                requiredChaliceCount: 2);

            board.PlaceItem(new BoardCoordinate(1, 1), new TntItemModel());
            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 1));
            board.PlaceObstacle(stoneCoordinate, new StoneObstacleModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var activation = specialItemTapResolver.Resolve(board, new BoardCoordinate(1, 1));
            var result = damageResolver.Resolve(board, activation);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1),
                new ObstacleDamage(stoneCoordinate, 1),
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                vaseCoordinate,
                stoneCoordinate
            }));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(1));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
        }
    }
}
