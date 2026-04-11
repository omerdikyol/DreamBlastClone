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
        // Fixed world-space point that all board sizes are centered on.
        // origin (bottom-left corner) is derived from this each Render() call.
        [SerializeField] private Vector2 boardCenter;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float itemZ = -0.1f;
        [SerializeField] private float obstacleZ = 0f;
        private Vector2 origin; // derived; not serialized

        [Header("Background")]
        [SerializeField] private SpriteRenderer gridBackgroundRenderer;
        [SerializeField] private Vector2 gridBackgroundPadding = new Vector2(0.2f, 0.2f);
        [SerializeField] private float gridBackgroundZ = 0.5f;
        [SerializeField] private SpriteMask boardClipMask;
        [SerializeField] private float boardClipMaskZ = -0.05f;

        [Header("Item Prefabs")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private GameObject horizontalRocketPrefab;
        [SerializeField] private GameObject verticalRocketPrefab;
        [SerializeField] private GameObject tntPrefab;

        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject vasePrefab;
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private GameObject chaliceBoxPrefab;

        private const int ChaliceSlotCount = 10;
        private static readonly bool[] HiddenChaliceSlotMask = new bool[ChaliceSlotCount];
        private readonly List<GameObject> spawnedVisuals = new List<GameObject>();
        private readonly CubeGroupDetector cubeGroupDetector = new CubeGroupDetector();
        private readonly Dictionary<BoardCoordinate, ChaliceBoxPresentationState> chalicePresentationStates = new Dictionary<BoardCoordinate, ChaliceBoxPresentationState>();
        private readonly Dictionary<BoardCoordinate, RenderedCubeAppearance> previousCubeAppearances = new Dictionary<BoardCoordinate, RenderedCubeAppearance>();
        private readonly System.Random chalicePresentationRandom = new System.Random();
        private static Sprite runtimeBoardClipMaskSprite;

        public float CellSize => cellSize;

        public void Render(BoardModel board, bool startItemIdleLoops = true)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            // Recompute bottom-left origin so the board stays centered on boardCenter
            // regardless of grid dimensions. All placement helpers read this field.
            origin = new Vector2(
                boardCenter.x - board.Width * cellSize * 0.5f,
                boardCenter.y - board.Height * cellSize * 0.5f);

            ClearSpawnedVisuals();
            UpdateGridBackground(board);
            UpdateBoardClipMask(board);
            RenderObstacles(board);
            RenderItems(board, startItemIdleLoops);
        }

        public void Clear()
        {
            ClearSpawnedVisuals();
            previousCubeAppearances.Clear();
        }

        private void ClearSpawnedVisuals()
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

        public void ApplyBoardClipMask(BoardModel board, GameObject visual)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (visual is null)
            {
                throw new ArgumentNullException(nameof(visual));
            }

            UpdateBoardClipMask(board);

            var renderers = visual.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] is not null)
                {
                    renderers[index].maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
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

        public GameObject CreateTransientCubeVisual(
            BoardModel boardContext,
            BoardCoordinate coordinate,
            CubeColor color,
            Transform parent,
            float z)
        {
            if (boardContext is null)
            {
                throw new ArgumentNullException(nameof(boardContext));
            }

            var instance = Instantiate(RequirePrefab(cubePrefab, nameof(cubePrefab)), parent, worldPositionStays: false);
            instance.name = $"Transient_Cube_{color}_{coordinate}";
            instance.transform.position = transform.TransformPoint(GetCellCenter(coordinate, z));
            instance.transform.rotation = transform.rotation;
            ApplyCubeAppearance(instance, color, ResolveCubeVisualState(coordinate, BuildCubeVisualStates(boardContext)));
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
            ApplyObstacleAppearance(instance, cell.Obstacle, startChaliceBoxTweenPresentation: false);
            return instance;
        }

        private void RenderItems(BoardModel board, bool startItemIdleLoops)
        {
            var cubeVisualStates = BuildCubeVisualStates(board);
            var currentCubeAppearances = new Dictionary<BoardCoordinate, RenderedCubeAppearance>();

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
                var cubeVisualState = cell.Item is CubeItemModel
                    ? ResolveCubeVisualState(cell.Coordinate, cubeVisualStates)
                    : CubeVisualState.Default;
                ApplyItemAppearance(instance, cell.Item, cell.Coordinate, cubeVisualStates);
                instance.transform.localScale = GetVisualScale(instance.transform, new Vector2(cellSize, cellSize));

                if (cell.Item is CubeItemModel cube && instance.TryGetComponent<CubeItemView>(out var cubeItemView))
                {
                    var shouldAnimateTransition = ShouldAnimateCubeVisualTransition(cell, cubeVisualState);
                    currentCubeAppearances[cell.Coordinate] = new RenderedCubeAppearance(cube.Color, cubeVisualState);
                    cubeItemView.PlayVisualStateTransition(
                        shouldAnimateTransition ? cubeVisualState : CubeVisualState.Default,
                        instance.TryGetComponent<BoardItemIdleLoopView>(out var cubeIdleLoop) ? cubeIdleLoop : null,
                        GetIdlePhaseOffset(cell.Coordinate),
                        startItemIdleLoops);
                }
                else if (startItemIdleLoops && instance.TryGetComponent<BoardItemIdleLoopView>(out var idleLoop))
                {
                    idleLoop.PlayAtGlobalPhase(GetIdlePhaseOffset(cell.Coordinate));
                }

                spawnedVisuals.Add(instance);
            }

            previousCubeAppearances.Clear();
            foreach (var pair in currentCubeAppearances)
            {
                previousCubeAppearances[pair.Key] = pair.Value;
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
                ApplyObstacleAppearance(instance, obstacle, startChaliceBoxTweenPresentation: true);
                spawnedVisuals.Add(instance);
            }
        }

        private Transform ResolveItemRoot()
        {
            return itemVisualRoot is not null ? itemVisualRoot : transform;
        }

        private static float GetIdlePhaseOffset(BoardCoordinate coordinate)
        {
            var hash = coordinate.X * 73856093 ^ coordinate.Y * 19349663;
            return Mathf.Abs(hash % 1000) / 1000f;
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

        private void ApplyObstacleAppearance(GameObject instance, ObstacleModel obstacle, bool startChaliceBoxTweenPresentation)
        {
            switch (obstacle)
            {
                case VaseObstacleModel vase:
                    ApplyVaseAppearance(instance, vase);
                    break;
                case ChaliceBoxObstacleModel chaliceBox:
                    ApplyChaliceBoxAppearance(instance, chaliceBox, startChaliceBoxTweenPresentation);
                    break;
                default:
                    ApplyDefaultObstacleAppearance(instance);
                    break;
            }
        }

        private void ApplyVaseAppearance(GameObject instance, VaseObstacleModel vase)
        {
            if (!instance.TryGetComponent<VaseObstacleView>(out var vaseObstacleView))
            {
                throw new InvalidOperationException("BoardView requires vasePrefab to include a VaseObstacleView component.");
            }

            vaseObstacleView.SetAppearance(vase.RemainingDurability);
        }

        private void ApplyChaliceBoxAppearance(GameObject instance, ChaliceBoxObstacleModel chaliceBox, bool startTweenPresentation)
        {
            if (!instance.TryGetComponent<ChaliceBoxObstacleView>(out var chaliceBoxView))
            {
                throw new InvalidOperationException("BoardView requires chaliceBoxPrefab to include a ChaliceBoxObstacleView component.");
            }

            var visualState = ResolveChaliceBoxVisualState(chaliceBox, trackPresentationState: startTweenPresentation);
            chaliceBoxView.SetAppearance(
                chaliceBox.RemainingDoorDurability > 0,
                visualState.VisibleSlotMask,
                startTweenPresentation,
                visualState.RemovedSlotIndices,
                GetIdlePhaseOffset(chaliceBox.Anchor));
        }

        private void ApplyDefaultObstacleAppearance(GameObject instance)
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

        private bool ShouldAnimateCubeVisualTransition(CellModel cell, CubeVisualState visualState)
        {
            if (cell.Item is not CubeItemModel cube
                || visualState == CubeVisualState.Default)
            {
                return false;
            }

            return !previousCubeAppearances.TryGetValue(cell.Coordinate, out var previousAppearance)
                || previousAppearance.Color != cube.Color
                || previousAppearance.State != visualState;
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

        private ChaliceBoxVisualState ResolveChaliceBoxVisualState(ChaliceBoxObstacleModel chaliceBox, bool trackPresentationState)
        {
            if (chaliceBox.RemainingDoorDurability > 0)
            {
                if (trackPresentationState)
                {
                    chalicePresentationStates.Remove(chaliceBox.Anchor);
                }

                return new ChaliceBoxVisualState(HiddenChaliceSlotMask, Array.Empty<int>());
            }

            var remainingChalices = Math.Max(0, Math.Min(ChaliceSlotCount, chaliceBox.RemainingChaliceCount));
            var removedSlotIndices = Array.Empty<int>();
            var hasTrackedState = chalicePresentationStates.TryGetValue(chaliceBox.Anchor, out var state);
            if (!trackPresentationState && hasTrackedState)
            {
                // Transient copies should never resurrect already-removed chalices.
                remainingChalices = Math.Min(remainingChalices, state.LastRemainingChalices);
            }

            if (!hasTrackedState || remainingChalices > state.LastRemainingChalices)
            {
                state = new ChaliceBoxPresentationState(CreateRandomRemovalOrder(), remainingChalices);
                if (trackPresentationState)
                {
                    chalicePresentationStates[chaliceBox.Anchor] = state;
                }
            }
            else
            {
                if (trackPresentationState && remainingChalices < state.LastRemainingChalices)
                {
                    removedSlotIndices = GetNewlyRemovedChaliceSlots(state, remainingChalices);
                }

                if (trackPresentationState)
                {
                    state.LastRemainingChalices = remainingChalices;
                }
            }

            var visibleSlotMask = new bool[ChaliceSlotCount];
            for (var index = 0; index < ChaliceSlotCount; index++)
            {
                visibleSlotMask[index] = true;
            }

            for (var removedCount = 0; removedCount < ChaliceSlotCount - remainingChalices; removedCount++)
            {
                visibleSlotMask[state.RemovalOrder[removedCount]] = false;
            }

            return new ChaliceBoxVisualState(visibleSlotMask, removedSlotIndices);
        }

        private static int[] GetNewlyRemovedChaliceSlots(ChaliceBoxPresentationState state, int remainingChalices)
        {
            var previousRemovedCount = ChaliceSlotCount - state.LastRemainingChalices;
            var nextRemovedCount = ChaliceSlotCount - remainingChalices;
            var newlyRemovedCount = Math.Max(0, nextRemovedCount - previousRemovedCount);
            var removedSlots = new int[newlyRemovedCount];

            for (var index = 0; index < newlyRemovedCount; index++)
            {
                removedSlots[index] = state.RemovalOrder[previousRemovedCount + index];
            }

            return removedSlots;
        }

        private int[] CreateRandomRemovalOrder()
        {
            var removalOrder = new int[ChaliceSlotCount];

            for (var index = 0; index < removalOrder.Length; index++)
            {
                removalOrder[index] = index;
            }

            for (var index = removalOrder.Length - 1; index > 0; index--)
            {
                var swapIndex = chalicePresentationRandom.Next(index + 1);
                (removalOrder[index], removalOrder[swapIndex]) = (removalOrder[swapIndex], removalOrder[index]);
            }

            return removalOrder;
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

        private void UpdateBoardClipMask(BoardModel board)
        {
            var clipMask = ResolveBoardClipMask();
            if (clipMask == null)
            {
                return;
            }

            clipMask.sprite = GetRuntimeBoardClipMaskSprite();
            clipMask.alphaCutoff = 0.01f;

            var targetSize = new Vector2(
                board.Width * cellSize,
                board.Height * cellSize);
            var center = new Vector3(
                origin.x + board.Width * cellSize * 0.5f,
                origin.y + board.Height * cellSize * 0.5f,
                boardClipMaskZ);

            var maskTransform = clipMask.transform;
            maskTransform.localPosition = center;
            maskTransform.localRotation = Quaternion.identity;
            maskTransform.localScale = new Vector3(targetSize.x, targetSize.y, 1f);
        }

        private SpriteMask ResolveBoardClipMask()
        {
            if (boardClipMask != null)
            {
                return boardClipMask;
            }

            var clipObject = new GameObject("BoardClipMask");
            clipObject.transform.SetParent(transform, false);
            boardClipMask = clipObject.AddComponent<SpriteMask>();
            return boardClipMask;
        }

        private static Sprite GetRuntimeBoardClipMaskSprite()
        {
            if (runtimeBoardClipMaskSprite != null)
            {
                return runtimeBoardClipMaskSprite;
            }

            runtimeBoardClipMaskSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            return runtimeBoardClipMaskSprite;
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

        private sealed class ChaliceBoxPresentationState
        {
            public ChaliceBoxPresentationState(int[] removalOrder, int lastRemainingChalices)
            {
                RemovalOrder = removalOrder ?? throw new ArgumentNullException(nameof(removalOrder));
                LastRemainingChalices = lastRemainingChalices;
            }

            public int[] RemovalOrder { get; }

            public int LastRemainingChalices { get; set; }
        }

        private readonly struct RenderedCubeAppearance
        {
            public RenderedCubeAppearance(CubeColor color, CubeVisualState state)
            {
                Color = color;
                State = state;
            }

            public CubeColor Color { get; }

            public CubeVisualState State { get; }
        }

        private readonly struct ChaliceBoxVisualState
        {
            public ChaliceBoxVisualState(IReadOnlyList<bool> visibleSlotMask, IReadOnlyList<int> removedSlotIndices)
            {
                VisibleSlotMask = visibleSlotMask;
                RemovedSlotIndices = removedSlotIndices;
            }

            public IReadOnlyList<bool> VisibleSlotMask { get; }

            public IReadOnlyList<int> RemovedSlotIndices { get; }
        }
    }
}
