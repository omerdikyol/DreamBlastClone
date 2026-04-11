using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class ChaliceBoxParticlePlayerTests
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
        public void TryPlayCreatesDoorDamageParticlesNearFootprintCenterAndCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out _, out _);
            var descriptor = new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(1, 2), ChaliceBoxParticleEventType.DoorDamage, amount: 1)
            });

            Assert.That(player.TryPlay(boardView, descriptor, destroyBelowWorldY: -2f), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(4));

            var center = GetFootprintCenter(boardView, new BoardCoordinate(1, 2));
            var firstChild = effectRoot.GetChild(0);
            Assert.That(firstChild.position.x, Is.EqualTo(center.x).Within(0.35f));
            Assert.That(firstChild.position.y, Is.EqualTo(center.y).Within(0.35f));

            player.Advance(0.1f);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(4));

            player.Advance(5f);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayDoorBreakUsesBothDoorAndChaliceSprites()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out var doorSprites, out var chaliceSprites);
            var descriptor = new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.DoorBreak, amount: 1)
            });

            Assert.That(player.TryPlay(boardView, descriptor, destroyBelowWorldY: -2f), Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(4));

            var usedSprites = effectRoot.GetComponentsInChildren<SpriteRenderer>().Select(renderer => renderer.sprite).ToArray();
            Assert.That(usedSprites, Has.Some.SameAs(doorSprites[0]));
            Assert.That(usedSprites, Has.Some.SameAs(chaliceSprites[0]));
        }

        [Test]
        public void TryPlayCreatesLargerBurstForCompletionThanDamage()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out _, out _);

            Assert.That(player.TryPlay(boardView, new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.ChaliceDamage, amount: 2)
            }), destroyBelowWorldY: -2f), Is.True);
            var damageCount = effectRoot.childCount;

            player.Stop();

            Assert.That(player.TryPlay(boardView, new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.ChaliceComplete, amount: 2)
            }), destroyBelowWorldY: -2f), Is.True);
            var completionCount = effectRoot.childCount;

            Assert.That(damageCount, Is.EqualTo(7));
            Assert.That(completionCount, Is.EqualTo(18));
            Assert.That(completionCount, Is.GreaterThan(damageCount));
        }

        [Test]
        public void TryPlayUsesDedicatedChaliceDamageSpriteWhenAssigned()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot, out _, out _);
            var chaliceDamageSprite = CreateSprite(30, 18, 20f);
            SetField(player, "chaliceDamageSprite", chaliceDamageSprite);

            Assert.That(player.TryPlay(boardView, new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.ChaliceDamage, amount: 2)
            }), destroyBelowWorldY: -2f), Is.True);

            Assert.That(effectRoot.childCount, Is.EqualTo(2));
            var usedSprites = effectRoot.GetComponentsInChildren<SpriteRenderer>().Select(renderer => renderer.sprite).ToArray();
            Assert.That(usedSprites, Is.All.SameAs(chaliceDamageSprite));
        }

        [Test]
        public void TryPlayUsesEveryAssignedSpriteForMajorBurstsOnly()
        {
            var boardView = CreateBoardView();
            var host = CreateGameObject("ChaliceBoxParticlePlayer");
            var effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var doorSprites = new[]
            {
                CreateSprite(20, 20, 20f),
                CreateSprite(21, 21, 20f),
                CreateSprite(22, 22, 20f),
                CreateSprite(23, 23, 20f)
            };
            var chaliceSprites = new[]
            {
                CreateSprite(24, 24, 20f),
                CreateSprite(25, 25, 20f),
                CreateSprite(26, 26, 20f)
            };

            var player = host.AddComponent<ChaliceBoxParticlePlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "doorPhaseSprites", doorSprites);
            SetField(player, "chalicePhaseSprites", chaliceSprites);
            SetField(player, "doorBreakDoorBurstCount", 1);
            SetField(player, "doorBreakChaliceBurstCount", 1);
            SetField(player, "doorDamageBurstCount", 1);
            SetField(player, "chaliceCompleteBurstCount", 1);

            Assert.That(player.TryPlay(boardView, new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.DoorDamage, amount: 1)
            }), destroyBelowWorldY: -2f), Is.True);

            var damageUsedSprites = effectRoot.GetComponentsInChildren<SpriteRenderer>().Select(renderer => renderer.sprite).ToArray();
            Assert.That(damageUsedSprites.Length, Is.EqualTo(1));
            Assert.That(damageUsedSprites, Is.Not.SupersetOf(doorSprites));

            player.Stop();

            Assert.That(player.TryPlay(boardView, new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.DoorBreak, amount: 1)
            }), destroyBelowWorldY: -2f), Is.True);

            var breakUsedSprites = effectRoot.GetComponentsInChildren<SpriteRenderer>().Select(renderer => renderer.sprite).ToArray();
            Assert.That(breakUsedSprites, Is.SupersetOf(doorSprites));
            Assert.That(breakUsedSprites, Is.SupersetOf(chaliceSprites));

            player.Stop();

            Assert.That(player.TryPlay(boardView, new ChaliceBoxParticleDescriptor(new[]
            {
                new ChaliceBoxParticleEvent(new BoardCoordinate(0, 0), ChaliceBoxParticleEventType.ChaliceComplete, amount: 2)
            }), destroyBelowWorldY: -2f), Is.True);

            var completionUsedSprites = effectRoot.GetComponentsInChildren<SpriteRenderer>().Select(renderer => renderer.sprite).ToArray();
            Assert.That(completionUsedSprites, Is.SupersetOf(chaliceSprites));
        }

        private BoardView CreateBoardView()
        {
            var host = CreateGameObject("BoardViewHost");
            var boardView = host.AddComponent<BoardView>();
            SetField(boardView, "origin", Vector2.zero);
            SetField(boardView, "cellSize", 1f);
            return boardView;
        }

        private ChaliceBoxParticlePlayer CreatePlayer(
            out Transform effectRoot,
            out Sprite[] doorSprites,
            out Sprite[] chaliceSprites)
        {
            var host = CreateGameObject("ChaliceBoxParticlePlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            doorSprites = new[]
            {
                CreateSprite(24, 24, 20f),
                CreateSprite(20, 18, 20f)
            };
            chaliceSprites = new[]
            {
                CreateSprite(28, 20, 20f),
                CreateSprite(16, 24, 20f)
            };

            var player = host.AddComponent<ChaliceBoxParticlePlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.36f);
            SetField(player, "doorPhaseSprites", doorSprites);
            SetField(player, "chaliceDamageSprite", null);
            SetField(player, "chalicePhaseSprites", chaliceSprites);
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

        private static Vector3 GetFootprintCenter(BoardView boardView, BoardCoordinate anchor)
        {
            return (boardView.GetCellCenterWorld(anchor)
                + boardView.GetCellCenterWorld(anchor.Offset(1, 0))
                + boardView.GetCellCenterWorld(anchor.Offset(0, 1))
                + boardView.GetCellCenterWorld(anchor.Offset(1, 1))) / 4f;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
