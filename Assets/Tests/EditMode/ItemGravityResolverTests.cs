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
        public void ObstaclesBlockItemsFromSettlingIntoTheirCells()
        {
            var board = new BoardModel(1, 4);
            var obstacleCoordinate = new BoardCoordinate(0, 0);
            var obstacle = new StoneObstacleModel();
            var item = new CubeItemModel(CubeColor.Yellow);

            board.PlaceObstacle(obstacleCoordinate, obstacle);
            board.PlaceItem(new BoardCoordinate(0, 3), item);

            var result = resolver.Resolve(board);

            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 3), new BoardCoordinate(0, 1))
            }));
            Assert.That(board.GetCell(obstacleCoordinate).Item, Is.Null);
            Assert.That(board.GetCell(obstacleCoordinate).Obstacle, Is.SameAs(obstacle));
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.SameAs(item));
            Assert.That(board.GetCell(new BoardCoordinate(0, 3)).Item, Is.Null);
        }

        [Test]
        public void StoneAllowsReachableCavityToFillLaterally()
        {
            var board = new BoardModel(3, 3);
            var blocker = new StoneObstacleModel();
            var baseCube = new CubeItemModel(CubeColor.Red);
            var settlingCube = new CubeItemModel(CubeColor.Blue);

            board.PlaceObstacle(new BoardCoordinate(1, 1), blocker);
            board.PlaceItem(new BoardCoordinate(0, 0), baseCube);
            board.PlaceItem(new BoardCoordinate(0, 1), settlingCube);

            var result = resolver.Resolve(board);

            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 1), new BoardCoordinate(1, 0))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(baseCube));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(settlingCube));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(blocker));
        }

        [Test]
        public void ChaliceBoxAllowsReachableCavitiesUnderItsFootprintToFillLaterally()
        {
            var board = new BoardModel(4, 4);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 2);
            var leftBaseCube = new CubeItemModel(CubeColor.Red);
            var leftSettlingCube = new CubeItemModel(CubeColor.Blue);
            var rightBaseCube = new CubeItemModel(CubeColor.Green);
            var rightSettlingCube = new CubeItemModel(CubeColor.Yellow);

            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);
            board.PlaceItem(new BoardCoordinate(0, 0), leftBaseCube);
            board.PlaceItem(new BoardCoordinate(0, 1), leftSettlingCube);
            board.PlaceItem(new BoardCoordinate(3, 0), rightBaseCube);
            board.PlaceItem(new BoardCoordinate(3, 1), rightSettlingCube);

            var result = resolver.Resolve(board);

            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(0, 1), new BoardCoordinate(1, 0)),
                new ItemFallMove(new BoardCoordinate(3, 1), new BoardCoordinate(2, 0))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.SameAs(leftSettlingCube));
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Item, Is.SameAs(rightSettlingCube));
            Assert.That(board.GetCell(chaliceBox.Anchor).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(board.GetCell(chaliceBox.Anchor.Offset(1, 1)).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void UnreachableCavityUnderRigidBlockerRemainsEmpty()
        {
            var board = new BoardModel(3, 3);
            var blocker = new StoneObstacleModel();
            var topCube = new CubeItemModel(CubeColor.Green);

            board.PlaceObstacle(new BoardCoordinate(1, 1), blocker);
            board.PlaceItem(new BoardCoordinate(1, 2), topCube);

            var result = resolver.Resolve(board);

            Assert.That(result.Moves, Is.EqualTo(new[]
            {
                new ItemFallMove(new BoardCoordinate(1, 2), new BoardCoordinate(0, 0))
            }));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(topCube));
        }

        [Test]
        public void VaseDoesNotTriggerRigidBlockerLateralSettling()
        {
            var board = new BoardModel(3, 3);
            var vase = new VaseObstacleModel();
            var baseCube = new CubeItemModel(CubeColor.Red);
            var settlingCube = new CubeItemModel(CubeColor.Blue);

            board.PlaceObstacle(new BoardCoordinate(1, 1), vase);
            board.PlaceItem(new BoardCoordinate(0, 0), baseCube);
            board.PlaceItem(new BoardCoordinate(0, 1), settlingCube);

            var result = resolver.Resolve(board);

            Assert.That(result.HasAnyMovement, Is.False);
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Item, Is.SameAs(settlingCube));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(vase));
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
