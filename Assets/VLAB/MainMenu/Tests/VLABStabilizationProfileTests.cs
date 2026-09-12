using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VLAB.Core.Input;
using Object=UnityEngine.Object;

namespace VLAB.MainMenu.Tests
{
    public class VLABStabilizationProfileTests
    {
        [Serializable] private class Sample
        {
            public string scene;
            public bool paused;
            public float editorMeanFrameMs,editorP95FrameMs;
            public long uiRaycastBytesPerCall,editorAllocatedBytesPerFrame,editorDrawCalls;
            public int sceneRenderers;
            public bool allocationCounterAvailable,drawCounterAvailable;
        }
        [Serializable] private class Report
        {
            public string context="Unity Editor development measurements; includes Editor overhead and test runner. Not Android FPS or player memory.";
            public string unity=Application.unityVersion;
            public string gpu=SystemInfo.graphicsDeviceName;
            public List<Sample> samples=new List<Sample>();
        }
        [UnityTest]
        public IEnumerator RecordEditorIdleCostAndSharedUiAllocation()
        {
            var report=new Report();
            foreach(var scene in new[]{"Menu","PhysicsLab_Base","ChemistryLab","BiologyLab","EngineeringLab"})
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return new WaitForSecondsRealtime(1.5f);
                var app=Object.FindAnyObjectByType<VLABApplicationUI>();
                var flow=VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance;
                while(flow!=null && flow.IsTransitioning)yield return null;
                for(int mode=0;mode<(scene=="Menu"?1:2);mode++)
                {
                    if(mode==1)app.TogglePause();
                    yield return new WaitForSecondsRealtime(.5f);
                    var times=new float[90];long allocated=0,draws=0;
                    bool validAlloc,validDraw;
                    using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
                    using(var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1))
                    {
                        validAlloc=gc.Valid;validDraw=draw.Valid;
                        for(int frame=0;frame<times.Length;frame++)
                        {yield return null;times[frame]=Time.unscaledDeltaTime*1000;allocated+=gc.LastValue;draws+=draw.LastValue;}
                    }
                    var view=Camera.main;var ray=new Ray(view.transform.position,view.transform.forward);
                    var pointer=new PointerEventData(EventSystem.current);var hits=new List<RaycastResult>(64);
                    for(int i=0;i<10;i++)VLabPointerUi.Raycast(ray,pointer,hits);
                    long before=GC.GetAllocatedBytesForCurrentThread();
                    for(int i=0;i<400;i++)VLabPointerUi.Raycast(ray,pointer,hits);
                    long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
                    float total=0;foreach(var time in times)total+=time;Array.Sort(times);
                    report.samples.Add(new Sample{scene=scene,paused=mode==1,editorMeanFrameMs=total/times.Length,editorP95FrameMs=times[85],uiRaycastBytesPerCall=bytes/400,editorAllocatedBytesPerFrame=allocated/90,editorDrawCalls=draws/90,sceneRenderers=Object.FindObjectsByType<Renderer>().Length,allocationCounterAvailable=validAlloc,drawCounterAvailable=validDraw});
                    Assert.That(bytes/400,Is.LessThan(128),"Steady shared UI raycasting must not allocate a list or array per call.");
                }
                if(app.IsPaused)app.TogglePause();
            }
            Directory.CreateDirectory("TestResults/Stabilization");File.WriteAllText("TestResults/Stabilization/editor-profile.json",JsonUtility.ToJson(report,true));
        }
        [TearDown]public void Restore(){Time.timeScale=1;}
    }
}
