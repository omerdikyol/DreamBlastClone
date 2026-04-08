using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Items;

namespace DreamBlastClone.Views
{
    public sealed class BoardSettleMotionDescriptor
    {
        private static readonly IReadOnlyList<ItemSettleMove> EmptyMoves = Array.Empty<ItemSettleMove>();
        private static readonly IReadOnlyList<RefillSpawnMotion> EmptySpawns = Array.Empty<RefillSpawnMotion>();

        public BoardSettleMotionDescriptor(
            IReadOnlyList<ItemSettleMove> gravityMoves,
            IReadOnlyList<RefillSpawnMotion> refillSpawns)
        {
            GravityMoves = gravityMoves ?? throw new ArgumentNullException(nameof(gravityMoves));
            RefillSpawns = refillSpawns ?? throw new ArgumentNullException(nameof(refillSpawns));
        }

        public IReadOnlyList<ItemSettleMove> GravityMoves { get; }

        public IReadOnlyList<RefillSpawnMotion> RefillSpawns { get; }

        public bool HasAnyMotion => GravityMoves.Count > 0 || RefillSpawns.Count > 0;

        public static BoardSettleMotionDescriptor Empty()
        {
            return new BoardSettleMotionDescriptor(EmptyMoves, EmptySpawns);
        }
    }

    public readonly struct ItemSettleMove
    {
        public ItemSettleMove(BoardCoordinate from, BoardCoordinate to)
        {
            From = from;
            To = to;
        }

        public BoardCoordinate From { get; }

        public BoardCoordinate To { get; }
    }

    public readonly struct RefillSpawnMotion
    {
        public RefillSpawnMotion(BoardCoordinate spawnFrom, BoardCoordinate to, CubeColor color)
        {
            SpawnFrom = spawnFrom;
            To = to;
            Color = color;
        }

        public BoardCoordinate SpawnFrom { get; }

        public BoardCoordinate To { get; }

        public CubeColor Color { get; }
    }
}
