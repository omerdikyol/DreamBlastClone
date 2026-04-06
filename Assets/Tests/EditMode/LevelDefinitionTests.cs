using System.Linq;
using DreamBlastClone.Core;
using DreamBlastClone.Data;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelDefinitionTests
    {
        [Test]
        public void StoresTypedCellDefinitions()
        {
            var definitions = new[]
            {
                new LevelCellDefinition(new BoardCoordinate(0, 0), new CubeLevelItemDefinition(CubeColor.Red)),
                new LevelCellDefinition(new BoardCoordinate(1, 0), new RandomCubeLevelItemDefinition()),
                new LevelCellDefinition(new BoardCoordinate(2, 0), new RocketLevelItemDefinition(RocketOrientation.Vertical)),
                new LevelCellDefinition(new BoardCoordinate(0, 1), new TntLevelItemDefinition()),
                new LevelCellDefinition(new BoardCoordinate(1, 1), obstacle: new VaseLevelObstacleDefinition()),
                new LevelCellDefinition(new BoardCoordinate(2, 1), obstacle: new StoneLevelObstacleDefinition()),
                new LevelCellDefinition(new BoardCoordinate(0, 2), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                new LevelCellDefinition(new BoardCoordinate(1, 2), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                new LevelCellDefinition(new BoardCoordinate(0, 3), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                new LevelCellDefinition(new BoardCoordinate(1, 3), obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopRight)),
                new LevelCellDefinition(new BoardCoordinate(2, 3), new CubeLevelItemDefinition(CubeColor.Blue), new VaseLevelObstacleDefinition()),
                new LevelCellDefinition(new BoardCoordinate(2, 2))
            };

            var levelDefinition = new LevelDefinition(1, 3, 4, 20, definitions);

            Assert.That(levelDefinition.LevelNumber, Is.EqualTo(1));
            Assert.That(levelDefinition.GridWidth, Is.EqualTo(3));
            Assert.That(levelDefinition.GridHeight, Is.EqualTo(4));
            Assert.That(levelDefinition.MoveCount, Is.EqualTo(20));
            Assert.That(levelDefinition.CellDefinitions.Count, Is.EqualTo(definitions.Length));
            Assert.That(levelDefinition.CellDefinitions.Select(cell => cell.Item).OfType<RandomCubeLevelItemDefinition>().Count(), Is.EqualTo(1));
            Assert.That(levelDefinition.CellDefinitions.Select(cell => cell.Obstacle).OfType<ChaliceBoxPartLevelObstacleDefinition>().Select(definition => definition.Part), Is.EquivalentTo(new[]
            {
                ChaliceBoxPart.BottomLeft,
                ChaliceBoxPart.BottomRight,
                ChaliceBoxPart.TopLeft,
                ChaliceBoxPart.TopRight
            }));
            Assert.That(levelDefinition.CellDefinitions.Count(cell => cell.Item is not null && cell.Obstacle is not null), Is.EqualTo(1));
            Assert.That(levelDefinition.CellDefinitions.Count(cell => cell.Item is null && cell.Obstacle is null), Is.EqualTo(1));
        }
    }
}
