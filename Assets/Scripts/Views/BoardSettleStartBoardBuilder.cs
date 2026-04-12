using System;
using DreamBlastClone.Grid;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class BoardSettleStartBoardBuilder
    {
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();

        public BoardModel Build(BoardModel previewBoard, BoardTapDispatchResult tap)
        {
            if (previewBoard is null)
            {
                throw new ArgumentNullException(nameof(previewBoard));
            }

            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            var gravity = tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.Gravity,
                TapRouteType.SpecialItem => tap.SpecialItem.Gravity,
                _ => ItemGravityResolutionResult.Empty()
            };

            var settleBoard = boardModelCloner.Clone(previewBoard);
            foreach (var move in gravity.Moves)
            {
                if (settleBoard.TryGetCell(move.From, out var cell) && cell.HasItem)
                {
                    settleBoard.ClearItem(move.From);
                }
            }

            foreach (var move in gravity.ObstacleMoves)
            {
                if (settleBoard.TryGetCell(move.From, out var cell) && cell.HasObstacle)
                {
                    settleBoard.ClearObstacle(move.From);
                }
            }

            return settleBoard;
        }
    }
}
