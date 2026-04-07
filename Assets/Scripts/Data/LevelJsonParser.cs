using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DreamBlastClone.Core;

namespace DreamBlastClone.Data
{
    public sealed class LevelJsonParser
    {
        public LevelDefinition Parse(string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var levelNumber = ReadRequiredInt(json, "level_number");
            var gridWidth = ReadRequiredInt(json, "grid_width");
            var gridHeight = ReadRequiredInt(json, "grid_height");
            var moveCount = ReadRequiredInt(json, "move_count");
            var gridSymbols = ReadRequiredGrid(json, levelNumber);
            var expectedCellCount = checked(gridWidth * gridHeight);

            if (gridSymbols.Count != expectedCellCount)
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} grid length {gridSymbols.Count} does not match declared dimensions {gridWidth}x{gridHeight}.");
            }

            var cellDefinitions = new List<LevelCellDefinition>(gridSymbols.Count);
            var chaliceParts = new Dictionary<BoardCoordinate, ChaliceBoxPart>();

            for (var index = 0; index < gridSymbols.Count; index++)
            {
                var coordinate = new BoardCoordinate(index % gridWidth, index / gridWidth);
                var definition = CreateCellDefinition(gridSymbols[index], coordinate, levelNumber);
                cellDefinitions.Add(definition);

                if (definition.Obstacle is ChaliceBoxPartLevelObstacleDefinition chalicePart)
                {
                    chaliceParts.Add(coordinate, chalicePart.Part);
                }
            }

            ValidateChaliceBoxes(chaliceParts, gridWidth, gridHeight, levelNumber);

