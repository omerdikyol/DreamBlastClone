using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class CubeBlastParticleDescriptorBuilderTests
    {
        private readonly CubeBlastParticleDescriptorBuilder builder = new CubeBlastParticleDescriptorBuilder();

        [Test]
        public void BuildUsesRemovedCoordinatesAndBlastedCubeColor()
        {
            var tap = new NormalCubeTapPipelineResult(
                isValidTap: true,
                blast: new CubeBlastResolutionResult(
                    isValidBlast: true,
                    blastCoordinates: new[]
                    {
                        new BoardCoordinate(0, 0),
                        new BoardCoordinate(1, 0),
                        new BoardCoordinate(1, 1)
                    },
                    removedCoordinates: new[]
                    {
                        new BoardCoordinate(0, 0),
                        new BoardCoordinate(1, 0),
                        new BoardCoordinate(1, 1)
                    },
                    blastedGroupSize: 3,
                    blastedCubeColor: CubeColor.Blue,
                    createdSpecialCoordinate: null,
                    createdSpecialItem: null),
                obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                gravity: ItemGravityResolutionResult.Empty(),
                refill: ItemRefillResolutionResult.Empty());

            var descriptor = builder.Build(tap);

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.BurstGroups, Has.Count.EqualTo(1));
            Assert.That(descriptor.BurstGroups[0].CubeColor, Is.EqualTo(CubeColor.Blue));
            Assert.That(descriptor.BurstGroups[0].BurstCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(1, 1)
            }));
        }

        [Test]
        public void BuildExcludesCreatedSpecialCoordinate()
        {
            var tap = new NormalCubeTapPipelineResult(
                isValidTap: true,
                blast: new CubeBlastResolutionResult(
                    isValidBlast: true,
                    blastCoordinates: new[]
                    {
                        new BoardCoordinate(0, 0),
                        new BoardCoordinate(1, 0),
                        new BoardCoordinate(2, 0),
                        new BoardCoordinate(3, 0)
                    },
                    removedCoordinates: new[]
                    {
                        new BoardCoordinate(0, 0),
                        new BoardCoordinate(1, 0),
                        new BoardCoordinate(2, 0),
                        new BoardCoordinate(3, 0)
                    },
                    blastedGroupSize: 4,
                    blastedCubeColor: CubeColor.Red,
                    createdSpecialCoordinate: new BoardCoordinate(2, 0),
                    createdSpecialItem: new RocketItemModel(RocketOrientation.Horizontal)),
                obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                gravity: ItemGravityResolutionResult.Empty(),
                refill: ItemRefillResolutionResult.Empty());

            var descriptor = builder.Build(tap);

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.BurstGroups, Has.Count.EqualTo(1));
            Assert.That(descriptor.BurstGroups[0].CubeColor, Is.EqualTo(CubeColor.Red));
            Assert.That(descriptor.BurstGroups[0].BurstCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(3, 0)
            }));
        }

        [Test]
        public void BuildFromSpecialTapGroupsRemovedCubesByPreTapColor()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 1), new TntItemModel());

            var tap = new BoardTapDispatchResult(
                isValidTap: true,
                routeType: TapRouteType.SpecialItem,
                normalCube: NormalCubeTapPipelineResult.Invalid(),
                specialItem: new SpecialItemTapPipelineResult(
                    isValidTap: true,
                    combo: SpecialItemComboActivationResult.Invalid(),
                    activation: new SpecialItemActivationResult(
                        isValidActivation: true,
                        activationType: SpecialActivationType.Tnt,
                        affectedCoordinates: new[]
                        {
                            new BoardCoordinate(0, 0),
                            new BoardCoordinate(1, 0),
                            new BoardCoordinate(2, 0),
                            new BoardCoordinate(0, 1),
                            new BoardCoordinate(1, 1)
                        },
                        removedItemCoordinates: new[]
                        {
                            new BoardCoordinate(0, 0),
                            new BoardCoordinate(1, 0),
                            new BoardCoordinate(2, 0),
                            new BoardCoordinate(0, 1),
                            new BoardCoordinate(1, 1)
                        }),
                    obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                    gravity: ItemGravityResolutionResult.Empty(),
                    refill: ItemRefillResolutionResult.Empty()));

            var descriptor = builder.Build(board, tap);

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.BurstGroups, Has.Count.EqualTo(2));
            Assert.That(descriptor.BurstGroups[0].CubeColor, Is.EqualTo(CubeColor.Red));
            Assert.That(descriptor.BurstGroups[0].BurstCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(0, 1)
            }));
            Assert.That(descriptor.BurstGroups[1].CubeColor, Is.EqualTo(CubeColor.Blue));
            Assert.That(descriptor.BurstGroups[1].BurstCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(1, 0)
            }));
        }
    }
}
