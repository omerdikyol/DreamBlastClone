using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class StoneParticlePlayerTests
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
        public void TryPlayCreatesParticlesAtRemovedStoneCoordinateAndCleansUp()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new StoneParticleDescriptor(new[]
            {
                new BoardCoordinate(1, 2)
            });

            Assert.That(player.TryPlay(boardView, descriptor, destroyBelowWorldY: -2f), Is.True);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(8));

            var firstChild = effectRoot.GetChild(0);
            Assert.That(firstChild.position.x, Is.EqualTo(boardView.GetCellCenterWorld(new BoardCoordinate(1, 2)).x).Within(0.2f));
            Assert.That(firstChild.position.y, Is.EqualTo(boardView.GetCellCenterWorld(new BoardCoordinate(1, 2)).y).Within(0.2f));

            player.Advance(0.1f);
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(effectRoot.childCount, Is.EqualTo(8));

            player.Advance(5f);

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(effectRoot.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlayCreatesBurstPerRemovedStone()
        {
            var boardView = CreateBoardView();
            var player = CreatePlayer(out var effectRoot);
            var descriptor = new StoneParticleDescriptor(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(2, 1)
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

        private StoneParticlePlayer CreatePlayer(out Transform effectRoot)
        {
            var host = CreateGameObject("StoneParticlePlayer");
            effectRoot = CreateGameObject("EffectRoot").transform;
            effectRoot.SetParent(host.transform, false);

            var player = host.AddComponent<StoneParticlePlayer>();
            SetField(player, "effectRoot", effectRoot);
            SetField(player, "duration", 0.24f);
            SetField(player, "mainChunkSprite", CreateSprite(24, 24, 20f));
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
