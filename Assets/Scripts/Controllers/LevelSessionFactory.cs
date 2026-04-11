using System;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Controllers
{
    public sealed class LevelSessionFactory
    {
        private readonly LevelBoardBuilder levelBoardBuilder = new LevelBoardBuilder();
        private readonly LevelGoalDefinitionBuilder levelGoalDefinitionBuilder = new LevelGoalDefinitionBuilder();

        public LevelSession Create(LevelDefinition levelDefinition)
        {
            if (levelDefinition is null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            var deterministicSeed = BuildDeterministicSeed(levelDefinition);
            var initialColorResolver = new DeterministicInitialCubeColorResolver(deterministicSeed);
            var refillColorResolver = new DeterministicRefillSequenceResolver(deterministicSeed);
            var board = levelBoardBuilder.Build(levelDefinition, initialColorResolver);
            var goals = levelGoalDefinitionBuilder.Build(levelDefinition);
            return new LevelSession(board, levelDefinition.MoveCount, refillColorResolver, goals);
        }

        private static uint BuildDeterministicSeed(LevelDefinition levelDefinition)
        {
            unchecked
            {
                var seed = 2166136261u;
                seed = (seed ^ (uint)levelDefinition.LevelNumber) * 16777619u;
                seed = (seed ^ (uint)levelDefinition.GridWidth) * 16777619u;
                seed = (seed ^ (uint)levelDefinition.GridHeight) * 16777619u;
                seed = (seed ^ (uint)levelDefinition.MoveCount) * 16777619u;
                seed = (seed ^ (uint)levelDefinition.CellDefinitions.Count) * 16777619u;

                return seed;
            }
        }
    }
}
