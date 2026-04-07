using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform itemVisualRoot;
        [SerializeField] private Transform obstacleVisualRoot;

        [Header("Layout")]
        [SerializeField] private Vector2 origin;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float itemZ = -0.1f;
        [SerializeField] private float obstacleZ = 0f;

        [Header("Item Prefabs")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private GameObject horizontalRocketPrefab;
        [SerializeField] private GameObject verticalRocketPrefab;
        [SerializeField] private GameObject tntPrefab;

        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject vasePrefab;
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private GameObject chaliceBoxPrefab;

        [Header("Cube Colors")]
        [SerializeField] private Color redCubeColor = Color.red;
        [SerializeField] private Color greenCubeColor = Color.green;
        [SerializeField] private Color blueCubeColor = Color.blue;
        [SerializeField] private Color yellowCubeColor = Color.yellow;

        private readonly List<GameObject> spawnedVisuals = new List<GameObject>();

        public void Render(BoardModel board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Clear();
            RenderObstacles(board);
            RenderItems(board);
        }

        public void Clear()
        {
            for (var index = spawnedVisuals.Count - 1; index >= 0; index--)
            {
                var visual = spawnedVisuals[index];

                if (visual is null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(visual);
                }
                else
                {
                    DestroyImmediate(visual);
                }
            }

            spawnedVisuals.Clear();
        }

        public bool TryWorldToBoardCoordinate(BoardModel board, Vector3 worldPoint, out BoardCoordinate coordinate)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var localPoint = transform.InverseTransformPoint(worldPoint);
            var localX = (localPoint.x - origin.x) / cellSize;
            var localY = (localPoint.y - origin.y) / cellSize;

            if (localX < 0f || localY < 0f || localX >= board.Width || localY >= board.Height)
            {
                coordinate = default;
                return false;
            }

            coordinate = new BoardCoordinate(
                (int)Math.Floor(localX),
                (int)Math.Floor(localY));
            return true;
        }

        private void RenderItems(BoardModel board)
        {
            foreach (var cell in board.GetAllCells())
            {
                if (!cell.HasItem)
                {
                    continue;
                }

                var prefab = ResolveItemPrefab(cell.Item);
                var instance = Instantiate(prefab, ResolveItemRoot(), worldPositionStays: false);
                instance.name = $"{prefab.name}_{cell.Coordinate}";
                instance.transform.localPosition = GetCellCenter(cell.Coordinate, itemZ);
                instance.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                ApplyColor(instance, ResolveItemColor(cell.Item));
                spawnedVisuals.Add(instance);
            }
        }

        private void RenderObstacles(BoardModel board)
        {
            var coordinatesByObstacle = new Dictionary<ObstacleModel, List<BoardCoordinate>>();
            var obstaclesInOrder = new List<ObstacleModel>();

            foreach (var cell in board.GetAllCells())
            {
                if (!cell.HasObstacle)
                {
                    continue;
                }

                // Multi-cell obstacles share one runtime instance and render as a single footprint view.
                if (!coordinatesByObstacle.TryGetValue(cell.Obstacle, out var occupiedCoordinates))
                {
                    occupiedCoordinates = new List<BoardCoordinate>();
                    coordinatesByObstacle.Add(cell.Obstacle, occupiedCoordinates);
                    obstaclesInOrder.Add(cell.Obstacle);
                }

                occupiedCoordinates.Add(cell.Coordinate);
            }

            foreach (var obstacle in obstaclesInOrder)
            {
                var occupiedCoordinates = coordinatesByObstacle[obstacle];
                var prefab = ResolveObstaclePrefab(obstacle);
                var instance = Instantiate(prefab, ResolveObstacleRoot(), worldPositionStays: false);
                instance.name = $"{prefab.name}_{DescribeObstacleFootprint(occupiedCoordinates)}";
                instance.transform.localPosition = GetFootprintCenter(occupiedCoordinates, obstacleZ);
                instance.transform.localScale = GetFootprintScale(occupiedCoordinates);
                spawnedVisuals.Add(instance);
            }
        }

        private Transform ResolveItemRoot()
        {
            return itemVisualRoot is not null ? itemVisualRoot : transform;
        }

        private Transform ResolveObstacleRoot()
        {
            return obstacleVisualRoot is not null ? obstacleVisualRoot : transform;
        }

        private GameObject ResolveItemPrefab(ItemModel item)
        {
            return item switch
            {
                CubeItemModel => RequirePrefab(cubePrefab, nameof(cubePrefab)),
                RocketItemModel rocket when rocket.Orientation == RocketOrientation.Horizontal => RequirePrefab(horizontalRocketPrefab, nameof(horizontalRocketPrefab)),
                RocketItemModel rocket when rocket.Orientation == RocketOrientation.Vertical => RequirePrefab(verticalRocketPrefab, nameof(verticalRocketPrefab)),
                TntItemModel => RequirePrefab(tntPrefab, nameof(tntPrefab)),
                _ => throw new InvalidOperationException($"Unsupported item view type '{item.GetType().Name}'.")
            };
        }

        private GameObject ResolveObstaclePrefab(ObstacleModel obstacle)
        {
            return obstacle switch
            {
                VaseObstacleModel => RequirePrefab(vasePrefab, nameof(vasePrefab)),
                StoneObstacleModel => RequirePrefab(stonePrefab, nameof(stonePrefab)),
                ChaliceBoxObstacleModel => RequirePrefab(chaliceBoxPrefab, nameof(chaliceBoxPrefab)),
                _ => throw new InvalidOperationException($"Unsupported obstacle view type '{obstacle.GetType().Name}'.")
            };
        }

        private Color ResolveItemColor(ItemModel item)
        {
            return item switch
            {
                CubeItemModel cube => ResolveCubeColor(cube.Color),
                _ => Color.white
            };
        }

        private Color ResolveCubeColor(CubeColor color)
        {
            return color switch
            {
                CubeColor.Red => redCubeColor,
                CubeColor.Green => greenCubeColor,
                CubeColor.Blue => blueCubeColor,
                CubeColor.Yellow => yellowCubeColor,
                _ => throw new InvalidOperationException($"Unsupported cube color '{color}'.")
            };
        }

        private static GameObject RequirePrefab(GameObject prefab, string fieldName)
        {
            if (prefab is not null)
            {
                return prefab;
            }

            throw new InvalidOperationException($"BoardView requires '{fieldName}' to be assigned before rendering.");
        }

        private void ApplyColor(GameObject instance, Color color)
        {
            if (instance.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer.color = color;
            }
        }

        private Vector3 GetCellCenter(BoardCoordinate coordinate, float z)
        {
            return new Vector3(
                origin.x + (coordinate.X + 0.5f) * cellSize,
                origin.y + (coordinate.Y + 0.5f) * cellSize,
                z);
        }

        private Vector3 GetFootprintCenter(IReadOnlyList<BoardCoordinate> occupiedCoordinates, float z)
        {
            GetFootprintBounds(occupiedCoordinates, out var minX, out var minY, out var maxX, out var maxY);
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            return new Vector3(
                origin.x + (minX + width * 0.5f) * cellSize,
                origin.y + (minY + height * 0.5f) * cellSize,
                z);
        }

        private Vector3 GetFootprintScale(IReadOnlyList<BoardCoordinate> occupiedCoordinates)
        {
            GetFootprintBounds(occupiedCoordinates, out var minX, out var minY, out var maxX, out var maxY);
            return new Vector3(
                (maxX - minX + 1) * cellSize,
                (maxY - minY + 1) * cellSize,
                1f);
        }

        private static string DescribeObstacleFootprint(IReadOnlyList<BoardCoordinate> occupiedCoordinates)
        {
            GetFootprintBounds(occupiedCoordinates, out var minX, out var minY, out var maxX, out var maxY);
            return $"({minX},{minY})-({maxX},{maxY})";
        }

        private static void GetFootprintBounds(
            IReadOnlyList<BoardCoordinate> occupiedCoordinates,
            out int minX,
            out int minY,
            out int maxX,
            out int maxY)
        {
            minX = int.MaxValue;
            minY = int.MaxValue;
            maxX = int.MinValue;
            maxY = int.MinValue;

            foreach (var coordinate in occupiedCoordinates)
            {
                minX = Math.Min(minX, coordinate.X);
                minY = Math.Min(minY, coordinate.Y);
                maxX = Math.Max(maxX, coordinate.X);
                maxY = Math.Max(maxY, coordinate.Y);
            }
        }
    }
}
