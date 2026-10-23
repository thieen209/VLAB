#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

[InitializeOnLoad]
public static class FinalPassUiChecks
{
    static FinalPassUiChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.FinalPassUI",false)){SessionState.SetBool("VLAB.FinalPassUI",false);new GameObject("Final pass UI checks",typeof(FinalPassUiProbe));}};}
    public static void Run(){Directory.CreateDirectory("Evidence/FinalPass");SecondTestGameView.Configure(out _);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","FinalPass-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.FinalPassUI",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class FinalPassUiProbe:MonoBehaviour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly List<string> rows=new List<string>();int failures;
    VlabApp App=>VlabApp.Instance;
    object Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Flags).Invoke(App,args);
    T Field<T>(string name)=>(T)typeof(VlabApp).GetField(name,Flags).GetValue(App);
    void Check(string name,bool ok){rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object v=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){rows.Add("BLOCKER "+error);failures++;break;}if(!next){stack.Pop();continue;}if(v is IEnumerator nested)stack.Push(nested);else yield return v;}
        File.WriteAllLines(Path.GetFullPath("Evidence/FinalPass/dialog-checks.txt"),rows);SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home startup timed out");
        App.Profile.settings.gazeAutoSelect=false;
        for(int i=0;i<10;i++){App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.2f);App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.2f);Check("Editor simulated mode roundtrip "+i,App.GetComponentInChildren<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay);}
        foreach(string choice in new[]{"Ở lại","Bỏ thay đổi","Áp dụng"})
        {
            Call("OpenSettings","home");var draft=Field<GameSettings>("draft");bool previous=App.Profile.settings.hints;draft.hints=!previous;typeof(VlabApp).GetField("dirty",Flags).SetValue(App,true);Call("SettingsBack");yield return null;yield return null;
            var modal=Field<RectTransform>("modal");var dialog=modal.GetComponentInChildren<PhoneDialogLayout>();var rect=(RectTransform)dialog.transform;var viewport=App.GetComponentInChildren<PhoneUiViewport>();
            float safeHeight=viewport.SafePixels.height/viewport.PixelsPerLogicalUnit;
            Check(choice+" confirmation occupies under80% safe height",rect.rect.height*rect.localScale.y<safeHeight*.8f);
            Check(choice+" layout bounds",dialog.AssertPhoneLayout(out _));
            Check(choice+" actions pinned together",modal.GetComponentsInChildren<Button>().Where(b=>new[]{"Ở lại","Bỏ thay đổi","Áp dụng"}.Contains(b.GetComponentInChildren<TMP_Text>().text)).All(b=>b.transform.parent.name=="Phone dialog pinned actions"));
            var shade=(RectTransform)modal.Find("Modal backdrop");var corners=new Vector3[4];shade.GetWorldCorners(corners);
            Check(choice+" backdrop covers physical screen",corners[0].x<1&&corners[0].y<1&&corners[2].x>Screen.width-1&&corners[2].y>Screen.height-1);
            var dim=modal.GetComponent<Image>();dim.color=Color.clear;yield return null;yield return null;Check(choice+" transparent-tool backdrop contract",shade.GetComponent<Image>().color.a==0);dim.color=new Color(0,0,0,.32f);yield return null;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Evidence/FinalPass/dialog-"+(choice=="Ở lại"?"stay":choice=="Bỏ thay đổi"?"discard":"apply")+".png"));
            yield return new WaitForSecondsRealtime(.35f);
            var button=modal.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>().text==choice);button.onClick.Invoke();yield return null;yield return null;
            Check(choice+" closes confirmation",!Field<RectTransform>("modal"));
            Check(choice+" apply/discard state",App.Profile.settings.hints==(choice=="Áp dụng"?!previous:previous));
            Check(choice+" destination",App.Route==(choice=="Ở lại"?"settings":"home"));
        }
        // Compare the real shared UI builder through flat/world rendering, not a sensor simulation.
        App.ShowHome();yield return new WaitForSecondsRealtime(.5f);App.GetComponentInChildren<Canvas>().enabled=false;
        var chartRoot=new GameObject("Isolated UI color chart");var chart=new VlabUi(chartRoot.transform);chart.canvas.sortingOrder=100;UiTheme.Select(0,1);
        var chartPage=chart.Page("Color chart");var swatch=chart.Panel(chartPage,"Opaque reference",600,350,400,300,VlabUi.Hex("202B36"));var opacity=swatch.GetComponent<VlabPanelOpacity>();opacity.Solid=true;opacity.Apply(false);
        Color flat=Color.clear;
        foreach(bool world in new[]{false,true})
        {
            chart.SetWorld(world,App.View);yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.GetFullPath("Evidence/FinalPass/color-"+(world?"world":"flat")+".png"),frame.EncodeToPNG());
            Color pixel=frame.GetPixel(frame.width/2,frame.height/2);rows.Add("COLOR "+(world?"world":"flat")+" center="+pixel);
            Check("Color chart "+(world?"world":"flat")+" control visible",Mathf.Abs(pixel.r-.1255f)<.06f&&Mathf.Abs(pixel.g-.1686f)<.06f&&Mathf.Abs(pixel.b-.2118f)<.06f);
            if(!world)flat=pixel;else rows.Add("DIAGNOSTIC flat/world maxRGB delta="+Mathf.Max(Mathf.Abs(pixel.r-flat.r),Mathf.Abs(pixel.g-flat.g),Mathf.Abs(pixel.b-flat.b)));Destroy(frame);
        }
        Destroy(chartRoot);
    }
}
#endif
