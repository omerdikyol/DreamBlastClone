using System;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class BoardTapDispatcher
    {
        private readonly NormalCubeTapCoordinator normalCubeTapCoordinator = new NormalCubeTapCoordinator();
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();

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
                CubeItemModel => new BoardTapDispatchResult(
                    isValidTap: true,
                    routeType: TapRouteType.NormalCube,
                    normalCube: normalCubeTapCoordinator.Resolve(board, tapCoordinate, refillColorResolver),
                    specialItem: SpecialItemActivationResult.Invalid()),
                RocketItemModel or TntItemModel => new BoardTapDispatchResult(
                    isValidTap: true,
                    routeType: TapRouteType.SpecialItem,
                    normalCube: NormalCubeTapPipelineResult.Invalid(),
                    specialItem: specialItemTapResolver.Resolve(board, tapCoordinate)),
                _ => BoardTapDispatchResult.Invalid()
            };
        }
    }
}
