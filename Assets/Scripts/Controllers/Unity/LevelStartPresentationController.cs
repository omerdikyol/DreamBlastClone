using System;
using System.Collections.Generic;
using DG.Tweening;
using DreamBlastClone.Views;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    // Run after the normal scene bootstrap so the intro can animate the fully built board/HUD.
    [DefaultExecutionOrder(200)]
    public sealed class LevelStartPresentationController : MonoBehaviour
    {
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private BoardView boardView;
        [SerializeField] private SpriteRenderer gridBackgroundRenderer;
        [SerializeField] private RectTransform canvasRoot;
        [SerializeField] private RectTransform topBarBackground;
        [SerializeField] private RectTransform moveTextTransform;
        [SerializeField] private RectTransform moveNumberTransform;
        [SerializeField] private RectTransform goalTextTransform;
        [SerializeField] private GameObject backgroundRoot;
        [SerializeField] private GameObject winPresentationRoot;
        [SerializeField] private GameObject losePopupRoot;
        [SerializeField] private float boardRevealDurationSeconds = 0.28f;
        [SerializeField] private float boardStartScale = 0.92f;
        [SerializeField] private float boardStartOffsetY = -0.2f;
        [SerializeField] private float itemRevealDurationSeconds = 0.18f;
        [SerializeField] private float itemRevealStaggerSeconds = 0.022f;
        [SerializeField] private float topBarRevealDelaySeconds = 0.08f;
        [SerializeField] private float topBarBackgroundDurationSeconds = 0.22f;
        [SerializeField] private float topBarElementDurationSeconds = 0.18f;
        [SerializeField] private float topBarElementStaggerSeconds = 0.045f;
        [SerializeField] private float topBarBackgroundStartOffsetY = 34f;
        [SerializeField] private float topBarElementStartOffsetY = 24f;
        [SerializeField] private float topBarBackgroundStartScale = 0.985f;
        [SerializeField] private float topBarElementStartScale = 0.96f;

        private readonly List<BoardVisualState> boardVisuals = new();
        private readonly List<TopBarVisualState> topBarVisuals = new();
        private IntroState state;
        private Sequence introSequence;
        private Vector3 boardBaseLocalPosition;
        private Vector3 boardBaseLocalScale;
        private Color boardBackgroundBaseColor;
        private bool hasCachedStaticState;

        public bool IsIntroPlaying => state == IntroState.Pending || state == IntroState.Playing;

        private void Awake()
        {
            state = IntroState.Hidden;
            CacheStaticState();
            ApplyCompletedVisualState();
        }

        private void OnEnable()
        {
            if (!TryResolveDependencies())
            {
                return;
            }

            CacheStaticState();
            StopIntroSequence();
            ClearDynamicState();
            ApplyHiddenVisualState();
            inputBridge.SetInputSuppressed(true);
            state = IntroState.Pending;
        }

        private void Start()
        {
            BeginIntro();
        }

        private void OnDisable()
        {
            CompleteAndReset();
        }

        private void OnDestroy()
        {
            CompleteAndReset();
        }

        private bool TryResolveDependencies()
        {
            if (inputBridge == null)
            {
                inputBridge = GetComponent<BoardInputSessionBridge>();
            }

            if (canvasRoot != null)
            {
                topBarBackground ??= FindCanvasChild("Top UI Background");
                moveTextTransform ??= FindCanvasChild("MoveText");
                moveNumberTransform ??= FindCanvasChild("MoveNumber");
                goalTextTransform ??= FindCanvasChild("GoalText");
            }

            return inputBridge != null
                && boardView != null
                && gridBackgroundRenderer != null
                && canvasRoot != null
                && topBarBackground != null
                && moveTextTransform != null
                && moveNumberTransform != null
                && goalTextTransform != null;
        }

        private void CacheStaticState()
        {
            if (hasCachedStaticState || !TryResolveDependencies())
            {
                return;
            }

            boardBaseLocalPosition = boardView.transform.localPosition;
            boardBaseLocalScale = boardView.transform.localScale;
            boardBackgroundBaseColor = gridBackgroundRenderer.color;
            hasCachedStaticState = true;
        }

        private void BeginIntro()
        {
            if (state != IntroState.Pending || !TryResolveDependencies())
            {
                return;
            }

            PresentationTweenBootstrap.EnsureInitialized();
            BuildBoardVisualState();
            BuildTopBarVisualState();
            ApplyHiddenVisualState();

            introSequence = DOTween.Sequence()
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(HandleIntroCompleted);

            introSequence.Join(boardView.transform.DOLocalMove(boardBaseLocalPosition, boardRevealDurationSeconds).SetEase(Ease.OutCubic));
            introSequence.Join(boardView.transform.DOScale(boardBaseLocalScale, boardRevealDurationSeconds).SetEase(Ease.OutBack));
            introSequence.Join(TweenSpriteColor(gridBackgroundRenderer, boardBackgroundBaseColor, boardRevealDurationSeconds, Ease.OutSine));

            for (var index = 0; index < boardVisuals.Count; index++)
            {
                var visual = boardVisuals[index];
                introSequence.Insert(
                    visual.DelaySeconds,
                    TweenSpriteRenderers(visual.Renderers, visual.BaseColors, itemRevealDurationSeconds, Ease.OutSine));
            }

            for (var index = 0; index < topBarVisuals.Count; index++)
            {
                var visual = topBarVisuals[index];
                var duration = visual.IsBackground
                    ? topBarBackgroundDurationSeconds
                    : topBarElementDurationSeconds;

                introSequence.Insert(
                    visual.DelaySeconds,
                    DOTween.To(
                            () => visual.RectTransform.anchoredPosition,
                            value => visual.RectTransform.anchoredPosition = value,
                            visual.BaseAnchoredPosition,
                            duration)
                        .SetEase(Ease.OutCubic));
                introSequence.Insert(visual.DelaySeconds, visual.RectTransform.DOScale(visual.BaseLocalScale, duration).SetEase(Ease.OutBack));
                introSequence.Insert(visual.DelaySeconds, DOTween.To(() => visual.CanvasGroup.alpha, value => visual.CanvasGroup.alpha = value, visual.BaseAlpha, duration).SetEase(Ease.OutSine));
            }

            state = IntroState.Playing;
        }

        private void HandleIntroCompleted()
        {
            CompleteIntro();
        }

        private void ApplyHiddenVisualState()
        {
            if (!hasCachedStaticState || boardView == null || gridBackgroundRenderer == null)
            {
                return;
            }

            boardView.transform.localPosition = boardBaseLocalPosition + Vector3.up * boardStartOffsetY;
            boardView.transform.localScale = boardBaseLocalScale * boardStartScale;
            gridBackgroundRenderer.color = WithAlpha(boardBackgroundBaseColor, 0f);
            for (var index = 0; index < boardVisuals.Count; index++)
            {
                var visual = boardVisuals[index];
                for (var rendererIndex = 0; rendererIndex < visual.Renderers.Length; rendererIndex++)
                {
                    var renderer = visual.Renderers[rendererIndex];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.color = WithAlpha(visual.BaseColors[rendererIndex], 0f);
                }
            }

            for (var index = 0; index < topBarVisuals.Count; index++)
            {
                var visual = topBarVisuals[index];
                var startOffset = visual.IsBackground ? topBarBackgroundStartOffsetY : topBarElementStartOffsetY;
                var startScale = visual.IsBackground ? topBarBackgroundStartScale : topBarElementStartScale;
                if (visual.RectTransform == null || visual.CanvasGroup == null)
                {
                    continue;
                }

                visual.RectTransform.anchoredPosition = visual.BaseAnchoredPosition + Vector2.up * startOffset;
                visual.RectTransform.localScale = visual.BaseLocalScale * startScale;
                visual.CanvasGroup.alpha = 0f;
            }
        }

        private void ApplyCompletedVisualState()
        {
            if (!hasCachedStaticState || boardView == null || gridBackgroundRenderer == null)
            {
                return;
            }

            boardView.transform.localPosition = boardBaseLocalPosition;
            boardView.transform.localScale = boardBaseLocalScale;
            gridBackgroundRenderer.color = boardBackgroundBaseColor;

            for (var index = 0; index < boardVisuals.Count; index++)
            {
                var visual = boardVisuals[index];
                for (var rendererIndex = 0; rendererIndex < visual.Renderers.Length; rendererIndex++)
                {
                    var renderer = visual.Renderers[rendererIndex];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.color = visual.BaseColors[rendererIndex];
                }
            }

            for (var index = 0; index < topBarVisuals.Count; index++)
            {
                var visual = topBarVisuals[index];
                if (visual.RectTransform == null || visual.CanvasGroup == null)
                {
                    continue;
                }

                visual.RectTransform.anchoredPosition = visual.BaseAnchoredPosition;
                visual.RectTransform.localScale = visual.BaseLocalScale;
                visual.CanvasGroup.alpha = visual.BaseAlpha;
            }
        }

        private void BuildBoardVisualState()
        {
            boardVisuals.Clear();

            var boardChildren = new List<Transform>();
            for (var index = 0; index < boardView.transform.childCount; index++)
            {
                var child = boardView.transform.GetChild(index);
                if (child == null || child.gameObject == gridBackgroundRenderer.gameObject)
                {
                    continue;
                }

                if (child.GetComponentsInChildren<SpriteRenderer>(includeInactive: true).Length == 0)
                {
                    continue;
                }

                boardChildren.Add(child);
            }

            boardChildren.Sort((left, right) =>
            {
                var vertical = right.position.y.CompareTo(left.position.y);
                return vertical != 0 ? vertical : left.position.x.CompareTo(right.position.x);
            });

            for (var index = 0; index < boardChildren.Count; index++)
            {
                var renderers = boardChildren[index].GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
                var colors = new Color[renderers.Length];
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    colors[rendererIndex] = renderers[rendererIndex].color;
                }

                boardVisuals.Add(new BoardVisualState(renderers, colors, index * itemRevealStaggerSeconds));
            }
        }

        private void BuildTopBarVisualState()
        {
            topBarVisuals.Clear();
            topBarVisuals.Add(BuildTopBarState(topBarBackground, topBarRevealDelaySeconds, isBackground: true));
            topBarVisuals.Add(BuildTopBarState(moveTextTransform, topBarRevealDelaySeconds + topBarElementStaggerSeconds, isBackground: false));
            topBarVisuals.Add(BuildTopBarState(moveNumberTransform, topBarRevealDelaySeconds + topBarElementStaggerSeconds * 2f, isBackground: false));
            topBarVisuals.Add(BuildTopBarState(goalTextTransform, topBarRevealDelaySeconds + topBarElementStaggerSeconds * 3f, isBackground: false));

            var goalRoots = new List<RectTransform>();
            for (var index = 0; index < canvasRoot.childCount; index++)
            {
                var child = canvasRoot.GetChild(index);
                if (child is not RectTransform rectTransform)
                {
                    continue;
                }

                var childObject = child.gameObject;
                if (childObject == backgroundRoot
                    || childObject == winPresentationRoot
                    || childObject == losePopupRoot
                    || childObject == topBarBackground.gameObject
                    || childObject == moveTextTransform.gameObject
                    || childObject == moveNumberTransform.gameObject
                    || childObject == goalTextTransform.gameObject
                    || !childObject.name.StartsWith("Goal", StringComparison.Ordinal))
                {
                    continue;
                }

                goalRoots.Add(rectTransform);
            }

            goalRoots.Sort((left, right) => left.anchoredPosition.x.CompareTo(right.anchoredPosition.x));
            for (var index = 0; index < goalRoots.Count; index++)
            {
                topBarVisuals.Add(BuildTopBarState(
                    goalRoots[index],
                    topBarRevealDelaySeconds + topBarElementStaggerSeconds * (4f + index),
                    isBackground: false));
            }
        }

        private TopBarVisualState BuildTopBarState(RectTransform rectTransform, float delaySeconds, bool isBackground)
        {
            var canvasGroup = rectTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }

            return new TopBarVisualState(
                rectTransform,
                canvasGroup,
                rectTransform.anchoredPosition,
                rectTransform.localScale,
                canvasGroup.alpha <= 0f ? 1f : canvasGroup.alpha,
                delaySeconds,
                isBackground);
        }

        private void ClearDynamicState()
        {
            boardVisuals.Clear();
            topBarVisuals.Clear();
        }

        private void CompleteAndReset()
        {
            if (!hasCachedStaticState)
            {
                return;
            }

            StopIntroSequence();
            ApplyCompletedVisualState();
            ClearDynamicState();
            state = IntroState.Hidden;

            if (inputBridge != null)
            {
                inputBridge.SetInputSuppressed(false);
            }
        }

        private void CompleteIntro()
        {
            StopIntroSequence();
            ApplyCompletedVisualState();
            state = IntroState.Completed;
            inputBridge.SetInputSuppressed(false);
        }

        // Intro state is built from live scene objects after bootstrap render,
        // so interrupted tweens must be killed before restoring cached transforms.
        private void StopIntroSequence()
        {
            if (introSequence == null)
            {
                return;
            }

            var sequence = introSequence;
            introSequence = null;
            sequence.Kill();
        }

        private Tween TweenSpriteColor(SpriteRenderer renderer, Color targetColor, float durationSeconds, Ease ease)
        {
            return DOTween.To(
                    () => renderer.color,
                    value => renderer.color = value,
                    targetColor,
                    durationSeconds)
                .SetEase(ease);
        }

        private Sequence TweenSpriteRenderers(SpriteRenderer[] renderers, Color[] targetColors, float durationSeconds, Ease ease)
        {
            var sequence = DOTween.Sequence();
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                sequence.Join(TweenSpriteColor(renderer, targetColors[index], durationSeconds, ease));
            }

            return sequence;
        }

        private RectTransform FindCanvasChild(string name)
        {
            for (var index = 0; index < canvasRoot.childCount; index++)
            {
                var child = canvasRoot.GetChild(index);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private enum IntroState
        {
            Hidden,
            Pending,
            Playing,
            Completed
        }

        private readonly struct BoardVisualState
        {
            public BoardVisualState(SpriteRenderer[] renderers, Color[] baseColors, float delaySeconds)
            {
                Renderers = renderers ?? Array.Empty<SpriteRenderer>();
                BaseColors = baseColors ?? Array.Empty<Color>();
                DelaySeconds = delaySeconds;
            }

            public SpriteRenderer[] Renderers { get; }

            public Color[] BaseColors { get; }

            public float DelaySeconds { get; }
        }

        private readonly struct TopBarVisualState
        {
            public TopBarVisualState(
                RectTransform rectTransform,
                CanvasGroup canvasGroup,
                Vector2 baseAnchoredPosition,
                Vector3 baseLocalScale,
                float baseAlpha,
                float delaySeconds,
                bool isBackground)
            {
                RectTransform = rectTransform;
                CanvasGroup = canvasGroup;
                BaseAnchoredPosition = baseAnchoredPosition;
                BaseLocalScale = baseLocalScale;
                BaseAlpha = baseAlpha;
                DelaySeconds = delaySeconds;
                IsBackground = isBackground;
            }

            public RectTransform RectTransform { get; }

            public CanvasGroup CanvasGroup { get; }

            public Vector2 BaseAnchoredPosition { get; }

            public Vector3 BaseLocalScale { get; }

            public float BaseAlpha { get; }

            public float DelaySeconds { get; }

            public bool IsBackground { get; }
        }
    }
}
