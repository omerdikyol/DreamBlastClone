using System;
using DreamBlastClone.Controllers;
using DreamBlastClone.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamBlastClone.Controllers.Unity
{
    public class LevelSceneFlowController : MonoBehaviour
    {
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private string mainSceneName = "MainScene";

        private readonly CurrentLevelStore currentLevelStore = new CurrentLevelStore();

        private void OnEnable()
        {
            var targetInputBridge = inputBridge is not null ? inputBridge : GetComponent<BoardInputSessionBridge>();

            if (targetInputBridge is null)
            {
                return;
            }

            inputBridge = targetInputBridge;
            inputBridge.TapProcessed += HandleTapProcessed;
        }

        private void OnDisable()
        {
            if (inputBridge is not null)
            {
                inputBridge.TapProcessed -= HandleTapProcessed;
            }
        }

        public void HandleTapProcessed(LevelSessionTapResult result)
        {
            if (result is null || result.LevelState == LevelState.Continue)
            {
                return;
            }

            if (result.LevelState == LevelState.Win && levelCatalog is not null && levelCatalog.LevelCount > 0)
            {
                currentLevelStore.AdvanceOnWin(levelCatalog.LevelCount);
            }

            LoadScene(mainSceneName);
        }

        protected virtual void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
