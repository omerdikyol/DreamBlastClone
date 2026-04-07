using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class MoveSpendEvaluatorTests
    {
        private readonly BoardTapDispatcher dispatcher = new BoardTapDispatcher();
        private readonly MoveSpendEvaluator evaluator = new MoveSpendEvaluator();

        [Test]
        public void NullTapResultThrows()
        {
            Assert.That(
                () => evaluator.ShouldSpendMove(null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void InvalidTopLevelTapDoesNotSpendMove()
        {
            Assert.That(evaluator.ShouldSpendMove(BoardTapDispatchResult.Invalid()), Is.False);
        }

        [Test]
        public void ValidNormalCubeTapSpendsMove()
        {
            var board = new BoardModel(3, 3);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Yellow },
                { new BoardCoordinate(1, 2), CubeColor.Green },
                { new BoardCoordinate(2, 1), CubeColor.Blue },
                { new BoardCoordinate(2, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Blue));

            var result = dispatcher.Resolve(board, new BoardCoordinate(0, 1), refillResolver);

            Assert.That(result.NormalCube.IsValidTap, Is.True);
            Assert.That(evaluator.ShouldSpendMove(result), Is.True);
        }

        [Test]
        public void ValidSingleSpecialTapSpendsMove()
        {
            var board = new BoardModel(4, 3);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Yellow },
                { new BoardCoordinate(1, 2), CubeColor.Green },
                { new BoardCoordinate(2, 2), CubeColor.Blue },
                { new BoardCoordinate(3, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 2), new CubeItemModel(CubeColor.Red));

            var result = dispatcher.Resolve(board, new BoardCoordinate(1, 1), refillResolver);

            Assert.That(result.SpecialItem.IsValidTap, Is.True);
            Assert.That(result.SpecialItem.Combo.IsComboActivated, Is.False);
            Assert.That(evaluator.ShouldSpendMove(result), Is.True);
        }

        [Test]
        public void ValidSpecialComboTapSpendsMove()
        {
            var board = new BoardModel(3, 3);
            var refillResolver = new FakeRefillCubeColorResolver(new Dictionary<BoardCoordinate, CubeColor>
            {
                { new BoardCoordinate(0, 2), CubeColor.Red },
                { new BoardCoordinate(1, 0), CubeColor.Green },
                { new BoardCoordinate(1, 1), CubeColor.Blue },
                { new BoardCoordinate(1, 2), CubeColor.Yellow },
                { new BoardCoordinate(2, 2), CubeColor.Red }
            });

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 2), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Green));

            var result = dispatcher.Resolve(board, new BoardCoordinate(1, 1), refillResolver);

            Assert.That(result.SpecialItem.IsValidTap, Is.True);
            Assert.That(result.SpecialItem.Combo.IsComboActivated, Is.True);
            Assert.That(evaluator.ShouldSpendMove(result), Is.True);
        }

        private sealed class FakeRefillCubeColorResolver : IRefillCubeColorResolver
        {
            private readonly IReadOnlyDictionary<BoardCoordinate, CubeColor> colorsByCoordinate;

            public FakeRefillCubeColorResolver(IReadOnlyDictionary<BoardCoordinate, CubeColor> colorsByCoordinate = null)
            {
                this.colorsByCoordinate = colorsByCoordinate;
            }

            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                if (colorsByCoordinate is not null && colorsByCoordinate.TryGetValue(coordinate, out var color))
                {
                    return color;
                }

                return CubeColor.Red;
            }
        }
    }
}
