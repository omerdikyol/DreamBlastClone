using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class NormalCubeTapCoordinator
    {
        private readonly CubeBlastResolver cubeBlastResolver = new CubeBlastResolver();
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

            // This coordinator runs a single blast -> gravity -> refill pass and intentionally stops there.
            var gravity = itemGravityResolver.Resolve(board);
            var refill = itemRefillResolver.Resolve(board, refillColorResolver);

            return new NormalCubeTapPipelineResult(
                isValidTap: true,
                blast: blast,
                gravity: gravity,
                refill: refill);
        }
    }
}
