using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
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

        [Header("Background")]
        [SerializeField] private SpriteRenderer gridBackgroundRenderer;
        [SerializeField] private Vector2 gridBackgroundPadding = new Vector2(0.2f, 0.2f);
        [SerializeField] private float gridBackgroundZ = 0.5f;

        [Header("Item Prefabs")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private GameObject horizontalRocketPrefab;
        [SerializeField] private GameObject verticalRocketPrefab;
        [SerializeField] private GameObject tntPrefab;

        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject vasePrefab;
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private GameObject chaliceBoxPrefab;

        private readonly List<GameObject> spawnedVisuals = new List<GameObject>();
        private readonly CubeGroupDetector cubeGroupDetector = new CubeGroupDetector();

        public float CellSize => cellSize;

        public void Render(BoardModel board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Clear();
            UpdateGridBackground(board);
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

        public Vector3 GetCellCenterWorld(BoardCoordinate coordinate)
        {
            return transform.TransformPoint(GetCellCenter(coordinate, 0f));
        }

        public GameObject CreateTransientItemVisual(BoardModel sourceBoard, BoardCoordinate coordinate, Transform parent, float z)
        {
            if (sourceBoard is null)
            {
                throw new ArgumentNullException(nameof(sourceBoard));
            }

            if (!sourceBoard.TryGetCell(coordinate, out var cell) || !cell.HasItem)
            {
                throw new InvalidOperationException($"Cannot create a transient item visual for empty coordinate {coordinate}.");
            }

            var instance = Instantiate(ResolveItemPrefab(cell.Item), parent, worldPositionStays: false);
            instance.name = $"Transient_{cell.Item.GetType().Name}_{coordinate}";
            instance.transform.position = transform.TransformPoint(GetCellCenter(coordinate, z));
            instance.transform.rotation = transform.rotation;
            ApplyItemAppearance(instance, cell.Item, coordinate, BuildCubeVisualStates(sourceBoard));
            instance.transform.localScale = GetVisualScale(instance.transform, new Vector2(cellSize, cellSize));
            return instance;
        }

        public GameObject CreateTransientObstacleVisual(
            BoardModel sourceBoard,
            IReadOnlyList<BoardCoordinate> occupiedCoordinates,
            Transform parent,
            float z)
        {
            if (sourceBoard is null)
            {
                throw new ArgumentNullException(nameof(sourceBoard));
            }

            if (occupiedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(occupiedCoordinates));
            }

            if (occupiedCoordinates.Count == 0)
            {
                throw new ArgumentException("Transient obstacle visuals require at least one occupied coordinate.", nameof(occupiedCoordinates));
            }

            if (!sourceBoard.TryGetCell(occupiedCoordinates[0], out var cell) || !cell.HasObstacle)
            {
                throw new InvalidOperationException($"Cannot create a transient obstacle visual for empty coordinate {occupiedCoordinates[0]}.");
            }

            var instance = Instantiate(ResolveObstaclePrefab(cell.Obstacle), parent, worldPositionStays: false);
            instance.name = $"Transient_{cell.Obstacle.GetType().Name}_{DescribeObstacleFootprint(occupiedCoordinates)}";
            instance.transform.position = transform.TransformPoint(GetFootprintCenter(occupiedCoordinates, z));
            instance.transform.rotation = transform.rotation;
            instance.transform.localScale = GetVisualScale(instance.transform, GetFootprintSize(occupiedCoordinates));
            return instance;
        }

        private void RenderItems(BoardModel board)
        {
            var cubeVisualStates = BuildCubeVisualStates(board);

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
                ApplyItemAppearance(instance, cell.Item, cell.Coordinate, cubeVisualStates);
                instance.transform.localScale = GetVisualScale(instance.transform, new Vector2(cellSize, cellSize));
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
                instance.transform.localScale = GetVisualScale(
                    instance.transform,
                    GetFootprintSize(occupiedCoordinates));
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

        private void ApplyItemAppearance(
            GameObject instance,
            ItemModel item,
            BoardCoordinate coordinate,
            IReadOnlyDictionary<BoardCoordinate, CubeVisualState> cubeVisualStates)
        {
            if (item is CubeItemModel cube)
            {
                ApplyCubeAppearance(instance, cube.Color, ResolveCubeVisualState(coordinate, cubeVisualStates));
                return;
            }

            ApplyDefaultItemAppearance(instance);
        }

        private void ApplyCubeAppearance(GameObject instance, CubeColor color, CubeVisualState visualState)
        {
            if (!instance.TryGetComponent<CubeItemView>(out var cubeItemView))
            {
                throw new InvalidOperationException("BoardView requires cubePrefab to include a CubeItemView component.");
            }

            cubeItemView.SetAppearance(color, visualState);
        }

        private void ApplyDefaultItemAppearance(GameObject instance)
        {
            if (instance.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            {
                spriteRenderer.color = Color.white;
            }
        }

        private IReadOnlyDictionary<BoardCoordinate, CubeVisualState> BuildCubeVisualStates(BoardModel board)
        {
            var visualStates = new Dictionary<BoardCoordinate, CubeVisualState>();

            foreach (var cell in board.GetAllCells())
            {
                if (!cell.HasItem || cell.Item is not CubeItemModel || visualStates.ContainsKey(cell.Coordinate))
                {
                    continue;
                }

                var group = cubeGroupDetector.FindGroup(board, cell.Coordinate);
                var visualState = ResolveCubeVisualState(group.Count);

                foreach (var coordinate in group.Coordinates)
                {
                    visualStates[coordinate] = visualState;
                }
            }

            return visualStates;
        }

        private static CubeVisualState ResolveCubeVisualState(
            BoardCoordinate coordinate,
            IReadOnlyDictionary<BoardCoordinate, CubeVisualState> cubeVisualStates)
        {
            return cubeVisualStates.TryGetValue(coordinate, out var visualState)
                ? visualState
                : CubeVisualState.Default;
        }

        private static CubeVisualState ResolveCubeVisualState(int groupSize)
        {
            if (groupSize == 4)
            {
                return CubeVisualState.RocketEligible;
            }

            if (groupSize >= 6)
            {
                return CubeVisualState.TntEligible;
            }

            return CubeVisualState.Default;
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

        private void UpdateGridBackground(BoardModel board)
        {
            if (gridBackgroundRenderer is null)
            {
                return;
            }

            var targetSize = new Vector2(
                board.Width * cellSize + gridBackgroundPadding.x * 2f,
                board.Height * cellSize + gridBackgroundPadding.y * 2f);
            var center = new Vector3(
                origin.x + board.Width * cellSize * 0.5f,
                origin.y + board.Height * cellSize * 0.5f,
                gridBackgroundZ);

            var backgroundTransform = gridBackgroundRenderer.transform;
            backgroundTransform.localPosition = center;

            if (gridBackgroundRenderer.drawMode == SpriteDrawMode.Simple)
            {
                backgroundTransform.localScale = GetSimpleSpriteScale(gridBackgroundRenderer, targetSize);
                return;
            }

            backgroundTransform.localScale = Vector3.one;
            gridBackgroundRenderer.size = targetSize;
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

        private Vector2 GetFootprintSize(IReadOnlyList<BoardCoordinate> occupiedCoordinates)
        {
            GetFootprintBounds(occupiedCoordinates, out var minX, out var minY, out var maxX, out var maxY);
            return new Vector2(
                (maxX - minX + 1) * cellSize,
                (maxY - minY + 1) * cellSize);
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

        private static Vector3 GetSimpleSpriteScale(SpriteRenderer spriteRenderer, Vector2 targetSize)
        {
            if (spriteRenderer.sprite is null)
            {
                return new Vector3(targetSize.x, targetSize.y, 1f);
            }

            var nativeSize = spriteRenderer.sprite.bounds.size;
            var safeWidth = nativeSize.x > 0f ? nativeSize.x : 1f;
            var safeHeight = nativeSize.y > 0f ? nativeSize.y : 1f;
            return new Vector3(targetSize.x / safeWidth, targetSize.y / safeHeight, 1f);
        }

        private static Vector3 GetVisualScale(Transform visualRoot, Vector2 targetSize)
        {
            if (!TryGetVisualSize(visualRoot, out var nativeSize))
            {
                return new Vector3(targetSize.x, targetSize.y, 1f);
            }

            var safeWidth = nativeSize.x > 0f ? nativeSize.x : 1f;
            var safeHeight = nativeSize.y > 0f ? nativeSize.y : 1f;
            return new Vector3(targetSize.x / safeWidth, targetSize.y / safeHeight, 1f);
        }

        private static bool TryGetVisualSize(Transform visualRoot, out Vector2 nativeSize)
        {
            var renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            var hasAnySprite = false;
            var min = Vector2.zero;
            var max = Vector2.zero;

            foreach (var spriteRenderer in renderers)
            {
                if (spriteRenderer.sprite is null)
                {
                    continue;
                }

                var spriteBounds = spriteRenderer.sprite.bounds;
                var corners = new[]
                {
                    new Vector3(spriteBounds.min.x, spriteBounds.min.y, 0f),
                    new Vector3(spriteBounds.min.x, spriteBounds.max.y, 0f),
                    new Vector3(spriteBounds.max.x, spriteBounds.min.y, 0f),
                    new Vector3(spriteBounds.max.x, spriteBounds.max.y, 0f)
                };

                for (var index = 0; index < 4; index++)
                {
                    var rootSpacePoint = (Vector2)visualRoot.InverseTransformPoint(
                        spriteRenderer.transform.TransformPoint(corners[index]));

                    if (!hasAnySprite)
                    {
                        min = rootSpacePoint;
                        max = rootSpacePoint;
                        hasAnySprite = true;
                        continue;
                    }

                    min = Vector2.Min(min, rootSpacePoint);
                    max = Vector2.Max(max, rootSpacePoint);
                }
            }

            nativeSize = hasAnySprite ? max - min : default;
            return hasAnySprite;
        }
    }
}
