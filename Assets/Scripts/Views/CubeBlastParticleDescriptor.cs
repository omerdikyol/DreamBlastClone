using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class CubeBlastParticleDescriptor
    {
        public CubeBlastParticleDescriptor(CubeColor cubeColor, IReadOnlyList<BoardCoordinate> burstCoordinates)
        {
            CubeColor = cubeColor;
            BurstCoordinates = burstCoordinates ?? throw new ArgumentNullException(nameof(burstCoordinates));
        }

        public CubeColor CubeColor { get; }

        public IReadOnlyList<BoardCoordinate> BurstCoordinates { get; }

        public bool HasAnyParticles => BurstCoordinates.Count > 0;
    }
}
