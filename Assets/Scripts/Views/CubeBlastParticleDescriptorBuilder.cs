using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class CubeBlastParticleDescriptorBuilder
    {
        public CubeBlastParticleDescriptor Build(NormalCubeTapPipelineResult tap)
        {
            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            if (!tap.IsValidTap || !tap.Blast.IsValidBlast || !tap.Blast.BlastedCubeColor.HasValue)
            {
                return null;
            }

            var createdSpecialCoordinate = tap.Blast.CreatedSpecialCoordinate;
            var burstCoordinates = new List<BoardCoordinate>();

            foreach (var coordinate in tap.Blast.RemovedCoordinates)
            {
                if (createdSpecialCoordinate.HasValue && coordinate == createdSpecialCoordinate.Value)
                {
                    continue;
                }

                burstCoordinates.Add(coordinate);
            }

            return burstCoordinates.Count == 0
                ? null
                : new CubeBlastParticleDescriptor(tap.Blast.BlastedCubeColor.Value, burstCoordinates);
        }

        public CubeBlastParticleDescriptor Build(BoardModel preTapBoard, BoardTapDispatchResult tap)
        {
            if (preTapBoard is null)
            {
                throw new ArgumentNullException(nameof(preTapBoard));
            }

            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            if (!tap.IsValidTap)
            {
                return null;
            }

            var removedCoordinates = new HashSet<BoardCoordinate>();
            switch (tap.RouteType)
            {
                case TapRouteType.NormalCube:
                    var normalDescriptor = Build(tap.NormalCube);
                    return normalDescriptor;
                case TapRouteType.SpecialItem:
                    CollectRemovedSpecialItemCoordinates(tap.SpecialItem, removedCoordinates);
                    break;
                default:
                    return null;
            }

            if (removedCoordinates.Count == 0)
            {
                return null;
            }

            var groupedCoordinates = new Dictionary<CubeColor, List<BoardCoordinate>>();
            foreach (var coordinate in removedCoordinates)
            {
                if (!preTapBoard.TryGetCell(coordinate, out var cell) || cell.Item is not CubeItemModel cube)
                {
                    continue;
                }

                if (!groupedCoordinates.TryGetValue(cube.Color, out var coordinates))
                {
                    coordinates = new List<BoardCoordinate>();
                    groupedCoordinates.Add(cube.Color, coordinates);
                }

                coordinates.Add(coordinate);
            }

            if (groupedCoordinates.Count == 0)
            {
                return null;
            }

            var burstGroups = new List<CubeBlastBurstGroup>(groupedCoordinates.Count);
            foreach (var pair in groupedCoordinates)
            {
                burstGroups.Add(new CubeBlastBurstGroup(pair.Key, pair.Value));
            }

            return new CubeBlastParticleDescriptor(burstGroups);
        }

        private static void CollectRemovedSpecialItemCoordinates(
            SpecialItemTapPipelineResult tap,
            ISet<BoardCoordinate> removedCoordinates)
        {
            if (!tap.IsValidTap)
            {
                return;
            }

            AddCoordinates(removedCoordinates, tap.Activation.RemovedItemCoordinates);
            AddCoordinates(removedCoordinates, tap.Combo.RemovedItemCoordinates);
            for (var index = 0; index < tap.TriggeredActivations.Count; index++)
            {
                AddCoordinates(removedCoordinates, tap.TriggeredActivations[index].Activation.RemovedItemCoordinates);
            }
        }

        private static void AddCoordinates(ISet<BoardCoordinate> target, IReadOnlyList<BoardCoordinate> coordinates)
        {
            for (var index = 0; index < coordinates.Count; index++)
            {
                target.Add(coordinates[index]);
            }
        }
    }
}
