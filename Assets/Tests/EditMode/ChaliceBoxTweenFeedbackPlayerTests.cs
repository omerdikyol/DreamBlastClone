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
    public sealed class ChaliceBoxTweenFeedbackPlayerTests
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

        [TestCase(ChaliceBoxParticleEventType.DoorDamage)]
        [TestCase(ChaliceBoxParticleEventType.DoorBreak)]
        [TestCase(ChaliceBoxParticleEventType.ChaliceDamage)]
        [TestCase(ChaliceBoxParticleEventType.ChaliceComplete)]
        public void TryPlayCreatesAndCleansTransientVisual(ChaliceBoxParticleEventType eventType)
        {
            var board = CreateBoard(eventType);
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), eventType, amount: 1)
            });

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(1));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void DoorBreakFeedbackUsesPreTapDoorVisual()
        {
            var board = CreateBoard(ChaliceBoxParticleEventType.DoorBreak);
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.DoorBreak, amount: 1)
            });

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);

            var transientView = effectRoot.GetChild(0).GetComponent<ChaliceBoxObstacleView>();
            Assert.That(transientView, Is.Not.Null);
            Assert.That(GetRenderer(transientView, "doorsRenderer").enabled, Is.True);
        }

        [Test]
        public void TryPlayDelaysTransientVisualByHitStep()
        {
            var board = CreateBoard(ChaliceBoxParticleEventType.DoorBreak);
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.DoorBreak, amount: 1, hitStep: 2)
            });

            SetField(player, "eventStepDelay", 0.1f);

            Assert.That(player.TryPlay(boardView, board, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));

            player.Advance(0.19f);

            Assert.That(effectRoot.childCount, Is.EqualTo(0));

            player.Advance(0.01f);

            Assert.That(effectRoot.childCount, Is.EqualTo(1));
        }

        [Test]
        public void TryPlayReturnsFalseForEmptyDescriptor()
        {
            var board = CreateBoard(ChaliceBoxParticleEventType.DoorDamage);
            var boardView = CreateBoardView();
            boardView.Render(board);
            var player = CreatePlayer(out _);

            Assert.That(player.TryPlay(boardView, board, ChaliceBoxParticleDescriptor.Empty()), Is.False);
        }

        private BoardModel CreateBoard(ChaliceBoxParticleEventType eventType)
        {
            var board = new BoardModel(2, 2);
            var chaliceBox = eventType is ChaliceBoxParticleEventType.DoorDamage or ChaliceBoxParticleEventType.DoorBreak
                ? new ChaliceBoxObstacleModel(new BoardCoordinate(0, 0), remainingDoorDurability: 1)
                : new ChaliceBoxObstacleModel(new BoardCoordinate(0, 0), remainingDoorDurability: 0, requiredChaliceCount: 10, collectedChaliceCount: 2);
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);
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
            SetField(boardView, "chaliceBoxPrefab", CreateChaliceBoxPrefab());
            return boardView;
        }

        private ChaliceBoxTweenFeedbackPlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("ChaliceBoxTweenFeedbackPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);
            var player = host.AddComponent<ChaliceBoxTweenFeedbackPlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.1f);
            return player;
        }

        private GameObject CreateChaliceBoxPrefab()
        {
            var prefab = CreateGameObject("ChaliceBoxPrefab");
            prefab.AddComponent<SpriteRenderer>();
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
            prefab.AddComponent<ChaliceBoxTweenPresentationView>();
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

        private static SpriteRenderer GetRenderer(ChaliceBoxObstacleView chaliceView, string fieldName)
        {
            return (SpriteRenderer)GetField(chaliceView, fieldName);
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
    }
}
