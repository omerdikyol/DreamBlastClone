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
    public sealed class BoardDestructionFeedbackPlayerTests
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
        public void TryPlaySpawnsAndCleansUpRemovedCubeVisuals()
        {
            var board = new BoardModel(2, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));

            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new BoardDestructionFeedbackDescriptor(
                new[]
                {
                    new BoardCoordinate(0, 0),
                    new BoardCoordinate(1, 0)
                },
                System.Array.Empty<RemovedObstacleFeedback>());

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(2));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlaySpawnsOneVisualPerRemovedObstacleInstance()
        {
            var board = new BoardModel(4, 4);
            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 1);

            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            var descriptor = new BoardDestructionFeedbackDescriptor(
                System.Array.Empty<BoardCoordinate>(),
                new[]
                {
                    new RemovedObstacleFeedback(chaliceBox.OccupiedCoordinates)
                });

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(1));
            Assert.That(effectRoot.GetChild(0).GetComponent<ChaliceBoxObstacleView>(), Is.Not.Null);
        }

        [Test]
        public void TryPlaySpawnsAndCleansUpRemovedStoneVisual()
        {
            var board = new BoardModel(1, 1);
            var boardView = CreateConfiguredBoardView();
            var player = CreatePlayer(out var effectRoot);
            board.PlaceObstacle(new BoardCoordinate(0, 0), new StoneObstacleModel());

            var descriptor = new BoardDestructionFeedbackDescriptor(
                System.Array.Empty<BoardCoordinate>(),
                new[]
                {
                    new RemovedObstacleFeedback(new[] { new BoardCoordinate(0, 0) })
                });

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(1));

            var effectVisual = effectRoot.GetChild(0).GetComponent<SpriteRenderer>();
            var stonePrefab = (GameObject)GetField(boardView, "stonePrefab");
            Assert.That(effectVisual.sprite, Is.SameAs(stonePrefab.GetComponent<SpriteRenderer>().sprite));
            Assert.That(effectVisual.color.r, Is.EqualTo(1f));
            Assert.That(effectVisual.color.g, Is.EqualTo(1f));
            Assert.That(effectVisual.color.b, Is.EqualTo(1f));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
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
            SetField(boardView, "stonePrefab", CreateStonePrefab());
            SetField(boardView, "chaliceBoxPrefab", CreateChaliceBoxPrefab());
            return boardView;
        }

        private BoardDestructionFeedbackPlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("DestructionFeedbackPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);
            var player = host.AddComponent<BoardDestructionFeedbackPlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.14f);
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

        private GameObject CreateStonePrefab()
        {
            var prefab = CreateVisualPrefab("StonePrefab");
            prefab.GetComponent<SpriteRenderer>().sprite = CreateSprite(26, 16, 26f);
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

        private static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return field.GetValue(target);
        }
    }
}
