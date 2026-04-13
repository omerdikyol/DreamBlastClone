using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamBlastClone.Controllers;
using DreamBlastClone.Controllers.Unity;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelSceneHudControllerTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] is not null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void RefreshHudShowsInitialMovesAndAllGoalTypes()
        {
            var board = new BoardModel(3, 3);
            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());
            board.PlaceObstacle(new BoardCoordinate(1, 0), new VaseObstacleModel());
            var session = new LevelSession(
                board,
                remainingMoves: 7,
                new FixedRefillCubeColorResolver(),
                new[]
                {
                    new LevelGoalDefinition(LevelGoalType.Stone, 1),
                    new LevelGoalDefinition(LevelGoalType.Vase, 1),
                    new LevelGoalDefinition(LevelGoalType.ChaliceBox, 10)
                });

            var controller = CreateConfiguredHudController(session, out var moveLabel, out var goalContainer, out var goalTemplate);
            SetField(controller, "stoneGoalIcon", CreateSprite());
            SetField(controller, "vaseGoalIcon", CreateSprite());
            SetField(controller, "chaliceGoalIcon", CreateSprite());

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            Assert.That(moveLabel.text, Is.EqualTo("07"));
            Assert.That(goalTemplate.gameObject.activeSelf, Is.False);

            var goalItems = GetActiveGoalItems(goalContainer).ToArray();
            Assert.That(goalItems, Has.Length.EqualTo(3));
            Assert.That(GetCountText(goalItems[0]), Is.EqualTo("01"));
            Assert.That(GetCountText(goalItems[1]), Is.EqualTo("01"));
            Assert.That(GetCountText(goalItems[2]), Is.EqualTo(string.Empty));
            Assert.That(GetTitleText(goalItems[2]), Is.EqualTo("Chalices"));
            Assert.That(goalItems[2].Find("Icon").GetComponent<Image>().color.a, Is.EqualTo(1f).Within(0.01f));
            Assert.That(GetCheckImage(goalItems[2]).enabled, Is.True);

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void HandleTapProcessedRefreshesMovesAndGoalCountsAfterValidTap()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new StoneObstacleModel());

            var session = new LevelSession(
                board,
                remainingMoves: 3,
                new FixedRefillCubeColorResolver(),
                new[]
                {
                    new LevelGoalDefinition(LevelGoalType.Stone, 1)
                });

            var controller = CreateConfiguredHudController(session, out var moveLabel, out var goalContainer, out _);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            Assert.That(moveLabel.text, Is.EqualTo("03"));
            Assert.That(GetCountText(GetActiveGoalItems(goalContainer).Single()), Is.EqualTo("01"));

            var tapResult = session.ProcessTap(new BoardCoordinate(0, 0));
            InvokeMethod(controller, "HandleTapProcessed", tapResult);

            Assert.That(moveLabel.text, Is.EqualTo("02"));
            var completedGoalItem = GetActiveGoalItems(goalContainer).Single();
            Assert.That(GetCountText(completedGoalItem), Is.EqualTo(string.Empty));
            var checkImage = GetCheckImage(completedGoalItem);
            Assert.That(checkImage.enabled, Is.True);
            Assert.That(checkImage.transform.localScale.x, Is.GreaterThan(1f));

            InvokeMethod(completedGoalItem.GetComponent<LevelGoalItemView>(), "Update");
            InvokeMethod(completedGoalItem.GetComponent<LevelGoalItemView>(), "AdvanceCompletedCheckAnimation", 0.18f);

            Assert.That(checkImage.transform.localScale.x, Is.EqualTo(1f).Within(0.001f));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void GoalCompletionAudioPlaysOnIncompleteToCompleteTransitionOnly()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new StoneObstacleModel());

            var session = new LevelSession(
                board,
                remainingMoves: 3,
                new FixedRefillCubeColorResolver(),
                new[]
                {
                    new LevelGoalDefinition(LevelGoalType.Stone, 1)
                });

            var controller = CreateConfiguredHudController(session, out _, out _, out _);
            var audioController = (TestRecordingGameAudioController)GetField(controller, "audioController");

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            Assert.That(audioController.PlayedSfx, Is.Empty);

            var tapResult = session.ProcessTap(new BoardCoordinate(0, 0));
            InvokeMethod(controller, "HandleTapProcessed", tapResult);
            Assert.That(audioController.PlayedSfx, Is.EqualTo(new[] { GameSfxCue.GoalCompletion }));

            var invalidTap = session.ProcessTap(new BoardCoordinate(0, 0));
            InvokeMethod(controller, "HandleTapProcessed", invalidTap);
            Assert.That(audioController.PlayedSfx, Is.EqualTo(new[] { GameSfxCue.GoalCompletion }));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void GoalCompletionHapticPlaysOnIncompleteToCompleteTransitionOnly()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new StoneObstacleModel());

            var session = new LevelSession(
                board,
                remainingMoves: 3,
                new FixedRefillCubeColorResolver(),
                new[]
                {
                    new LevelGoalDefinition(LevelGoalType.Stone, 1)
                });

            var controller = CreateConfiguredHudController(session, out _, out _, out _);
            var hapticsController = (TestRecordingGameHapticsController)GetField(controller, "hapticsController");

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var tapResult = session.ProcessTap(new BoardCoordinate(0, 0));
            InvokeMethod(controller, "HandleTapProcessed", tapResult);
            Assert.That(hapticsController.PlayedCues, Is.EqualTo(new[] { GameHapticCue.Medium }));

            var invalidTap = session.ProcessTap(new BoardCoordinate(0, 0));
            InvokeMethod(controller, "HandleTapProcessed", invalidTap);
            Assert.That(hapticsController.PlayedCues, Is.EqualTo(new[] { GameHapticCue.Medium }));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void RefreshHudShowsRemainingChalicesInsteadOfBoxCount()
        {
            var board = new BoardModel(2, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(0, 0),
                remainingDoorDurability: 0,
                requiredChaliceCount: 10,
                collectedChaliceCount: 4);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var session = new LevelSession(
                board,
                remainingMoves: 7,
                new FixedRefillCubeColorResolver(),
                new[]
                {
                    new LevelGoalDefinition(LevelGoalType.ChaliceBox, 10)
                });

            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out _);
            SetField(controller, "chaliceGoalIcon", CreateSprite());

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var goalItem = GetActiveGoalItems(goalContainer).Single();
            Assert.That(GetTitleText(goalItem), Is.EqualTo("Chalices"));
            Assert.That(GetCountText(goalItem), Is.EqualTo("06"));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void RefreshHudShowsAllGoalsForRealMultiGoalLevel()
        {
            var session = CreateSessionFromLevel("level_07.json");
            var controller = CreateConfiguredHudController(session, out var moveLabel, out var goalContainer, out _);
            SetField(controller, "stoneGoalIcon", CreateSprite());
            SetField(controller, "vaseGoalIcon", CreateSprite());
            SetField(controller, "chaliceGoalIcon", CreateSprite());

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            Assert.That(moveLabel.text, Is.EqualTo("24"));

            var goalItems = GetActiveGoalItems(goalContainer).ToArray();
            Assert.That(goalItems, Has.Length.EqualTo(3));
            Assert.That(GetCountText(goalItems[0]), Is.EqualTo("12"));
            Assert.That(GetCountText(goalItems[1]), Is.EqualTo("04"));
            Assert.That(GetCountText(goalItems[2]), Is.EqualTo("20"));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void RefreshHudShowsNormalizedChaliceTotalsForRealChaliceOnlyLevels()
        {
            Assert.That(GetDisplayedGoalCountsForLevel("level_03.json"), Is.EqualTo(new[] { "40" }));
            Assert.That(GetDisplayedGoalCountsForLevel("level_09.json"), Is.EqualTo(new[] { "40" }));
        }

        [Test]
        public void RefreshHudShowsAllGoalsForRealTwoGoalLevel()
        {
            Assert.That(GetDisplayedGoalCountsForLevel("level_08.json"), Is.EqualTo(new[] { "17", "05" }));
        }

        [Test]
        public void LayoutPlacesTwoGoalsOnSingleHorizontalLine()
        {
            var session = CreateSessionWithGoals(
                new LevelGoalDefinition(LevelGoalType.Stone, 1),
                new LevelGoalDefinition(LevelGoalType.Vase, 1));
            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out _);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var goalItems = GetActiveGoalItems(goalContainer).ToArray();
            Assert.That(goalItems, Has.Length.EqualTo(2));
            Assert.That(goalItems[0].anchoredPosition.y, Is.EqualTo(goalItems[1].anchoredPosition.y).Within(0.01f));
            Assert.That(goalItems[0].anchoredPosition.x, Is.LessThan(goalItems[1].anchoredPosition.x));
            Assert.That(goalItems[0].localScale.x, Is.LessThan(1f));
            Assert.That(goalItems[1].localScale.x, Is.LessThan(1f));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void LayoutExpandsSpacingWhenGoalTemplateScaleIsLarger()
        {
            var session = CreateSessionWithGoals(
                new LevelGoalDefinition(LevelGoalType.Stone, 1),
                new LevelGoalDefinition(LevelGoalType.Vase, 1));
            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out var goalTemplate);
            goalTemplate.localScale = new Vector3(2f, 2f, 2f);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var goalItems = GetActiveGoalItems(goalContainer).OrderBy(item => item.anchoredPosition.x).ToArray();
            Assert.That(goalItems, Has.Length.EqualTo(2));
            Assert.That(goalItems[1].anchoredPosition.x - goalItems[0].anchoredPosition.x, Is.GreaterThan(70f));
            Assert.That(goalItems.All(item => item.localScale.x > 1f), Is.True);

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void LayoutPlacesThreeGoalsAsTriangle()
        {
            var session = CreateSessionWithGoals(
                new LevelGoalDefinition(LevelGoalType.Stone, 1),
                new LevelGoalDefinition(LevelGoalType.Vase, 1),
                new LevelGoalDefinition(LevelGoalType.ChaliceBox, 10));
            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out _);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var goalItems = GetActiveGoalItems(goalContainer)
                .OrderByDescending(item => item.anchoredPosition.y)
                .ToArray();

            Assert.That(goalItems, Has.Length.EqualTo(3));
            Assert.That(goalItems[0].anchoredPosition.x, Is.EqualTo(10f).Within(0.01f));
            Assert.That(goalItems[1].anchoredPosition.y, Is.EqualTo(goalItems[2].anchoredPosition.y).Within(0.01f));
            Assert.That(goalItems[1].anchoredPosition.x, Is.LessThan(goalItems[2].anchoredPosition.x));
            Assert.That(goalItems[0].localScale.x, Is.LessThan(1f));

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void LayoutPlacesFourGoalsAsSquare()
        {
            var session = CreateSessionWithGoals(
                new LevelGoalDefinition(LevelGoalType.Stone, 1),
                new LevelGoalDefinition(LevelGoalType.Vase, 1),
                new LevelGoalDefinition(LevelGoalType.ChaliceBox, 10),
                new LevelGoalDefinition(LevelGoalType.Stone, 1));
            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out _);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var goalItems = GetActiveGoalItems(goalContainer).ToArray();
            var distinctX = goalItems.Select(item => Mathf.Round(item.anchoredPosition.x * 100f) / 100f).Distinct().Count();
            var distinctY = goalItems.Select(item => Mathf.Round(item.anchoredPosition.y * 100f) / 100f).Distinct().Count();

            Assert.That(goalItems, Has.Length.EqualTo(4));
            Assert.That(distinctX, Is.EqualTo(2));
            Assert.That(distinctY, Is.EqualTo(2));
            Assert.That(goalItems.All(item => item.localScale.x < 1f), Is.True);

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void LayoutFallsBackToCompactGridForFiveGoals()
        {
            var session = CreateSessionWithGoals(
                new LevelGoalDefinition(LevelGoalType.Stone, 1),
                new LevelGoalDefinition(LevelGoalType.Vase, 1),
                new LevelGoalDefinition(LevelGoalType.ChaliceBox, 10),
                new LevelGoalDefinition(LevelGoalType.Stone, 1),
                new LevelGoalDefinition(LevelGoalType.Vase, 1));
            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out _);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var goalItems = GetActiveGoalItems(goalContainer).ToArray();

            Assert.That(goalItems, Has.Length.EqualTo(5));
            Assert.That(goalItems.All(item => item.localScale.x < 0.55f), Is.True);
            Assert.That(goalItems.All(item => Mathf.Abs(item.anchoredPosition.x - 10f) <= 28f), Is.True);
            Assert.That(goalItems.All(item => Mathf.Abs(item.anchoredPosition.y) <= 28f), Is.True);

            InvokeMethod(controller, "OnDisable");
        }

        [Test]
        public void HandleTapProcessedLeavesHudUnchangedAfterInvalidTap()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(1, 1), new StoneObstacleModel());

            var session = new LevelSession(
                board,
                remainingMoves: 3,
                new FixedRefillCubeColorResolver(),
                new[]
                {
                    new LevelGoalDefinition(LevelGoalType.Stone, 1)
                });

            var controller = CreateConfiguredHudController(session, out var moveLabel, out var goalContainer, out _);

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            Assert.That(moveLabel.text, Is.EqualTo("03"));
            Assert.That(GetCountText(GetActiveGoalItems(goalContainer).Single()), Is.EqualTo("01"));

            var tapResult = session.ProcessTap(new BoardCoordinate(0, 0));
            Assert.That(tapResult.Tap.IsValidTap, Is.False);
            Assert.That(tapResult.DidSpendMove, Is.False);
            InvokeMethod(controller, "HandleTapProcessed", tapResult);

            Assert.That(moveLabel.text, Is.EqualTo("03"));
            Assert.That(GetCountText(GetActiveGoalItems(goalContainer).Single()), Is.EqualTo("01"));

            InvokeMethod(controller, "OnDisable");
        }

        private LevelSceneHudController CreateConfiguredHudController(
            LevelSession session,
            out Text moveLabel,
            out RectTransform goalContainer,
            out RectTransform goalTemplate)
        {
            var runtime = CreateGameObject("Runtime");
            var host = runtime.AddComponent<LevelSessionHost>();
            host.SetSession(session);
            var bridge = runtime.AddComponent<BoardInputSessionBridge>();
            var audioController = runtime.AddComponent<TestRecordingGameAudioController>();
            var hapticsController = runtime.AddComponent<TestRecordingGameHapticsController>();
            var controller = runtime.AddComponent<LevelSceneHudController>();

            moveLabel = CreateText("MoveLabel");
            goalContainer = CreateRectTransform("GoalContainer");
            goalTemplate = CreateGoalTemplate(goalContainer);

            SetField(controller, "sessionHost", host);
            SetField(controller, "inputBridge", bridge);
            SetField(controller, "audioController", audioController);
            SetField(controller, "hapticsController", hapticsController);
            SetField(controller, "moveCountLabel", moveLabel);
            SetField(controller, "goalItemTemplate", goalTemplate);
            SetField(controller, "goalItemsContainer", goalContainer);
            SetField(controller, "multiGoalLayoutSize", 56f);
            return controller;
        }

        private string[] GetDisplayedGoalCountsForLevel(string levelFileName)
        {
            var session = CreateSessionFromLevel(levelFileName);
            var controller = CreateConfiguredHudController(session, out _, out var goalContainer, out _);
            SetField(controller, "stoneGoalIcon", CreateSprite());
            SetField(controller, "vaseGoalIcon", CreateSprite());
            SetField(controller, "chaliceGoalIcon", CreateSprite());

            InvokeMethod(controller, "OnEnable");
            InvokeMethod(controller, "Start");

            var counts = GetActiveGoalItems(goalContainer)
                .Select(GetCountText)
                .ToArray();

            InvokeMethod(controller, "OnDisable");
            return counts;
        }

        private static LevelSession CreateSessionFromLevel(string levelFileName)
        {
            var parser = new LevelJsonParser();
            var factory = new LevelSessionFactory();
            var level = parser.Parse(ReadLevelJson(levelFileName));
            return factory.Create(level);
        }

        private static LevelSession CreateSessionWithGoals(params LevelGoalDefinition[] goals)
        {
            var board = new BoardModel(2, 2);
            return new LevelSession(board, remainingMoves: 7, new FixedRefillCubeColorResolver(), goals);
        }

        private RectTransform CreateGoalTemplate(Transform parent)
        {
            var root = new GameObject("GoalTemplate", typeof(RectTransform), typeof(LevelGoalItemView));
            createdObjects.Add(root);
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = new Vector2(10f, 0f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            createdObjects.Add(icon);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.SetParent(root.transform, false);

            var count = new GameObject("Count", typeof(RectTransform), typeof(Text));
            createdObjects.Add(count);
            var countRect = count.GetComponent<RectTransform>();
            countRect.SetParent(root.transform, false);
            var countCheck = new GameObject("CountCheck", typeof(RectTransform), typeof(Image));
            createdObjects.Add(countCheck);
            var countCheckRect = countCheck.GetComponent<RectTransform>();
            countCheckRect.SetParent(root.transform, false);

            var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            createdObjects.Add(title);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.SetParent(root.transform, false);

            var countText = count.GetComponent<Text>();
            countText.text = "00";
            var countCheckImage = countCheck.GetComponent<Image>();
            countCheckImage.enabled = false;
            var titleText = title.GetComponent<Text>();
            titleText.text = string.Empty;

            var goalItemView = root.GetComponent<LevelGoalItemView>();
            SetField(goalItemView, "iconTarget", icon.GetComponent<Image>());
            SetField(goalItemView, "titleLabel", titleText);
            SetField(goalItemView, "countLabel", countText);
            SetField(goalItemView, "completedCountTarget", countCheckImage);
            SetField(goalItemView, "completedCountSprite", CreateSprite());
            SetField(goalItemView, "activeColor", Color.white);
            return rect;
        }

        private Text CreateText(string name)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            createdObjects.Add(textObject);
            return textObject.GetComponent<Text>();
        }

        private RectTransform CreateRectTransform(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            createdObjects.Add(gameObject);
            return gameObject.GetComponent<RectTransform>();
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(1, 1);
            createdObjects.Add(texture);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            createdObjects.Add(sprite);
            return sprite;
        }

        private static IEnumerable<RectTransform> GetActiveGoalItems(RectTransform container)
        {
            return container.Cast<Transform>()
                .Select(child => child as RectTransform)
                .Where(child => child is not null && child.gameObject.activeSelf);
        }

        private static string GetCountText(RectTransform goalItem)
        {
            return goalItem.Find("Count").GetComponent<Text>().text;
        }

        private static Image GetCheckImage(RectTransform goalItem)
        {
            return goalItem.Find("CountCheck").GetComponent<Image>();
        }

        private static string GetTitleText(RectTransform goalItem)
        {
            return goalItem.Find("Title").GetComponent<Text>().text;
        }

        private static string ReadLevelJson(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "GameContent", "Levels", fileName);
            return File.ReadAllText(path);
        }

        private static void InvokeMethod(object target, string methodName, params object[] parameters)
        {
            var method = FindMethod(target.GetType(), methodName, parameters.Length);
            method.Invoke(target, parameters);
        }

        private static MethodInfo FindMethod(Type type, string methodName, int parameterCount)
        {
            while (type is not null)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (var method in methods)
                {
                    if (method.Name == methodName && method.GetParameters().Length == parameterCount)
                    {
                        return method;
                    }
                }

                type = type.BaseType;
            }

            throw new MissingMethodException(methodName);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            var field = FindField(target.GetType(), fieldName);
            return field.GetValue(target);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type is not null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (field is not null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(fieldName);
        }

        private sealed class FixedRefillCubeColorResolver : IRefillCubeColorResolver
        {
            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                return CubeColor.Blue;
            }
        }
    }
}
