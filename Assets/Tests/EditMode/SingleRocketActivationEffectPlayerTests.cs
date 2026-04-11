using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SingleRocketActivationEffectPlayerTests
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
        public void TryPlayHorizontalRocketCreatesSplitPartsAndParticlesAndCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out var starSprite, out var smokeSprite);
            var descriptor = new SingleRocketActivationEffectDescriptor(
                RocketOrientation.Horizontal,
                new BoardCoordinate(2, 2),
                new BoardCoordinate(0, 2),
                new BoardCoordinate(4, 2));

            Assert.That(player.TryPlay(boardView, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(player.Duration, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(ContainsChildWithPrefix(effectRoot, "HorizontalRocketNegativePart"), Is.True);
            Assert.That(ContainsChildWithPrefix(effectRoot, "HorizontalRocketPositivePart"), Is.True);
            Assert.That(CountChildrenWithPrefix(effectRoot, "RocketParticleStar_"), Is.GreaterThan(0));
            Assert.That(CountChildrenWithPrefix(effectRoot, "RocketParticleSmoke_"), Is.GreaterThan(0));

            AssertParticleSprites(effectRoot, starSprite, smokeSprite);
            AssertMaskedRocketRenderers(effectRoot);

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayVerticalRocketCreatesSplitPartsAndParticlesAndCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out _, out _);
            var descriptor = new SingleRocketActivationEffectDescriptor(
                RocketOrientation.Vertical,
                new BoardCoordinate(1, 1),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(1, 4));

            Assert.That(player.TryPlay(boardView, descriptor), Is.True);
            Assert.That(ContainsChildWithPrefix(effectRoot, "VerticalRocketNegativePart"), Is.True);
            Assert.That(ContainsChildWithPrefix(effectRoot, "VerticalRocketPositivePart"), Is.True);
            Assert.That(CountChildrenWithPrefix(effectRoot, "RocketParticleStar_"), Is.GreaterThan(0));
            Assert.That(CountChildrenWithPrefix(effectRoot, "RocketParticleSmoke_"), Is.GreaterThan(0));
            AssertMaskedRocketRenderers(effectRoot);

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

        private SingleRocketActivationEffectPlayer CreatePlayer(out Transform effectRoot, out Sprite starSprite, out Sprite smokeSprite)
        {
            var host = CreateGameObject("SingleRocketEffectPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var player = host.AddComponent<SingleRocketActivationEffectPlayer>();
            starSprite = CreateSprite(14, 14, 20f);
            smokeSprite = CreateSprite(20, 20, 20f);
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.2f);
            SetField(player, "horizontalRocketPartLeftSprite", CreateSprite(18, 18, 18f));
            SetField(player, "horizontalRocketPartRightSprite", CreateSprite(19, 18, 19f));
            SetField(player, "verticalRocketPartTopSprite", CreateSprite(18, 19, 18f));
            SetField(player, "verticalRocketPartBottomSprite", CreateSprite(19, 19, 19f));
            SetField(player, "rocketParticleStarSprite", starSprite);
            SetField(player, "rocketParticleSmokeSprite", smokeSprite);
            return player;
        }

        private void AssertParticleSprites(Transform effectRoot, Sprite starSprite, Sprite smokeSprite)
        {
            for (var index = 0; index < effectRoot.childCount; index++)
            {
                var child = effectRoot.GetChild(index);
                if (!child.name.StartsWith("RocketParticle", System.StringComparison.Ordinal))
                {
                    continue;
                }

                var renderer = child.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null);

                if (child.name.StartsWith("RocketParticleStar_", System.StringComparison.Ordinal))
                {
                    Assert.That(renderer.sprite, Is.SameAs(starSprite));
                }
                else if (child.name.StartsWith("RocketParticleSmoke_", System.StringComparison.Ordinal))
                {
                    Assert.That(renderer.sprite, Is.SameAs(smokeSprite));
                }
            }
        }

        private static void AssertMaskedRocketRenderers(Transform effectRoot)
        {
            for (var index = 0; index < effectRoot.childCount; index++)
            {
                var renderer = effectRoot.GetChild(index).GetComponent<SpriteRenderer>();
                if (renderer is null)
                {
                    continue;
                }

                Assert.That(renderer.maskInteraction, Is.EqualTo(SpriteMaskInteraction.VisibleInsideMask));
            }
        }

        private static bool ContainsChildWithPrefix(Transform root, string prefix)
        {
            return CountChildrenWithPrefix(root, prefix) > 0;
        }

        private static int CountChildrenWithPrefix(Transform root, string prefix)
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
