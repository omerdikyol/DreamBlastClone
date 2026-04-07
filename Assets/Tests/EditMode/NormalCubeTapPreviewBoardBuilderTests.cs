using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class NormalCubeTapPreviewBoardBuilderTests
    {
        private readonly NormalCubeTapCoordinator coordinator = new NormalCubeTapCoordinator();
        private readonly NormalCubeTapPreviewBoardBuilder previewBoardBuilder = new NormalCubeTapPreviewBoardBuilder();
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly TestRefillCubeColorResolver refillColorResolver = new TestRefillCubeColorResolver();

        [Test]
        public void SizeTwoGroupPreviewRemovesBothCubes()
        {
            var board = new BoardModel(2, 1);
            var tap = new BoardCoordinate(0, 0);

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var preview = BuildPreview(board, tap);

            Assert.That(preview.GetCell(tap).Item, Is.Null);
            Assert.That(preview.GetCell(new BoardCoordinate(1, 0)).Item, Is.Null);
        }

        [Test]
        public void LShapedThreeCubePreviewRemovesWholeGroupOnly()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);
            var up = new BoardCoordinate(1, 2);
            var right = new BoardCoordinate(2, 1);
            var diagonal = new BoardCoordinate(2, 2);

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(up, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(right, new CubeItemModel(CubeColor.Green));
            board.PlaceItem(diagonal, new CubeItemModel(CubeColor.Green));

            var preview = BuildPreview(board, tap);

            Assert.That(preview.GetCell(tap).Item, Is.Null);
            Assert.That(preview.GetCell(up).Item, Is.Null);
            Assert.That(preview.GetCell(right).Item, Is.Null);
            Assert.That(preview.GetCell(diagonal).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void SizeFourPreviewKeepsRocketAtTappedCoordinate()
        {
            var board = new BoardModel(4, 1);
            var tap = new BoardCoordinate(1, 0);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 0), new CubeItemModel(CubeColor.Blue));

            var preview = BuildPreview(board, tap);

            Assert.That(preview.GetCell(tap).Item, Is.TypeOf<RocketItemModel>());
            Assert.That(preview.GetCell(new BoardCoordinate(0, 0)).Item, Is.Null);
            Assert.That(preview.GetCell(new BoardCoordinate(2, 0)).Item, Is.Null);
            Assert.That(preview.GetCell(new BoardCoordinate(3, 0)).Item, Is.Null);
        }

        [Test]
        public void SizeFivePreviewRemovesAllFiveWithoutSpecial()
        {
            var board = new BoardModel(5, 1);
            var tap = new BoardCoordinate(2, 0);

            for (var x = 0; x < 5; x++)
            {
                board.PlaceItem(new BoardCoordinate(x, 0), new CubeItemModel(CubeColor.Yellow));
            }

            var preview = BuildPreview(board, tap);

            for (var x = 0; x < 5; x++)
            {
                Assert.That(preview.GetCell(new BoardCoordinate(x, 0)).Item, Is.Null);
            }
        }

        [Test]
        public void SizeSixPreviewKeepsTntAtTappedCoordinate()
        {
            var board = new BoardModel(3, 2);
            var tap = new BoardCoordinate(1, 0);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    board.PlaceItem(new BoardCoordinate(x, y), new CubeItemModel(CubeColor.Red));
                }
            }

            var preview = BuildPreview(board, tap);

            Assert.That(preview.GetCell(tap).Item, Is.TypeOf<TntItemModel>());

            foreach (var coordinate in board.GetAllCoordinates())
            {
                if (coordinate == tap)
                {
                    continue;
                }

                Assert.That(preview.GetCell(coordinate).Item, Is.Null);
            }
        }

        [Test]
        public void PreviewRemovesCompletedObstacleWithoutApplyingGravityOrRefill()
        {
            var board = new BoardModel(3, 2);
            var tap = new BoardCoordinate(1, 0);
            var left = new BoardCoordinate(0, 0);
            var up = new BoardCoordinate(1, 1);
            var vaseCoordinate = new BoardCoordinate(0, 1);

            board.PlaceItem(tap, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(left, new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(up, new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(vaseCoordinate, new VaseObstacleModel(remainingDurability: 2));

            var preview = BuildPreview(board, tap);
            var finalBoard = BuildFinalBoard(board, tap);

            Assert.That(preview.GetCell(tap).Item, Is.Null);
            Assert.That(preview.GetCell(left).Item, Is.Null);
            Assert.That(preview.GetCell(up).Item, Is.Null);
            Assert.That(preview.GetCell(vaseCoordinate).Obstacle, Is.Null);
            Assert.That(finalBoard.GetCell(tap).Item, Is.TypeOf<CubeItemModel>());
        }

        private BoardModel BuildPreview(BoardModel board, BoardCoordinate tapCoordinate)
        {
            var preTapBoard = boardModelCloner.Clone(board);
            var result = coordinator.Resolve(board, tapCoordinate, refillColorResolver);
            return previewBoardBuilder.Build(preTapBoard, result);
        }

        private BoardModel BuildFinalBoard(BoardModel board, BoardCoordinate tapCoordinate)
        {
            var finalBoard = boardModelCloner.Clone(board);
            coordinator.Resolve(finalBoard, tapCoordinate, refillColorResolver);
            return finalBoard;
        }

        private sealed class TestRefillCubeColorResolver : IRefillCubeColorResolver
        {
            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                return CubeColor.Yellow;
            }
        }
    }
}
