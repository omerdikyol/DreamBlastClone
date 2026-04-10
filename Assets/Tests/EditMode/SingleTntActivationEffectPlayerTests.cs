using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SingleTntActivationEffectPlayerTests
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
        public void TryPlayCreatesPulseAndFieldParticlesThenCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out var burstSprite, out var debrisSprite);
            var descriptor = new SingleTntActivationEffectDescriptor(
                new BoardCoordinate(1, 1),
                new[]
                {
                    new BoardCoordinate(0, 0),
                    new BoardCoordinate(1, 0),
                    new BoardCoordinate(2, 0),
                    new BoardCoordinate(0, 1),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(0, 2),
                    new BoardCoordinate(1, 2),
                    new BoardCoordinate(2, 2)
                },
                minX: 0,
                minY: 0,
                maxX: 2,
                maxY: 2);

            Assert.That(player.TryPlay(boardView, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(player.Duration, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(CountChildrenByPrefix(effectRoot, "SingleTntPulse"), Is.EqualTo(1));
            Assert.That(CountChildrenByPrefix(effectRoot, "SingleTntBurst_"), Is.EqualTo(9));
            Assert.That(CountChildrenByPrefix(effectRoot, "SingleTntDebris_"), Is.EqualTo(9));

            AssertParticleSprites(effectRoot, burstSprite, debrisSprite);

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        private BoardView CreateBoardView()
        {
            var host = CreateGameObject("BoardViewHost");
            var boardView = host.AddComponent<BoardView>();
            SetField(boardView, "origin", Vector2.zero);
            SetField(boardView, "cellSize", 1f);
            return boardView;
        }

        private SingleTntActivationEffectPlayer CreatePlayer(out Transform effectRoot, out Sprite burstSprite, out Sprite debrisSprite)
        {
            var host = CreateGameObject("SingleTntEffectPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var player = host.AddComponent<SingleTntActivationEffectPlayer>();
            burstSprite = CreateSprite(24, 24, 20f);
            debrisSprite = CreateSprite(22, 22, 20f);
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "tntBurstSprite", burstSprite);
            SetField(player, "tntDebrisSprite", debrisSprite);
            SetField(player, "duration", 0.22f);
            return player;
        }

        private void AssertParticleSprites(Transform effectRoot, Sprite burstSprite, Sprite debrisSprite)
        {
            for (var index = 0; index < effectRoot.childCount; index++)
            {
                var child = effectRoot.GetChild(index);
                var renderer = child.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null);

                if (child.name.StartsWith("SingleTntPulse", System.StringComparison.Ordinal)
                    || child.name.StartsWith("SingleTntBurst_", System.StringComparison.Ordinal))
                {
                    Assert.That(renderer.sprite, Is.SameAs(burstSprite));
                }
                else if (child.name.StartsWith("SingleTntDebris_", System.StringComparison.Ordinal))
                {
                    Assert.That(renderer.sprite, Is.SameAs(debrisSprite));
                }
            }
        }

        private static int CountChildrenByPrefix(Transform root, string prefix)
        {
            var count = 0;
            for (var index = 0; index < root.childCount; index++)
            {
                if (root.GetChild(index).name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
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
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
