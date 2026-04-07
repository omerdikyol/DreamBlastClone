using System;

namespace DreamBlastClone.Systems
{
    public sealed class SpecialItemTapPipelineResult
    {
        public SpecialItemTapPipelineResult(
            bool isValidTap,
            SpecialItemActivationResult activation,
            ObstacleDamageResolutionResult obstacleDamage,
            ItemGravityResolutionResult gravity,
            ItemRefillResolutionResult refill)
        {
            IsValidTap = isValidTap;
            Activation = activation ?? throw new ArgumentNullException(nameof(activation));
            ObstacleDamage = obstacleDamage ?? throw new ArgumentNullException(nameof(obstacleDamage));
            Gravity = gravity ?? throw new ArgumentNullException(nameof(gravity));
            Refill = refill ?? throw new ArgumentNullException(nameof(refill));
        }

        public bool IsValidTap { get; }

        public SpecialItemActivationResult Activation { get; }

        public ObstacleDamageResolutionResult ObstacleDamage { get; }

        public ItemGravityResolutionResult Gravity { get; }

        public ItemRefillResolutionResult Refill { get; }

        public static SpecialItemTapPipelineResult Invalid()
        {
            return new SpecialItemTapPipelineResult(
                isValidTap: false,
                activation: SpecialItemActivationResult.Invalid(),
                obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                gravity: ItemGravityResolutionResult.Empty(),
                refill: ItemRefillResolutionResult.Empty());
        }
    }
}
