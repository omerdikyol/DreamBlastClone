using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class ItemRefillResolver
    {
        public ItemRefillResolutionResult Resolve(BoardModel board, IRefillCubeColorResolver colorResolver)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (colorResolver is null)
            {
                throw new ArgumentNullException(nameof(colorResolver));
            }

            var spawns = new List<ItemSpawn>();

            for (var x = 0; x < board.Width; x++)
            {
                for (var y = 0; y < board.Height; y++)
                {
                    var coordinate = new BoardCoordinate(x, y);
                    var cell = board.GetCell(coordinate);
                    if (cell.HasItem)
                    {
                        continue;
                    }

                    // Refill is item-layer-only for now, so obstacle occupancy does not block spawning.
                    var color = colorResolver.ResolveColor(coordinate);
                    if (!Enum.IsDefined(typeof(CubeColor), color))
                    {
                        throw new InvalidOperationException($"Refill color resolver returned invalid color '{color}' for {coordinate}.");
                    }

                    board.PlaceItem(coordinate, new CubeItemModel(color));
                    spawns.Add(new ItemSpawn(coordinate, color));
                }
            }

            return spawns.Count == 0
                ? ItemRefillResolutionResult.Empty()
                : new ItemRefillResolutionResult(spawns);
        }
    }
}
