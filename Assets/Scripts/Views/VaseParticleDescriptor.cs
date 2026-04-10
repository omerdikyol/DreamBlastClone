using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class VaseParticleDescriptor
    {
        private static readonly IReadOnlyList<VaseParticleEvent> EmptyEvents = Array.Empty<VaseParticleEvent>();

        public VaseParticleDescriptor(IReadOnlyList<VaseParticleEvent> events)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IReadOnlyList<VaseParticleEvent> Events { get; }

        public bool HasAnyParticles => Events.Count > 0;

        public static VaseParticleDescriptor Empty()
        {
            return new VaseParticleDescriptor(EmptyEvents);
        }
    }

    public readonly struct VaseParticleEvent
    {
        public VaseParticleEvent(BoardCoordinate coordinate, bool isRemoval)
        {
            Coordinate = coordinate;
            IsRemoval = isRemoval;
        }

        public BoardCoordinate Coordinate { get; }

        public bool IsRemoval { get; }
    }
}
