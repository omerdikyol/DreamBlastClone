using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class UIButtonClickSoundPlayerTests
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
        public void ButtonClickPlaysMenuButtonSound()
        {
            var root = CreateGameObject("Root");
            var audioController = root.AddComponent<TestRecordingGameAudioController>();
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonClickSoundPlayer));
            createdObjects.Add(buttonObject);
            buttonObject.transform.SetParent(root.transform, false);
            var button = buttonObject.GetComponent<Button>();

            button.onClick.Invoke();

            Assert.That(audioController.PlayedSfx, Is.EqualTo(new[] { GameSfxCue.MenuButtonClick }));
        }

        [Test]
        public void OnDisableUnsubscribesCleanly()
        {
            var root = CreateGameObject("Root");
            var audioController = root.AddComponent<TestRecordingGameAudioController>();
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonClickSoundPlayer));
            createdObjects.Add(buttonObject);
            buttonObject.transform.SetParent(root.transform, false);
            var button = buttonObject.GetComponent<Button>();
            var player = buttonObject.GetComponent<UIButtonClickSoundPlayer>();

            InvokeMethod(player, "OnDisable");
            button.onClick.Invoke();

            Assert.That(audioController.PlayedSfx, Is.Empty);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }
    }
}
