using System;
using System.Collections.Generic;
using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Systems;

namespace DreamBlastClone.Views
{
    public sealed class SpecialItemComboPresentationDescriptorBuilder
    {
        public SpecialItemComboPresentationDescriptor Build(SpecialItemComboActivationResult combo)
        {
            if (combo is null)
            {
                throw new ArgumentNullException(nameof(combo));
            }

            if (!combo.IsComboActivated || combo.ComboType == SpecialItemComboType.None)
            {
                throw new InvalidOperationException("Combo presentation requires an activated combo result.");
            }

            if (combo.ParticipatingSpecialCoordinates.Count == 0)
            {
                throw new InvalidOperationException("Combo presentation requires at least one participating special coordinate.");
            }

            var comboOrigin = combo.ParticipatingSpecialCoordinates[0];
            var rocketSweeps = BuildRocketSweeps(combo.ComboType, comboOrigin, combo.AffectedCoordinates);

            return new SpecialItemComboPresentationDescriptor(
                combo.ComboType,
                comboOrigin,
                combo.ParticipatingSpecialCoordinates.ToArray(),
                combo.AffectedCoordinates.ToArray(),
                rocketSweeps);
        }

        private static IReadOnlyList<SingleRocketActivationEffectDescriptor> BuildRocketSweeps(
            SpecialItemComboType comboType,
            BoardCoordinate comboOrigin,
            IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            if (affectedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(affectedCoordinates));
            }

            return comboType switch
            {
                SpecialItemComboType.RocketRocket => BuildRocketRocketSweeps(comboOrigin, affectedCoordinates),
                SpecialItemComboType.TntRocket => BuildTntRocketSweeps(comboOrigin, affectedCoordinates),
                _ => Array.Empty<SingleRocketActivationEffectDescriptor>()
            };
        }

        private static IReadOnlyList<SingleRocketActivationEffectDescriptor> BuildRocketRocketSweeps(
            BoardCoordinate comboOrigin,
            IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            return new[]
            {
                BuildHorizontalSweep(comboOrigin.X, comboOrigin.Y, affectedCoordinates),
                BuildVerticalSweep(comboOrigin.X, comboOrigin.Y, affectedCoordinates)
            };
        }

        private static IReadOnlyList<SingleRocketActivationEffectDescriptor> BuildTntRocketSweeps(
            BoardCoordinate comboOrigin,
            IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            var sweeps = new List<SingleRocketActivationEffectDescriptor>(6);

            for (var row = comboOrigin.Y - 1; row <= comboOrigin.Y + 1; row++)
            {
                if (affectedCoordinates.Any(coordinate => coordinate.Y == row))
                {
                    sweeps.Add(BuildHorizontalSweep(comboOrigin.X, row, affectedCoordinates));
                }
            }

            for (var column = comboOrigin.X - 1; column <= comboOrigin.X + 1; column++)
            {
                if (affectedCoordinates.Any(coordinate => coordinate.X == column))
                {
                    sweeps.Add(BuildVerticalSweep(column, comboOrigin.Y, affectedCoordinates));
                }
            }

            return sweeps;
        }

        private static SingleRocketActivationEffectDescriptor BuildHorizontalSweep(
            int originX,
            int row,
            IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            var lineCoordinates = affectedCoordinates.Where(coordinate => coordinate.Y == row).ToArray();
            if (lineCoordinates.Length == 0)
            {
                throw new InvalidOperationException($"Horizontal combo sweep for row {row} requires at least one affected coordinate.");
            }

            var minX = lineCoordinates.Min(coordinate => coordinate.X);
            var maxX = lineCoordinates.Max(coordinate => coordinate.X);
            return new SingleRocketActivationEffectDescriptor(
                RocketOrientation.Horizontal,
                new BoardCoordinate(originX, row),
                new BoardCoordinate(minX, row),
                new BoardCoordinate(maxX, row));
        }

        private static SingleRocketActivationEffectDescriptor BuildVerticalSweep(
            int column,
            int originY,
            IReadOnlyList<BoardCoordinate> affectedCoordinates)
        {
            var lineCoordinates = affectedCoordinates.Where(coordinate => coordinate.X == column).ToArray();
            if (lineCoordinates.Length == 0)
            {
                throw new InvalidOperationException($"Vertical combo sweep for column {column} requires at least one affected coordinate.");
            }

            var minY = lineCoordinates.Min(coordinate => coordinate.Y);
            var maxY = lineCoordinates.Max(coordinate => coordinate.Y);
            return new SingleRocketActivationEffectDescriptor(
                RocketOrientation.Vertical,
                new BoardCoordinate(column, originY),
                new BoardCoordinate(column, minY),
                new BoardCoordinate(column, maxY));
        }
    }
}
