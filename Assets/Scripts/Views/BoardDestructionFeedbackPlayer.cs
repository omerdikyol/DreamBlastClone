using System;
using System.Collections.Generic;
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
        [SerializeField] private float staggerStepDelay = 0.035f;

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

            var maxStartDelay = 0f;

            foreach (var removedItem in descriptor.RemovedItems)
            {
                var startDelay = Mathf.Max(0f, removedItem.HitStep * staggerStepDelay);
                maxStartDelay = Mathf.Max(maxStartDelay, startDelay);
                Register(boardView.CreateTransientItemVisual(sourceBoard, removedItem.Coordinate, root, effectZ), startDelay);
            }

            foreach (var removedObstacle in descriptor.RemovedObstacles)
            {
                Register(boardView.CreateTransientObstacleVisual(sourceBoard, removedObstacle.OccupiedCoordinates, root, effectZ), startDelay: 0f);
            }

            activeDuration = duration + maxStartDelay;
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
                    Destroy(activeVisuals[index].Root);
                }
                else
                {
                    DestroyImmediate(activeVisuals[index].Root);
                }
            }

            activeVisuals.Clear();
            elapsed = 0f;
            activeDuration = 0f;
        }

        private void Register(GameObject root, float startDelay)
        {
            activeVisuals.Add(new ActiveDestructionVisual(
                root,
                root.transform.localScale,
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true),
                startDelay));
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
                    visual.Root.transform.localScale = visual.InitialScale;
                    ApplyAlpha(visual, alpha: 1f);
                    continue;
                }

                var progress = duration > 0f ? Mathf.Clamp01(localElapsed / duration) : 1f;
                var easedProgress = EaseOutCubic(progress);
                var scaleMultiplier = Mathf.Lerp(startScaleMultiplier, endScaleMultiplier, easedProgress);
                var alpha = 1f - easedProgress;
                visual.Root.transform.localScale = visual.InitialScale * scaleMultiplier;
                ApplyAlpha(visual, alpha);
            }
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

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private readonly struct ActiveDestructionVisual
        {
            public ActiveDestructionVisual(GameObject root, Vector3 initialScale, SpriteRenderer[] renderers, float startDelay)
            {
                Root = root;
                InitialScale = initialScale;
                Renderers = renderers;
                StartDelay = startDelay;
            }

            public GameObject Root { get; }

            public Vector3 InitialScale { get; }

            public SpriteRenderer[] Renderers { get; }

            public float StartDelay { get; }
        }
    }
}
