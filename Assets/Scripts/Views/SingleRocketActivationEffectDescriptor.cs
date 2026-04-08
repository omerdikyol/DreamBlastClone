using DreamBlastClone.Core;

namespace DreamBlastClone.Views
{
    public sealed class SingleRocketActivationEffectDescriptor
    {
        public SingleRocketActivationEffectDescriptor(
            RocketOrientation orientation,
            BoardCoordinate origin,
            BoardCoordinate negativeEnd,
            BoardCoordinate positiveEnd)
        {
            Orientation = orientation;
            Origin = origin;
            NegativeEnd = negativeEnd;
            PositiveEnd = positiveEnd;
        }

        public RocketOrientation Orientation { get; }

        public BoardCoordinate Origin { get; }

        public BoardCoordinate NegativeEnd { get; }

        public BoardCoordinate PositiveEnd { get; }
    }
}
