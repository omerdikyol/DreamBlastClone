using System;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class CurrentLevelStore
    {
        private const string DefaultPlayerPrefsKey = "DreamBlastClone.CurrentLevel";
        private readonly string playerPrefsKey;

        public CurrentLevelStore(string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            this.playerPrefsKey = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? throw new ArgumentException("PlayerPrefs key cannot be null or whitespace.", nameof(playerPrefsKey))
                : playerPrefsKey;
        }

        public int GetCurrentLevel()
        {
            return Math.Max(1, PlayerPrefs.GetInt(playerPrefsKey, 1));
        }

        public int GetPlayableLevel(int maxLevelCount)
        {
            if (maxLevelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevelCount), maxLevelCount, "Maximum level count must be at least 1.");
            }

            return Math.Clamp(GetCurrentLevel(), 1, maxLevelCount);
        }

        public bool IsFinished(int maxLevelCount)
        {
            if (maxLevelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevelCount), maxLevelCount, "Maximum level count must be at least 1.");
            }

            return GetCurrentLevel() > maxLevelCount;
        }

        public void SetCurrentLevel(int levelNumber)
        {
            if (levelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber), levelNumber, "Current level must be at least 1.");
            }

            PlayerPrefs.SetInt(playerPrefsKey, levelNumber);
            PlayerPrefs.Save();
        }

        public void AdvanceOnWin(int maxLevelCount)
        {
            if (maxLevelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevelCount), maxLevelCount, "Maximum level count must be at least 1.");
            }

            SetCurrentLevel(Math.Min(GetCurrentLevel() + 1, maxLevelCount + 1));
        }
    }
}
