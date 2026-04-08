using System;
using System.Collections.Generic;
using System.Reflection;
using DreamBlastClone.Controllers;
using DreamBlastClone.Controllers.Unity;
using DreamBlastClone.Core;
using DreamBlastClone.Grid;
using DreamBlastClone.Items;
using DreamBlastClone.Obstacles;
using DreamBlastClone.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DreamBlastClone.Tests.EditMode
{
    public sealed class LevelFlowControllerTests
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
        public void CurrentLevelStoreDefaultsToOne()
        {
            var store = new CurrentLevelStore(PlayerPrefsKey);

            Assert.That(store.GetCurrentLevel(), Is.EqualTo(1));
        }

        [Test]
        public void CurrentLevelStoreClampsToAtLeastOne()
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, -5);
            PlayerPrefs.Save();

            var store = new CurrentLevelStore(PlayerPrefsKey);

            Assert.That(store.GetCurrentLevel(), Is.EqualTo(1));
        }

        [Test]
        public void CurrentLevelStoreStoresFinishedSentinelAfterFinalWin()
        {
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(10);

            store.AdvanceOnWin(10);

            Assert.That(store.GetCurrentLevel(), Is.EqualTo(11));
            Assert.That(store.IsFinished(10), Is.True);
        }

        [Test]
        public void CurrentLevelStoreReturnsPlayableLevelWhenFinished()
        {
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(11);

            Assert.That(store.GetPlayableLevel(10), Is.EqualTo(10));
        }

        [Test]
        public void MainSceneLauncherUpdatesLabelAndStartsLevelSceneFromButton()
        {
            var catalog = CreateCatalog(new TextAsset("1"), new TextAsset("2"), new TextAsset("3"));
            var launcher = CreateGameObject("Launcher").AddComponent<TestMainSceneLauncher>();
            var startButton = CreateButton("StartButton");
            var levelLabel = CreateLabel("LevelLabel");
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(2);

            SetField(launcher, "levelCatalog", catalog);
            SetField(launcher, "startButton", startButton);
            SetField(launcher, "levelLabel", levelLabel);
            InvokeMethod(launcher, "Awake");
            InvokeMethod(launcher, "OnEnable");

            Assert.That(launcher.CurrentLevelNumber, Is.EqualTo(2));
            Assert.That(launcher.IsFinished, Is.False);
            Assert.That(levelLabel.text, Is.EqualTo("Level 2"));
            Assert.That(startButton.interactable, Is.True);

            startButton.onClick.Invoke();

            Assert.That(launcher.LoadedSceneName, Is.EqualTo("LevelScene"));
            Assert.That(launcher.TryStartCurrentLevel(), Is.False);
            Assert.That(startButton.interactable, Is.False);

            InvokeMethod(launcher, "OnDisable");
        }

        [Test]
        public void MainSceneLauncherShowsFinishedAndDisablesButtonWhenProgressionIsComplete()
        {
            var catalog = CreateCatalog(new TextAsset("1"), new TextAsset("2"), new TextAsset("3"));
            var launcher = CreateGameObject("Launcher").AddComponent<TestMainSceneLauncher>();
            var startButton = CreateButton("StartButton");
            var levelLabel = CreateLabel("LevelLabel");
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(4);

            SetField(launcher, "levelCatalog", catalog);
            SetField(launcher, "startButton", startButton);
            SetField(launcher, "levelLabel", levelLabel);
            InvokeMethod(launcher, "Awake");
            InvokeMethod(launcher, "OnEnable");

            Assert.That(launcher.CurrentLevelNumber, Is.EqualTo(3));
            Assert.That(launcher.IsFinished, Is.True);
            Assert.That(levelLabel.text, Is.EqualTo("Finished"));
            Assert.That(startButton.interactable, Is.False);

            startButton.onClick.Invoke();

            Assert.That(launcher.LoadedSceneName, Is.Null);
            Assert.That(launcher.TryStartCurrentLevel(), Is.False);

            InvokeMethod(launcher, "OnDisable");
        }

        [Test]
        public void LevelSceneFlowControllerCompletesWinAndAdvancesCurrentLevelOnce()
        {
            var catalog = CreateCatalog(new TextAsset("1"), new TextAsset("2"), new TextAsset("3"));
            var controller = CreateGameObject("FlowController").AddComponent<TestLevelSceneFlowController>();
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(1);

            SetField(controller, "levelCatalog", catalog);

            controller.CompleteWinAndReturnToMainScene();
            controller.CompleteWinAndReturnToMainScene();

            Assert.That(store.GetCurrentLevel(), Is.EqualTo(2));
            Assert.That(controller.LoadedSceneName, Is.EqualTo("MainScene"));
        }

        [Test]
        public void LevelSceneFlowControllerCompleteWinAndReturnToMainSceneSupportsFinishedSentinel()
        {
            var catalog = CreateCatalog(new TextAsset("1"), new TextAsset("2"), new TextAsset("3"));
            var controller = CreateGameObject("FlowController").AddComponent<TestLevelSceneFlowController>();
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(3);
            SetField(controller, "levelCatalog", catalog);

            controller.CompleteWinAndReturnToMainScene();

            Assert.That(store.IsFinished(3), Is.True);
            Assert.That(store.GetPlayableLevel(3), Is.EqualTo(3));
            Assert.That(controller.LoadedSceneName, Is.EqualTo("MainScene"));
        }

        [Test]
        public void LevelSceneFlowControllerRetryCurrentLevelLoadsLevelScene()
        {
            var controller = CreateGameObject("FlowController").AddComponent<TestLevelSceneFlowController>();

            controller.RetryCurrentLevel();

            Assert.That(controller.LoadedSceneName, Is.EqualTo("LevelScene"));
        }

        [Test]
        public void LevelSceneFlowControllerReturnToMainSceneLoadsMainScene()
        {
            var controller = CreateGameObject("FlowController").AddComponent<TestLevelSceneFlowController>();

            controller.ReturnToMainScene();

            Assert.That(controller.LoadedSceneName, Is.EqualTo("MainScene"));
        }

        [Test]
        public void MainSceneLauncherShowsNextLevelAfterProgressionAdvances()
        {
            var catalog = CreateCatalog(new TextAsset("1"), new TextAsset("2"), new TextAsset("3"));
            var launcher = CreateGameObject("Launcher").AddComponent<TestMainSceneLauncher>();
            var levelLabel = CreateLabel("LevelLabel");
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(1);
            store.AdvanceOnWin(3);

            SetField(launcher, "levelCatalog", catalog);
            SetField(launcher, "levelLabel", levelLabel);

            InvokeMethod(launcher, "Awake");

            Assert.That(launcher.CurrentLevelNumber, Is.EqualTo(2));
            Assert.That(launcher.IsFinished, Is.False);
            Assert.That(levelLabel.text, Is.EqualTo("Level 2"));
        }

        [Test]
        public void MainSceneLauncherShowsFinishedAfterFinalWinProgression()
        {
            var catalog = CreateCatalog(new TextAsset("1"), new TextAsset("2"), new TextAsset("3"));
            var launcher = CreateGameObject("Launcher").AddComponent<TestMainSceneLauncher>();
            var levelLabel = CreateLabel("LevelLabel");
            var store = new CurrentLevelStore(PlayerPrefsKey);
            store.SetCurrentLevel(3);
            store.AdvanceOnWin(3);

            SetField(launcher, "levelCatalog", catalog);
            SetField(launcher, "levelLabel", levelLabel);

            InvokeMethod(launcher, "Awake");

            Assert.That(launcher.CurrentLevelNumber, Is.EqualTo(3));
            Assert.That(launcher.IsFinished, Is.True);
            Assert.That(levelLabel.text, Is.EqualTo("Finished"));
        }

        private LevelCatalogAsset CreateCatalog(params TextAsset[] files)
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            createdObjects.Add(catalog);
            SetField(catalog, "levelJsonFiles", files);
            return catalog;
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

        private Text CreateLabel(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            createdObjects.Add(gameObject);
            return gameObject.GetComponent<Text>();
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = FindMethod(target.GetType(), methodName);
            method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static MethodInfo FindMethod(Type type, string methodName)
        {
            while (type is not null)
            {
                var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (method is not null)
                {
                    return method;
                }

                type = type.BaseType;
            }

            throw new MissingMethodException(methodName);
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

        private sealed class TestMainSceneLauncher : MainSceneLauncher
        {
            public string LoadedSceneName { get; private set; }

            protected override void LoadScene(string sceneName)
            {
                LoadedSceneName = sceneName;
            }
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
