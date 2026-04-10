using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class CubeBlastParticleDescriptorBuilder
    {
        public CubeBlastParticleDescriptor Build(NormalCubeTapPipelineResult tap)
        {
            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            if (!tap.IsValidTap || !tap.Blast.IsValidBlast || !tap.Blast.BlastedCubeColor.HasValue)
            {
                return null;
            }

            var createdSpecialCoordinate = tap.Blast.CreatedSpecialCoordinate;
            var burstCoordinates = new List<BoardCoordinate>();

            foreach (var coordinate in tap.Blast.RemovedCoordinates)
            {
                if (createdSpecialCoordinate.HasValue && coordinate == createdSpecialCoordinate.Value)
                {
                    continue;
                }

                burstCoordinates.Add(coordinate);
            }

            return burstCoordinates.Count == 0
                ? null
                : new CubeBlastParticleDescriptor(tap.Blast.BlastedCubeColor.Value, burstCoordinates);
        }
    }
}
