using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers.Unity;
using DreamBlastClone.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelStartPresentationControllerTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] is not null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void IntroStartsSuppressedAndHiddenBeforeGameplay()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeMethod(setup.Controller, "Start");

            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.Controller.IsIntroPlaying, Is.True);
            Assert.That(setup.GridBackground.color.a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.BoardVisual.color.a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.TopBarBackgroundRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.MoveTextRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.MoveNumberRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.GoalTextRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.GoalHudRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.BoardView.transform.localScale.x, Is.LessThan(1f));
        }

        [Test]
        public void IntroCompletionRestoresVisibleStateAndReleasesInput()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeMethod(setup.Controller, "Start");

            InvokeMethod(setup.Controller, "CompleteIntro");

            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
            Assert.That(setup.Controller.IsIntroPlaying, Is.False);
            Assert.That(setup.GridBackground.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(setup.BoardVisual.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(setup.BoardView.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(setup.TopBarBackgroundRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(setup.MoveTextRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(setup.MoveNumberRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(setup.GoalTextRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(setup.GoalHudRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ReenableResetsIntroStateAndCanPlayAgain()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeMethod(setup.Controller, "Start");
            InvokeMethod(setup.Controller, "CompleteIntro");

            InvokeMethod(setup.Controller, "OnDisable");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeMethod(setup.Controller, "Start");

            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.Controller.IsIntroPlaying, Is.True);
            Assert.That(setup.GridBackground.color.a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.BoardVisual.color.a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.MoveTextRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
        }

        private ControllerSetup CreateController()
        {
            var runtime = CreateGameObject("LevelSceneRuntime");
            var inputBridge = runtime.AddComponent<BoardInputSessionBridge>();
            var controller = runtime.AddComponent<LevelStartPresentationController>();

            var boardRoot = CreateGameObject("BoardView");
            var boardView = boardRoot.AddComponent<BoardView>();
            var boardVisual = CreateBoardVisual("CubeVisual");
            boardVisual.transform.SetParent(boardRoot.transform, false);

            var gridBackgroundObject = CreateGameObject("GridBackground");
            var gridBackgroundTransform = gridBackgroundObject.transform;
            gridBackgroundTransform.SetParent(boardRoot.transform, false);
            var gridBackground = gridBackgroundObject.AddComponent<SpriteRenderer>();
            gridBackground.color = Color.white;

            var canvasRootObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            createdObjects.Add(canvasRootObject);
            var canvasRoot = canvasRootObject.GetComponent<RectTransform>();

            var backgroundRoot = CreateUiElement("Background", canvasRoot);
            var topBarBackgroundRoot = CreateUiElement("Top UI Background", canvasRoot);
            var moveTextRoot = CreateUiElement("MoveText", canvasRoot);
            var moveNumberRoot = CreateUiElement("MoveNumber", canvasRoot);
            var goalTextRoot = CreateUiElement("GoalText", canvasRoot);
            var goalHudRoot = CreateUiElement("Goal_1", canvasRoot);
            var winRoot = CreateUiElement("WinPresentationRoot", canvasRoot).gameObject;
            var loseRoot = CreateUiElement("LosePopupRoot", canvasRoot).gameObject;

            SetField(controller, "inputBridge", inputBridge);
            SetField(controller, "boardView", boardView);
            SetField(controller, "gridBackgroundRenderer", gridBackground);
            SetField(controller, "canvasRoot", canvasRoot);
            SetField(controller, "topBarBackground", topBarBackgroundRoot);
            SetField(controller, "moveTextTransform", moveTextRoot);
            SetField(controller, "moveNumberTransform", moveNumberRoot);
            SetField(controller, "goalTextTransform", goalTextRoot);
            SetField(controller, "backgroundRoot", backgroundRoot.gameObject);
            SetField(controller, "winPresentationRoot", winRoot);
            SetField(controller, "losePopupRoot", loseRoot);
            SetField(controller, "boardRevealDurationSeconds", 0.28f);
            SetField(controller, "itemRevealDurationSeconds", 0.18f);
            SetField(controller, "itemRevealStaggerSeconds", 0.02f);
            SetField(controller, "topBarRevealDelaySeconds", 0.08f);
            SetField(controller, "topBarBackgroundDurationSeconds", 0.22f);
            SetField(controller, "topBarElementDurationSeconds", 0.18f);
            SetField(controller, "topBarElementStaggerSeconds", 0.045f);

            return new ControllerSetup(
                controller,
                inputBridge,
                boardView,
                gridBackground,
                boardVisual,
                topBarBackgroundRoot,
                moveTextRoot,
                moveNumberRoot,
                goalTextRoot,
                goalHudRoot);
        }

        private RectTransform CreateUiElement(string name, RectTransform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            createdObjects.Add(gameObject);
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private SpriteRenderer CreateBoardVisual(string name)
        {
            var gameObject = CreateGameObject(name);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 1f, 1f, 1f);
            return renderer;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private readonly struct ControllerSetup
        {
            public ControllerSetup(
                LevelStartPresentationController controller,
                BoardInputSessionBridge inputBridge,
                BoardView boardView,
                SpriteRenderer gridBackground,
                SpriteRenderer boardVisual,
                RectTransform topBarBackgroundRoot,
                RectTransform moveTextRoot,
                RectTransform moveNumberRoot,
                RectTransform goalTextRoot,
                RectTransform goalHudRoot)
            {
                Controller = controller;
                InputBridge = inputBridge;
                BoardView = boardView;
                GridBackground = gridBackground;
                BoardVisual = boardVisual;
                TopBarBackgroundRoot = topBarBackgroundRoot;
                MoveTextRoot = moveTextRoot;
                MoveNumberRoot = moveNumberRoot;
                GoalTextRoot = goalTextRoot;
                GoalHudRoot = goalHudRoot;
            }

            public LevelStartPresentationController Controller { get; }

            public BoardInputSessionBridge InputBridge { get; }

            public BoardView BoardView { get; }

            public SpriteRenderer GridBackground { get; }

            public SpriteRenderer BoardVisual { get; }

            public RectTransform TopBarBackgroundRoot { get; }

            public RectTransform MoveTextRoot { get; }

            public RectTransform MoveNumberRoot { get; }

            public RectTransform GoalTextRoot { get; }

            public RectTransform GoalHudRoot { get; }
        }
    }
}
