using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using DreamBlastClone.Grid;
using UnityEngine;

namespace DreamBlastClone.Views
{
    public sealed class BoardSettleMotionPlayer : MonoBehaviour
    {
        [SerializeField] private Transform effectRoot;
        [FormerlySerializedAs("secondsPerCell")]
        [SerializeField] private float shortFallDurationSeconds = 0.11f;
        [SerializeField] private float longFallDurationSeconds = 0.28f;
        [SerializeField] private float distanceForLongFallSeconds = 7f;
        [SerializeField] private float minimumDuration = 0.12f;
        [FormerlySerializedAs("columnLandingStep")]
        [SerializeField] private float gravityCascadeDelayStep = 0.03f;
        [SerializeField] private float shortLandingDurationSeconds = 0.085f;
        [SerializeField] private float longLandingDurationSeconds = 0.12f;
        [SerializeField] private float landingDipCells = 0.12f;
        [SerializeField] private float landingReboundCells = 0.12f;
        [SerializeField] private float landingScaleX = 1.12f;
        [SerializeField] private float landingScaleY = 0.88f;
        [SerializeField] private float reboundScaleX = 0.91f;
        [SerializeField] private float reboundScaleY = 1.1f;
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

            if (!descriptor.HasAnyMotion
                || shortFallDurationSeconds <= 0f
                || longFallDurationSeconds <= 0f
                || shortLandingDurationSeconds < 0f
                || longLandingDurationSeconds < 0f)
            {
                return false;
            }

            Stop();
            elapsed = 0f;

            var root = effectRoot is not null ? effectRoot : transform;

            foreach (var move in descriptor.GravityMoves)
            {
                var visual = boardView.CreateTransientItemVisual(finalBoard, move.To, root, effectZ);
                boardView.ApplyBoardClipMask(finalBoard, visual);
                Register(
                    visual,
                    GetWorldPosition(boardView, move.From),
                    GetWorldPosition(boardView, move.To),
                    move.To.X,
                    move.To.Y,
                    isRefillSpawn: false);
            }

            foreach (var move in descriptor.ObstacleMoves)
            {
                var visual = boardView.CreateTransientObstacleVisual(
                    finalBoard,
                    new[] { move.To },
                    root,
                    effectZ);
                boardView.ApplyBoardClipMask(finalBoard, visual);
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
                boardView.ApplyBoardClipMask(finalBoard, visual);
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
            activeVisuals.Add(new ActiveMotionVisual(
                root,
                startWorldPosition,
                endWorldPosition,
                destinationColumn,
                destinationRow,
                isRefillSpawn,
                root.transform.localScale));
        }

        private void InitializeTravelDurations(float cellSize)
        {
            var maxCompletionTime = 0f;
            var dipWorld = Mathf.Max(0f, landingDipCells * Mathf.Max(0f, cellSize));
            var reboundWorld = Mathf.Max(0f, landingReboundCells * Mathf.Max(0f, cellSize));

            for (var index = 0; index < activeVisuals.Count; index++)
            {
                var visual = activeVisuals[index];
                var cellDistance = cellSize > 0f
                    ? Vector3.Distance(visual.StartWorldPosition, visual.EndWorldPosition) / cellSize
                    : 0f;
                var durationProgress = GetDistanceTimingProgress(cellDistance);
                var travelDuration = Mathf.Lerp(shortFallDurationSeconds, longFallDurationSeconds, durationProgress);
                var landingDuration = Mathf.Lerp(shortLandingDurationSeconds, longLandingDurationSeconds, durationProgress);
                var landingStartWorldPosition = visual.EndWorldPosition + Vector3.down * dipWorld;
                var reboundWorldPosition = visual.EndWorldPosition + Vector3.up * reboundWorld;

                activeVisuals[index] = visual.WithTiming(
                    cellDistance,
                    travelDuration,
                    0f,
                    landingDuration,
                    landingStartWorldPosition,
                    reboundWorldPosition);

                maxCompletionTime = Mathf.Max(maxCompletionTime, travelDuration + landingDuration);
            }

            activeDuration = Mathf.Max(minimumDuration, ApplyGravityCascadeDelays(maxCompletionTime));
        }

        private float ApplyGravityCascadeDelays(float initialMaxCompletionTime)
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
                    var baseCompletionTime = visual.TravelDuration + visual.LandingDuration;
                    var targetCompletionTime = order == 0
                        ? baseCompletionTime
                        : Mathf.Max(baseCompletionTime, previousCompletionTime + gravityCascadeDelayStep);
                    var startDelay = Mathf.Max(0f, targetCompletionTime - baseCompletionTime);

                    activeVisuals[visualIndex] = visual.WithTiming(
                        visual.CellDistance,
                        visual.TravelDuration,
                        startDelay,
                        visual.LandingDuration,
                        visual.LandingStartWorldPosition,
                        visual.LandingReboundWorldPosition);

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

                var target = visual.Root.transform;
                var localElapsed = Mathf.Max(0f, elapsed - visual.StartDelay);
                if (localElapsed <= 0f)
                {
                    target.position = visual.StartWorldPosition;
                    target.localScale = visual.BaseLocalScale;
                    continue;
                }

