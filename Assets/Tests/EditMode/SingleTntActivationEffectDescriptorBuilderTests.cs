using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SingleTntActivationEffectDescriptorBuilderTests
    {
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();
        private readonly SingleTntActivationEffectDescriptorBuilder builder = new SingleTntActivationEffectDescriptorBuilder();

        [Test]
        public void BuildReturnsClippedFootprintForTntActivation()
        {
            var board = new BoardModel(4, 4);
            var tap = new BoardCoordinate(1, 1);
            board.PlaceItem(tap, new TntItemModel());

            var activation = specialItemTapResolver.Resolve(board, tap);
            var descriptor = builder.Build(tap, activation);

            Assert.That(descriptor.Origin, Is.EqualTo(tap));
            Assert.That(descriptor.MinX, Is.EqualTo(0));
            Assert.That(descriptor.MinY, Is.EqualTo(0));
            Assert.That(descriptor.MaxX, Is.EqualTo(3));
            Assert.That(descriptor.MaxY, Is.EqualTo(3));
            Assert.That(descriptor.AffectedCoordinates, Has.Count.EqualTo(16));
        }

        [Test]
        public void BuildRejectsNonTntActivation()
        {
            var board = new BoardModel(5, 3);
            var tap = new BoardCoordinate(2, 1);
            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));

            var activation = specialItemTapResolver.Resolve(board, tap);

            Assert.That(() => builder.Build(tap, activation), Throws.InvalidOperationException);
        }
    }
}
