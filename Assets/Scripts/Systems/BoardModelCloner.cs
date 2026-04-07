using System;
using System.Collections.Generic;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class BoardModelCloner
    {
        public BoardModel Clone(BoardModel board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var clone = new BoardModel(board.Width, board.Height);
            var clonedObstacles = new Dictionary<ObstacleModel, ObstacleModel>();

            foreach (var cell in board.GetAllCells())
            {
                if (cell.HasItem)
                {
                    clone.PlaceItem(cell.Coordinate, CloneItem(cell.Item));
                }

                if (!cell.HasObstacle || clonedObstacles.ContainsKey(cell.Obstacle))
                {
                    continue;
                }

                var clonedObstacle = CloneObstacle(cell.Obstacle);
                clonedObstacles.Add(cell.Obstacle, clonedObstacle);

                switch (cell.Obstacle)
                {
                    case ChaliceBoxObstacleModel chaliceBox:
                        clone.PlaceObstacle(chaliceBox.OccupiedCoordinates, clonedObstacle);
                        break;
                    default:
                        clone.PlaceObstacle(cell.Coordinate, clonedObstacle);
                        break;
                }
            }

            return clone;
        }

        private static ItemModel CloneItem(ItemModel item)
        {
            return item switch
            {
                CubeItemModel cube => new CubeItemModel(cube.Color),
                RocketItemModel rocket => new RocketItemModel(rocket.Orientation),
                TntItemModel => new TntItemModel(),
                _ => throw new InvalidOperationException($"Unsupported item clone type '{item.GetType().Name}'.")
            };
        }

        private static ObstacleModel CloneObstacle(ObstacleModel obstacle)
        {
            return obstacle switch
            {
                VaseObstacleModel vase => new VaseObstacleModel(vase.RemainingDurability),
                StoneObstacleModel stone => new StoneObstacleModel(stone.RemainingDurability),
                ChaliceBoxObstacleModel chaliceBox => new ChaliceBoxObstacleModel(
                    chaliceBox.Anchor,
                    chaliceBox.RemainingDoorDurability,
                    chaliceBox.RequiredChaliceCount,
                    chaliceBox.CollectedChaliceCount),
                _ => throw new InvalidOperationException($"Unsupported obstacle clone type '{obstacle.GetType().Name}'.")
            };
        }
    }
}
