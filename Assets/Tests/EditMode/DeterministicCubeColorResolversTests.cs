using DreamBlastClone.Core;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class DeterministicCubeColorResolversTests
    {
        [Test]
        public void InitialResolverReturnsSameColorForSameSeedAndCoordinate()
        {
            var resolverA = new DeterministicInitialCubeColorResolver(seed: 12345u);
            var resolverB = new DeterministicInitialCubeColorResolver(seed: 12345u);
            var coordinate = new BoardCoordinate(4, 6);

            var colorA = resolverA.ResolveColor(coordinate);
            var colorB = resolverB.ResolveColor(coordinate);

            Assert.That(colorA, Is.EqualTo(colorB));
        }

        [Test]
        public void RefillResolverReturnsMatchingSequenceAcrossFreshInstances()
        {
            var resolverA = new DeterministicRefillSequenceResolver(seed: 54321u);
            var resolverB = new DeterministicRefillSequenceResolver(seed: 54321u);
            var requests = new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 1)
            };

            for (var i = 0; i < requests.Length; i++)
            {
                Assert.That(
                    resolverA.ResolveColor(requests[i]),
                    Is.EqualTo(resolverB.ResolveColor(requests[i])),
                    $"Expected matching refill color at request index {i}.");
            }
        }

        [Test]
        public void RefillResolverDoesNotPinRepeatedCoordinateToSingleColor()
        {
            var resolver = new DeterministicRefillSequenceResolver(seed: 777u);
            var coordinate = new BoardCoordinate(2, 3);

            var firstColor = resolver.ResolveColor(coordinate);
            var secondColor = resolver.ResolveColor(coordinate);
            var thirdColor = resolver.ResolveColor(coordinate);

            Assert.That(secondColor, Is.Not.EqualTo(firstColor));
            Assert.That(thirdColor, Is.Not.EqualTo(secondColor));
        }
    }
}
