using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class SpecialItemComboPresentationPlayerTests
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
        public void TryPlayRocketRocketCreatesCrossSweepsAndFlashThenCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new SpecialItemComboPresentationDescriptor(
                DreamBlastClone.Systems.SpecialItemComboType.RocketRocket,
                new BoardCoordinate(2, 2),
                new[]
                {
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(2, 3)
                },
                new[]
                {
                    new BoardCoordinate(2, 0),
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(0, 2),
                    new BoardCoordinate(1, 2),
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(3, 2),
                    new BoardCoordinate(4, 2),
                    new BoardCoordinate(2, 3),
                    new BoardCoordinate(2, 4)
                },
                new[]
                {
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Horizontal, new BoardCoordinate(2, 2), new BoardCoordinate(0, 2), new BoardCoordinate(4, 2)),
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Vertical, new BoardCoordinate(2, 2), new BoardCoordinate(2, 0), new BoardCoordinate(2, 4))
                });

            Assert.That(player.TryPlay(boardView, descriptor), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(player.Duration, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboRocketSweep_"), Is.EqualTo(2));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboFlash_"), Is.EqualTo(9));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboTntPulse"), Is.EqualTo(0));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayTntTntCreatesPulseAndAffectedCellFlashesThenCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new SpecialItemComboPresentationDescriptor(
                DreamBlastClone.Systems.SpecialItemComboType.TntTnt,
                new BoardCoordinate(1, 1),
                new[]
                {
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(1, 2)
                },
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
                System.Array.Empty<SingleRocketActivationEffectDescriptor>());

            Assert.That(player.TryPlay(boardView, descriptor), Is.True);
            Assert.That(player.Duration, Is.EqualTo(0.26f).Within(0.0001f));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboRocketSweep_"), Is.EqualTo(0));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboFlash_"), Is.EqualTo(9));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboTntPulse"), Is.EqualTo(1));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboTntExplosion"), Is.EqualTo(1));

            player.Advance(player.Duration);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayTntRocketCreatesSixSweepsPulseAndFlashesThenCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new SpecialItemComboPresentationDescriptor(
                DreamBlastClone.Systems.SpecialItemComboType.TntRocket,
                new BoardCoordinate(2, 2),
                new[]
                {
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(2, 3)
                },
                new[]
                {
                    new BoardCoordinate(1, 0),
                    new BoardCoordinate(2, 0),
                    new BoardCoordinate(3, 0),
                    new BoardCoordinate(0, 1),
                    new BoardCoordinate(1, 1),
                    new BoardCoordinate(2, 1),
                    new BoardCoordinate(3, 1),
                    new BoardCoordinate(4, 1),
                    new BoardCoordinate(0, 2),
                    new BoardCoordinate(1, 2),
                    new BoardCoordinate(2, 2),
                    new BoardCoordinate(3, 2),
                    new BoardCoordinate(4, 2),
                    new BoardCoordinate(0, 3),
                    new BoardCoordinate(1, 3),
                    new BoardCoordinate(2, 3),
                    new BoardCoordinate(3, 3),
                    new BoardCoordinate(4, 3),
                    new BoardCoordinate(1, 4),
                    new BoardCoordinate(2, 4),
                    new BoardCoordinate(3, 4)
                },
                new[]
                {
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Horizontal, new BoardCoordinate(2, 1), new BoardCoordinate(0, 1), new BoardCoordinate(4, 1)),
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Horizontal, new BoardCoordinate(2, 2), new BoardCoordinate(0, 2), new BoardCoordinate(4, 2)),
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Horizontal, new BoardCoordinate(2, 3), new BoardCoordinate(0, 3), new BoardCoordinate(4, 3)),
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Vertical, new BoardCoordinate(1, 2), new BoardCoordinate(1, 0), new BoardCoordinate(1, 4)),
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Vertical, new BoardCoordinate(2, 2), new BoardCoordinate(2, 0), new BoardCoordinate(2, 4)),
                    new SingleRocketActivationEffectDescriptor(RocketOrientation.Vertical, new BoardCoordinate(3, 2), new BoardCoordinate(3, 0), new BoardCoordinate(3, 4))
                });

            Assert.That(player.TryPlay(boardView, descriptor), Is.True);
            Assert.That(player.Duration, Is.EqualTo(0.24f).Within(0.0001f));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboRocketSweep_"), Is.EqualTo(6));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboFlash_"), Is.EqualTo(21));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboTntPulse"), Is.EqualTo(1));
            Assert.That(CountChildrenByPrefix(effectRoot, "ComboTntExplosion"), Is.EqualTo(1));
            Assert.That(CountDescendantsByPrefix(effectRoot, "RocketParticleStar_"), Is.GreaterThan(0));
            Assert.That(CountDescendantsByPrefix(effectRoot, "RocketParticleSmoke_"), Is.GreaterThan(0));

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

        private SpecialItemComboPresentationPlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("ComboPresentationPlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var player = host.AddComponent<SpecialItemComboPresentationPlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "horizontalRocketPartLeftSprite", CreateSprite(18, 18, 18f));
            SetField(player, "horizontalRocketPartRightSprite", CreateSprite(19, 18, 19f));
            SetField(player, "verticalRocketPartTopSprite", CreateSprite(18, 19, 18f));
            SetField(player, "verticalRocketPartBottomSprite", CreateSprite(19, 19, 19f));
            SetField(player, "rocketParticleStarSprite", CreateSprite(14, 14, 20f));
            SetField(player, "rocketParticleSmokeSprite", CreateSprite(20, 20, 20f));
            SetField(player, "tntBurstSprite", CreateSprite(24, 24, 24f));
            SetField(player, "tntDebrisSprite", CreateSprite(26, 24, 24f));
            SetField(player, "rocketRocketDuration", 0.22f);
            SetField(player, "tntRocketDuration", 0.24f);
            SetField(player, "tntTntDuration", 0.26f);
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

        private static int CountDescendantsByPrefix(Transform root, string prefix)
        {
            var count = 0;
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    count++;
                }

                count += CountDescendantsByPrefix(child, prefix);
            }

            return count;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
