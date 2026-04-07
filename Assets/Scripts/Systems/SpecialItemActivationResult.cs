using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemActivationResult
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public SpecialItemActivationResult(
            bool isValidActivation,
            SpecialActivationType activationType,
            IReadOnlyList<BoardCoordinate> affectedCoordinates,
            IReadOnlyList<BoardCoordinate> removedItemCoordinates)
        {
            IsValidActivation = isValidActivation;
            ActivationType = activationType;
            AffectedCoordinates = affectedCoordinates ?? throw new ArgumentNullException(nameof(affectedCoordinates));
            RemovedItemCoordinates = removedItemCoordinates ?? throw new ArgumentNullException(nameof(removedItemCoordinates));
        }

        public bool IsValidActivation { get; }

        public SpecialActivationType ActivationType { get; }

        public IReadOnlyList<BoardCoordinate> AffectedCoordinates { get; }

        public IReadOnlyList<BoardCoordinate> RemovedItemCoordinates { get; }

        public static SpecialItemActivationResult Invalid()
        {
            return new SpecialItemActivationResult(
                isValidActivation: false,
                activationType: SpecialActivationType.None,
                affectedCoordinates: EmptyCoordinates,
                removedItemCoordinates: EmptyCoordinates);
        }
    }
}
