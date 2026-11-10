// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using TitanGate.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TitanGate.EditorTools
{
    /// <summary>Creates the one scene the game needs (everything else is built in code) and registers it.</summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Titan Gate/Rebuild Main Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GameFlow");
            go.AddComponent<GameFlow>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Junior Astronaut Trainer";
            PlayerSettings.productName = "Titan Gate 2177";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Debug.Log("Titan Gate: Main scene rebuilt at " + ScenePath);
        }

        /// <summary>Windows build to Builds/Win. Batch: -executeMethod TitanGate.EditorTools.ProjectSetup.BuildWindows</summary>
        [MenuItem("Titan Gate/Build Windows")]
        public static void BuildWindows()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Win/TitanGate2177.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Titan Gate build: " + report.summary.result + " errors=" + report.summary.totalErrors);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
