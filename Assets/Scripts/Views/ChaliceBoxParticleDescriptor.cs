using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public enum ChaliceBoxParticleEventType
    {
        DoorDamage,
        DoorBreak,
        ChaliceDamage,
        ChaliceComplete
    }

    public readonly struct ChaliceBoxParticleEvent
    {
        public ChaliceBoxParticleEvent(BoardCoordinate anchor, ChaliceBoxParticleEventType eventType, int amount, int hitStep = 0)
        {
            Anchor = anchor;
            EventType = eventType;
            Amount = amount;
            HitStep = hitStep;
        }

        public BoardCoordinate Anchor { get; }

        public ChaliceBoxParticleEventType EventType { get; }

        public int Amount { get; }

        public int HitStep { get; }
    }

    public sealed class ChaliceBoxParticleDescriptor
    {
        private static readonly IReadOnlyList<ChaliceBoxParticleEvent> EmptyEvents = Array.Empty<ChaliceBoxParticleEvent>();

        public ChaliceBoxParticleDescriptor(IReadOnlyList<ChaliceBoxParticleEvent> events)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IReadOnlyList<ChaliceBoxParticleEvent> Events { get; }

        public bool HasAnyParticles => Events.Count > 0;

        public static ChaliceBoxParticleDescriptor Empty()
        {
            return new ChaliceBoxParticleDescriptor(EmptyEvents);
        }
    }
}
