using System;
using System.Collections.Generic;
using DreamBlastClone.Controllers;
using DreamBlastClone.Data;
using DreamBlastClone.Systems;
using UnityEngine;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class LevelSceneHudController : MonoBehaviour
    {
        [SerializeField] private LevelSessionHost sessionHost;
        [SerializeField] private BoardInputSessionBridge inputBridge;
        [SerializeField] private Component moveCountLabel;
        [SerializeField] private RectTransform goalItemTemplate;
        [SerializeField] private Transform goalItemsContainer;
        [SerializeField] private float goalItemSpacing = 140f;
        [SerializeField] private Sprite stoneGoalIcon;
        [SerializeField] private Sprite vaseGoalIcon;
        [SerializeField] private Sprite chaliceGoalIcon;

        private readonly LevelGoalProgressEvaluator goalProgressEvaluator = new LevelGoalProgressEvaluator();
        private readonly List<LevelGoalItemView> spawnedGoalItems = new List<LevelGoalItemView>();

        private void OnEnable()
        {
            var targetInputBridge = inputBridge is not null ? inputBridge : GetComponent<BoardInputSessionBridge>();

            if (targetInputBridge is null)
            {
                return;
            }

            inputBridge = targetInputBridge;
            inputBridge.TapProcessed += HandleTapProcessed;
        }

        private void Start()
        {
            RefreshHud();
        }

        private void OnDisable()
        {
            if (inputBridge is not null)
            {
                inputBridge.TapProcessed -= HandleTapProcessed;
            }
        }

        public bool RefreshHud()
        {
            if (!TryResolveSession(out var session))
            {
                return false;
            }

            UiComponentBinding.SetText(moveCountLabel, session.RemainingMoves.ToString("00"));
            RefreshGoals(session);
            return true;
        }

        private void HandleTapProcessed(LevelSessionTapResult result)
        {
            RefreshHud();
        }

        private bool TryResolveSession(out LevelSession session)
        {
            var targetSessionHost = sessionHost is not null ? sessionHost : GetComponent<LevelSessionHost>();

            if (targetSessionHost is null)
            {
                session = null;
                return false;
            }

            sessionHost = targetSessionHost;
            session = sessionHost.Session;
            return session is not null;
        }

        private void RefreshGoals(LevelSession session)
        {
            if (goalItemTemplate is null)
            {
                return;
            }

            var goalProgress = goalProgressEvaluator.Evaluate(session.Board, session.Goals);
            EnsureGoalItems(goalProgress.Count);
            LayoutGoalItems(goalProgress.Count);

            for (var index = 0; index < goalProgress.Count; index++)
            {
                var progress = goalProgress[index];
                spawnedGoalItems[index].Bind(progress, ResolveGoalIcon(progress.GoalType), ResolveGoalTitle(progress.GoalType));
            }
        }

        private void EnsureGoalItems(int goalCount)
        {
            goalItemTemplate.gameObject.SetActive(false);
            var container = goalItemsContainer is not null ? goalItemsContainer : goalItemTemplate.parent;

            while (spawnedGoalItems.Count < goalCount)
            {
                var instance = Instantiate(goalItemTemplate.gameObject, container, false);
                instance.name = $"{goalItemTemplate.name}_{spawnedGoalItems.Count + 1}";
                instance.SetActive(true);

                if (!instance.TryGetComponent<LevelGoalItemView>(out var goalItemView))
                {
                    throw new InvalidOperationException($"{nameof(LevelSceneHudController)} requires the goal template to have a {nameof(LevelGoalItemView)} component.");
                }

                spawnedGoalItems.Add(goalItemView);
            }

            for (var index = 0; index < spawnedGoalItems.Count; index++)
            {
                spawnedGoalItems[index].gameObject.SetActive(index < goalCount);
            }
        }

        private void LayoutGoalItems(int goalCount)
        {
            if (goalCount <= 0)
            {
                return;
            }

            var templatePosition = goalItemTemplate.anchoredPosition;
            var centeredOffset = (goalCount - 1) * 0.5f * goalItemSpacing;

            for (var index = 0; index < goalCount; index++)
            {
                var itemTransform = (RectTransform)spawnedGoalItems[index].transform;
                itemTransform.anchoredPosition = new Vector2(
                    templatePosition.x + index * goalItemSpacing - centeredOffset,
                    templatePosition.y);
                itemTransform.localScale = goalItemTemplate.localScale;
            }
        }

        private Sprite ResolveGoalIcon(LevelGoalType goalType)
        {
            return goalType switch
            {
                LevelGoalType.Stone => stoneGoalIcon,
                LevelGoalType.Vase => vaseGoalIcon,
                LevelGoalType.ChaliceBox => chaliceGoalIcon,
                _ => null
            };
        }

        private static string ResolveGoalTitle(LevelGoalType goalType)
        {
            return goalType switch
            {
                LevelGoalType.Stone => "Stone",
                LevelGoalType.Vase => "Vase",
                LevelGoalType.ChaliceBox => "Chalices",
                _ => goalType.ToString()
            };
        }
    }
}
