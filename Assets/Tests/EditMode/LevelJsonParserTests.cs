using System;
using System.IO;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelJsonParserTests
    {
        [Test]
        public void ParseRealLevelJsonIntoExpectedDefinition()
        {
            var parser = new LevelJsonParser();

            var level = parser.Parse(ReadLevelJson("level_01.json"));

            Assert.That(level.LevelNumber, Is.EqualTo(1));
            Assert.That(level.GridWidth, Is.EqualTo(10));
            Assert.That(level.GridHeight, Is.EqualTo(7));
            Assert.That(level.MoveCount, Is.EqualTo(15));
            Assert.That(level.CellDefinitions, Has.Count.EqualTo(70));
            Assert.That(level.CellDefinitions[0].Coordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(level.CellDefinitions[0].Obstacle, Is.TypeOf<StoneLevelObstacleDefinition>());
            Assert.That(level.CellDefinitions[26].Item, Is.TypeOf<CubeLevelItemDefinition>());
            Assert.That(((CubeLevelItemDefinition)level.CellDefinitions[26].Item).Color, Is.EqualTo(CubeColor.Green));
            Assert.That(level.CellDefinitions[30].Item, Is.TypeOf<RandomCubeLevelItemDefinition>());
        }

        [Test]
        public void UnknownSymbolThrows()
        {
            var parser = new LevelJsonParser();
            const string json = "{\"level_number\":1,\"grid_width\":1,\"grid_height\":1,\"move_count\":3,\"grid\":[\"mystery\"]}";

            Assert.That(
                () => parser.Parse(json),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("unsupported grid symbol"));
        }

        [Test]
        public void GridLengthMismatchThrows()
        {
            var parser = new LevelJsonParser();
            const string json = "{\"level_number\":1,\"grid_width\":2,\"grid_height\":2,\"move_count\":3,\"grid\":[\"r\",\"g\",\"b\"]}";

            Assert.That(
                () => parser.Parse(json),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("grid length"));
        }

        [Test]
        public void ChaliceBoxPartsNormalizeCorrectlyThroughLevelBoardBuilder()
        {
            var parser = new LevelJsonParser();
            var builder = new LevelBoardBuilder();

            var level = parser.Parse(ReadLevelJson("level_04.json"));
            var board = builder.Build(level, new FixedColorResolver(CubeColor.Yellow));

            var chaliceBox = board.GetCell(new BoardCoordinate(0, 2)).Obstacle as ChaliceBoxObstacleModel;

            Assert.That(chaliceBox, Is.Not.Null);
            Assert.That(chaliceBox.Anchor, Is.EqualTo(new BoardCoordinate(0, 2)));
            Assert.That(board.GetCell(new BoardCoordinate(1, 2)).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(0, 3)).Obstacle, Is.SameAs(chaliceBox));
            Assert.That(board.GetCell(new BoardCoordinate(1, 3)).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void ParseAllAuthoredLevelsSuccessfully()
        {
            var parser = new LevelJsonParser();

            foreach (var filePath in Directory.GetFiles(Path.Combine(Application.dataPath, "GameContent", "Levels"), "level_*.json"))
            {
                Assert.That(() => parser.Parse(File.ReadAllText(filePath)), Throws.Nothing, Path.GetFileName(filePath));
            }
        }

        [Test]
        public void LevelThreeChaliceRegionsNormalizeIntoExpectedTwoByTwoBoxes()
        {
            var parser = new LevelJsonParser();
            var builder = new LevelBoardBuilder();
            var level = parser.Parse(ReadLevelJson("level_03.json"));
            var board = builder.Build(level, new FixedColorResolver(CubeColor.Yellow));

            AssertChaliceBox(board, new BoardCoordinate(0, 0));
            AssertChaliceBox(board, new BoardCoordinate(3, 0));
            AssertChaliceBox(board, new BoardCoordinate(5, 0));
            AssertChaliceBox(board, new BoardCoordinate(8, 0));
            Assert.That(board.GetCell(new BoardCoordinate(3, 0)).Obstacle, Is.Not.SameAs(board.GetCell(new BoardCoordinate(5, 0)).Obstacle));
        }

        [Test]
        public void LevelNineChaliceRegionsNormalizeIntoExpectedTwoByTwoBoxes()
        {
            var parser = new LevelJsonParser();
            var builder = new LevelBoardBuilder();
            var level = parser.Parse(ReadLevelJson("level_09.json"));
            var board = builder.Build(level, new FixedColorResolver(CubeColor.Yellow));

            AssertChaliceBox(board, new BoardCoordinate(0, 5));
            AssertChaliceBox(board, new BoardCoordinate(0, 7));
            AssertChaliceBox(board, new BoardCoordinate(6, 0));
            AssertChaliceBox(board, new BoardCoordinate(6, 2));
            Assert.That(board.GetCell(new BoardCoordinate(0, 5)).Obstacle, Is.Not.SameAs(board.GetCell(new BoardCoordinate(0, 7)).Obstacle));
            Assert.That(board.GetCell(new BoardCoordinate(6, 0)).Obstacle, Is.Not.SameAs(board.GetCell(new BoardCoordinate(6, 2)).Obstacle));
        }

        private static string ReadLevelJson(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "GameContent", "Levels", fileName);
            return File.ReadAllText(path);
        }

        private static void AssertChaliceBox(DreamBlastClone.Grid.BoardModel board, BoardCoordinate anchor)
        {
            var chaliceBox = board.GetCell(anchor).Obstacle as ChaliceBoxObstacleModel;

            Assert.That(chaliceBox, Is.Not.Null);
            Assert.That(chaliceBox.Anchor, Is.EqualTo(anchor));
            Assert.That(chaliceBox.FootprintWidth, Is.EqualTo(2));
            Assert.That(chaliceBox.FootprintHeight, Is.EqualTo(2));
            Assert.That(chaliceBox.OccupiedCoordinates, Has.Count.EqualTo(4));

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    Assert.That(board.GetCell(anchor.Offset(x, y)).Obstacle, Is.SameAs(chaliceBox));
                }
            }
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
    }
}
