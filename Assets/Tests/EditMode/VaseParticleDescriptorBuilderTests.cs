using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class VaseParticleDescriptorBuilderTests
    {
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly VaseParticleDescriptorBuilder builder = new VaseParticleDescriptorBuilder();

        [Test]
        public void BuildReturnsDamageEventForAdjacentVaseBlast()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new VaseObstacleModel(remainingDurability: 2));

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(2, 0)));
            Assert.That(descriptor.Events[0].IsRemoval, Is.False);
        }

        [Test]
        public void BuildReturnsRemovalEventWhenVaseIsCleared()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new VaseObstacleModel(remainingDurability: 1));

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(2, 0)));
            Assert.That(descriptor.Events[0].IsRemoval, Is.True);
        }

        [Test]
        public void BuildReturnsVaseEventForSpecialActivationDamage()
        {
            var board = new BoardModel(4, 4);
            board.PlaceItem(new BoardCoordinate(1, 1), new TntItemModel());
            board.PlaceObstacle(new BoardCoordinate(0, 0), new VaseObstacleModel(remainingDurability: 1));

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(1, 1), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(descriptor.Events[0].IsRemoval, Is.True);
        }

        [Test]
        public void BuildIgnoresNonVaseObstacleDamage()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new StoneObstacleModel());

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.False);
            Assert.That(descriptor.Events, Is.Empty);
        }

        [Test]
        public void BuildReturnsEmptyForInvalidTap()
        {
            var board = new BoardModel(1, 1);
            var descriptor = builder.Build(board, BoardTapDispatchResult.Invalid());

            Assert.That(descriptor.HasAnyParticles, Is.False);
            Assert.That(descriptor.Events, Is.Empty);
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
