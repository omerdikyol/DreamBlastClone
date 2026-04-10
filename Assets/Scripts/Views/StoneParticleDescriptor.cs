using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class StoneParticleDescriptor
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public StoneParticleDescriptor(IReadOnlyList<BoardCoordinate> removedCoordinates)
        {
            RemovedCoordinates = removedCoordinates ?? throw new ArgumentNullException(nameof(removedCoordinates));
        }

        public IReadOnlyList<BoardCoordinate> RemovedCoordinates { get; }

        public bool HasAnyParticles => RemovedCoordinates.Count > 0;

        public static StoneParticleDescriptor Empty()
        {
            return new StoneParticleDescriptor(EmptyCoordinates);
        }
    }
}
