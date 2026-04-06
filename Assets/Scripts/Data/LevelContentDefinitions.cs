using DreamBlastClone.Core;

namespace DreamBlastClone.Data
{
    public abstract class LevelItemDefinition
    {
    }

    public abstract class LevelObstacleDefinition
    {
    }

    public sealed class CubeLevelItemDefinition : LevelItemDefinition
    {
        public CubeLevelItemDefinition(CubeColor color)
        {
            Color = color;
        }

        public CubeColor Color { get; }
    }

    public sealed class RandomCubeLevelItemDefinition : LevelItemDefinition
    {
    }

    public sealed class RocketLevelItemDefinition : LevelItemDefinition
    {
        public RocketLevelItemDefinition(RocketOrientation orientation)
        {
            Orientation = orientation;
        }

        public RocketOrientation Orientation { get; }
    }

    public sealed class TntLevelItemDefinition : LevelItemDefinition
    {
    }

    public sealed class VaseLevelObstacleDefinition : LevelObstacleDefinition
    {
    }

    public sealed class StoneLevelObstacleDefinition : LevelObstacleDefinition
    {
    }

    public sealed class ChaliceBoxPartLevelObstacleDefinition : LevelObstacleDefinition
    {
        public ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart part)
        {
            Part = part;
        }

        public ChaliceBoxPart Part { get; }
    }
}
