using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class ItemGravityResolverTests
    {
        private readonly ItemGravityResolver resolver = new ItemGravityResolver();

        [Test]
        public void EmptyBoardReturnsNoMoves()
        {
            var board = new BoardModel(3, 3);

            var result = resolver.Resolve(board);

            Assert.That(result.HasAnyMovement, Is.False);
            Assert.That(result.MoveCount, Is.EqualTo(0));
            Assert.That(result.Moves, Is.Empty);
            Assert.That(board.GetAllCells(), Has.All.Matches<CellModel>(cell => cell.Item is null));
        }

        [Test]
        public void AlreadyCompactBoardReturnsNoMoves()
        {
            var board = new BoardModel(1, 3);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(0, 1), new RocketItemModel(RocketOrientation.Horizontal));

            var result = resolver.Resolve(board);

            Assert.That(result.HasAnyMovement, Is.False);
            Assert.That(result.Moves, Is.Empty);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.TypeOf<RocketItemModel>());
        }

        [Test]
        public void SingleColumnGapsCollapseItemsDownward()
        {
            var board = new BoardModel(1, 5);
            var middleCube = new CubeItemModel(CubeColor.Blue);
            var topTnt = new TntItemModel();

            board.PlaceItem(new BoardCoordinate(0, 2), middleCube);
            board.PlaceItem(new BoardCoordinate(0, 4), topTnt);

            var result = resolver.Resolve(board);

            Assert.That(result.HasAnyMovement, Is.True);
            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 2), new BoardCoordinate(0, 0)),
                new ItemFallMove(new BoardCoordinate(0, 4), new BoardCoordinate(0, 1))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(middleCube));
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.SameAs(topTnt));
            Assert.That(board.GetCell(new BoardCoordinate(0, 2)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(0, 4)).Item, Is.Null);
        }

        [Test]
        public void ColumnsCollapseIndependentlyInDeterministicOrder()
        {
            var board = new BoardModel(2, 4);

            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 3), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 2), new TntItemModel());

            var result = resolver.Resolve(board);

            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 1), new BoardCoordinate(0, 0)),
                new ItemFallMove(new BoardCoordinate(0, 3), new BoardCoordinate(0, 1)),
                new ItemFallMove(new BoardCoordinate(1, 2), new BoardCoordinate(1, 1))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.TypeOf<RocketItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Item, Is.TypeOf<TntItemModel>());
        }

        [Test]
        public void ObstaclesRemainUntouchedWhileItemsCanSettleIntoObstacleCells()
        {
            var board = new BoardModel(1, 3);
            var obstacleCoordinate = new BoardCoordinate(0, 0);
            var obstacle = new StoneObstacleModel();
            var item = new CubeItemModel(CubeColor.Yellow);

            board.PlaceObstacle(obstacleCoordinate, obstacle);
            board.PlaceItem(new BoardCoordinate(0, 2), item);

            var result = resolver.Resolve(board);

            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 2), obstacleCoordinate)
            }));
            Assert.That(board.GetCell(obstacleCoordinate).Item, Is.SameAs(item));
            Assert.That(board.GetCell(obstacleCoordinate).Obstacle, Is.SameAs(obstacle));
            Assert.That(board.GetCell(new BoardCoordinate(0, 2)).Item, Is.Null);
        }

        [Test]
        public void SingleRowBoardProducesNoMovement()
        {
            var board = new BoardModel(3, 1);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new TntItemModel());

            var result = resolver.Resolve(board);

            Assert.That(result.HasAnyMovement, Is.False);
            Assert.That(result.Moves, Is.Empty);
        }
    }
}
