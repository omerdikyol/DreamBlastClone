using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Data
{
    public enum LevelGoalType
    {
        Stone = 0,
        Vase = 1,
        ChaliceBox = 2
    }

    public sealed class LevelGoalDefinition
    {
        public LevelGoalDefinition(LevelGoalType goalType, int initialCount)
        {
            if (initialCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount), initialCount, "Goal count must be greater than zero.");
            }

            GoalType = goalType;
            InitialCount = initialCount;
        }

        public LevelGoalType GoalType { get; }

        public int InitialCount { get; }
    }

    public sealed class LevelGoalDefinitionBuilder
    {
        public IReadOnlyList<LevelGoalDefinition> Build(LevelDefinition levelDefinition)
        {
            if (levelDefinition is null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            var countsByType = new Dictionary<LevelGoalType, int>();
            var orderedGoalTypes = new List<LevelGoalType>();
            var chaliceParts = new Dictionary<BoardCoordinate, ChaliceBoxPart>();

            foreach (var cell in levelDefinition.CellDefinitions)
            {
                if (cell.Obstacle is null)
                {
                    continue;
                }

                var goalType = ResolveGoalType(cell.Obstacle);

                if (!countsByType.ContainsKey(goalType))
                {
                    countsByType.Add(goalType, 0);
                    orderedGoalTypes.Add(goalType);
                }

                if (cell.Obstacle is ChaliceBoxPartLevelObstacleDefinition chalicePart)
                {
                    chaliceParts.Add(cell.Coordinate, chalicePart.Part);
                    continue;
                }

                countsByType[goalType]++;
            }

            if (chaliceParts.Count > 0)
            {
                var chaliceRegions = ChaliceBoxRegionNormalizer.Normalize(
                    chaliceParts,
                    levelDefinition.GridWidth,
                    levelDefinition.GridHeight,
                    levelDefinition.LevelNumber);
                countsByType[LevelGoalType.ChaliceBox] = chaliceRegions.Count * 10;
            }

            var goals = new List<LevelGoalDefinition>(orderedGoalTypes.Count);

            foreach (var goalType in orderedGoalTypes)
            {
                goals.Add(new LevelGoalDefinition(goalType, countsByType[goalType]));
            }

            return goals.AsReadOnly();
        }

        private static LevelGoalType ResolveGoalType(LevelObstacleDefinition obstacle)
        {
            return obstacle switch
            {
                StoneLevelObstacleDefinition => LevelGoalType.Stone,
                VaseLevelObstacleDefinition => LevelGoalType.Vase,
                ChaliceBoxPartLevelObstacleDefinition => LevelGoalType.ChaliceBox,
                _ => throw new InvalidOperationException($"Unsupported goal obstacle definition '{obstacle.GetType().Name}'.")
            };
        }
    }
}
