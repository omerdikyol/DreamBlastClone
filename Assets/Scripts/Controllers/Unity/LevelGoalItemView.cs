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
        }
    }
}
