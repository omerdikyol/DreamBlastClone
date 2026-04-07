using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class BoardViewTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();
        private readonly List<Sprite> createdSprites = new List<Sprite>();
        private readonly List<Texture2D> createdTextures = new List<Texture2D>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdSprites.Count - 1; index >= 0; index--)
            {
                if (createdSprites[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdSprites[index]);
                }
            }

            createdSprites.Clear();

            for (var index = createdTextures.Count - 1; index >= 0; index--)
            {
                if (createdTextures[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdTextures[index]);
                }
            }

            createdTextures.Clear();

            for (var index = createdGameObjects.Count - 1; index >= 0; index--)
            {
                var gameObject = createdGameObjects[index];

                if (gameObject is not null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }

            createdGameObjects.Clear();
        }

        [Test]
        public void RenderNullThrows()
        {
            var boardView = CreateConfiguredBoardView();

            Assert.That(() => boardView.Render(null), Throws.ArgumentNullException);
        }

        [Test]
        public void RenderCreatesItemAndObstacleVisualsFromBoardProjection()
        {
            var board = new BoardModel(3, 3);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(2, 1), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceObstacle(new BoardCoordinate(1, 0), new VaseObstacleModel());
            board.PlaceObstacle(new BoardCoordinate(2, 2), new StoneObstacleModel());

            var cubeBeforeRender = board.GetCell(new BoardCoordinate(0, 0)).Item;
            var vaseBeforeRender = board.GetCell(new BoardCoordinate(1, 0)).Obstacle;

            boardView.Render(board);

            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(2));
            Assert.That(GetObstacleRoot(boardView).childCount, Is.EqualTo(2));
            Assert.That(board.GetCell(new BoardCoordinate(0, 0)).Item, Is.SameAs(cubeBeforeRender));
            Assert.That(board.GetCell(new BoardCoordinate(1, 0)).Obstacle, Is.SameAs(vaseBeforeRender));
        }

        [Test]
        public void RenderDedupesChaliceBoxAndUsesTwoByTwoFootprint()
        {
            var board = new BoardModel(4, 4);
            var boardView = CreateConfiguredBoardView(origin: new Vector2(1f, 2f), cellSize: 2f);
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(1, 1), remainingDoorDurability: 2);

            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            boardView.Render(board);

            Assert.That(GetObstacleRoot(boardView).childCount, Is.EqualTo(1));

            var obstacleVisual = GetObstacleRoot(boardView).GetChild(0);
            Assert.That(obstacleVisual.name, Does.StartWith("ChaliceBoxPrefab"));
            Assert.That(obstacleVisual.localPosition, Is.EqualTo(new Vector3(5f, 6f, 0f)));
            Assert.That(obstacleVisual.localScale, Is.EqualTo(new Vector3(4f, 4f, 1f)));
        }

        [Test]
        public void RenderUsesCorrectPrefabsAndCubeDefaultSpritesForSupportedTypes()
        {
            var board = new BoardModel(4, 2);
            var boardView = CreateConfiguredBoardView();
            var chaliceBox = new ChaliceBoxObstacleModel(new BoardCoordinate(2, 0), remainingDoorDurability: 2);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(2, 0), new RocketItemModel(RocketOrientation.Vertical));
            board.PlaceItem(new BoardCoordinate(3, 0), new TntItemModel());
            board.PlaceObstacle(new BoardCoordinate(0, 1), new VaseObstacleModel());
            board.PlaceObstacle(new BoardCoordinate(1, 1), new StoneObstacleModel());
            board.PlaceObstacle(chaliceBox.OccupiedCoordinates, chaliceBox);

            boardView.Render(board);

            var itemNames = GetChildNames(GetItemRoot(boardView));
            var obstacleNames = GetChildNames(GetObstacleRoot(boardView));

            Assert.That(itemNames, Has.Some.StartsWith("CubePrefab"));
            Assert.That(itemNames, Has.Some.StartsWith("HorizontalRocketPrefab"));
            Assert.That(itemNames, Has.Some.StartsWith("VerticalRocketPrefab"));
            Assert.That(itemNames, Has.Some.StartsWith("TntPrefab"));
            Assert.That(obstacleNames, Has.Some.StartsWith("VasePrefab"));
            Assert.That(obstacleNames, Has.Some.StartsWith("StonePrefab"));
            Assert.That(obstacleNames, Has.Some.StartsWith("ChaliceBoxPrefab"));

            var cubeRenderer = FindChildByPrefix(GetItemRoot(boardView), "CubePrefab").GetComponent<SpriteRenderer>();
            var cubeView = cubeRenderer.GetComponent<CubeItemView>();
            Assert.That(cubeRenderer.sprite, Is.SameAs(GetField(cubeView, "greenDefaultSprite")));
            Assert.That(cubeRenderer.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void RenderUsesConfiguredCubeSpriteInsteadOfTintingBaseSprite()
        {
            var board = new BoardModel(1, 1);
            var boardView = CreateConfiguredBoardView();
            var baseSprite = CreateSprite(width: 8, height: 8, pixelsPerUnit: 8f);
            var redSprite = CreateSprite(width: 9, height: 9, pixelsPerUnit: 9f);
            var greenSprite = CreateSprite(width: 10, height: 10, pixelsPerUnit: 10f);
            var blueSprite = CreateSprite(width: 11, height: 11, pixelsPerUnit: 11f);
            var yellowSprite = CreateSprite(width: 12, height: 12, pixelsPerUnit: 12f);
            var cubePrefab = (GameObject)GetField(boardView, "cubePrefab");
            var cubeView = cubePrefab.GetComponent<CubeItemView>();

            cubePrefab.GetComponent<SpriteRenderer>().sprite = baseSprite;
            SetField(cubeView, "redDefaultSprite", redSprite);
            SetField(cubeView, "greenDefaultSprite", greenSprite);
            SetField(cubeView, "blueDefaultSprite", blueSprite);
            SetField(cubeView, "yellowDefaultSprite", yellowSprite);

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Green));

            boardView.Render(board);

            var cubeRenderer = GetItemRoot(boardView).GetChild(0).GetComponent<SpriteRenderer>();
            Assert.That(cubeRenderer.sprite, Is.SameAs(greenSprite));
            Assert.That(cubeRenderer.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void RenderCubeWithoutCubeItemViewThrows()
        {
            var board = new BoardModel(1, 1);
            var boardView = CreateConfiguredBoardView();
            var cubePrefab = (GameObject)GetField(boardView, "cubePrefab");

            UnityEngine.Object.DestroyImmediate(cubePrefab.GetComponent<CubeItemView>());
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));

            Assert.That(
                () => boardView.Render(board),
                Throws.InvalidOperationException.With.Message.Contains("CubeItemView"));
        }

        [Test]
        public void RenderUsesDefaultSpritesForSizeTwoAndThreeGroups()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Green));
            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Green));

            boardView.Render(board);

            AssertCubeVisual(boardView, new BoardCoordinate(0, 0), "redDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 0), "redDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(0, 1), "greenDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 1), "greenDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(2, 1), "greenDefaultSprite");
        }

        [Test]
        public void RenderUsesRocketSpritesForEveryCubeInSizeFourGroup()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Blue));

            boardView.Render(board);

            AssertCubeVisual(boardView, new BoardCoordinate(0, 0), "blueRocketSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 0), "blueRocketSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(2, 0), "blueRocketSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 1), "blueRocketSprite");
        }

        [Test]
        public void RenderUsesDefaultSpritesForSizeFiveGroup()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(2, 0), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(0, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Yellow));

            boardView.Render(board);

            AssertCubeVisual(boardView, new BoardCoordinate(0, 0), "yellowDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 0), "yellowDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(2, 0), "yellowDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(0, 1), "yellowDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 1), "yellowDefaultSprite");
        }

        [Test]
        public void RenderUsesTntSpritesForEveryCubeInSizeSixGroup()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView();

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    board.PlaceItem(new BoardCoordinate(x, y), new CubeItemModel(CubeColor.Green));
                }
            }

            boardView.Render(board);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    AssertCubeVisual(boardView, new BoardCoordinate(x, y), "greenTntSprite");
                }
            }
        }

        [Test]
        public void RenderIgnoresDiagonalNeighborsWhenChoosingCubeStateSprites()
        {
            var board = new BoardModel(2, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceItem(new BoardCoordinate(1, 1), new CubeItemModel(CubeColor.Red));

            boardView.Render(board);

            AssertCubeVisual(boardView, new BoardCoordinate(0, 0), "redDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 1), "redDefaultSprite");
        }

        [Test]
        public void RenderChoosesIndependentStatesForSeparateSameColorGroups()
        {
            var board = new BoardModel(5, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(1, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(4, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(3, 1), new CubeItemModel(CubeColor.Blue));
            board.PlaceItem(new BoardCoordinate(4, 1), new CubeItemModel(CubeColor.Blue));

            boardView.Render(board);

            AssertCubeVisual(boardView, new BoardCoordinate(0, 0), "blueDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(1, 0), "blueDefaultSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(3, 0), "blueRocketSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(4, 0), "blueRocketSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(3, 1), "blueRocketSprite");
            AssertCubeVisual(boardView, new BoardCoordinate(4, 1), "blueRocketSprite");
        }

        [Test]
        public void RenderLeavesSpecialItemsOnTheirOwnPrefabs()
        {
            var board = new BoardModel(2, 1);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new RocketItemModel(RocketOrientation.Horizontal));
            board.PlaceItem(new BoardCoordinate(1, 0), new TntItemModel());

            boardView.Render(board);

            var itemNames = GetChildNames(GetItemRoot(boardView));
            Assert.That(itemNames, Has.Some.StartsWith("HorizontalRocketPrefab"));
            Assert.That(itemNames, Has.Some.StartsWith("TntPrefab"));
            Assert.That(itemNames, Has.None.StartsWith("CubePrefab"));
        }

        [Test]
        public void RenderFitsTallItemSpritesInsideLogicalCellSize()
        {
            var board = new BoardModel(1, 1);
            var boardView = CreateConfiguredBoardView(cellSize: 1f);
            var tallSprite = CreateSprite(width: 10, height: 20, pixelsPerUnit: 10f);

            SetField(((GameObject)GetField(boardView, "cubePrefab")).GetComponent<CubeItemView>(), "redDefaultSprite", tallSprite);
            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));

            boardView.Render(board);

            var itemVisual = GetItemRoot(boardView).GetChild(0);
            Assert.That(itemVisual.localScale, Is.EqualTo(new Vector3(1f, 0.5f, 1f)));
        }

        [Test]
        public void RenderUpdatesConfiguredGridBackgroundToBoardFootprint()
        {
            var board = new BoardModel(10, 8);
            var boardView = CreateConfiguredBoardView(origin: new Vector2(-2.5f, -3.9f), cellSize: 0.5f);
            var background = CreateGameObject("GridBackground");
            var renderer = background.AddComponent<SpriteRenderer>();

            SetField(boardView, "gridBackgroundRenderer", renderer);
            SetField(boardView, "gridBackgroundPadding", new Vector2(0.2f, 0.2f));
            SetField(boardView, "gridBackgroundZ", 0.5f);

            background.transform.SetParent(boardView.transform, false);

            boardView.Render(board);

            Assert.That(background.transform.localPosition, Is.EqualTo(new Vector3(0f, -1.9f, 0.5f)));
            Assert.That(background.transform.localScale, Is.EqualTo(new Vector3(5.4f, 4.4f, 1f)));
        }

        [Test]
        public void RerenderClearsStaleVisualsAndRebuildsFromCurrentBoardState()
        {
            var board = new BoardModel(2, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Red));
            board.PlaceObstacle(new BoardCoordinate(1, 1), new VaseObstacleModel());

            boardView.Render(board);

            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(1));
            Assert.That(GetObstacleRoot(boardView).childCount, Is.EqualTo(1));

            board.ClearItem(new BoardCoordinate(0, 0));
            board.ClearObstacle(new BoardCoordinate(1, 1));
            board.PlaceItem(new BoardCoordinate(1, 0), new TntItemModel());

            boardView.Render(board);

            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(1));
            Assert.That(GetObstacleRoot(boardView).childCount, Is.EqualTo(0));
            Assert.That(GetItemRoot(boardView).GetChild(0).name, Does.StartWith("TntPrefab"));
        }

        [Test]
        public void ClearRemovesAllSpawnedVisuals()
        {
            var board = new BoardModel(2, 2);
            var boardView = CreateConfiguredBoardView();

            board.PlaceItem(new BoardCoordinate(0, 0), new CubeItemModel(CubeColor.Blue));
            board.PlaceObstacle(new BoardCoordinate(1, 1), new StoneObstacleModel());

            boardView.Render(board);
            boardView.Clear();

            Assert.That(GetItemRoot(boardView).childCount, Is.EqualTo(0));
            Assert.That(GetObstacleRoot(boardView).childCount, Is.EqualTo(0));
        }

        [Test]
        public void CoordinateMappingIsDeterministicForItemsAndItemsRenderAboveObstacles()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView(origin: new Vector2(1f, 2f), cellSize: 2f, itemZ: -0.5f, obstacleZ: 0.25f);

            board.PlaceItem(new BoardCoordinate(2, 1), new CubeItemModel(CubeColor.Yellow));
            board.PlaceObstacle(new BoardCoordinate(0, 0), new VaseObstacleModel());

            boardView.Render(board);

            var itemVisual = FindChildByPrefix(GetItemRoot(boardView), "CubePrefab");
            var obstacleVisual = FindChildByPrefix(GetObstacleRoot(boardView), "VasePrefab");

            Assert.That(itemVisual.transform.localPosition, Is.EqualTo(new Vector3(6f, 5f, -0.5f)));
            Assert.That(obstacleVisual.transform.localPosition, Is.EqualTo(new Vector3(2f, 3f, 0.25f)));
            Assert.That(itemVisual.transform.localPosition.z, Is.LessThan(obstacleVisual.transform.localPosition.z));
        }

        [Test]
        public void TryWorldToBoardCoordinateMapsCellCentersDeterministically()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView(origin: new Vector2(1f, 2f), cellSize: 2f);

            var bottomLeftWorld = boardView.transform.TransformPoint(new Vector3(2f, 3f, 0f));
            var topRightWorld = boardView.transform.TransformPoint(new Vector3(6f, 5f, 0f));

            Assert.That(boardView.TryWorldToBoardCoordinate(board, bottomLeftWorld, out var bottomLeft), Is.True);
            Assert.That(bottomLeft, Is.EqualTo(new BoardCoordinate(0, 0)));
            Assert.That(boardView.TryWorldToBoardCoordinate(board, topRightWorld, out var topRight), Is.True);
            Assert.That(topRight, Is.EqualTo(new BoardCoordinate(2, 1)));
        }

        [Test]
        public void TryWorldToBoardCoordinateReturnsFalseOutsideBoardBounds()
        {
            var board = new BoardModel(3, 2);
            var boardView = CreateConfiguredBoardView(origin: new Vector2(1f, 2f), cellSize: 2f);
            var outsideWorld = boardView.transform.TransformPoint(new Vector3(0.9f, 2.5f, 0f));

            Assert.That(boardView.TryWorldToBoardCoordinate(board, outsideWorld, out _), Is.False);
        }

        private BoardView CreateConfiguredBoardView(
            Vector2? origin = null,
            float cellSize = 1f,
            float itemZ = -0.1f,
            float obstacleZ = 0f)
        {
            var host = CreateGameObject("BoardViewHost");
            var itemRoot = CreateGameObject("ItemRoot").transform;
            var obstacleRoot = CreateGameObject("ObstacleRoot").transform;

            itemRoot.SetParent(host.transform, false);
            obstacleRoot.SetParent(host.transform, false);

            var boardView = host.AddComponent<BoardView>();

            SetField(boardView, "itemVisualRoot", itemRoot);
            SetField(boardView, "obstacleVisualRoot", obstacleRoot);
            SetField(boardView, "origin", origin ?? Vector2.zero);
            SetField(boardView, "cellSize", cellSize);
            SetField(boardView, "itemZ", itemZ);
            SetField(boardView, "obstacleZ", obstacleZ);
            SetField(boardView, "cubePrefab", CreateCubePrefab());
            SetField(boardView, "horizontalRocketPrefab", CreateVisualPrefab("HorizontalRocketPrefab"));
            SetField(boardView, "verticalRocketPrefab", CreateVisualPrefab("VerticalRocketPrefab"));
            SetField(boardView, "tntPrefab", CreateVisualPrefab("TntPrefab"));
            SetField(boardView, "vasePrefab", CreateVisualPrefab("VasePrefab"));
            SetField(boardView, "stonePrefab", CreateVisualPrefab("StonePrefab"));
            SetField(boardView, "chaliceBoxPrefab", CreateVisualPrefab("ChaliceBoxPrefab"));

            return boardView;
        }

        private Transform GetItemRoot(BoardView boardView)
        {
            return (Transform)GetField(boardView, "itemVisualRoot");
        }

        private Transform GetObstacleRoot(BoardView boardView)
        {
            return (Transform)GetField(boardView, "obstacleVisualRoot");
        }

        private GameObject CreateVisualPrefab(string name)
        {
            var prefab = CreateGameObject(name);
            prefab.AddComponent<SpriteRenderer>();
            return prefab;
        }

        private GameObject CreateCubePrefab()
        {
            var prefab = CreateVisualPrefab("CubePrefab");
            var cubeView = prefab.AddComponent<CubeItemView>();
            var spriteRenderer = prefab.GetComponent<SpriteRenderer>();

            SetField(cubeView, "spriteRenderer", spriteRenderer);
            SetField(cubeView, "redDefaultSprite", CreateSprite(width: 14, height: 16, pixelsPerUnit: 14f));
            SetField(cubeView, "greenDefaultSprite", CreateSprite(width: 15, height: 16, pixelsPerUnit: 15f));
            SetField(cubeView, "blueDefaultSprite", CreateSprite(width: 16, height: 16, pixelsPerUnit: 16f));
            SetField(cubeView, "yellowDefaultSprite", CreateSprite(width: 17, height: 16, pixelsPerUnit: 17f));
            SetField(cubeView, "redRocketSprite", CreateSprite(width: 18, height: 16, pixelsPerUnit: 18f));
            SetField(cubeView, "greenRocketSprite", CreateSprite(width: 19, height: 16, pixelsPerUnit: 19f));
            SetField(cubeView, "blueRocketSprite", CreateSprite(width: 20, height: 16, pixelsPerUnit: 20f));
            SetField(cubeView, "yellowRocketSprite", CreateSprite(width: 21, height: 16, pixelsPerUnit: 21f));
            SetField(cubeView, "redTntSprite", CreateSprite(width: 22, height: 16, pixelsPerUnit: 22f));
            SetField(cubeView, "greenTntSprite", CreateSprite(width: 23, height: 16, pixelsPerUnit: 23f));
            SetField(cubeView, "blueTntSprite", CreateSprite(width: 24, height: 16, pixelsPerUnit: 24f));
            SetField(cubeView, "yellowTntSprite", CreateSprite(width: 25, height: 16, pixelsPerUnit: 25f));
            return prefab;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            createdTextures.Add(texture);
            createdSprites.Add(sprite);
            return sprite;
        }

        private static GameObject FindChildByPrefix(Transform root, string prefix)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);

                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }

            Assert.Fail($"Could not find child with prefix '{prefix}'.");
            return null;
        }

        private static IReadOnlyList<string> GetChildNames(Transform root)
        {
            var names = new List<string>();

            for (var index = 0; index < root.childCount; index++)
            {
                names.Add(root.GetChild(index).name);
            }

            return names;
        }

        private void AssertCubeVisual(BoardView boardView, BoardCoordinate coordinate, string expectedSpriteFieldName)
        {
            var cubeRenderer = FindChildByPrefix(GetItemRoot(boardView), $"CubePrefab_{coordinate}")
                .GetComponent<SpriteRenderer>();
            var cubeView = ((GameObject)GetField(boardView, "cubePrefab")).GetComponent<CubeItemView>();
            var expectedSprite = (Sprite)GetField(cubeView, expectedSpriteFieldName);
            Assert.That(cubeRenderer.sprite, Is.SameAs(expectedSprite));
            Assert.That(cubeRenderer.color, Is.EqualTo(Color.white));
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(target);
        }
    }
}
