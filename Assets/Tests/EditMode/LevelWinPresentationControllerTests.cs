using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers;
using DreamBlastClone.Controllers.Unity;
using DreamBlastClone.Systems;
using DreamBlastClone.Views;
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
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
            Assert.That(setup.WinParticlePlayer.IsPlaying, Is.False);
            Assert.That(setup.PresentationRoot.GetComponent<Canvas>().overrideSorting, Is.True);
            Assert.That(setup.PresentationRoot.GetComponent<Canvas>().sortingOrder, Is.EqualTo(110));
            Assert.That(setup.PresentationRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f));

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);

            Assert.That(setup.PresentationRoot.activeSelf, Is.True);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.WinParticlePlayer.IsPlaying, Is.True);
            Assert.That(setup.PresentationRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void WinStartPlaysPopupOpenAndWinSting()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);

            Assert.That(setup.AudioController.PlayedSfx, Is.EqualTo(new[] { GameSfxCue.PopupOpen, GameSfxCue.WinSting }));
            Assert.That(setup.HapticsController.PlayedCues, Is.EqualTo(new[] { GameHapticCue.Light, GameHapticCue.Heavy }));
        }

        [Test]
        public void ContinueAndLoseDoNotShowWinPresentation()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Continue);
            Assert.That(setup.PresentationRoot.activeSelf, Is.False);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.WinParticlePlayer.IsPlaying, Is.False);

            InvokeHandleTapProcessed(setup.Controller, LevelState.Lose);
            Assert.That(setup.PresentationRoot.activeSelf, Is.False);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.WinParticlePlayer.IsPlaying, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.False);
        }

        [Test]
        public void EnterPhaseDoesNotImmediatelyAllowContinue()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.1f);

            Assert.That(setup.PresentationRoot.activeSelf, Is.True);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
        }

        [Test]
        public void ContinueHintAppearsAfterEnterAndMinimumHold()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.28f);
            AdvanceWinPresentation(setup.Controller, 1.19f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);

            AdvanceWinPresentation(setup.Controller, 0.02f);

            Assert.That(setup.ContinueHintRoot.activeSelf, Is.True);
            Assert.That(setup.ContinueHintRoot.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));
        }

        [Test]
        public void ContinueTapBeforeHoldDoesNothing()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.2f);
            InvokeHandleContinuePressed(setup.Controller);

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
            Assert.That(setup.PresentationRoot.activeSelf, Is.True);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
        }

        [Test]
        public void ContinueTapAfterHoldAdvancesProgressionAndLoadsMainScene()
        {
            var setup = CreateController();
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(1);

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.28f);
            AdvanceWinPresentation(setup.Controller, 1.21f);
            InvokeHandleContinuePressed(setup.Controller);
            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);

            AdvanceWinPresentation(setup.Controller, 0.22f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.EqualTo("MainScene"));
            Assert.That(store.GetCurrentLevel(), Is.EqualTo(2));
        }

        [Test]
        public void ContinueExitPlaysPopupClose()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            setup.AudioController.PlayedSfx.Clear();
            AdvanceWinPresentation(setup.Controller, 0.28f);
            AdvanceWinPresentation(setup.Controller, 1.21f);
            InvokeHandleContinuePressed(setup.Controller);

            Assert.That(setup.AudioController.PlayedSfx, Does.Contain(GameSfxCue.PopupClose));
            Assert.That(setup.HapticsController.PlayedCues, Does.Contain(GameHapticCue.Light));
        }

        [Test]
        public void WinPresentationDoesNotAutoReturnWithoutTap()
        {
            var setup = CreateController();
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(1);

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.28f);
            AdvanceWinPresentation(setup.Controller, 10f);

            Assert.That(setup.FlowController.LoadedSceneName, Is.Null);
            Assert.That(store.GetCurrentLevel(), Is.EqualTo(1));
            Assert.That(setup.PresentationRoot.activeSelf, Is.True);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.True);
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
            AdvanceWinPresentation(setup.Controller, 0.28f);
            AdvanceWinPresentation(setup.Controller, 1.21f);
            InvokeHandleContinuePressed(setup.Controller);
            AdvanceWinPresentation(setup.Controller, 0.22f);

            Assert.That(store.GetCurrentLevel(), Is.EqualTo(2));
        }

        [Test]
        public void DebugShowDisplaysWinPresentation()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");

            var didShow = setup.Controller.TryShowForDebug();

            Assert.That(didShow, Is.True);
            Assert.That(setup.PresentationRoot.activeSelf, Is.True);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.InputBridge.IsInputSuppressed, Is.True);
            Assert.That(setup.WinParticlePlayer.IsPlaying, Is.True);
        }

        [Test]
        public void ReenableResetsWinPresentationVisualState()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);
            AdvanceWinPresentation(setup.Controller, 0.28f);
            AdvanceWinPresentation(setup.Controller, 1.21f);

            InvokeMethod(setup.Controller, "OnDisable");
            InvokeMethod(setup.Controller, "OnEnable");

            Assert.That(setup.PresentationRoot.activeSelf, Is.False);
            Assert.That(setup.ContinueHintRoot.activeSelf, Is.False);
            Assert.That(setup.PresentationRoot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void WinPresentationPreservesConfiguredStarScale()
        {
            var setup = CreateController();

            InvokeMethod(setup.Controller, "Awake");
            InvokeMethod(setup.Controller, "OnEnable");
            InvokeHandleTapProcessed(setup.Controller, LevelState.Win);

            Assert.That(setup.StarRoot.localScale.x, Is.EqualTo(0.84f).Within(0.001f));
            Assert.That(setup.StarRoot.localScale.y, Is.EqualTo(0.84f).Within(0.001f));
            Assert.That(setup.StarRoot.localScale.z, Is.EqualTo(1f).Within(0.001f));
        }

        private ControllerSetup CreateController()
        {
            var runtime = CreateGameObject("LevelSceneRuntime");
            var inputBridge = runtime.AddComponent<BoardInputSessionBridge>();
            var flowController = runtime.AddComponent<TestLevelSceneFlowController>();
            var audioController = runtime.AddComponent<TestRecordingGameAudioController>();
            var hapticsController = runtime.AddComponent<TestRecordingGameHapticsController>();
            var controller = runtime.AddComponent<LevelWinPresentationController>();
            var presentationRoot = CreateGameObject("WinPresentationRoot");
            var presentationCanvasGroup = presentationRoot.AddComponent<CanvasGroup>();
            var overlayImage = presentationRoot.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.9f);
            var continueHintRootObject = new GameObject("ContinueHintRoot", typeof(RectTransform));
            createdObjects.Add(continueHintRootObject);
            var continueHintRoot = continueHintRootObject.GetComponent<RectTransform>();
            var continueHintCanvasGroup = continueHintRootObject.AddComponent<CanvasGroup>();
            var particleRoot = CreateGameObject("WinCelebrationParticles").AddComponent<RectTransform>();
            var contentRoot = CreateGameObject("WinPopupPanel").AddComponent<RectTransform>();
            var starRoot = CreateGameObject("WinStar").AddComponent<RectTransform>();
            var titleRoot = CreateGameObject("WinTitle").AddComponent<RectTransform>();
            var winParticlePlayer = presentationRoot.AddComponent<WinCelebrationParticlePlayer>();

            presentationRoot.AddComponent<Canvas>();
            presentationRoot.AddComponent<GraphicRaycaster>();
            particleRoot.SetParent(presentationRoot.transform, false);
            contentRoot.SetParent(presentationRoot.transform, false);
            starRoot.SetParent(presentationRoot.transform, false);
            titleRoot.SetParent(contentRoot, false);
            continueHintRoot.SetParent(contentRoot, false);

            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            createdObjects.Add(catalog);
            SetField(catalog, "levelJsonFiles", new[] { new TextAsset("1"), new TextAsset("2"), new TextAsset("3") });

            var testParticleSprite = CreateSprite(70, 70, 70f);
            SetField(winParticlePlayer, "effectRoot", particleRoot);
            SetField(winParticlePlayer, "particleSprite", testParticleSprite);
            SetField(winParticlePlayer, "initialBurstCount", 4);
            SetField(winParticlePlayer, "ambientBurstCount", 1);
            SetField(winParticlePlayer, "ambientSpawnInterval", 0.1f);
            SetField(winParticlePlayer, "particleLifetime", 0.6f);
            SetField(winParticlePlayer, "initialSpawnRadius", 18f);
            SetField(winParticlePlayer, "ambientSpawnRadius", 24f);
            SetField(winParticlePlayer, "travelDistance", 20f);
            SetField(winParticlePlayer, "driftUpward", 8f);
            SetField(winParticlePlayer, "initialStartSize", 24f);
            SetField(winParticlePlayer, "ambientStartSize", 14f);
            SetField(winParticlePlayer, "endSize", 8f);

            SetField(flowController, "levelCatalog", catalog);
            SetField(controller, "inputBridge", inputBridge);
            SetField(controller, "flowController", flowController);
            SetField(controller, "audioController", audioController);
            SetField(controller, "hapticsController", hapticsController);
            SetField(controller, "presentationRoot", presentationRoot);
            SetField(controller, "continueHintRoot", continueHintRoot.gameObject);
            SetField(controller, "presentationCanvasGroup", presentationCanvasGroup);
            SetField(controller, "continueHintCanvasGroup", continueHintCanvasGroup);
            SetField(controller, "winCelebrationParticlePlayer", winParticlePlayer);
            SetField(controller, "contentRoot", contentRoot);
            SetField(controller, "starTransform", starRoot);
            SetField(controller, "titleTransform", titleRoot);
            SetField(controller, "overlayImage", overlayImage);
            SetField(controller, "enterDurationSeconds", 0.28f);
            SetField(controller, "minimumHoldSeconds", 1.2f);
            SetField(controller, "exitDurationSeconds", 0.22f);
            SetField(controller, "titleEnterOffsetY", 12f);
            SetField(controller, "starIdlePulseAmplitude", 0.02f);
            SetField(controller, "starIdlePulseFrequency", 2f);
            SetField(controller, "overlayEnterAlphaMultiplier", 0.76f);

            return new ControllerSetup(controller, inputBridge, flowController, audioController, hapticsController, presentationRoot, continueHintRoot.gameObject, starRoot, winParticlePlayer);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateSprite(int width, int height, float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            createdObjects.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            createdObjects.Add(sprite);
            return sprite;
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

        private static void InvokeHandleContinuePressed(LevelWinPresentationController controller)
        {
            var method = controller.GetType().GetMethod("HandleContinuePressed", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, null);
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

        private static FieldInfo FindField(Type type, string fieldName)
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
                TestRecordingGameAudioController audioController,
                TestRecordingGameHapticsController hapticsController,
                GameObject presentationRoot,
                GameObject continueHintRoot,
                RectTransform starRoot,
                WinCelebrationParticlePlayer winParticlePlayer)
            {
                Controller = controller;
                InputBridge = inputBridge;
                FlowController = flowController;
                AudioController = audioController;
                HapticsController = hapticsController;
                PresentationRoot = presentationRoot;
                ContinueHintRoot = continueHintRoot;
                StarRoot = starRoot;
                WinParticlePlayer = winParticlePlayer;
            }

            public LevelWinPresentationController Controller { get; }

            public BoardInputSessionBridge InputBridge { get; }

            public TestLevelSceneFlowController FlowController { get; }

            public TestRecordingGameAudioController AudioController { get; }

            public TestRecordingGameHapticsController HapticsController { get; }

            public GameObject PresentationRoot { get; }

            public GameObject ContinueHintRoot { get; }

            public RectTransform StarRoot { get; }

            public WinCelebrationParticlePlayer WinParticlePlayer { get; }
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
