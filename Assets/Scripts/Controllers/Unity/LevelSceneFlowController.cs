using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamBlastClone.Controllers.Unity
{
    public class LevelSceneFlowController : MonoBehaviour
    {
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private string levelSceneName = "LevelScene";
        [SerializeField] private string mainSceneName = "MainScene";

        private readonly CurrentLevelStore currentLevelStore = new CurrentLevelStore();
        private bool hasCompletedWinFlow;

        public void ReturnToMainScene()
        {
            LoadScene(mainSceneName);
        }

        public void RetryCurrentLevel()
        {
            LoadScene(levelSceneName);
        }

        public void CompleteWinAndReturnToMainScene()
        {
            if (hasCompletedWinFlow)
            {
                return;
            }

            hasCompletedWinFlow = true;

            if (levelCatalog is not null && levelCatalog.LevelCount > 0)
            {
                currentLevelStore.AdvanceOnWin(levelCatalog.LevelCount);
            }

            ReturnToMainScene();
        }

        protected virtual void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
