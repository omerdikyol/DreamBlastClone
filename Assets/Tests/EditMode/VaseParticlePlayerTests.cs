using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class VaseParticlePlayerTests
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
        public void TryPlayCreatesDamageParticlesAtBoardCoordinatesAndCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new VaseParticleDescriptor(new[]
            {
                new VaseParticleEvent(new BoardCoordinate(1, 2), isRemoval: false)
            });

            Assert.That(player.TryPlay(boardView, descriptor, destroyBelowWorldY: -2f), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(6));

            var firstChild = effectRoot.GetChild(0);
            Assert.That(firstChild.position.x, Is.EqualTo(boardView.GetCellCenterWorld(new BoardCoordinate(1, 2)).x).Within(0.2f));
            Assert.That(firstChild.position.y, Is.EqualTo(boardView.GetCellCenterWorld(new BoardCoordinate(1, 2)).y).Within(0.2f));

            player.Advance(0.1f);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(6));

            player.Advance(5f);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayCreatesMoreParticlesForRemovalThanDamage()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);

            Assert.That(player.TryPlay(boardView, new VaseParticleDescriptor(new[]
            {
                new VaseParticleEvent(new BoardCoordinate(0, 0), isRemoval: false)
            }), destroyBelowWorldY: -2f), Is.True);
            var damageCount = effectRoot.childCount;

            player.Stop();

            Assert.That(player.TryPlay(boardView, new VaseParticleDescriptor(new[]
            {
                new VaseParticleEvent(new BoardCoordinate(0, 0), isRemoval: true)
            }), destroyBelowWorldY: -2f), Is.True);
            var removalCount = effectRoot.childCount;

            Assert.That(damageCount, Is.EqualTo(6));
            Assert.That(removalCount, Is.EqualTo(10));
            Assert.That(removalCount, Is.GreaterThan(damageCount));
        }

        [Test]
        public void TryPlayCreatesBurstPerEvent()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new VaseParticleDescriptor(new[]
            {
                new VaseParticleEvent(new BoardCoordinate(0, 0), isRemoval: false),
                new VaseParticleEvent(new BoardCoordinate(2, 1), isRemoval: true)
            });

            Assert.That(player.TryPlay(boardView, descriptor, destroyBelowWorldY: -2f), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(16));
        }

        private BoardView CreateBoardView()
        {
            var host = CreateGameObject("BoardViewHost");
            var boardView = host.AddComponent<BoardView>();
            SetField(boardView, "origin", Vector2.zero);
            SetField(boardView, "cellSize", 1f);
            return boardView;
        }

        private VaseParticlePlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("VaseParticlePlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var player = host.AddComponent<VaseParticlePlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.26f);
            SetField(player, "mainShardSprite", CreateSprite(24, 24, 20f));
            SetField(player, "fragmentSprite", CreateSprite(18, 18, 20f));
            SetField(player, "dustSprite", CreateSprite(20, 16, 20f));
            return player;
        }

        private Sprite CreateSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
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
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
