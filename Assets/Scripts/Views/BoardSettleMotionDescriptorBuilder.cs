using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class BoardSettleMotionDescriptorBuilder
    {
        public BoardSettleMotionDescriptor Build(
            BoardModel preSettleBoard,
            BoardTapDispatchResult tap,
            BoardModel finalBoard)
        {
            if (preSettleBoard is null)
            {
                throw new ArgumentNullException(nameof(preSettleBoard));
            }

            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            if (finalBoard is null)
            {
                throw new ArgumentNullException(nameof(finalBoard));
            }

            if (!tap.IsValidTap)
            {
                return BoardSettleMotionDescriptor.Empty();
            }

            var gravity = GetGravityResult(tap);
            var refill = GetRefillResult(tap);
            if (!gravity.HasAnyMovement && !refill.HasAnySpawn)
            {
                return BoardSettleMotionDescriptor.Empty();
            }

            var gravityMoves = BuildGravityMoves(gravity);
            var refillSpawns = BuildRefillSpawns(preSettleBoard, refill);
            return new BoardSettleMotionDescriptor(gravityMoves, refillSpawns);
        }

        private static IReadOnlyList<ItemSettleMove> BuildGravityMoves(ItemGravityResolutionResult gravity)
        {
            var moves = new List<ItemSettleMove>(gravity.MoveCount);

            foreach (var move in gravity.Moves)
            {
                moves.Add(new ItemSettleMove(move.From, move.To));
            }

            return moves;
        }

        private static IReadOnlyList<RefillSpawnMotion> BuildRefillSpawns(
            BoardModel preSettleBoard,
            ItemRefillResolutionResult refill)
        {
            var spawnsByColumn = new Dictionary<int, List<ItemSpawn>>();

            foreach (var spawn in refill.Spawns)
            {
                if (!spawnsByColumn.TryGetValue(spawn.Coordinate.X, out var columnSpawns))
                {
                    columnSpawns = new List<ItemSpawn>();
                    spawnsByColumn.Add(spawn.Coordinate.X, columnSpawns);
                }

                columnSpawns.Add(spawn);
            }

            var refillMotions = new List<RefillSpawnMotion>(refill.SpawnCount);

            foreach (var pair in spawnsByColumn)
            {
                // The lowest destination should spawn closest to the board so refill reads as a top-down stack.
                pair.Value.Sort(static (left, right) => left.Coordinate.Y.CompareTo(right.Coordinate.Y));

                for (var index = 0; index < pair.Value.Count; index++)
                {
                    var spawn = pair.Value[index];
                    refillMotions.Add(new RefillSpawnMotion(
                        new BoardCoordinate(pair.Key, preSettleBoard.Height + index),
                        spawn.Coordinate,
                        spawn.Color));
                }
            }

            return refillMotions;
        }

        private static ItemGravityResolutionResult GetGravityResult(BoardTapDispatchResult tap)
        {
            return tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.Gravity,
                TapRouteType.SpecialItem => tap.SpecialItem.Gravity,
                _ => ItemGravityResolutionResult.Empty()
            };
        }

        private static ItemRefillResolutionResult GetRefillResult(BoardTapDispatchResult tap)
        {
            return tap.RouteType switch
            {
                TapRouteType.NormalCube => tap.NormalCube.Refill,
                TapRouteType.SpecialItem => tap.SpecialItem.Refill,
                _ => ItemRefillResolutionResult.Empty()
            };
        }
    }
}
