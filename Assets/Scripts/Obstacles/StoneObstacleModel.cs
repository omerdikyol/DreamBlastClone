using DreamBlastClone.Core;

namespace DreamBlastClone.Obstacles
{
    public sealed class StoneObstacleModel : ObstacleModel
    {
        public StoneObstacleModel(int remainingDurability = 1)
        {
            ValidatePositive(nameof(remainingDurability), remainingDurability);
            RemainingDurability = remainingDurability;
        }

        public int RemainingDurability { get; set; }
    }
}
