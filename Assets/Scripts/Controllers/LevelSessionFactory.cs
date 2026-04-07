using System;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
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

            var colorResolver = new DeterministicCubeColorResolver();
            var board = levelBoardBuilder.Build(levelDefinition, colorResolver);
            var goals = levelGoalDefinitionBuilder.Build(levelDefinition);
            return new LevelSession(board, levelDefinition.MoveCount, colorResolver, goals);
        }

        private sealed class DeterministicCubeColorResolver : IRandomCubeColorResolver, IRefillCubeColorResolver
        {
            public CubeColor ResolveColor(BoardCoordinate coordinate)
            {
                var hash = unchecked((uint)(coordinate.X * 73856093) ^ (uint)(coordinate.Y * 19349663));

                return (hash % 4u) switch
                {
                    0u => CubeColor.Red,
                    1u => CubeColor.Green,
                    2u => CubeColor.Blue,
                    _ => CubeColor.Yellow
                };
            }
        }
    }
}
