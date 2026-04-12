using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardSettleMotionPlayerTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();
        private readonly List<Sprite> createdSprites = new List<Sprite>();
        private readonly List<Texture2D> createdTextures = new List<Texture2D>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdSprites.Count - 1; index >= 0; index--)
            {
                if (createdSprites[index] is not null)
                {
                    Object.DestroyImmediate(createdSprites[index]);
                }
            }

            createdSprites.Clear();

            for (var index = createdTextures.Count - 1; index >= 0; index--)
            {
                if (createdTextures[index] is not null)
                {
                    Object.DestroyImmediate(createdTextures[index]);
                }
            }

            createdTextures.Clear();

            for (var index = createdGameObjects.Count - 1; index >= 0; index--)
            {
                if (createdGameObjects[index] is not null)
                {
                    Object.DestroyImmediate(createdGameObjects[index]);
                }
            }

            createdGameObjects.Clear();
        }

        [Test]
        public void TryPlayCreatesTransientMotionVisualsAtTheirStartPositions()
        {
            var finalBoard = new BoardModel(2, 4);
            finalBoard.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            finalBoard.PlaceItem(new BoardCoordinate(1, 2), new CubeItemModel(CubeColor.Green));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                new[]
                {
                    new ItemSettleMove(new BoardCoordinate(0, 2), new BoardCoordinate(0, 0))
                },
                new[]
                {
                    new RefillSpawnMotion(new BoardCoordinate(1, 4), new BoardCoordinate(1, 2), CubeColor.Green)
                });

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(2));
            Assert.That(Vector3.Distance(effectRoot.GetChild(0).position, boardView.GetCellCenterWorld(new BoardCoordinate(0, 2))), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(effectRoot.GetChild(1).position, boardView.GetCellCenterWorld(new BoardCoordinate(1, 4))), Is.LessThan(0.0001f));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayCreatesTransientObstacleMotionVisualAtItsStartPosition()
        {
            var finalBoard = new BoardModel(1, 3);
            finalBoard.PlaceObstacle(new BoardCoordinate(0, 0), new VaseObstacleModel());

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                System.Array.Empty<ItemSettleMove>(),
                System.Array.Empty<RefillSpawnMotion>(),
                new[]
                {
                    new ObstacleSettleMove(new BoardCoordinate(0, 2), new BoardCoordinate(0, 0))
                });

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(effectRoot.GetChild(0).position, boardView.GetCellCenterWorld(new BoardCoordinate(0, 2))), Is.LessThan(0.0001f));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayMakesLongFallsTakeLongerButClampsBelowSluggishDuration()
        {
            var boardView = CreateConfiguredBoardView();

            var shortBoard = new BoardModel(1, 2);
            shortBoard.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            var shortPlayer = CreatePlayer(out _);
            var shortDescriptor = new BoardSettleMotionDescriptor(
                new[]
                {
                    new ItemSettleMove(new BoardCoordinate(0, 1), new BoardCoordinate(0, 0))
                },
                System.Array.Empty<RefillSpawnMotion>());

            var longBoard = new BoardModel(1, 8);
            longBoard.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            var longPlayer = CreatePlayer(out _);
            var longDescriptor = new BoardSettleMotionDescriptor(
                new[]
                {
                    new ItemSettleMove(new BoardCoordinate(0, 7), new BoardCoordinate(0, 0))
                },
                System.Array.Empty<RefillSpawnMotion>());

            Assert.That(shortPlayer.TryPlay(boardView, shortBoard, shortDescriptor), Is.True);
            Assert.That(longPlayer.TryPlay(boardView, longBoard, longDescriptor), Is.True);
            Assert.That(shortPlayer.Duration, Is.LessThan(longPlayer.Duration));
            Assert.That(longPlayer.Duration, Is.LessThan(0.37f));
        }

        [Test]
        public void TryPlayReturnsFalseForEmptyDescriptor()
        {
            var finalBoard = new BoardModel(1, 1);
            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out _);

            Assert.That(player.TryPlay(boardView, finalBoard, BoardSettleMotionDescriptor.Empty()), Is.False);
        }

        [Test]
        public void TryPlayLandsLowerDestinationBeforeHigherDestinationInSameColumn()
        {
            var finalBoard = new BoardModel(1, 6);
            finalBoard.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            finalBoard.PlaceItem(new BoardCoordinate(0, 3), new CubeItemModel(CubeColor.Green));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                new[]
                {
                    new ItemSettleMove(new BoardCoordinate(0, 4), new BoardCoordinate(0, 0)),
                    new ItemSettleMove(new BoardCoordinate(0, 5), new BoardCoordinate(0, 3))
                },
                System.Array.Empty<RefillSpawnMotion>());

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);

            player.Advance(0.26f);

            Assert.That(Vector3.Distance(effectRoot.GetChild(0).position, boardView.GetCellCenterWorld(new BoardCoordinate(0, 0))), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(effectRoot.GetChild(1).position, boardView.GetCellCenterWorld(new BoardCoordinate(0, 3))), Is.GreaterThan(0.01f));
        }

        [Test]
        public void TryPlayKeepsRefillSpawnOrderNaturalFromTopOfBoard()
        {
            var finalBoard = new BoardModel(1, 6);
            finalBoard.PlaceItem(new BoardCoordinate(0, 4), new CubeItemModel(CubeColor.Blue));
            finalBoard.PlaceItem(new BoardCoordinate(0, 3), new CubeItemModel(CubeColor.Green));
            finalBoard.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Red));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                System.Array.Empty<ItemSettleMove>(),
                new[]
                {
                    new RefillSpawnMotion(new BoardCoordinate(0, 6), new BoardCoordinate(0, 4), CubeColor.Blue),
                    new RefillSpawnMotion(new BoardCoordinate(0, 7), new BoardCoordinate(0, 3), CubeColor.Green),
                    new RefillSpawnMotion(new BoardCoordinate(0, 8), new BoardCoordinate(0, 2), CubeColor.Red)
                });

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);

            player.Advance(0.1f);

            Assert.That(effectRoot.GetChild(0).position.y, Is.LessThan(effectRoot.GetChild(1).position.y));
            Assert.That(effectRoot.GetChild(1).position.y, Is.LessThan(effectRoot.GetChild(2).position.y));
        }

        [Test]
        public void TryPlayConfiguresRefillVisualToBeClippedInsideBoardMask()
        {
            var finalBoard = new BoardModel(1, 4);
            finalBoard.PlaceItem(new BoardCoordinate(0, 2), new CubeItemModel(CubeColor.Green));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                System.Array.Empty<ItemSettleMove>(),
                new[]
                {
                    new RefillSpawnMotion(new BoardCoordinate(0, 4), new BoardCoordinate(0, 2), CubeColor.Green)
                });

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);

            var refillRenderer = effectRoot.GetChild(0).GetComponent<SpriteRenderer>();
            Assert.That(refillRenderer.maskInteraction, Is.EqualTo(SpriteMaskInteraction.VisibleInsideMask));
        }

        [Test]
        public void TryPlayLateLandingReturnsNearDestinationAndBaseScaleBeforeCleanup()
        {
            var finalBoard = new BoardModel(1, 3);
            finalBoard.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                new[]
                {
                    new ItemSettleMove(new BoardCoordinate(0, 2), new BoardCoordinate(0, 0))
                },
                System.Array.Empty<RefillSpawnMotion>());

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);

            player.Advance(player.Duration - 0.001f);

            var visual = effectRoot.GetChild(0);
            var destination = boardView.GetCellCenterWorld(new BoardCoordinate(0, 0));
            Assert.That(Vector3.Distance(visual.position, destination), Is.LessThan(0.002f));
            Assert.That(Vector3.Distance(visual.localScale, Vector3.one), Is.LessThan(0.02f));
        }

        [Test]
        public void TryPlayLandingReboundsSlightlyAboveDestinationBeforeSettling()
        {
            var finalBoard = new BoardModel(1, 3);
            finalBoard.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardSettleMotionDescriptor(
                new[]
                {
                    new ItemSettleMove(new BoardCoordinate(0, 2), new BoardCoordinate(0, 0))
                },
                System.Array.Empty<RefillSpawnMotion>());

            Assert.That(player.TryPlay(boardView, finalBoard, descriptor), Is.True);

            player.Advance(0.165f);

            var visual = effectRoot.GetChild(0);
            var destination = boardView.GetCellCenterWorld(new BoardCoordinate(0, 0));
            Assert.That(visual.position.y, Is.GreaterThan(destination.y));
        }

        private BoardView CreateConfiguredBoardView()
        {
            var host = CreateGameObject("BoardViewHost");
            var itemRoot = CreateGameObject("ItemRoot").transform;
            var obstacleRoot = CreateGameObject("ObstacleRoot").transform;
            itemRoot.SetParent(host.transform, false);
            obstacleRoot.SetParent(host.transform, false);

            var boardView = host.AddComponent<BoardView>();
            SetField(boardView, "itemVisualRoot", itemRoot);
            SetField(boardView, "obstacleVisualRoot", obstacleRoot);
            SetField(boardView, "origin", Vector2.zero);
            SetField(boardView, "cellSize", 1f);
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

        private BoardSettleMotionPlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("SettleMotionPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);
            var player = host.AddComponent<BoardSettleMotionPlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "shortFallDurationSeconds", 0.11f);
            SetField(player, "longFallDurationSeconds", 0.28f);
            SetField(player, "distanceForLongFallSeconds", 7f);
            SetField(player, "minimumDuration", 0.12f);
            SetField(player, "gravityCascadeDelayStep", 0.03f);
            SetField(player, "shortLandingDurationSeconds", 0.085f);
            SetField(player, "longLandingDurationSeconds", 0.12f);
            SetField(player, "landingDipCells", 0.12f);
            SetField(player, "landingReboundCells", 0.12f);
            SetField(player, "landingScaleX", 1.12f);
            SetField(player, "landingScaleY", 0.88f);
            SetField(player, "reboundScaleX", 0.91f);
            SetField(player, "reboundScaleY", 1.1f);
            SetField(player, "effectZ", 0f);
            return player;
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

        private GameObject CreateVisualPrefab(string name)
        {
            var prefab = CreateGameObject(name);
            prefab.AddComponent<SpriteRenderer>();
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

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

    }
}
