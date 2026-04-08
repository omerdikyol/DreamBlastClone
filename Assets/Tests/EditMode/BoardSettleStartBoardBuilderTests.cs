using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardSettleStartBoardBuilderTests
    {
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly NormalCubeTapPreviewBoardBuilder previewBoardBuilder = new NormalCubeTapPreviewBoardBuilder();
        private readonly SpecialItemTapPreviewBoardBuilder specialItemPreviewBoardBuilder = new SpecialItemTapPreviewBoardBuilder();
        private readonly BoardSettleStartBoardBuilder builder = new BoardSettleStartBoardBuilder();

        [Test]
        public void BuildClearsGravitySourcesAndKeepsStationaryItemsVisible()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Green));

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new FixedRefillCubeColorResolver());
            var previewBoard = previewBoardBuilder.Build(preTapBoard, tap.NormalCube);
            var settleStartBoard = builder.Build(previewBoard, tap);

            Assert.That(settleStartBoard.GetCell(new BoardCoordinate(0, 2)).Item, Is.Null);
            Assert.That(settleStartBoard.GetCell(new BoardCoordinate(2, 2)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(settleStartBoard.GetCell(new BoardCoordinate(0, 1)).Item, Is.Null);
            Assert.That(settleStartBoard.GetCell(new BoardCoordinate(1, 2)).Item, Is.Null);
        }

        [Test]
        public void BuildClonesPreviewBoardWhenChaliceBoxDoorHasJustBroken()
        {
            var board = new BoardModel(4, 3);
            var tap = new BoardCoordinate(1, 1);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 1);

            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var preTapBoard = boardModelCloner.Clone(board);
            var dispatchResult = dispatcher.Resolve(board, tap, new FixedRefillCubeColorResolver());
            var previewBoard = specialItemPreviewBoardBuilder.Build(preTapBoard, dispatchResult.SpecialItem);

            var previewChaliceBox = (ChaliceBoxObstacleModel)previewBoard.GetCell(chaliceBox.Anchor).Obstacle;
            Assert.That(previewChaliceBox.RemainingDoorDurability, Is.EqualTo(0));

            Assert.That(() => builder.Build(previewBoard, dispatchResult), Throws.Nothing);

            var settleStartBoard = builder.Build(previewBoard, dispatchResult);
            var settleStartChaliceBox = (ChaliceBoxObstacleModel)settleStartBoard.GetCell(chaliceBox.Anchor).Obstacle;
            Assert.That(settleStartChaliceBox.RemainingDoorDurability, Is.EqualTo(0));
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
