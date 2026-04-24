using System;
using System.Collections.Generic;
using DG.Tweening;
using DreamBlastClone.Grid;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class BoardDestructionFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.14f;
        [SerializeField] private float effectZ = -0.15f;
        [SerializeField] private float startScaleMultiplier = 1.08f;
        [SerializeField] private float endScaleMultiplier = 0.15f;
        [SerializeField] private float staggerStepDelay = 0.09f;
        [SerializeField] private float growBeforeRemovalScaleMultiplier = 3f;
        [SerializeField] private float growBeforeRemovalDuration = 1.2f;
        [SerializeField] private float growBeforeRemovalOvershootMultiplier = 1.08f;
        [SerializeField] private float growBeforeRemovalSquashMultiplier = 0.9f;
        [SerializeField] private float growBeforeRemovalRotationDegrees = 6f;
        [SerializeField] private int growBeforeRemovalSortingOrderBoost = 30;
        [SerializeField] private float tntWaveStepDelay = 0.055f;
        [SerializeField] private float tntWaveRemovalDuration = 0.28f;

        private readonly List<ActiveDestructionVisual> activeVisuals = new List<ActiveDestructionVisual>();
        private float elapsed;
        private float activeDuration;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => activeDuration > 0f ? activeDuration : duration;

        public bool TryPlay(BoardView boardView, BoardModel sourceBoard, BoardDestructionFeedbackDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (sourceBoard is null)
            {
                throw new ArgumentNullException(nameof(sourceBoard));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (duration <= 0f || !descriptor.HasAnyFeedback)
            {
                return false;
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;

            var isTntWave = ContainsGrowBeforeRemovalItem(descriptor);
            var sharedGrowDelay = isTntWave
                ? Mathf.Max(0f, growBeforeRemovalDuration)
                : 0f;
            var stepDelay = isTntWave
                ? Mathf.Max(0f, tntWaveStepDelay)
                : Mathf.Max(0f, staggerStepDelay);
            var removalDuration = isTntWave
                ? Mathf.Max(0.01f, tntWaveRemovalDuration)
                : duration;
            var maxEndTime = 0f;

            foreach (var removedItem in descriptor.RemovedItems)
            {
                var startDelay = sharedGrowDelay + Mathf.Max(0f, removedItem.HitStep * stepDelay);
                maxEndTime = Mathf.Max(maxEndTime, startDelay + removalDuration);
                var visual = boardView.CreateTransientItemVisual(sourceBoard, removedItem.Coordinate, root, effectZ);
                Register(
                    visual,
                    startDelay,
                    removalDuration,
                    removedItem.GrowsBeforeRemoval,
                    smoothRemoval: isTntWave,
                    growTween: CreateGrowTween(visual.transform, removedItem.GrowsBeforeRemoval));
            }

            foreach (var removedObstacle in descriptor.RemovedObstacles)
            {
                var startDelay = sharedGrowDelay + Mathf.Max(0f, removedObstacle.HitStep * stepDelay);
                maxEndTime = Mathf.Max(maxEndTime, startDelay + removalDuration);
                var visual = boardView.CreateTransientObstacleVisual(sourceBoard, removedObstacle.OccupiedCoordinates, root, effectZ);
                Register(
                    visual,
                    startDelay,
                    removalDuration,
                    growsBeforeRemoval: false,
                    smoothRemoval: isTntWave,
                    growTween: null);
            }

            activeDuration = maxEndTime;
            ApplyCurrentState();
            return activeVisuals.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(Duration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentState();

            if (elapsed >= Duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            for (var index = activeVisuals.Count - 1; index >= 0; index--)
            {
                if (Application.isPlaying)
                {
                    activeVisuals[index].GrowTween?.Kill();
                    Destroy(activeVisuals[index].Root);
                }
                else
                {
                    activeVisuals[index].GrowTween?.Kill();
                    DestroyImmediate(activeVisuals[index].Root);
                }
            }

            activeVisuals.Clear();
            elapsed = 0f;
            activeDuration = 0f;
        }

        private void Register(
            GameObject root,
            float startDelay,
            float removalDuration,
            bool growsBeforeRemoval,
            bool smoothRemoval,
            Sequence growTween)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            if (growsBeforeRemoval)
            {
                BoostSortingOrder(renderers, growBeforeRemovalSortingOrderBoost);
            }

            activeVisuals.Add(new ActiveDestructionVisual(
                root,
                root.transform.localScale,
                renderers,
                startDelay,
                removalDuration,
                growsBeforeRemoval,
                smoothRemoval,
                growTween));
        }

        private void ApplyCurrentState()
        {
            foreach (var visual in activeVisuals)
            {
                if (visual.Root is null)
                {
                    continue;
                }

                var localElapsed = elapsed - visual.StartDelay;
                if (localElapsed <= 0f)
                {
                    if (!visual.GrowsBeforeRemoval)
                    {
                        visual.Root.transform.localScale = visual.InitialScale;
                    }

                    ApplyAlpha(visual, alpha: 1f);
                    continue;
                }

                visual.GrowTween?.Kill();
                var progress = visual.RemovalDuration > 0f ? Mathf.Clamp01(localElapsed / visual.RemovalDuration) : 1f;
                var easedProgress = visual.SmoothRemoval ? SmoothStep(progress) : EaseOutCubic(progress);
                var startScale = visual.GrowsBeforeRemoval
                    ? growBeforeRemovalScaleMultiplier
                    : startScaleMultiplier;
                var scaleMultiplier = Mathf.Lerp(startScale, endScaleMultiplier, easedProgress);
                var alpha = 1f - easedProgress;
                visual.Root.transform.localScale = visual.InitialScale * scaleMultiplier;
                ApplyAlpha(visual, alpha);
            }
        }

        private Sequence CreateGrowTween(Transform target, bool growsBeforeRemoval)
        {
            if (!growsBeforeRemoval || target is null)
            {
                return null;
            }

            var baseScale = target.localScale;
            var targetScale = baseScale * Mathf.Max(1f, growBeforeRemovalScaleMultiplier);
            var overshootScale = targetScale * Mathf.Max(1f, growBeforeRemovalOvershootMultiplier);
            var squashScale = new Vector3(
                targetScale.x * Mathf.Max(0.01f, growBeforeRemovalSquashMultiplier),
                targetScale.y / Mathf.Max(0.01f, growBeforeRemovalSquashMultiplier),
                targetScale.z);
            var durationSeconds = Mathf.Max(0.01f, growBeforeRemovalDuration);
            var firstStep = durationSeconds * 0.52f;
            var secondStep = durationSeconds * 0.22f;
            var finalStep = durationSeconds - firstStep - secondStep;
            var baseRotation = target.localEulerAngles;

            return DOTween.Sequence()
                .Append(target.DOScale(overshootScale, firstStep).SetEase(Ease.OutBack))
                .Join(target.DOLocalRotate(baseRotation + Vector3.forward * growBeforeRemovalRotationDegrees, firstStep, RotateMode.Fast).SetEase(Ease.OutSine))
                .Append(target.DOScale(squashScale, secondStep).SetEase(Ease.InOutSine))
                .Join(target.DOLocalRotate(baseRotation - Vector3.forward * growBeforeRemovalRotationDegrees * 0.6f, secondStep, RotateMode.Fast).SetEase(Ease.InOutSine))
                .Append(target.DOScale(targetScale, finalStep).SetEase(Ease.OutBack))
                .Join(target.DOLocalRotate(baseRotation, finalStep, RotateMode.Fast).SetEase(Ease.OutSine));
        }

        private static bool ContainsGrowBeforeRemovalItem(BoardDestructionFeedbackDescriptor descriptor)
        {
            foreach (var removedItem in descriptor.RemovedItems)
            {
                if (removedItem.GrowsBeforeRemoval)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyAlpha(ActiveDestructionVisual visual, float alpha)
        {
            foreach (var spriteRenderer in visual.Renderers)
            {
                if (spriteRenderer is null)
                {
                    continue;
                }

                var color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }

        private static void BoostSortingOrder(IReadOnlyList<SpriteRenderer> renderers, int sortingOrderBoost)
        {
            if (sortingOrderBoost <= 0)
            {
                return;
            }

            for (var index = 0; index < renderers.Count; index++)
            {
                if (renderers[index] is not null)
                {
                    renderers[index].sortingOrder += sortingOrderBoost;
                }
            }
        }

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private static float SmoothStep(float progress)
        {
            return progress * progress * (3f - 2f * progress);
        }

        private readonly struct ActiveDestructionVisual
        {
            public ActiveDestructionVisual(
                GameObject root,
                Vector3 initialScale,
                SpriteRenderer[] renderers,
                float startDelay,
                float removalDuration,
                bool growsBeforeRemoval,
                bool smoothRemoval,
                Sequence growTween)
            {
                Root = root;
                InitialScale = initialScale;
                Renderers = renderers;
                StartDelay = startDelay;
                RemovalDuration = removalDuration;
                GrowsBeforeRemoval = growsBeforeRemoval;
                SmoothRemoval = smoothRemoval;
                GrowTween = growTween;
            }

            public GameObject Root { get; }

            public Vector3 InitialScale { get; }

            public SpriteRenderer[] Renderers { get; }

            public float StartDelay { get; }

            public float RemovalDuration { get; }

            public bool GrowsBeforeRemoval { get; }

            public bool SmoothRemoval { get; }

            public Sequence GrowTween { get; }
        }
    }
}
