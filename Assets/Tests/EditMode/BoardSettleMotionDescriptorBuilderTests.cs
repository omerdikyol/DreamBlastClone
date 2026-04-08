using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardSettleMotionDescriptorBuilderTests
    {
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly NormalCubeTapPreviewBoardBuilder normalPreviewBoardBuilder = new NormalCubeTapPreviewBoardBuilder();
        private readonly BoardSettleMotionDescriptorBuilder builder = new BoardSettleMotionDescriptorBuilder();

        [Test]
        public void BuildMapsGravityMovesAndRefillSpawnsFromResolvedTap()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Green));

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new FixedRefillCubeColorResolver());
            var previewBoard = normalPreviewBoardBuilder.Build(preTapBoard, tap.NormalCube);
            var descriptor = builder.Build(previewBoard, tap, board);

            Assert.That(descriptor.HasAnyMotion, Is.True);
            Assert.That(descriptor.GravityMoves.Any(move => move.From == new BoardCoordinate(0, 2) && move.To == new BoardCoordinate(0, 0)), Is.True);
            Assert.That(descriptor.RefillSpawns.Any(spawn => spawn.SpawnFrom == new BoardCoordinate(1, 3) && spawn.To == new BoardCoordinate(1, 0)), Is.True);
            Assert.That(descriptor.RefillSpawns.Any(spawn => spawn.SpawnFrom == new BoardCoordinate(1, 4) && spawn.To == new BoardCoordinate(1, 1)), Is.True);
            Assert.That(descriptor.RefillSpawns.Any(spawn => spawn.SpawnFrom == new BoardCoordinate(1, 5) && spawn.To == new BoardCoordinate(1, 2)), Is.True);
        }

        [Test]
        public void BuildReturnsEmptyForInvalidTap()
        {
            var board = new BoardModel(2, 2);
            var descriptor = builder.Build(board, BoardTapDispatchResult.Invalid(), board);

            Assert.That(descriptor.HasAnyMotion, Is.False);
            Assert.That(descriptor.GravityMoves, Is.Empty);
            Assert.That(descriptor.RefillSpawns, Is.Empty);
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
