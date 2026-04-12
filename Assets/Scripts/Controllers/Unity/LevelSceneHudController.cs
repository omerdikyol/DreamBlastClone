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
        [SerializeField] private GameAudioController audioController;
        [SerializeField] private Component moveCountLabel;
        [SerializeField] private RectTransform goalItemTemplate;
        [SerializeField] private Transform goalItemsContainer;
        [SerializeField] private float multiGoalLayoutSize = 56f;
        [SerializeField] private Sprite stoneGoalIcon;
        [SerializeField] private Sprite vaseGoalIcon;
        [SerializeField] private Sprite chaliceGoalIcon;

        private readonly LevelGoalProgressEvaluator goalProgressEvaluator = new LevelGoalProgressEvaluator();
        private readonly List<LevelGoalItemView> spawnedGoalItems = new List<LevelGoalItemView>();
        private readonly List<bool> displayedGoalCompletionStates = new List<bool>();
        private bool hasInitializedGoalState;

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
            ResolveAudioController();
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
                var wasCompleted = index < displayedGoalCompletionStates.Count && displayedGoalCompletionStates[index];
                spawnedGoalItems[index].Bind(progress, ResolveGoalIcon(progress.GoalType), ResolveGoalTitle(progress.GoalType));

                if (hasInitializedGoalState && !wasCompleted && progress.IsCompleted)
                {
                    audioController?.PlaySfx(GameSfxCue.GoalCompletion);
                }

                if (index < displayedGoalCompletionStates.Count)
                {
                    displayedGoalCompletionStates[index] = progress.IsCompleted;
                }
            }

            hasInitializedGoalState = true;
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

            while (displayedGoalCompletionStates.Count < goalCount)
            {
                displayedGoalCompletionStates.Add(false);
            }

            if (displayedGoalCompletionStates.Count > goalCount)
            {
                displayedGoalCompletionStates.RemoveRange(goalCount, displayedGoalCompletionStates.Count - goalCount);
            }
        }

        private void ResolveAudioController()
        {
            audioController ??= GetComponent<GameAudioController>() ?? gameObject.AddComponent<GameAudioController>();
        }

        private void LayoutGoalItems(int goalCount)
        {
            if (goalCount <= 0)
            {
                return;
            }

            var templatePosition = goalItemTemplate.anchoredPosition;
            var layoutSize = multiGoalLayoutSize * ResolveTemplateScaleMultiplier();
            var layout = BuildGoalLayout(goalCount);

            for (var index = 0; index < goalCount; index++)
            {
                var itemTransform = (RectTransform)spawnedGoalItems[index].transform;
                itemTransform.anchoredPosition = templatePosition + layout[index].Offset * layoutSize;
                itemTransform.localScale = goalItemTemplate.localScale * layout[index].ScaleMultiplier;
            }
        }

        private float ResolveTemplateScaleMultiplier()
        {
            if (goalItemTemplate is null)
            {
                return 1f;
            }

            return Mathf.Max(
                1f,
                Mathf.Abs(goalItemTemplate.localScale.x),
                Mathf.Abs(goalItemTemplate.localScale.y));
        }

        private static IReadOnlyList<GoalLayoutSlot> BuildGoalLayout(int goalCount)
        {
            if (goalCount <= 1)
            {
                return new[]
                {
                    new GoalLayoutSlot(Vector2.zero, 1f)
                };
            }

            return BuildGridGoalLayout(goalCount);
        }

        private static IReadOnlyList<GoalLayoutSlot> BuildGridGoalLayout(int goalCount)
        {
            var columns = Mathf.CeilToInt(Mathf.Sqrt(goalCount));
            var rows = Mathf.CeilToInt((float)goalCount / columns);
            const float layoutSpan = 0.72f;
            var horizontalStep = columns <= 1 ? 0f : layoutSpan / (columns - 1);
            var verticalStep = rows <= 1 ? 0f : layoutSpan / (rows - 1);
            var horizontalRoom = columns <= 1 ? layoutSpan : horizontalStep;
            var verticalRoom = rows <= 1 ? layoutSpan : verticalStep;
            var scaleMultiplier = Mathf.Clamp(Mathf.Min(horizontalRoom, verticalRoom) * 0.78f, 0.3f, 1f);
            var slots = new List<GoalLayoutSlot>(goalCount);

            for (var index = 0; index < goalCount; index++)
            {
                var row = index / columns;
                var column = index % columns;
                var rowCount = row == rows - 1 ? goalCount - row * columns : columns;
                var x = rowCount <= 1 ? 0f : (column - (rowCount - 1) * 0.5f) * horizontalStep;
                var y = rows <= 1 ? 0f : ((rows - 1) * 0.5f - row) * verticalStep;
                slots.Add(new GoalLayoutSlot(new Vector2(x, y), scaleMultiplier));
            }

            return slots;
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

        private readonly struct GoalLayoutSlot
        {
            public GoalLayoutSlot(Vector2 offset, float scaleMultiplier)
            {
                Offset = offset;
                ScaleMultiplier = scaleMultiplier;
            }

            public Vector2 Offset { get; }

            public float ScaleMultiplier { get; }
        }
    }
}
