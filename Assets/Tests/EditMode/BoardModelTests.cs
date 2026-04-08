using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardModelTests
    {
        [Test]
        public void ConstructorInitializesBoardDimensionsAndEmptyCells()
        {
            var board = new BoardModel(3, 2);

            Assert.That(board.Width, Is.EqualTo(3));
            Assert.That(board.Height, Is.EqualTo(2));
            Assert.That(board.GetAllCells().Count(), Is.EqualTo(6));
            Assert.That(board.GetAllCells().All(cell => cell.IsCompletelyEmpty), Is.True);
        }

        [Test]
        public void BoundsChecksAndCellLookupBehaveAsExpected()
        {
            var board = new BoardModel(2, 2);
            var validCoordinate = new BoardCoordinate(1, 1);
            var invalidCoordinate = new BoardCoordinate(2, 0);

            Assert.That(board.IsWithinBounds(validCoordinate), Is.True);
            Assert.That(board.IsWithinBounds(invalidCoordinate), Is.False);
            Assert.That(board.TryGetCell(validCoordinate, out var cell), Is.True);
            Assert.That(cell.Coordinate, Is.EqualTo(validCoordinate));
            Assert.That(board.TryGetCell(invalidCoordinate, out _), Is.False);
            Assert.That(() => board.GetCell(invalidCoordinate), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void PlaceItemAffectsOnlyTheItemLayer()
        {
            var board = new BoardModel(3, 3);
            var targetCoordinate = new BoardCoordinate(1, 2);
            var cube = new CubeItemModel(CubeColor.Blue);

            board.PlaceItem(targetCoordinate, cube);

            Assert.That(board.GetCell(targetCoordinate).Item, Is.SameAs(cube));
            Assert.That(board.GetCell(targetCoordinate).Obstacle, Is.Null);
            Assert.That(board.GetAllCells().Count(cell => cell.HasItem), Is.EqualTo(1));

            board.ClearItem(targetCoordinate);

            Assert.That(board.GetCell(targetCoordinate).Item, Is.Null);
            Assert.That(board.GetAllCells().All(cell => !cell.HasItem), Is.True);
        }

        [Test]
        public void PlaceObstacleAffectsOnlyTheObstacleLayer()
        {
            var board = new BoardModel(3, 3);
            var targetCoordinate = new BoardCoordinate(1, 1);
            var vase = new VaseObstacleModel();

            board.PlaceObstacle(targetCoordinate, vase);

            Assert.That(board.GetCell(targetCoordinate).Obstacle, Is.SameAs(vase));
            Assert.That(board.GetCell(targetCoordinate).Item, Is.Null);
            Assert.That(board.GetAllCells().Count(cell => cell.HasObstacle), Is.EqualTo(1));
        }

        [Test]
        public void CellCanHoldItemAndObstacleAtTheSameTime()
        {
            var board = new BoardModel(3, 3);
            var coordinate = new BoardCoordinate(1, 1);
            var cube = new CubeItemModel(CubeColor.Yellow);
            var vase = new VaseObstacleModel();

            board.PlaceItem(coordinate, cube);
            board.PlaceObstacle(coordinate, vase);

            Assert.That(board.GetCell(coordinate).Item, Is.SameAs(cube));
            Assert.That(board.GetCell(coordinate).Obstacle, Is.SameAs(vase));
        }

        [Test]
        public void PlacingSecondItemIntoSameCellThrows()
        {
            var board = new BoardModel(2, 2);
            var coordinate = new BoardCoordinate(0, 0);

            board.PlaceItem(coordinate, new CubeItemModel(CubeColor.Red));

            Assert.That(
                () => board.PlaceItem(coordinate, new CubeItemModel(CubeColor.Green)),
                Throws.TypeOf<System.InvalidOperationException>());
        }

        [Test]
        public void PlacingDifferentObstacleIntoSameCellThrows()
        {
            var board = new BoardModel(2, 2);
            var coordinate = new BoardCoordinate(0, 0);

            board.PlaceObstacle(coordinate, new VaseObstacleModel());

            Assert.That(
                () => board.PlaceObstacle(coordinate, new StoneObstacleModel()),
                Throws.TypeOf<System.InvalidOperationException>());
        }

        [Test]
        public void MultiCellObstaclePlacementReferencesTheSameObstacleInstance()
        {
            var board = new BoardModel(4, 4);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 4);

            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            foreach (var coordinate in chaliceBox.OccupiedCoordinates)
            {
                Assert.That(board.GetCell(coordinate).Obstacle, Is.SameAs(chaliceBox));
                Assert.That(board.GetCell(coordinate).Item, Is.Null);
            }
        }

        [Test]
        public void ClearObstacleRemovesAllCellsOccupiedByTheSameInstance()
        {
            var board = new BoardModel(5, 4);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(0, 0), remainingDoorDurability: 4);
            var rocket = new RocketItemModel(RocketOrientation.Horizontal);

            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);
            board.PlaceItem(new BoardCoordinate(3, 3), rocket);

            board.ClearObstacle(chaliceBox);

            foreach (var coordinate in chaliceBox.OccupiedCoordinates)
            {
                Assert.That(board.GetCell(coordinate).Obstacle, Is.Null);
            }

            Assert.That(board.GetCell(new BoardCoordinate(3, 3)).Item, Is.SameAs(rocket));
        }

        [Test]
        public void ChaliceBoxAllowsZeroDoorDurabilityForChalicePhase()
        {
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(2, 3),
                remainingDoorDurability: 0,
                requiredChaliceCount: 10,
                collectedChaliceCount: 4);

            Assert.That(chaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(2, 3)));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(0));
            Assert.That(chaliceBox.FootprintWidth, Is.EqualTo(2));
            Assert.That(chaliceBox.FootprintHeight, Is.EqualTo(2));
            Assert.That(chaliceBox.OccupiedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(2, 3),
                new BoardCoordinate(3, 3),
                new BoardCoordinate(2, 4),
                new BoardCoordinate(3, 4)
            }));
        }

        [Test]
        public void ClearItemDoesNotAffectObstacleLayer()
        {
            var board = new BoardModel(2, 2);
            var coordinate = new BoardCoordinate(1, 0);
            var cube = new CubeItemModel(CubeColor.Blue);
            var stone = new StoneObstacleModel();

            board.PlaceItem(coordinate, cube);
            board.PlaceObstacle(coordinate, stone);

            board.ClearItem(coordinate);

            Assert.That(board.GetCell(coordinate).Item, Is.Null);
            Assert.That(board.GetCell(coordinate).Obstacle, Is.SameAs(stone));
        }
    }
}
