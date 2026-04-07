using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemComboResolver
    {
        public SpecialItemComboActivationResult Resolve(BoardModel board, BoardCoordinate tapCoordinate)
        {
            if (board is null)
            {
                return SpecialItemComboActivationResult.Invalid();
            }

            if (!board.TryGetCell(tapCoordinate, out var tappedCell) || !IsComboSpecial(tappedCell.Item))
            {
                return SpecialItemComboActivationResult.Invalid();
            }

            foreach (var neighborCoordinate in tapCoordinate.GetOrthogonalNeighbors())
            {
                if (!board.TryGetCell(neighborCoordinate, out var neighborCell) || !IsComboSpecial(neighborCell.Item))
                {
                    continue;
                }

                var comboType = ResolveComboType(tappedCell.Item, neighborCell.Item);
                var affectedCoordinates = BuildAffectedCoordinates(board, tapCoordinate, comboType);
                var removedItemCoordinates = RemoveAffectedItems(board, affectedCoordinates);

                return new SpecialItemComboActivationResult(
                    isComboActivated: true,
                    comboType: comboType,
                    participatingSpecialCoordinates: new[]
                    {
                        tapCoordinate,
                        neighborCoordinate
                    },
                    affectedCoordinates: affectedCoordinates,
                    removedItemCoordinates: removedItemCoordinates);
            }

            return SpecialItemComboActivationResult.Invalid();
        }

        private static bool IsComboSpecial(ItemModel item)
        {
            return item is RocketItemModel or TntItemModel;
        }

        private static SpecialItemComboType ResolveComboType(ItemModel tappedItem, ItemModel neighborItem)
        {
            if (tappedItem is RocketItemModel && neighborItem is RocketItemModel)
            {
                return SpecialItemComboType.RocketRocket;
            }

            if (tappedItem is TntItemModel && neighborItem is TntItemModel)
            {
                return SpecialItemComboType.TntTnt;
            }

            return SpecialItemComboType.TntRocket;
        }

        private static IReadOnlyList<BoardCoordinate> BuildAffectedCoordinates(
            BoardModel board,
            BoardCoordinate tapCoordinate,
            SpecialItemComboType comboType)
        {
            var affectedCoordinates = new List<BoardCoordinate>();

            switch (comboType)
            {
                case SpecialItemComboType.RocketRocket:
                    for (var y = 0; y < board.Height; y++)
                    {
                        for (var x = 0; x < board.Width; x++)
                        {
                            if (x == tapCoordinate.X || y == tapCoordinate.Y)
                            {
                                affectedCoordinates.Add(new BoardCoordinate(x, y));
                            }
                        }
                    }

                    break;
                case SpecialItemComboType.TntTnt:
                    var minTntX = tapCoordinate.X - 3;
                    var maxTntX = tapCoordinate.X + 3;
                    var minTntY = tapCoordinate.Y - 3;
                    var maxTntY = tapCoordinate.Y + 3;

                    for (var y = 0; y < board.Height; y++)
                    {
                        for (var x = 0; x < board.Width; x++)
                        {
                            if (x >= minTntX && x <= maxTntX && y >= minTntY && y <= maxTntY)
                            {
                                affectedCoordinates.Add(new BoardCoordinate(x, y));
                            }
                        }
                    }

                    break;
                case SpecialItemComboType.TntRocket:
                    var minRocketX = tapCoordinate.X - 1;
                    var maxRocketX = tapCoordinate.X + 1;
                    var minRocketY = tapCoordinate.Y - 1;
                    var maxRocketY = tapCoordinate.Y + 1;

                    for (var y = 0; y < board.Height; y++)
                    {
                        for (var x = 0; x < board.Width; x++)
                        {
                            // TNT + Rocket uses the centered 3x3 area as rocket origins, so the footprint is the union of 3 rows and 3 columns.
                            if ((x >= minRocketX && x <= maxRocketX) || (y >= minRocketY && y <= maxRocketY))
                            {
                                affectedCoordinates.Add(new BoardCoordinate(x, y));
                            }
                        }
                    }

                    break;
            }

            return affectedCoordinates;
        }

        private static IReadOnlyList<BoardCoordinate> RemoveAffectedItems(BoardModel board, IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            var removedItemCoordinates = new List<BoardCoordinate>();

            foreach (var coordinate in affectedCoordinates)
            {
                var cell = board.GetCell(coordinate);
                if (!cell.HasItem)
                {
                    continue;
                }

                board.ClearItem(coordinate);
                removedItemCoordinates.Add(coordinate);
            }

            return removedItemCoordinates;
        }
    }
}
