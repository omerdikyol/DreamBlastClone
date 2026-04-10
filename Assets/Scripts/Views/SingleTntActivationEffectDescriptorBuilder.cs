using System;
using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class SingleTntActivationEffectDescriptorBuilder
    {
        public SingleTntActivationEffectDescriptor Build(BoardCoordinate tapCoordinate, SpecialItemActivationResult activation)
        {
            if (activation is null)
            {
                throw new ArgumentNullException(nameof(activation));
            }

            if (!activation.IsValidActivation || activation.ActivationType != SpecialActivationType.Tnt)
            {
                throw new InvalidOperationException("Single TNT effects require a valid TNT activation result.");
            }

            if (activation.AffectedCoordinates.Count == 0)
            {
                throw new InvalidOperationException("TNT activation must affect at least one coordinate.");
            }

            return new SingleTntActivationEffectDescriptor(
                tapCoordinate,
                activation.AffectedCoordinates,
                activation.AffectedCoordinates.Min(coordinate => coordinate.X),
                activation.AffectedCoordinates.Min(coordinate => coordinate.Y),
                activation.AffectedCoordinates.Max(coordinate => coordinate.X),
                activation.AffectedCoordinates.Max(coordinate => coordinate.Y));
        }
    }
}
