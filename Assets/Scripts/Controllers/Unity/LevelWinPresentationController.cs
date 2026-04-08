using DreamBlastClone.Controllers;
using DreamBlastClone.Systems;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelWinPresentationController : MonoBehaviour
    {
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private LevelSceneFlowController flowController;
        [SerializeField] private GameObject presentationRoot;
        [SerializeField] private float durationSeconds = 1f;
        [SerializeField] private int presentationSortingOrder = 110;

        private bool hasStartedWinFlow;
        private float remainingSeconds;

        private void Awake()
        {
            ResetPresentation();
        }

        private void OnEnable()
        {
            ResetPresentation();

            if (!TryResolveDependencies())
            {
                return;
            }

            ConfigurePresentationRoot();
            inputBridge.SetInputSuppressed(false);
            inputBridge.TapProcessed += HandleTapProcessed;
        }

        private void OnDisable()
        {
            if (inputBridge is not null)
            {
                inputBridge.TapProcessed -= HandleTapProcessed;
                inputBridge.SetInputSuppressed(false);
            }
        }

        private void Update()
        {
            AdvanceWinPresentation(Time.unscaledDeltaTime);
        }

        private void HandleTapProcessed(LevelSessionTapResult result)
        {
            if (result is null || result.LevelState != LevelState.Win || hasStartedWinFlow)
            {
                return;
            }

            hasStartedWinFlow = true;
            remainingSeconds = Mathf.Max(0f, durationSeconds);
            presentationRoot.transform.SetAsLastSibling();
            presentationRoot.SetActive(true);
            inputBridge.SetInputSuppressed(true);

            if (remainingSeconds <= 0f)
            {
                CompleteWinFlow();
            }
        }

        private void AdvanceWinPresentation(float deltaTime)
        {
            if (!hasStartedWinFlow)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - deltaTime);
            if (remainingSeconds > 0f)
            {
                return;
            }

            CompleteWinFlow();
        }

        private void CompleteWinFlow()
        {
            if (!hasStartedWinFlow)
            {
                return;
            }

            hasStartedWinFlow = false;
            flowController.CompleteWinAndReturnToMainScene();
        }

        private void ResetPresentation()
        {
            hasStartedWinFlow = false;
            remainingSeconds = 0f;

            if (presentationRoot is not null)
            {
                presentationRoot.SetActive(false);
            }
        }

        private bool TryResolveDependencies()
        {
            var targetInputBridge = inputBridge is not null ? inputBridge : GetComponent<BoardInputSessionBridge>();
            var targetFlowController = flowController is not null ? flowController : GetComponent<LevelSceneFlowController>();

            if (targetInputBridge is null || targetFlowController is null || presentationRoot is null)
            {
                Debug.LogError($"{nameof(LevelWinPresentationController)} is missing required references.", this);
                return false;
            }

            inputBridge = targetInputBridge;
            flowController = targetFlowController;
            return true;
        }

        private void ConfigurePresentationRoot()
        {
            presentationRoot.transform.SetAsLastSibling();

            if (!presentationRoot.TryGetComponent<Canvas>(out var presentationCanvas))
            {
                Debug.LogError($"{nameof(LevelWinPresentationController)} requires {nameof(presentationRoot)} to have a {nameof(Canvas)}.", presentationRoot);
                return;
            }

            presentationCanvas.overrideSorting = true;
            presentationCanvas.sortingOrder = presentationSortingOrder;
        }
    }
}
