using DreamBlastClone.Controllers;
using DreamBlastClone.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelLosePopupController : MonoBehaviour
    {
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private LevelSceneFlowController flowController;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private int popupSortingOrder = 100;

        private bool hasShownLosePopup;

        private void Awake()
        {
            hasShownLosePopup = false;
            HidePopup();
        }

        private void OnEnable()
        {
            hasShownLosePopup = false;
            HidePopup();

            if (!TryResolveDependencies())
            {
                return;
            }

            ConfigurePopupRoot();
            inputBridge.SetInputSuppressed(false);
            inputBridge.TapProcessed += HandleTapProcessed;
            closeButton.onClick.AddListener(HandleCloseClicked);
            tryAgainButton.onClick.AddListener(HandleTryAgainClicked);
        }

        private void OnDisable()
        {
            if (inputBridge is not null)
            {
                inputBridge.TapProcessed -= HandleTapProcessed;
                inputBridge.SetInputSuppressed(false);
            }

            if (closeButton is not null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (tryAgainButton is not null)
            {
                tryAgainButton.onClick.RemoveListener(HandleTryAgainClicked);
            }
        }

        private void HandleTapProcessed(LevelSessionTapResult result)
        {
            if (result is null || result.LevelState != LevelState.Lose || hasShownLosePopup)
            {
                return;
            }

            ShowLosePopup();
        }

        public bool TryShowForDebug()
        {
            if (!TryResolveDependencies())
            {
                return false;
            }

            ConfigurePopupRoot();
            return ShowLosePopup();
        }

        private void HandleCloseClicked()
        {
            inputBridge.SetInputSuppressed(false);
            HidePopup();
            flowController.ReturnToMainScene();
        }

        private void HandleTryAgainClicked()
        {
            inputBridge.SetInputSuppressed(false);
            HidePopup();
            flowController.RetryCurrentLevel();
        }

        private void HidePopup()
        {
            if (popupRoot is not null)
            {
                popupRoot.SetActive(false);
            }
        }

        private bool ShowLosePopup()
        {
            if (hasShownLosePopup || popupRoot == null || inputBridge == null)
            {
                return false;
            }

            hasShownLosePopup = true;
            popupRoot.transform.SetAsLastSibling();
            popupRoot.SetActive(true);
            inputBridge.SetInputSuppressed(true);
            return true;
        }

        private bool TryResolveDependencies()
        {
            var targetInputBridge = inputBridge is not null ? inputBridge : GetComponent<BoardInputSessionBridge>();
            var targetFlowController = flowController is not null ? flowController : GetComponent<LevelSceneFlowController>();

            if (targetInputBridge is null
                || targetFlowController is null
                || popupRoot is null
                || closeButton is null
                || tryAgainButton is null)
            {
                Debug.LogError($"{nameof(LevelLosePopupController)} is missing required references.", this);
                return false;
            }

            inputBridge = targetInputBridge;
            flowController = targetFlowController;
            return true;
        }

        private void ConfigurePopupRoot()
        {
            popupRoot.transform.SetAsLastSibling();

            if (!popupRoot.TryGetComponent<Canvas>(out var popupCanvas))
            {
                Debug.LogError($"{nameof(LevelLosePopupController)} requires {nameof(popupRoot)} to have a {nameof(Canvas)}.", popupRoot);
                return;
            }

            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = popupSortingOrder;
        }
    }
}
