using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemTapCoordinator
    {
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();
        private readonly SpecialActivationObstacleDamageResolver specialActivationObstacleDamageResolver = new SpecialActivationObstacleDamageResolver();
        private readonly ItemGravityResolver itemGravityResolver = new ItemGravityResolver();
        private readonly ItemRefillResolver itemRefillResolver = new ItemRefillResolver();

        public SpecialItemTapPipelineResult Resolve(BoardModel board, BoardCoordinate tapCoordinate, IRefillCubeColorResolver refillColorResolver)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (refillColorResolver is null)
            {
                throw new ArgumentNullException(nameof(refillColorResolver));
            }

            var activation = specialItemTapResolver.Resolve(board, tapCoordinate);
            if (!activation.IsValidActivation)
            {
                return SpecialItemTapPipelineResult.Invalid();
            }

            // This coordinator runs one special activation pass, then settles the board without recursing into combos or chains.
            var obstacleDamage = specialActivationObstacleDamageResolver.Resolve(board, activation);
            var gravity = itemGravityResolver.Resolve(board);
            var refill = itemRefillResolver.Resolve(board, refillColorResolver);

            return new SpecialItemTapPipelineResult(
                isValidTap: true,
                activation: activation,
                obstacleDamage: obstacleDamage,
                gravity: gravity,
                refill: refill);
        }
    }
}
