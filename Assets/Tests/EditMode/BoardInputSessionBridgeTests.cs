using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DreamBlastClone.Controllers;
using DreamBlastClone.Controllers.Unity;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardInputSessionBridgeTests
    {
        private const string CurrentLevelPlayerPrefsKey = "DreamBlastClone.CurrentLevel";
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();
        private readonly List<ScriptableObject> createdScriptableObjects = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(CurrentLevelPlayerPrefsKey);
            PlayerPrefs.Save();

            for (var index = createdScriptableObjects.Count - 1; index >= 0; index--)
            {
                if (createdScriptableObjects[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdScriptableObjects[index]);
                }
            }

            createdScriptableObjects.Clear();

            for (var index = createdGameObjects.Count - 1; index >= 0; index--)
            {
                var gameObject = createdGameObjects[index];

                if (gameObject is not null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }

            createdGameObjects.Clear();
        }

        [Test]
        public void LevelSessionHostSetSessionNullThrows()
        {
            var host = CreateGameObject("SessionHost").AddComponent<LevelSessionHost>();

            Assert.That(() => host.SetSession(null), Throws.ArgumentNullException);
        }

        [Test]
        public void LevelSceneSessionBootstrapLoadsPersistedLevelIntoHost()
        {
            var hostObject = CreateGameObject("SessionHost");
            var host = hostObject.AddComponent<LevelSessionHost>();
            var bootstrap = hostObject.AddComponent<LevelSceneSessionBootstrap>();
            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            createdScriptableObjects.Add(catalog);

            PlayerPrefs.SetInt(CurrentLevelPlayerPrefsKey, 4);
            PlayerPrefs.Save();

            SetField(catalog, "levelJsonFiles", new[]
            {
                new TextAsset(ReadLevelJson("level_01.json")),
                new TextAsset(ReadLevelJson("level_02.json")),
                new TextAsset(ReadLevelJson("level_03.json")),
                new TextAsset(ReadLevelJson("level_04.json"))
            });

            SetField(bootstrap, "sessionHost", host);
            SetField(bootstrap, "levelCatalog", catalog);

            Assert.That(bootstrap.InitializeSession(), Is.True);

            Assert.That(host.Session, Is.Not.Null);
            Assert.That(host.Session.RemainingMoves, Is.EqualTo(17));
            Assert.That(host.Session.Board.Width, Is.EqualTo(7));
            Assert.That(host.Session.Board.Height, Is.EqualTo(8));
            Assert.That(host.Session.Board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.TypeOf<StoneObstacleModel>());
            Assert.That(host.Session.Board.GetCell(new BoardCoordinate(4, 2)).Obstacle, Is.TypeOf<VaseObstacleModel>());
            Assert.That(host.Session.Board.GetCell(new BoardCoordinate(0, 2)).Obstacle, Is.TypeOf<ChaliceBoxObstacleModel>());
            Assert.That(host.Session.Board.GetCell(new BoardCoordinate(0, 2)).Obstacle, Is.SameAs(host.Session.Board.GetCell(new BoardCoordinate(1, 3)).Obstacle));
        }

        [Test]
        public void TryHandleScreenTapReturnsFalseWhenRequiredReferencesAreMissing()
        {
            var bridge = CreateGameObject("Bridge").AddComponent<BoardInputSessionBridge>();

            Assert.That(bridge.TryHandleScreenTap(Vector2.zero), Is.False);
        }

        [Test]
        public void TryHandleScreenTapReturnsFalseOutsideBoardBounds()
        {
            var board = new BoardModel(2, 2);
            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session, boardViewOrigin: Vector2.zero, boardViewCellSize: 1f, cameraPosition: new Vector3(0f, 0f, -10f));

            var outsideScreen = GetCamera(bridge).WorldToScreenPoint(new Vector3(-1f, -1f, 0f));

            Assert.That(bridge.TryHandleScreenTap(outsideScreen), Is.False);
            Assert.That(session.RemainingMoves, Is.EqualTo(3));
        }

        [Test]
        public void TryHandleScreenTapForwardsValidNormalTapIntoSessionAndRerendersBoard()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 4, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");
            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(2));

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(1.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(3));
            Assert.That(GetItemRoot(boardView).childCount, Is.GreaterThan(0));
        }

        [Test]
        public void TryHandleScreenTapRaisesTapProcessedAfterForwardingTap()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 4, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);
            LevelSessionTapResult capturedResult = null;
            bridge.TapProcessed += result => capturedResult = result;

            InvokeMethod(bridge, "Start");
            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(capturedResult, Is.Not.Null);
            Assert.That(capturedResult.DidSpendMove, Is.True);
            Assert.That(capturedResult.Tap.RouteType, Is.EqualTo(TapRouteType.NormalCube));
        }

        [Test]
        public void TryHandleScreenTapForwardsValidSpecialTapIntoSession()
        {
            var board = new BoardModel(4, 4);
            board.PlaceItem(new BoardCoordinate(2, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Blue));

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 1.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
            Assert.That(session.Board.GetCell(new BoardCoordinate(2, 1)).HasItem, Is.False);
        }

        [Test]
        public void TryHandleScreenTapWithinBoardStillRerendersSafelyForNoOpGameplayTap()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 2, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 2.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(2));
            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(1));
        }

        private BoardInputSessionBridge CreateConfiguredBridge(
            LevelSession session,
            Vector2? boardViewOrigin = null,
            float boardViewCellSize = 1f,
            Vector3? cameraPosition = null)
        {
            var boardView = CreateConfiguredBoardView(origin: boardViewOrigin ?? Vector2.zero, cellSize: boardViewCellSize);
            var host = CreateGameObject("SessionHost").AddComponent<LevelSessionHost>();
            host.SetSession(session);

            var cameraObject = CreateGameObject("InputCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = cameraPosition ?? new Vector3(0f, 0f, -10f);

            var bridgeObject = CreateGameObject("Bridge");
            var bridge = bridgeObject.AddComponent<BoardInputSessionBridge>();
            SetField(bridge, "boardView", boardView);
            SetField(bridge, "sessionHost", host);
            SetField(bridge, "inputCamera", camera);
            return bridge;
        }

        private BoardView CreateConfiguredBoardView(Vector2 origin, float cellSize)
        {
            var host = CreateGameObject("BoardViewHost");
            var itemRoot = CreateGameObject("ItemRoot").transform;
            var obstacleRoot = CreateGameObject("ObstacleRoot").transform;

            itemRoot.SetParent(host.transform, false);
            obstacleRoot.SetParent(host.transform, false);

            var boardView = host.AddComponent<BoardView>();
            SetField(boardView, "itemVisualRoot", itemRoot);
            SetField(boardView, "obstacleVisualRoot", obstacleRoot);
            SetField(boardView, "origin", origin);
            SetField(boardView, "cellSize", cellSize);
            SetField(boardView, "itemZ", -0.1f);
            SetField(boardView, "obstacleZ", 0f);
            SetField(boardView, "cubePrefab", CreateVisualPrefab("CubePrefab"));
            SetField(boardView, "horizontalRocketPrefab", CreateVisualPrefab("HorizontalRocketPrefab"));
            SetField(boardView, "verticalRocketPrefab", CreateVisualPrefab("VerticalRocketPrefab"));
            SetField(boardView, "tntPrefab", CreateVisualPrefab("TntPrefab"));
            SetField(boardView, "vasePrefab", CreateVisualPrefab("VasePrefab"));
            SetField(boardView, "stonePrefab", CreateVisualPrefab("StonePrefab"));
            SetField(boardView, "chaliceBoxPrefab", CreateVisualPrefab("ChaliceBoxPrefab"));
            SetField(boardView, "redCubeColor", Color.red);
            SetField(boardView, "greenCubeColor", Color.green);
            SetField(boardView, "blueCubeColor", Color.blue);
            SetField(boardView, "yellowCubeColor", Color.yellow);
            return boardView;
        }

        private Transform GetItemRoot(BoardView boardView)
        {
            return (Transform)GetField(boardView, "itemVisualRoot");
        }

        private BoardView GetBoardView(BoardInputSessionBridge bridge)
        {
            return (BoardView)GetField(bridge, "boardView");
        }

        private Camera GetCamera(BoardInputSessionBridge bridge)
        {
            return (Camera)GetField(bridge, "inputCamera");
        }

        private GameObject CreateVisualPrefab(string name)
        {
            var prefab = CreateGameObject(name);
            prefab.AddComponent<SpriteRenderer>();
            return prefab;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(target);
        }

        private static string ReadLevelJson(string fileName)
        {
            var path = Path.Combine(Application.dataPath, "GameContent", "Levels", fileName);
            return File.ReadAllText(path);
        }

        private sealed class TestRefillCubeColorResolver : IRefillCubeColorResolver
        {
            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                return CubeColor.Yellow;
            }
        }
    }
}
