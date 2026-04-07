using System;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialActivationObstacleDamageResolver
    {
        public ObstacleDamageResolutionResult Resolve(BoardModel board, SpecialItemActivationResult activation)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (activation is null)
            {
                throw new ArgumentNullException(nameof(activation));
            }

            if (!activation.IsValidActivation)
            {
                return ObstacleDamageResolutionResult.Empty();
            }

            return SpecialFootprintObstacleDamageResolver.Resolve(board, activation.AffectedCoordinates);
        }
    }
}
