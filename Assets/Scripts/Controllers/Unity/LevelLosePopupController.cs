using DreamBlastClone.Controllers;
using DreamBlastClone.Systems;
using UnityEngine;
using UnityEngine.UI;
using DreamBlastClone.Views;

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
        [SerializeField] private RectTransform subtitleTransform;
        [SerializeField] private RectTransform tryAgainTextTransform;
        [SerializeField] private RectTransform closeButtonTransform;
        [SerializeField] private RectTransform tryAgainButtonTransform;
        [SerializeField] private RectTransform mainMenuButtonTransform;
        [SerializeField] private UIButtonFeedbackView closeButtonFeedback;
        [SerializeField] private UIButtonFeedbackView tryAgainButtonFeedback;
        [SerializeField] private UIButtonFeedbackView mainMenuButtonFeedback;
        [SerializeField] private int popupSortingOrder = 100;
        [SerializeField] private float enterDurationSeconds = 0.28f;
        [SerializeField] private float exitDurationSeconds = 0.18f;
        [SerializeField] private float contentEnterOffsetY = 62f;
        [SerializeField] private float contentEnterScale = 0.86f;
        [SerializeField] private float overlayEnterAlphaMultiplier = 0.55f;
        [SerializeField] private float titleEnterOffsetY = 18f;
        [SerializeField] private float titleEnterScale = 0.88f;
        [SerializeField] private float subtitleEnterOffsetY = 14f;
        [SerializeField] private float subtitleEnterScale = 0.92f;
        [SerializeField] private float closeButtonEnterOffsetY = 28f;
        [SerializeField] private float closeButtonExitOffsetY = 14f;
        [SerializeField] private float actionButtonsEnterOffsetY = 34f;
        [SerializeField] private float actionButtonsExitOffsetY = 18f;
        [SerializeField] private float tryAgainPulseAmplitude = 0.05f;
        [SerializeField] private float tryAgainPulseFrequency = 1.8f;
        [SerializeField] private float titleIdlePulseAmplitude = 0.018f;
        [SerializeField] private float titleIdlePulseFrequency = 1.35f;
        [SerializeField] private float titleIdleBobAmplitude = 3f;
        [SerializeField] private float titleIdleBobFrequency = 1.15f;
        [SerializeField] private float titleIdleTiltAmplitude = 2.2f;
        [SerializeField] private float titleIdleTiltFrequency = 0.9f;
        [SerializeField] private float subtitleIdleScaleAmplitude = 0.01f;
        [SerializeField] private float subtitleIdleScaleFrequency = 1.05f;
        [SerializeField] private float subtitleIdleFloatAmplitude = 2.5f;
        [SerializeField] private float subtitleIdleFloatFrequency = 0.95f;
        [SerializeField] private float subtitleIdleDriftAmplitude = 6f;
        [SerializeField] private float subtitleIdleDriftFrequency = 0.62f;

        private bool hasShownLosePopup;
        private LosePopupState state;
        private PendingAction pendingAction;
        private float stateElapsedSeconds;
        private Vector2 contentBaseAnchoredPosition;
        private Vector2 titleBaseAnchoredPosition;
        private Vector2 subtitleBaseAnchoredPosition;
        private Vector2 closeButtonBaseAnchoredPosition;
        private Vector2 tryAgainButtonBaseAnchoredPosition;
        private Vector2 mainMenuButtonBaseAnchoredPosition;
        private Vector3 contentBaseScale;
        private Vector3 titleBaseScale;
        private Vector3 subtitleBaseScale;
        private Vector3 tryAgainTextBaseScale;
        private Quaternion titleBaseRotation;
        private Quaternion subtitleBaseRotation;
        private Color overlayBaseColor;
        private Image overlayImage;
        private bool hasCachedVisualDefaults;

        private void Awake()
        {
            hasShownLosePopup = false;
            hasCachedVisualDefaults = false;
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

            StopButtonFeedbacks();
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
                titleScaleMultiplier: titleEnterScale,
                titleRotationDegrees: -5f,
                subtitleXOffset: 0f,
                subtitleYOffset: subtitleEnterOffsetY,
                subtitleScaleMultiplier: subtitleEnterScale,
                subtitleRotationDegrees: 0f,
                overlayAlphaMultiplier: overlayEnterAlphaMultiplier,
                tryAgainScaleMultiplier: 1f,
                closeButtonYOffset: closeButtonEnterOffsetY,
                tryAgainButtonYOffset: -actionButtonsEnterOffsetY,
                mainMenuButtonYOffset: -actionButtonsEnterOffsetY);

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
            var overlayProgress = EaseOutCubic(progress);
            var contentProgress = EaseOutBack(progress);
            var titleProgress = EaseOutBack(Mathf.Clamp01((progress - 0.1f) / 0.9f));
            var buttonsProgress = EaseOutBack(Mathf.Clamp01((progress - 0.16f) / 0.84f));

            ApplyVisualState(
                rootAlpha: overlayProgress,
                contentAlpha: overlayProgress,
                contentYOffset: Mathf.LerpUnclamped(-contentEnterOffsetY, 0f, contentProgress),
                contentScaleMultiplier: Mathf.LerpUnclamped(contentEnterScale, 1f, contentProgress),
                titleYOffset: Mathf.LerpUnclamped(titleEnterOffsetY, 0f, titleProgress),
                titleScaleMultiplier: Mathf.LerpUnclamped(titleEnterScale, 1f, titleProgress),
                titleRotationDegrees: Mathf.LerpUnclamped(-5f, 0f, titleProgress),
                subtitleXOffset: 0f,
                subtitleYOffset: Mathf.LerpUnclamped(subtitleEnterOffsetY, 0f, Mathf.Clamp01(EaseOutBack(Mathf.Clamp01((progress - 0.18f) / 0.82f)))),
                subtitleScaleMultiplier: Mathf.LerpUnclamped(subtitleEnterScale, 1f, Mathf.Clamp01(EaseOutBack(Mathf.Clamp01((progress - 0.18f) / 0.82f)))),
                subtitleRotationDegrees: Mathf.LerpUnclamped(2f, 0f, Mathf.Clamp01(EaseOutBack(Mathf.Clamp01((progress - 0.18f) / 0.82f)))),
                overlayAlphaMultiplier: Mathf.Lerp(overlayEnterAlphaMultiplier, 1f, overlayProgress),
                tryAgainScaleMultiplier: 1f,
                closeButtonYOffset: Mathf.LerpUnclamped(closeButtonEnterOffsetY, 0f, buttonsProgress),
                tryAgainButtonYOffset: Mathf.LerpUnclamped(-actionButtonsEnterOffsetY, 0f, buttonsProgress),
                mainMenuButtonYOffset: Mathf.LerpUnclamped(-actionButtonsEnterOffsetY, 0f, buttonsProgress));

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
            var overlayProgress = EaseInCubic(progress);
            var contentProgress = EaseInBack(progress);

            ApplyVisualState(
                rootAlpha: 1f - overlayProgress,
                contentAlpha: 1f - overlayProgress,
                contentYOffset: Mathf.LerpUnclamped(0f, -contentEnterOffsetY * 0.45f, contentProgress),
                contentScaleMultiplier: Mathf.LerpUnclamped(1f, contentEnterScale, contentProgress),
                titleYOffset: Mathf.LerpUnclamped(0f, titleEnterOffsetY * 0.35f, contentProgress),
                titleScaleMultiplier: Mathf.LerpUnclamped(1f, titleEnterScale, contentProgress),
                titleRotationDegrees: Mathf.LerpUnclamped(EvaluateTitleTilt(), 4f, contentProgress),
                subtitleXOffset: Mathf.LerpUnclamped(EvaluateSubtitleDrift(), 8f, contentProgress),
                subtitleYOffset: Mathf.LerpUnclamped(EvaluateSubtitleFloat(), subtitleEnterOffsetY * 0.35f, contentProgress),
                subtitleScaleMultiplier: Mathf.LerpUnclamped(EvaluateSubtitleScale(), subtitleEnterScale, contentProgress),
                subtitleRotationDegrees: Mathf.LerpUnclamped(EvaluateSubtitleTilt(), -2f, contentProgress),
                overlayAlphaMultiplier: Mathf.Lerp(1f, overlayEnterAlphaMultiplier, overlayProgress),
                tryAgainScaleMultiplier: Mathf.LerpUnclamped(EvaluateTryAgainPulse(), 1f, overlayProgress),
                closeButtonYOffset: Mathf.LerpUnclamped(0f, closeButtonExitOffsetY, contentProgress),
                tryAgainButtonYOffset: Mathf.LerpUnclamped(0f, -actionButtonsExitOffsetY, contentProgress),
                mainMenuButtonYOffset: Mathf.LerpUnclamped(0f, -actionButtonsExitOffsetY, contentProgress));

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
                titleScaleMultiplier: titleEnterScale,
                titleRotationDegrees: -5f,
                subtitleXOffset: 0f,
                subtitleYOffset: subtitleEnterOffsetY,
                subtitleScaleMultiplier: subtitleEnterScale,
                subtitleRotationDegrees: 0f,
                overlayAlphaMultiplier: overlayEnterAlphaMultiplier,
                tryAgainScaleMultiplier: 1f,
                closeButtonYOffset: closeButtonEnterOffsetY,
                tryAgainButtonYOffset: -actionButtonsEnterOffsetY,
                mainMenuButtonYOffset: -actionButtonsEnterOffsetY);

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            StopButtonFeedbacks();
        }

        private void ApplyVisibleVisualState()
        {
            ApplyVisualState(
                rootAlpha: 1f,
                contentAlpha: 1f,
                contentYOffset: 0f,
                contentScaleMultiplier: 1f,
                titleYOffset: EvaluateTitleBob(),
                titleScaleMultiplier: EvaluateTitlePulse(),
                titleRotationDegrees: EvaluateTitleTilt(),
                subtitleXOffset: EvaluateSubtitleDrift(),
                subtitleYOffset: EvaluateSubtitleFloat(),
                subtitleScaleMultiplier: EvaluateSubtitleScale(),
                subtitleRotationDegrees: EvaluateSubtitleTilt(),
                overlayAlphaMultiplier: 1f,
                tryAgainScaleMultiplier: EvaluateTryAgainPulse(),
                closeButtonYOffset: 0f,
                tryAgainButtonYOffset: 0f,
                mainMenuButtonYOffset: 0f);
        }

        private void ApplyVisualState(
            float rootAlpha,
            float contentAlpha,
            float contentYOffset,
            float contentScaleMultiplier,
            float titleYOffset,
            float titleScaleMultiplier,
            float titleRotationDegrees,
            float subtitleXOffset,
            float subtitleYOffset,
            float subtitleScaleMultiplier,
            float subtitleRotationDegrees,
            float overlayAlphaMultiplier,
            float tryAgainScaleMultiplier,
            float closeButtonYOffset,
            float tryAgainButtonYOffset,
            float mainMenuButtonYOffset)
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
                titleTransform.localScale = titleBaseScale * titleScaleMultiplier;
                titleTransform.localRotation = titleBaseRotation * Quaternion.Euler(0f, 0f, titleRotationDegrees);
            }

            if (subtitleTransform != null)
            {
                subtitleTransform.anchoredPosition = subtitleBaseAnchoredPosition + new Vector2(subtitleXOffset, subtitleYOffset);
                subtitleTransform.localScale = subtitleBaseScale * subtitleScaleMultiplier;
                subtitleTransform.localRotation = subtitleBaseRotation * Quaternion.Euler(0f, 0f, subtitleRotationDegrees);
            }

            if (tryAgainTextTransform != null)
            {
                tryAgainTextTransform.localScale = tryAgainTextBaseScale * tryAgainScaleMultiplier;
            }

            if (closeButtonTransform != null)
            {
                closeButtonTransform.anchoredPosition = closeButtonBaseAnchoredPosition + Vector2.up * closeButtonYOffset;
            }

            if (tryAgainButtonTransform != null)
            {
                tryAgainButtonTransform.anchoredPosition = tryAgainButtonBaseAnchoredPosition + Vector2.up * tryAgainButtonYOffset;
            }

            if (mainMenuButtonTransform != null)
            {
                mainMenuButtonTransform.anchoredPosition = mainMenuButtonBaseAnchoredPosition + Vector2.up * mainMenuButtonYOffset;
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
            subtitleTransform ??= FindRectTransform("LoseSubtitle");
            tryAgainTextTransform ??= FindRectTransform("TryAgainText");
            closeButtonTransform ??= closeButton != null ? closeButton.transform as RectTransform : null;
            tryAgainButtonTransform ??= tryAgainButton != null ? tryAgainButton.transform as RectTransform : null;
            mainMenuButtonTransform ??= mainMenuButton != null ? mainMenuButton.transform as RectTransform : null;
            overlayImage ??= popupRoot.GetComponent<Image>();

            if (contentRoot != null && contentCanvasGroup == null)
            {
                contentCanvasGroup = contentRoot.GetComponent<CanvasGroup>() ?? contentRoot.gameObject.AddComponent<CanvasGroup>();
            }

            closeButtonFeedback = ResolveButtonFeedback(closeButton, closeButtonFeedback);
            tryAgainButtonFeedback = ResolveButtonFeedback(tryAgainButton, tryAgainButtonFeedback);
            mainMenuButtonFeedback = ResolveButtonFeedback(mainMenuButton, mainMenuButtonFeedback);

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
            if (hasCachedVisualDefaults)
            {
                return;
            }

            if (contentRoot != null)
            {
                contentBaseAnchoredPosition = contentRoot.anchoredPosition;
                contentBaseScale = contentRoot.localScale;
            }

            if (titleTransform != null)
            {
                titleBaseAnchoredPosition = titleTransform.anchoredPosition;
                titleBaseScale = titleTransform.localScale;
                titleBaseRotation = titleTransform.localRotation;
            }

            if (subtitleTransform != null)
            {
                subtitleBaseAnchoredPosition = subtitleTransform.anchoredPosition;
                subtitleBaseScale = subtitleTransform.localScale;
                subtitleBaseRotation = subtitleTransform.localRotation;
            }

            if (tryAgainTextTransform != null)
            {
                tryAgainTextBaseScale = tryAgainTextTransform.localScale;
            }

            if (closeButtonTransform != null)
            {
                closeButtonBaseAnchoredPosition = closeButtonTransform.anchoredPosition;
            }

            if (tryAgainButtonTransform != null)
            {
                tryAgainButtonBaseAnchoredPosition = tryAgainButtonTransform.anchoredPosition;
            }

            if (mainMenuButtonTransform != null)
            {
                mainMenuButtonBaseAnchoredPosition = mainMenuButtonTransform.anchoredPosition;
            }

            if (overlayImage != null)
            {
                overlayBaseColor = overlayImage.color;
            }

            hasCachedVisualDefaults = true;
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

        private static float EaseOutBack(float progress)
        {
            const float overshoot = 1.70158f;
            var adjusted = progress - 1f;
            return 1f + (overshoot + 1f) * adjusted * adjusted * adjusted + overshoot * adjusted * adjusted;
        }

        private static float EaseInCubic(float progress)
        {
            return progress * progress * progress;
        }

        private static float EaseInBack(float progress)
        {
            const float overshoot = 1.70158f;
            return (overshoot + 1f) * progress * progress * progress - overshoot * progress * progress;
        }

        private float EvaluateTryAgainPulse()
        {
            if (tryAgainTextTransform == null || tryAgainPulseAmplitude <= 0f || tryAgainPulseFrequency <= 0f)
            {
                return 1f;
            }

            return 1f + Mathf.Sin(stateElapsedSeconds * tryAgainPulseFrequency * Mathf.PI * 2f) * tryAgainPulseAmplitude;
        }

        private float EvaluateTitlePulse()
        {
            if (titleTransform == null || titleIdlePulseAmplitude <= 0f || titleIdlePulseFrequency <= 0f)
            {
                return 1f;
            }

            return 1f + Mathf.Sin(stateElapsedSeconds * titleIdlePulseFrequency * Mathf.PI * 2f) * titleIdlePulseAmplitude;
        }

        private float EvaluateTitleBob()
        {
            if (titleTransform == null || titleIdleBobAmplitude <= 0f || titleIdleBobFrequency <= 0f)
            {
                return 0f;
            }

            return Mathf.Sin((stateElapsedSeconds + 0.1f) * titleIdleBobFrequency * Mathf.PI * 2f) * titleIdleBobAmplitude;
        }

        private float EvaluateTitleTilt()
        {
            if (titleTransform == null || titleIdleTiltAmplitude <= 0f || titleIdleTiltFrequency <= 0f)
            {
                return 0f;
            }

            return Mathf.Sin((stateElapsedSeconds + 0.08f) * titleIdleTiltFrequency * Mathf.PI * 2f) * titleIdleTiltAmplitude;
        }

        private float EvaluateSubtitleScale()
        {
            if (subtitleTransform == null || subtitleIdleScaleAmplitude <= 0f || subtitleIdleScaleFrequency <= 0f)
            {
                return 1f;
            }

            return 1f + Mathf.Sin((stateElapsedSeconds + 0.17f) * subtitleIdleScaleFrequency * Mathf.PI * 2f) * subtitleIdleScaleAmplitude;
        }

        private float EvaluateSubtitleFloat()
        {
            if (subtitleTransform == null || subtitleIdleFloatAmplitude <= 0f || subtitleIdleFloatFrequency <= 0f)
            {
                return 0f;
            }

            return Mathf.Sin((stateElapsedSeconds + 0.14f) * subtitleIdleFloatFrequency * Mathf.PI * 2f) * subtitleIdleFloatAmplitude;
        }

        private float EvaluateSubtitleDrift()
        {
            if (subtitleTransform == null || subtitleIdleDriftAmplitude <= 0f || subtitleIdleDriftFrequency <= 0f)
            {
                return 0f;
            }

            return Mathf.Sin((stateElapsedSeconds + 0.31f) * subtitleIdleDriftFrequency * Mathf.PI * 2f) * subtitleIdleDriftAmplitude;
        }

        private float EvaluateSubtitleTilt()
        {
            if (subtitleTransform == null || subtitleIdleDriftAmplitude <= 0f || subtitleIdleDriftFrequency <= 0f)
            {
                return 0f;
            }

            return Mathf.Sin((stateElapsedSeconds + 0.27f) * subtitleIdleDriftFrequency * Mathf.PI * 2f) * 0.9f;
        }

        private static UIButtonFeedbackView ResolveButtonFeedback(Button button, UIButtonFeedbackView existingFeedback)
        {
            if (existingFeedback != null || button == null)
            {
                return existingFeedback;
            }

            return button.GetComponent<UIButtonFeedbackView>() ?? button.gameObject.AddComponent<UIButtonFeedbackView>();
        }

        private void StopButtonFeedbacks()
        {
            closeButtonFeedback?.StopAndReset();
            tryAgainButtonFeedback?.StopAndReset();
            mainMenuButtonFeedback?.StopAndReset();
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
