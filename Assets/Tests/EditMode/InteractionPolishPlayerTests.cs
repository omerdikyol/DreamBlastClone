using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class InteractionPolishPlayerTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();
        private readonly List<Sprite> createdSprites = new List<Sprite>();
        private readonly List<Texture2D> createdTextures = new List<Texture2D>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdGameObjects.Count - 1; index >= 0; index--)
            {
                if (createdGameObjects[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdGameObjects[index]);
                }
            }

            createdGameObjects.Clear();

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
        }

        [Test]
        public void TapAnticipationPlayerCreatesAndCleansTransientItemVisual()
        {
            var board = new BoardModel(1, 1);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            var boardView = CreateConfiguredBoardView();
            boardView.Render(board);
            var effectRoot = CreateGameObject("TapAnticipationRoot").transform;
            var player = CreateGameObject("TapAnticipationPlayer").AddComponent<TapAnticipationPlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.1f);
            var descriptor = new TapAnticipationDescriptor(new[]
            {
                new TapAnticipationEvent(new BoardCoordinate(0, 0), TapAnticipationEventType.CubeGroup)
            });

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(1));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void PlayersReturnFalseForEmptyDescriptors()
        {
            var board = new BoardModel(1, 1);
            var boardView = CreateConfiguredBoardView();
            boardView.Render(board);

            Assert.That(CreateGameObject("TapAnticipationPlayer").AddComponent<TapAnticipationPlayer>()
                .TryPlay(boardView, board, TapAnticipationDescriptor.Empty()), Is.False);
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
            SetField(boardView, "boardCenter", Vector2.zero);
            SetField(boardView, "cellSize", 1f);
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

        private GameObject CreateVasePrefab()
        {
            var prefab = CreateVisualPrefab("VasePrefab");
            var vaseView = prefab.AddComponent<VaseObstacleView>();
            SetField(vaseView, "spriteRenderer", prefab.GetComponent<SpriteRenderer>());
            SetField(vaseView, "undamagedSprite", CreateSprite(26, 16, 26f));
            SetField(vaseView, "damagedSprite", CreateSprite(27, 16, 27f));
            return prefab;
        }

        private GameObject CreateChaliceBoxPrefab()
        {
            var prefab = CreateVisualPrefab("ChaliceBoxPrefab");
            var background = CreateGameObject("Background");
            var doors = CreateGameObject("Doors");
            var chalice = CreateGameObject("Chalice");
            background.transform.SetParent(prefab.transform, false);
            doors.transform.SetParent(prefab.transform, false);
            chalice.transform.SetParent(prefab.transform, false);

            var chaliceView = prefab.AddComponent<ChaliceBoxObstacleView>();
            SetField(chaliceView, "backgroundRenderer", background.AddComponent<SpriteRenderer>());
            SetField(chaliceView, "doorsRenderer", doors.AddComponent<SpriteRenderer>());
            SetField(chaliceView, "chaliceRenderer", chalice.AddComponent<SpriteRenderer>());
            ((SpriteRenderer)GetField(chaliceView, "backgroundRenderer")).sprite = CreateSprite(40, 40, 20f);
            ((SpriteRenderer)GetField(chaliceView, "doorsRenderer")).sprite = CreateSprite(42, 42, 21f);
            ((SpriteRenderer)GetField(chaliceView, "chaliceRenderer")).sprite = CreateSprite(20, 26, 20f);
            return prefab;
        }

        private GameObject CreateVisualPrefab(string name)
        {
            var prefab = CreateGameObject(name);
            prefab.AddComponent<SpriteRenderer>().sprite = CreateSprite(20, 20, 20f);
            return prefab;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            createdTextures.Add(texture);
            createdSprites.Add(sprite);
            return sprite;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Could not find field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Could not find field '{fieldName}' on {target.GetType().Name}.");
            return field.GetValue(target);
        }
    }
}
