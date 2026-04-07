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

        [TearDown]
        public void TearDown()
        {
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
        public void RenderUsesCorrectPrefabsAndCubeTintForSupportedTypes()
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
            Assert.That(cubeRenderer.color, Is.EqualTo(Color.green));
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
            SetField(boardView, "cubePrefab", CreateVisualPrefab("CubePrefab"));
            SetField(boardView, "horizontalRocketPrefab", CreateVisualPrefab("HorizontalRocketPrefab"));
            SetField(boardView, "verticalRocketPrefab", CreateVisualPrefab("VerticalRocketPrefab"));
            SetField(boardView, "tntPrefab", CreateVisualPrefab("TntPrefab"));
            SetField(boardView, "vasePrefab", CreateVisualPrefab("VasePrefab"));
            SetField(boardView, "stonePrefab", CreateVisualPrefab("StonePrefab"));
            SetField(boardView, "chaliceBoxPrefab", CreateVisualPrefab("ChaliceBoxPrefab"));
            SetField(boardView, "redCubeColor", Color.red);
            SetField(boardView, "greenCubeColor", Color.green);
            SetField(boardView, "blueCubeColor", Color.blue);
            SetField(boardView, "yellowCubeColor", Color.yellow);

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

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);
            return gameObject;
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
