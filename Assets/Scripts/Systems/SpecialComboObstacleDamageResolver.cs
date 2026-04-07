using System;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialComboObstacleDamageResolver
    {
        public ObstacleDamageResolutionResult Resolve(BoardModel board, SpecialItemComboActivationResult combo)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (combo is null)
            {
                throw new ArgumentNullException(nameof(combo));
            }

            if (!combo.IsComboActivated)
            {
                return ObstacleDamageResolutionResult.Empty();
            }

            return SpecialFootprintObstacleDamageResolver.Resolve(board, combo.AffectedCoordinates);
        }
    }
}
