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
    public sealed class LevelWinPresentationControllerTests
    {
        private const string PlayerPrefsKey = "DreamBlastClone.CurrentLevel";
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();

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
        public void PresentationStartsHiddenAndShowsOnWin()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            Assert.That(setup.PresentationRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
            Assert.That(setup.PresentationRoot.GetComponent<Canvas>().overrideSorting, Is.True);
            Assert.That(setup.PresentationRoot.GetComponent<Canvas>().sortingOrder, Is.EqualTo(110));

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);

            Assert.That(setup.PresentationRoot.activeSelf, Is.True);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
        }

        [Test]
        public void ContinueAndLoseDoNotShowWinPresentation()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Continue);
            Assert.That(setup.PresentationRoot.activeSelf, Is.False);

            InvokeHandleTapProcessed(setup.Controller, LevelState.Lose);
            Assert.That(setup.PresentationRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        [Test]
        public void TimerCompletionAdvancesProgressionAndLoadsMainScene()
        {
            var setup = CreateController();
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(1);

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.5f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);

            AdvanceWinPresentation(setup.Controller, 0.5f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("MainScene"));
            Assert.That(store.GetCurrentLevel(), Is.EqualTo(2));
        }

        [Test]
        public void RepeatedWinNotificationsDoNotDoubleAdvanceProgression()
        {
            var setup = CreateController();
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(1);

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 1f);

            Assert.That(store.GetCurrentLevel(), Is.EqualTo(2));
        }

        private ControllerSetup CreateController()
        {
            var runtime = CreateGameObject("LevelSceneRuntime");
            var inputBridge = runtime.AddComponent<BoardInputSessionBridge>();
            var flowController = runtime.AddComponent<TestLevelSceneFlowController>();
            var controller = runtime.AddComponent<LevelWinPresentationController>();
            var presentationRoot = CreateGameObject("WinPresentationRoot");
            presentationRoot.AddComponent<Canvas>();
            presentationRoot.AddComponent<GraphicRaycaster>();
            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();

            createdObjects.Add(catalog);
            SetField(catalog, "levelJsonFiles", new[] { new TextAsset("1"), new TextAsset("2"), new TextAsset("3") });

            SetField(flowController, "levelCatalog", catalog);
            SetField(controller, "inputBridge", inputBridge);
            SetField(controller, "flowController", flowController);
            SetField(controller, "presentationRoot", presentationRoot);
            SetField(controller, "durationSeconds", 1f);

            return new ControllerSetup(controller, inputBridge, flowController, presentationRoot);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeHandleTapProcessed(LevelWinPresentationController controller, LevelState levelState)
        {
            var tapResult = new LevelSessionTapResult(BoardTapDispatchResult.Invalid(), false, 0, levelState);
            var method = controller.GetType().GetMethod("HandleTapProcessed", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { tapResult });
        }

        private static void AdvanceWinPresentation(LevelWinPresentationController controller, float deltaTime)
        {
            var method = controller.GetType().GetMethod("AdvanceWinPresentation", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { deltaTime });
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static FieldInfo FindField(System.Type type, string fieldName)
        {
            while (type is not null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (field is not null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(fieldName);
        }

        private readonly struct ControllerSetup
        {
            public ControllerSetup(
                LevelWinPresentationController controller,
                BoardInputSessionBridge inputBridge,
                TestLevelSceneFlowController flowController,
                GameObject presentationRoot)
            {
                Controller = controller;
                InputBridge = inputBridge;
                FlowController = flowController;
                PresentationRoot = presentationRoot;
            }

            public LevelWinPresentationController Controller { get; }

            public BoardInputSessionBridge InputBridge { get; }

            public TestLevelSceneFlowController FlowController { get; }

            public GameObject PresentationRoot { get; }
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
