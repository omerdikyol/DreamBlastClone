using System;

namespace DreamBlastClone.Systems
{
    public sealed class NormalCubeTapPipelineResult
    {
        public NormalCubeTapPipelineResult(
            bool isValidTap,
            CubeBlastResolutionResult blast,
            ObstacleDamageResolutionResult obstacleDamage,
            ItemGravityResolutionResult gravity,
            ItemRefillResolutionResult refill)
        {
            IsValidTap = isValidTap;
            Blast = blast ?? throw new ArgumentNullException(nameof(blast));
            ObstacleDamage = obstacleDamage ?? throw new ArgumentNullException(nameof(obstacleDamage));
            Gravity = gravity ?? throw new ArgumentNullException(nameof(gravity));
            Refill = refill ?? throw new ArgumentNullException(nameof(refill));
        }

        public bool IsValidTap { get; }

        public CubeBlastResolutionResult Blast { get; }

        public ObstacleDamageResolutionResult ObstacleDamage { get; }

        public ItemGravityResolutionResult Gravity { get; }

        public ItemRefillResolutionResult Refill { get; }

        public static NormalCubeTapPipelineResult Invalid()
        {
            return new NormalCubeTapPipelineResult(
                isValidTap: false,
                blast: CubeBlastResolutionResult.Invalid(),
                obstacleDamage: ObstacleDamageResolutionResult.Empty(),
                gravity: ItemGravityResolutionResult.Empty(),
                refill: ItemRefillResolutionResult.Empty());
        }
    }
}
