using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TitanGate.EditorTools
{
    /// <summary>Opt-in local QA queue for an already-open Editor. No scene replacement or runtime injection.</summary>
    [InitializeOnLoad]
    public static class PolishQARunner
    {
        const string Request = "Logs/polish-qa.request";
        static double _next;
        static TestRunnerApi _api;
        static bool _requestRefreshed;
        static PolishQARunner() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
            // A background Editor may not have imported newly written scripts yet.
            // Refresh before consuming the request so QA cannot silently use old assemblies.
            if (!_requestRefreshed) { _requestRefreshed = true; AssetDatabase.Refresh(); return; }
            string command = File.ReadAllText(Request).Trim(); File.Delete(Request);
            _requestRefreshed = false;
            if (command == "tests")
            {
                _api = ScriptableObject.CreateInstance<TestRunnerApi>(); _api.RegisterCallbacks(new Results());
                _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
            }
            else if (command == "build") Build();
            else if (command == "build-story") Build("Builds/Story/TitanGate2177.exe");
        }
        public static void Build(string location = "Builds/Polish/TitanGate2177.exe")
        {
            var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath }, locationPathName = location,
                target = UnityEditor.BuildTarget.StandaloneWindows64, options = UnityEditor.BuildOptions.None
            });
            File.WriteAllText("Logs/polish-build-result.txt", report.summary.result + " errors=" + report.summary.totalErrors);
        }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, "Logs/polish-tests.xml");
                File.WriteAllText("Logs/polish-tests-result.txt", result.TestStatus + " passed=" + result.PassCount + " failed=" + result.FailCount);
                Object.DestroyImmediate(_api);
            }
        }
    }
}
