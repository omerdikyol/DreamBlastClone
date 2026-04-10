using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class ChaliceBoxParticleDescriptorBuilderTests
    {
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly ChaliceBoxParticleDescriptorBuilder builder = new ChaliceBoxParticleDescriptorBuilder();

        [Test]
        public void BuildReturnsDoorDamageEventForNormalBlast()
        {
            var board = new BoardModel(4, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(0, 0), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Anchor, Is.EqualTo(chaliceBox.Anchor));
            Assert.That(descriptor.Events[0].EventType, Is.EqualTo(ChaliceBoxParticleEventType.DoorDamage));
            Assert.That(descriptor.Events[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void BuildReturnsDoorBreakEventWhenDoorReachesZero()
        {
            var board = new BoardModel(4, 3);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 1);
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(1, 1), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Anchor, Is.EqualTo(chaliceBox.Anchor));
            Assert.That(descriptor.Events[0].EventType, Is.EqualTo(ChaliceBoxParticleEventType.DoorBreak));
            Assert.That(descriptor.Events[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void BuildReturnsChaliceDamageEventForChalicePhaseHit()
        {
            var board = new BoardModel(5, 5);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 1), remainingDoorDurability: 0, requiredChaliceCount: 10, collectedChaliceCount: 0);
            board.PlaceItem(new BoardCoordinate(2, 2), new TntItemModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(2, 2), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Anchor, Is.EqualTo(chaliceBox.Anchor));
            Assert.That(descriptor.Events[0].EventType, Is.EqualTo(ChaliceBoxParticleEventType.ChaliceDamage));
            Assert.That(descriptor.Events[0].Amount, Is.EqualTo(4));
        }

        [Test]
        public void BuildReturnsChaliceCompleteEventWhenChaliceBoxIsRemoved()
        {
            var board = new BoardModel(5, 5);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 1), remainingDoorDurability: 0, requiredChaliceCount: 3, collectedChaliceCount: 1);
            board.PlaceItem(new BoardCoordinate(2, 2), new TntItemModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var preTapBoard = boardModelCloner.Clone(board);
            var tap = dispatcher.Resolve(board, new BoardCoordinate(2, 2), new TestRefillCubeColorResolver());
            var descriptor = builder.Build(preTapBoard, tap);

            Assert.That(descriptor.HasAnyParticles, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(1));
            Assert.That(descriptor.Events[0].Anchor, Is.EqualTo(chaliceBox.Anchor));
            Assert.That(descriptor.Events[0].EventType, Is.EqualTo(ChaliceBoxParticleEventType.ChaliceComplete));
            Assert.That(descriptor.Events[0].Amount, Is.EqualTo(2));
        }

        [Test]
        public void BuildIgnoresNonChaliceObstacleDamage()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new VaseObstacleModel(remainingDurability: 1));

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
                return CubeColor.Blue;
            }
        }
    }
}
