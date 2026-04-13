using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class MoveHintOutlineSegmentBuilderTests
    {
        private readonly MoveHintOutlineSegmentBuilder builder = new MoveHintOutlineSegmentBuilder();

        [Test]
        public void BuildSingleCellReturnsFourBoundarySegments()
        {
            var segments = builder.Build(new[] { new BoardCoordinate(2, 3) });

            Assert.That(segments, Is.EquivalentTo(new[]
            {
                new MoveHintOutlineSegment(2, 3, 3, 3),
                new MoveHintOutlineSegment(3, 3, 3, 4),
                new MoveHintOutlineSegment(2, 4, 3, 4),
                new MoveHintOutlineSegment(2, 3, 2, 4)
            }));
        }

        [Test]
        public void BuildAdjacentCellsOmitsSharedInteriorEdge()
        {
            var segments = builder.Build(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0)
            });

            Assert.That(segments.Count, Is.EqualTo(6));
            Assert.That(segments, Has.None.EqualTo(new MoveHintOutlineSegment(1, 0, 1, 1)));
        }
    }
}
