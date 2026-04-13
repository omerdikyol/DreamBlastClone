using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class MoveHintResolver
    {
        private readonly CubeGroupDetector cubeGroupDetector = new CubeGroupDetector();
        private readonly CubeBlastResolver cubeBlastResolver = new CubeBlastResolver();
        private readonly SpecialItemComboResolver specialItemComboResolver = new SpecialItemComboResolver();
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();
        private readonly NormalBlastObstacleDamageResolver normalBlastObstacleDamageResolver = new NormalBlastObstacleDamageResolver();
        private readonly SpecialComboObstacleDamageResolver specialComboObstacleDamageResolver = new SpecialComboObstacleDamageResolver();
        private readonly SpecialActivationObstacleDamageResolver specialActivationObstacleDamageResolver = new SpecialActivationObstacleDamageResolver();
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly LevelGoalProgressEvaluator goalProgressEvaluator = new LevelGoalProgressEvaluator();

        public MoveHintSuggestion Resolve(BoardModel board, IReadOnlyList<LevelGoalDefinition> goals = null)
        {
            var suggestions = ResolveTopMoves(board, goals, 1);
            return suggestions.Count > 0 ? suggestions[0] : MoveHintSuggestion.Invalid();
        }

        public IReadOnlyList<MoveHintSuggestion> ResolveTopMoves(
            BoardModel board,
            IReadOnlyList<LevelGoalDefinition> goals = null,
            int maxCount = 3)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Max hint count must be greater than zero.");
            }

            var remainingGoalTypes = BuildRemainingGoalTypes(board, goals);
            var seenNormalGroupCoordinates = new HashSet<BoardCoordinate>();
            var candidates = new List<MoveCandidate>();

            foreach (var cell in board.GetAllCells())
            {
                if (!cell.HasItem)
                {
                    continue;
                }

                MoveCandidate candidate = cell.Item switch
                {
                    CubeItemModel => ResolveNormalGroupCandidate(board, cell.Coordinate, remainingGoalTypes, seenNormalGroupCoordinates),
                    RocketItemModel or TntItemModel => ResolveSpecialCandidate(board, cell.Coordinate, remainingGoalTypes),
                    _ => null
                };

                if (candidate is not null)
                {
                    candidates.Add(candidate);
                }
            }

            candidates.Sort(CompareCandidates);

            var suggestions = new List<MoveHintSuggestion>(Math.Min(maxCount, candidates.Count));
            var seenTapCoordinates = new HashSet<BoardCoordinate>();

            for (var index = 0; index < candidates.Count && suggestions.Count < maxCount; index++)
            {
                var candidate = candidates[index];
                if (!seenTapCoordinates.Add(candidate.TapCoordinate))
                {
                    continue;
                }

                suggestions.Add(candidate.ToSuggestion());
            }

            return suggestions;
        }

        private MoveCandidate ResolveNormalGroupCandidate(
            BoardModel board,
            BoardCoordinate tapCoordinate,
            HashSet<LevelGoalType> remainingGoalTypes,
            HashSet<BoardCoordinate> seenNormalGroupCoordinates)
        {
            if (seenNormalGroupCoordinates.Contains(tapCoordinate))
            {
                return null;
            }

            var group = cubeGroupDetector.FindGroup(board, tapCoordinate);
            foreach (var coordinate in group.Coordinates)
            {
                seenNormalGroupCoordinates.Add(coordinate);
            }

            if (!group.IsValidStart || group.Count < 2)
            {
                return null;
            }

            var clonedBoard = boardModelCloner.Clone(board);
            var blast = cubeBlastResolver.Resolve(clonedBoard, tapCoordinate);
            var obstacleDamage = normalBlastObstacleDamageResolver.Resolve(clonedBoard, blast);

            return new MoveCandidate(
                MoveHintTargetType.NormalGroup,
                tapCoordinate,
                group.Coordinates,
                createsTnt: blast.CreatedSpecialItem is TntItemModel,
                createsRocket: blast.CreatedSpecialItem is RocketItemModel,
                goalImpactScore: CalculateGoalImpactScore(board, obstacleDamage, remainingGoalTypes),
                movePower: blast.BlastedGroupSize);
        }

        private MoveCandidate ResolveSpecialCandidate(
            BoardModel board,
            BoardCoordinate tapCoordinate,
            HashSet<LevelGoalType> remainingGoalTypes)
        {
            var clonedBoard = boardModelCloner.Clone(board);
            var combo = specialItemComboResolver.Resolve(clonedBoard, tapCoordinate);

            if (combo.IsComboActivated)
            {
                var comboObstacleDamage = specialComboObstacleDamageResolver.Resolve(clonedBoard, combo);
                return new MoveCandidate(
                    MoveHintTargetType.SpecialItem,
                    tapCoordinate,
                    new[] { tapCoordinate },
                    createsTnt: false,
                    createsRocket: false,
                    goalImpactScore: CalculateGoalImpactScore(board, comboObstacleDamage, remainingGoalTypes),
                    movePower: combo.RemovedItemCoordinates.Count);
            }

            clonedBoard = boardModelCloner.Clone(board);
            var activation = specialItemTapResolver.Resolve(clonedBoard, tapCoordinate);
            if (!activation.IsValidActivation)
            {
                return null;
            }

            var activationObstacleDamage = specialActivationObstacleDamageResolver.Resolve(clonedBoard, activation);
            return new MoveCandidate(
                MoveHintTargetType.SpecialItem,
                tapCoordinate,
                new[] { tapCoordinate },
                createsTnt: false,
                createsRocket: false,
                goalImpactScore: CalculateGoalImpactScore(board, activationObstacleDamage, remainingGoalTypes),
                movePower: activation.RemovedItemCoordinates.Count);
        }

        private HashSet<LevelGoalType> BuildRemainingGoalTypes(BoardModel board, IReadOnlyList<LevelGoalDefinition> goals)
        {
            if (goals is null || goals.Count == 0)
            {
                return null;
            }

            var progress = goalProgressEvaluator.Evaluate(board, goals);
            var remainingGoalTypes = new HashSet<LevelGoalType>();

            foreach (var goal in progress)
            {
                if (!goal.IsCompleted)
                {
                    remainingGoalTypes.Add(goal.GoalType);
                }
            }

            return remainingGoalTypes;
        }

        private static int CalculateGoalImpactScore(
            BoardModel originalBoard,
            ObstacleDamageResolutionResult obstacleDamage,
            HashSet<LevelGoalType> remainingGoalTypes)
        {
            if (obstacleDamage is null || !obstacleDamage.HasAnyDamage)
            {
                return 0;
            }

            var removedCoordinates = new HashSet<BoardCoordinate>(obstacleDamage.RemovedCoordinates);
            var totalScore = 0;

            foreach (var damage in obstacleDamage.Damages)
            {
                if (!TryResolveGoalType(originalBoard, damage.Coordinate, remainingGoalTypes, out _))
                {
                    continue;
                }

                totalScore += Math.Max(1, damage.Amount) * 10;

                if (removedCoordinates.Contains(damage.Coordinate))
                {
                    totalScore += 50;
                }
            }

            return totalScore;
        }

        private static bool TryResolveGoalType(
            BoardModel board,
            BoardCoordinate coordinate,
            HashSet<LevelGoalType> remainingGoalTypes,
            out LevelGoalType goalType)
        {
            goalType = default;

            if (!board.TryGetCell(coordinate, out var cell) || cell.Obstacle is null)
            {
                return false;
            }

            goalType = cell.Obstacle switch
            {
                StoneObstacleModel => LevelGoalType.Stone,
                VaseObstacleModel => LevelGoalType.Vase,
                ChaliceBoxObstacleModel => LevelGoalType.ChaliceBox,
                _ => default
            };

            return remainingGoalTypes is null || remainingGoalTypes.Contains(goalType);
        }

        private static int CompareCandidates(MoveCandidate left, MoveCandidate right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return 1;
            }

            if (right is null)
            {
                return -1;
            }

            if (left.CreatesTnt != right.CreatesTnt)
            {
                return right.CreatesTnt.CompareTo(left.CreatesTnt);
            }

            if (left.CreatesRocket != right.CreatesRocket)
            {
                return right.CreatesRocket.CompareTo(left.CreatesRocket);
            }

            var leftHasGoalImpact = left.GoalImpactScore > 0;
            var rightHasGoalImpact = right.GoalImpactScore > 0;
            if (leftHasGoalImpact != rightHasGoalImpact)
            {
                return rightHasGoalImpact.CompareTo(leftHasGoalImpact);
            }

            if (left.GoalImpactScore != right.GoalImpactScore)
            {
                return right.GoalImpactScore.CompareTo(left.GoalImpactScore);
            }

            if (left.MovePower != right.MovePower)
            {
                return right.MovePower.CompareTo(left.MovePower);
            }

            if (left.TargetType != right.TargetType)
            {
                return left.TargetType.CompareTo(right.TargetType);
            }

            if (left.TapCoordinate.Y != right.TapCoordinate.Y)
            {
                return left.TapCoordinate.Y.CompareTo(right.TapCoordinate.Y);
            }

            return left.TapCoordinate.X.CompareTo(right.TapCoordinate.X);
        }

        private sealed class MoveCandidate
        {
            public MoveCandidate(
                MoveHintTargetType targetType,
                BoardCoordinate tapCoordinate,
                IReadOnlyList<BoardCoordinate> highlightCoordinates,
                bool createsTnt,
                bool createsRocket,
                int goalImpactScore,
                int movePower)
            {
                TargetType = targetType;
                TapCoordinate = tapCoordinate;
                HighlightCoordinates = highlightCoordinates ?? throw new ArgumentNullException(nameof(highlightCoordinates));
                CreatesTnt = createsTnt;
                CreatesRocket = createsRocket;
                GoalImpactScore = goalImpactScore;
                MovePower = movePower;
            }

            public MoveHintTargetType TargetType { get; }

            public BoardCoordinate TapCoordinate { get; }

            public IReadOnlyList<BoardCoordinate> HighlightCoordinates { get; }

            public bool CreatesTnt { get; }

            public bool CreatesRocket { get; }

            public int GoalImpactScore { get; }

            public int MovePower { get; }

            public MoveHintSuggestion ToSuggestion()
            {
                return new MoveHintSuggestion(
                    isValid: true,
                    targetType: TargetType,
                    tapCoordinate: TapCoordinate,
                    highlightCoordinates: HighlightCoordinates);
            }
        }
    }
}
