using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class CubeBlastParticlePlayerTests
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
        public void TryPlaySpawnsColorMatchedParticlesAtBurstCoordinatesAndCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out var redSprite, out var blueSprite);
            var descriptor = new CubeBlastParticleDescriptor(new CubeBlastBurstGroup[]
            {
                new(CubeColor.Red, new[]
                {
                    new BoardCoordinate(0, 0),
                    new BoardCoordinate(1, 0)
                }),
                new(CubeColor.Blue, new[]
                {
                    new BoardCoordinate(0, 1)
                })
            });

            Assert.That(player.TryPlay(boardView, descriptor, destroyBelowWorldY: -2f), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(player.Duration, Is.EqualTo(0.18f).Within(0.0001f));
            Assert.That(effectRoot.childCount, Is.EqualTo(9));

            var firstCenter = boardView.GetCellCenterWorld(new BoardCoordinate(0, 0));
            var secondCenter = boardView.GetCellCenterWorld(new BoardCoordinate(1, 0));
            var thirdCenter = boardView.GetCellCenterWorld(new BoardCoordinate(0, 1));
            var firstBurstCount = 0;
            var secondBurstCount = 0;
            var thirdBurstCount = 0;

            for (var index = 0; index < effectRoot.childCount; index++)
            {
                var particle = effectRoot.GetChild(index);
                var renderer = particle.GetComponent<SpriteRenderer>();

                Assert.That(renderer.color, Is.EqualTo(Color.white));

                var distanceToFirst = Vector2.Distance(particle.position, firstCenter);
                var distanceToSecond = Vector2.Distance(particle.position, secondCenter);
                var distanceToThird = Vector2.Distance(particle.position, thirdCenter);
                Assert.That(Mathf.Min(distanceToFirst, Mathf.Min(distanceToSecond, distanceToThird)), Is.LessThan(0.2f));

                if (distanceToFirst <= distanceToSecond && distanceToFirst <= distanceToThird)
                {
                    Assert.That(renderer.sprite, Is.SameAs(redSprite));
                    firstBurstCount++;
                }
                else if (distanceToSecond <= distanceToThird)
                {
                    Assert.That(renderer.sprite, Is.SameAs(redSprite));
                    secondBurstCount++;
                }
                else
                {
                    Assert.That(renderer.sprite, Is.SameAs(blueSprite));
                    thirdBurstCount++;
                }
            }

            Assert.That(firstBurstCount, Is.EqualTo(3));
            Assert.That(secondBurstCount, Is.EqualTo(3));
            Assert.That(thirdBurstCount, Is.EqualTo(3));

            player.Advance(0.1f);

            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(9));

            player.Advance(5f);

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

        private CubeBlastParticlePlayer CreatePlayer(out Transform effectRoot, out Sprite redSprite, out Sprite blueSprite)
        {
            var host = CreateGameObject("CubeBlastParticlePlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var player = host.AddComponent<CubeBlastParticlePlayer>();
            redSprite = CreateSprite(20, 16, 20f);
            blueSprite = CreateSprite(22, 16, 22f);
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.18f);
            SetField(player, "particlesPerBurst", 3);
            SetField(player, "redParticleSprite", redSprite);
            SetField(player, "greenParticleSprite", CreateSprite(21, 16, 21f));
            SetField(player, "blueParticleSprite", blueSprite);
            SetField(player, "yellowParticleSprite", CreateSprite(23, 16, 23f));
            return player;
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
