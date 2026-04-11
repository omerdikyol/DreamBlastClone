using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public enum TapAnticipationEventType
    {
        Invalid,
        CubeGroup,
        Rocket,
        Tnt
    }

    public readonly struct TapAnticipationEvent
    {
        public TapAnticipationEvent(BoardCoordinate coordinate, TapAnticipationEventType eventType)
        {
            Coordinate = coordinate;
            EventType = eventType;
        }

        public BoardCoordinate Coordinate { get; }

        public TapAnticipationEventType EventType { get; }
    }

    public sealed class TapAnticipationDescriptor
    {
        private static readonly TapAnticipationDescriptor EmptyDescriptor = new TapAnticipationDescriptor(Array.Empty<TapAnticipationEvent>());

        public TapAnticipationDescriptor(IReadOnlyList<TapAnticipationEvent> events)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IReadOnlyList<TapAnticipationEvent> Events { get; }

        public bool HasAnyFeedback => Events.Count > 0;

        public static TapAnticipationDescriptor Empty()
        {
            return EmptyDescriptor;
        }
    }
}
