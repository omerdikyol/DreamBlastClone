using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class CubeBlastParticleDescriptor
    {
        public CubeBlastParticleDescriptor(IReadOnlyList<CubeBlastBurstGroup> burstGroups)
        {
            BurstGroups = burstGroups ?? throw new ArgumentNullException(nameof(burstGroups));
        }

        public CubeBlastParticleDescriptor(CubeColor cubeColor, IReadOnlyList<BoardCoordinate> burstCoordinates)
            : this(new[] { new CubeBlastBurstGroup(cubeColor, burstCoordinates) })
        {
        }

        public IReadOnlyList<CubeBlastBurstGroup> BurstGroups { get; }

        public bool HasAnyParticles
        {
            get
            {
                for (var index = 0; index < BurstGroups.Count; index++)
                {
                    if (BurstGroups[index].BurstCoordinates.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    public sealed class CubeBlastBurstGroup
    {
        public CubeBlastBurstGroup(CubeColor cubeColor, IReadOnlyList<BoardCoordinate> burstCoordinates)
        {
            CubeColor = cubeColor;
            BurstCoordinates = burstCoordinates ?? throw new ArgumentNullException(nameof(burstCoordinates));
        }

        public CubeColor CubeColor { get; }

        public IReadOnlyList<BoardCoordinate> BurstCoordinates { get; }
    }
}
