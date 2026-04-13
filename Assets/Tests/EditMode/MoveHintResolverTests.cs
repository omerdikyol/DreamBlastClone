using DreamBlastClone.Core;
using DreamBlastClone.Data;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class MoveHintResolverTests
    {
        private readonly MoveHintResolver resolver = new MoveHintResolver();

        [Test]
        public void ResolvePrefersTntCreatingGroupOverOtherValidMoves()
        {
            var board = new BoardModel(6, 4);
            PlaceGroup(board, CubeColor.Red, new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1)
            });

            PlaceGroup(board, CubeColor.Green, new[]
            {
                new BoardCoordinate(4, 0),
                new BoardCoordinate(5, 0),
                new BoardCoordinate(4, 1),
                new BoardCoordinate(5, 1)
            });

            var suggestion = resolver.Resolve(board, goals: null);

            Assert.That(suggestion.IsValid, Is.True);
            Assert.That(suggestion.TargetType, Is.EqualTo(MoveHintTargetType.NormalGroup));
            Assert.That(suggestion.TapCoordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(suggestion.HighlightCoordinates, Is.EquivalentTo(new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1)
            }));
        }

        [Test]
        public void ResolvePrefersRocketCreationOverGoalDamageAndLargerStandardGroup()
        {
            var board = new BoardModel(6, 4);
            PlaceGroup(board, CubeColor.Green, new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1)
            });

            PlaceGroup(board, CubeColor.Blue, new[]
            {
                new BoardCoordinate(3, 0),
                new BoardCoordinate(4, 0),
                new BoardCoordinate(5, 0),
                new BoardCoordinate(4, 1),
                new BoardCoordinate(4, 2)
            });

            board.PlaceItem(new BoardCoordinate(2, 2), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 3), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(1, 2), new StoneObstacleModel());

            var goals = new[] { new LevelGoalDefinition(LevelGoalType.Stone, 1) };

            var suggestion = resolver.Resolve(board, goals);

            Assert.That(suggestion.IsValid, Is.True);
            Assert.That(suggestion.TargetType, Is.EqualTo(MoveHintTargetType.NormalGroup));
            Assert.That(suggestion.TapCoordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
        }

        [Test]
        public void ResolvePrefersGoalHelpingSpecialMoveOverLargerStandardGroup()
        {
            var board = new BoardModel(6, 4);
            board.PlaceItem(new BoardCoordinate(0, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceObstacle(new BoardCoordinate(4, 0), new StoneObstacleModel());

            PlaceGroup(board, CubeColor.Blue, new[]
            {
                new BoardCoordinate(1, 2),
                new BoardCoordinate(2, 2),
                new BoardCoordinate(3, 2),
                new BoardCoordinate(2, 1),
                new BoardCoordinate(2, 3)
            });

            var goals = new[] { new LevelGoalDefinition(LevelGoalType.Stone, 1) };

            var suggestion = resolver.Resolve(board, goals);

            Assert.That(suggestion.IsValid, Is.True);
            Assert.That(suggestion.TargetType, Is.EqualTo(MoveHintTargetType.SpecialItem));
            Assert.That(suggestion.TapCoordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(suggestion.HighlightCoordinates, Is.EqualTo(new[] { new BoardCoordinate(0, 0) }));
        }

        [Test]
        public void ResolveTopMovesReturnsBestThreeMovesInPriorityOrder()
        {
            var board = new BoardModel(7, 5);

            PlaceGroup(board, CubeColor.Red, new[]
            {
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 0),
                new BoardCoordinate(2, 0),
                new BoardCoordinate(0, 1),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 1)
            });

            PlaceGroup(board, CubeColor.Green, new[]
            {
                new BoardCoordinate(4, 0),
                new BoardCoordinate(5, 0),
                new BoardCoordinate(4, 1),
                new BoardCoordinate(5, 1)
            });

            PlaceGroup(board, CubeColor.Blue, new[]
            {
                new BoardCoordinate(0, 3),
                new BoardCoordinate(1, 3),
                new BoardCoordinate(2, 3)
            });

            board.PlaceItem(new BoardCoordinate(6, 4), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(6, 3), new CubeItemModel(CubeColor.Yellow));

            var suggestions = resolver.ResolveTopMoves(board, goals: null, maxCount: 3);

            Assert.That(suggestions, Has.Count.EqualTo(3));
            Assert.That(suggestions[0].TapCoordinate, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(suggestions[1].TapCoordinate, Is.EqualTo(new BoardCoordinate(4, 0)));
            Assert.That(suggestions[2].TapCoordinate, Is.EqualTo(new BoardCoordinate(0, 3)));
        }

        private static void PlaceGroup(BoardModel board, CubeColor color, BoardCoordinate[] coordinates)
        {
            foreach (var coordinate in coordinates)
            {
                board.PlaceItem(coordinate, new CubeItemModel(color));
            }
        }
    }
}
