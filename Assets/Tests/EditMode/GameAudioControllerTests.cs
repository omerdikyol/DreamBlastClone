using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class GameAudioControllerTests
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
        public void PlayMusicSetsRequestedCue()
        {
            var controller = CreateControllerWithBank();

            controller.PlayMusic(GameMusicCue.MainMenu);
            Assert.That(controller.CurrentMusicCue, Is.EqualTo(GameMusicCue.MainMenu));

            controller.PlayMusic(GameMusicCue.Gameplay);
            Assert.That(controller.CurrentMusicCue, Is.EqualTo(GameMusicCue.Gameplay));
        }

        [Test]
        public void CubeBlastCooldownSuppressesImmediateReplay()
        {
            var controller = CreateControllerWithBank();

            Assert.That(controller.PlaySfx(GameSfxCue.CubeBlast), Is.True);
            Assert.That(controller.PlaySfx(GameSfxCue.CubeBlast), Is.False);
        }

        [Test]
        public void ChaliceCollectVoiceCapLimitsConcurrentVoices()
        {
            var controller = CreateControllerWithBank();

            Assert.That(controller.PlaySfx(GameSfxCue.ChaliceCollect), Is.True);
            ResetCooldown(controller, GameSfxCue.ChaliceCollect);
            Assert.That(controller.PlaySfx(GameSfxCue.ChaliceCollect), Is.True);
            ResetCooldown(controller, GameSfxCue.ChaliceCollect);
            Assert.That(controller.PlaySfx(GameSfxCue.ChaliceCollect), Is.False);
            Assert.That(controller.CountActiveVoices(GameSfxCue.ChaliceCollect), Is.EqualTo(2));
        }

        [Test]
        public void MissingClipFailsSafely()
        {
            var controllerObject = CreateGameObject("AudioController");
            var controller = controllerObject.AddComponent<GameAudioController>();
            var bank = ScriptableObject.CreateInstance<GameAudioBank>();
            createdObjects.Add(bank);
            SetField(controller, "audioBank", bank);

            Assert.That(controller.PlaySfx(GameSfxCue.WinSting), Is.False);
        }

        private GameAudioController CreateControllerWithBank()
        {
            var controllerObject = CreateGameObject("AudioController");
            var controller = controllerObject.AddComponent<GameAudioController>();
            var bank = ScriptableObject.CreateInstance<GameAudioBank>();
            createdObjects.Add(bank);

            SetField(bank, "mainMenuMusic", CreateClip("main_menu_music"));
            SetField(bank, "gameplayMusic", CreateClip("gameplay_music"));
            SetField(bank, "cubeBlast", CreateClip("cube_blast"));
            SetField(bank, "chaliceCollect", CreateClip("chalice_collect"));
            SetField(bank, "winSting", CreateClip("win_sting"));
            SetField(controller, "audioBank", bank);
            SetField(controller, "sfxVoicePoolSize", 4);
            return controller;
        }

        private AudioClip CreateClip(string name)
        {
            var clip = AudioClip.Create(name, 44100, 1, 44100, stream: false);
            createdObjects.Add(clip);
            return clip;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void ResetCooldown(GameAudioController controller, GameSfxCue cue)
        {
            var dictionary = (Dictionary<GameSfxCue, float>)GetField(controller, "nextAllowedPlayTimes");
            dictionary[cue] = 0f;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            var field = FindField(target.GetType(), fieldName);
            return field.GetValue(target);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(fieldName);
        }
    }
}