            return new LevelDefinition(
                levelNumber,
                gridWidth,
                gridHeight,
                moveCount,
                cellDefinitions);
        }

        private static LevelCellDefinition CreateCellDefinition(string symbol, BoardCoordinate coordinate, int levelNumber)
        {
            return symbol switch
            {
                "r" => new LevelCellDefinition(coordinate, new CubeLevelItemDefinition(CubeColor.Red)),
                "g" => new LevelCellDefinition(coordinate, new CubeLevelItemDefinition(CubeColor.Green)),
                "b" => new LevelCellDefinition(coordinate, new CubeLevelItemDefinition(CubeColor.Blue)),
                "y" => new LevelCellDefinition(coordinate, new CubeLevelItemDefinition(CubeColor.Yellow)),
                "rand" => new LevelCellDefinition(coordinate, new RandomCubeLevelItemDefinition()),
                "vro" => new LevelCellDefinition(coordinate, new RocketLevelItemDefinition(RocketOrientation.Vertical)),
                "hro" => new LevelCellDefinition(coordinate, new RocketLevelItemDefinition(RocketOrientation.Horizontal)),
                "t" => new LevelCellDefinition(coordinate, new TntLevelItemDefinition()),
                "s" => new LevelCellDefinition(coordinate, obstacle: new StoneLevelObstacleDefinition()),
                "v" => new LevelCellDefinition(coordinate, obstacle: new VaseLevelObstacleDefinition()),
                "cbBL" => new LevelCellDefinition(coordinate, obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomLeft)),
                "cbBR" => new LevelCellDefinition(coordinate, obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.BottomRight)),
                "cbTL" => new LevelCellDefinition(coordinate, obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopLeft)),
                "cbTR" => new LevelCellDefinition(coordinate, obstacle: new ChaliceBoxPartLevelObstacleDefinition(ChaliceBoxPart.TopRight)),
                _ => throw new InvalidOperationException($"Level {levelNumber} contains unsupported grid symbol '{symbol}' at {coordinate}.")
            };
        }

        private static void ValidateChaliceBoxes(
            IReadOnlyDictionary<BoardCoordinate, ChaliceBoxPart> chaliceParts,
            int gridWidth,
            int gridHeight,
            int levelNumber)
        {
            var partsByAnchor = new Dictionary<BoardCoordinate, Dictionary<ChaliceBoxPart, BoardCoordinate>>();

            foreach (var partEntry in chaliceParts)
            {
                var anchor = GetAnchor(partEntry.Key, partEntry.Value);

                if (!partsByAnchor.TryGetValue(anchor, out var anchoredParts))
                {
                    anchoredParts = new Dictionary<ChaliceBoxPart, BoardCoordinate>();
                    partsByAnchor.Add(anchor, anchoredParts);
                }

                anchoredParts.Add(partEntry.Value, partEntry.Key);
            }

            foreach (var chaliceEntry in partsByAnchor)
            {
                var anchor = chaliceEntry.Key;
                var parts = chaliceEntry.Value;
                var expectedCoordinates = new Dictionary<ChaliceBoxPart, BoardCoordinate>
                {
                    { ChaliceBoxPart.BottomLeft, anchor },
                    { ChaliceBoxPart.BottomRight, anchor.Offset(1, 0) },
                    { ChaliceBoxPart.TopLeft, anchor.Offset(0, 1) },
                    { ChaliceBoxPart.TopRight, anchor.Offset(1, 1) }
                };

                if (parts.Count != expectedCoordinates.Count)
                {
                    throw new InvalidOperationException($"Level {levelNumber} contains an incomplete chalice box anchored at {anchor}.");
                }

                foreach (var expectedPart in expectedCoordinates)
                {
                    if (!parts.TryGetValue(expectedPart.Key, out var actualCoordinate) || actualCoordinate != expectedPart.Value)
                    {
                        throw new InvalidOperationException($"Level {levelNumber} contains a malformed chalice box anchored at {anchor}.");
                    }

                    if (actualCoordinate.X < 0
                        || actualCoordinate.X >= gridWidth
                        || actualCoordinate.Y < 0
                        || actualCoordinate.Y >= gridHeight)
                    {
                        throw new InvalidOperationException($"Level {levelNumber} contains a chalice box that extends beyond the board bounds.");
                    }
                }
            }
        }

        private static BoardCoordinate GetAnchor(BoardCoordinate coordinate, ChaliceBoxPart part)
        {
            return part switch
            {
                ChaliceBoxPart.BottomLeft => coordinate,
                ChaliceBoxPart.BottomRight => coordinate.Offset(-1, 0),
                ChaliceBoxPart.TopLeft => coordinate.Offset(0, -1),
                ChaliceBoxPart.TopRight => coordinate.Offset(-1, -1),
                _ => throw new InvalidOperationException($"Unsupported chalice box part '{part}'.")
            };
        }

        private static int ReadRequiredInt(string json, string propertyName)
        {
            var match = Regex.Match(json, $"\"{propertyName}\"\\s*:\\s*(\\d+)");

            if (!match.Success)
            {
                throw new InvalidOperationException($"Level JSON is missing required integer property '{propertyName}'.");
            }

            return int.Parse(match.Groups[1].Value);
        }

        private static IReadOnlyList<string> ReadRequiredGrid(string json, int levelNumber)
        {
            var gridMatch = Regex.Match(json, "\"grid\"\\s*:\\s*\\[(.*)\\]", RegexOptions.Singleline);

            if (!gridMatch.Success)
            {
                throw new InvalidOperationException("Level JSON must contain a grid array.");
            }

            var gridContent = gridMatch.Groups[1].Value;
            var symbolMatches = Regex.Matches(gridContent, "\"([^\"]*)\"");
            var symbols = new List<string>(symbolMatches.Count);

            foreach (Match symbolMatch in symbolMatches)
            {
                symbols.Add(symbolMatch.Groups[1].Value);
            }

            if (symbols.Count == 0 && gridContent.Trim().Length > 0)
            {
                throw new InvalidOperationException($"Level {levelNumber} contains an invalid grid array.");
            }

            return symbols;
        }
    }
}
