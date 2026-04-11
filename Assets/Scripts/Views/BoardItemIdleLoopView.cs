using DG.Tweening;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class BoardItemIdleLoopView : MonoBehaviour
    {
        private enum IdleLoopStyle
        {
            CubePulse,
            RocketCharge,
            TntWeight
        }

        [SerializeField] private IdleLoopStyle idleLoopStyle = IdleLoopStyle.CubePulse;
        [SerializeField] private float durationSeconds = 1.8f;
        [SerializeField] private float scaleAmplitude = 0.025f;
        [SerializeField] private float verticalAmplitude = 0.012f;
        [SerializeField] private float rotationAmplitudeDegrees;
        [SerializeField] private float phaseOffsetSeconds = 0.5f;

        private Sequence idleTween;
        private Vector3 baseLocalScale;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalEulerAngles;
        private bool hasBaseTransform;

        public bool IsPlaying => idleTween != null && idleTween.IsActive() && idleTween.IsPlaying();

        public void Play()
        {
            var phaseOffset = phaseOffsetSeconds > 0f
                ? Random.Range(0f, phaseOffsetSeconds)
                : 0f;

            PlayAtGlobalPhase(phaseOffset);
        }

        public void PlayAtGlobalPhase(float phaseOffset)
        {
            PresentationTweenBootstrap.EnsureInitialized();

            StopAndReset();
            CaptureBaseTransform();

            idleTween = CreateIdleTween()
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            var phase = Mathf.Repeat(Time.time + Mathf.Max(0f, phaseOffset), Mathf.Max(0.05f, durationSeconds));
            idleTween.Goto(phase, andPlay: true);
        }

        public void StopAndReset()
        {
            if (idleTween != null)
            {
                idleTween.Kill();
                idleTween = null;
            }

            if (!hasBaseTransform)
            {
                return;
            }

            transform.localScale = baseLocalScale;
            transform.localPosition = baseLocalPosition;
            transform.localEulerAngles = baseLocalEulerAngles;
        }

        private void OnDisable()
        {
            StopAndReset();
        }

        private void OnDestroy()
        {
            StopAndReset();
        }

        private void CaptureBaseTransform()
        {
            baseLocalScale = transform.localScale;
            baseLocalPosition = transform.localPosition;
            baseLocalEulerAngles = transform.localEulerAngles;
            hasBaseTransform = true;
        }

        private Sequence CreateIdleTween()
        {
            return idleLoopStyle switch
            {
                IdleLoopStyle.RocketCharge => CreateRocketChargeTween(),
                IdleLoopStyle.TntWeight => CreateTntWeightTween(),
                _ => CreateCubePulseTween()
            };
        }

        private Sequence CreateCubePulseTween()
        {
            var halfDuration = Mathf.Max(0.05f, durationSeconds * 0.5f);
            var targetScale = baseLocalScale * (1f + Mathf.Max(0f, scaleAmplitude));
            var targetPosition = baseLocalPosition + Vector3.up * verticalAmplitude;

            return DOTween.Sequence()
                .Join(transform.DOScale(targetScale, halfDuration).SetEase(Ease.InOutSine))
                .Join(transform.DOLocalMove(targetPosition, halfDuration).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo);
        }

        private Sequence CreateRocketChargeTween()
        {
            var stepDuration = Mathf.Max(0.05f, durationSeconds * 0.25f);
            var chargeScale = new Vector3(
                baseLocalScale.x * (1f + scaleAmplitude * 0.75f),
                baseLocalScale.y * (1f - scaleAmplitude * 0.35f),
                baseLocalScale.z);
            var recoilScale = new Vector3(
                baseLocalScale.x * (1f - scaleAmplitude * 0.35f),
                baseLocalScale.y * (1f + scaleAmplitude * 0.55f),
                baseLocalScale.z);
            var sway = Mathf.Max(0f, verticalAmplitude);
            var rotation = Mathf.Max(0f, rotationAmplitudeDegrees);

            return DOTween.Sequence()
                .Append(transform.DOLocalMove(baseLocalPosition + Vector3.right * sway, stepDuration).SetEase(Ease.InOutSine))
                .Join(transform.DOScale(chargeScale, stepDuration).SetEase(Ease.InOutSine))
                .Join(transform.DOLocalRotate(baseLocalEulerAngles + Vector3.forward * rotation, stepDuration, RotateMode.Fast).SetEase(Ease.InOutSine))
                .Append(transform.DOLocalMove(baseLocalPosition - Vector3.right * sway, stepDuration).SetEase(Ease.InOutSine))
                .Join(transform.DOScale(recoilScale, stepDuration).SetEase(Ease.InOutSine))
                .Join(transform.DOLocalRotate(baseLocalEulerAngles - Vector3.forward * rotation, stepDuration, RotateMode.Fast).SetEase(Ease.InOutSine))
                .Append(transform.DOLocalMove(baseLocalPosition, stepDuration).SetEase(Ease.OutSine))
                .Join(transform.DOScale(baseLocalScale, stepDuration).SetEase(Ease.OutSine))
                .Join(transform.DOLocalRotate(baseLocalEulerAngles, stepDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                .SetLoops(-1, LoopType.Restart);
        }

        private Sequence CreateTntWeightTween()
        {
            var settleDuration = Mathf.Max(0.05f, durationSeconds * 0.45f);
            var reboundDuration = Mathf.Max(0.05f, durationSeconds * 0.2f);
            var recoverDuration = Mathf.Max(0.05f, durationSeconds * 0.35f);
            var squashScale = new Vector3(
                baseLocalScale.x * (1f + scaleAmplitude),
                baseLocalScale.y * (1f - scaleAmplitude * 0.55f),
                baseLocalScale.z);
            var reboundScale = new Vector3(
                baseLocalScale.x * (1f - scaleAmplitude * 0.25f),
                baseLocalScale.y * (1f + scaleAmplitude * 0.45f),
                baseLocalScale.z);
            var sink = Mathf.Max(0f, verticalAmplitude);
            var rotation = Mathf.Max(0f, rotationAmplitudeDegrees);

            return DOTween.Sequence()
                .Append(transform.DOLocalMove(baseLocalPosition + Vector3.down * sink, settleDuration).SetEase(Ease.InSine))
                .Join(transform.DOScale(squashScale, settleDuration).SetEase(Ease.InSine))
                .Join(transform.DOLocalRotate(baseLocalEulerAngles - Vector3.forward * rotation, settleDuration, RotateMode.Fast).SetEase(Ease.InOutSine))
                .Append(transform.DOLocalMove(baseLocalPosition + Vector3.up * sink * 0.35f, reboundDuration).SetEase(Ease.OutSine))
                .Join(transform.DOScale(reboundScale, reboundDuration).SetEase(Ease.OutSine))
                .Join(transform.DOLocalRotate(baseLocalEulerAngles + Vector3.forward * rotation, reboundDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                .Append(transform.DOLocalMove(baseLocalPosition, recoverDuration).SetEase(Ease.OutSine))
                .Join(transform.DOScale(baseLocalScale, recoverDuration).SetEase(Ease.OutSine))
                .Join(transform.DOLocalRotate(baseLocalEulerAngles, recoverDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                .SetLoops(-1, LoopType.Restart);
        }
    }
}
