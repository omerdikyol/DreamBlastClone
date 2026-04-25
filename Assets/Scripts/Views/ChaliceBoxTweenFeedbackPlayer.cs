using System;
using System.Collections.Generic;
using DG.Tweening;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class ChaliceBoxTweenFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.14f;
        [SerializeField] private float eventStepDelay = 0.09f;
        [SerializeField] private float effectZ = -0.18f;
        [SerializeField] private float doorDamageRotationDegrees = 4f;
        [SerializeField] private float doorBreakScaleMultiplier = 1.16f;
        [SerializeField] private float chaliceDamageScaleMultiplier = 1.08f;
        [SerializeField] private float chaliceCompleteScaleMultiplier = 1.22f;

        private readonly List<GameObject> activeVisuals = new List<GameObject>();
        private readonly List<Sequence> activeTweens = new List<Sequence>();
        private readonly List<PendingFeedbackEvent> pendingEvents = new List<PendingFeedbackEvent>();
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0 || pendingEvents.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, BoardModel preTapBoard, ChaliceBoxParticleDescriptor descriptor)
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
            foreach (var feedbackEvent in descriptor.Events)
            {
                var delay = Mathf.Max(0f, feedbackEvent.HitStep) * Mathf.Max(0f, eventStepDelay);
                if (delay > 0f)
                {
                    pendingEvents.Add(new PendingFeedbackEvent(boardView, preTapBoard, root, feedbackEvent, delay));
                    continue;
                }

                if (!TryStartFeedbackEvent(boardView, preTapBoard, root, feedbackEvent))
                {
                    continue;
                }
            }

            return activeVisuals.Count > 0 || pendingEvents.Count > 0;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            for (var index = pendingEvents.Count - 1; index >= 0; index--)
            {
                var pendingEvent = pendingEvents[index];
                pendingEvent.RemainingDelay -= safeDeltaTime;
                if (pendingEvent.RemainingDelay > 0f)
                {
                    continue;
                }

                TryStartFeedbackEvent(
                    pendingEvent.BoardView,
                    pendingEvent.PreTapBoard,
                    pendingEvent.Root,
                    pendingEvent.Event);
                elapsed = 0f;
                pendingEvents.RemoveAt(index);
            }

            if (activeVisuals.Count > 0)
            {
                elapsed = Mathf.Min(duration, elapsed + safeDeltaTime);
                if (elapsed >= duration)
                {
                    Stop();
                }
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
            pendingEvents.Clear();
            elapsed = 0f;
        }

        private bool TryStartFeedbackEvent(
            BoardView boardView,
            BoardModel preTapBoard,
            Transform root,
            ChaliceBoxParticleEvent feedbackEvent)
        {
            if (!TryCreateVisual(boardView, preTapBoard, root, feedbackEvent, out var visual))
            {
                return false;
            }

            activeVisuals.Add(visual);
            activeTweens.Add(CreateTween(visual, feedbackEvent.EventType));
            return true;
        }

        private bool TryCreateVisual(
            BoardView boardView,
            BoardModel preTapBoard,
            Transform root,
            ChaliceBoxParticleEvent feedbackEvent,
            out GameObject visual)
        {
            visual = null;

            if (!preTapBoard.TryGetCell(feedbackEvent.Anchor, out var cell)
                || cell.Obstacle is not ChaliceBoxObstacleModel chaliceBox)
            {
                return false;
            }

            visual = boardView.CreateTransientObstacleVisual(preTapBoard, FindFootprint(preTapBoard, chaliceBox), root, effectZ);
            return true;
        }

        private Sequence CreateTween(GameObject visual, ChaliceBoxParticleEventType eventType)
        {
            var target = visual.transform;
            var baseScale = target.localScale;
            var baseRotation = target.localEulerAngles;
            var halfDuration = Mathf.Max(0.03f, duration * 0.5f);
            var renderers = visual.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            var sequence = DOTween.Sequence();
            switch (eventType)
            {
                case ChaliceBoxParticleEventType.DoorDamage:
                    sequence
                        .Append(target.DOLocalRotate(baseRotation + Vector3.forward * doorDamageRotationDegrees, halfDuration * 0.5f, RotateMode.Fast).SetEase(Ease.OutSine))
                        .Append(target.DOLocalRotate(baseRotation - Vector3.forward * doorDamageRotationDegrees, halfDuration, RotateMode.Fast).SetEase(Ease.InOutSine))
                        .Append(target.DOLocalRotate(baseRotation, halfDuration * 0.5f, RotateMode.Fast).SetEase(Ease.InSine));
                    break;
                case ChaliceBoxParticleEventType.DoorBreak:
                    sequence
                        .Append(target.DOScale(baseScale * doorBreakScaleMultiplier, halfDuration).SetEase(Ease.OutBack))
                        .Join(target.DOLocalRotate(baseRotation + Vector3.forward * (doorDamageRotationDegrees * 1.4f), halfDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                        .Append(target.DOScale(baseScale * 0.96f, halfDuration).SetEase(Ease.InSine))
                        .Join(FadeRenderers(renderers, 0.25f, halfDuration));
                    break;
                case ChaliceBoxParticleEventType.ChaliceDamage:
                    sequence
                        .Append(target.DOScale(baseScale * chaliceDamageScaleMultiplier, halfDuration).SetEase(Ease.OutBack))
                        .Append(target.DOScale(baseScale, halfDuration).SetEase(Ease.InSine));
                    break;
                case ChaliceBoxParticleEventType.ChaliceComplete:
                    sequence
                        .Append(target.DOScale(baseScale * chaliceCompleteScaleMultiplier, halfDuration).SetEase(Ease.OutBack))
                        .Append(target.DOScale(baseScale * 0.2f, halfDuration).SetEase(Ease.InBack))
                        .Join(FadeRenderers(renderers, 0f, halfDuration));
                    break;
            }

            return sequence
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

        private static IReadOnlyList<BoardCoordinate> FindFootprint(BoardModel board, ObstacleModel obstacle)
        {
            var coordinates = new List<BoardCoordinate>();

            foreach (var cell in board.GetAllCells())
            {
                if (ReferenceEquals(cell.Obstacle, obstacle))
                {
                    coordinates.Add(cell.Coordinate);
                }
            }

            return coordinates;
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

        private sealed class PendingFeedbackEvent
        {
            public PendingFeedbackEvent(
                BoardView boardView,
                BoardModel preTapBoard,
                Transform root,
                ChaliceBoxParticleEvent feedbackEvent,
                float remainingDelay)
            {
                BoardView = boardView;
                PreTapBoard = preTapBoard;
                Root = root;
                Event = feedbackEvent;
                RemainingDelay = remainingDelay;
            }

            public BoardView BoardView { get; }

            public BoardModel PreTapBoard { get; }

            public Transform Root { get; }

            public ChaliceBoxParticleEvent Event { get; }

            public float RemainingDelay { get; set; }
        }
    }
}
