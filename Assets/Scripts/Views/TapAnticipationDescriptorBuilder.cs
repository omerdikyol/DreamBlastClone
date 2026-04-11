using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class TapAnticipationDescriptorBuilder
    {
        public TapAnticipationDescriptor Build(BoardCoordinate resolvedCoordinate, BoardTapDispatchResult tap)
        {
            if (tap is null)
            {
                throw new ArgumentNullException(nameof(tap));
            }

            if (!tap.IsValidTap)
            {
                return new TapAnticipationDescriptor(new[] { new TapAnticipationEvent(resolvedCoordinate, TapAnticipationEventType.Invalid) });
            }

            return tap.RouteType switch
            {
                TapRouteType.NormalCube => BuildNormalCube(tap.NormalCube),
                TapRouteType.SpecialItem => BuildSpecialItem(resolvedCoordinate, tap.SpecialItem),
                _ => TapAnticipationDescriptor.Empty()
            };
        }

        private static TapAnticipationDescriptor BuildNormalCube(NormalCubeTapPipelineResult tap)
        {
            if (!tap.IsValidTap || !tap.Blast.IsValidBlast)
            {
                return TapAnticipationDescriptor.Empty();
            }

            var events = new List<TapAnticipationEvent>();
            var createdSpecialCoordinate = tap.Blast.CreatedSpecialCoordinate;

            foreach (var coordinate in tap.Blast.RemovedCoordinates)
            {
                if (createdSpecialCoordinate.HasValue && coordinate == createdSpecialCoordinate.Value)
                {
                    continue;
                }

                events.Add(new TapAnticipationEvent(coordinate, TapAnticipationEventType.CubeGroup));
            }

            return events.Count == 0
                ? TapAnticipationDescriptor.Empty()
                : new TapAnticipationDescriptor(events);
        }

        private static TapAnticipationDescriptor BuildSpecialItem(BoardCoordinate resolvedCoordinate, SpecialItemTapPipelineResult tap)
        {
            if (!tap.IsValidTap || tap.Combo.IsComboActivated || !tap.Activation.IsValidActivation)
            {
                return TapAnticipationDescriptor.Empty();
            }

            var eventType = tap.Activation.ActivationType switch
            {
                SpecialActivationType.Rocket => TapAnticipationEventType.Rocket,
                SpecialActivationType.Tnt => TapAnticipationEventType.Tnt,
                _ => (TapAnticipationEventType?)null
            };

            return eventType.HasValue
                ? new TapAnticipationDescriptor(new[] { new TapAnticipationEvent(resolvedCoordinate, eventType.Value) })
                : TapAnticipationDescriptor.Empty();
        }
    }
}
