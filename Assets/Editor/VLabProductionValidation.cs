using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// A fixed local command set lets the open Editor run the same suites as batch CI.
[InitializeOnLoad]
public static class VLabProductionValidation
{
    private const string Command = "Library/VLAB-production-command.txt";
    private const string ReportKey = "VLAB.Production.Report";
    private static bool refreshedCommand;
    private static double readyAt;
    static VLabProductionValidation()
    {
        TestRunnerApi.RegisterTestCallback(new Results());
        EditorApplication.update += Poll;
    }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Command)) return;
        if(!refreshedCommand)
        {refreshedCommand=true;AssetDatabase.Refresh();readyAt=EditorApplication.timeSinceStartup+.5;return;}
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<readyAt)return;
        string command;
        try {command = File.ReadAllText(Command).Trim();File.Delete(Command);}
        catch(IOException){return;} // Retry an in-progress atomic command handoff next Editor update.
        refreshedCommand=false;
        try
        {
            Directory.CreateDirectory("TestResults/Production");
            if (command == "validate") { VLabUnifiedBuild.ValidateScenes(); return; }
            if (command == "assets") { PrepareSharedController(); return; }
            if (command != "edit" && command != "play" && command != "journey" && command != "production" && command != "tap") throw new ArgumentException("Unknown validation command: " + command);
            SessionState.SetString(ReportKey, "TestResults/Production/" + command + ".xml");
            var filter = new Filter { testMode = command == "edit" ? TestMode.EditMode : TestMode.PlayMode };
            if (command == "journey") filter.testNames = new[] { "VLAB.MainMenu.Tests.VLABUnifiedJourneyTests.EveryLab_ReturnsAndReentersWithoutDuplicateOwners" };
            if (command == "production") filter.groupNames = new[] { "VLAB.MainMenu.Tests.VLABProduction" };
            if (command == "tap") filter.testNames = new[] { "VLAB.ChemistryLab.Tests.PlayMode.DesktopGrabTests.MouseTap_HoldDrainsAndReleaseOrFocusLossStops" };
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
        }
        catch (Exception error) { File.WriteAllText("TestResults/Production/session-error.txt", error.ToString()); Debug.LogException(error); }
    }
    [MenuItem("Tools/VLAB/Production/Prepare shared controller visual")]
    public static void PrepareSharedController()
    {
        const string destination="Assets/VLAB/MainMenu/Resources/VLAB Controller Visual.prefab";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab");
        if(source==null)throw new InvalidOperationException("The installed controller visual is missing.");
        var clone=UnityEngine.Object.Instantiate(source);
        try
        {
            clone.name="VLAB Controller Visual";
            foreach(var component in clone.GetComponentsInChildren<Behaviour>(true))UnityEngine.Object.DestroyImmediate(component);
            foreach(var collider in clone.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            var prefab=PrefabUtility.SaveAsPrefabAsset(clone,destination);
            var assets=Resources.Load<VLAB.MainMenu.VLABMenuAssets>("VLABMenuAssets");
            assets.activityFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/VLAB/DemoLabs/Art/UI/VLAB_Vietnamese.asset");
            assets.controllerVisual=prefab;EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
        }
        finally {UnityEngine.Object.DestroyImmediate(clone);}
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test)
        {Application.logMessageReceived-=EditorServiceWarning;Application.logMessageReceived+=EditorServiceWarning;}
        public void TestFinished(ITestResultAdaptor result) { Application.logMessageReceived-=EditorServiceWarning; }
        private static void EditorServiceWarning(string message,string stack,LogType type)
        {
            const string externalWarning="Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.";
            if(type!=LogType.Warning || message!=externalWarning || !stack.Contains("Unity.AI.Toolkit.Accounts"))return;
            // Preserve this known Editor-only service warning as evidence. Runtime logs still fail normally.
            Directory.CreateDirectory("TestResults/Production");
            File.AppendAllText("TestResults/Production/editor-environment.txt",DateTime.UtcNow.ToString("O")+" "+message+Environment.NewLine);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,message);
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            var path = SessionState.GetString(ReportKey, "");
            if (string.IsNullOrEmpty(path)) return;
            TestRunnerApi.SaveResultToFile(result, path);
            File.WriteAllText(path + ".summary.txt", result.ResultState + "; passed=" + result.PassCount + "; failed=" + result.FailCount + "; duration=" + result.Duration);
            SessionState.EraseString(ReportKey);
        }
    }
}
