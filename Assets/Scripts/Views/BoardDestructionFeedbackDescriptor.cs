using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class BoardDestructionFeedbackDescriptor
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();
        private static readonly IReadOnlyList<RemovedObstacleFeedback> EmptyObstacles = Array.Empty<RemovedObstacleFeedback>();

        public BoardDestructionFeedbackDescriptor(
            IReadOnlyList<BoardCoordinate> removedItemCoordinates,
            IReadOnlyList<RemovedObstacleFeedback> removedObstacles)
        {
            RemovedItemCoordinates = removedItemCoordinates ?? throw new ArgumentNullException(nameof(removedItemCoordinates));
            RemovedObstacles = removedObstacles ?? throw new ArgumentNullException(nameof(removedObstacles));
        }

        public IReadOnlyList<BoardCoordinate> RemovedItemCoordinates { get; }

        public IReadOnlyList<RemovedObstacleFeedback> RemovedObstacles { get; }

        public bool HasAnyFeedback => RemovedItemCoordinates.Count > 0 || RemovedObstacles.Count > 0;

        public static BoardDestructionFeedbackDescriptor Empty()
        {
            return new BoardDestructionFeedbackDescriptor(EmptyCoordinates, EmptyObstacles);
        }
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
