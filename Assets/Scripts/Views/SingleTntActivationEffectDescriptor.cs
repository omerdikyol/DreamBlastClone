using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class SingleTntActivationEffectDescriptor
    {
        public SingleTntActivationEffectDescriptor(
            BoardCoordinate origin,
            IReadOnlyList<BoardCoordinate> affectedCoordinates,
            int minX,
            int minY,
            int maxX,
            int maxY)
        {
            Origin = origin;
            AffectedCoordinates = affectedCoordinates ?? throw new ArgumentNullException(nameof(affectedCoordinates));
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public BoardCoordinate Origin { get; }

        public IReadOnlyList<BoardCoordinate> AffectedCoordinates { get; }

        public int MinX { get; }

        public int MinY { get; }

        public int MaxX { get; }

        public int MaxY { get; }
    }
}
