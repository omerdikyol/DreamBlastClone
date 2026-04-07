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
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color completedColor = new Color(1f, 1f, 1f, 0.5f);

        public void Bind(LevelGoalProgress goalProgress, Sprite icon, string title)
        {
            if (goalProgress is null)
            {
                throw new ArgumentNullException(nameof(goalProgress));
            }

            UiComponentBinding.SetSprite(iconTarget, icon);
            UiComponentBinding.SetText(titleLabel, title);
            UiComponentBinding.SetText(countLabel, goalProgress.RemainingCount.ToString("00"));

            var color = goalProgress.IsCompleted ? completedColor : activeColor;
            UiComponentBinding.SetColor(iconTarget, color);
            UiComponentBinding.SetColor(titleLabel, color);
            UiComponentBinding.SetColor(countLabel, color);
        }
    }
}
