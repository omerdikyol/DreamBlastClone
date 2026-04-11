using DreamBlastClone.Controllers;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelWinPresentationController : MonoBehaviour
    {
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private LevelSceneFlowController flowController;
        [SerializeField] private GameObject presentationRoot;
        [SerializeField] private GameObject continueHintRoot;
        [SerializeField] private CanvasGroup presentationCanvasGroup;
        [SerializeField] private CanvasGroup continueHintCanvasGroup;
        [SerializeField] private WinCelebrationParticlePlayer winCelebrationParticlePlayer;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform starTransform;
        [SerializeField] private RectTransform titleTransform;
        [SerializeField] private RectTransform continueHintTransform;
        [SerializeField] private CanvasGroup titleCanvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private float enterDurationSeconds = 0.28f;
        [SerializeField] private float minimumHoldSeconds = 1.2f;
        [SerializeField] private float exitDurationSeconds = 0.22f;
        [SerializeField] private int presentationSortingOrder = 110;
        [SerializeField] private float contentEnterOffsetY = 36f;
        [SerializeField] private float contentExitOffsetY = 12f;
        [SerializeField] private float contentEnterScale = 0.92f;
        [SerializeField] private float contentExitScale = 0.96f;
        [SerializeField] private float starEnterStartScale = 0.84f;
        [SerializeField] private float starExitEndScale = 1.08f;
        [SerializeField] private float continueHintFadeDurationSeconds = 0.18f;
        [SerializeField] private float continueHintEnterOffsetY = -10f;
        [SerializeField] private float continueHintExitOffsetY = -6f;
        [SerializeField] private float titleEnterOffsetY = 12f;
        [SerializeField] private float titleEnterScale = 0.94f;
        [SerializeField] private float starIdlePulseAmplitude = 0.035f;
        [SerializeField] private float starIdlePulseFrequency = 2.2f;
        [SerializeField] private float titleIdlePulseAmplitude = 0.01f;
        [SerializeField] private float titleIdlePulseFrequency = 1.35f;
        [SerializeField] private float overlayEnterAlphaMultiplier = 0.76f;

        private WinPresentationState state;
        private float stateElapsedSeconds;
        private float continueHintElapsedSeconds;
        private Vector2 contentBaseAnchoredPosition;
        private Vector2 continueHintBaseAnchoredPosition;
        private Vector2 titleBaseAnchoredPosition;
        private Vector3 contentBaseScale;
        private Vector3 starBaseScale;
        private Vector3 titleBaseScale;
        private Color overlayBaseColor;
        private bool hasCachedVisualDefaults;

        private void Awake()
        {
            state = WinPresentationState.Hidden;
            hasCachedVisualDefaults = false;
            stateElapsedSeconds = 0f;
            continueHintElapsedSeconds = 0f;

            if (presentationRoot != null)
            {
                presentationRoot.SetActive(false);
            }

            if (continueHintRoot != null)
            {
                continueHintRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (!TryResolveDependencies())
            {
                return;
            }

            ConfigurePresentationRoot();
            CacheVisualDefaults();
            ResetPresentation();
            inputBridge.SetInputSuppressed(false);
            inputBridge.TapProcessed += HandleTapProcessed;
        }

        private void OnDisable()
        {
            winCelebrationParticlePlayer?.Stop();

            if (inputBridge != null)
            {
                inputBridge.TapProcessed -= HandleTapProcessed;
                inputBridge.SetInputSuppressed(false);
            }
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            AdvanceWinPresentation(deltaTime);
            winCelebrationParticlePlayer?.Advance(deltaTime);

            if (state == WinPresentationState.AwaitingContinue && TryReadContinueInput())
            {
                HandleContinuePressed();
            }
        }

        private void HandleTapProcessed(LevelSessionTapResult result)
        {
            if (result is null || result.LevelState != LevelState.Win || state != WinPresentationState.Hidden)
            {
                return;
            }

            StartWinPresentation();
        }

        public bool TryShowForDebug()
        {
            if (!TryResolveDependencies())
            {
                return false;
            }

            ConfigurePresentationRoot();
            CacheVisualDefaults();
            return StartWinPresentation();
        }

        private void AdvanceWinPresentation(float deltaTime)
        {
            if (state == WinPresentationState.Hidden || state == WinPresentationState.Completing)
            {
                return;
            }

            var clampedDeltaTime = Mathf.Max(0f, deltaTime);
            stateElapsedSeconds += clampedDeltaTime;

            if (state == WinPresentationState.AwaitingContinue)
            {
                continueHintElapsedSeconds = Mathf.Min(continueHintFadeDurationSeconds, continueHintElapsedSeconds + clampedDeltaTime);
            }

            switch (state)
            {
                case WinPresentationState.Entering:
                    UpdateEnteringState();
                    break;
                case WinPresentationState.Holding:
                    UpdateHoldingState();
                    break;
                case WinPresentationState.AwaitingContinue:
                    UpdateAwaitingContinueState();
                    break;
                case WinPresentationState.Exiting:
                    UpdateExitingState();
                    break;
            }
        }

        private void CompleteWinFlow()
        {
            if (state == WinPresentationState.Completing)
            {
                return;
            }

            state = WinPresentationState.Completing;
            flowController.CompleteWinAndReturnToMainScene();
        }

        private bool StartWinPresentation()
        {
            if (state != WinPresentationState.Hidden || presentationRoot == null || inputBridge == null)
            {
                return false;
            }

            presentationRoot.transform.SetAsLastSibling();
            presentationRoot.SetActive(true);
            inputBridge.SetInputSuppressed(true);
            BeginEntering();
            winCelebrationParticlePlayer?.TryPlay(starTransform);
            return true;
        }

        private void ResetPresentation()
        {
            state = WinPresentationState.Hidden;
            stateElapsedSeconds = 0f;
            continueHintElapsedSeconds = 0f;
            winCelebrationParticlePlayer?.Stop();
            ApplyHiddenVisualState();
        }

        private bool TryResolveDependencies()
        {
            var targetInputBridge = inputBridge != null ? inputBridge : GetComponent<BoardInputSessionBridge>();
            var targetFlowController = flowController != null ? flowController : GetComponent<LevelSceneFlowController>();

            if (targetInputBridge == null || targetFlowController == null || presentationRoot == null)
            {
                Debug.LogError($"{nameof(LevelWinPresentationController)} is missing required references.", this);
                return false;
            }

            inputBridge = targetInputBridge;
            flowController = targetFlowController;
            presentationCanvasGroup ??= presentationRoot.GetComponent<CanvasGroup>();
            continueHintCanvasGroup ??= continueHintRoot != null ? continueHintRoot.GetComponent<CanvasGroup>() : null;
            winCelebrationParticlePlayer ??= presentationRoot.GetComponent<WinCelebrationParticlePlayer>();
            contentRoot ??= FindRectTransform("WinPopupPanel");
            starTransform ??= FindRectTransform("WinStar");
            titleTransform ??= FindRectTransform("WinTitle");
            continueHintTransform ??= continueHintRoot != null ? continueHintRoot.transform as RectTransform : null;
            overlayImage ??= presentationRoot.GetComponent<Image>();

            if (presentationCanvasGroup == null)
            {
                presentationCanvasGroup = presentationRoot.AddComponent<CanvasGroup>();
            }

            if (continueHintRoot != null && continueHintCanvasGroup == null)
            {
                continueHintCanvasGroup = continueHintRoot.AddComponent<CanvasGroup>();
            }

            if (titleTransform != null && titleCanvasGroup == null)
            {
                titleCanvasGroup = titleTransform.GetComponent<CanvasGroup>() ?? titleTransform.gameObject.AddComponent<CanvasGroup>();
            }

            if (contentRoot == null || starTransform == null)
            {
                Debug.LogError($"{nameof(LevelWinPresentationController)} is missing required animated UI references.", this);
                return false;
            }

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

        private void BeginEntering()
        {
            state = WinPresentationState.Entering;
            stateElapsedSeconds = 0f;
            continueHintElapsedSeconds = 0f;
            SetContinueHintVisible(false);
            ApplyVisualState(
                rootAlpha: 0f,
                contentYOffset: contentEnterOffsetY,
                contentScaleMultiplier: contentEnterScale,
                starScaleMultiplier: starEnterStartScale,
                hintAlpha: 0f,
                hintYOffset: continueHintEnterOffsetY,
                titleAlpha: 0f,
                titleYOffset: titleEnterOffsetY,
                titleScaleMultiplier: titleEnterScale,
                overlayAlphaMultiplier: overlayEnterAlphaMultiplier);

            if (enterDurationSeconds <= 0f)
            {
                BeginHolding();
            }
        }

        private void BeginHolding()
        {
            state = WinPresentationState.Holding;
            stateElapsedSeconds = 0f;
            ApplyVisualState(
                rootAlpha: 1f,
                contentYOffset: 0f,
                contentScaleMultiplier: 1f,
                starScaleMultiplier: EvaluateIdleStarPulse(),
                hintAlpha: 0f,
                hintYOffset: continueHintEnterOffsetY,
                titleAlpha: 1f,
                titleYOffset: 0f,
                titleScaleMultiplier: EvaluateIdleTitlePulse(),
                overlayAlphaMultiplier: 1f);

            if (minimumHoldSeconds <= 0f)
            {
                BeginAwaitingContinue();
            }
        }

        private void BeginAwaitingContinue()
        {
            if (state != WinPresentationState.Holding && state != WinPresentationState.Entering)
            {
                return;
            }

            state = WinPresentationState.AwaitingContinue;
            stateElapsedSeconds = 0f;
            continueHintElapsedSeconds = 0f;
            SetContinueHintVisible(true);
            ApplyVisualState(
                rootAlpha: 1f,
                contentYOffset: 0f,
                contentScaleMultiplier: 1f,
                starScaleMultiplier: EvaluateIdleStarPulse(),
                hintAlpha: 0f,
                hintYOffset: continueHintEnterOffsetY,
                titleAlpha: 1f,
                titleYOffset: 0f,
                titleScaleMultiplier: EvaluateIdleTitlePulse(),
                overlayAlphaMultiplier: 1f);
        }

        private void BeginExiting()
        {
            if (state == WinPresentationState.Hidden || state == WinPresentationState.Exiting || state == WinPresentationState.Completing)
            {
                return;
            }

            state = WinPresentationState.Exiting;
            stateElapsedSeconds = 0f;

            if (exitDurationSeconds <= 0f)
            {
                CompleteWinFlow();
            }
        }

        private void HandleContinuePressed()
        {
            if (state != WinPresentationState.AwaitingContinue)
            {
                return;
            }

            BeginExiting();
        }

        private void SetContinueHintVisible(bool isVisible)
        {
            if (continueHintRoot != null)
            {
                continueHintRoot.SetActive(isVisible);
            }
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
            }

            if (starTransform != null)
            {
                starBaseScale = starTransform.localScale;
            }

            if (continueHintTransform != null)
            {
                continueHintBaseAnchoredPosition = continueHintTransform.anchoredPosition;
            }

            if (overlayImage != null)
            {
                overlayBaseColor = overlayImage.color;
            }

            hasCachedVisualDefaults = true;
        }

        private void ApplyHiddenVisualState()
        {
            ApplyVisualState(
                rootAlpha: 0f,
                contentYOffset: contentEnterOffsetY,
                contentScaleMultiplier: contentEnterScale,
                starScaleMultiplier: starEnterStartScale,
                hintAlpha: 0f,
                hintYOffset: continueHintEnterOffsetY,
                titleAlpha: 0f,
                titleYOffset: titleEnterOffsetY,
                titleScaleMultiplier: titleEnterScale,
                overlayAlphaMultiplier: overlayEnterAlphaMultiplier);
            SetContinueHintVisible(false);

            if (presentationRoot != null)
            {
                presentationRoot.SetActive(false);
            }
        }

        private void ApplyVisualState(
            float rootAlpha,
            float contentYOffset,
            float contentScaleMultiplier,
            float starScaleMultiplier,
            float hintAlpha,
            float hintYOffset,
            float titleAlpha,
            float titleYOffset,
            float titleScaleMultiplier,
            float overlayAlphaMultiplier)
        {
            if (presentationCanvasGroup != null)
            {
                presentationCanvasGroup.alpha = rootAlpha;
                presentationCanvasGroup.interactable = rootAlpha > 0f;
                presentationCanvasGroup.blocksRaycasts = rootAlpha > 0f;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentBaseAnchoredPosition + Vector2.up * contentYOffset;
                contentRoot.localScale = contentBaseScale * contentScaleMultiplier;
            }

            if (starTransform != null)
            {
                starTransform.localScale = starBaseScale * starScaleMultiplier;
            }

            if (titleTransform != null)
            {
                titleTransform.anchoredPosition = titleBaseAnchoredPosition + Vector2.up * titleYOffset;
                titleTransform.localScale = titleBaseScale * titleScaleMultiplier;
            }

            if (titleCanvasGroup != null)
            {
                titleCanvasGroup.alpha = titleAlpha;
                titleCanvasGroup.interactable = false;
                titleCanvasGroup.blocksRaycasts = false;
            }

            if (continueHintCanvasGroup != null)
            {
                continueHintCanvasGroup.alpha = hintAlpha;
                continueHintCanvasGroup.interactable = false;
                continueHintCanvasGroup.blocksRaycasts = false;
            }

            if (continueHintTransform != null)
            {
                continueHintTransform.anchoredPosition = continueHintBaseAnchoredPosition + Vector2.up * hintYOffset;
            }

            if (overlayImage != null)
            {
                var color = overlayBaseColor;
                color.a *= overlayAlphaMultiplier;
                overlayImage.color = color;
            }
        }

        private void UpdateEnteringState()
        {
            var progress = enterDurationSeconds > 0f
                ? Mathf.Clamp01(stateElapsedSeconds / enterDurationSeconds)
                : 1f;
            var overlayProgress = EaseOutCubic(progress);
            var contentProgress = EaseOutBack(progress);
            var starProgress = EaseOutBack(Mathf.Clamp01((progress - 0.05f) / 0.95f));
            var titleProgress = Mathf.Clamp01(EaseOutBack(Mathf.Clamp01((progress - 0.16f) / 0.84f)));

            ApplyVisualState(
                rootAlpha: overlayProgress,
                contentYOffset: Mathf.LerpUnclamped(contentEnterOffsetY, 0f, contentProgress),
                contentScaleMultiplier: Mathf.LerpUnclamped(contentEnterScale, 1f, contentProgress),
                starScaleMultiplier: Mathf.LerpUnclamped(starEnterStartScale, 1f, starProgress),
                hintAlpha: 0f,
                hintYOffset: continueHintEnterOffsetY,
                titleAlpha: titleProgress,
                titleYOffset: Mathf.LerpUnclamped(titleEnterOffsetY, 0f, titleProgress),
                titleScaleMultiplier: Mathf.LerpUnclamped(titleEnterScale, 1f, titleProgress),
                overlayAlphaMultiplier: Mathf.Lerp(overlayEnterAlphaMultiplier, 1f, overlayProgress));

            if (progress >= 1f)
            {
                BeginHolding();
            }
        }

        private void UpdateHoldingState()
        {
            ApplyVisualState(
                rootAlpha: 1f,
                contentYOffset: 0f,
                contentScaleMultiplier: 1f,
                starScaleMultiplier: EvaluateIdleStarPulse(),
                hintAlpha: 0f,
                hintYOffset: continueHintEnterOffsetY,
                titleAlpha: 1f,
                titleYOffset: 0f,
                titleScaleMultiplier: EvaluateIdleTitlePulse(),
                overlayAlphaMultiplier: 1f);

            if (stateElapsedSeconds >= minimumHoldSeconds)
            {
                BeginAwaitingContinue();
            }
        }

        private void UpdateAwaitingContinueState()
        {
            var hintProgress = continueHintFadeDurationSeconds > 0f
                ? Mathf.Clamp01(continueHintElapsedSeconds / continueHintFadeDurationSeconds)
                : 1f;

            ApplyVisualState(
                rootAlpha: 1f,
                contentYOffset: 0f,
                contentScaleMultiplier: 1f,
                starScaleMultiplier: EvaluateIdleStarPulse(),
                hintAlpha: EaseOutCubic(hintProgress),
                hintYOffset: Mathf.Lerp(continueHintEnterOffsetY, 0f, EaseOutCubic(hintProgress)),
                titleAlpha: 1f,
                titleYOffset: 0f,
                titleScaleMultiplier: EvaluateIdleTitlePulse(),
                overlayAlphaMultiplier: 1f);
        }

        private void UpdateExitingState()
        {
            var progress = exitDurationSeconds > 0f
                ? Mathf.Clamp01(stateElapsedSeconds / exitDurationSeconds)
                : 1f;
            var overlayProgress = EaseInCubic(progress);
            var contentProgress = EaseInBack(progress);

            ApplyVisualState(
                rootAlpha: 1f - overlayProgress,
                contentYOffset: Mathf.LerpUnclamped(0f, contentExitOffsetY, contentProgress),
                contentScaleMultiplier: Mathf.LerpUnclamped(1f, contentExitScale, contentProgress),
                starScaleMultiplier: Mathf.LerpUnclamped(EvaluateIdleStarPulse(), starExitEndScale, contentProgress),
                hintAlpha: 1f - overlayProgress,
                hintYOffset: Mathf.LerpUnclamped(0f, continueHintExitOffsetY, contentProgress),
                titleAlpha: 1f - overlayProgress,
                titleYOffset: Mathf.LerpUnclamped(0f, titleEnterOffsetY * 0.45f, contentProgress),
                titleScaleMultiplier: Mathf.LerpUnclamped(EvaluateIdleTitlePulse(), titleEnterScale, contentProgress),
                overlayAlphaMultiplier: Mathf.Lerp(1f, overlayEnterAlphaMultiplier, overlayProgress));

            if (progress >= 1f)
            {
                CompleteWinFlow();
            }
        }

        private RectTransform FindRectTransform(string childName)
        {
            if (presentationRoot == null)
            {
                return null;
            }

            var children = presentationRoot.GetComponentsInChildren<RectTransform>(includeInactive: true);
            foreach (var child in children)
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private float EvaluateIdleStarPulse()
        {
            if (starIdlePulseAmplitude <= 0f || starIdlePulseFrequency <= 0f)
            {
                return 1f;
            }

            return 1f + Mathf.Sin(stateElapsedSeconds * starIdlePulseFrequency * Mathf.PI * 2f) * starIdlePulseAmplitude;
        }

        private float EvaluateIdleTitlePulse()
        {
            if (titleIdlePulseAmplitude <= 0f || titleIdlePulseFrequency <= 0f)
            {
                return 1f;
            }

            return 1f + Mathf.Sin(stateElapsedSeconds * titleIdlePulseFrequency * Mathf.PI * 2f) * titleIdlePulseAmplitude;
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

        private static bool TryReadContinueInput()
        {
            return (Mouse.current is not null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current is not null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }

        private enum WinPresentationState
        {
            Hidden,
            Entering,
            Holding,
            AwaitingContinue,
            Exiting,
            Completing
        }
    }
}
