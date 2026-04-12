using DreamBlastClone.Core;
using System;

namespace DreamBlastClone.Obstacles
{
    public abstract class ObstacleModel
    {
        public virtual int FootprintWidth => 1;

        public virtual int FootprintHeight => 1;

        public virtual bool FallsWithGravity => false;

        protected static void ValidatePositive(string paramName, int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value must be greater than zero.");
            }
        }

        protected static void ValidateNonNegative(string paramName, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }
        }
    }
}
