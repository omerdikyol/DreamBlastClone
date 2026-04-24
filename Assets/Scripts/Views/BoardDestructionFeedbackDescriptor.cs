using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class BoardDestructionFeedbackDescriptor
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();
        private static readonly IReadOnlyList<RemovedItemFeedback> EmptyItems = Array.Empty<RemovedItemFeedback>();
        private static readonly IReadOnlyList<RemovedObstacleFeedback> EmptyObstacles = Array.Empty<RemovedObstacleFeedback>();

        public BoardDestructionFeedbackDescriptor(
            IReadOnlyList<BoardCoordinate> removedItemCoordinates,
            IReadOnlyList<RemovedObstacleFeedback> removedObstacles)
            : this(BuildRemovedItems(removedItemCoordinates), removedObstacles)
        {
        }

        public BoardDestructionFeedbackDescriptor(
            IReadOnlyList<RemovedItemFeedback> removedItems,
            IReadOnlyList<RemovedObstacleFeedback> removedObstacles)
        {
            RemovedItems = removedItems ?? throw new ArgumentNullException(nameof(removedItems));
            RemovedObstacles = removedObstacles ?? throw new ArgumentNullException(nameof(removedObstacles));
            RemovedItemCoordinates = BuildRemovedCoordinates(removedItems);
        }

        public IReadOnlyList<RemovedItemFeedback> RemovedItems { get; }

        public IReadOnlyList<BoardCoordinate> RemovedItemCoordinates { get; }

        public IReadOnlyList<RemovedObstacleFeedback> RemovedObstacles { get; }

        public bool HasAnyFeedback => RemovedItemCoordinates.Count > 0 || RemovedObstacles.Count > 0;

        public static BoardDestructionFeedbackDescriptor Empty()
        {
            return new BoardDestructionFeedbackDescriptor(EmptyItems, EmptyObstacles);
        }

        private static IReadOnlyList<RemovedItemFeedback> BuildRemovedItems(IReadOnlyList<BoardCoordinate> removedItemCoordinates)
        {
            if (removedItemCoordinates is null)
            {
                throw new ArgumentNullException(nameof(removedItemCoordinates));
            }

            if (removedItemCoordinates.Count == 0)
            {
                return EmptyItems;
            }

            var removedItems = new RemovedItemFeedback[removedItemCoordinates.Count];
            for (var index = 0; index < removedItemCoordinates.Count; index++)
            {
                removedItems[index] = new RemovedItemFeedback(removedItemCoordinates[index], hitStep: 0);
            }

            return removedItems;
        }

        private static IReadOnlyList<BoardCoordinate> BuildRemovedCoordinates(IReadOnlyList<RemovedItemFeedback> removedItems)
        {
            if (removedItems.Count == 0)
            {
                return EmptyCoordinates;
            }

            var removedCoordinates = new BoardCoordinate[removedItems.Count];
            for (var index = 0; index < removedItems.Count; index++)
            {
                removedCoordinates[index] = removedItems[index].Coordinate;
            }

            return removedCoordinates;
        }
    }

    public sealed class RemovedItemFeedback
    {
        public RemovedItemFeedback(BoardCoordinate coordinate, int hitStep)
            : this(coordinate, hitStep, growsBeforeRemoval: false)
        {
        }

        public RemovedItemFeedback(BoardCoordinate coordinate, int hitStep, bool growsBeforeRemoval)
        {
            if (hitStep < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hitStep), hitStep, "Hit step cannot be negative.");
            }

            Coordinate = coordinate;
            HitStep = hitStep;
            GrowsBeforeRemoval = growsBeforeRemoval;
        }

        public BoardCoordinate Coordinate { get; }

        public int HitStep { get; }

        public bool GrowsBeforeRemoval { get; }
    }

    public sealed class RemovedObstacleFeedback
    {
        public RemovedObstacleFeedback(IReadOnlyList<BoardCoordinate> occupiedCoordinates)
        {
            if (occupiedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(occupiedCoordinates));
            }

            if (occupiedCoordinates.Count == 0)
            {
                throw new ArgumentException("Removed obstacle feedback requires at least one occupied coordinate.", nameof(occupiedCoordinates));
            }

            OccupiedCoordinates = occupiedCoordinates;
        }

        public IReadOnlyList<BoardCoordinate> OccupiedCoordinates { get; }
    }
}
