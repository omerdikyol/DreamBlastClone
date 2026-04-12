using System;
using System.Collections.Generic;

namespace DreamBlastClone.Systems
{
    public sealed class ItemGravityResolutionResult
    {
        private static readonly IReadOnlyList<ItemFallMove> EmptyMoves = Array.Empty<ItemFallMove>();
        private static readonly IReadOnlyList<ObstacleFallMove> EmptyObstacleMoves = Array.Empty<ObstacleFallMove>();

        public ItemGravityResolutionResult(
            IReadOnlyList<ItemFallMove> moves,
            IReadOnlyList<ObstacleFallMove> obstacleMoves = null)
        {
            Moves = moves ?? throw new ArgumentNullException(nameof(moves));
            ObstacleMoves = obstacleMoves ?? EmptyObstacleMoves;
        }

        public IReadOnlyList<ItemFallMove> Moves { get; }

        public IReadOnlyList<ObstacleFallMove> ObstacleMoves { get; }

        public int MoveCount => Moves.Count;

        public int ObstacleMoveCount => ObstacleMoves.Count;

        public bool HasAnyMovement => MoveCount > 0 || ObstacleMoveCount > 0;

        public static ItemGravityResolutionResult Empty()
        {
            return new ItemGravityResolutionResult(EmptyMoves, EmptyObstacleMoves);
        }
    }
}
