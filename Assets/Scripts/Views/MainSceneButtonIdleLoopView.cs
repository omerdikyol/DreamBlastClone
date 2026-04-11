using DG.Tweening;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class MainSceneButtonIdleLoopView : MonoBehaviour
    {
        [SerializeField] private float durationSeconds = 1.65f;
        [SerializeField] private float scaleAmplitude = 0.055f;

        private Tween idleTween;
        private Vector3 baseLocalScale;
        private bool hasBaseScale;

        public bool IsPlaying => idleTween != null && idleTween.IsActive() && idleTween.IsPlaying();

        public void Play()
        {
            PresentationTweenBootstrap.EnsureInitialized();

            StopAndReset();
            baseLocalScale = transform.localScale;
            hasBaseScale = true;

            idleTween = transform
                .DOScale(baseLocalScale * (1f + Mathf.Max(0f, scaleAmplitude)), Mathf.Max(0.05f, durationSeconds * 0.5f))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void StopAndReset()
        {
            if (this == null)
            {
                return;
            }

            if (idleTween != null)
            {
                idleTween.Kill();
                idleTween = null;
            }

            if (hasBaseScale)
            {
                transform.localScale = baseLocalScale;
            }
        }

        private void OnDisable()
        {
            StopAndReset();
        }

        private void OnDestroy()
        {
            StopAndReset();
        }
    }
}
