using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemComboActivationResult
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public SpecialItemComboActivationResult(
            bool isComboActivated,
            SpecialItemComboType comboType,
            IReadOnlyList<BoardCoordinate> participatingSpecialCoordinates,
            IReadOnlyList<BoardCoordinate> affectedCoordinates,
            IReadOnlyList<BoardCoordinate> removedItemCoordinates)
        {
            IsComboActivated = isComboActivated;
            ComboType = comboType;
            ParticipatingSpecialCoordinates = participatingSpecialCoordinates ?? throw new ArgumentNullException(nameof(participatingSpecialCoordinates));
            AffectedCoordinates = affectedCoordinates ?? throw new ArgumentNullException(nameof(affectedCoordinates));
            RemovedItemCoordinates = removedItemCoordinates ?? throw new ArgumentNullException(nameof(removedItemCoordinates));
        }

        public bool IsComboActivated { get; }

        public SpecialItemComboType ComboType { get; }

        public IReadOnlyList<BoardCoordinate> ParticipatingSpecialCoordinates { get; }

        public IReadOnlyList<BoardCoordinate> AffectedCoordinates { get; }

        public IReadOnlyList<BoardCoordinate> RemovedItemCoordinates { get; }

        public static SpecialItemComboActivationResult Invalid()
        {
            return new SpecialItemComboActivationResult(
                isComboActivated: false,
                comboType: SpecialItemComboType.None,
                participatingSpecialCoordinates: EmptyCoordinates,
                affectedCoordinates: EmptyCoordinates,
                removedItemCoordinates: EmptyCoordinates);
        }
    }
}
