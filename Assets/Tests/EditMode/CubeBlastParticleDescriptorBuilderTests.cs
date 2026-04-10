using DreamBlastClone.Core;
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
            Assert.That(descriptor.CubeColor, Is.EqualTo(CubeColor.Blue));
            Assert.That(descriptor.BurstCoordinates, Is.EqualTo(new[]
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
            Assert.That(descriptor.CubeColor, Is.EqualTo(CubeColor.Red));
            Assert.That(descriptor.BurstCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(3, 0)
            }));
        }
    }
}
