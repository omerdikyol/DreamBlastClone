using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;

namespace DreamBlastClone.Systems
{
    public sealed class ItemGravityResolver
    {
        public ItemGravityResolutionResult Resolve(BoardModel board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var initialPositions = new Dictionary<ItemModel, BoardCoordinate>();
            var initialObstaclePositions = new Dictionary<ObstacleModel, BoardCoordinate>();
            foreach (var cell in board.GetAllCells())
            {
                if (cell.HasItem)
                {
                    initialPositions[cell.Item] = cell.Coordinate;
                }

                if (cell.HasObstacle
                    && cell.Obstacle.FallsWithGravity
                    && !initialObstaclePositions.ContainsKey(cell.Obstacle))
                {
                    initialObstaclePositions[cell.Obstacle] = cell.Coordinate;
                }
            }

            var movedAnyItem = false;
            var movedAnyObstacle = false;
            var movedThisPass = true;

            while (movedThisPass)
            {
                movedThisPass = false;
                movedThisPass |= ApplyObstacleGravityStep(board);
                movedAnyObstacle |= movedThisPass;

                var movedItemThisPass = ApplyItemGravityStep(board);
                movedThisPass |= movedItemThisPass;
                movedAnyItem |= movedItemThisPass;
            }

            if (!movedAnyItem && !movedAnyObstacle)
            {
                return ItemGravityResolutionResult.Empty();
            }

            var moves = new List<ItemFallMove>();
            foreach (var pair in initialPositions)
            {
                var finalCoordinate = FindItemCoordinate(board, pair.Key);
                if (finalCoordinate == pair.Value)
                {
                    continue;
                }

                moves.Add(new ItemFallMove(pair.Value, finalCoordinate));
            }

            var obstacleMoves = new List<ObstacleFallMove>();
            foreach (var pair in initialObstaclePositions)
            {
                var finalCoordinate = FindObstacleCoordinate(board, pair.Key);
                if (finalCoordinate == pair.Value)
                {
                    continue;
                }

                obstacleMoves.Add(new ObstacleFallMove(pair.Value, finalCoordinate));
            }

            moves.Sort(static (left, right) =>
            {
                var xComparison = left.From.X.CompareTo(right.From.X);
                return xComparison != 0
                    ? xComparison
                    : left.From.Y.CompareTo(right.From.Y);
            });

            obstacleMoves.Sort(static (left, right) =>
            {
                var xComparison = left.From.X.CompareTo(right.From.X);
                return xComparison != 0
                    ? xComparison
                    : left.From.Y.CompareTo(right.From.Y);
            });

            return moves.Count == 0 && obstacleMoves.Count == 0
                ? ItemGravityResolutionResult.Empty()
                : new ItemGravityResolutionResult(moves, obstacleMoves);
        }

        private static bool ApplyObstacleGravityStep(BoardModel board)
        {
            var movedAnyObstacle = false;

            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    var sourceCoordinate = new BoardCoordinate(x, y);
                    var sourceCell = board.GetCell(sourceCoordinate);
                    if (!sourceCell.HasObstacle
                        || !sourceCell.Obstacle.FallsWithGravity
                        || !TryResolveObstacleStepDestination(board, sourceCoordinate, out var destinationCoordinate))
                    {
                        continue;
                    }

                    var obstacle = sourceCell.Obstacle;
                    board.ClearObstacle(sourceCoordinate);
                    board.PlaceObstacle(destinationCoordinate, obstacle);
                    movedAnyObstacle = true;
                }
            }

