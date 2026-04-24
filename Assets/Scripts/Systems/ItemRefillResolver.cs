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
            var refillableCells = FindRefillableCells(board);

            for (var x = 0; x < board.Width; x++)
            {
                for (var y = 0; y < board.Height; y++)
                {
                    var coordinate = new BoardCoordinate(x, y);
                    var cell = board.GetCell(coordinate);
                    if (cell.HasItem
                        || cell.HasObstacle
                        || !refillableCells.Contains(coordinate))
                    {
                        continue;
                    }

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

        private static HashSet<BoardCoordinate> FindRefillableCells(BoardModel board)
        {
            var refillableCells = new HashSet<BoardCoordinate>();
            var pending = new Queue<BoardCoordinate>();

            for (var x = 0; x < board.Width; x++)
            {
                var topCoordinate = new BoardCoordinate(x, board.Height - 1);
                if (IsOpenCell(board, topCoordinate) && refillableCells.Add(topCoordinate))
                {
                    pending.Enqueue(topCoordinate);
                }
            }

            while (pending.Count > 0)
            {
                var sourceCoordinate = pending.Dequeue();
                TryAddReachableCell(board, sourceCoordinate, sourceCoordinate.Offset(0, -1), refillableCells, pending);
                TryAddReachableCell(board, sourceCoordinate, sourceCoordinate.Offset(-1, -1), refillableCells, pending);
                TryAddReachableCell(board, sourceCoordinate, sourceCoordinate.Offset(1, -1), refillableCells, pending);
            }

            return refillableCells;
        }

        private static void TryAddReachableCell(
            BoardModel board,
            BoardCoordinate sourceCoordinate,
            BoardCoordinate targetCoordinate,
            HashSet<BoardCoordinate> refillableCells,
            Queue<BoardCoordinate> pending)
        {
            if (!ItemGravityResolver.CanRefillStreamMoveBetweenEmptyCells(board, sourceCoordinate, targetCoordinate)
                || !refillableCells.Add(targetCoordinate))
            {
                return;
            }

            pending.Enqueue(targetCoordinate);
        }

        private static bool IsOpenCell(BoardModel board, BoardCoordinate coordinate)
        {
            return board.TryGetCell(coordinate, out var cell)
                && !cell.HasItem
                && !cell.HasObstacle;
        }
    }
}
