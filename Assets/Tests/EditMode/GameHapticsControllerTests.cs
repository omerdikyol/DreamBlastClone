using System.Collections.Generic;
using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class GameHapticsControllerTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void UnsupportedBackendFailsSafely()
        {
            var controller = CreateGameObject("Haptics").AddComponent<TestGameHapticsController>();
            controller.Configure(isSupported: false, shouldPlaySucceed: false);

            Assert.That(controller.Play(GameHapticCue.Light), Is.False);
            Assert.That(controller.PlatformPlayCount, Is.EqualTo(0));
        }

        [Test]
        public void RepeatedLightCueIsCooldownLimited()
        {
            var controller = CreateGameObject("Haptics").AddComponent<TestGameHapticsController>();
            controller.Configure(isSupported: true, shouldPlaySucceed: true);

            Assert.That(controller.Play(GameHapticCue.Light), Is.True);
            Assert.That(controller.Play(GameHapticCue.Light), Is.False);
            Assert.That(controller.PlatformPlayCount, Is.EqualTo(1));
        }

        [Test]
        public void StrongerCueOverridesWeakerRecentCue()
        {
            var controller = CreateGameObject("Haptics").AddComponent<TestGameHapticsController>();
            controller.Configure(isSupported: true, shouldPlaySucceed: true);

            Assert.That(controller.Play(GameHapticCue.Light), Is.True);
            Assert.That(controller.Play(GameHapticCue.Heavy), Is.True);
            Assert.That(controller.PlayedCues, Is.EqualTo(new[] { GameHapticCue.Light, GameHapticCue.Heavy }));
        }

        [Test]
        public void WeakerCueDoesNotOverrideRecentStrongerCue()
        {
            var controller = CreateGameObject("Haptics").AddComponent<TestGameHapticsController>();
            controller.Configure(isSupported: true, shouldPlaySucceed: true);

            Assert.That(controller.Play(GameHapticCue.Heavy), Is.True);
            Assert.That(controller.Play(GameHapticCue.Light), Is.False);
            Assert.That(controller.PlatformPlayCount, Is.EqualTo(1));
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class TestGameHapticsController : GameHapticsController
        {
            private bool isSupported;
            private bool shouldPlaySucceed;

            public int PlatformPlayCount { get; private set; }

            public List<GameHapticCue> PlayedCues { get; } = new List<GameHapticCue>();

            public void Configure(bool isSupported, bool shouldPlaySucceed)
            {
                this.isSupported = isSupported;
                this.shouldPlaySucceed = shouldPlaySucceed;
            }

            protected override bool IsPlatformHapticsSupported()
            {
                return isSupported;
            }

            protected override bool TryPlayPlatformHaptic(GameHapticCue cue)
            {
                if (!shouldPlaySucceed)
                {
                    return false;
                }

                PlatformPlayCount++;
                PlayedCues.Add(cue);
                return true;
            }
        }
    }
}
