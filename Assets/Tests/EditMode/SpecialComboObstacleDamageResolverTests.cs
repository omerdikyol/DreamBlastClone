using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialComboObstacleDamageResolverTests
    {
        private readonly SpecialItemComboResolver comboResolver = new SpecialItemComboResolver();
        private readonly SpecialComboObstacleDamageResolver damageResolver = new SpecialComboObstacleDamageResolver();

        [Test]
        public void InvalidComboReturnsNoDamageAndLeavesBoardUnchanged()
        {
            var board = new BoardModel(2, 2);
            var vase = new VaseObstacleModel();

            board.PlaceObstacle(new BoardCoordinate(1, 1), vase);

            var result = damageResolver.Resolve(board, SpecialItemComboActivationResult.Invalid());

            Assert.That(result.HasAnyDamage, Is.False);
            Assert.That(result.Damages, Is.Empty);
            Assert.That(result.RemovedCoordinates, Is.Empty);
            Assert.That(vase.RemainingDurability, Is.EqualTo(2));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void RocketRocketDamagesVaseAndStoneAlongPlusFootprintOnly()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            var vaseCoordinate = new BoardCoordinate(2, 4);
            var stoneCoordinate = new BoardCoordinate(0, 2);
            var outsideCoordinate = new BoardCoordinate(4, 4);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();
            var outsideVase = new VaseObstacleModel();

            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 3), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceObstacle(vaseCoordinate, vase);
            board.PlaceObstacle(stoneCoordinate, stone);
            board.PlaceObstacle(outsideCoordinate, outsideVase);

            var combo = comboResolver.Resolve(board, tap);
            var result = damageResolver.Resolve(board, combo);

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
            Assert.That(board.GetCell(outsideCoordinate).Obstacle, Is.SameAs(outsideVase));
        }

        [Test]
        public void TntTntDamagesObstaclesInsideSevenBySevenAreaOnly()
        {
            var board = new BoardModel(8, 8);
            var tap = new BoardCoordinate(3, 3);
            var insideVaseCoordinate = new BoardCoordinate(0, 3);
            var insideStoneCoordinate = new BoardCoordinate(6, 6);
            var outsideVaseCoordinate = new BoardCoordinate(7, 7);
            var insideVase = new VaseObstacleModel(remainingDurability: 1);
            var insideStone = new StoneObstacleModel(remainingDurability: 2);
            var outsideVase = new VaseObstacleModel();

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(3, 4), new TntItemModel());
            board.PlaceObstacle(insideVaseCoordinate, insideVase);
            board.PlaceObstacle(insideStoneCoordinate, insideStone);
            board.PlaceObstacle(outsideVaseCoordinate, outsideVase);

            var combo = comboResolver.Resolve(board, tap);
            var result = damageResolver.Resolve(board, combo);

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
        public void TntRocketDamagesObstaclesInsideThreeRocketLineUnionOnly()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            var vaseCoordinate = new BoardCoordinate(4, 3);
            var stoneCoordinate = new BoardCoordinate(2, 0);
            var outsideStoneCoordinate = new BoardCoordinate(0, 0);
            var vase = new VaseObstacleModel();
            var stone = new StoneObstacleModel();
            var outsideStone = new StoneObstacleModel();

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 3), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(vaseCoordinate, vase);
            board.PlaceObstacle(stoneCoordinate, stone);
            board.PlaceObstacle(outsideStoneCoordinate, outsideStone);

            var combo = comboResolver.Resolve(board, tap);
            var result = damageResolver.Resolve(board, combo);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(vaseCoordinate, 1),
                new ObstacleDamage(stoneCoordinate, 1)
            }));
            Assert.That(result.RemovedCoordinates, Is.EqualTo(new[]
            {
                stoneCoordinate
            }));
            Assert.That(board.GetCell(outsideStoneCoordinate).Obstacle, Is.SameAs(outsideStone));
        }

        [Test]
        public void DoorPhaseChaliceBoxTakesExactlyOneTotalDamagePerComboEvent()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 1), remainingDoorDurability: 4);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 3), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var combo = comboResolver.Resolve(board, tap);
            var result = damageResolver.Resolve(board, combo);

            Assert.That(result.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(result.RemovedCoordinates, Is.Empty);
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(3));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
        }

        [Test]
        public void BreakingDoorDoesNotCollectChalicesUntilNextDistinctComboEvent()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            var partner = new BoardCoordinate(2, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 1), remainingDoorDurability: 1, requiredChaliceCount: 10);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(partner, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var firstCombo = comboResolver.Resolve(board, tap);
            var firstResult = damageResolver.Resolve(board, firstCombo);

            Assert.That(firstResult.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 1)
            }));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(0));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(partner, new RocketItemModel(RocketOrientation.Horizontal));

            var secondCombo = comboResolver.Resolve(board, tap);
            var secondResult = damageResolver.Resolve(board, secondCombo);

            Assert.That(secondResult.Damages, Is.EqualTo(new[]
            {
                new ObstacleDamage(chaliceBox.Anchor, 4)
            }));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(4));
        }

        [Test]
        public void ChalicePhaseCollectsByUniqueAffectedCellsAndClearsWhenGoalIsReached()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(2, 1),
                remainingDoorDurability: 1,
                requiredChaliceCount: 3,
                collectedChaliceCount: 1);
            chaliceBox.RemainingDoorDurability = 0;

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 3), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var combo = comboResolver.Resolve(board, tap);
            var result = damageResolver.Resolve(board, combo);

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
    }
}
