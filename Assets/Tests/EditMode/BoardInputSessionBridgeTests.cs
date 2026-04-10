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
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetDestructionFeedbackPlayer(bridge).Duration);

            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetItemRoot(boardView).childCount, Is.GreaterThan(0));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.EqualTo(0f));
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);
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
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);
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
            LevelSessionTapResult capturedTapResult = null;

            bridge.TapProcessed += result => capturedTapResult = result;

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 1.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
            Assert.That(capturedTapResult, Is.Not.Null);
            Assert.That(capturedTapResult.Tap.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(capturedTapResult.Tap.SpecialItem.Activation.IsValidActivation, Is.True);
            Assert.That(capturedTapResult.Tap.SpecialItem.Activation.RemovedItemCoordinates, Does.Contain(new BoardCoordinate(2, 1)));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.True);

            AdvancePendingPreview(bridge, GetDestructionFeedbackPlayer(bridge).Duration);

            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.True);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetRemainingPreviewSeconds(bridge), Is.EqualTo(0f));
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);
        }

        [Test]
        public void TryHandleScreenTapForwardsTntTapWithoutRocketPreview()
        {
            var board = new BoardModel(4, 4);
            board.PlaceItem(new BoardCoordinate(1, 1), new TntItemModel());

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(1.5f, 1.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleTntEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetDestructionFeedbackPlayer(bridge).Duration);

            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleTntEffectPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetSingleTntEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.True);
        }

        [Test]
        public void TryHandleScreenTapRunsComboPresentationBetweenDestructionAndSettle()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 2), new RocketItemModel(RocketOrientation.Vertical));

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(1.5f, 1.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
            Assert.That(GetRemainingPreviewSeconds(bridge), Is.GreaterThan(0f));
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSingleTntEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetComboPresentationPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetDestructionFeedbackPlayer(bridge).Duration);

            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetComboPresentationPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetComboPresentationPlayer(bridge).Duration);

            Assert.That(GetComboPresentationPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.True);
        }

        [Test]
        public void TryHandleScreenTapNearAdjacentRocketsSnapsToSpecialCombo()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 2), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);
            LevelSessionTapResult capturedTapResult = null;

            bridge.TapProcessed += result => capturedTapResult = result;

            InvokeMethod(bridge, "Start");

            var worldPoint = boardView.transform.TransformPoint(new Vector3(1.5f, 0.98f, 0f));
            var screenPosition = GetCamera(bridge).WorldToScreenPoint(worldPoint);

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
            Assert.That(capturedTapResult, Is.Not.Null);
            Assert.That(capturedTapResult.Tap.RouteType, Is.EqualTo(TapRouteType.SpecialItem));
            Assert.That(capturedTapResult.Tap.SpecialItem.Combo.IsComboActivated, Is.True);
            Assert.That(capturedTapResult.Tap.SpecialItem.Combo.RemovedItemCoordinates, Does.Contain(new BoardCoordinate(1, 0)));
            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetCubeBlastParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
        }

        [Test]
        public void TryHandleScreenTapStartsVaseParticlesForVaseDamage()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new VaseObstacleModel(remainingDurability: 2));

            var session = new LevelSession(board, 4, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.False);
        }

        [Test]
        public void TryHandleScreenTapStartsVaseParticlesForVaseRemoval()
        {
            var board = new BoardModel(3, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(2, 0), new VaseObstacleModel(remainingDurability: 1));

            var session = new LevelSession(board, 4, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.True);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetVaseParticlePlayer(bridge).IsPlaying, Is.False);
        }

        [Test]
        public void TryHandleScreenTapStartsChaliceBoxParticlesForDoorDamage()
        {
            var board = new BoardModel(4, 2);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var session = new LevelSession(board, 4, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);

            var chaliceVisual = FindChildByPrefix(GetObstacleRoot(boardView), "ChaliceBoxPrefab");
            Assert.That(chaliceVisual.transform.Find("Doors").GetComponent<SpriteRenderer>().enabled, Is.True);
            Assert.That(CountEnabledChaliceSlots(chaliceVisual.transform), Is.EqualTo(0));
        }

        [Test]
        public void TryHandleScreenTapIgnoresFurtherTapsWhileComboPresentationIsActive()
        {
            var board = new BoardModel(3, 3);
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 2), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var comboTap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(1.5f, 1.5f, 0f)));
            var cubeTap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(comboTap), Is.True);

            AdvancePendingPreview(bridge, GetDestructionFeedbackPlayer(bridge).Duration);

            Assert.That(GetComboPresentationPlayer(bridge).IsPlaying, Is.True);
            Assert.That(bridge.TryHandleScreenTap(cubeTap), Is.False);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
        }

        [Test]
        public void TryHandleScreenTapNextToStoneDoesNotProduceStoneRemovalFeedback()
        {
            var board = new BoardModel(2, 2);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(0, 1), new StoneObstacleModel());

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(session.Board.GetCell(new BoardCoordinate(0, 1)).Obstacle, Is.TypeOf<StoneObstacleModel>());

            var feedbackRoot = GetDestructionFeedbackPlayer(bridge).transform;
            Assert.That(feedbackRoot.childCount, Is.EqualTo(2));
            Assert.That(ContainsChildNameWithFragment(feedbackRoot, "StoneObstacleModel"), Is.False);
            Assert.That(GetStoneParticlePlayer(bridge).IsPlaying, Is.False);
        }

        [Test]
        public void TryHandleScreenTapThatRemovesStoneUsesCurrentDestructionFeedbackFlow()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(1, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(1.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);

            var feedbackRoot = GetDestructionFeedbackPlayer(bridge).transform;
            Assert.That(ContainsChildNameWithFragment(feedbackRoot, "StoneObstacleModel"), Is.True);
            Assert.That(GetStoneParticlePlayer(bridge).IsPlaying, Is.True);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));
            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(session.Board.GetCell(new BoardCoordinate(0, 0)).Obstacle, Is.Null);
            Assert.That(GetStoneParticlePlayer(bridge).IsPlaying, Is.False);
        }

        [Test]
        public void TryHandleScreenTapThatBreaksChaliceDoorRendersChalicePhasePreviewWithoutThrowing()
        {
            var board = new BoardModel(4, 3);
            var tapCoordinate = new BoardCoordinate(1, 1);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 1);

            board.PlaceItem(tapCoordinate, new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(
                boardView.transform.TransformPoint(new Vector3(1.5f, 1.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);

            var chaliceVisual = FindChildByPrefix(GetObstacleRoot(boardView), "ChaliceBoxPrefab");
            Assert.That(chaliceVisual.transform.Find("Doors").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(CountEnabledChaliceSlots(chaliceVisual.transform), Is.EqualTo(10));

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.False);
        }

        [Test]
        public void TryHandleScreenTapStartsChaliceBoxParticlesForChalicePhaseDamage()
        {
            var board = new BoardModel(5, 5);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 1), remainingDoorDurability: 0, requiredChaliceCount: 10, collectedChaliceCount: 0);
            board.PlaceItem(new BoardCoordinate(2, 2), new TntItemModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 2.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);

            var chaliceVisual = FindChildByPrefix(GetObstacleRoot(boardView), "ChaliceBoxPrefab");
            Assert.That(chaliceVisual.transform.Find("Doors").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(CountEnabledChaliceSlots(chaliceVisual.transform), Is.EqualTo(6));
        }

        [Test]
        public void TryHandleScreenTapStartsChaliceBoxParticlesForChaliceCompletion()
        {
            var board = new BoardModel(5, 5);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 1), remainingDoorDurability: 0, requiredChaliceCount: 3, collectedChaliceCount: 1);
            board.PlaceItem(new BoardCoordinate(2, 2), new TntItemModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 2.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));
            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.False);
            Assert.That(session.Board.GetCell(chaliceBox.Anchor).Obstacle, Is.Null);
        }

        [Test]
        public void LongChaliceParticlesDoNotDelaySettleMotion()
        {
            var board = new BoardModel(4, 4);
            var chaliceBox = new ChaliceBoxObstacleModel(
                new BoardCoordinate(2, 0),
                remainingDoorDurability: 0,
                requiredChaliceCount: 2,
                collectedChaliceCount: 0);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);
            board.PlaceItem(new BoardCoordinate(1, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(3, 2), new CubeItemModel(CubeColor.Blue));

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);
            SetField(GetChaliceBoxParticlePlayer(bridge), "duration", 1.5f);

            InvokeMethod(bridge, "Start");

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(
                boardView.transform.TransformPoint(new Vector3(1.5f, 1.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.True);
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetDestructionFeedbackPlayer(bridge).Duration);

            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetSingleRocketEffectPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetChaliceBoxParticlePlayer(bridge).IsPlaying, Is.True);
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.True);
        }

        [Test]
        public void TryHandleScreenTapIgnoresFurtherTapsWhileSingleRocketEffectIsActive()
        {
            var board = new BoardModel(4, 4);
            board.PlaceItem(new BoardCoordinate(2, 1), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 5, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var rocketTap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(2.5f, 1.5f, 0f)));
            var cubeTap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(rocketTap), Is.True);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
            Assert.That(GetDestructionFeedbackPlayer(bridge).IsPlaying, Is.True);
            Assert.That(bridge.TryHandleScreenTap(cubeTap), Is.False);
            Assert.That(session.RemainingMoves, Is.EqualTo(4));
        }

        [Test]
        public void TryHandleScreenTapReturnsFalseWhenInputIsExternallySuppressed()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");
            bridge.SetInputSuppressed(true);

            var screenPosition = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(screenPosition), Is.False);
            Assert.That(session.RemainingMoves, Is.EqualTo(3));
            Assert.That(bridge.IsInputSuppressed, Is.True);
        }

        [Test]
        public void AdvancePendingPreviewContinuesWhileInputIsExternallySuppressed()
        {
            var board = new BoardModel(3, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));

            var session = new LevelSession(board, 3, new TestRefillCubeColorResolver());
            var bridge = CreateConfiguredBridge(session);
            var boardView = GetBoardView(bridge);

            InvokeMethod(bridge, "Start");

            var tap = GetCamera(bridge).WorldToScreenPoint(boardView.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));

            Assert.That(bridge.TryHandleScreenTap(tap), Is.True);
            bridge.SetInputSuppressed(true);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.True);

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetRemainingPreviewSeconds(bridge), Is.EqualTo(0f));
            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.False);
            Assert.That(GetItemRoot(boardView).childCount, Is.GreaterThan(0));
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

        [Test]
        public void TryHandleScreenTapIgnoresFurtherTapsWhileSettleMotionIsActive()
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

            AdvancePendingPreview(bridge, GetRemainingPreviewSeconds(bridge));

            Assert.That(GetSettleMotionPlayer(bridge).IsPlaying, Is.True);
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
            SetField(bridge, "destructionFeedbackPlayer", CreateDestructionFeedbackPlayer());
            SetField(bridge, "cubeBlastParticlePlayer", CreateCubeBlastParticlePlayer());
            SetField(bridge, "vaseParticlePlayer", CreateVaseParticlePlayer());
            SetField(bridge, "stoneParticlePlayer", CreateStoneParticlePlayer());
            SetField(bridge, "chaliceBoxParticlePlayer", CreateChaliceBoxParticlePlayer());
            SetField(bridge, "settleMotionPlayer", CreateSettleMotionPlayer());
            SetField(bridge, "singleRocketEffectPlayer", CreateRocketEffectPlayer());
            SetField(bridge, "singleTntEffectPlayer", CreateTntEffectPlayer());
            SetField(bridge, "comboPresentationPlayer", CreateComboPresentationPlayer());
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
            SetField(boardView, "vasePrefab", CreateVasePrefab());
            SetField(boardView, "stonePrefab", CreateVisualPrefab("StonePrefab"));
            SetField(boardView, "chaliceBoxPrefab", CreateChaliceBoxPrefab());
            return boardView;
        }

        private Transform GetItemRoot(BoardView boardView)
        {
            return (Transform)GetField(boardView, "itemVisualRoot");
        }

        private Transform GetObstacleRoot(BoardView boardView)
        {
            return (Transform)GetField(boardView, "obstacleVisualRoot");
        }

        private BoardView GetBoardView(BoardInputSessionBridge bridge)
        {
            return (BoardView)GetField(bridge, "boardView");
        }

        private Camera GetCamera(BoardInputSessionBridge bridge)
        {
            return (Camera)GetField(bridge, "inputCamera");
        }

        private SingleRocketActivationEffectPlayer GetSingleRocketEffectPlayer(BoardInputSessionBridge bridge)
        {
            return (SingleRocketActivationEffectPlayer)GetField(bridge, "singleRocketEffectPlayer");
        }

        private SingleTntActivationEffectPlayer GetSingleTntEffectPlayer(BoardInputSessionBridge bridge)
        {
            return (SingleTntActivationEffectPlayer)GetField(bridge, "singleTntEffectPlayer");
        }

        private BoardDestructionFeedbackPlayer GetDestructionFeedbackPlayer(BoardInputSessionBridge bridge)
        {
            return (BoardDestructionFeedbackPlayer)GetField(bridge, "destructionFeedbackPlayer");
        }

        private CubeBlastParticlePlayer GetCubeBlastParticlePlayer(BoardInputSessionBridge bridge)
        {
            return (CubeBlastParticlePlayer)GetField(bridge, "cubeBlastParticlePlayer");
        }

        private VaseParticlePlayer GetVaseParticlePlayer(BoardInputSessionBridge bridge)
        {
            return (VaseParticlePlayer)GetField(bridge, "vaseParticlePlayer");
        }

        private StoneParticlePlayer GetStoneParticlePlayer(BoardInputSessionBridge bridge)
        {
            return (StoneParticlePlayer)GetField(bridge, "stoneParticlePlayer");
        }

        private ChaliceBoxParticlePlayer GetChaliceBoxParticlePlayer(BoardInputSessionBridge bridge)
        {
            return (ChaliceBoxParticlePlayer)GetField(bridge, "chaliceBoxParticlePlayer");
        }

        private BoardSettleMotionPlayer GetSettleMotionPlayer(BoardInputSessionBridge bridge)
        {
            return (BoardSettleMotionPlayer)GetField(bridge, "settleMotionPlayer");
        }

        private SpecialItemComboPresentationPlayer GetComboPresentationPlayer(BoardInputSessionBridge bridge)
        {
            return (SpecialItemComboPresentationPlayer)GetField(bridge, "comboPresentationPlayer");
        }

        private GameObject CreateVisualPrefab(string name)
        {
            var prefab = CreateGameObject(name);
            prefab.AddComponent<SpriteRenderer>();
            return prefab;
        }

        private GameObject CreateVasePrefab()
        {
            var prefab = CreateVisualPrefab("VasePrefab");
            var vaseView = prefab.AddComponent<VaseObstacleView>();
            var spriteRenderer = prefab.GetComponent<SpriteRenderer>();
            SetField(vaseView, "spriteRenderer", spriteRenderer);
            SetField(vaseView, "undamagedSprite", CreateSprite(26, 16, 26f));
            SetField(vaseView, "damagedSprite", CreateSprite(27, 16, 27f));
            return prefab;
        }

        private GameObject CreateChaliceBoxPrefab()
        {
            var prefab = CreateVisualPrefab("ChaliceBoxPrefab");
            var background = CreateGameObject("Bg");
            var doors = CreateGameObject("Doors");
            var chalice = CreateGameObject("Chalice");

            background.transform.SetParent(prefab.transform, false);
            doors.transform.SetParent(prefab.transform, false);
            chalice.transform.SetParent(prefab.transform, false);

            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            var doorsRenderer = doors.AddComponent<SpriteRenderer>();
            var chaliceRenderer = chalice.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = CreateSprite(40, 40, 20f);
            doorsRenderer.sprite = CreateSprite(42, 42, 21f);
            chaliceRenderer.sprite = CreateSprite(20, 26, 20f);

            var chaliceView = prefab.AddComponent<ChaliceBoxObstacleView>();
            SetField(chaliceView, "backgroundRenderer", backgroundRenderer);
            SetField(chaliceView, "doorsRenderer", doorsRenderer);
            SetField(chaliceView, "chaliceRenderer", chaliceRenderer);
            return prefab;
        }

        private SingleRocketActivationEffectPlayer CreateRocketEffectPlayer()
        {
            var host = CreateGameObject("SingleRocketEffectPlayer");
            var effectPlayer = host.AddComponent<SingleRocketActivationEffectPlayer>();
            SetField(effectPlayer, "horizontalRocketPartLeftSprite", CreateSprite(18, 18, 18f));
            SetField(effectPlayer, "horizontalRocketPartRightSprite", CreateSprite(19, 18, 19f));
            SetField(effectPlayer, "verticalRocketPartTopSprite", CreateSprite(18, 19, 18f));
            SetField(effectPlayer, "verticalRocketPartBottomSprite", CreateSprite(19, 19, 19f));
            SetField(effectPlayer, "rocketParticleStarSprite", CreateSprite(14, 14, 20f));
            SetField(effectPlayer, "rocketParticleSmokeSprite", CreateSprite(20, 20, 20f));
            SetField(effectPlayer, "duration", 0.2f);
            return effectPlayer;
        }

        private SingleTntActivationEffectPlayer CreateTntEffectPlayer()
        {
            var host = CreateGameObject("SingleTntEffectPlayer");
            var effectPlayer = host.AddComponent<SingleTntActivationEffectPlayer>();
            SetField(effectPlayer, "tntBurstSprite", CreateSprite(24, 24, 20f));
            SetField(effectPlayer, "tntDebrisSprite", CreateSprite(22, 22, 20f));
            SetField(effectPlayer, "duration", 0.22f);
            return effectPlayer;
        }

        private BoardDestructionFeedbackPlayer CreateDestructionFeedbackPlayer()
        {
            var host = CreateGameObject("DestructionFeedbackPlayer");
            var effectPlayer = host.AddComponent<BoardDestructionFeedbackPlayer>();
            SetField(effectPlayer, "duration", 0.14f);
            return effectPlayer;
        }

        private CubeBlastParticlePlayer CreateCubeBlastParticlePlayer()
        {
            var host = CreateGameObject("CubeBlastParticlePlayer");
            var effectPlayer = host.AddComponent<CubeBlastParticlePlayer>();
            SetField(effectPlayer, "duration", 0.18f);
            SetField(effectPlayer, "particlesPerBurst", 3);
            SetField(effectPlayer, "redParticleSprite", CreateSprite(26, 18, 20f));
            SetField(effectPlayer, "greenParticleSprite", CreateSprite(27, 18, 20f));
            SetField(effectPlayer, "blueParticleSprite", CreateSprite(28, 18, 20f));
            SetField(effectPlayer, "yellowParticleSprite", CreateSprite(29, 18, 20f));
            return effectPlayer;
        }

        private VaseParticlePlayer CreateVaseParticlePlayer()
        {
            var host = CreateGameObject("VaseParticlePlayer");
            var effectPlayer = host.AddComponent<VaseParticlePlayer>();
            SetField(effectPlayer, "duration", 0.2f);
            SetField(effectPlayer, "mainShardSprite", CreateSprite(24, 24, 20f));
            SetField(effectPlayer, "fragmentSprite", CreateSprite(18, 18, 20f));
            SetField(effectPlayer, "dustSprite", CreateSprite(20, 16, 20f));
            return effectPlayer;
        }

        private StoneParticlePlayer CreateStoneParticlePlayer()
        {
            var host = CreateGameObject("StoneParticlePlayer");
            var effectPlayer = host.AddComponent<StoneParticlePlayer>();
            SetField(effectPlayer, "duration", 0.24f);
            SetField(effectPlayer, "mainChunkSprite", CreateSprite(24, 24, 20f));
            SetField(effectPlayer, "fragmentSprite", CreateSprite(18, 18, 20f));
            SetField(effectPlayer, "dustSprite", CreateSprite(20, 16, 20f));
            return effectPlayer;
        }

        private ChaliceBoxParticlePlayer CreateChaliceBoxParticlePlayer()
        {
            var host = CreateGameObject("ChaliceBoxParticlePlayer");
            var effectPlayer = host.AddComponent<ChaliceBoxParticlePlayer>();
            SetField(effectPlayer, "duration", 0.28f);
            SetField(effectPlayer, "doorPhaseSprites", new[]
            {
                CreateSprite(22, 22, 20f),
                CreateSprite(20, 16, 20f),
                CreateSprite(18, 24, 20f),
                CreateSprite(24, 18, 20f),
                CreateSprite(16, 20, 20f),
                CreateSprite(26, 22, 20f),
                CreateSprite(20, 26, 20f),
                CreateSprite(18, 18, 20f),
                CreateSprite(24, 24, 20f),
                CreateSprite(22, 18, 20f)
            });
            SetField(effectPlayer, "chalicePhaseSprites", new[]
            {
                CreateSprite(28, 20, 20f),
                CreateSprite(18, 26, 20f),
                CreateSprite(22, 22, 20f),
                CreateSprite(24, 16, 20f),
                CreateSprite(16, 24, 20f)
            });
            return effectPlayer;
        }

        private BoardSettleMotionPlayer CreateSettleMotionPlayer()
        {
            var host = CreateGameObject("SettleMotionPlayer");
            var effectPlayer = host.AddComponent<BoardSettleMotionPlayer>();
            SetField(effectPlayer, "secondsPerCell", 0.08f);
            SetField(effectPlayer, "minimumDuration", 0.12f);
            SetField(effectPlayer, "effectZ", -0.12f);
            return effectPlayer;
        }

        private SpecialItemComboPresentationPlayer CreateComboPresentationPlayer()
        {
            var host = CreateGameObject("ComboPresentationPlayer");
            var effectPlayer = host.AddComponent<SpecialItemComboPresentationPlayer>();
            SetField(effectPlayer, "horizontalRocketPartLeftSprite", CreateSprite(18, 18, 18f));
            SetField(effectPlayer, "horizontalRocketPartRightSprite", CreateSprite(19, 18, 19f));
            SetField(effectPlayer, "verticalRocketPartTopSprite", CreateSprite(18, 19, 18f));
            SetField(effectPlayer, "verticalRocketPartBottomSprite", CreateSprite(19, 19, 19f));
            SetField(effectPlayer, "tntSprite", CreateSprite(26, 20, 20f));
            SetField(effectPlayer, "rocketRocketDuration", 0.22f);
            SetField(effectPlayer, "tntRocketDuration", 0.24f);
            SetField(effectPlayer, "tntTntDuration", 0.26f);
            return effectPlayer;
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

        private static bool ContainsChildNameWithFragment(Transform root, string fragment)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                if (root.GetChild(index).name.Contains(fragment, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountEnabledChaliceSlots(Transform chaliceVisual)
        {
            var slotRoot = chaliceVisual.Find("ChaliceSlots");
            if (slotRoot is null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < slotRoot.childCount; index++)
            {
                var renderer = slotRoot.GetChild(index).GetComponent<SpriteRenderer>();
                if (renderer is not null && renderer.enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static GameObject FindChildByPrefix(Transform root, string prefix)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }

            Assert.Fail($"Could not find child with prefix '{prefix}'.");
            return null;
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
