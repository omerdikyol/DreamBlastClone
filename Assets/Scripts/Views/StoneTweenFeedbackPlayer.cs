using System;
using System.Collections.Generic;
using DG.Tweening;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class StoneTweenFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.12f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private float impactScaleMultiplier = 0.82f;
        [SerializeField] private float recoilScaleMultiplier = 1.04f;
        [SerializeField] private float endScaleMultiplier = 0.08f;
        [SerializeField] private float impactDipDistance = 0.025f;
        [SerializeField] private float recoilLiftDistance = 0.04f;
        [SerializeField] private float recoilRotationDegrees = 3f;

        private readonly List<GameObject> activeVisuals = new List<GameObject>();
        private readonly List<Sequence> activeTweens = new List<Sequence>();
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, BoardModel preTapBoard, StoneParticleDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (preTapBoard is null)
            {
                throw new ArgumentNullException(nameof(preTapBoard));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (duration <= 0f || !descriptor.HasAnyParticles)
            {
                return false;
            }

            Stop();
            PresentationTweenBootstrap.EnsureInitialized();
            elapsed = 0f;

            var root = effectRoot != null ? effectRoot : transform;
            foreach (var coordinate in descriptor.RemovedCoordinates)
            {
                if (!TryCreateVisual(boardView, preTapBoard, root, coordinate, out var visual))
                {
                    continue;
                }

                activeVisuals.Add(visual);
                activeTweens.Add(CreateTween(visual));
            }

            return activeVisuals.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, deltaTime));
            if (elapsed >= duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            for (var index = activeTweens.Count - 1; index >= 0; index--)
            {
                activeTweens[index]?.Kill();
            }

            activeTweens.Clear();

            for (var index = activeVisuals.Count - 1; index >= 0; index--)
            {
                DestroyObject(activeVisuals[index]);
            }

            activeVisuals.Clear();
            elapsed = 0f;
        }

        private bool TryCreateVisual(
            BoardView boardView,
            BoardModel preTapBoard,
            Transform root,
            BoardCoordinate coordinate,
            out GameObject visual)
        {
            visual = null;

            if (!preTapBoard.TryGetCell(coordinate, out var cell)
                || cell.Obstacle is not StoneObstacleModel)
            {
                return false;
            }

            visual = boardView.CreateTransientObstacleVisual(
                preTapBoard,
                new[] { coordinate },
                root,
                effectZ);
            return true;
        }

        private Sequence CreateTween(GameObject visual)
        {
            var target = visual.transform;
            var baseScale = target.localScale;
            var baseLocalPosition = target.localPosition;
            var baseRotation = target.localEulerAngles;
            var impactDuration = Mathf.Max(0.02f, duration * 0.22f);
            var recoilDuration = Mathf.Max(0.02f, duration * 0.24f);
            var finishDuration = Mathf.Max(0.03f, duration - impactDuration - recoilDuration);
            var renderers = visual.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            return DOTween.Sequence()
                .Append(target.DOScale(baseScale * impactScaleMultiplier, impactDuration).SetEase(Ease.InQuad))
                .Join(target.DOLocalMove(baseLocalPosition + Vector3.down * impactDipDistance, impactDuration).SetEase(Ease.InQuad))
                .Append(target.DOScale(baseScale * recoilScaleMultiplier, recoilDuration).SetEase(Ease.OutQuad))
                .Join(target.DOLocalMove(baseLocalPosition + Vector3.up * recoilLiftDistance, recoilDuration).SetEase(Ease.OutQuad))
                .Join(target.DOLocalRotate(baseRotation + Vector3.forward * recoilRotationDegrees, recoilDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                .Append(target.DOScale(baseScale * endScaleMultiplier, finishDuration).SetEase(Ease.InBack))
                .Join(target.DOLocalMove(baseLocalPosition + Vector3.up * (recoilLiftDistance * 0.25f), finishDuration).SetEase(Ease.InSine))
                .Join(target.DOLocalRotate(baseRotation, finishDuration, RotateMode.Fast).SetEase(Ease.InSine))
                .Join(FadeRenderers(renderers, 0f, finishDuration))
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                .SetLink(visual, LinkBehaviour.KillOnDestroy);
        }

        private static Tween FadeRenderers(IReadOnlyList<SpriteRenderer> renderers, float targetAlpha, float fadeDuration)
        {
            var sequence = DOTween.Sequence();
            for (var index = 0; index < renderers.Count; index++)
            {
                var renderer = renderers[index];
                if (renderer != null && renderer.enabled)
                {
                    sequence.Join(TweenRendererAlpha(renderer, targetAlpha, fadeDuration));
                }
            }

            return sequence;
        }

        private static Tween TweenRendererAlpha(SpriteRenderer renderer, float targetAlpha, float fadeDuration)
        {
            return DOTween.To(
                () => renderer.color.a,
                value =>
                {
                    var color = renderer.color;
                    color.a = value;
                    renderer.color = color;
                },
                targetAlpha,
                fadeDuration);
        }

        private static void DestroyObject(GameObject gameObject)
        {
            if (gameObject is null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
