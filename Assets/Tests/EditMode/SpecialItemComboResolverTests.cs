using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialItemComboResolverTests
    {
        private readonly SpecialItemComboResolver resolver = new SpecialItemComboResolver();

        [Test]
        public void InvalidTapsReturnNoCombo()
        {
            var board = new BoardModel(3, 3);

            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Red));

            AssertInvalid(resolver.Resolve(null, new BoardCoordinate(0, 0)));
            AssertInvalid(resolver.Resolve(board, new BoardCoordinate(0, 0)));
            AssertInvalid(resolver.Resolve(board, new BoardCoordinate(1, 1)));
            AssertInvalid(resolver.Resolve(board, new BoardCoordinate(-1, 0)));
        }

        [Test]
        public void NoAdjacentSpecialReturnsInvalidWithoutMutatingBoard()
        {
            var board = new BoardModel(3, 3);
            var rocket = new RocketItemModel(RocketOrientation.Horizontal);

            board.PlaceItem(new BoardCoordinate(1, 1), rocket);
            board.PlaceItem(new BoardCoordinate(2, 2), new TntItemModel());

            var result = resolver.Resolve(board, new BoardCoordinate(1, 1));

            AssertInvalid(result);
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Item, Is.SameAs(rocket));
            Assert.That(board.GetCell(new BoardCoordinate(2, 2)).Item, Is.TypeOf<TntItemModel>());
        }

        [Test]
        public void PartnerSelectionUsesOrthogonalNeighborOrder()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);

            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 2), new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 1), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(0, 1), new TntItemModel());

            var result = resolver.Resolve(board, tap);

            Assert.That(result.IsComboActivated, Is.True);
            Assert.That(result.ComboType, Is.EqualTo(SpecialItemComboType.TntRocket));
            Assert.That(result.ParticipatingSpecialCoordinates, Is.EqualTo(new[]
            {
                tap,
                new BoardCoordinate(1, 2)
            }));
        }

        [Test]
        public void RocketRocketReturnsCrossFootprintAndRemovesBothParticipatingRockets()
        {
            var board = new BoardModel(5, 4);
            var tap = new BoardCoordinate(2, 1);
            var partner = new BoardCoordinate(2, 2);

            board.PlaceItem(tap, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(partner, new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(4, 3), new CubeItemModel(CubeColor.Green));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.ComboType, Is.EqualTo(SpecialItemComboType.RocketRocket));
            Assert.That(result.AffectedCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(3, 1),
                new BoardCoordinate(4, 1),
                new BoardCoordinate(2, 2),
                new BoardCoordinate(2, 3)
            }));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(2, 2)
            }));
            Assert.That(board.GetCell(tap).Item, Is.Null);
            Assert.That(board.GetCell(partner).Item, Is.Null);
            Assert.That(board.GetCell(new BoardCoordinate(4, 3)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void TntTntReturnsClippedSevenBySevenArea()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(0, 0);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(0, 1), new TntItemModel());
            board.PlaceItem(new BoardCoordinate(4, 4), new CubeItemModel(CubeColor.Yellow));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.ComboType, Is.EqualTo(SpecialItemComboType.TntTnt));
            Assert.That(result.AffectedCoordinates.Count, Is.EqualTo(16));
            Assert.That(result.AffectedCoordinates[0], Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(result.AffectedCoordinates[^1], Is.EqualTo(new BoardCoordinate(3, 3)));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(0, 1)
            }));
            Assert.That(board.GetCell(new BoardCoordinate(4, 4)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void TntRocketReturnsUnionOfThreeRowsAndThreeColumns()
        {
            var board = new BoardModel(5, 5);
            var tap = new BoardCoordinate(2, 2);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 3), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(4, 4), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(4, 2), new RocketItemModel(RocketOrientation.Vertical));

            var result = resolver.Resolve(board, tap);

            Assert.That(result.ComboType, Is.EqualTo(SpecialItemComboType.TntRocket));
            Assert.That(result.AffectedCoordinates, Is.EqualTo(new[]
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
            }));
            Assert.That(result.RemovedItemCoordinates, Is.EqualTo(new[]
            {
                new BoardCoordinate(2, 2),
                new BoardCoordinate(4, 2),
                new BoardCoordinate(2, 3)
            }));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(4, 4)).Item, Is.TypeOf<CubeItemModel>());
        }

        [Test]
        public void DiagonalSpecialsDoNotParticipate()
        {
            var board = new BoardModel(3, 3);
            var tap = new BoardCoordinate(1, 1);

            board.PlaceItem(tap, new TntItemModel());
            board.PlaceItem(new BoardCoordinate(2, 2), new RocketItemModel(RocketOrientation.Horizontal));

            var result = resolver.Resolve(board, tap);

            AssertInvalid(result);
            Assert.That(board.GetCell(tap).Item, Is.TypeOf<TntItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(2, 2)).Item, Is.TypeOf<RocketItemModel>());
        }

        private static void AssertInvalid(SpecialItemComboActivationResult result)
        {
            Assert.That(result.IsComboActivated, Is.False);
            Assert.That(result.ComboType, Is.EqualTo(SpecialItemComboType.None));
            Assert.That(result.ParticipatingSpecialCoordinates, Is.Empty);
            Assert.That(result.AffectedCoordinates, Is.Empty);
            Assert.That(result.RemovedItemCoordinates, Is.Empty);
        }
    }
}
