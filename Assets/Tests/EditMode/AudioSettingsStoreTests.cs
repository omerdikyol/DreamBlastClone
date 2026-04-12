using DreamBlastClone.Controllers.Unity;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class AudioSettingsStoreTests
    {
        private const string MusicKey = "DreamBlastClone.Tests.Audio.Music";
        private const string SfxKey = "DreamBlastClone.Tests.Audio.Sfx";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(MusicKey);
            PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(MusicKey);
            PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void DefaultsToFullVolume()
        {
            var store = new AudioSettingsStore(MusicKey, SfxKey);

            Assert.That(store.GetMusicVolume(), Is.EqualTo(1f));
            Assert.That(store.GetSfxVolume(), Is.EqualTo(1f));
        }

        [Test]
        public void ClampsAndPersistsNormalizedValues()
        {
            var store = new AudioSettingsStore(MusicKey, SfxKey);

            store.SetMusicVolume(1.4f);
            store.SetSfxVolume(-0.2f);

            var reloaded = new AudioSettingsStore(MusicKey, SfxKey);
            Assert.That(reloaded.GetMusicVolume(), Is.EqualTo(1f));
            Assert.That(reloaded.GetSfxVolume(), Is.EqualTo(0f));
        }
    }
}
