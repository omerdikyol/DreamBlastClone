using System;
using System.Collections.Generic;
using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class AudioSettingsOpenButtonTests
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
        public void ButtonClickOpensPopup()
        {
            var eventSystemObject = CreateGameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();

            var audioController = CreateGameObject("AudioController").AddComponent<GameAudioController>();
            var popupObject = CreateGameObject("Popup", typeof(RectTransform));
            CreateRequiredPopupHierarchy(popupObject.transform);
            var popup = popupObject.AddComponent<AudioSettingsPopupController>();
            popup.Configure(audioController);

            var buttonObject = CreateGameObject("SettingsButton", typeof(RectTransform));
            buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            var openButton = buttonObject.AddComponent<AudioSettingsOpenButton>();

            button.onClick.Invoke();

            Assert.That(openButton, Is.Not.Null);
            Assert.That(popup.IsOpen, Is.True);
        }

        private void CreateRequiredPopupHierarchy(Transform host)
        {
            var contentRoot = CreateGameObject("ContentRoot", typeof(RectTransform));
            contentRoot.transform.SetParent(host, false);

            var closeButton = CreateGameObject("CloseButton", typeof(RectTransform));
            closeButton.transform.SetParent(contentRoot.transform, false);
            closeButton.AddComponent<Image>();
            closeButton.AddComponent<Button>();

            var musicSlider = CreateGameObject("MusicSlider", typeof(RectTransform));
            musicSlider.transform.SetParent(contentRoot.transform, false);
            musicSlider.AddComponent<Slider>();

            var sfxSlider = CreateGameObject("SfxSlider", typeof(RectTransform));
            sfxSlider.transform.SetParent(contentRoot.transform, false);
            sfxSlider.AddComponent<Slider>();
        }

        private GameObject CreateGameObject(string name, params Type[] components)
        {
            var gameObject = new GameObject(name, components);
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
