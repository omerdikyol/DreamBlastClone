using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemTapResolver
    {
        public SpecialItemActivationResult Resolve(BoardModel board, BoardCoordinate tapCoordinate)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.TryGetCell(tapCoordinate, out var tappedCell))
            {
                return SpecialItemActivationResult.Invalid();
            }

            return tappedCell.Item switch
            {
                RocketItemModel rocket => ResolveRocket(board, tapCoordinate, rocket),
                TntItemModel => ResolveTnt(board, tapCoordinate),
                _ => SpecialItemActivationResult.Invalid()
            };
        }

        private static SpecialItemActivationResult ResolveRocket(BoardModel board, BoardCoordinate tapCoordinate, RocketItemModel rocket)
        {
            var affectedCoordinates = new List<BoardCoordinate>();
            var removedItemCoordinates = new List<BoardCoordinate>();

            if (rocket.Orientation == RocketOrientation.Horizontal)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    AddAffectedCoordinate(board, new BoardCoordinate(x, tapCoordinate.Y), affectedCoordinates, removedItemCoordinates);
                }
            }
            else
            {
                for (var y = 0; y < board.Height; y++)
                {
                    AddAffectedCoordinate(board, new BoardCoordinate(tapCoordinate.X, y), affectedCoordinates, removedItemCoordinates);
                }
            }

            return new SpecialItemActivationResult(
                isValidActivation: true,
                activationType: SpecialActivationType.Rocket,
                affectedCoordinates: affectedCoordinates,
                removedItemCoordinates: removedItemCoordinates);
        }

        private static SpecialItemActivationResult ResolveTnt(BoardModel board, BoardCoordinate tapCoordinate)
        {
            var affectedCoordinates = new List<BoardCoordinate>();
            var removedItemCoordinates = new List<BoardCoordinate>();

            var minX = Math.Max(0, tapCoordinate.X - 2);
            var maxX = Math.Min(board.Width - 1, tapCoordinate.X + 2);
            var minY = Math.Max(0, tapCoordinate.Y - 2);
            var maxY = Math.Min(board.Height - 1, tapCoordinate.Y + 2);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    AddAffectedCoordinate(board, new BoardCoordinate(x, y), affectedCoordinates, removedItemCoordinates);
                }
            }

            return new SpecialItemActivationResult(
                isValidActivation: true,
                activationType: SpecialActivationType.Tnt,
                affectedCoordinates: affectedCoordinates,
                removedItemCoordinates: removedItemCoordinates);
        }

        private static void AddAffectedCoordinate(
            BoardModel board,
            BoardCoordinate coordinate,
            ICollection<BoardCoordinate> affectedCoordinates,
            ICollection<BoardCoordinate> removedItemCoordinates)
        {
            var cell = board.GetCell(coordinate);
            affectedCoordinates.Add(coordinate);

            // Single special activation removes affected items but intentionally does not recurse into any triggered specials.
            if (!cell.HasItem)
            {
                return;
            }

            board.ClearItem(coordinate);
            removedItemCoordinates.Add(coordinate);
        }
    }
}
