using System.Collections.Generic;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class WinCelebrationParticlePlayerTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] is not null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void TryPlaySpawnsParticlesAroundAnchorAndStopCleansUp()
        {
            var rootObject = CreateGameObject("WinPresentationRoot");
            var rootRect = rootObject.AddComponent<RectTransform>();
            var effectRoot = CreateGameObject("WinCelebrationParticles").AddComponent<RectTransform>();
            effectRoot.SetParent(rootRect, false);
            var anchor = CreateGameObject("WinStar").AddComponent<RectTransform>();
            anchor.SetParent(rootRect, false);
            anchor.anchoredPosition = new Vector2(40f, -30f);

            var player = rootObject.AddComponent<WinCelebrationParticlePlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "particleSprite", CreateSprite(64, 64, 64f));
            SetField(player, "initialBurstCount", 4);
            SetField(player, "ambientBurstCount", 1);
            SetField(player, "ambientSpawnInterval", 0.1f);
            SetField(player, "particleLifetime", 0.4f);
            SetField(player, "initialSpawnRadius", 12f);
            SetField(player, "ambientSpawnRadius", 18f);
            SetField(player, "travelDistance", 20f);
            SetField(player, "driftUpward", 8f);
            SetField(player, "initialStartSize", 24f);
            SetField(player, "ambientStartSize", 12f);
            SetField(player, "endSize", 8f);

            var didPlay = player.TryPlay(anchor);

            Assert.That(didPlay, Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(4));

            for (var index = 0; index < effectRoot.childCount; index++)
            {
                var child = effectRoot.GetChild(index) as RectTransform;
                Assert.That(child, Is.Not.Null);
                Assert.That(child.GetComponent<Image>(), Is.Not.Null);
                Assert.That(Vector2.Distance(child.anchoredPosition, anchor.anchoredPosition), Is.LessThan(30f));
            }

            player.Advance(0.11f);
            Assert.That(effectRoot.childCount, Is.GreaterThan(4));

            player.Stop();

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayFailsSafelyWithoutSpriteOrAnchor()
        {
            var rootObject = CreateGameObject("WinPresentationRoot");
            var rootRect = rootObject.AddComponent<RectTransform>();
            var effectRoot = CreateGameObject("WinCelebrationParticles").AddComponent<RectTransform>();
            effectRoot.SetParent(rootRect, false);
            var player = rootObject.AddComponent<WinCelebrationParticlePlayer>();
            SetField(player, "effectRoot", effectRoot);

            Assert.That(player.TryPlay(null), Is.False);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));

            var anchor = CreateGameObject("WinStar").AddComponent<RectTransform>();
            anchor.SetParent(rootRect, false);

            Assert.That(player.TryPlay(anchor), Is.False);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            createdObjects.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            createdObjects.Add(sprite);
            return sprite;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