            return movedAnyObstacle;
        }

        private static bool ApplyItemGravityStep(BoardModel board)
        {
            var movedAnyItem = false;

            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    var sourceCoordinate = new BoardCoordinate(x, y);
                    var sourceCell = board.GetCell(sourceCoordinate);
                    if (!sourceCell.HasItem || !TryResolveStepDestination(board, sourceCoordinate, out var destinationCoordinate))
                    {
                        continue;
                    }

                    var item = sourceCell.Item;
                    board.ClearItem(sourceCoordinate);
                    board.PlaceItem(destinationCoordinate, item);
                    movedAnyItem = true;
                }
            }

            return movedAnyItem;
        }

        private static bool TryResolveStepDestination(BoardModel board, BoardCoordinate sourceCoordinate, out BoardCoordinate destinationCoordinate)
        {
            var down = sourceCoordinate.Offset(0, -1);
            if (IsOpenLandingCell(board, down))
            {
                destinationCoordinate = down;
                return true;
            }

            var downLeft = sourceCoordinate.Offset(-1, -1);
            if (CanSlipAroundRigidBlocker(board, sourceCoordinate, horizontalDirection: -1, downLeft))
            {
                destinationCoordinate = downLeft;
                return true;
            }

            var downRight = sourceCoordinate.Offset(1, -1);
            if (CanSlipAroundRigidBlocker(board, sourceCoordinate, horizontalDirection: 1, downRight))
            {
                destinationCoordinate = downRight;
                return true;
            }

            destinationCoordinate = default;
            return false;
        }

        private static bool TryResolveObstacleStepDestination(
            BoardModel board,
            BoardCoordinate sourceCoordinate,
            out BoardCoordinate destinationCoordinate)
        {
            var down = sourceCoordinate.Offset(0, -1);
            if (IsOpenLandingCell(board, down))
            {
                destinationCoordinate = down;
                return true;
            }

            destinationCoordinate = default;
            return false;
        }

        private static bool CanSlipAroundRigidBlocker(
            BoardModel board,
            BoardCoordinate sourceCoordinate,
            int horizontalDirection,
            BoardCoordinate targetCoordinate)
        {
            if (!IsOpenLandingCell(board, targetCoordinate))
            {
                return false;
            }

            if (HasVerticalItemClaimAbove(board, sourceCoordinate, targetCoordinate))
            {
                return false;
            }

            var belowCoordinate = sourceCoordinate.Offset(0, -1);
            if (ContainsRigidBlocker(board, belowCoordinate))
            {
                return true;
            }

            var sideCoordinate = sourceCoordinate.Offset(horizontalDirection, 0);
            return ContainsRigidBlocker(board, sideCoordinate);
        }

        private static bool HasVerticalItemClaimAbove(
            BoardModel board,
            BoardCoordinate sourceCoordinate,
            BoardCoordinate targetCoordinate)
        {
            for (var y = targetCoordinate.Y + 1; y < board.Height; y++)
            {
                var coordinate = new BoardCoordinate(targetCoordinate.X, y);
                if (coordinate == sourceCoordinate)
                {
                    continue;
                }

                var cell = board.GetCell(coordinate);
                if (cell.HasObstacle)
                {
                    return false;
                }

                if (cell.HasItem)
                {
                    return HasOpenVerticalPath(board, coordinate, targetCoordinate);
                }
            }

            return false;
        }

        private static bool HasOpenVerticalPath(BoardModel board, BoardCoordinate sourceCoordinate, BoardCoordinate targetCoordinate)
        {
            for (var y = sourceCoordinate.Y - 1; y >= targetCoordinate.Y; y--)
            {
                if (!IsOpenLandingCell(board, new BoardCoordinate(sourceCoordinate.X, y)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsOpenLandingCell(BoardModel board, BoardCoordinate coordinate)
        {
            return board.TryGetCell(coordinate, out var cell)
                && !cell.HasItem
                && !cell.HasObstacle;
        }

        internal static bool CanRefillStreamMoveBetweenEmptyCells(
            BoardModel board,
            BoardCoordinate sourceCoordinate,
            BoardCoordinate targetCoordinate)
        {
            if (!IsOpenLandingCell(board, sourceCoordinate)
                || !IsOpenLandingCell(board, targetCoordinate))
            {
                return false;
            }

            if (targetCoordinate == sourceCoordinate.Offset(0, -1))
            {
                return true;
            }

            if (targetCoordinate == sourceCoordinate.Offset(-1, -1))
            {
                return CanSlipAroundRigidBlocker(board, sourceCoordinate, horizontalDirection: -1, targetCoordinate);
            }

            if (targetCoordinate == sourceCoordinate.Offset(1, -1))
            {
                return CanSlipAroundRigidBlocker(board, sourceCoordinate, horizontalDirection: 1, targetCoordinate);
            }

            return false;
        }

        private static bool ContainsRigidBlocker(BoardModel board, BoardCoordinate coordinate)
        {
            return board.TryGetCell(coordinate, out var cell)
                && cell.HasObstacle
                && IsRigidBlocker(cell.Obstacle);
        }

        private static bool IsRigidBlocker(ObstacleModel obstacle)
        {
            return obstacle is StoneObstacleModel or ChaliceBoxObstacleModel;
        }

        private static BoardCoordinate FindItemCoordinate(BoardModel board, ItemModel item)
        {
            foreach (var cell in board.GetAllCells())
            {
                if (ReferenceEquals(cell.Item, item))
                {
                    return cell.Coordinate;
                }
            }

            throw new InvalidOperationException("Gravity lost track of an item while resolving settle.");
        }

        private static BoardCoordinate FindObstacleCoordinate(BoardModel board, ObstacleModel obstacle)
        {
            foreach (var cell in board.GetAllCells())
            {
                if (ReferenceEquals(cell.Obstacle, obstacle))
                {
                    return cell.Coordinate;
                }
            }

            throw new InvalidOperationException("Gravity lost track of a falling obstacle while resolving settle.");
        }
    }
}
