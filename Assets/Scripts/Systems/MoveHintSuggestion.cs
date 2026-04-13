using System;
using System.Collections.Generic;
using DreamBlastClone.Core;

namespace DreamBlastClone.Systems
{
    public enum MoveHintTargetType
    {
        None = 0,
        NormalGroup = 1,
        SpecialItem = 2
    }

    public sealed class MoveHintSuggestion
    {
        private static readonly IReadOnlyList<BoardCoordinate> EmptyCoordinates = Array.Empty<BoardCoordinate>();

        public MoveHintSuggestion(
            bool isValid,
            MoveHintTargetType targetType,
            BoardCoordinate tapCoordinate,
            IReadOnlyList<BoardCoordinate> highlightCoordinates)
        {
            if (isValid && targetType == MoveHintTargetType.None)
            {
                throw new ArgumentException("Valid hints must declare a concrete target type.", nameof(targetType));
            }

            IsValid = isValid;
            TargetType = targetType;
            TapCoordinate = tapCoordinate;
            HighlightCoordinates = highlightCoordinates ?? throw new ArgumentNullException(nameof(highlightCoordinates));
        }

        public bool IsValid { get; }

        public MoveHintTargetType TargetType { get; }

        public BoardCoordinate TapCoordinate { get; }

        public IReadOnlyList<BoardCoordinate> HighlightCoordinates { get; }

        public static MoveHintSuggestion Invalid()
        {
            return new MoveHintSuggestion(
                isValid: false,
                targetType: MoveHintTargetType.None,
                tapCoordinate: default,
                highlightCoordinates: EmptyCoordinates);
        }
    }
}
