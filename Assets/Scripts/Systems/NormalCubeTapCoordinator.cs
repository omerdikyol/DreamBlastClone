using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class NormalCubeTapCoordinator
    {
        private readonly CubeBlastResolver cubeBlastResolver = new CubeBlastResolver();
        private readonly NormalBlastObstacleDamageResolver obstacleDamageResolver = new NormalBlastObstacleDamageResolver();
        private readonly ItemGravityResolver itemGravityResolver = new ItemGravityResolver();
        private readonly ItemRefillResolver itemRefillResolver = new ItemRefillResolver();

        public NormalCubeTapPipelineResult Resolve(BoardModel board, BoardCoordinate tapCoordinate, IRefillCubeColorResolver refillColorResolver)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (refillColorResolver is null)
            {
                throw new ArgumentNullException(nameof(refillColorResolver));
            }

            var blast = cubeBlastResolver.Resolve(board, tapCoordinate);
            if (!blast.IsValidBlast)
            {
                return NormalCubeTapPipelineResult.Invalid();
            }

            // This coordinator runs a single blast -> obstacle damage -> gravity -> refill pass and intentionally stops there.
            var obstacleDamage = obstacleDamageResolver.Resolve(board, blast);
            var gravity = itemGravityResolver.Resolve(board);
            var refill = itemRefillResolver.Resolve(board, refillColorResolver);

            return new NormalCubeTapPipelineResult(
                isValidTap: true,
                blast: blast,
                obstacleDamage: obstacleDamage,
                gravity: gravity,
                refill: refill);
        }
    }
}
