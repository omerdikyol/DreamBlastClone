using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DreamBlastClone.Views;

namespace DreamBlastClone.Controllers.Unity
{
    public class MainSceneLauncher : MonoBehaviour
    {
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private Button startButton;
        [SerializeField] private MainSceneButtonIdleLoopView startButtonIdleLoop;
        [SerializeField] private Component levelLabel;
        [SerializeField] private string levelSceneName = "LevelScene";

        private readonly CurrentLevelStore currentLevelStore = new CurrentLevelStore();
        private bool hasStartedLevelLoad;

        public int CurrentLevelNumber { get; private set; }
        public bool IsFinished { get; private set; }

        private void Awake()
        {
            RefreshCurrentLevel();
            RefreshUi();
        }

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStartButtonClicked);
            }

            RefreshCurrentLevel();
            RefreshUi();
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartButtonClicked);
            }

            if (startButtonIdleLoop != null)
            {
                startButtonIdleLoop.StopAndReset();
            }
        }

        public void RefreshUi()
        {
            if (levelLabel != null)
            {
                SetLabelText(IsFinished ? "Finished" : $"Level {CurrentLevelNumber}");
            }

            if (startButton != null)
            {
                startButton.interactable = !hasStartedLevelLoad && HasValidCatalog() && !IsFinished;
            }

            RefreshStartButtonIdleLoop();
        }

        public bool TryStartCurrentLevel()
        {
            if (hasStartedLevelLoad || !HasValidCatalog())
            {
                return false;
            }

            RefreshCurrentLevel();

            if (IsFinished)
            {
                RefreshUi();
                return false;
            }

            hasStartedLevelLoad = true;
            RefreshUi();
            LoadScene(levelSceneName);
            return true;
        }

        public void RefreshForInspector()
        {
            RefreshCurrentLevel();
            RefreshUi();
        }

        protected virtual void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private void RefreshCurrentLevel()
        {
            if (!HasValidCatalog())
            {
                CurrentLevelNumber = 1;
                IsFinished = false;
                return;
            }

            CurrentLevelNumber = currentLevelStore.GetPlayableLevel(levelCatalog.LevelCount);
            IsFinished = currentLevelStore.IsFinished(levelCatalog.LevelCount);
        }

        private bool HasValidCatalog()
        {
            return levelCatalog != null && levelCatalog.LevelCount > 0;
        }

        private void HandleStartButtonClicked()
        {
            TryStartCurrentLevel();
        }

        private void RefreshStartButtonIdleLoop()
        {
            if (startButtonIdleLoop == null && startButton != null)
            {
                startButtonIdleLoop = startButton.GetComponent<MainSceneButtonIdleLoopView>();
            }

            if (startButtonIdleLoop == null)
            {
                return;
            }

            if (startButton != null && startButton.interactable)
            {
                startButtonIdleLoop.Play();
                return;
            }

            startButtonIdleLoop.StopAndReset();
        }

        private void SetLabelText(string value)
        {
            var textProperty = levelLabel.GetType().GetProperty("text");

            if (textProperty?.CanWrite == true && textProperty.PropertyType == typeof(string))
            {
                textProperty.SetValue(levelLabel, value);
            }
        }
    }
}
