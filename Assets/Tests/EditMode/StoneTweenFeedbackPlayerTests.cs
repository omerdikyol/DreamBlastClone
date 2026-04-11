using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class StoneTweenFeedbackPlayerTests
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
                    Object.DestroyImmediate(createdGameObjects[index]);
                }
            }

            createdGameObjects.Clear();

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
        }

        [Test]
        public void TryPlayCreatesAndCleansTransientVisual()
        {
            var board = CreateBoard(new[] { new BoardCoordinate(1, 2) });
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new StoneParticleDescriptor(new[] { new BoardCoordinate(1, 2) });

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(1));
            Assert.That(effectRoot.GetChild(0).name, Does.Contain("StoneObstacleModel"));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayCreatesVisualPerRemovedStone()
        {
            var coordinates = new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(2, 1)
            };
            var board = CreateBoard(coordinates);
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new StoneParticleDescriptor(coordinates);

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(2));
        }

        [Test]
        public void TryPlayReturnsFalseForEmptyDescriptor()
        {
            var board = CreateBoard(new[] { new BoardCoordinate(0, 0) });
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out var effectRoot);

            Assert.That(player.TryPlay(boardView, board, StoneParticleDescriptor.Empty()), Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        private BoardModel CreateBoard(IReadOnlyList<BoardCoordinate> stoneCoordinates)
        {
            var board = new BoardModel(3, 3);
            foreach (var coordinate in stoneCoordinates)
            {
                board.PlaceObstacle(coordinate, new StoneObstacleModel());
            }

            return board;
        }

        private BoardView CreateBoardView()
        {
            var host = CreateGameObject("BoardViewHost");
            var obstacleRoot = CreateGameObject("ObstacleRoot").transform;
            obstacleRoot.SetParent(host.transform, false);

            var boardView = host.AddComponent<BoardView>();
            SetField(boardView, "obstacleVisualRoot", obstacleRoot);
            SetField(boardView, "cellSize", 1f);
            SetField(boardView, "stonePrefab", CreateStonePrefab());
            return boardView;
        }

        private StoneTweenFeedbackPlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("StoneTweenFeedbackPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);
            var player = host.AddComponent<StoneTweenFeedbackPlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.1f);
            return player;
        }

        private GameObject CreateStonePrefab()
        {
            var prefab = CreateGameObject("StonePrefab");
            var renderer = prefab.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSprite(28, 16, 28f);
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
            field.SetValue(target, value);
        }
    }
}
