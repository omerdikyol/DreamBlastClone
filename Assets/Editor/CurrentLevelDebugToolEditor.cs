using DreamBlastClone.Controllers.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamBlastClone.Editor
{
    [CustomEditor(typeof(CurrentLevelDebugTool))]
    public sealed class CurrentLevelDebugToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var levelCatalogProperty = serializedObject.FindProperty("levelCatalog");
            var selectedLevelProperty = serializedObject.FindProperty("selectedLevel");

            EditorGUILayout.PropertyField(levelCatalogProperty);
            EditorGUILayout.PropertyField(selectedLevelProperty);

            serializedObject.ApplyModifiedProperties();

            var tool = (CurrentLevelDebugTool)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Stored level: {tool.StoredLevel}\n" +
                $"Playable level: {GetPlayableLevelText(tool)}\n" +
                $"Finished: {(tool.IsFinished ? "Yes" : "No")}",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(tool.LevelCatalog is null || tool.MaxLevelCount == 0))
            {
                if (GUILayout.Button("Use Selected Level"))
                {
                    Apply(tool, static currentTool => currentTool.ApplySelectedLevel());
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Lose"))
                {
                    Apply(tool, static currentTool => currentTool.ApplySelectedLevelAsLose());
                }

                if (GUILayout.Button("Win"))
                {
                    Apply(tool, static currentTool => currentTool.ApplySelectedLevelAsWin());
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("First"))
                {
                    Apply(tool, static currentTool => currentTool.SetFirstLevel());
                }

                if (GUILayout.Button("Last"))
                {
                    Apply(tool, static currentTool => currentTool.SetLastLevel());
                }

                if (GUILayout.Button("Finished"))
                {
                    Apply(tool, static currentTool => currentTool.SetFinishedState());
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Read Saved Level"))
            {
                tool.LoadStoredLevelIntoSelection();
                EditorUtility.SetDirty(tool);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode Simulation", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Simulate Lose"))
                {
                    tool.TrySimulateLosePresentation();
                }

                if (GUILayout.Button("Simulate Win"))
                {
                    tool.TrySimulateWinPresentation();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode in LevelScene to simulate the real win/lose presentation with one click.", MessageType.None);
            }
        }

        private static string GetPlayableLevelText(CurrentLevelDebugTool tool)
        {
            return tool.MaxLevelCount > 0
                ? tool.StoredLevel > tool.MaxLevelCount
                    ? tool.MaxLevelCount.ToString()
                    : tool.StoredLevel.ToString()
                : tool.StoredLevel.ToString();
        }

        private static void Apply(CurrentLevelDebugTool tool, System.Action<CurrentLevelDebugTool> apply)
        {
            Undo.RecordObject(tool, "Change current level");
            apply(tool);
            tool.RefreshKnownUi();
            EditorUtility.SetDirty(tool);

            if (!Application.isPlaying && tool.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(tool.gameObject.scene);
            }
        }
    }
}
