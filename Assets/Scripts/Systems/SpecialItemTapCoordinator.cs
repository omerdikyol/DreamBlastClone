using System;
using System.Collections.Generic;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemTapCoordinator
    {
        private readonly SpecialItemComboResolver specialItemComboResolver = new SpecialItemComboResolver();
        private readonly SpecialComboObstacleDamageResolver specialComboObstacleDamageResolver = new SpecialComboObstacleDamageResolver();
        private readonly SpecialItemTapResolver specialItemTapResolver = new SpecialItemTapResolver();
        private readonly SpecialActivationObstacleDamageResolver specialActivationObstacleDamageResolver = new SpecialActivationObstacleDamageResolver();
        private readonly ItemGravityResolver itemGravityResolver = new ItemGravityResolver();
        private readonly ItemRefillResolver itemRefillResolver = new ItemRefillResolver();

        public SpecialItemTapPipelineResult Resolve(BoardModel board, BoardCoordinate tapCoordinate, IRefillCubeColorResolver refillColorResolver)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (refillColorResolver is null)
            {
                throw new ArgumentNullException(nameof(refillColorResolver));
            }

            var triggeredActivations = new List<TriggeredSpecialActivationResult>();
            var obstacleDamageResults = new List<ObstacleDamageResolutionResult>();
            var pendingTriggeredSeeds = new Queue<TriggeredSpecialSeed>();
            var queuedTriggeredCoordinates = new HashSet<BoardCoordinate>();

            var comboStep = specialItemComboResolver.ResolveStep(board, tapCoordinate);
            if (comboStep.Combo.IsComboActivated)
            {
                obstacleDamageResults.Add(specialComboObstacleDamageResolver.Resolve(board, comboStep.Combo));
                EnqueueTriggeredSpecials(comboStep.TriggeredSpecials, pendingTriggeredSeeds, queuedTriggeredCoordinates);
                ResolveTriggeredSpecialActivations(board, pendingTriggeredSeeds, queuedTriggeredCoordinates, triggeredActivations, obstacleDamageResults);

                var comboGravity = itemGravityResolver.Resolve(board);
                var comboRefill = itemRefillResolver.Resolve(board, refillColorResolver);

                return new SpecialItemTapPipelineResult(
                    isValidTap: true,
                    combo: comboStep.Combo,
                    activation: SpecialItemActivationResult.Invalid(),
                    obstacleDamage: CombineObstacleDamageResults(obstacleDamageResults),
                    gravity: comboGravity,
                    refill: comboRefill,
                    triggeredActivations: triggeredActivations);
            }

            var activationStep = specialItemTapResolver.ResolveStep(board, tapCoordinate);
            if (!activationStep.Activation.IsValidActivation)
            {
                return SpecialItemTapPipelineResult.Invalid();
            }

            obstacleDamageResults.Add(specialActivationObstacleDamageResolver.Resolve(board, activationStep.Activation));
            EnqueueTriggeredSpecials(activationStep.TriggeredSpecials, pendingTriggeredSeeds, queuedTriggeredCoordinates);
            ResolveTriggeredSpecialActivations(board, pendingTriggeredSeeds, queuedTriggeredCoordinates, triggeredActivations, obstacleDamageResults);
            var gravity = itemGravityResolver.Resolve(board);
            var refill = itemRefillResolver.Resolve(board, refillColorResolver);

            return new SpecialItemTapPipelineResult(
                isValidTap: true,
                combo: SpecialItemComboActivationResult.Invalid(),
                activation: activationStep.Activation,
                obstacleDamage: CombineObstacleDamageResults(obstacleDamageResults),
                gravity: gravity,
                refill: refill,
                triggeredActivations: triggeredActivations);
        }

        private void ResolveTriggeredSpecialActivations(
            BoardModel board,
            Queue<TriggeredSpecialSeed> pendingTriggeredSeeds,
            HashSet<BoardCoordinate> queuedTriggeredCoordinates,
            List<TriggeredSpecialActivationResult> triggeredActivations,
            List<ObstacleDamageResolutionResult> obstacleDamageResults)
        {
            while (pendingTriggeredSeeds.Count > 0)
            {
                var triggeredSeed = pendingTriggeredSeeds.Dequeue();
                var activationStep = specialItemTapResolver.ResolveStep(board, triggeredSeed.Coordinate, triggeredSeed.Item);
                if (!activationStep.Activation.IsValidActivation)
                {
                    continue;
                }

                triggeredActivations.Add(new TriggeredSpecialActivationResult(triggeredSeed.Coordinate, activationStep.Activation));
                obstacleDamageResults.Add(specialActivationObstacleDamageResolver.Resolve(board, activationStep.Activation));
                EnqueueTriggeredSpecials(activationStep.TriggeredSpecials, pendingTriggeredSeeds, queuedTriggeredCoordinates);
            }
        }

        private static void EnqueueTriggeredSpecials(
            IReadOnlyList<TriggeredSpecialSeed> triggeredSpecials,
            Queue<TriggeredSpecialSeed> pendingTriggeredSeeds,
            HashSet<BoardCoordinate> queuedTriggeredCoordinates)
        {
            foreach (var triggeredSpecial in triggeredSpecials)
            {
                if (!queuedTriggeredCoordinates.Add(triggeredSpecial.Coordinate))
                {
                    continue;
                }

                pendingTriggeredSeeds.Enqueue(triggeredSpecial);
            }
        }

        private static ObstacleDamageResolutionResult CombineObstacleDamageResults(
            IReadOnlyList<ObstacleDamageResolutionResult> obstacleDamageResults)
        {
            if (obstacleDamageResults.Count == 0)
            {
                return ObstacleDamageResolutionResult.Empty();
            }

            var damages = new List<ObstacleDamage>();
            var removedCoordinates = new List<BoardCoordinate>();
            var seenRemovedCoordinates = new HashSet<BoardCoordinate>();

            foreach (var obstacleDamageResult in obstacleDamageResults)
            {
                foreach (var damage in obstacleDamageResult.Damages)
                {
                    damages.Add(damage);
                }

                foreach (var removedCoordinate in obstacleDamageResult.RemovedCoordinates)
                {
                    if (seenRemovedCoordinates.Add(removedCoordinate))
                    {
                        removedCoordinates.Add(removedCoordinate);
                    }
                }
            }

            return damages.Count == 0
                ? ObstacleDamageResolutionResult.Empty()
                : new ObstacleDamageResolutionResult(damages, removedCoordinates);
        }
    }
}
