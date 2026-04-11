using System.IO;
using System.Linq;
using DreamBlastClone.Controllers;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelSessionFactoryTests
    {
        [Test]
        public void CreateBuildsRealLevelSessionFromParsedDefinition()
        {
            var parser = new LevelJsonParser();
            var factory = new LevelSessionFactory();

            var level = parser.Parse(ReadLevelJson("level_01.json"));
            var session = factory.Create(level);

            Assert.That(session.RemainingMoves, Is.EqualTo(15));
            Assert.That(session.Board.Width, Is.EqualTo(10));
            Assert.That(session.Board.Height, Is.EqualTo(7));
            Assert.That(session.Board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.TypeOf<StoneObstacleModel>());
            Assert.That(session.Board.GetCell(new BoardCoordinate(0, 2)).Item, Is.TypeOf<CubeItemModel>());
            Assert.That(session.Goals, Has.Count.EqualTo(1));
            Assert.That(session.Goals[0].GoalType, Is.EqualTo(LevelGoalType.Stone));
            Assert.That(session.Goals[0].InitialCount, Is.EqualTo(20));
        }

        [Test]
        public void CreateUsesDeterministicRandomColorResolution()
        {
            var parser = new LevelJsonParser();
            var factory = new LevelSessionFactory();
            var level = parser.Parse(ReadLevelJson("level_01.json"));

            var sessionA = factory.Create(level);
            var sessionB = factory.Create(level);
            var randomCoordinate = new BoardCoordinate(7, 5);

            var cubeA = (CubeItemModel)sessionA.Board.GetCell(randomCoordinate).Item;
            var cubeB = (CubeItemModel)sessionB.Board.GetCell(randomCoordinate).Item;

            Assert.That(cubeA.Color, Is.EqualTo(cubeB.Color));
        }

        [Test]
        public void CreateUsesDeterministicRefillSequenceAcrossMatchingPlaythroughs()
        {
            var factory = new LevelSessionFactory();
            var level = new LevelDefinition(
                levelNumber: 99,
                gridWidth: 2,
                gridHeight: 3,
                moveCount: 5,
                cellDefinitions: new[]
                {
                    new LevelCellDefinition(new BoardCoordinate(0, 0), new CubeLevelItemDefinition(CubeColor.Red)),
                    new LevelCellDefinition(new BoardCoordinate(1, 0), new CubeLevelItemDefinition(CubeColor.Red)),
                    new LevelCellDefinition(new BoardCoordinate(1, 2), obstacle: new StoneLevelObstacleDefinition())
                });

            var sessionA = factory.Create(level);
            var sessionB = factory.Create(level);

            var resultA = sessionA.ProcessTap(new BoardCoordinate(0, 0));
            var resultB = sessionB.ProcessTap(new BoardCoordinate(0, 0));

            Assert.That(resultA.Tap.NormalCube.IsValidTap, Is.True);
            Assert.That(resultB.Tap.NormalCube.IsValidTap, Is.True);
            Assert.That(resultA.Tap.NormalCube.Refill.Spawns, Is.EqualTo(resultB.Tap.NormalCube.Refill.Spawns));

            foreach (var coordinate in resultA.Tap.NormalCube.Refill.Spawns.Select(spawn => spawn.Coordinate))
            {
                var cubeA = (CubeItemModel)sessionA.Board.GetCell(coordinate).Item;
                var cubeB = (CubeItemModel)sessionB.Board.GetCell(coordinate).Item;
                Assert.That(cubeA.Color, Is.EqualTo(cubeB.Color), $"Expected matching refill color at {coordinate}.");
            }
        }

        [Test]
        public void CreateNormalizesAuthoredChalicePartsIntoSharedRuntimeObstacle()
        {
            var parser = new LevelJsonParser();
            var factory = new LevelSessionFactory();

            var level = parser.Parse(ReadLevelJson("level_04.json"));
            var session = factory.Create(level);
            var chaliceBox = session.Board.GetCell(new BoardCoordinate(0, 2)).Obstacle;

            Assert.That(chaliceBox, Is.TypeOf<ChaliceBoxObstacleModel>());
            Assert.That(session.Board.GetCell(new BoardCoordinate(1, 3)).Obstacle, Is.SameAs(chaliceBox));
        }

        [Test]
        public void CreateCarriesMultipleGoalDefinitionsFromRealLevelData()
        {
            var parser = new LevelJsonParser();
            var factory = new LevelSessionFactory();

            var level = parser.Parse(ReadLevelJson("level_07.json"));
            var session = factory.Create(level);

            Assert.That(session.Goals, Has.Count.EqualTo(3));
            Assert.That(session.Goals[0].GoalType, Is.EqualTo(LevelGoalType.Stone));
            Assert.That(session.Goals[0].InitialCount, Is.EqualTo(12));
            Assert.That(session.Goals[1].GoalType, Is.EqualTo(LevelGoalType.Vase));
            Assert.That(session.Goals[1].InitialCount, Is.EqualTo(4));
            Assert.That(session.Goals[2].GoalType, Is.EqualTo(LevelGoalType.ChaliceBox));
            Assert.That(session.Goals[2].InitialCount, Is.EqualTo(20));
        }

        [Test]
        public void LevelOneTapDoesNotFillStoneCellsWithItems()
        {
            var parser = new LevelJsonParser();
            var factory = new LevelSessionFactory();
            var goalEvaluator = new LevelGoalProgressEvaluator();

            var level = parser.Parse(ReadLevelJson("level_01.json"));
            var session = factory.Create(level);

            var result = session.ProcessTap(new BoardCoordinate(0, 2));
            var goalProgress = goalEvaluator.Evaluate(session.Board, session.Goals);

            Assert.That(result.Tap.NormalCube.IsValidTap, Is.True);

            for (var y = 0; y <= 1; y++)
            {
                for (var x = 0; x < session.Board.Width; x++)
                {
                    var coordinate = new BoardCoordinate(x, y);
                    var cell = session.Board.GetCell(coordinate);
                    Assert.That(cell.Obstacle, Is.TypeOf<StoneObstacleModel>(), $"Expected stone to remain at {coordinate}.");
                    Assert.That(cell.Item, Is.Null, $"Expected obstacle cell {coordinate} to stay empty of items.");
                }
            }

            Assert.That(goalProgress, Has.Count.EqualTo(1));
            Assert.That(goalProgress[0].GoalType, Is.EqualTo(LevelGoalType.Stone));
            Assert.That(goalProgress[0].RemainingCount, Is.EqualTo(20));
        }

        private static string ReadLevelJson(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "GameContent", "Levels", fileName);
            return File.ReadAllText(path);
        }
    }
}
