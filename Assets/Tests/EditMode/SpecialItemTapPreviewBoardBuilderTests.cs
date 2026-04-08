using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialItemTapPreviewBoardBuilderTests
    {
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly SpecialItemTapCoordinator specialItemTapCoordinator = new SpecialItemTapCoordinator();
        private readonly SpecialItemTapPreviewBoardBuilder previewBoardBuilder = new SpecialItemTapPreviewBoardBuilder();

        [Test]
        public void BuildRemovesActivatedRocketLineWithoutApplyingGravityOrRefill()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);
            var refillResolver = new FixedRefillCubeColorResolver();

            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Yellow));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = specialItemTapCoordinator.Resolve(board, tap, refillResolver);
            var previewBoard = previewBoardBuilder.Build(preTapBoard, tapResult);

            Assert.That(previewBoard.GetCell(new BoardCoordinate(0, 1)).Item, Is.Null);
            Assert.That(previewBoard.GetCell(tap).Item, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(2, 1)).Item, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(3, 1)).Item, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(0, 2)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(previewBoard.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
        }

        [Test]
        public void BuildAppliesObstacleDamageAndRemovalsWithoutRefill()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);
            var refillResolver = new FixedRefillCubeColorResolver();
            var vaseCoordinate = new BoardCoordinate(3, 1);
            var stoneCoordinate = new BoardCoordinate(0, 1);

            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 1));
            board.PlaceObstacle(stoneCoordinate, new StoneObstacleModel());

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = specialItemTapCoordinator.Resolve(board, tap, refillResolver);
            var previewBoard = previewBoardBuilder.Build(preTapBoard, tapResult);

            Assert.That(previewBoard.GetCell(vaseCoordinate).Obstacle, Is.Null);
            Assert.That(previewBoard.GetCell(stoneCoordinate).Obstacle, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(1, 2)).Item, Is.Null);
        }

        [Test]
        public void BuildRemovesComboItemsWithoutApplyingGravityOrRefill()
        {
            var board = new BoardModel(4, 4);
            var tap = new BoardCoordinate(1, 1);
            var refillResolver = new FixedRefillCubeColorResolver();

            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 2), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 3), new CubeItemModel(CubeColor.Green));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = specialItemTapCoordinator.Resolve(board, tap, refillResolver);
            var previewBoard = previewBoardBuilder.Build(preTapBoard, tapResult);

            Assert.That(tapResult.Combo.IsComboActivated, Is.True);
            Assert.That(previewBoard.GetCell(tap).Item, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(1, 2)).Item, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(0, 1)).Item, Is.Null);
            Assert.That(previewBoard.GetCell(new BoardCoordinate(3, 3)).Item, Is.TypeOf<CubeItemModel>());
        }

        private sealed class FixedRefillCubeColorResolver : IRefillCubeColorResolver
        {
            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                return CubeColor.Yellow;
            }
        }
    }
}
