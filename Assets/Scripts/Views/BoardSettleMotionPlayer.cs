using System;
using System.Collections.Generic;
using DreamBlastClone.Grid;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class BoardSettleMotionPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [SerializeField] private float secondsPerCell = 0.08f;
        [SerializeField] private float minimumDuration = 0.12f;
        [SerializeField] private float columnLandingStep = 0.04f;
        [SerializeField] private float effectZ = -0.12f;

        private readonly List<ActiveMotionVisual> activeVisuals = new List<ActiveMotionVisual>();
        private float activeDuration;
        private float elapsed;

        public bool IsPlaying => activeVisuals.Count > 0;

        public float Duration => activeDuration;

        public bool TryPlay(BoardView boardView, BoardModel finalBoard, BoardSettleMotionDescriptor descriptor)
        {
            if (boardView is null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (finalBoard is null)
            {
                throw new ArgumentNullException(nameof(finalBoard));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!descriptor.HasAnyMotion || secondsPerCell <= 0f)
            {
                return false;
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;

            foreach (var move in descriptor.GravityMoves)
            {
                var visual = boardView.CreateTransientItemVisual(finalBoard, move.To, root, effectZ);
                Register(
                    visual,
                    GetWorldPosition(boardView, move.From),
                    GetWorldPosition(boardView, move.To),
                    move.To.X,
                    move.To.Y,
                    isRefillSpawn: false);
            }

            foreach (var spawn in descriptor.RefillSpawns)
            {
                var visual = boardView.CreateTransientCubeVisual(finalBoard, spawn.To, spawn.Color, root, effectZ);
                Register(
                    visual,
                    GetWorldPosition(boardView, spawn.SpawnFrom),
                    GetWorldPosition(boardView, spawn.To),
                    spawn.To.X,
                    spawn.To.Y,
                    isRefillSpawn: true);
            }

            if (activeVisuals.Count == 0)
            {
                return false;
            }

            InitializeTravelDurations(boardView.CellSize);
            ApplyCurrentState();
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed = Mathf.Min(activeDuration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyCurrentState();

            if (elapsed >= activeDuration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            for (var index = activeVisuals.Count - 1; index >= 0; index--)
            {
                if (Application.isPlaying)
                {
                    Destroy(activeVisuals[index].Root);
                }
                else
                {
                    DestroyImmediate(activeVisuals[index].Root);
                }
            }

            activeVisuals.Clear();
            activeDuration = 0f;
            elapsed = 0f;
        }

        private void Register(
            GameObject root,
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            int destinationColumn,
            int destinationRow,
            bool isRefillSpawn)
        {
            activeVisuals.Add(new ActiveMotionVisual(root, startWorldPosition, endWorldPosition, destinationColumn, destinationRow, isRefillSpawn));
        }

        private void InitializeTravelDurations(float cellSize)
        {
            var maxCompletionTime = 0f;

            for (var index = 0; index < activeVisuals.Count; index++)
            {
                var visual = activeVisuals[index];
                var cellDistance = cellSize > 0f
                    ? Vector3.Distance(visual.StartWorldPosition, visual.EndWorldPosition) / cellSize
                    : 0f;
                var travelDuration = cellDistance * secondsPerCell;
                activeVisuals[index] = visual.WithTiming(cellDistance, travelDuration, startDelay: 0f);
                maxCompletionTime = Mathf.Max(maxCompletionTime, travelDuration);
            }

            activeDuration = Mathf.Max(minimumDuration, ApplyGravityLandingOrder(maxCompletionTime));
        }

        private float ApplyGravityLandingOrder(float initialMaxCompletionTime)
        {
            var indicesByColumn = new Dictionary<int, List<int>>();

            for (var index = 0; index < activeVisuals.Count; index++)
            {
                var visual = activeVisuals[index];
                if (visual.IsRefillSpawn)
                {
                    continue;
                }

                if (!indicesByColumn.TryGetValue(visual.DestinationColumn, out var indices))
                {
                    indices = new List<int>();
                    indicesByColumn.Add(visual.DestinationColumn, indices);
                }

                indices.Add(index);
            }

            var maxCompletionTime = initialMaxCompletionTime;

            foreach (var pair in indicesByColumn)
            {
                pair.Value.Sort((leftIndex, rightIndex) =>
                {
                    var left = activeVisuals[leftIndex];
                    var right = activeVisuals[rightIndex];
                    return left.DestinationRow.CompareTo(right.DestinationRow);
                });

                var previousCompletionTime = 0f;
                for (var order = 0; order < pair.Value.Count; order++)
                {
                    var visualIndex = pair.Value[order];
                    var visual = activeVisuals[visualIndex];
                    var targetCompletionTime = order == 0
                        ? visual.TravelDuration
                        : Mathf.Max(visual.TravelDuration, previousCompletionTime + columnLandingStep);

                    activeVisuals[visualIndex] = visual.WithTiming(
                        visual.CellDistance,
                        targetCompletionTime,
                        startDelay: 0f);

                    previousCompletionTime = targetCompletionTime;
                    maxCompletionTime = Mathf.Max(maxCompletionTime, targetCompletionTime);
                }
            }

            return maxCompletionTime;
        }

        private void ApplyCurrentState()
        {
            foreach (var visual in activeVisuals)
            {
                if (visual.Root is null)
                {
                    continue;
                }

                var progress = visual.TravelDuration > 0f
                    ? Mathf.Clamp01(elapsed / visual.TravelDuration)
                    : 1f;
                visual.Root.transform.position = Vector3.Lerp(
                    visual.StartWorldPosition,
                    visual.EndWorldPosition,
                    EaseInOutCubic(progress));
            }
        }

        private Vector3 GetWorldPosition(BoardView boardView, Core.BoardCoordinate coordinate)
        {
            var worldPosition = boardView.GetCellCenterWorld(coordinate);
            worldPosition.z = boardView.transform.position.z + effectZ;
            return worldPosition;
        }

        private static float EaseInOutCubic(float progress)
        {
            return progress < 0.5f
                ? 4f * progress * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 3f) / 2f;
        }

        private readonly struct ActiveMotionVisual
        {
            public ActiveMotionVisual(
                GameObject root,
                Vector3 startWorldPosition,
                Vector3 endWorldPosition,
                int destinationColumn,
                int destinationRow,
                bool isRefillSpawn,
                float cellDistance = 0f,
                float travelDuration = 0f,
                float startDelay = 0f)
            {
                Root = root;
                StartWorldPosition = startWorldPosition;
                EndWorldPosition = endWorldPosition;
                DestinationColumn = destinationColumn;
                DestinationRow = destinationRow;
                IsRefillSpawn = isRefillSpawn;
                CellDistance = cellDistance;
                TravelDuration = travelDuration;
                StartDelay = startDelay;
            }

            public GameObject Root { get; }

            public Vector3 StartWorldPosition { get; }

            public Vector3 EndWorldPosition { get; }

            public int DestinationColumn { get; }

            public int DestinationRow { get; }

            public bool IsRefillSpawn { get; }

            public float CellDistance { get; }

            public float TravelDuration { get; }

            public float StartDelay { get; }

            public ActiveMotionVisual WithTiming(float cellDistance, float travelDuration, float startDelay)
            {
                return new ActiveMotionVisual(
                    Root,
                    StartWorldPosition,
                    EndWorldPosition,
                    DestinationColumn,
                    DestinationRow,
                    IsRefillSpawn,
                    cellDistance,
                    travelDuration,
                    startDelay);
            }
        }
    }
}
