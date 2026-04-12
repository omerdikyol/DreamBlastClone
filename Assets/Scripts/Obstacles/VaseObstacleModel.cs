using DreamBlastClone.Core;

namespace DreamBlastClone.Obstacles
{
    public sealed class VaseObstacleModel : ObstacleModel
    {
        public override bool FallsWithGravity => true;

        public VaseObstacleModel(int remainingDurability = 2)
        {
            ValidatePositive(nameof(remainingDurability), remainingDurability);
            RemainingDurability = remainingDurability;
        }

        public int RemainingDurability { get; set; }
    }
}
