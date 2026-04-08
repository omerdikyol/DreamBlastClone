using System;
using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class SingleRocketActivationEffectDescriptorBuilder
    {
        public SingleRocketActivationEffectDescriptor Build(BoardCoordinate tapCoordinate, SpecialItemActivationResult activation)
        {
            if (activation is null)
            {
                throw new ArgumentNullException(nameof(activation));
            }

            if (!activation.IsValidActivation || activation.ActivationType != SpecialActivationType.Rocket)
            {
                throw new InvalidOperationException("Single rocket effects require a valid rocket activation result.");
            }

            if (activation.AffectedCoordinates.Count == 0)
            {
                throw new InvalidOperationException("Rocket activation must affect at least one coordinate.");
            }

            var allSameY = activation.AffectedCoordinates.All(coordinate => coordinate.Y == tapCoordinate.Y);
            var allSameX = activation.AffectedCoordinates.All(coordinate => coordinate.X == tapCoordinate.X);

            if (allSameY == allSameX)
            {
                throw new InvalidOperationException("Rocket activation footprint must be a single row or a single column.");
            }

            if (allSameY)
            {
                var minX = activation.AffectedCoordinates.Min(coordinate => coordinate.X);
                var maxX = activation.AffectedCoordinates.Max(coordinate => coordinate.X);
                return new SingleRocketActivationEffectDescriptor(
                    RocketOrientation.Horizontal,
                    tapCoordinate,
                    new BoardCoordinate(minX, tapCoordinate.Y),
                    new BoardCoordinate(maxX, tapCoordinate.Y));
            }

            var minY = activation.AffectedCoordinates.Min(coordinate => coordinate.Y);
            var maxY = activation.AffectedCoordinates.Max(coordinate => coordinate.Y);
            return new SingleRocketActivationEffectDescriptor(
                RocketOrientation.Vertical,
                tapCoordinate,
                new BoardCoordinate(tapCoordinate.X, minY),
                new BoardCoordinate(tapCoordinate.X, maxY));
        }
    }
}
