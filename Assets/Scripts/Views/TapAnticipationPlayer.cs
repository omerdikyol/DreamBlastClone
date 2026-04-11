using System;
using System.Collections.Generic;
using DG.Tweening;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Obstacles;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class TapAnticipationPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private float effectZ = -0.22f;
        [SerializeField] private float cubeScaleMultiplier = 1.12f;
        [SerializeField] private float rocketScaleMultiplier = 1.16f;
        [SerializeField] private float tntScaleMultiplier = 1.18f;
        [SerializeField] private float rocketRotationDegrees = 6f;
        [SerializeField] private float tntSquashAmount = 0.08f;
        [SerializeField] private float invalidShakeDistance = 0.06f;

        private readonly List<GameObject> activeVisuals = new List<GameObject>();
        private readonly List<Sequence> activeTweens = new List<Sequence>();
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => duration;

        public bool TryPlay(BoardView boardView, BoardModel preTapBoard, TapAnticipationDescriptor descriptor)
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

            if (duration <= 0f || !descriptor.HasAnyFeedback)
            {
                return false;
            }

            Stop();
            PresentationTweenBootstrap.EnsureInitialized();
            elapsed = 0f;

            var root = effectRoot != null ? effectRoot : transform;
            foreach (var tapEvent in descriptor.Events)
            {
                if (!TryCreateTransientVisual(boardView, preTapBoard, tapEvent, root, effectZ, out var visual))
                {
                    continue;
                }

                activeVisuals.Add(visual);
                activeTweens.Add(CreateTween(visual.transform, tapEvent.EventType));
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

        private Sequence CreateTween(Transform target, TapAnticipationEventType eventType)
        {
            var baseScale = target.localScale;
            var basePosition = target.localPosition;
            var baseRotation = target.localEulerAngles;
            var halfDuration = Mathf.Max(0.03f, duration * 0.5f);

            return eventType switch
            {
                TapAnticipationEventType.Invalid => DOTween.Sequence()
                    .Append(target.DOLocalMoveX(basePosition.x - invalidShakeDistance, halfDuration * 0.5f).SetEase(Ease.OutSine))
                    .Append(target.DOLocalMoveX(basePosition.x + invalidShakeDistance, halfDuration).SetEase(Ease.InOutSine))
                    .Append(target.DOLocalMoveX(basePosition.x, halfDuration * 0.5f).SetEase(Ease.InSine))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy),
                TapAnticipationEventType.Rocket => DOTween.Sequence()
                    .Append(target.DOScale(baseScale * rocketScaleMultiplier, halfDuration).SetEase(Ease.OutBack))
                    .Join(target.DOLocalRotate(baseRotation + Vector3.forward * rocketRotationDegrees, halfDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                    .Append(target.DOScale(baseScale, halfDuration).SetEase(Ease.InSine))
                    .Join(target.DOLocalRotate(baseRotation, halfDuration, RotateMode.Fast).SetEase(Ease.InSine))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy),
                TapAnticipationEventType.Tnt => DOTween.Sequence()
                    .Append(target.DOScale(new Vector3(
                        baseScale.x * tntScaleMultiplier,
                        baseScale.y * (1f - tntSquashAmount),
                        baseScale.z), halfDuration).SetEase(Ease.OutBack))
                    .Append(target.DOScale(baseScale, halfDuration).SetEase(Ease.InSine))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy),
                _ => DOTween.Sequence()
                    .Append(target.DOScale(baseScale * cubeScaleMultiplier, halfDuration).SetEase(Ease.OutBack))
                    .Append(target.DOScale(baseScale, halfDuration).SetEase(Ease.InSine))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                    .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy)
            };
        }

        private static bool TryCreateTransientVisual(
            BoardView boardView,
            BoardModel preTapBoard,
            TapAnticipationEvent tapEvent,
            Transform root,
            float effectZ,
            out GameObject visual)
        {
            visual = null;

            if (!preTapBoard.TryGetCell(tapEvent.Coordinate, out var cell))
            {
                return false;
            }

            if (cell.HasItem)
            {
                visual = boardView.CreateTransientItemVisual(preTapBoard, tapEvent.Coordinate, root, effectZ);
                return true;
            }

            if (tapEvent.EventType != TapAnticipationEventType.Invalid || !cell.HasObstacle)
            {
                return false;
            }

            visual = boardView.CreateTransientObstacleVisual(
                preTapBoard,
                FindObstacleFootprint(preTapBoard, cell.Obstacle),
                root,
                effectZ);
            return true;
        }

        private static IReadOnlyList<BoardCoordinate> FindObstacleFootprint(BoardModel board, ObstacleModel obstacle)
        {
            var occupiedCoordinates = new List<BoardCoordinate>();

            foreach (var cell in board.GetAllCells())
            {
                if (ReferenceEquals(cell.Obstacle, obstacle))
                {
                    occupiedCoordinates.Add(cell.Coordinate);
                }
            }

            return occupiedCoordinates;
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
