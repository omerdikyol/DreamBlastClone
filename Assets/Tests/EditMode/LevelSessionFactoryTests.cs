using System.IO;
using DreamBlastClone.Controllers;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
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

        private static string ReadLevelJson(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "GameContent", "Levels", fileName);
            return File.ReadAllText(path);
        }
    }
}
