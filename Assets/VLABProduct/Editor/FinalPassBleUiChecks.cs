#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;
[InitializeOnLoad]
public static class FinalPassBleUiChecks
{
    static FinalPassBleUiChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.BleUi",false)){SessionState.SetBool("VLAB.BleUi",false);new GameObject("BLE UI checks",typeof(FinalPassBleUiProbe));}};}
    public static void Run(){Directory.CreateDirectory("Evidence/FinalPass");SecondTestGameView.Configure(out _);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","BleUi-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.BleUi",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class FinalPassBleUiProbe:MonoBehaviour
{
    readonly List<string> rows=new List<string>();int failures;
    VlabApp App=>VlabApp.Instance;
    object Call(string method,params object[] args)=>typeof(VlabApp).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(App,args);
    void Check(string name,bool pass){rows.Add((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3);
        var run=Run();while(true){bool next;try{next=run.MoveNext();}catch(Exception e){rows.Add("FAIL "+e);failures++;break;}if(!next)break;yield return run.Current;}
        File.WriteAllLines("Evidence/FinalPass/ble-ui-checks.txt",rows);Debug.Log("BLE UI: "+rows.Count+" checks; failures="+failures);EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        foreach(string locale in new[]{"vi","en"})
        {
            App.Profile.locale=locale;Call("ApplySettings");Call("OpenSettings","home");
            foreach(string screen in new[]{"connection","profiles","calibration"})
            {
                if(screen=="connection")Call("ShowBleController",0,false);else Call(screen=="profiles"?"ShowBleProfiles":"ShowBleCalibration");
                yield return new WaitForSecondsRealtime(.5f);Canvas.ForceUpdateCanvases();
                var box=App.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Dialog");
                foreach(var label in box.GetComponentsInChildren<TextMeshProUGUI>())label.ForceMeshUpdate(true,true);
                var overflow=box.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.isTextOverflowing||t.isTextTruncated).Select(t=>t.text).ToArray();
                Check(locale+" "+screen+" text fits: "+string.Join(" | ",overflow),overflow.Length==0);
                Check(locale+" "+screen+" close reachable",App.GetComponentsInChildren<Button>().Any(b=>b.name=="Close dialog"&&b.interactable));
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/ble-"+locale+"-"+screen+".png");yield return null;
                App.GetComponentsInChildren<Button>().First(b=>b.name=="Close dialog").onClick.Invoke();yield return null;
                Check(locale+" "+screen+" close clears modal",!App.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Modal input blocker"&&r.gameObject.activeInHierarchy));
            }
        }
    }
}
#endif
