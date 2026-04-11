using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DreamBlastClone.Views
{
    public class UIButtonFeedbackView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Transform animatedTransform;
        [SerializeField] private float durationSeconds = 1.65f;
        [SerializeField] private float scaleAmplitude = 0.055f;
        [SerializeField] private float pressScaleMultiplier = 0.93f;
        [SerializeField] private float pressDurationSeconds = 0.06f;
        [SerializeField] private float releaseDurationSeconds = 0.18f;
        [SerializeField] private float releaseOvershootScaleMultiplier = 1.025f;

        private Tween idleTween;
        private Tween pressTween;
        private Transform targetTransform;
        private Vector3 baseLocalScale;
        private bool hasBaseScale;
        private bool isIdleRequested;
        private bool isPressed;

        public bool IsPlaying => idleTween != null && idleTween.IsActive() && idleTween.IsPlaying();

        public bool IsPressed => isPressed;

        public void PlayIdle()
        {
            PresentationTweenBootstrap.EnsureInitialized();
            CacheBaseScale(forceRefresh: !IsPlaying && !isPressed);
            isIdleRequested = true;

            if (isPressed || IsPlaying)
            {
                return;
            }

            StartIdleTween();
        }

        public void StopAndReset()
        {
            if (this == null)
            {
                return;
            }

            isIdleRequested = false;
            isPressed = false;
            KillTweens();

            if (hasBaseScale && TryResolveTargetTransform(out var resolvedTransform))
            {
                resolvedTransform.localScale = baseLocalScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PresentationTweenBootstrap.EnsureInitialized();
            CacheBaseScale(forceRefresh: !hasBaseScale);

            if (!TryResolveTargetTransform(out var resolvedTransform))
            {
                return;
            }

            isPressed = true;
            KillIdleTween();
            KillPressTween();

            pressTween = resolvedTransform
                .DOScale(baseLocalScale * Mathf.Max(0.01f, pressScaleMultiplier), Mathf.Max(0.01f, pressDurationSeconds))
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => pressTween = null);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleasePressState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ReleasePressState();
        }

        protected virtual void OnDisable()
        {
            StopAndReset();
        }

        protected virtual void OnDestroy()
        {
            StopAndReset();
        }

        private void ReleasePressState()
        {
            if (!isPressed)
            {
                return;
            }

            isPressed = false;

            if (!TryResolveTargetTransform(out var resolvedTransform))
            {
                KillPressTween();
                return;
            }

            KillPressTween();

            var releaseDuration = Mathf.Max(0.05f, releaseDurationSeconds);
            var overshootScale = baseLocalScale * Mathf.Max(1f, releaseOvershootScaleMultiplier);

            pressTween = DOTween.Sequence()
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(resolvedTransform.DOScale(overshootScale, releaseDuration * 0.38f).SetEase(Ease.OutQuad))
                .Append(resolvedTransform.DOScale(baseLocalScale, releaseDuration * 0.62f).SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    pressTween = null;

                    if (isIdleRequested)
                    {
                        StartIdleTween();
                    }
                });
        }

        private void StartIdleTween()
        {
            if (!TryResolveTargetTransform(out var resolvedTransform))
            {
                return;
            }

            KillIdleTween();
            KillPressTween();
            resolvedTransform.localScale = baseLocalScale;

            idleTween = resolvedTransform
                .DOScale(baseLocalScale * (1f + Mathf.Max(0f, scaleAmplitude)), Mathf.Max(0.05f, durationSeconds * 0.5f))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnKill(() => idleTween = null);
        }

        private void CacheBaseScale(bool forceRefresh)
        {
            if (!TryResolveTargetTransform(out var resolvedTransform))
            {
                return;
            }

            if (!hasBaseScale || forceRefresh)
            {
                baseLocalScale = resolvedTransform.localScale;
                hasBaseScale = true;
            }
        }

        private bool TryResolveTargetTransform(out Transform resolvedTransform)
        {
            targetTransform = animatedTransform != null ? animatedTransform : transform;
            resolvedTransform = targetTransform;
            return resolvedTransform != null;
        }

        private void KillTweens()
        {
            KillIdleTween();
            KillPressTween();
        }

        private void KillIdleTween()
        {
            if (idleTween == null)
            {
                return;
            }

            idleTween.Kill();
            idleTween = null;
        }

        private void KillPressTween()
        {
            if (pressTween == null)
            {
                return;
            }

            pressTween.Kill();
            pressTween = null;
        }
    }
}
