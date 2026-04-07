using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public sealed class ObstacleDamageResolutionResult
    {
        private static readonly IReadOnlyList<ObstacleDamage> EmptyDamages = Array.Empty<ObstacleDamage>();
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public ObstacleDamageResolutionResult(IReadOnlyList<ObstacleDamage> damages, IReadOnlyList<BoardCoordinate> removedCoordinates = null)
        {
            Damages = damages ?? throw new ArgumentNullException(nameof(damages));
            RemovedCoordinates = removedCoordinates ?? EmptyCoordinates;
        }

        public IReadOnlyList<ObstacleDamage> Damages { get; }

        public IReadOnlyList<BoardCoordinate> RemovedCoordinates { get; }

        public int DamageCount => Damages.Count;

        public bool HasAnyDamage => DamageCount > 0;

        public bool HasAnyRemoval => RemovedCoordinates.Count > 0;

        public static ObstacleDamageResolutionResult Empty()
        {
            return new ObstacleDamageResolutionResult(EmptyDamages, EmptyCoordinates);
        }
    }
}
