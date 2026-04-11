using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class ChaliceBoxTweenPresentationView : MonoBehaviour
    {
        [SerializeField] private float doorIdleDuration = 1.45f;
        [SerializeField] private float doorIdleScaleAmplitude = 0.018f;
        [SerializeField] private float doorIdleRotationDegrees = 1.4f;
        [SerializeField] private float chaliceIdleDuration = 1.25f;
        [SerializeField] private float chaliceIdleScaleAmplitude = 0.045f;
        [SerializeField] private float chaliceIdleFloatDistance = 0.018f;
        [SerializeField] private float removedChaliceDuration = 0.18f;
        [SerializeField] private float removedChaliceLiftDistance = 0.08f;

        private readonly List<Sequence> activeTweens = new List<Sequence>();
        private readonly List<GameObject> removalClones = new List<GameObject>();
        private readonly List<TransformState> capturedTransforms = new List<TransformState>();

        public bool IsPlaying => activeTweens.Count > 0;

        public int ActiveRemovalCloneCount => removalClones.Count;

        public void PlayState(
            SpriteRenderer background,
            SpriteRenderer doors,
            IReadOnlyList<SpriteRenderer> slotRenderers,
            bool isDoorPhase,
            bool startTweenPresentation,
            IReadOnlyList<int> removedSlotIndices,
            float phaseOffset)
        {
            StopAndReset();

            if (!startTweenPresentation)
            {
                return;
            }

            PresentationTweenBootstrap.EnsureInitialized();

            if (isDoorPhase)
            {
                StartDoorIdle(doors != null ? doors.transform : background?.transform, phaseOffset);
                return;
            }

            StartChaliceSlotIdle(slotRenderers, phaseOffset);
            StartRemovedChaliceTweens(slotRenderers, removedSlotIndices);
        }

        public void StopAndReset()
        {
            for (var index = activeTweens.Count - 1; index >= 0; index--)
            {
                activeTweens[index]?.Kill();
            }

            activeTweens.Clear();

            for (var index = 0; index < capturedTransforms.Count; index++)
            {
                capturedTransforms[index].Restore();
            }

            capturedTransforms.Clear();

            for (var index = removalClones.Count - 1; index >= 0; index--)
            {
                DestroyObject(removalClones[index]);
            }

            removalClones.Clear();
        }

        private void OnDisable()
        {
            StopAndReset();
        }

        private void OnDestroy()
        {
            StopAndReset();
        }

        private void StartDoorIdle(Transform target, float phaseOffset)
        {
            if (target is null)
            {
                return;
            }

            CaptureTransform(target);

            var baseScale = target.localScale;
            var baseRotation = target.localEulerAngles;
            var halfDuration = Mathf.Max(0.05f, doorIdleDuration * 0.5f);
            var tween = DOTween.Sequence()
                .Join(target.DOScale(baseScale * (1f + doorIdleScaleAmplitude), halfDuration).SetEase(Ease.InOutSine))
                .Join(target.DOLocalRotate(baseRotation + Vector3.forward * doorIdleRotationDegrees, halfDuration, RotateMode.Fast).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            tween.Goto(GetGlobalPhase(doorIdleDuration, phaseOffset), andPlay: true);
            activeTweens.Add(tween);
        }

        private void StartChaliceSlotIdle(IReadOnlyList<SpriteRenderer> slotRenderers, float phaseOffset)
        {
            if (slotRenderers is null)
            {
                return;
            }

            for (var index = 0; index < slotRenderers.Count; index++)
            {
                var renderer = slotRenderers[index];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var target = renderer.transform;
                CaptureTransform(target);

                var duration = Mathf.Max(0.1f, chaliceIdleDuration + (index % 3) * 0.09f);
                var halfDuration = duration * 0.5f;
                var baseScale = target.localScale;
                var basePosition = target.localPosition;
                var targetScale = baseScale * (1f + chaliceIdleScaleAmplitude);
                var targetPosition = basePosition + Vector3.up * chaliceIdleFloatDistance;

                var tween = DOTween.Sequence()
                    .Join(target.DOScale(targetScale, halfDuration).SetEase(Ease.InOutSine))
                    .Join(target.DOLocalMove(targetPosition, halfDuration).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

                tween.Goto(GetGlobalPhase(duration, phaseOffset + index * 0.17f), andPlay: true);
                activeTweens.Add(tween);
            }
        }

        private void StartRemovedChaliceTweens(IReadOnlyList<SpriteRenderer> slotRenderers, IReadOnlyList<int> removedSlotIndices)
        {
            if (slotRenderers is null || removedSlotIndices is null)
            {
                return;
            }

            for (var index = 0; index < removedSlotIndices.Count; index++)
            {
                var slotIndex = removedSlotIndices[index];
                if (slotIndex < 0 || slotIndex >= slotRenderers.Count)
                {
                    continue;
                }

                var source = slotRenderers[slotIndex];
                if (source == null || source.sprite is null)
                {
                    continue;
                }

                var clone = new GameObject($"RemovedChaliceSlot_{slotIndex}");
                clone.transform.SetParent(source.transform.parent, worldPositionStays: false);
                clone.transform.localPosition = source.transform.localPosition;
                clone.transform.localRotation = source.transform.localRotation;
                clone.transform.localScale = source.transform.localScale;

                var renderer = clone.AddComponent<SpriteRenderer>();
                renderer.sprite = source.sprite;
                renderer.color = Color.white;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder + 3;

                removalClones.Add(clone);

                var baseScale = clone.transform.localScale;
                var targetPosition = clone.transform.localPosition + Vector3.up * removedChaliceLiftDistance;
                var tween = DOTween.Sequence()
                    .Join(clone.transform.DOScale(baseScale * 0.15f, removedChaliceDuration).SetEase(Ease.InBack))
                    .Join(clone.transform.DOLocalMove(targetPosition, removedChaliceDuration).SetEase(Ease.OutSine))
                    .Join(TweenRendererAlpha(renderer, 0f, removedChaliceDuration).SetEase(Ease.InSine))
                    .OnComplete(() => DestroyRemovalClone(clone))
                    .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                    .SetLink(clone, LinkBehaviour.KillOnDestroy);

                activeTweens.Add(tween);
            }
        }

        private void CaptureTransform(Transform target)
        {
            for (var index = 0; index < capturedTransforms.Count; index++)
            {
                if (capturedTransforms[index].Target == target)
                {
                    return;
                }
            }

            capturedTransforms.Add(new TransformState(target));
        }

        private void DestroyRemovalClone(GameObject clone)
        {
            if (clone is null)
            {
                return;
            }

            removalClones.Remove(clone);
            DestroyObject(clone);
        }

        private static float GetGlobalPhase(float duration, float phaseOffset)
        {
            return Mathf.Repeat(Time.time + phaseOffset, Mathf.Max(0.05f, duration));
        }

        private static Tween TweenRendererAlpha(SpriteRenderer renderer, float targetAlpha, float tweenDuration)
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
                tweenDuration);
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

        private readonly struct TransformState
        {
            public TransformState(Transform target)
            {
                Target = target;
                LocalPosition = target.localPosition;
                LocalScale = target.localScale;
                LocalEulerAngles = target.localEulerAngles;
            }

            public Transform Target { get; }

            private Vector3 LocalPosition { get; }

            private Vector3 LocalScale { get; }

            private Vector3 LocalEulerAngles { get; }

            public void Restore()
            {
                if (Target is null)
                {
                    return;
                }

                Target.localPosition = LocalPosition;
                Target.localScale = LocalScale;
                Target.localEulerAngles = LocalEulerAngles;
            }
        }
    }
}
