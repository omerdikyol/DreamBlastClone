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
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private CanvasGroup popupCanvasGroup;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform titleTransform;
        [SerializeField] private RectTransform tryAgainTextTransform;
        [SerializeField] private int popupSortingOrder = 100;
        [SerializeField] private float enterDurationSeconds = 0.2f;
        [SerializeField] private float exitDurationSeconds = 0.16f;
        [SerializeField] private float contentEnterOffsetY = 28f;
        [SerializeField] private float contentEnterScale = 0.94f;
        [SerializeField] private float overlayEnterAlphaMultiplier = 1f;
        [SerializeField] private float titleEnterOffsetY = 10f;
        [SerializeField] private float tryAgainPulseAmplitude = 0.05f;
        [SerializeField] private float tryAgainPulseFrequency = 1.8f;

        private bool hasShownLosePopup;
        private LosePopupState state;
        private PendingAction pendingAction;
        private float stateElapsedSeconds;
        private Vector2 contentBaseAnchoredPosition;
        private Vector2 titleBaseAnchoredPosition;
        private Vector3 contentBaseScale;
        private Vector3 tryAgainTextBaseScale;
        private Color overlayBaseColor;
        private Image overlayImage;

        private void Awake()
        {
            hasShownLosePopup = false;
            state = LosePopupState.Hidden;
            pendingAction = PendingAction.None;
            stateElapsedSeconds = 0f;

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            hasShownLosePopup = false;

            if (!TryResolveDependencies())
            {
                return;
            }

            ConfigurePopupRoot();
            CacheVisualDefaults();
            ResetPopup();
            inputBridge.SetInputSuppressed(false);
            inputBridge.TapProcessed += HandleTapProcessed;
            closeButton.onClick.AddListener(HandleCloseClicked);
            tryAgainButton.onClick.AddListener(HandleTryAgainClicked);
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
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

            if (mainMenuButton is not null)
            {
                mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            SetActionButtonsInteractable(false);
            state = LosePopupState.Hidden;
            pendingAction = PendingAction.None;
            stateElapsedSeconds = 0f;
        }

        private void Update()
        {
            AdvanceLosePresentation(Time.unscaledDeltaTime);
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
            CacheVisualDefaults();
            return ShowLosePopup();
        }

        private void HandleCloseClicked()
        {
            BeginClosing(PendingAction.MainMenu);
        }

        private void HandleTryAgainClicked()
        {
            BeginClosing(PendingAction.Retry);
        }

        private void HandleMainMenuClicked()
        {
            BeginClosing(PendingAction.MainMenu);
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
            BeginEntering();
            return true;
        }

        private void AdvanceLosePresentation(float deltaTime)
        {
            if (state == LosePopupState.Hidden)
            {
                return;
            }

            var clampedDeltaTime = Mathf.Max(0f, deltaTime);
            stateElapsedSeconds += clampedDeltaTime;

            switch (state)
            {
                case LosePopupState.Entering:
                    UpdateEnteringState();
                    break;
                case LosePopupState.Visible:
                    ApplyVisibleVisualState();
                    break;
                case LosePopupState.Closing:
                    UpdateClosingState();
                    break;
            }
        }

        private void BeginEntering()
        {
            state = LosePopupState.Entering;
            pendingAction = PendingAction.None;
            stateElapsedSeconds = 0f;
            SetActionButtonsInteractable(false);
            ApplyVisualState(
                rootAlpha: 0f,
                contentAlpha: 0f,
                contentYOffset: -contentEnterOffsetY,
                contentScaleMultiplier: contentEnterScale,
                titleYOffset: titleEnterOffsetY,
                overlayAlphaMultiplier: overlayEnterAlphaMultiplier,
                tryAgainScaleMultiplier: 1f);

            if (enterDurationSeconds <= 0f)
            {
                BeginVisible();
            }
        }

        private void BeginVisible()
        {
            state = LosePopupState.Visible;
            stateElapsedSeconds = 0f;
            ApplyVisibleVisualState();
            SetActionButtonsInteractable(true);
        }

        private void BeginClosing(PendingAction action)
        {
            if (state != LosePopupState.Visible)
            {
                return;
            }

            state = LosePopupState.Closing;
            pendingAction = action;
            stateElapsedSeconds = 0f;
            SetActionButtonsInteractable(false);

            if (exitDurationSeconds <= 0f)
            {
                CompletePendingAction();
            }
        }

        private void UpdateEnteringState()
        {
            var progress = enterDurationSeconds > 0f
                ? Mathf.Clamp01(stateElapsedSeconds / enterDurationSeconds)
                : 1f;
            var easedProgress = EaseOutCubic(progress);

            ApplyVisualState(
                rootAlpha: easedProgress,
                contentAlpha: Mathf.Lerp(0.82f, 1f, easedProgress),
                contentYOffset: Mathf.Lerp(-contentEnterOffsetY, 0f, easedProgress),
                contentScaleMultiplier: Mathf.Lerp(contentEnterScale, 1f, easedProgress),
                titleYOffset: Mathf.Lerp(titleEnterOffsetY, 0f, easedProgress),
                overlayAlphaMultiplier: Mathf.Lerp(overlayEnterAlphaMultiplier, 1f, easedProgress),
                tryAgainScaleMultiplier: 1f);

            if (progress >= 1f)
            {
                BeginVisible();
            }
        }

        private void UpdateClosingState()
        {
            var progress = exitDurationSeconds > 0f
                ? Mathf.Clamp01(stateElapsedSeconds / exitDurationSeconds)
                : 1f;
            var easedProgress = EaseInCubic(progress);

            ApplyVisualState(
                rootAlpha: 1f - easedProgress,
                contentAlpha: Mathf.Lerp(1f, 0.88f, easedProgress),
                contentYOffset: Mathf.Lerp(0f, -contentEnterOffsetY * 0.45f, easedProgress),
                contentScaleMultiplier: Mathf.Lerp(1f, contentEnterScale, easedProgress),
                titleYOffset: Mathf.Lerp(0f, titleEnterOffsetY * 0.45f, easedProgress),
                overlayAlphaMultiplier: Mathf.Lerp(1f, overlayEnterAlphaMultiplier, easedProgress),
                tryAgainScaleMultiplier: 1f);

            if (progress >= 1f)
            {
                CompletePendingAction();
            }
        }

        private void CompletePendingAction()
        {
            var action = pendingAction;
            pendingAction = PendingAction.None;
            inputBridge.SetInputSuppressed(false);
            ResetPopup();

            switch (action)
            {
                case PendingAction.MainMenu:
                    flowController.ReturnToMainScene();
                    break;
                case PendingAction.Retry:
                    flowController.RetryCurrentLevel();
                    break;
            }
        }

        private void ResetPopup()
        {
            state = LosePopupState.Hidden;
            pendingAction = PendingAction.None;
            stateElapsedSeconds = 0f;
            SetActionButtonsInteractable(false);
            ApplyHiddenVisualState();
        }

        private void ApplyHiddenVisualState()
        {
            ApplyVisualState(
                rootAlpha: 0f,
                contentAlpha: 0f,
                contentYOffset: -contentEnterOffsetY,
                contentScaleMultiplier: contentEnterScale,
                titleYOffset: titleEnterOffsetY,
                overlayAlphaMultiplier: overlayEnterAlphaMultiplier,
                tryAgainScaleMultiplier: 1f);

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void ApplyVisibleVisualState()
        {
            ApplyVisualState(
                rootAlpha: 1f,
                contentAlpha: 1f,
                contentYOffset: 0f,
                contentScaleMultiplier: 1f,
                titleYOffset: 0f,
                overlayAlphaMultiplier: 1f,
                tryAgainScaleMultiplier: EvaluateTryAgainPulse());
        }

        private void ApplyVisualState(
            float rootAlpha,
            float contentAlpha,
            float contentYOffset,
            float contentScaleMultiplier,
            float titleYOffset,
            float overlayAlphaMultiplier,
            float tryAgainScaleMultiplier)
        {
            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha = rootAlpha;
                popupCanvasGroup.interactable = rootAlpha > 0f;
                popupCanvasGroup.blocksRaycasts = rootAlpha > 0f;
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = contentAlpha;
                contentCanvasGroup.interactable = false;
                contentCanvasGroup.blocksRaycasts = false;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentBaseAnchoredPosition + Vector2.up * contentYOffset;
                contentRoot.localScale = contentBaseScale * contentScaleMultiplier;
            }

            if (titleTransform != null)
            {
                titleTransform.anchoredPosition = titleBaseAnchoredPosition + Vector2.up * titleYOffset;
            }

            if (tryAgainTextTransform != null)
            {
                tryAgainTextTransform.localScale = tryAgainTextBaseScale * tryAgainScaleMultiplier;
            }

            if (overlayImage != null)
            {
                var color = overlayBaseColor;
                color.a *= overlayAlphaMultiplier;
                overlayImage.color = color;
            }
        }

        private void SetActionButtonsInteractable(bool isInteractable)
        {
            if (closeButton != null)
            {
                closeButton.interactable = isInteractable;
            }

            if (tryAgainButton != null)
            {
                tryAgainButton.interactable = isInteractable;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = isInteractable;
            }
        }

        private bool TryResolveDependencies()
        {
            var targetInputBridge = inputBridge is not null ? inputBridge : GetComponent<BoardInputSessionBridge>();
            var targetFlowController = flowController is not null ? flowController : GetComponent<LevelSceneFlowController>();

            if (targetInputBridge is null || targetFlowController is null || popupRoot is null)
            {
                Debug.LogError($"{nameof(LevelLosePopupController)} is missing required references.", this);
                return false;
            }

            inputBridge = targetInputBridge;
            flowController = targetFlowController;
            closeButton ??= FindButton("CloseButton");
            tryAgainButton ??= FindButton("TryAgainButton");
            mainMenuButton ??= FindButton("MainMenuButton");
            popupCanvasGroup ??= popupRoot.GetComponent<CanvasGroup>() ?? popupRoot.AddComponent<CanvasGroup>();
            contentRoot ??= FindRectTransform("LosePopupPanel");
            titleTransform ??= FindRectTransform("LoseLabel");
            tryAgainTextTransform ??= FindRectTransform("TryAgainText");
            overlayImage ??= popupRoot.GetComponent<Image>();

            if (contentRoot != null && contentCanvasGroup == null)
            {
                contentCanvasGroup = contentRoot.GetComponent<CanvasGroup>() ?? contentRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (closeButton is null
                || tryAgainButton is null
                || mainMenuButton is null
                || contentRoot is null
                || titleTransform is null)
            {
                Debug.LogError($"{nameof(LevelLosePopupController)} is missing required UI references.", this);
                return false;
            }

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

        private void CacheVisualDefaults()
        {
            if (contentRoot != null)
            {
                contentBaseAnchoredPosition = contentRoot.anchoredPosition;
                contentBaseScale = contentRoot.localScale;
            }

            if (titleTransform != null)
            {
                titleBaseAnchoredPosition = titleTransform.anchoredPosition;
            }

            if (tryAgainTextTransform != null)
            {
                tryAgainTextBaseScale = tryAgainTextTransform.localScale;
            }

            if (overlayImage != null)
            {
                overlayBaseColor = overlayImage.color;
            }
        }

        private Button FindButton(string childName)
        {
            if (popupRoot == null)
            {
                return null;
            }

            var buttons = popupRoot.GetComponentsInChildren<Button>(includeInactive: true);
            foreach (var button in buttons)
            {
                if (button.name == childName)
                {
                    return button;
                }
            }

            return null;
        }

        private RectTransform FindRectTransform(string childName)
        {
            if (popupRoot == null)
            {
                return null;
            }

            var transforms = popupRoot.GetComponentsInChildren<RectTransform>(includeInactive: true);
            foreach (var rectTransform in transforms)
            {
                if (rectTransform.name == childName)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float progress)
        {
            return progress * progress * progress;
        }

        private float EvaluateTryAgainPulse()
        {
            if (tryAgainTextTransform == null || tryAgainPulseAmplitude <= 0f || tryAgainPulseFrequency <= 0f)
            {
                return 1f;
            }

            return 1f + Mathf.Sin(stateElapsedSeconds * tryAgainPulseFrequency * Mathf.PI * 2f) * tryAgainPulseAmplitude;
        }

        private enum LosePopupState
        {
            Hidden,
            Entering,
            Visible,
            Closing
        }

        private enum PendingAction
        {
            None,
            Retry,
            MainMenu
        }
    }
}
