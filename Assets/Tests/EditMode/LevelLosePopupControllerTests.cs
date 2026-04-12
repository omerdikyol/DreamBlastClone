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
            Assert.That(setup.PopupCanvasGroup.alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.TryAgainButton.interactable, Is.False);
            Assert.That(setup.MainMenuButton.interactable, Is.False);
            Assert.That(setup.CloseButton.interactable, Is.False);

            InvokeMethod(setup.PopupController, "OnDisable");
        }

        [Test]
        public void LoseShowPlaysPopupOpenAndLoseSting()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);

            Assert.That(setup.AudioController.PlayedSfx, Is.EqualTo(new[] { GameSfxCue.PopupOpen, GameSfxCue.LoseSting }));
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
        public void EnteringStateKeepsGameplaySuppressedUntilPopupIsVisible()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.1f);

            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.TryAgainButton.interactable, Is.False);
            Assert.That(setup.MainMenuButton.interactable, Is.False);
            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
        }

        [Test]
        public void VisibleStateEnablesContentRaycastsForButtons()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.3f);

            Assert.That(setup.ContentCanvasGroup.blocksRaycasts, Is.True);
            Assert.That(setup.ContentCanvasGroup.interactable, Is.True);
            Assert.That(setup.TryAgainButton.interactable, Is.True);
            Assert.That(setup.MainMenuButton.interactable, Is.True);
            Assert.That(setup.CloseButton.interactable, Is.True);
        }

        [Test]
        public void CloseButtonReturnsToMainSceneAfterExitAnimation()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.2f);

            Assert.That(setup.CloseButton.interactable, Is.True);
            setup.CloseButton.onClick.Invoke();

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);

            AdvanceLosePresentation(setup.PopupController, 0.16f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("MainScene"));
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        [Test]
        public void MainMenuButtonReturnsToMainSceneAfterExitAnimation()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.2f);

            setup.MainMenuButton.onClick.Invoke();

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
            AdvanceLosePresentation(setup.PopupController, 0.16f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("MainScene"));
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        [Test]
        public void TryAgainButtonReloadsLevelSceneAfterExitAnimation()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.2f);

            setup.TryAgainButton.onClick.Invoke();

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);

            AdvanceLosePresentation(setup.PopupController, 0.16f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("LevelScene"));
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        [Test]
        public void ClosingPopupPlaysPopupClose()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            setup.AudioController.PlayedSfx.Clear();
            AdvanceLosePresentation(setup.PopupController, 0.2f);
            setup.MainMenuButton.onClick.Invoke();

            Assert.That(setup.AudioController.PlayedSfx, Does.Contain(GameSfxCue.PopupClose));
        }

        [Test]
        public void RepeatedLoseNotificationsDoNotReopenOrDuplicateActions()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.2f);
            setup.TryAgainButton.onClick.Invoke();
            AdvanceLosePresentation(setup.PopupController, 0.16f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("LevelScene"));
        }

        [Test]
        public void DebugShowDisplaysLosePopup()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");

            var didShow = setup.PopupController.TryShowForDebug();

            Assert.That(didShow, Is.True);
            Assert.That(setup.PopupRoot.activeSelf, Is.True);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.PopupCanvasGroup.alpha, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ShowLosePopupAppliesRandomConfiguredTitleAndSubtitle()
        {
            var setup = CreatePopupController();
            var titles = new[] { "So close!", "Keep going!" };
            var subtitles = new[] { "Try a new path.", "You can beat this one." };
            SetField(setup.PopupController, "loseTitles", titles);
            SetField(setup.PopupController, "loseSubtitles", subtitles);

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);

            Assert.That(titles, Does.Contain(setup.TitleLabel.text));
            Assert.That(subtitles, Does.Contain(setup.SubtitleLabel.text));
        }

        [Test]
        public void ReenableResetsLosePopupVisualState()
        {
            var setup = CreatePopupController();

            InvokeMethod(setup.PopupController, "Awake");
            InvokeMethod(setup.PopupController, "OnEnable");
            InvokeHandleTapProcessed(setup.PopupController, LevelState.Lose);
            AdvanceLosePresentation(setup.PopupController, 0.2f);

            InvokeMethod(setup.PopupController, "OnDisable");
            InvokeMethod(setup.PopupController, "OnEnable");

            Assert.That(setup.PopupRoot.activeSelf, Is.False);
            Assert.That(setup.PopupCanvasGroup.alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(setup.TryAgainButton.interactable, Is.False);
        }

        private PopupControllerSetup CreatePopupController()
        {
            var runtime = CreateGameObject("LevelSceneRuntime");
            var inputBridge = runtime.AddComponent<BoardInputSessionBridge>();
            var flowController = runtime.AddComponent<TestLevelSceneFlowController>();
            var audioController = runtime.AddComponent<TestRecordingGameAudioController>();
            var popupController = runtime.AddComponent<LevelLosePopupController>();
            var popupRoot = CreateGameObject("LosePopupRoot");
            var popupCanvas = popupRoot.AddComponent<Canvas>();
            popupRoot.AddComponent<GraphicRaycaster>();
            var popupOverlay = popupRoot.AddComponent<Image>();
            popupOverlay.color = new Color(0f, 0f, 0f, 0.8f);
            var popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();

            var contentRoot = CreateGameObject("LosePopupPanel").AddComponent<RectTransform>();
            contentRoot.SetParent(popupRoot.transform, false);
            contentRoot.gameObject.AddComponent<Image>();
            var contentCanvasGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();

            var titleRoot = CreateGameObject("LoseLabel").AddComponent<RectTransform>();
            titleRoot.SetParent(contentRoot, false);
            var titleLabel = titleRoot.gameObject.AddComponent<Text>();
            titleLabel.text = "Oh no!";
            var subtitleRoot = CreateGameObject("LoseSubtitle").AddComponent<RectTransform>();
            subtitleRoot.SetParent(contentRoot, false);
            var subtitleLabel = subtitleRoot.gameObject.AddComponent<Text>();
            subtitleLabel.text = "Try again.";

            var closeButton = CreateButton("CloseButton");
            closeButton.transform.SetParent(contentRoot, false);
            var tryAgainButton = CreateButton("TryAgainButton");
            tryAgainButton.transform.SetParent(contentRoot, false);
            var mainMenuButton = CreateButton("MainMenuButton");
            mainMenuButton.transform.SetParent(contentRoot, false);

            SetField(popupController, "inputBridge", inputBridge);
            SetField(popupController, "flowController", flowController);
            SetField(popupController, "audioController", audioController);
            SetField(popupController, "popupRoot", popupRoot);
            SetField(popupController, "closeButton", closeButton);
            SetField(popupController, "tryAgainButton", tryAgainButton);
            SetField(popupController, "mainMenuButton", mainMenuButton);
            SetField(popupController, "popupCanvasGroup", popupCanvasGroup);
            SetField(popupController, "contentCanvasGroup", contentCanvasGroup);
            SetField(popupController, "contentRoot", contentRoot);
            SetField(popupController, "titleTransform", titleRoot);
            SetField(popupController, "subtitleTransform", subtitleRoot);
            SetField(popupController, "enterDurationSeconds", 0.2f);
            SetField(popupController, "exitDurationSeconds", 0.16f);
            SetField(popupController, "contentEnterOffsetY", 28f);
            SetField(popupController, "contentEnterScale", 0.94f);
            SetField(popupController, "overlayEnterAlphaMultiplier", 0.78f);
            SetField(popupController, "titleEnterOffsetY", 10f);

            popupCanvas.overrideSorting = true;

            return new PopupControllerSetup(
                popupController,
                inputBridge,
                flowController,
                audioController,
                popupRoot,
                popupCanvasGroup,
                contentCanvasGroup,
                titleLabel,
                subtitleLabel,
                closeButton,
                tryAgainButton,
                mainMenuButton);
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

        private static void AdvanceLosePresentation(LevelLosePopupController controller, float deltaTime)
        {
            var method = controller.GetType().GetMethod("AdvanceLosePresentation", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { deltaTime });
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
                TestRecordingGameAudioController audioController,
                GameObject popupRoot,
                CanvasGroup popupCanvasGroup,
                CanvasGroup contentCanvasGroup,
                Text titleLabel,
                Text subtitleLabel,
                Button closeButton,
                Button tryAgainButton,
                Button mainMenuButton)
            {
                PopupController = popupController;
                InputBridge = inputBridge;
                FlowController = flowController;
                AudioController = audioController;
                PopupRoot = popupRoot;
                PopupCanvasGroup = popupCanvasGroup;
                ContentCanvasGroup = contentCanvasGroup;
                TitleLabel = titleLabel;
                SubtitleLabel = subtitleLabel;
                CloseButton = closeButton;
                TryAgainButton = tryAgainButton;
                MainMenuButton = mainMenuButton;
            }

            public LevelLosePopupController PopupController { get; }

            public BoardInputSessionBridge InputBridge { get; }

            public TestLevelSceneFlowController FlowController { get; }

            public TestRecordingGameAudioController AudioController { get; }

            public GameObject PopupRoot { get; }

            public CanvasGroup PopupCanvasGroup { get; }

            public CanvasGroup ContentCanvasGroup { get; }

            public Text TitleLabel { get; }

            public Text SubtitleLabel { get; }

            public Button CloseButton { get; }

            public Button TryAgainButton { get; }

            public Button MainMenuButton { get; }
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
