#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

public sealed class FollowupSmokeProbe:MonoBehaviour
{
    readonly List<string> rows=new List<string>(),errors=new List<string>();string folder;
    void Check(string text,bool ok){rows.Add((ok?"PASS ":"FAIL ")+text);}
    void Log(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(message+"\n"+stack);}
    IEnumerator Collect(string name,IEnumerator routine)
    {
        int start=rows.Count;var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool next=false;object value=null;Exception failure=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception ex){failure=ex;}
            if(failure!=null){rows.Add("FAIL "+name+" "+failure);while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();yield break;}
            if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(value is IEnumerator nested&&!(value is CustomYieldInstruction)){stack.Push(nested);continue;}yield return value;
        }
        rows.Add((rows.Skip(start).Any(x=>x.StartsWith("FAIL ",StringComparison.Ordinal))?"FAIL ":"PASS ")+"CASE "+name);
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);folder=Path.GetFullPath("Evidence/Runtime");Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return new WaitForSecondsRealtime(1);var app=VlabApp.Instance;
        Check("Isolated Vietnamese Home ready",app&&app.Route=="home"&&app.Profile.locale=="vi");
        yield return Collect("foreground lifecycle",Patch1AppChecks.RunLifecycle(app,Capture,Check));
        yield return Collect("phone world tap",Patch1AppChecks.RunWorldTap(app,Capture,Check));
        try{PhoneInputChecks.Run();Check("Phone input including provider provenance checks",true);}catch(Exception ex){rows.Add("FAIL phone input "+ex);}
        try{GamepadIntegrationChecks.Run();Check("Existing HID fallback regressions",true);}catch(Exception ex){rows.Add("FAIL HID "+ex);}
        if(!SessionState.GetBool("VLAB.FollowupBaseline",false))
        {
            app.ShowHome();yield return new WaitForSecondsRealtime(.3f);
            yield return Collect("existing queued UGUI taps",PhoneInputChecks.RunUiTap(app,Capture,Check));
            app.ShowHome();yield return new WaitForSecondsRealtime(.3f);
            yield return Collect("existing manual equipment and modal routes",Patch1AppChecks.Run(app,Capture,Check));
        }
        Application.logMessageReceived-=Log;Check("No unexpected runtime Error/Exception logs",errors.Count==0);rows.AddRange(errors.Select(x=>"FAIL runtime "+x));
        bool pass=rows.All(x=>!x.StartsWith("FAIL ",StringComparison.Ordinal));string kind=SessionState.GetBool("VLAB.FollowupBaseline",false)?"baseline":"fixed";
        File.WriteAllLines(Path.Combine(folder,"patch2-"+kind+".txt"),new[]{pass?"PASS":"FAILED","Editor synthetic touchscreen/lifecycle only; no physical Android acceptance."}.Concat(rows));
        SessionState.SetBool("VLAB.FollowupRun",false);SessionState.SetBool("VLAB.FollowupBaseline",false);SessionState.SetString("VLAB.TestProfile","");EditorApplication.Exit(pass?0:1);
    }
    void Capture(string name)
    {
        var app=VlabApp.Instance;var camera=app.View;var canvas=app.GetComponentInChildren<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var target=camera.targetTexture;float distance=canvas.planeDistance;
        var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;if(mode!=RenderMode.WorldSpace){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;}
        var layout=canvas.GetComponent<PhoneUiViewport>();var joystick=canvas.GetComponent<PhoneJoystickView>();if(layout)layout.Refresh();Canvas.ForceUpdateCanvases();if(joystick)joystick.Refresh();Canvas.ForceUpdateCanvases();
        LabPanoramaCapture.RenderMono(camera,rt);var active=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,"patch2-"+name+".png"),texture.EncodeToPNG());
        RenderTexture.active=active;camera.targetTexture=target;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;if(layout)layout.Refresh();if(joystick)joystick.Refresh();Canvas.ForceUpdateCanvases();Destroy(texture);Destroy(rt);
    }
}
[InitializeOnLoad]public static class VlabFollowupSmoke
{
    static VlabFollowupSmoke(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.FollowupRun",false))new GameObject("Isolated VLAB patch2 checks",typeof(FollowupSmokeProbe));};}
    public static void Baseline(){Launch(true);}public static void Fixed(){Launch(false);}
    static void Launch(bool baseline)
    {
        SessionState.SetBool("NguaManVLAB.ProductSmoke",false);SessionState.SetBool("VLAB.FollowupBaseline",baseline);SessionState.SetBool("VLAB.FollowupRun",true);
        SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","Patch2-"+Guid.NewGuid().ToString("N")));EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
}
#endif
