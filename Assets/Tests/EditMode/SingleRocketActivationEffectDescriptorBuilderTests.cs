using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SingleRocketActivationEffectDescriptorBuilderTests
    {
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();
        private readonly SingleRocketActivationEffectDescriptorBuilder builder = new SingleRocketActivationEffectDescriptorBuilder();

        [Test]
        public void BuildReturnsLeftAndRightEndsForHorizontalRocket()
        {
            var board = new BoardModel(5, 3);
            var tap = new BoardCoordinate(2, 1);
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));

            var activation = specialItemTapResolver.Resolve(board, tap);
            var descriptor = builder.Build(tap, activation);

            Assert.That(descriptor.Orientation, Is.EqualTo(RocketOrientation.Horizontal));
            Assert.That(descriptor.Origin, Is.EqualTo(tap));
            Assert.That(descriptor.NegativeEnd, Is.EqualTo(new BoardCoordinate(0, 1)));
            Assert.That(descriptor.PositiveEnd, Is.EqualTo(new BoardCoordinate(4, 1)));
        }

        [Test]
        public void BuildReturnsBottomAndTopEndsForVerticalRocket()
        {
            var board = new BoardModel(4, 6);
            var tap = new BoardCoordinate(1, 3);
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Vertical));

            var activation = specialItemTapResolver.Resolve(board, tap);
            var descriptor = builder.Build(tap, activation);

            Assert.That(descriptor.Orientation, Is.EqualTo(RocketOrientation.Vertical));
            Assert.That(descriptor.Origin, Is.EqualTo(tap));
            Assert.That(descriptor.NegativeEnd, Is.EqualTo(new BoardCoordinate(1, 0)));
            Assert.That(descriptor.PositiveEnd, Is.EqualTo(new BoardCoordinate(1, 5)));
        }

        [Test]
        public void BuildRejectsNonRocketActivation()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);
            board.PlaceItem(tap, new TntItemModel());

            var activation = specialItemTapResolver.Resolve(board, tap);

            Assert.That(() => builder.Build(tap, activation), Throws.InvalidOperationException);
        }
    }
}
