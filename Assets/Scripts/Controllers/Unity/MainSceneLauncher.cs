using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DreamBlastClone.Views;

namespace DreamBlastClone.Controllers.Unity
{
    public class MainSceneLauncher : MonoBehaviour
    {
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private Button startButton;
        [FormerlySerializedAs("startButtonIdleLoop")]
        [SerializeField] private UIButtonFeedbackView startButtonFeedback;
        [SerializeField] private GameAudioController audioController;
        [SerializeField] private Transform audioSettingsCanvasRoot;
        [SerializeField] private AudioSettingsPopupController audioSettingsPopupPrefab;
        [SerializeField] private Component levelLabel;
        [SerializeField] private string levelSceneName = "LevelScene";

        private readonly CurrentLevelStore currentLevelStore = new CurrentLevelStore();
        private bool hasStartedLevelLoad;

        public int CurrentLevelNumber { get; private set; }
        public bool IsFinished { get; private set; }

        private void Awake()
        {
            ResolveAudioController();
            EnsureAudioSettingsPopup();
            RefreshCurrentLevel();
            RefreshUi();
        }

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStartButtonClicked);
            }

            ResolveAudioController();
            EnsureAudioSettingsPopup();
            EnsureStartButtonClickSoundPlayer();
            RefreshCurrentLevel();
            RefreshUi();
            audioController?.PlayMusic(GameMusicCue.MainMenu);
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartButtonClicked);
            }

            if (startButtonFeedback != null)
            {
                startButtonFeedback.StopAndReset();
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

            RefreshStartButtonFeedback();
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

        private void RefreshStartButtonFeedback()
        {
            if (startButtonFeedback == null && startButton != null)
            {
                startButtonFeedback = startButton.GetComponent<UIButtonFeedbackView>()
                    ?? startButton.gameObject.AddComponent<UIButtonFeedbackView>();
            }

            if (startButtonFeedback == null)
            {
                return;
            }

            if (startButton != null && startButton.interactable)
            {
                startButtonFeedback.PlayIdle();
                return;
            }

            startButtonFeedback.StopAndReset();
        }

        private void ResolveAudioController()
        {
            audioController ??= GetComponent<GameAudioController>() ?? gameObject.AddComponent<GameAudioController>();
        }

        private void EnsureAudioSettingsPopup()
        {
            var popupRoot = audioSettingsCanvasRoot != null
                ? audioSettingsCanvasRoot
                : startButton != null
                    ? startButton.GetComponentInParent<Canvas>()?.transform
                    : FindFirstObjectByType<Canvas>()?.transform;
            if (popupRoot == null)
            {
                return;
            }

            AudioSettingsPopupController.EnsureInstance(popupRoot, audioController, prefab: audioSettingsPopupPrefab);
        }

        private void EnsureStartButtonClickSoundPlayer()
        {
            if (startButton == null)
            {
                return;
            }

            _ = startButton.GetComponent<UIButtonClickSoundPlayer>() ?? startButton.gameObject.AddComponent<UIButtonClickSoundPlayer>();
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
