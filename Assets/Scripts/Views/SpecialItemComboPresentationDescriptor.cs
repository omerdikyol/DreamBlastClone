using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class SpecialItemComboPresentationDescriptor
    {
        public SpecialItemComboPresentationDescriptor(
            SpecialItemComboType comboType,
            BoardCoordinate comboOrigin,
            IReadOnlyList<BoardCoordinate> participatingSpecialCoordinates,
            IReadOnlyList<BoardCoordinate> flashCoordinates,
            IReadOnlyList<SingleRocketActivationEffectDescriptor> rocketSweeps)
        {
            ComboType = comboType;
            ComboOrigin = comboOrigin;
            ParticipatingSpecialCoordinates = participatingSpecialCoordinates ?? throw new ArgumentNullException(nameof(participatingSpecialCoordinates));
            FlashCoordinates = flashCoordinates ?? throw new ArgumentNullException(nameof(flashCoordinates));
            RocketSweeps = rocketSweeps ?? throw new ArgumentNullException(nameof(rocketSweeps));
        }

        public SpecialItemComboType ComboType { get; }

        public BoardCoordinate ComboOrigin { get; }

        public IReadOnlyList<BoardCoordinate> ParticipatingSpecialCoordinates { get; }

        public IReadOnlyList<BoardCoordinate> FlashCoordinates { get; }

        public IReadOnlyList<SingleRocketActivationEffectDescriptor> RocketSweeps { get; }

        public bool HasTntPulse => ComboType is SpecialItemComboType.TntRocket or SpecialItemComboType.TntTnt;
    }
}
