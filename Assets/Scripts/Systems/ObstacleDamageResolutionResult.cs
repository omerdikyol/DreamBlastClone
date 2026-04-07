using System;
using System.Collections.Generic;

namespace DreamBlastClone.Systems
{
    public sealed class ObstacleDamageResolutionResult
    {
        private static readonly IReadOnlyList<ObstacleDamage> EmptyDamages = Array.Empty<ObstacleDamage>();

        public ObstacleDamageResolutionResult(IReadOnlyList<ObstacleDamage> damages)
        {
            Damages = damages ?? throw new ArgumentNullException(nameof(damages));
        }

        public IReadOnlyList<ObstacleDamage> Damages { get; }

        public int DamageCount => Damages.Count;

        public bool HasAnyDamage => DamageCount > 0;

        public static ObstacleDamageResolutionResult Empty()
        {
            return new ObstacleDamageResolutionResult(EmptyDamages);
        }
    }
}
