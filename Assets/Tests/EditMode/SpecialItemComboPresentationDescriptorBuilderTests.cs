using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialItemComboPresentationDescriptorBuilderTests
    {
        private readonly SpecialItemComboPresentationDescriptorBuilder builder = new SpecialItemComboPresentationDescriptorBuilder();

        [Test]
        public void BuildRocketRocketCreatesHorizontalAndVerticalSweepsThroughComboOrigin()
        {
            var combo = new SpecialItemComboActivationResult(
                isComboActivated: true,
                comboType: SpecialItemComboType.RocketRocket,
                participatingSpecialCoordinates: new[]
                {
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(2, 2)
                },
                affectedCoordinates: new[]
                {
                    new BoardCoordinate(2, 0),
                    new BoardCoordinate(0, 1),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(3, 1),
                    new BoardCoordinate(4, 1),
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(2, 3)
                },
                removedItemCoordinates: System.Array.Empty<BoardCoordinate>());

            var descriptor = builder.Build(combo);

            Assert.That(descriptor.ComboType, Is.EqualTo(SpecialItemComboType.RocketRocket));
            Assert.That(descriptor.ComboOrigin, Is.EqualTo(new BoardCoordinate(2, 1)));
            Assert.That(descriptor.FlashCoordinates, Is.EqualTo(combo.AffectedCoordinates));
            Assert.That(descriptor.RocketSweeps.Count, Is.EqualTo(2));
            Assert.That(descriptor.HasTntPulse, Is.False);

            var horizontalSweep = descriptor.RocketSweeps.Single(sweep => sweep.Orientation == RocketOrientation.Horizontal);
            Assert.That(horizontalSweep.Origin, Is.EqualTo(new BoardCoordinate(2, 1)));
            Assert.That(horizontalSweep.NegativeEnd, Is.EqualTo(new BoardCoordinate(0, 1)));
            Assert.That(horizontalSweep.PositiveEnd, Is.EqualTo(new BoardCoordinate(4, 1)));

            var verticalSweep = descriptor.RocketSweeps.Single(sweep => sweep.Orientation == RocketOrientation.Vertical);
            Assert.That(verticalSweep.Origin, Is.EqualTo(new BoardCoordinate(2, 1)));
            Assert.That(verticalSweep.NegativeEnd, Is.EqualTo(new BoardCoordinate(2, 0)));
            Assert.That(verticalSweep.PositiveEnd, Is.EqualTo(new BoardCoordinate(2, 3)));
        }

        [Test]
        public void BuildTntTntUsesAffectedCoordinatesForFlashAndAddsPulseOnly()
        {
            var combo = new SpecialItemComboActivationResult(
                isComboActivated: true,
                comboType: SpecialItemComboType.TntTnt,
                participatingSpecialCoordinates: new[]
                {
                    new BoardCoordinate(0, 0),
                    new BoardCoordinate(0, 1)
                },
                affectedCoordinates: new[]
                {
                    new BoardCoordinate(0, 0),
                    new BoardCoordinate(1, 0),
                    new BoardCoordinate(2, 0),
                    new BoardCoordinate(0, 1),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(0, 2),
                    new BoardCoordinate(1, 2),
                    new BoardCoordinate(2, 2)
                },
                removedItemCoordinates: System.Array.Empty<BoardCoordinate>());

            var descriptor = builder.Build(combo);

            Assert.That(descriptor.ComboType, Is.EqualTo(SpecialItemComboType.TntTnt));
            Assert.That(descriptor.FlashCoordinates, Is.EqualTo(combo.AffectedCoordinates));
            Assert.That(descriptor.RocketSweeps, Is.Empty);
            Assert.That(descriptor.HasTntPulse, Is.True);
        }

        [Test]
        public void BuildTntRocketCreatesThreeRowsAndThreeColumnsFromAffectedFootprint()
        {
            var combo = new SpecialItemComboActivationResult(
                isComboActivated: true,
                comboType: SpecialItemComboType.TntRocket,
                participatingSpecialCoordinates: new[]
                {
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(2, 3)
                },
                affectedCoordinates: new[]
                {
                    new BoardCoordinate(1, 0),
                    new BoardCoordinate(2, 0),
                    new BoardCoordinate(3, 0),
                    new BoardCoordinate(0, 1),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(3, 1),
                    new BoardCoordinate(4, 1),
                    new BoardCoordinate(0, 2),
                    new BoardCoordinate(1, 2),
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(3, 2),
                    new BoardCoordinate(4, 2),
                    new BoardCoordinate(0, 3),
                    new BoardCoordinate(1, 3),
                    new BoardCoordinate(2, 3),
                    new BoardCoordinate(3, 3),
                    new BoardCoordinate(4, 3),
                    new BoardCoordinate(1, 4),
                    new BoardCoordinate(2, 4),
                    new BoardCoordinate(3, 4)
                },
                removedItemCoordinates: System.Array.Empty<BoardCoordinate>());

            var descriptor = builder.Build(combo);

            Assert.That(descriptor.ComboType, Is.EqualTo(SpecialItemComboType.TntRocket));
            Assert.That(descriptor.RocketSweeps.Count, Is.EqualTo(6));
            Assert.That(descriptor.HasTntPulse, Is.True);

            var horizontalOrigins = descriptor.RocketSweeps
                .Where(sweep => sweep.Orientation == RocketOrientation.Horizontal)
                .Select(sweep => sweep.Origin)
                .ToArray();
            Assert.That(horizontalOrigins, Is.EqualTo(new[]
            {
                new BoardCoordinate(2, 1),
                new BoardCoordinate(2, 2),
                new BoardCoordinate(2, 3)
            }));

            var verticalOrigins = descriptor.RocketSweeps
                .Where(sweep => sweep.Orientation == RocketOrientation.Vertical)
                .Select(sweep => sweep.Origin)
                .ToArray();
            Assert.That(verticalOrigins, Is.EqualTo(new[]
            {
                new BoardCoordinate(1, 2),
                new BoardCoordinate(2, 2),
                new BoardCoordinate(3, 2)
            }));

            var topRowSweep = descriptor.RocketSweeps.Single(sweep => sweep.Orientation == RocketOrientation.Horizontal && sweep.Origin == new BoardCoordinate(2, 3));
            Assert.That(topRowSweep.NegativeEnd, Is.EqualTo(new BoardCoordinate(0, 3)));
            Assert.That(topRowSweep.PositiveEnd, Is.EqualTo(new BoardCoordinate(4, 3)));

            var leftColumnSweep = descriptor.RocketSweeps.Single(sweep => sweep.Orientation == RocketOrientation.Vertical && sweep.Origin == new BoardCoordinate(1, 2));
            Assert.That(leftColumnSweep.NegativeEnd, Is.EqualTo(new BoardCoordinate(1, 0)));
            Assert.That(leftColumnSweep.PositiveEnd, Is.EqualTo(new BoardCoordinate(1, 4)));
        }
    }
}
