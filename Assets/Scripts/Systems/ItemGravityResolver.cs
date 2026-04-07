using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class ItemGravityResolver
    {
        public ItemGravityResolutionResult Resolve(BoardModel board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var moves = new List<ItemFallMove>();

            for (var x = 0; x < board.Width; x++)
            {
                var nextLandingY = 0;

                for (var y = 0; y < board.Height; y++)
                {
                    var sourceCoordinate = new BoardCoordinate(x, y);
                    var sourceCell = board.GetCell(sourceCoordinate);
                    if (!sourceCell.HasItem)
                    {
                        continue;
                    }

                    if (y == nextLandingY)
                    {
                        nextLandingY++;
                        continue;
                    }

                    var item = sourceCell.Item;
                    var destinationCoordinate = new BoardCoordinate(x, nextLandingY);

                    // This step compacts only the item layer; obstacle occupancy is intentionally ignored for now.
                    board.PlaceItem(destinationCoordinate, item);
                    board.ClearItem(sourceCoordinate);
                    moves.Add(new ItemFallMove(sourceCoordinate, destinationCoordinate));
                    nextLandingY++;
                }
            }

            return moves.Count == 0
                ? ItemGravityResolutionResult.Empty()
                : new ItemGravityResolutionResult(moves);
        }
    }
}
