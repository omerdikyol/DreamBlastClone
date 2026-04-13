using DreamBlastClone.Views;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class AudioSettingsPopupController : MonoBehaviour
    {
        [SerializeField] private GameAudioController audioController;
        [SerializeField] private GameHapticsController hapticsController;
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private Canvas popupCanvas;
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        [SerializeField] private CanvasGroup popupCanvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private int popupSortingOrder = 120;
        [SerializeField] private float enterDurationSeconds = 0.22f;
        [SerializeField] private float exitDurationSeconds = 0.16f;
        [SerializeField] private float contentEnterOffsetY = 48f;
        [SerializeField] private float contentEnterScale = 0.9f;
        [SerializeField] private float overlayEnterAlphaMultiplier = 0.6f;
        [SerializeField] private bool fitContentInsideSafeArea = true;
        [SerializeField] private float safeAreaContentPadding = 80f;

        private PopupState state;
        private float stateElapsedSeconds;
        private bool hasSuppressedInput;
        private bool previousInputSuppressed;
        private bool isApplyingSliderValues;
        private bool hasCachedVisualDefaults;
        private RectTransform rootRect;
        private Vector2 contentBaseAnchoredPosition;
        private Vector3 contentBaseScale;
        private Color overlayBaseColor;

        public bool IsOpen => state != PopupState.Hidden;

        private void Awake()
        {
            if (!EnsureUiBuilt())
            {
                return;
            }

            CacheVisualDefaults();
            ApplyHiddenVisualState();
        }

        private void OnEnable()
        {
            if (!EnsureUiBuilt())
            {
                return;
            }

            CacheVisualDefaults();
            HookUiEvents();
            ApplyHiddenVisualState();
        }

        private void OnDisable()
        {
            UnhookUiEvents();
            RestoreInputSuppression();
            ApplyHiddenVisualState();
        }

        private void Update()
        {
            AdvancePresentation(Time.unscaledDeltaTime);
        }

        public static AudioSettingsPopupController EnsureInstance(
            Transform canvasRoot,
            GameAudioController audioController,
            BoardInputSessionBridge inputBridge = null,
            AudioSettingsPopupController prefab = null)
        {
            var existing = FindFirstObjectByType<AudioSettingsPopupController>();
            if (existing != null)
            {
                existing.Configure(audioController, inputBridge);
                if (canvasRoot != null && existing.transform.parent != canvasRoot)
                {
                    existing.transform.SetParent(canvasRoot, false);
                }

                return existing;
            }

            if (prefab == null)
            {
                prefab = TryLoadEditorPrefabFallback();
                if (prefab == null)
                {
                    Debug.LogError($"{nameof(AudioSettingsPopupController)} requires an assigned SettingsPopUp prefab.");
                    return null;
                }
            }

            var instance = Instantiate(prefab, canvasRoot, false);
            instance.name = prefab.name;
            instance.Configure(audioController, inputBridge);
            return instance;
        }

        private static AudioSettingsPopupController TryLoadEditorPrefabFallback()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioSettingsPopupController>("Assets/Prefabs/SettingsPopUp.prefab");
#else
            return null;
#endif
        }

        public void Configure(GameAudioController targetAudioController, BoardInputSessionBridge targetInputBridge = null)
        {
            if (targetAudioController != null)
            {
                audioController = targetAudioController;
            }

            if (targetInputBridge != null)
            {
                inputBridge = targetInputBridge;
            }

            if (!EnsureUiBuilt())
            {
                return;
            }

            ResolveHapticsController();
            SyncSlidersFromAudio();
        }

        public bool TryOpen()
        {
            if (!EnsureUiBuilt() || state != PopupState.Hidden || audioController == null)
            {
                return false;
            }

            SyncSlidersFromAudio();
            popupCanvas.transform.SetAsLastSibling();
            SuppressInputIfNeeded();
            audioController.PlaySfx(GameSfxCue.PopupOpen);
            ResolveHapticsController();
            hapticsController?.Play(GameHapticCue.Light);
            state = PopupState.Entering;
            stateElapsedSeconds = 0f;
            ApplyVisualState(0f, 0f, contentEnterOffsetY, contentEnterScale, overlayEnterAlphaMultiplier);

            if (enterDurationSeconds <= 0f)
            {
                BeginVisible();
            }

            return true;
        }

        public bool TryClose()
        {
            if (!EnsureUiBuilt() || state == PopupState.Hidden || state == PopupState.Closing)
            {
                return false;
            }

            audioController?.PlaySfx(GameSfxCue.PopupClose);
            ResolveHapticsController();
            hapticsController?.Play(GameHapticCue.Light);
            state = PopupState.Closing;
            stateElapsedSeconds = 0f;

            if (exitDurationSeconds <= 0f)
            {
                CompleteClose();
            }

            return true;
        }

        private void AdvancePresentation(float deltaTime)
        {
            if (state == PopupState.Hidden)
            {
                return;
            }

            stateElapsedSeconds += Mathf.Max(0f, deltaTime);

            switch (state)
            {
                case PopupState.Entering:
                    var enterProgress = enterDurationSeconds > 0f ? Mathf.Clamp01(stateElapsedSeconds / enterDurationSeconds) : 1f;
                    var enterOverlay = EaseOutCubic(enterProgress);
                    var enterContent = EaseOutBack(enterProgress);
                    ApplyVisualState(
                        enterOverlay,
                        enterOverlay,
                        Mathf.LerpUnclamped(contentEnterOffsetY, 0f, enterContent),
                        Mathf.LerpUnclamped(contentEnterScale, 1f, enterContent),
                        Mathf.Lerp(overlayEnterAlphaMultiplier, 1f, enterOverlay));
                    if (enterProgress >= 1f)
                    {
                        BeginVisible();
                    }
                    break;
                case PopupState.Visible:
                    ApplyVisualState(1f, 1f, 0f, 1f, 1f);
                    break;
                case PopupState.Closing:
                    var exitProgress = exitDurationSeconds > 0f ? Mathf.Clamp01(stateElapsedSeconds / exitDurationSeconds) : 1f;
                    var exitOverlay = EaseInCubic(exitProgress);
                    var exitContent = EaseInBack(exitProgress);
                    ApplyVisualState(
                        1f - exitOverlay,
                        1f - exitOverlay,
                        Mathf.LerpUnclamped(0f, contentEnterOffsetY * 0.35f, exitContent),
                        Mathf.LerpUnclamped(1f, contentEnterScale, exitContent),
                        Mathf.Lerp(1f, overlayEnterAlphaMultiplier, exitOverlay));
                    if (exitProgress >= 1f)
                    {
                        CompleteClose();
                    }
                    break;
            }
        }

        private void ResolveHapticsController()
        {
            hapticsController ??=
                audioController != null
                    ? audioController.GetComponent<GameHapticsController>() ?? audioController.gameObject.AddComponent<GameHapticsController>()
                    : GetComponentInParent<GameHapticsController>() ?? FindFirstObjectByType<GameHapticsController>() ?? gameObject.AddComponent<GameHapticsController>();
        }

        private void BeginVisible()
        {
            state = PopupState.Visible;
            stateElapsedSeconds = 0f;
            ApplyVisualState(1f, 1f, 0f, 1f, 1f);
        }

        private void CompleteClose()
        {
            state = PopupState.Hidden;
            stateElapsedSeconds = 0f;
            RestoreInputSuppression();
            ApplyHiddenVisualState();
        }

        private void ApplyHiddenVisualState()
        {
            ApplyVisualState(0f, 0f, contentEnterOffsetY, contentEnterScale, overlayEnterAlphaMultiplier);
        }

        private void ApplyVisualState(float rootAlpha, float contentAlpha, float contentYOffset, float contentScale, float overlayAlphaMultiplier)
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
                contentCanvasGroup.interactable = contentAlpha > 0f;
                contentCanvasGroup.blocksRaycasts = contentAlpha > 0f;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentBaseAnchoredPosition + ResolveSafeAreaContentOffset() + Vector2.up * contentYOffset;
                contentRoot.localScale = contentBaseScale * contentScale * ResolveSafeAreaContentScale();
            }

            if (overlayImage != null)
            {
                var color = overlayBaseColor;
                color.a *= overlayAlphaMultiplier;
                overlayImage.color = color;
            }
        }

        private bool EnsureUiBuilt()
        {
            if (rootRect == null)
            {
                rootRect = transform as RectTransform ?? gameObject.AddComponent<RectTransform>();
            }

            if (popupCanvas == null)
            {
                popupCanvas = GetComponent<Canvas>();
            }

            if (popupCanvas == null)
            {
                popupCanvas = gameObject.AddComponent<Canvas>();
            }

            if (graphicRaycaster == null)
            {
                graphicRaycaster = GetComponent<GraphicRaycaster>();
            }

            if (graphicRaycaster == null)
            {
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            if (popupCanvasGroup == null)
            {
                popupCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (popupCanvasGroup == null)
            {
                popupCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (overlayImage == null)
            {
                overlayImage = GetComponent<Image>();
            }

            if (overlayImage == null)
            {
                overlayImage = gameObject.AddComponent<Image>();
            }

            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = popupSortingOrder;
            var parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
            if (parentCanvas != null)
            {
                popupCanvas.renderMode = parentCanvas.renderMode;
                popupCanvas.worldCamera = parentCanvas.worldCamera;
                popupCanvas.planeDistance = parentCanvas.planeDistance;
            }
            SetStretch(rootRect);
            overlayImage.color = new Color(0f, 0f, 0f, 0.82f);

            contentRoot ??= FindRectTransformInChildren("ContentRoot");
            closeButton ??= FindComponentInChildrenByName<Button>(contentRoot, "CloseButton");
            musicSlider ??= FindComponentInChildrenByName<Slider>(contentRoot, "MusicSlider");
            sfxSlider ??= FindComponentInChildrenByName<Slider>(contentRoot, "SfxSlider");

            if (contentRoot == null || closeButton == null || musicSlider == null || sfxSlider == null)
            {
                Debug.LogError($"{nameof(AudioSettingsPopupController)} requires a prefab hierarchy with ContentRoot, CloseButton, MusicSlider, and SfxSlider.", this);
                return false;
            }

            if (contentCanvasGroup == null)
            {
                contentCanvasGroup = contentRoot.GetComponent<CanvasGroup>();
            }

            if (contentCanvasGroup == null)
            {
                contentCanvasGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            }

            _ = closeButton.GetComponent<UIButtonClickSoundPlayer>() ?? closeButton.gameObject.AddComponent<UIButtonClickSoundPlayer>();
            _ = closeButton.GetComponent<UIButtonFeedbackView>() ?? closeButton.gameObject.AddComponent<UIButtonFeedbackView>();
            return true;
        }

        private void HookUiEvents()
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
            musicSlider.onValueChanged.RemoveListener(HandleMusicSliderChanged);
            musicSlider.onValueChanged.AddListener(HandleMusicSliderChanged);
            sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
        }

        private void UnhookUiEvents()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(HandleMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
            }
        }

        private void SyncSlidersFromAudio()
        {
            if (audioController == null || musicSlider == null || sfxSlider == null)
            {
                return;
            }

            isApplyingSliderValues = true;
            musicSlider.SetValueWithoutNotify(audioController.MusicUserVolume);
            sfxSlider.SetValueWithoutNotify(audioController.SfxUserVolume);
            isApplyingSliderValues = false;
        }

        private void HandleCloseClicked() => TryClose();

        private void HandleMusicSliderChanged(float value)
        {
            if (!isApplyingSliderValues)
            {
                audioController?.SetMusicUserVolume(value);
            }
        }

        private void HandleSfxSliderChanged(float value)
        {
            if (!isApplyingSliderValues)
            {
                audioController?.SetSfxUserVolume(value);
            }
        }

        private void SuppressInputIfNeeded()
        {
            if (inputBridge == null)
            {
                return;
            }

            previousInputSuppressed = inputBridge.IsInputSuppressed;
            hasSuppressedInput = true;
            inputBridge.SetInputSuppressed(true);
        }

        private void RestoreInputSuppression()
        {
            if (hasSuppressedInput && inputBridge != null)
            {
                inputBridge.SetInputSuppressed(previousInputSuppressed);
            }

            hasSuppressedInput = false;
            previousInputSuppressed = false;
        }

        private void CacheVisualDefaults()
        {
            if (hasCachedVisualDefaults)
            {
                return;
            }

            contentBaseAnchoredPosition = contentRoot.anchoredPosition;
            contentBaseScale = contentRoot.localScale;
            overlayBaseColor = overlayImage.color;
            hasCachedVisualDefaults = true;
        }

        private Vector2 ResolveSafeAreaContentOffset()
        {
            if (!fitContentInsideSafeArea)
            {
                return Vector2.zero;
            }

            var canvas = ResolveLayoutCanvas();
            var scaleFactor = canvas != null ? Mathf.Max(0.001f, canvas.scaleFactor) : 1f;
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return (Screen.safeArea.center - screenCenter) / scaleFactor;
        }

        private float ResolveSafeAreaContentScale()
        {
            if (!fitContentInsideSafeArea || contentRoot == null)
            {
                return 1f;
            }

            var canvas = ResolveLayoutCanvas();
            var scaleFactor = canvas != null ? Mathf.Max(0.001f, canvas.scaleFactor) : 1f;
            var safeSize = Screen.safeArea.size / scaleFactor;
            var contentSize = contentRoot.rect.size;

            if (contentSize.x <= 0f || contentSize.y <= 0f || safeSize.x <= 0f || safeSize.y <= 0f)
            {
                return 1f;
            }

            var maxWidth = Mathf.Max(1f, safeSize.x - safeAreaContentPadding * 2f);
            var maxHeight = Mathf.Max(1f, safeSize.y - safeAreaContentPadding * 2f);
            return Mathf.Min(1f, maxWidth / contentSize.x, maxHeight / contentSize.y);
        }

        private Canvas ResolveLayoutCanvas()
        {
            var parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
            if (parentCanvas != null)
            {
                return parentCanvas;
            }

            return popupCanvas != null ? popupCanvas : GetComponentInParent<Canvas>();
        }

        private RectTransform FindRectTransformInChildren(string name)
        {
            var rectTransforms = GetComponentsInChildren<RectTransform>(true);
            foreach (var rectTransform in rectTransforms)
            {
                if (rectTransform.name == name)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private static T FindComponentInChildrenByName<T>(RectTransform root, string name) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            var components = root.GetComponentsInChildren<T>(true);
            foreach (var component in components)
            {
                if (component.name == name)
                {
                    return component;
                }
            }

            return null;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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

        private enum PopupState
        {
            Hidden,
            Entering,
            Visible,
            Closing
        }
    }
}
