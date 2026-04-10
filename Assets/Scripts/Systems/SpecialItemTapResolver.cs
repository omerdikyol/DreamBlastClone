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
            return ResolveStep(board, tapCoordinate).Activation;
        }

        internal ResolvedSpecialActivationStep ResolveStep(BoardModel board, BoardCoordinate tapCoordinate)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.TryGetCell(tapCoordinate, out var tappedCell))
            {
                return ResolvedSpecialActivationStep.Invalid();
            }

            return tappedCell.Item switch
            {
                RocketItemModel rocket => ResolveRocket(board, tapCoordinate, rocket),
                TntItemModel => ResolveTnt(board, tapCoordinate),
                _ => ResolvedSpecialActivationStep.Invalid()
            };
        }

        internal ResolvedSpecialActivationStep ResolveStep(BoardModel board, BoardCoordinate originCoordinate, ItemModel specialItem)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (specialItem is null)
            {
                throw new ArgumentNullException(nameof(specialItem));
            }

            return specialItem switch
            {
                RocketItemModel rocket => ResolveRocket(board, originCoordinate, rocket),
                TntItemModel => ResolveTnt(board, originCoordinate),
                _ => ResolvedSpecialActivationStep.Invalid()
            };
        }

        private static ResolvedSpecialActivationStep ResolveRocket(BoardModel board, BoardCoordinate tapCoordinate, RocketItemModel rocket)
        {
            var affectedCoordinates = new List<BoardCoordinate>();

            if (rocket.Orientation == RocketOrientation.Horizontal)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    affectedCoordinates.Add(new BoardCoordinate(x, tapCoordinate.Y));
                }
            }
            else
            {
                for (var y = 0; y < board.Height; y++)
                {
                    affectedCoordinates.Add(new BoardCoordinate(tapCoordinate.X, y));
                }
            }

            var removedItems = SpecialItemEffectItemRemovalResolver.Resolve(
                board,
                affectedCoordinates,
                new[] { tapCoordinate });

            return new ResolvedSpecialActivationStep(
                new SpecialItemActivationResult(
                    isValidActivation: true,
                    activationType: SpecialActivationType.Rocket,
                    affectedCoordinates: affectedCoordinates,
                    removedItemCoordinates: removedItems.RemovedItemCoordinates),
                removedItems.TriggeredSpecials);
        }

        private static ResolvedSpecialActivationStep ResolveTnt(BoardModel board, BoardCoordinate tapCoordinate)
        {
            var affectedCoordinates = new List<BoardCoordinate>();

            var minX = Math.Max(0, tapCoordinate.X - 2);
            var maxX = Math.Min(board.Width - 1, tapCoordinate.X + 2);
            var minY = Math.Max(0, tapCoordinate.Y - 2);
            var maxY = Math.Min(board.Height - 1, tapCoordinate.Y + 2);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    affectedCoordinates.Add(new BoardCoordinate(x, y));
                }
            }

            var removedItems = SpecialItemEffectItemRemovalResolver.Resolve(
                board,
                affectedCoordinates,
                new[] { tapCoordinate });

            return new ResolvedSpecialActivationStep(
                new SpecialItemActivationResult(
                    isValidActivation: true,
                    activationType: SpecialActivationType.Tnt,
                    affectedCoordinates: affectedCoordinates,
                    removedItemCoordinates: removedItems.RemovedItemCoordinates),
                removedItems.TriggeredSpecials);
        }
    }

    internal sealed class ResolvedSpecialActivationStep
    {
        private static readonly IReadOnlyList<TriggeredSpecialSeed> EmptyTriggeredSpecials = Array.Empty<TriggeredSpecialSeed>();

        public ResolvedSpecialActivationStep(
            SpecialItemActivationResult activation,
            IReadOnlyList<TriggeredSpecialSeed> triggeredSpecials)
        {
            Activation = activation ?? throw new ArgumentNullException(nameof(activation));
            TriggeredSpecials = triggeredSpecials ?? throw new ArgumentNullException(nameof(triggeredSpecials));
        }

        public SpecialItemActivationResult Activation { get; }

        public IReadOnlyList<TriggeredSpecialSeed> TriggeredSpecials { get; }

        public static ResolvedSpecialActivationStep Invalid()
        {
            return new ResolvedSpecialActivationStep(
                SpecialItemActivationResult.Invalid(),
                EmptyTriggeredSpecials);
        }
    }

    internal sealed class TriggeredSpecialSeed
    {
        public TriggeredSpecialSeed(BoardCoordinate coordinate, ItemModel item)
        {
            Coordinate = coordinate;
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public BoardCoordinate Coordinate { get; }

        public ItemModel Item { get; }
    }

    internal static class SpecialItemEffectItemRemovalResolver
    {
        public static ResolvedAffectedItemRemoval Resolve(
            BoardModel board,
            IReadOnlyList<BoardCoordinate> affectedCoordinates,
            IReadOnlyCollection<BoardCoordinate> initiatingSpecialCoordinates)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (affectedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(affectedCoordinates));
            }

            if (initiatingSpecialCoordinates is null)
            {
                throw new ArgumentNullException(nameof(initiatingSpecialCoordinates));
            }

            var removedItemCoordinates = new List<BoardCoordinate>();
            var triggeredSpecials = new List<TriggeredSpecialSeed>();
            var seenTriggeredCoordinates = new HashSet<BoardCoordinate>();
            var initiatingCoordinates = new HashSet<BoardCoordinate>(initiatingSpecialCoordinates);

            foreach (var coordinate in affectedCoordinates)
            {
                var cell = board.GetCell(coordinate);
                if (!cell.HasItem)
                {
                    continue;
                }

                if (!initiatingCoordinates.Contains(coordinate)
                    && TryCloneTriggeredSpecial(cell.Item, out var clonedSpecial)
                    && seenTriggeredCoordinates.Add(coordinate))
                {
                    triggeredSpecials.Add(new TriggeredSpecialSeed(coordinate, clonedSpecial));
                }

                board.ClearItem(coordinate);
                removedItemCoordinates.Add(coordinate);
            }

            return new ResolvedAffectedItemRemoval(removedItemCoordinates, triggeredSpecials);
        }

        private static bool TryCloneTriggeredSpecial(ItemModel item, out ItemModel clonedSpecial)
        {
            switch (item)
            {
                case RocketItemModel rocket:
                    clonedSpecial = new RocketItemModel(rocket.Orientation);
                    return true;
                case TntItemModel:
                    clonedSpecial = new TntItemModel();
                    return true;
                default:
                    clonedSpecial = null;
                    return false;
            }
        }
    }

    internal sealed class ResolvedAffectedItemRemoval
    {
        public ResolvedAffectedItemRemoval(
            IReadOnlyList<BoardCoordinate> removedItemCoordinates,
            IReadOnlyList<TriggeredSpecialSeed> triggeredSpecials)
        {
            RemovedItemCoordinates = removedItemCoordinates ?? throw new ArgumentNullException(nameof(removedItemCoordinates));
            TriggeredSpecials = triggeredSpecials ?? throw new ArgumentNullException(nameof(triggeredSpecials));
        }

        public IReadOnlyList<BoardCoordinate> RemovedItemCoordinates { get; }

        public IReadOnlyList<TriggeredSpecialSeed> TriggeredSpecials { get; }
    }
}
