using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class AudioSettingsPopupControllerTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("DreamBlastClone.Audio.MusicVolume");
            PlayerPrefs.DeleteKey("DreamBlastClone.Audio.SfxVolume");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("DreamBlastClone.Audio.MusicVolume");
            PlayerPrefs.DeleteKey("DreamBlastClone.Audio.SfxVolume");
            PlayerPrefs.Save();

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
        public void TryOpenLoadsSliderValuesFromAudioController()
        {
            var audioController = CreateGameObject("AudioController").AddComponent<GameAudioController>();
            audioController.SetMusicUserVolume(0.3f);
            audioController.SetSfxUserVolume(0.6f);

            var popup = CreatePopup(audioController);

            Assert.That(popup.TryOpen(), Is.True);

            Assert.That(FindSlider(popup, "MusicSlider").value, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(FindSlider(popup, "SfxSlider").value, Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void SliderChangesUpdateAudioImmediatelyAndPersist()
        {
            var audioController = CreateGameObject("AudioController").AddComponent<GameAudioController>();
            var popup = CreatePopup(audioController);
            popup.TryOpen();

            FindSlider(popup, "MusicSlider").value = 0.25f;
            FindSlider(popup, "SfxSlider").value = 0.45f;

            var store = new AudioSettingsStore();
            Assert.That(audioController.MusicUserVolume, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(audioController.SfxUserVolume, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(store.GetMusicVolume(), Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(store.GetSfxVolume(), Is.EqualTo(0.45f).Within(0.001f));
        }

        [Test]
        public void CloseRestoresLevelInputSuppression()
        {
            var audioController = CreateGameObject("AudioController").AddComponent<GameAudioController>();
            var bridge = CreateGameObject("InputBridge").AddComponent<BoardInputSessionBridge>();
            var popup = CreatePopup(audioController, bridge);
            SetField(popup, "enterDurationSeconds", 0f);
            SetField(popup, "exitDurationSeconds", 0f);

            Assert.That(popup.TryOpen(), Is.True);
            Assert.That(bridge.IsInputSuppressed, Is.True);

            Assert.That(popup.TryClose(), Is.True);
            Assert.That(bridge.IsInputSuppressed, Is.False);
            Assert.That(popup.IsOpen, Is.False);
        }

        private AudioSettingsPopupController CreatePopup(GameAudioController audioController, BoardInputSessionBridge inputBridge = null)
        {
            var host = CreateGameObject("SettingsPopUp", typeof(RectTransform));
            CreateRequiredPopupHierarchy(host.transform);
            var popup = host.AddComponent<AudioSettingsPopupController>();
            popup.Configure(audioController, inputBridge);
            return popup;
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

        private static Slider FindSlider(AudioSettingsPopupController popup, string name)
        {
            var sliders = popup.GetComponentsInChildren<Slider>(true);
            foreach (var slider in sliders)
            {
                if (slider.name == name)
                {
                    return slider;
                }
            }

            Assert.Fail($"Could not find slider '{name}'.");
            return null;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
