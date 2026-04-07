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
        private readonly List<Sprite> createdSprites = new List<Sprite>();
        private readonly List<Texture2D> createdTextures = new List<Texture2D>();

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(CurrentLevelPlayerPrefsKey);
            PlayerPrefs.Save();

            for (var index = createdSprites.Count - 1; index >= 0; index--)
            {
                if (createdSprites[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdSprites[index]);
                }
            }

            createdSprites.Clear();

            for (var index = createdTextures.Count - 1; index >= 0; index--)
            {
                if (createdTextures[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdTextures[index]);
                }
            }

            createdTextures.Clear();

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
            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(0));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));

            AdvancePendingPreview(bridge, 0.12f);

            Assert.That(GetItemRoot(boardView).childCount, Is.GreaterThan(0));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.EqualTo(0f));
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
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));
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
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.EqualTo(0f));
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
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.EqualTo(0f));
        }

        [Test]
        public void TryHandleScreenTapNearBoundarySnapsToStrongerAdjacentCubeGroup()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Blue));

            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var worldPoint = boardView.transform.TransformPoint(new Vector3(0.95f, 0.5f, 0f));
            var screenPosition = GetCamera(bridge).WorldToScreenPoint(worldPoint);

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(2));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(0, 0)).Item).Color, Is.EqualTo(CubeColor.Green));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(1, 0)).Item).Color, Is.EqualTo(CubeColor.Yellow));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(2, 0)).Item).Color, Is.EqualTo(CubeColor.Yellow));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));
        }

        [Test]
        public void TryHandleScreenTapAtSceneCellScaleSnapsToNearbyValidGroupWhenRawCubeIsInvalid()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Blue));

            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session, boardViewOrigin: Vector2.zero, boardViewCellSize: 0.5f);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var worldPoint = boardView.transform.TransformPoint(new Vector3(0.25f, 0.42f, 0f));
            var screenPosition = GetCamera(bridge).WorldToScreenPoint(worldPoint);

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(2));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(0, 0)).Item).Color, Is.EqualTo(CubeColor.Green));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(0, 1)).Item).Color, Is.EqualTo(CubeColor.Yellow));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(1, 1)).Item).Color, Is.EqualTo(CubeColor.Yellow));
        }

        [Test]
        public void TryHandleScreenTapAtIsolatedCubeCenterDoesNotSnapToNeighborGroup()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Blue));

            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var worldPoint = boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
            var screenPosition = GetCamera(bridge).WorldToScreenPoint(worldPoint);

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(3));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(0, 0)).Item).Color, Is.EqualTo(CubeColor.Green));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(1, 0)).Item).Color, Is.EqualTo(CubeColor.Blue));
            Assert.That(((CubeItemModel)session.Board.GetCell(new BoardCoordinate(2, 0)).Item).Color, Is.EqualTo(CubeColor.Blue));
        }

        [Test]
        public void TryHandleScreenTapIgnoresFurtherTapsWhileNormalPreviewIsActive()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Blue));

            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var firstTap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
            var secondTap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(firstTap), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(2));
            Assert.That(bridge.TryHandleScreenTap(secondTap), Is.False);
            Assert.That(session.RemainingMoves, Is.EqualTo(2));
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
            SetField(boardView, "cubePrefab", CreateCubePrefab());
            SetField(boardView, "horizontalRocketPrefab", CreateVisualPrefab("HorizontalRocketPrefab"));
            SetField(boardView, "verticalRocketPrefab", CreateVisualPrefab("VerticalRocketPrefab"));
            SetField(boardView, "tntPrefab", CreateVisualPrefab("TntPrefab"));
            SetField(boardView, "vasePrefab", CreateVisualPrefab("VasePrefab"));
            SetField(boardView, "stonePrefab", CreateVisualPrefab("StonePrefab"));
            SetField(boardView, "chaliceBoxPrefab", CreateVisualPrefab("ChaliceBoxPrefab"));
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

        private GameObject CreateCubePrefab()
        {
            var prefab = CreateVisualPrefab("CubePrefab");
            var cubeView = prefab.AddComponent<CubeItemView>();
            var spriteRenderer = prefab.GetComponent<SpriteRenderer>();

            SetField(cubeView, "spriteRenderer", spriteRenderer);
            SetField(cubeView, "redDefaultSprite", CreateSprite(14, 16, 14f));
            SetField(cubeView, "greenDefaultSprite", CreateSprite(15, 16, 15f));
            SetField(cubeView, "blueDefaultSprite", CreateSprite(16, 16, 16f));
            SetField(cubeView, "yellowDefaultSprite", CreateSprite(17, 16, 17f));
            SetField(cubeView, "redRocketSprite", CreateSprite(18, 16, 18f));
            SetField(cubeView, "greenRocketSprite", CreateSprite(19, 16, 19f));
            SetField(cubeView, "blueRocketSprite", CreateSprite(20, 16, 20f));
            SetField(cubeView, "yellowRocketSprite", CreateSprite(21, 16, 21f));
            SetField(cubeView, "redTntSprite", CreateSprite(22, 16, 22f));
            SetField(cubeView, "greenTntSprite", CreateSprite(23, 16, 23f));
            SetField(cubeView, "blueTntSprite", CreateSprite(24, 16, 24f));
            SetField(cubeView, "yellowTntSprite", CreateSprite(25, 16, 25f));
            return prefab;
        }

        private Sprite CreateSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            createdTextures.Add(texture);
            createdSprites.Add(sprite);
            return sprite;
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

        private static void AdvancePendingPreview(BoardInputSessionBridge bridge, float deltaTime)
        {
            var method = bridge.GetType().GetMethod("AdvancePendingPreview", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(bridge, new object[] { deltaTime });
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

        private static float GetRemainingPreviewSeconds(BoardInputSessionBridge bridge)
        {
            return (float)GetField(bridge, "remainingPreviewSeconds");
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
