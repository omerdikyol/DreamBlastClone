using DreamBlastClone.Core;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class TapAnticipationDescriptorBuilderTests
    {
        private readonly TapAnticipationDescriptorBuilder builder = new TapAnticipationDescriptorBuilder();

        [Test]
        public void BuildNormalCubeExcludesCreatedSpecialCoordinate()
        {
            var createdSpecialCoordinate = new BoardCoordinate(1, 0);
            var tap = new BoardTapDispatchResult(
                isValidTap: true,
                routeType: TapRouteType.NormalCube,
                normalCube: new NormalCubeTapPipelineResult(
                    isValidTap: true,
                    blast: new CubeBlastResolutionResult(
                        isValidBlast: true,
                        blastCoordinates: new[]
                        {
                            new BoardCoordinate(0, 0),
                            createdSpecialCoordinate,
                            new BoardCoordinate(2, 0)
                        },
                        removedCoordinates: new[]
                        {
                            new BoardCoordinate(0, 0),
                            createdSpecialCoordinate,
                            new BoardCoordinate(2, 0)
                        },
                        blastedGroupSize: 3,
                        blastedCubeColor: CubeColor.Red,
                        createdSpecialCoordinate: createdSpecialCoordinate,
                        createdSpecialItem: new RocketItemModel(RocketOrientation.Horizontal)),
                    obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                    gravity: ItemGravityResolutionResult.Empty(),
                    refill: ItemRefillResolutionResult.Empty()),
                specialItem: SpecialItemTapPipelineResult.Invalid());

            var descriptor = builder.Build(new BoardCoordinate(0, 0), tap);

            Assert.That(descriptor.HasAnyFeedback, Is.True);
            Assert.That(descriptor.Events, Has.Count.EqualTo(2));
            Assert.That(descriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(descriptor.Events[1].Coordinate, Is.EqualTo(new BoardCoordinate(2, 0)));
            Assert.That(descriptor.Events[0].EventType, Is.EqualTo(TapAnticipationEventType.CubeGroup));
        }

        [Test]
        public void BuildSpecialItemReturnsRocketAndTntEvents()
        {
            var rocketTap = CreateSpecialTap(SpecialActivationType.Rocket);
            var tntTap = CreateSpecialTap(SpecialActivationType.Tnt);

            var rocketDescriptor = builder.Build(new BoardCoordinate(2, 1), rocketTap);
            var tntDescriptor = builder.Build(new BoardCoordinate(3, 2), tntTap);

            Assert.That(rocketDescriptor.Events, Has.Count.EqualTo(1));
            Assert.That(rocketDescriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(2, 1)));
            Assert.That(rocketDescriptor.Events[0].EventType, Is.EqualTo(TapAnticipationEventType.Rocket));
            Assert.That(tntDescriptor.Events, Has.Count.EqualTo(1));
            Assert.That(tntDescriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(3, 2)));
            Assert.That(tntDescriptor.Events[0].EventType, Is.EqualTo(TapAnticipationEventType.Tnt));
        }

        [Test]
        public void BuildReturnsInvalidEventForInvalidTapAndEmptyForComboTap()
        {
            var comboTap = new BoardTapDispatchResult(
                isValidTap: true,
                routeType: TapRouteType.SpecialItem,
                normalCube: NormalCubeTapPipelineResult.Invalid(),
                specialItem: new SpecialItemTapPipelineResult(
                    isValidTap: true,
                    combo: new SpecialItemComboActivationResult(
                        isComboActivated: true,
                        comboType: SpecialItemComboType.RocketRocket,
                        participatingSpecialCoordinates: new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 0) },
                        affectedCoordinates: new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 0) },
                        removedItemCoordinates: new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 0) }),
                    activation: new SpecialItemActivationResult(
                        isValidActivation: true,
                        activationType: SpecialActivationType.Rocket,
                        affectedCoordinates: new[] { new BoardCoordinate(0, 0) },
                        removedItemCoordinates: new[] { new BoardCoordinate(0, 0) }),
                    obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                    gravity: ItemGravityResolutionResult.Empty(),
                    refill: ItemRefillResolutionResult.Empty()));

            var invalidDescriptor = builder.Build(new BoardCoordinate(0, 0), BoardTapDispatchResult.Invalid());

            Assert.That(invalidDescriptor.Events, Has.Count.EqualTo(1));
            Assert.That(invalidDescriptor.Events[0].Coordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(invalidDescriptor.Events[0].EventType, Is.EqualTo(TapAnticipationEventType.Invalid));
            Assert.That(builder.Build(new BoardCoordinate(0, 0), comboTap).HasAnyFeedback, Is.False);
        }

        private static BoardTapDispatchResult CreateSpecialTap(SpecialActivationType activationType)
        {
            return new BoardTapDispatchResult(
                isValidTap: true,
                routeType: TapRouteType.SpecialItem,
                normalCube: NormalCubeTapPipelineResult.Invalid(),
                specialItem: new SpecialItemTapPipelineResult(
                    isValidTap: true,
                    combo: SpecialItemComboActivationResult.Invalid(),
                    activation: new SpecialItemActivationResult(
                        isValidActivation: true,
                        activationType: activationType,
                        affectedCoordinates: new[] { new BoardCoordinate(0, 0) },
                        removedItemCoordinates: new[] { new BoardCoordinate(0, 0) }),
                    obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                    gravity: ItemGravityResolutionResult.Empty(),
                    refill: ItemRefillResolutionResult.Empty()));
        }
    }
}
