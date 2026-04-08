using System;
using System.Collections.Generic;
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
            Assert.That(GetCountText(goalItems[2]), Is.EqualTo("00"));
            Assert.That(GetTitleText(goalItems[2]), Is.EqualTo("Chalices"));
            Assert.That(goalItems[2].Find("Count").GetComponent<Text>().color.a, Is.EqualTo(0.5f).Within(0.01f));

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
            Assert.That(GetCountText(GetActiveGoalItems(goalContainer).Single()), Is.EqualTo("00"));

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
            var controller = runtime.AddComponent<LevelSceneHudController>();

            moveLabel = CreateText("MoveLabel");
            goalContainer = CreateRectTransform("GoalContainer");
            goalTemplate = CreateGoalTemplate(goalContainer);

            SetField(controller, "sessionHost", host);
            SetField(controller, "inputBridge", bridge);
            SetField(controller, "moveCountLabel", moveLabel);
            SetField(controller, "goalItemTemplate", goalTemplate);
            SetField(controller, "goalItemsContainer", goalContainer);
            SetField(controller, "goalItemSpacing", 120f);
            return controller;
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

            var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            createdObjects.Add(title);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.SetParent(root.transform, false);

            var countText = count.GetComponent<Text>();
            countText.text = "00";
            var titleText = title.GetComponent<Text>();
            titleText.text = string.Empty;

            var goalItemView = root.GetComponent<LevelGoalItemView>();
            SetField(goalItemView, "iconTarget", icon.GetComponent<Image>());
            SetField(goalItemView, "titleLabel", titleText);
            SetField(goalItemView, "countLabel", countText);
            SetField(goalItemView, "activeColor", Color.white);
            SetField(goalItemView, "completedColor", new Color(1f, 1f, 1f, 0.5f));
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

        private static string GetTitleText(RectTransform goalItem)
        {
            return goalItem.Find("Title").GetComponent<Text>().text;
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
