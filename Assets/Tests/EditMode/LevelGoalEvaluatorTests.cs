using System.IO;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelGoalEvaluatorTests
    {
        [Test]
        public void AuthoredLevelsCanContainMultipleSimultaneousGoals()
        {
            var parser = new LevelJsonParser();
            var builder = new LevelGoalDefinitionBuilder();

            var level = parser.Parse(ReadLevelJson("level_04.json"));
            var goals = builder.Build(level);

            Assert.That(goals, Has.Count.EqualTo(3));
            Assert.That(goals[0].GoalType, Is.EqualTo(LevelGoalType.Stone));
            Assert.That(goals[0].InitialCount, Is.EqualTo(14));
            Assert.That(goals[1].GoalType, Is.EqualTo(LevelGoalType.Vase));
            Assert.That(goals[1].InitialCount, Is.EqualTo(4));
            Assert.That(goals[2].GoalType, Is.EqualTo(LevelGoalType.ChaliceBox));
            Assert.That(goals[2].InitialCount, Is.EqualTo(1));
        }

        [Test]
        public void SingleGoalLevelProducesOneGoalDefinition()
        {
            var parser = new LevelJsonParser();
            var builder = new LevelGoalDefinitionBuilder();

            var level = parser.Parse(ReadLevelJson("level_02.json"));
            var goals = builder.Build(level);

            Assert.That(goals, Has.Count.EqualTo(1));
            Assert.That(goals[0].GoalType, Is.EqualTo(LevelGoalType.Vase));
            Assert.That(goals[0].InitialCount, Is.EqualTo(25));
        }

        [Test]
        public void GoalProgressUsesRemainingObstacleInstancesAndKeepsCompletedTypesVisible()
        {
            var board = new BoardModel(4, 4);
            var evaluator = new LevelGoalProgressEvaluator();
            var goals = new[]
            {
                new LevelGoalDefinition(LevelGoalType.Stone, 2),
                new LevelGoalDefinition(LevelGoalType.Vase, 1),
                new LevelGoalDefinition(LevelGoalType.ChaliceBox, 1)
            };
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(0, 2), remainingDoorDurability: 4);

            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var progress = evaluator.Evaluate(board, goals);

            Assert.That(progress, Has.Count.EqualTo(3));
            Assert.That(progress[0].GoalType, Is.EqualTo(LevelGoalType.Stone));
            Assert.That(progress[0].RemainingCount, Is.EqualTo(1));
            Assert.That(progress[0].IsCompleted, Is.False);
            Assert.That(progress[1].GoalType, Is.EqualTo(LevelGoalType.Vase));
            Assert.That(progress[1].RemainingCount, Is.EqualTo(0));
            Assert.That(progress[1].IsCompleted, Is.True);
            Assert.That(progress[2].GoalType, Is.EqualTo(LevelGoalType.ChaliceBox));
            Assert.That(progress[2].RemainingCount, Is.EqualTo(1));
            Assert.That(progress[2].IsCompleted, Is.False);
        }

        [Test]
        public void PartiallyDamagedObstaclesStillCountAsRemainingGoals()
        {
            var board = new BoardModel(3, 3);
            var evaluator = new LevelGoalProgressEvaluator();
            var goals = new[]
            {
                new LevelGoalDefinition(LevelGoalType.Vase, 1),
                new LevelGoalDefinition(LevelGoalType.ChaliceBox, 1)
            };
            var vase = new VaseObstacleModel(remainingDurability: 1);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(1, 1),
                remainingDoorDurability: 1,
                requiredChaliceCount: 10,
                collectedChaliceCount: 9);

            board.PlaceObstacle(new BoardCoordinate(0, 0), vase);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var progress = evaluator.Evaluate(board, goals);

            Assert.That(progress[0].RemainingCount, Is.EqualTo(1));
            Assert.That(progress[1].RemainingCount, Is.EqualTo(1));
        }

        private static string ReadLevelJson(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "GameContent", "Levels", fileName);
            return File.ReadAllText(path);
        }
    }
}
