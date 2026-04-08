using System;
using System.Collections.Generic;
using System.Text;
using DreamBlastClone.Core;
using DreamBlastClone.Controllers;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamBlastClone.Controllers.Unity
{
    public sealed class BoardInputSessionBridge : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private LevelSessionHost sessionHost;
        [SerializeField] private Camera inputCamera;
        [SerializeField] private float normalCubePreviewDuration = 0.12f;
        [SerializeField] private BoardDestructionFeedbackPlayer destructionFeedbackPlayer;
        [SerializeField] private BoardSettleMotionPlayer settleMotionPlayer;
        [SerializeField] private SingleRocketActivationEffectPlayer singleRocketEffectPlayer;

        private readonly CubeGroupDetector cubeGroupDetector = new CubeGroupDetector();
        private readonly BoardModelCloner boardModelCloner = new BoardModelCloner();
        private readonly NormalCubeTapPreviewBoardBuilder previewBoardBuilder = new NormalCubeTapPreviewBoardBuilder();
        private readonly SpecialItemTapPreviewBoardBuilder specialItemPreviewBoardBuilder = new SpecialItemTapPreviewBoardBuilder();
        private readonly BoardDestructionFeedbackDescriptorBuilder destructionFeedbackDescriptorBuilder = new BoardDestructionFeedbackDescriptorBuilder();
        private readonly BoardSettleMotionDescriptorBuilder settleMotionDescriptorBuilder = new BoardSettleMotionDescriptorBuilder();
        private readonly BoardSettleStartBoardBuilder settleStartBoardBuilder = new BoardSettleStartBoardBuilder();
        private readonly SingleRocketActivationEffectDescriptorBuilder singleRocketEffectDescriptorBuilder = new SingleRocketActivationEffectDescriptorBuilder();
        private static readonly bool EnableTapDebugLogging = false;
        private const float CubeTapSnapRadiusFactor = 1.1f;
        private const float CubeTapStickinessFactor = 0.35f;
        private BoardModel pendingFinalBoard;
        private BoardModel pendingSettleStartBoard;
        private BoardSettleMotionDescriptor pendingSettleMotionDescriptor;
        private float remainingPreviewSeconds;
        private bool isInputSuppressed;

        public event Action<LevelSessionTapResult> TapProcessed;

        public bool IsInputSuppressed => isInputSuppressed;

        private void Start()
        {
            RenderCurrentBoard();
        }

        private void Update()
        {
            AdvancePendingPreview(Time.unscaledDeltaTime);

            if (IsPreviewActive())
            {
                return;
            }

            if (isInputSuppressed)
            {
                return;
            }

            if (TryReadScreenTap(out var screenPosition))
            {
                TryHandleScreenTap(screenPosition);
            }
        }

        public void SetInputSuppressed(bool isSuppressed)
        {
            isInputSuppressed = isSuppressed;
        }

        public bool TryHandleScreenTap(Vector2 screenPosition)
        {
            if (!TryResolveDependencies(out var session))
            {
                return false;
            }

            if (IsPreviewActive() || isInputSuppressed)
            {
                return false;
            }

            var board = session.Board;
            var worldPoint = ScreenToBoardWorldPoint(screenPosition);

            if (!boardView.TryWorldToBoardCoordinate(board, worldPoint, out var coordinate))
            {
                return false;
            }

            var resolvedCoordinate = ResolveTapCoordinate(board, worldPoint, coordinate);
            var preTapBoard = boardModelCloner.Clone(board);
            LogTappedCubeGroupSize(board, coordinate, resolvedCoordinate);
            var tapResult = session.ProcessTap(resolvedCoordinate);
            LogTapResolution(coordinate, resolvedCoordinate, tapResult);
            TapProcessed?.Invoke(tapResult);
            RenderTapOutcome(preTapBoard, session.Board, resolvedCoordinate, tapResult);
            return true;
        }

        private bool TryResolveDependencies(out LevelSession session)
        {
            session = null;

            if (boardView is null || sessionHost is null || inputCamera is null)
            {
                return false;
            }

            session = sessionHost.Session;
            return session is not null;
        }

        private void RenderCurrentBoard()
        {
            if (boardView is null || sessionHost?.Session is null)
            {
                return;
            }

            ClearPendingPreview();
            boardView.Render(sessionHost.Session.Board);
        }

        private void RenderTapOutcome(
            BoardModel preTapBoard,
            BoardModel finalBoard,
            BoardCoordinate resolvedCoordinate,
            LevelSessionTapResult tapResult)
        {
            ClearPendingPreview();

            var previewBoard = BuildPreviewBoard(preTapBoard, tapResult.Tap);
            if (previewBoard is null)
            {
                boardView.Render(finalBoard);
                return;
            }

            var settleMotionDescriptor = settleMotionDescriptorBuilder.Build(previewBoard, tapResult.Tap, finalBoard);
            var settleStartBoard = settleMotionDescriptor.HasAnyMotion
                ? settleStartBoardBuilder.Build(previewBoard, tapResult.Tap)
                : null;

            boardView.Render(previewBoard);

            var presentationDuration = 0f;
            if (TryStartDestructionFeedback(preTapBoard, tapResult.Tap, out var destructionDuration))
            {
                presentationDuration = Math.Max(presentationDuration, destructionDuration);
            }

            if (TryStartSingleRocketEffect(resolvedCoordinate, tapResult.Tap, out var rocketDuration))
            {
                presentationDuration = Math.Max(presentationDuration, rocketDuration);
            }

            pendingFinalBoard = finalBoard;
            pendingSettleStartBoard = settleStartBoard;
            pendingSettleMotionDescriptor = settleMotionDescriptor;

            if (presentationDuration <= 0f)
            {
                if (TryStartPendingSettleMotion())
                {
                    return;
                }

                if (normalCubePreviewDuration <= 0f)
                {
                    boardView.Render(finalBoard);
                    ClearPendingPreview();
                    return;
                }

                presentationDuration = normalCubePreviewDuration;
            }

            remainingPreviewSeconds = presentationDuration;
        }

        private void AdvancePendingPreview(float deltaTime)
        {
            if (destructionFeedbackPlayer is not null)
            {
                destructionFeedbackPlayer.Advance(deltaTime);
            }

            if (settleMotionPlayer is not null)
            {
                settleMotionPlayer.Advance(deltaTime);
            }

            if (singleRocketEffectPlayer is not null)
            {
                singleRocketEffectPlayer.Advance(deltaTime);
            }

            if (!IsPreviewActive())
            {
                return;
            }

            remainingPreviewSeconds = Math.Max(0f, remainingPreviewSeconds - deltaTime);
            if (remainingPreviewSeconds > 0f || pendingFinalBoard is null || boardView is null)
            {
                return;
            }

            if (TryStartPendingSettleMotion())
            {
                return;
            }

            boardView.Render(pendingFinalBoard);
            ClearPendingPreview();
        }

        private bool IsPreviewActive()
        {
            return remainingPreviewSeconds > 0f && pendingFinalBoard is not null;
        }

        private void ClearPendingPreview()
        {
            destructionFeedbackPlayer?.Stop();
            settleMotionPlayer?.Stop();
            singleRocketEffectPlayer?.Stop();
            pendingFinalBoard = null;
            pendingSettleStartBoard = null;
            pendingSettleMotionDescriptor = null;
            remainingPreviewSeconds = 0f;
        }

        private bool TryStartPendingSettleMotion()
        {
            if (pendingFinalBoard is null
                || pendingSettleStartBoard is null
                || pendingSettleMotionDescriptor is null
                || !pendingSettleMotionDescriptor.HasAnyMotion
                || settleMotionPlayer is null
                || boardView is null)
            {
                return false;
            }

            boardView.Render(pendingSettleStartBoard);

            if (!settleMotionPlayer.TryPlay(boardView, pendingFinalBoard, pendingSettleMotionDescriptor))
            {
                pendingSettleStartBoard = null;
                pendingSettleMotionDescriptor = null;
                return false;
            }

            pendingSettleStartBoard = null;
            pendingSettleMotionDescriptor = null;
            remainingPreviewSeconds = settleMotionPlayer.Duration;
            return true;
        }

        private BoardModel BuildPreviewBoard(BoardModel preTapBoard, BoardTapDispatchResult tap)
        {
            if (!tap.IsValidTap)
            {
                return null;
            }

            return tap.RouteType switch
            {
                TapRouteType.NormalCube when tap.NormalCube.IsValidTap => previewBoardBuilder.Build(preTapBoard, tap.NormalCube),
                TapRouteType.SpecialItem when tap.SpecialItem.IsValidTap => specialItemPreviewBoardBuilder.Build(preTapBoard, tap.SpecialItem),
                _ => null
            };
        }

        private bool TryStartDestructionFeedback(BoardModel preTapBoard, BoardTapDispatchResult tap, out float duration)
        {
            duration = 0f;

            if (boardView is null || destructionFeedbackPlayer is null)
            {
                return false;
            }

            var descriptor = destructionFeedbackDescriptorBuilder.Build(preTapBoard, tap);
            if (!destructionFeedbackPlayer.TryPlay(boardView, preTapBoard, descriptor))
            {
                return false;
            }

            duration = destructionFeedbackPlayer.Duration;
            return true;
        }

        private bool TryStartSingleRocketEffect(BoardCoordinate resolvedCoordinate, BoardTapDispatchResult tap, out float duration)
        {
            duration = 0f;

            if (boardView is null
                || singleRocketEffectPlayer is null
                || tap.RouteType != TapRouteType.SpecialItem)
            {
                return false;
            }

            var specialTap = tap.SpecialItem;
            if (!specialTap.IsValidTap
                || specialTap.Combo.IsComboActivated
                || !specialTap.Activation.IsValidActivation
                || specialTap.Activation.ActivationType != SpecialActivationType.Rocket)
            {
                return false;
            }

            var effectDescriptor = singleRocketEffectDescriptorBuilder.Build(
                resolvedCoordinate,
                specialTap.Activation);

            if (!singleRocketEffectPlayer.TryPlay(boardView, effectDescriptor))
            {
                return false;
            }

            duration = singleRocketEffectPlayer.Duration;
            return true;
        }

        private Vector3 ScreenToBoardWorldPoint(Vector2 screenPosition)
        {
            var boardPlaneDistance = boardView.transform.position.z - inputCamera.transform.position.z;
            return inputCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, boardPlaneDistance));
        }

        private BoardCoordinate ResolveTapCoordinate(BoardModel board, Vector3 worldPoint, BoardCoordinate initialCoordinate)
        {
            if (!board.TryGetCell(initialCoordinate, out var initialCell))
            {
                return initialCoordinate;
            }

            if (initialCell.Item is not null && initialCell.Item is not CubeItemModel)
            {
                return initialCoordinate;
            }

            var initialGroupCount = cubeGroupDetector.FindGroup(board, initialCoordinate).Count;
            if (initialGroupCount >= 2)
            {
                return initialCoordinate;
            }

            var initialDistanceSquared = GetCellCenterDistanceSquared(initialCoordinate, worldPoint);
            var stickyRadiusSquared = boardView.CellSize * CubeTapStickinessFactor;
            stickyRadiusSquared *= stickyRadiusSquared;
            if (initialDistanceSquared <= stickyRadiusSquared)
            {
                return initialCoordinate;
            }

            var bestCoordinate = initialCoordinate;
            var bestGroupCount = 1;
            var bestDistanceSquared = float.MaxValue;
            var snapRadiusSquared = boardView.CellSize * CubeTapSnapRadiusFactor;
            snapRadiusSquared *= snapRadiusSquared;

            foreach (var candidate in GetTapCandidateCoordinates(initialCoordinate))
            {
                if (!board.TryGetCell(candidate, out var candidateCell) || candidateCell.Item is not CubeItemModel)
                {
                    continue;
                }

                var centerDistanceSquared = (boardView.GetCellCenterWorld(candidate) - worldPoint).sqrMagnitude;
                if (centerDistanceSquared > snapRadiusSquared)
                {
                    continue;
                }

                var groupCount = cubeGroupDetector.FindGroup(board, candidate).Count;
                if (groupCount < 2)
                {
                    continue;
                }

                if (groupCount > bestGroupCount
                    || (groupCount == bestGroupCount && centerDistanceSquared < bestDistanceSquared)
                    || (groupCount == bestGroupCount && Math.Abs(centerDistanceSquared - bestDistanceSquared) < 0.0001f && candidate == initialCoordinate))
                {
                    bestCoordinate = candidate;
                    bestGroupCount = groupCount;
                    bestDistanceSquared = centerDistanceSquared;
                }
            }

            return bestCoordinate;
        }

        private float GetCellCenterDistanceSquared(BoardCoordinate coordinate, Vector3 worldPoint)
        {
            return (boardView.GetCellCenterWorld(coordinate) - worldPoint).sqrMagnitude;
        }

        private static IEnumerable<BoardCoordinate> GetTapCandidateCoordinates(BoardCoordinate center)
        {
            for (var deltaY = -1; deltaY <= 1; deltaY++)
            {
                for (var deltaX = -1; deltaX <= 1; deltaX++)
                {
                    yield return center.Offset(deltaX, deltaY);
                }
            }
        }

        private void LogTappedCubeGroupSize(BoardModel board, BoardCoordinate coordinate, BoardCoordinate resolvedCoordinate)
        {
            if (!EnableTapDebugLogging)
            {
                return;
            }

            if (!board.TryGetCell(resolvedCoordinate, out var cell) || cell.Item is not CubeItemModel)
            {
                return;
            }

            var group = cubeGroupDetector.FindGroup(board, resolvedCoordinate);
            Debug.Log($"Tapped cube group size: {group.Count} at {resolvedCoordinate} (raw hit: {coordinate})");

            if (group.Count <= 1)
            {
                Debug.Log(BuildCubeNeighborDebugMessage(board, coordinate, resolvedCoordinate, (CubeItemModel)cell.Item, group));
            }
        }

        private static void LogTapResolution(BoardCoordinate rawCoordinate, BoardCoordinate resolvedCoordinate, LevelSessionTapResult tapResult)
        {
            if (!EnableTapDebugLogging)
            {
                return;
            }

            if (tapResult.Tap.RouteType != TapRouteType.NormalCube)
            {
                Debug.Log($"Tap at {rawCoordinate} resolved to {resolvedCoordinate} as {tapResult.Tap.RouteType} (valid: {tapResult.Tap.IsValidTap}, spent move: {tapResult.DidSpendMove})");
                return;
            }

            var normalTap = tapResult.Tap.NormalCube;
            var createdSpecial = normalTap.Blast.CreatedSpecialItem?.GetType().Name ?? "None";
            Debug.Log(
                $"Normal cube tap at {rawCoordinate} resolved to {resolvedCoordinate}: valid={normalTap.IsValidTap}, " +
                $"group={normalTap.Blast.BlastedGroupSize}, removed={normalTap.Blast.RemovedCoordinates.Count}, " +
                $"special={createdSpecial}, obstacleDamage={normalTap.ObstacleDamage.DamageCount}, " +
                $"obstacleRemovals={normalTap.ObstacleDamage.RemovedCoordinates.Count}, gravityMoves={normalTap.Gravity.MoveCount}, refillSpawns={normalTap.Refill.SpawnCount}");
        }

        private static bool TryReadScreenTap(out Vector2 screenPosition)
        {
            if (Mouse.current is not null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current is not null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private static string BuildCubeNeighborDebugMessage(
            BoardModel board,
            BoardCoordinate rawCoordinate,
            BoardCoordinate resolvedCoordinate,
            CubeItemModel cube,
            CubeGroupDetectionResult group)
        {
            var builder = new StringBuilder();
            builder.Append("Cube debug raw=").Append(rawCoordinate)
                .Append(" resolved=").Append(resolvedCoordinate)
                .Append(" color=").Append(cube.Color)
                .Append(" groupCoords=[").Append(JoinCoordinates(group.Coordinates)).Append(']')
                .Append(" orthogonal=[");

            var firstNeighbor = true;
            foreach (var neighbor in resolvedCoordinate.GetOrthogonalNeighbors())
            {
                if (!firstNeighbor)
                {
                    builder.Append("; ");
                }

                firstNeighbor = false;
                builder.Append(neighbor).Append('=').Append(DescribeNeighbor(board, cube.Color, neighbor));
            }

            builder.Append("] area3x3=[");

            var firstAreaCell = true;
            for (var deltaY = 1; deltaY >= -1; deltaY--)
            {
                for (var deltaX = -1; deltaX <= 1; deltaX++)
                {
                    if (!firstAreaCell)
                    {
                        builder.Append("; ");
                    }

                    firstAreaCell = false;
                    var coordinate = resolvedCoordinate.Offset(deltaX, deltaY);
                    builder.Append(coordinate).Append('=').Append(DescribeItem(board, coordinate));
                }
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string JoinCoordinates(IReadOnlyList<BoardCoordinate> coordinates)
        {
            if (coordinates.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            for (var index = 0; index < coordinates.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(coordinates[index]);
            }

            return builder.ToString();
        }

        private static string DescribeNeighbor(BoardModel board, CubeColor color, BoardCoordinate coordinate)
        {
            if (!board.TryGetCell(coordinate, out var cell))
            {
                return "out";
            }

            if (cell.Item is CubeItemModel cube)
            {
                return cube.Color == color
                    ? $"cube:{cube.Color}:same"
                    : $"cube:{cube.Color}:diff";
            }

            return DescribeItem(board, coordinate);
        }

        private static string DescribeItem(BoardModel board, BoardCoordinate coordinate)
        {
            if (!board.TryGetCell(coordinate, out var cell))
            {
                return "out";
            }

            return cell.Item switch
            {
                CubeItemModel cube => $"cube:{cube.Color}",
                RocketItemModel rocket => $"rocket:{rocket.Orientation}",
                TntItemModel => "tnt",
                null => "empty",
                _ => cell.Item.GetType().Name
            };
        }
    }
}
