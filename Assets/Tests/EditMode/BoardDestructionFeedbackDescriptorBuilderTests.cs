using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardDestructionFeedbackDescriptorBuilderTests
    {
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly BoardDestructionFeedbackDescriptorBuilder builder = new BoardDestructionFeedbackDescriptorBuilder();
        private readonly TestRefillCubeColorResolver refillColorResolver = new TestRefillCubeColorResolver();

        [Test]
        public void BuildCollectsNormalCubeRemovedCoordinates()
        {
            var board = new BoardModel(3, 1);
            var tap = new BoardCoordinate(1, 0);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Red));

            var descriptor = BuildDescriptor(board, tap);

            Assert.That(descriptor.RemovedItemCoordinates, Is.EquivalentTo(new[]
            {
                new BoardCoordinate(0, 0),
                tap
            }));
        }

        [Test]
        public void BuildExcludesCreatedSpecialCoordinateFromNormalCubeRemovals()
        {
            var board = new BoardModel(4, 1);
            var tap = new BoardCoordinate(1, 0);

            for (var x = 0; x < 4; x++)
            {
                board.PlaceItem(new BoardCoordinate(x, 0), new CubeItemModel(CubeColor.Blue));
            }

            var descriptor = BuildDescriptor(board, tap);

            Assert.That(descriptor.RemovedItemCoordinates, Has.Count.EqualTo(3));
            Assert.That(descriptor.RemovedItemCoordinates, Has.None.EqualTo(tap));
        }

        [Test]
        public void BuildCollectsSingleRocketActivationRemovals()
        {
            var board = new BoardModel(5, 1);
            var tap = new BoardCoordinate(2, 0);
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(4, 0), new CubeItemModel(CubeColor.Green));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tap, refillColorResolver);
            var descriptor = builder.Build(preTapBoard, tapResult);

            Assert.That(descriptor.RemovedItemCoordinates, Is.EquivalentTo(tapResult.SpecialItem.Activation.RemovedItemCoordinates));
        }

        [Test]
        public void BuildAssignsRocketRemovalHitStepsFromActivationOrigin()
        {
            var board = new BoardModel(5, 1);
            var tap = new BoardCoordinate(2, 0);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(3, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(4, 0), new CubeItemModel(CubeColor.Yellow));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tap, refillColorResolver);
            var descriptor = builder.Build(preTapBoard, tapResult, tap);

            Assert.That(descriptor.RemovedItems, Has.Count.EqualTo(5));
            Assert.That(descriptor.RemovedItems[0].Coordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(descriptor.RemovedItems[0].HitStep, Is.EqualTo(2));
            Assert.That(descriptor.RemovedItems[1].Coordinate, Is.EqualTo(new BoardCoordinate(1, 0)));
            Assert.That(descriptor.RemovedItems[1].HitStep, Is.EqualTo(1));
            Assert.That(descriptor.RemovedItems[2].Coordinate, Is.EqualTo(tap));
            Assert.That(descriptor.RemovedItems[2].HitStep, Is.EqualTo(0));
            Assert.That(descriptor.RemovedItems[3].Coordinate, Is.EqualTo(new BoardCoordinate(3, 0)));
            Assert.That(descriptor.RemovedItems[3].HitStep, Is.EqualTo(1));
            Assert.That(descriptor.RemovedItems[4].Coordinate, Is.EqualTo(new BoardCoordinate(4, 0)));
            Assert.That(descriptor.RemovedItems[4].HitStep, Is.EqualTo(2));
        }

        [Test]
        public void BuildAssignsTntRemovalHitStepsFromActivationOriginAndMarksOriginToGrow()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 3), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(4, 2), new CubeItemModel(CubeColor.Blue));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tap, refillColorResolver);
            var descriptor = builder.Build(preTapBoard, tapResult, tap);

            Assert.That(descriptor.RemovedItems, Has.Count.EqualTo(3));
            Assert.That(descriptor.RemovedItems[0].Coordinate, Is.EqualTo(tap));
            Assert.That(descriptor.RemovedItems[0].HitStep, Is.EqualTo(1));
            Assert.That(descriptor.RemovedItems[0].GrowsBeforeRemoval, Is.True);
            Assert.That(descriptor.RemovedItems[1].Coordinate, Is.EqualTo(new BoardCoordinate(4, 2)));
            Assert.That(descriptor.RemovedItems[1].HitStep, Is.EqualTo(3));
            Assert.That(descriptor.RemovedItems[1].GrowsBeforeRemoval, Is.False);
            Assert.That(descriptor.RemovedItems[2].Coordinate, Is.EqualTo(new BoardCoordinate(2, 3)));
            Assert.That(descriptor.RemovedItems[2].HitStep, Is.EqualTo(2));
            Assert.That(descriptor.RemovedItems[2].GrowsBeforeRemoval, Is.False);
        }

        [Test]
        public void BuildAssignsTntObstacleRemovalHitStepsFromActivationOrigin()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            var stoneCoordinate = new BoardCoordinate(4, 2);
            board.PlaceItem(tap, new TntItemModel());
            board.PlaceObstacle(stoneCoordinate, new StoneObstacleModel());

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tap, refillColorResolver);
            var descriptor = builder.Build(preTapBoard, tapResult, tap);

            Assert.That(descriptor.RemovedObstacles, Has.Count.EqualTo(1));
            Assert.That(
                descriptor.RemovedObstacles[0].OccupiedCoordinates,
                Is.EquivalentTo(new[] { stoneCoordinate }));
            Assert.That(descriptor.RemovedObstacles[0].HitStep, Is.EqualTo(3));
        }

        [Test]
        public void BuildCollectsComboRemovedItemCoordinates()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 2), new TntItemModel());
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Green));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tap, refillColorResolver);
            var descriptor = builder.Build(preTapBoard, tapResult);

            Assert.That(tapResult.SpecialItem.Combo.IsComboActivated, Is.True);
            Assert.That(descriptor.RemovedItemCoordinates, Is.EquivalentTo(tapResult.SpecialItem.Combo.RemovedItemCoordinates));
        }

        [Test]
        public void BuildCollectsTriggeredActivationRemovedItemCoordinates()
        {
            var board = new BoardModel(7, 5);
            var tap = new BoardCoordinate(2, 2);
            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(4, 2), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(6, 2), new CubeItemModel(CubeColor.Blue));

            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tap, refillColorResolver);
            var descriptor = builder.Build(preTapBoard, tapResult);

            Assert.That(tapResult.SpecialItem.TriggeredActivations, Has.Count.EqualTo(1));
            Assert.That(descriptor.RemovedItemCoordinates, Is.EquivalentTo(new[]
            {
                tap,
                new BoardCoordinate(4, 2),
                new BoardCoordinate(6, 2)
            }));
        }

        [Test]
        public void BuildDedupesRemovedObstacleInstancesIncludingChaliceBox()
        {
            var board = new BoardModel(4, 4);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 1);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var descriptor = builder.Build(
                board,
                new BoardTapDispatchResult(
                    isValidTap: true,
                    routeType: TapRouteType.NormalCube,
                    normalCube: new NormalCubeTapPipelineResult(
                        isValidTap: true,
                        blast: new CubeBlastResolutionResult(
                            isValidBlast: true,
                            blastCoordinates: System.Array.Empty<BoardCoordinate>(),
                            removedCoordinates: System.Array.Empty<BoardCoordinate>(),
                            blastedGroupSize: 0,
                            blastedCubeColor: null,
                            createdSpecialCoordinate: null,
                            createdSpecialItem: null),
                        obstacleDamage: new ObstacleDamageResolutionResult(
                            damages: System.Array.Empty<ObstacleDamage>(),
                            removedCoordinates: chaliceBox.OccupiedCoordinates),
                        gravity: ItemGravityResolutionResult.Empty(),
                        refill: ItemRefillResolutionResult.Empty()),
                    specialItem: SpecialItemTapPipelineResult.Invalid()));

            Assert.That(descriptor.RemovedObstacles, Has.Count.EqualTo(1));
            Assert.That(descriptor.RemovedObstacles[0].OccupiedCoordinates, Is.EquivalentTo(chaliceBox.OccupiedCoordinates));
        }

        [Test]
        public void BuildDoesNotIncludeStoneForNormalCubeTapNextToStone()
        {
            var board = new BoardModel(2, 2);
            var tap = new BoardCoordinate(0, 0);
            board.PlaceItem(tap, new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new StoneObstacleModel());

            var descriptor = BuildDescriptor(board, tap);

            Assert.That(descriptor.RemovedObstacles, Is.Empty);
        }

        [Test]
        public void BuildCollectsRemovedStoneForSpecialActivation()
        {
            var board = new BoardModel(3, 1);
            var tap = new BoardCoordinate(1, 0);
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());

            var descriptor = BuildDescriptor(board, tap);

            Assert.That(descriptor.RemovedObstacles, Has.Count.EqualTo(1));
            Assert.That(
                descriptor.RemovedObstacles[0].OccupiedCoordinates,
                Is.EquivalentTo(new[] { new BoardCoordinate(0, 0) }));
        }

        private BoardDestructionFeedbackDescriptor BuildDescriptor(BoardModel board, BoardCoordinate tapCoordinate)
        {
            var preTapBoard = boardModelCloner.Clone(board);
            var tapResult = dispatcher.Resolve(board, tapCoordinate, refillColorResolver);
            return builder.Build(preTapBoard, tapResult);
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
