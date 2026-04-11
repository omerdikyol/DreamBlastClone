using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelBoardBuilderTests
    {
        [Test]
        public void BuildsEmptyBoardFromEmptyLevel()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(1, 2, 3, 10, Array.Empty<LevelCellDefinition>());

            var board = builder.Build(level, randomCubeColorResolver: null);

            Assert.That(board.Width, Is.EqualTo(2));
            Assert.That(board.Height, Is.EqualTo(3));
            Assert.That(board.GetAllCells(), Has.All.Matches<CellModel>(cell => cell.IsCompletelyEmpty));
        }

        [Test]
        public void BuildsSingleCellItemDefinitionsIntoRuntimeItems()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                4,
                1,
                12,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), new CubeLevelItemDefinition(CubeColor.Red)),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), new RocketLevelItemDefinition(RocketOrientation.Vertical)),
                    new LevelCellDefinition(new BoardCoordinate(2, 0), new TntLevelItemDefinition()),
                    new LevelCellDefinition(new BoardCoordinate(3, 0), new RandomCubeLevelItemDefinition())
                });

            var board = builder.Build(level, new FixedColorResolver(CubeColor.Green));

            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(((CubeItemModel)board.GetCell(new BoardCoordinate(0, 0)).Item).Color, Is.EqualTo(CubeColor.Red));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Item, Is.TypeOf<RocketItemModel>());
            Assert.That(((RocketItemModel)board.GetCell(new BoardCoordinate(1, 0)).Item).Orientation, Is.EqualTo(RocketOrientation.Vertical));
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Item, Is.TypeOf<TntItemModel>());
            Assert.That(((CubeItemModel)board.GetCell(new BoardCoordinate(3, 0)).Item).Color, Is.EqualTo(CubeColor.Green));
        }

        [Test]
        public void BuildsSingleCellObstacleDefinitionsIntoRuntimeObstacles()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                2,
                1,
                10,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), obstacle: new VaseLevelObstacleDefinition()),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), obstacle: new StoneLevelObstacleDefinition())
                });

            var board = builder.Build(level, randomCubeColorResolver: null);

            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.TypeOf<VaseObstacleModel>());
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Obstacle, Is.TypeOf<StoneObstacleModel>());
        }

        [Test]
        public void BuildsBothLayersIntoTheSameCell()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                1,
                1,
                5,
                new[]
                {
                    new LevelCellDefinition(
                        new BoardCoordinate(0, 0),
                        new CubeLevelItemDefinition(CubeColor.Blue),
                        new VaseLevelObstacleDefinition())
                });

            var board = builder.Build(level, randomCubeColorResolver: null);

            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.TypeOf<VaseObstacleModel>());
        }

        [Test]
        public void BuildsChaliceBoxAsOneSharedRuntimeObstacle()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                3,
                3,
                10,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(0, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopRight))
                });

            var board = builder.Build(level, randomCubeColorResolver: null);
            var chaliceBox = (ChaliceBoxObstacleModel)board.GetCell(new BoardCoordinate(0, 0)).Obstacle;

            Assert.That(chaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(chaliceBox.RemainingDoorDurability, Is.EqualTo(1));
            Assert.That(chaliceBox.RequiredChaliceCount, Is.EqualTo(10));
            Assert.That(chaliceBox.CollectedChaliceCount, Is.EqualTo(0));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(0, 1)).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void BuildsWideAuthoredChaliceRegionAsMultipleTwoByTwoRuntimeObstacles()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                7,
                3,
                10,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(1, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(2, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(3, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(4, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(1, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                    new LevelCellDefinition(new BoardCoordinate(2, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                    new LevelCellDefinition(new BoardCoordinate(3, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                    new LevelCellDefinition(new BoardCoordinate(4, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopRight))
                });

            var board = builder.Build(level, randomCubeColorResolver: null);
            var leftChaliceBox = (ChaliceBoxObstacleModel)board.GetCell(new BoardCoordinate(1, 0)).Obstacle;
            var rightChaliceBox = (ChaliceBoxObstacleModel)board.GetCell(new BoardCoordinate(3, 0)).Obstacle;

            Assert.That(leftChaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(1, 0)));
            Assert.That(leftChaliceBox.FootprintWidth, Is.EqualTo(2));
            Assert.That(leftChaliceBox.FootprintHeight, Is.EqualTo(2));
            Assert.That(board.GetCell(new BoardCoordinate(2, 0)).Obstacle, Is.SameAs(leftChaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(1, 1)).Obstacle, Is.SameAs(leftChaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(2, 1)).Obstacle, Is.SameAs(leftChaliceBox));

            Assert.That(rightChaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(3, 0)));
            Assert.That(rightChaliceBox.FootprintWidth, Is.EqualTo(2));
            Assert.That(rightChaliceBox.FootprintHeight, Is.EqualTo(2));
            Assert.That(board.GetCell(new BoardCoordinate(4, 0)).Obstacle, Is.SameAs(rightChaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(3, 1)).Obstacle, Is.SameAs(rightChaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(4, 1)).Obstacle, Is.SameAs(rightChaliceBox));
            Assert.That(rightChaliceBox, Is.Not.SameAs(leftChaliceBox));
        }

        [Test]
        public void BuildsTallAuthoredChaliceRegionAsMultipleTwoByTwoRuntimeObstacles()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                3,
                6,
                10,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(0, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(0, 2), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 2), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(0, 3), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 3), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopRight))
                });

            var board = builder.Build(level, randomCubeColorResolver: null);
            var bottomChaliceBox = (ChaliceBoxObstacleModel)board.GetCell(new BoardCoordinate(0, 0)).Obstacle;
            var topChaliceBox = (ChaliceBoxObstacleModel)board.GetCell(new BoardCoordinate(0, 2)).Obstacle;

            Assert.That(bottomChaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(topChaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(0, 2)));
            Assert.That(bottomChaliceBox, Is.Not.SameAs(topChaliceBox));

            foreach (var coordinate in bottomChaliceBox.OccupiedCoordinates)
            {
                Assert.That(board.GetCell(coordinate).Obstacle, Is.SameAs(bottomChaliceBox));
            }

            foreach (var coordinate in topChaliceBox.OccupiedCoordinates)
            {
                Assert.That(board.GetCell(coordinate).Obstacle, Is.SameAs(topChaliceBox));
            }
        }

        [Test]
        public void ThrowsWhenRandomCubeResolverIsMissing()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                1,
                1,
                5,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), new RandomCubeLevelItemDefinition())
                });

            Assert.That(
                () => builder.Build(level, randomCubeColorResolver: null),
                Throws.ArgumentNullException.With.Message.Contains("randomCubeColorResolver"));
        }

        [Test]
        public void ThrowsWhenRandomCubeResolverReturnsInvalidColor()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                1,
                1,
                5,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), new RandomCubeLevelItemDefinition())
                });

            Assert.That(
                () => builder.Build(level, new InvalidColorResolver()),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("invalid color"));
        }

        [Test]
        public void ThrowsWhenChaliceBoxIsMissingPart()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                3,
                3,
                10,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(0, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft))
                });

            Assert.That(
                () => builder.Build(level, randomCubeColorResolver: null),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("incomplete"));
        }

        [Test]
        public void ThrowsWhenChaliceBoxPartsDoNotFormAValidFootprint()
        {
            var builder = new LevelBoardBuilder();
            var level = new LevelDefinition(
                1,
                3,
                2,
                10,
                new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                    new LevelCellDefinition(new BoardCoordinate(0, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                    new LevelCellDefinition(new BoardCoordinate(1, 1), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft))
                });

            Assert.That(
                () => builder.Build(level, randomCubeColorResolver: null),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("expected"));
        }

        private sealed class FixedColorResolver : IRandomCubeColorResolver
        {
            private readonly CubeColor color;

            public FixedColorResolver(CubeColor color)
            {
                this.color = color;
            }

            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                return color;
            }
        }

        private sealed class InvalidColorResolver : IRandomCubeColorResolver
        {
            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                return (CubeColor)999;
            }
        }
    }
}
