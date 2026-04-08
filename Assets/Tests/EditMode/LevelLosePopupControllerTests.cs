using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers;
using DreamBlastClone.Controllers.Unity;
using DreamBlastClone.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelLosePopupControllerTests
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
        public void PopupStartsHiddenAndShowsOnLose()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");

            Assert.That(setup.PopupRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
            Assert.That(setup.PopupRoot.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(setup.PopupRoot.GetComponent<Canvas>().overrideSorting, Is.True);
            Assert.That(setup.PopupRoot.GetComponent<Canvas>().sortingOrder, Is.EqualTo(100));

            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);

            Assert.That(setup.PopupRoot.activeSelf, Is.True);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);

            InvokeMethod(setup.PopupController, "OnDisable");
        }

        [Test]
        public void ContinueAndWinDoNotShowLosePopup()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");

            InvokeHandleTapProcessed(setup.PopupController, LevelState.Continue);
            Assert.That(setup.PopupRoot.activeSelf, Is.False);

            InvokeHandleTapProcessed(setup.PopupController, LevelState.Win);
            Assert.That(setup.PopupRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);

            InvokeMethod(setup.PopupController, "OnDisable");
        }

        [Test]
        public void CloseButtonReturnsToMainScene()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);

            setup.CloseButton.onClick.Invoke();

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("MainScene"));
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        [Test]
        public void TryAgainButtonReloadsLevelScene()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);

            setup.TryAgainButton.onClick.Invoke();

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("LevelScene"));
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        private PopupControllerSetup CreatePopupController()
        {
            var runtime = CreateGameObject("LevelSceneRuntime");
            var inputBridge = runtime.AddComponent<BoardInputSessionBridge>();
            var flowController = runtime.AddComponent<TestLevelSceneFlowController>();
            var popupController = runtime.AddComponent<LevelLosePopupController>();
            var popupRoot = CreateGameObject("LosePopupRoot");
            popupRoot.AddComponent<Canvas>();
            popupRoot.AddComponent<GraphicRaycaster>();
            var closeButton = CreateButton("CloseButton");
            var tryAgainButton = CreateButton("TryAgainButton");

            closeButton.transform.SetParent(popupRoot.transform, false);
            tryAgainButton.transform.SetParent(popupRoot.transform, false);

            SetField(popupController, "inputBridge", inputBridge);
            SetField(popupController, "flowController", flowController);
            SetField(popupController, "popupRoot", popupRoot);
            SetField(popupController, "closeButton", closeButton);
            SetField(popupController, "tryAgainButton", tryAgainButton);

            return new PopupControllerSetup(
                popupController,
                inputBridge,
                flowController,
                popupRoot,
                closeButton,
                tryAgainButton);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Button CreateButton(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            createdObjects.Add(gameObject);
            return gameObject.GetComponent<Button>();
        }

        private static void InvokeHandleTapProcessed(LevelLosePopupController controller, LevelState levelState)
        {
            var tapResult = new LevelSessionTapResult(BoardTapDispatchResult.Invalid(), false, 0, levelState);
            var method = controller.GetType().GetMethod("HandleTapProcessed", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { tapResult });
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

        private readonly struct PopupControllerSetup
        {
            public PopupControllerSetup(
                LevelLosePopupController popupController,
                BoardInputSessionBridge inputBridge,
                TestLevelSceneFlowController flowController,
                GameObject popupRoot,
                Button closeButton,
                Button tryAgainButton)
            {
                PopupController = popupController;
                InputBridge = inputBridge;
                FlowController = flowController;
                PopupRoot = popupRoot;
                CloseButton = closeButton;
                TryAgainButton = tryAgainButton;
            }

            public LevelLosePopupController PopupController { get; }

            public BoardInputSessionBridge InputBridge { get; }

            public TestLevelSceneFlowController FlowController { get; }

            public GameObject PopupRoot { get; }

            public Button CloseButton { get; }

            public Button TryAgainButton { get; }
        }

        private sealed class TestLevelSceneFlowController : LevelSceneFlowController
        {
            public string LoadedSceneName { get; private set; }

            protected override void LoadScene(string sceneName)
            {
                LoadedSceneName = sceneName;
            }
        }
    }
}