                if (localElapsed < visual.TravelDuration)
                {
                    var travelProgress = visual.TravelDuration > 0f
                        ? Mathf.Clamp01(localElapsed / visual.TravelDuration)
                        : 1f;
                    target.position = Vector3.Lerp(
                        visual.StartWorldPosition,
                        visual.LandingStartWorldPosition,
                        EaseInQuad(travelProgress));
                    target.localScale = visual.BaseLocalScale;
                    continue;
                }

                var landingElapsed = Mathf.Min(visual.LandingDuration, localElapsed - visual.TravelDuration);
                var landingProgress = visual.LandingDuration > 0f
                    ? Mathf.Clamp01(landingElapsed / visual.LandingDuration)
                    : 1f;

                if (landingProgress < 0.5f)
                {
                    var reboundProgress = EaseOutCubic(Mathf.Clamp01(landingProgress / 0.5f));
                    target.position = Vector3.Lerp(
                        visual.LandingStartWorldPosition,
                        visual.LandingReboundWorldPosition,
                        reboundProgress);
                    target.localScale = Vector3.Lerp(
                        GetLandingScale(visual.BaseLocalScale),
                        GetReboundScale(visual.BaseLocalScale),
                        reboundProgress);
                    continue;
                }

                var settleProgress = EaseOutCubic(Mathf.Clamp01((landingProgress - 0.5f) / 0.5f));
                target.position = Vector3.Lerp(
                    visual.LandingReboundWorldPosition,
                    visual.EndWorldPosition,
                    settleProgress);
                target.localScale = Vector3.Lerp(
                    GetReboundScale(visual.BaseLocalScale),
                    visual.BaseLocalScale,
                    settleProgress);
            }
        }

        private Vector3 GetWorldPosition(BoardView boardView, Core.BoardCoordinate coordinate)
        {
            var worldPosition = boardView.GetCellCenterWorld(coordinate);
            worldPosition.z = boardView.transform.position.z + effectZ;
            return worldPosition;
        }

        private float GetDistanceTimingProgress(float cellDistance)
        {
            if (distanceForLongFallSeconds <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((cellDistance - 1f) / distanceForLongFallSeconds);
        }

        private Vector3 GetLandingScale(Vector3 baseLocalScale)
        {
            return new Vector3(
                baseLocalScale.x * landingScaleX,
                baseLocalScale.y * landingScaleY,
                baseLocalScale.z);
        }

        private Vector3 GetReboundScale(Vector3 baseLocalScale)
        {
            return new Vector3(
                baseLocalScale.x * reboundScaleX,
                baseLocalScale.y * reboundScaleY,
                baseLocalScale.z);
        }

        private static float EaseInQuad(float progress)
        {
            return progress * progress;
        }

        private static float EaseOutCubic(float progress)
        {
            return 1f - Mathf.Pow(1f - progress, 3f);
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
                Vector3 baseLocalScale,
                float cellDistance = 0f,
                float travelDuration = 0f,
                float startDelay = 0f,
                float landingDuration = 0f,
                Vector3 landingStartWorldPosition = default,
                Vector3 landingReboundWorldPosition = default)
            {
                Root = root;
                StartWorldPosition = startWorldPosition;
                EndWorldPosition = endWorldPosition;
                DestinationColumn = destinationColumn;
                DestinationRow = destinationRow;
                IsRefillSpawn = isRefillSpawn;
                BaseLocalScale = baseLocalScale;
                CellDistance = cellDistance;
                TravelDuration = travelDuration;
                StartDelay = startDelay;
                LandingDuration = landingDuration;
                LandingStartWorldPosition = landingStartWorldPosition == default ? endWorldPosition : landingStartWorldPosition;
                LandingReboundWorldPosition = landingReboundWorldPosition == default ? endWorldPosition : landingReboundWorldPosition;
            }

            public GameObject Root { get; }

            public Vector3 StartWorldPosition { get; }

            public Vector3 EndWorldPosition { get; }

            public int DestinationColumn { get; }

            public int DestinationRow { get; }

            public bool IsRefillSpawn { get; }

            public Vector3 BaseLocalScale { get; }

            public float CellDistance { get; }

            public float TravelDuration { get; }

            public float StartDelay { get; }

            public float LandingDuration { get; }

            public Vector3 LandingStartWorldPosition { get; }

            public Vector3 LandingReboundWorldPosition { get; }

            public ActiveMotionVisual WithTiming(
                float cellDistance,
                float travelDuration,
                float startDelay,
                float landingDuration,
                Vector3 landingStartWorldPosition,
                Vector3 landingReboundWorldPosition)
            {
                return new ActiveMotionVisual(
                    Root,
                    StartWorldPosition,
                    EndWorldPosition,
                    DestinationColumn,
                    DestinationRow,
                    IsRefillSpawn,
                    BaseLocalScale,
                    cellDistance,
                    travelDuration,
                    startDelay,
                    landingDuration,
                    landingStartWorldPosition,
                    landingReboundWorldPosition);
            }
        }
    }
}
