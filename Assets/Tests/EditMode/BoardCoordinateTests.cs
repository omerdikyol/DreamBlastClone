using System.Linq;
using DreamBlastClone.Core;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardCoordinateTests
    {
        [Test]
        public void EqualityAndHashCodeMatchForSameCoordinate()
        {
            var first = new BoardCoordinate(2, 3);
            var second = new BoardCoordinate(2, 3);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
        }

        [Test]
        public void OffsetReturnsTranslatedCoordinate()
        {
            var coordinate = new BoardCoordinate(4, 5);

            var offset = coordinate.Offset(-2, 3);

            Assert.That(offset, Is.EqualTo(new BoardCoordinate(2, 8)));
        }

        [Test]
        public void OrthogonalNeighborsReturnsExpectedCoordinates()
        {
            var coordinate = new BoardCoordinate(3, 4);

            var neighbors = coordinate.GetOrthogonalNeighbors().ToArray();

            Assert.That(neighbors, Is.EqualTo(new[]
            {
                new BoardCoordinate(3, 5),
                new BoardCoordinate(4, 4),
                new BoardCoordinate(3, 3),
                new BoardCoordinate(2, 4)
            }));
        }
    }
}
