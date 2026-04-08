using System;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class CurrentLevelDebugTool : MonoBehaviour
    {
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [Min(1)]
        [SerializeField] private int selectedLevel = 1;

        private readonly CurrentLevelStore currentLevelStore = new CurrentLevelStore();

        public LevelCatalogAsset LevelCatalog => levelCatalog;

        public int SelectedLevel => selectedLevel;

        public int StoredLevel => currentLevelStore.GetCurrentLevel();

        public int MaxLevelCount => levelCatalog is not null ? levelCatalog.LevelCount : 0;

        public bool IsFinished => MaxLevelCount > 0 && currentLevelStore.IsFinished(MaxLevelCount);

        public void LoadStoredLevelIntoSelection()
        {
            selectedLevel = MaxLevelCount > 0
                ? currentLevelStore.GetPlayableLevel(MaxLevelCount)
                : currentLevelStore.GetCurrentLevel();
        }

        public void ApplySelectedLevel()
        {
            selectedLevel = Math.Max(1, selectedLevel);

            var storedLevel = MaxLevelCount > 0
                ? Math.Clamp(selectedLevel, 1, MaxLevelCount)
                : selectedLevel;

            currentLevelStore.SetCurrentLevel(storedLevel);
            selectedLevel = storedLevel;
        }

        public void SetFirstLevel()
        {
            selectedLevel = 1;
            ApplySelectedLevel();
        }

        public void SetLastLevel()
        {
            if (MaxLevelCount < 1)
            {
                throw new InvalidOperationException($"{nameof(CurrentLevelDebugTool)} requires a populated {nameof(LevelCatalogAsset)} to set the last level.");
            }

            selectedLevel = MaxLevelCount;
            ApplySelectedLevel();
        }

        public void SetFinishedState()
        {
            if (MaxLevelCount < 1)
            {
                throw new InvalidOperationException($"{nameof(CurrentLevelDebugTool)} requires a populated {nameof(LevelCatalogAsset)} to set the finished state.");
            }

            currentLevelStore.SetCurrentLevel(MaxLevelCount + 1);
            selectedLevel = MaxLevelCount;
        }

        public void RefreshKnownUi()
        {
            if (TryGetComponent<MainSceneLauncher>(out var mainSceneLauncher))
            {
                mainSceneLauncher.RefreshForInspector();
            }
        }

        private void OnValidate()
        {
            selectedLevel = Math.Max(1, selectedLevel);
        }
    }
}
