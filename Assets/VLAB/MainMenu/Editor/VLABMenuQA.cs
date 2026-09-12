using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VLAB.MainMenu.Editor
{
    [InitializeOnLoad]
    public static class VLABMenuQA
    {
        private static TestRunnerApi runner;
        static VLABMenuQA()
        {
            runner=ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.hideFlags=HideFlags.HideAndDontSave;
            runner.RegisterCallbacks(new Results());
            EditorApplication.update+=CheckRequest;
        }
        private static void CheckRequest()
        {
            const string request="TestResults/MenuUI/run-tests.request";
            const string physicsRequest="TestResults/MenuUI/run-physics.request";
            const string previewRequest="TestResults/SpatialUI/preview-quit.request";
            if(!EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(previewRequest))
            {
                var app=Object.FindAnyObjectByType<VLABApplicationUI>();
                if(EditorApplication.isPlaying && app!=null && app.CurrentScreen!="boot")
                {
                    // Manual QA may restore the originally unset language after testing
                    // Continue. This never records agreement to either legal document.
                    if(File.ReadAllText(previewRequest).Trim()=="restore-unset-language")
                    {PlayerPrefs.DeleteKey(VLABOnboarding.LanguageKey);PlayerPrefs.Save();}
                    File.Delete(previewRequest);app.Show("quit");
                }
                return;
            }
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            // TestRunnerApi starts asynchronously, before isPlaying becomes true.
            if(SessionState.GetBool("VLAB.UI.QA",false))return;
            if(File.Exists(request)){File.Delete(request);Run();}
            else if(File.Exists(physicsRequest))
            {
                File.Delete(physicsRequest);SessionState.SetBool("VLAB.UI.QA",true);
                SessionState.SetString("VLAB.UI.ResultPath","TestResults/MenuUI/physics-regression.xml");
                runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,assemblyNames=new[]{"VLAB.PhysicsLab.Tests.PlayMode"}}));
            }
        }
        [MenuItem("Tools/VLAB/UI/Run journey tests")]
        public static void Run()
        {
            SessionState.SetBool("VLAB.UI.QA",true);
            SessionState.SetString("VLAB.UI.ResultPath","TestResults/MenuUI/results.xml");
            runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,assemblyNames=new[]{"VLAB.MainMenu.Tests"}}));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests){}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor result){}
            public void RunFinished(ITestResultAdaptor result)
            {
                if(!SessionState.GetBool("VLAB.UI.QA",false))return;
                SessionState.SetBool("VLAB.UI.QA",false);
                TestRunnerApi.SaveResultToFile(result,SessionState.GetString("VLAB.UI.ResultPath","TestResults/MenuUI/results.xml"));
                Debug.Log("VLAB UI tests: "+result.ResultState+"; passed="+result.PassCount+"; failed="+result.FailCount);
            }
        }
    }
}
