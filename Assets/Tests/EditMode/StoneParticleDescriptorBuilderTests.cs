using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class StoneParticleDescriptorBuilderTests
    {
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly StoneParticleDescriptorBuilder builder = new StoneParticleDescriptorBuilder();

        [Test]
        public void BuildReturnsRemovedStoneCoordinateForSpecialActivation()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(1, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(1, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.RemovedCoordinates, Is.EqualTo(new[] { new BoardCoordinate(0, 0) }));
        }

        [Test]
        public void BuildReturnsMultipleRemovedStoneCoordinates()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(1, 1), new TntItemModel());
            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());
            board.PlaceObstacle(new BoardCoordinate(2, 2), new StoneObstacleModel());

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(1, 1), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.RemovedCoordinates, Is.EquivalentTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(2, 2)
            }));
        }

        [Test]
        public void BuildReturnsEmptyForNormalCubeTap()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new StoneObstacleModel());

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.False);
            Assert.That(descriptor.RemovedCoordinates, Is.Empty);
        }

        [Test]
        public void BuildIgnoresNonStoneObstacleRemovals()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new VaseObstacleModel(remainingDurability: 1));

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.False);
            Assert.That(descriptor.RemovedCoordinates, Is.Empty);
        }

        [Test]
        public void BuildReturnsEmptyForInvalidTap()
        {
            var board = new BoardModel(1, 1);
            var descriptor = builder.Build(board, BoardTapDispatchResult.Invalid());

            Assert.That(descriptor.HasAnyParticles, Is.False);
            Assert.That(descriptor.RemovedCoordinates, Is.Empty);
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
