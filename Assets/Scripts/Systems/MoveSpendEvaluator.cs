using System;

namespace DreamBlastClone.Systems
{
    public sealed class MoveSpendEvaluator
    {
        public bool ShouldSpendMove(BoardTapDispatchResult tapResult)
        {
            if (tapResult is null)
            {
                throw new ArgumentNullException(nameof(tapResult));
            }

            return tapResult.IsValidTap;
        }
    }
}
