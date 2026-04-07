using System;
using System.Collections.Generic;

namespace DreamBlastClone.Systems
{
    public sealed class ItemGravityResolutionResult
    {
        private static readonly IReadOnlyList<ItemFallMove> EmptyMoves = Array.Empty<ItemFallMove>();

        public ItemGravityResolutionResult(IReadOnlyList<ItemFallMove> moves)
        {
            Moves = moves ?? throw new ArgumentNullException(nameof(moves));
        }

        public IReadOnlyList<ItemFallMove> Moves { get; }

        public int MoveCount => Moves.Count;

        public bool HasAnyMovement => MoveCount > 0;

        public static ItemGravityResolutionResult Empty()
        {
            return new ItemGravityResolutionResult(EmptyMoves);
        }
    }
}
