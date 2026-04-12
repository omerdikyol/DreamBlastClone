using System;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class AudioSettingsStore
    {
        private const string DefaultMusicVolumeKey = "DreamBlastClone.Audio.MusicVolume";
        private const string DefaultSfxVolumeKey = "DreamBlastClone.Audio.SfxVolume";
        private readonly string musicVolumeKey;
        private readonly string sfxVolumeKey;

        public AudioSettingsStore(
            string musicVolumeKey = DefaultMusicVolumeKey,
            string sfxVolumeKey = DefaultSfxVolumeKey)
        {
            this.musicVolumeKey = string.IsNullOrWhiteSpace(musicVolumeKey)
                ? throw new ArgumentException("Music volume key cannot be null or whitespace.", nameof(musicVolumeKey))
                : musicVolumeKey;
            this.sfxVolumeKey = string.IsNullOrWhiteSpace(sfxVolumeKey)
                ? throw new ArgumentException("SFX volume key cannot be null or whitespace.", nameof(sfxVolumeKey))
                : sfxVolumeKey;
        }

        public float GetMusicVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(musicVolumeKey, 1f));
        }

        public float GetSfxVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(sfxVolumeKey, 1f));
        }

        public void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(musicVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        public void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(sfxVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
