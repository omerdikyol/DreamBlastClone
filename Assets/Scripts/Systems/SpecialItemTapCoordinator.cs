using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemTapCoordinator
    {
        private readonly SpecialItemComboResolver specialItemComboResolver = new SpecialItemComboResolver();
        private readonly SpecialComboObstacleDamageResolver specialComboObstacleDamageResolver = new SpecialComboObstacleDamageResolver();
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

            var combo = specialItemComboResolver.Resolve(board, tapCoordinate);
            if (combo.IsComboActivated)
            {
                var comboObstacleDamage = specialComboObstacleDamageResolver.Resolve(board, combo);
                var comboGravity = itemGravityResolver.Resolve(board);
                var comboRefill = itemRefillResolver.Resolve(board, refillColorResolver);

                return new SpecialItemTapPipelineResult(
                    isValidTap: true,
                    combo: combo,
                    activation: SpecialItemActivationResult.Invalid(),
                    obstacleDamage: comboObstacleDamage,
                    gravity: comboGravity,
                    refill: comboRefill);
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
                combo: SpecialItemComboActivationResult.Invalid(),
                activation: activation,
                obstacleDamage: obstacleDamage,
                gravity: gravity,
                refill: refill);
        }
    }
}
