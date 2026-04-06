using System;
using DreamBlastClone.Core;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Grid
{
    public sealed class CellModel
    {
        public CellModel(BoardCoordinate coordinate)
        {
            Coordinate = coordinate;
        }

        public BoardCoordinate Coordinate { get; }

        public ItemModel Item { get; private set; }

        public ObstacleModel Obstacle { get; private set; }

        public bool HasItem => Item is not null;

        public bool HasObstacle => Obstacle is not null;

        public bool IsCompletelyEmpty => !HasItem && !HasObstacle;

        // A cell can hold one movable item and one obstacle layer independently.
        // Multi-cell obstacles reuse the same obstacle instance across several cells.
        internal void PlaceItem(ItemModel item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        internal void PlaceObstacle(ObstacleModel obstacle)
        {
            Obstacle = obstacle ?? throw new ArgumentNullException(nameof(obstacle));
        }

        internal void ClearItem()
        {
            Item = null;
        }

        internal void ClearObstacle()
        {
            Obstacle = null;
        }
    }
}
