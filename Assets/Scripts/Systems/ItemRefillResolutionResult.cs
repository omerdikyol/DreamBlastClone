using System;
using System.Collections.Generic;

namespace DreamBlastClone.Systems
{
    public sealed class ItemRefillResolutionResult
    {
        private static readonly IReadOnlyList<ItemSpawn> EmptySpawns = Array.Empty<ItemSpawn>();

        public ItemRefillResolutionResult(IReadOnlyList<ItemSpawn> spawns)
        {
            Spawns = spawns ?? throw new ArgumentNullException(nameof(spawns));
        }

        public IReadOnlyList<ItemSpawn> Spawns { get; }

        public int SpawnCount => Spawns.Count;

        public bool HasAnySpawn => SpawnCount > 0;

        public static ItemRefillResolutionResult Empty()
        {
            return new ItemRefillResolutionResult(EmptySpawns);
        }
    }
}
