using DG.Tweening;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class MoveHintTargetEffectView : MonoBehaviour
    {
        [SerializeField] private float pulseScaleMultiplier = 1.07f;
        [SerializeField] private float pulseDurationSeconds = 0.72f;
        [SerializeField] private float pulseFrequency = 1f;

        private Sequence pulseTween;
        private Vector3 baseScale;
        private bool hasBaseScale;

        public void Play()
        {
            Stop();
            EnsureBaseScale();

            if (!Application.isPlaying)
            {
                return;
            }

            PresentationTweenBootstrap.EnsureInitialized();
            var targetScale = baseScale * Mathf.Max(1f, pulseScaleMultiplier);
            var resolvedPulseDuration = pulseDurationSeconds > 0f
                ? pulseDurationSeconds
                : Mathf.Max(0.05f, 1f / Mathf.Max(0.01f, pulseFrequency));

            pulseTween = DOTween.Sequence()
                .Append(transform.DOScale(targetScale, resolvedPulseDuration * 0.5f).SetEase(Ease.InOutSine))
                .Append(transform.DOScale(baseScale, resolvedPulseDuration * 0.5f).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void Stop()
        {
            if (pulseTween != null)
            {
                pulseTween.Kill();
                pulseTween = null;
            }

            if (hasBaseScale)
            {
                transform.localScale = baseScale;
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }

        private void EnsureBaseScale()
        {
            baseScale = transform.localScale;
            hasBaseScale = true;
        }
    }
}
