using System;
using DreamBlastClone.Systems;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelGoalItemView : MonoBehaviour
    {
        [SerializeField] private Component iconTarget;
        [SerializeField] private Component titleLabel;
        [SerializeField] private Component countLabel;
        [SerializeField] private Component completedCountTarget;
        [SerializeField] private Sprite completedCountSprite;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private float completedCheckPopDuration = 0.24f;
        [SerializeField] private float completedCheckStartScaleMultiplier = 2.6f;

        private bool wasShowingCompletedCheck;
        private bool isAnimatingCompletedCheck;
        private float completedCheckAnimationElapsed;
        private Vector3 completedCheckBaseScale = Vector3.one;
        private bool hasCompletedCheckBaseScale;

        public void Bind(LevelGoalProgress goalProgress, Sprite icon, string title)
        {
            if (goalProgress is null)
            {
                throw new ArgumentNullException(nameof(goalProgress));
            }

            UiComponentBinding.SetSprite(iconTarget, icon);
            UiComponentBinding.SetText(titleLabel, title);
            var showCompletedCheck = goalProgress.IsCompleted && completedCountTarget is not null && completedCountSprite is not null;
            UiComponentBinding.SetText(countLabel, showCompletedCheck ? string.Empty : goalProgress.RemainingCount.ToString("00"));
            UiComponentBinding.SetSprite(completedCountTarget, completedCountSprite);

            UiComponentBinding.SetEnabled(countLabel, !showCompletedCheck);
            UiComponentBinding.SetEnabled(completedCountTarget, showCompletedCheck);

            UiComponentBinding.SetColor(iconTarget, activeColor);
            UiComponentBinding.SetColor(titleLabel, activeColor);
            UiComponentBinding.SetColor(countLabel, activeColor);
            UiComponentBinding.SetColor(completedCountTarget, activeColor);

            if (showCompletedCheck && !wasShowingCompletedCheck)
            {
                BeginCompletedCheckAnimation();
            }
            else if (!showCompletedCheck)
            {
                StopCompletedCheckAnimation(resetToBaseScale: true);
            }

            wasShowingCompletedCheck = showCompletedCheck;
        }

        private void Update()
        {
            AdvanceCompletedCheckAnimation(Time.unscaledDeltaTime);
        }

        private void BeginCompletedCheckAnimation()
        {
            if (!TryGetCompletedCheckTransform(out var completedCheckTransform))
            {
                return;
            }

            CacheCompletedCheckBaseScale(completedCheckTransform);

            if (completedCheckPopDuration <= 0f)
            {
                completedCheckTransform.localScale = completedCheckBaseScale;
                isAnimatingCompletedCheck = false;
                completedCheckAnimationElapsed = 0f;
                return;
            }

            completedCheckAnimationElapsed = 0f;
            isAnimatingCompletedCheck = true;
            completedCheckTransform.localScale = completedCheckBaseScale * Mathf.Max(0.01f, completedCheckStartScaleMultiplier);
        }

        private void AdvanceCompletedCheckAnimation(float deltaTime)
        {
            if (!isAnimatingCompletedCheck || !TryGetCompletedCheckTransform(out var completedCheckTransform))
            {
                return;
            }

            CacheCompletedCheckBaseScale(completedCheckTransform);
            completedCheckAnimationElapsed += Mathf.Max(0f, deltaTime);
            var progress = Mathf.Clamp01(completedCheckAnimationElapsed / completedCheckPopDuration);
            var scaleMultiplier = Mathf.LerpUnclamped(
                Mathf.Max(0.01f, completedCheckStartScaleMultiplier),
                1f,
                EaseOutBack(progress));
            completedCheckTransform.localScale = completedCheckBaseScale * scaleMultiplier;

            if (progress >= 1f)
            {
                StopCompletedCheckAnimation(resetToBaseScale: true);
            }
        }

        private void StopCompletedCheckAnimation(bool resetToBaseScale)
        {
            isAnimatingCompletedCheck = false;
            completedCheckAnimationElapsed = 0f;

            if (resetToBaseScale && TryGetCompletedCheckTransform(out var completedCheckTransform))
            {
                CacheCompletedCheckBaseScale(completedCheckTransform);
                completedCheckTransform.localScale = completedCheckBaseScale;
            }
        }

        private bool TryGetCompletedCheckTransform(out Transform completedCheckTransform)
        {
            completedCheckTransform = completedCountTarget != null ? completedCountTarget.transform : null;
            return completedCheckTransform != null;
        }

        private void CacheCompletedCheckBaseScale(Transform completedCheckTransform)
        {
            if (hasCompletedCheckBaseScale)
            {
                return;
            }

            completedCheckBaseScale = completedCheckTransform.localScale;
            hasCompletedCheckBaseScale = true;
        }

        private static float EaseOutBack(float progress)
        {
            const float overshoot = 1.70158f;
            var adjusted = progress - 1f;
            return 1f + (overshoot + 1f) * adjusted * adjusted * adjusted + overshoot * adjusted * adjusted;
        }
    }
}
