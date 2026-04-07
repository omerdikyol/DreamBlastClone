using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class BoardTapDispatcher
    {
        private readonly NormalCubeTapCoordinator normalCubeTapCoordinator = new NormalCubeTapCoordinator();
        private readonly SpecialItemTapCoordinator specialItemTapCoordinator = new SpecialItemTapCoordinator();

        public BoardTapDispatchResult Resolve(BoardModel board, BoardCoordinate tapCoordinate, IRefillCubeColorResolver refillColorResolver)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (refillColorResolver is null)
            {
                throw new ArgumentNullException(nameof(refillColorResolver));
            }

            if (!board.TryGetCell(tapCoordinate, out var tappedCell))
            {
                return BoardTapDispatchResult.Invalid();
            }

            // Top-level tap dispatch only routes to the existing branch and does not add extra behavior on top.
            return tappedCell.Item switch
            {
                CubeItemModel => BuildNormalCubeResult(board, tapCoordinate, refillColorResolver),
                RocketItemModel or TntItemModel => BuildSpecialItemResult(board, tapCoordinate, refillColorResolver),
                _ => BoardTapDispatchResult.Invalid()
            };
        }

        private BoardTapDispatchResult BuildNormalCubeResult(
            BoardModel board,
            BoardCoordinate tapCoordinate,
            IRefillCubeColorResolver refillColorResolver)
        {
            var normalCube = normalCubeTapCoordinator.Resolve(board, tapCoordinate, refillColorResolver);

            return new BoardTapDispatchResult(
                isValidTap: normalCube.IsValidTap,
                routeType: TapRouteType.NormalCube,
                normalCube: normalCube,
                specialItem: SpecialItemTapPipelineResult.Invalid());
        }

        private BoardTapDispatchResult BuildSpecialItemResult(
            BoardModel board,
            BoardCoordinate tapCoordinate,
            IRefillCubeColorResolver refillColorResolver)
        {
            var specialItem = specialItemTapCoordinator.Resolve(board, tapCoordinate, refillColorResolver);

            return new BoardTapDispatchResult(
                isValidTap: specialItem.IsValidTap,
                routeType: TapRouteType.SpecialItem,
                normalCube: NormalCubeTapPipelineResult.Invalid(),
                specialItem: specialItem);
        }
    }
}
