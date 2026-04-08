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

        private readonly List<ActiveDestructionVisual> activeVisuals = new List<ActiveDestructionVisual>();
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => duration;

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

            foreach (var coordinate in descriptor.RemovedItemCoordinates)
            {
                Register(boardView.CreateTransientItemVisual(sourceBoard, coordinate, root, effectZ));
            }

            foreach (var removedObstacle in descriptor.RemovedObstacles)
            {
                Register(boardView.CreateTransientObstacleVisual(sourceBoard, removedObstacle.OccupiedCoordinates, root, effectZ));
            }

            ApplyCurrentState();
            return activeVisuals.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentState();

            if (elapsed >= duration)
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
        }

        private void Register(GameObject root)
        {
            activeVisuals.Add(new ActiveDestructionVisual(
                root,
                root.transform.localScale,
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true)));
        }

        private void ApplyCurrentState()
        {
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            var easedProgress = EaseOutCubic(progress);
            var scaleMultiplier = Mathf.Lerp(startScaleMultiplier, endScaleMultiplier, easedProgress);
            var alpha = 1f - easedProgress;

            foreach (var visual in activeVisuals)
            {
                if (visual.Root is null)
                {
                    continue;
                }

                visual.Root.transform.localScale = visual.InitialScale * scaleMultiplier;

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
        }

        private static float EaseOutCubic(float progress)
        {
            var inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private readonly struct ActiveDestructionVisual
        {
            public ActiveDestructionVisual(GameObject root, Vector3 initialScale, SpriteRenderer[] renderers)
            {
                Root = root;
                InitialScale = initialScale;
                Renderers = renderers;
            }

            public GameObject Root { get; }

            public Vector3 InitialScale { get; }

            public SpriteRenderer[] Renderers { get; }
        }
    }
}
