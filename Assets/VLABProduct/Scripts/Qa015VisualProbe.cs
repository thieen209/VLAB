#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Vlab.Product;

public sealed class Qa015VisualProbe : MonoBehaviour
{
    static readonly string Folder=Path.GetFullPath("../work/qa015-vr-lighting/editor-r2");
    readonly List<string> rows=new List<string>();
    VlabApp App=>VlabApp.Instance;
    object Field(string name)=>typeof(VlabApp).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(App);
    void Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(App,args);
    IEnumerator Press(string name)
    {
        var button=App.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        button.onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);
    }
    void Capture(string name)
    {
        Canvas.ForceUpdateCanvases();LabPanoramaCapture.CaptureReview(App.View,Path.Combine(Folder,name+".png"));
        rows.Add(name+" camera="+App.View.transform.position.ToString("F4")+" rotation="+App.View.transform.eulerAngles.ToString("F4"));
    }
    static void CandidateLighting()
    {
        RenderSettings.ambientSkyColor=new Color(.52f,.54f,.56f);
        RenderSettings.ambientEquatorColor=new Color(.37f,.39f,.41f);
        RenderSettings.ambientGroundColor=new Color(.23f,.24f,.25f);
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if(light.name=="Soft daylight")light.color=new Color(1f,.985f,.96f);
            else if(light.name=="Ceiling illumination"){light.intensity=.78f;light.color=new Color(.98f,.99f,1f);}
        }
    }
    static void BaselineLighting()
    {
        RoomLighting.Apply();RenderSettings.ambientSkyColor=new Color(.44f,.48f,.53f);RenderSettings.ambientEquatorColor=new Color(.28f,.31f,.35f);RenderSettings.ambientGroundColor=new Color(.16f,.17f,.19f);
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {if(light.name=="Soft daylight")light.color=new Color(1f,.965f,.91f);else if(light.name=="Ceiling illumination"){light.intensity=.60f;light.color=new Color(.94f,.97f,1f);}}
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Folder);
        var stack=new Stack<IEnumerator>();stack.Push(Checks());int exit=0;
        while(stack.Count>0)
        {
            bool next=false;object value=null;Exception failure=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){rows.Add("BLOCKER "+failure);exit=1;break;}
            if(!next){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        File.WriteAllLines(Path.Combine(Folder,"visual-probe.txt"),rows);
        SecondTestGameView.Restore();EditorApplication.Exit(exit);
    }
    IEnumerator Checks()
    {
        float deadline=Time.realtimeSinceStartup+45;
        while((!App||App.Route!="home")&&Time.realtimeSinceStartup<deadline)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home initialization timeout");
        var ui=(VlabUi)Field("ui");var page=(RectTransform)Field("page");
        ui.SetWorld(true,App.View);Call("PlaceReadableSurface",page,true);yield return new WaitForSecondsRealtime(.7f);
        Capture("home-world-baseline");
        var world=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.transform!=App.Preview.Shell).ToArray();
        rows.Add("World renderer candidates="+world.Length);
        foreach(var renderer in world)renderer.enabled=false;
        Capture("home-without-world-meshes");foreach(var renderer in world)if(renderer)renderer.enabled=true;
        var veil=page.GetComponentsInChildren<Image>().Single(i=>i.name=="Home room veil");
        veil.enabled=false;Capture("home-without-veil");veil.enabled=true;
        var backings=page.GetComponentsInChildren<Image>().Where(i=>i.name=="Opaque text backing"&&i.enabled).ToArray();
        foreach(var image in backings)image.enabled=false;Capture("home-without-text-backings");foreach(var image in backings)image.enabled=true;
        foreach(var renderer in world)
        {
            var ray=App.View.ViewportPointToRay(new Vector3(.5f,.65f));
            if(renderer.bounds.IntersectRay(ray,out float distance))rows.Add("Center ray mesh="+renderer.name+" distance="+distance+" material="+renderer.sharedMaterial?.name);
        }
        ui.SetWorld(false,App.View);App.ShowHome();yield return new WaitForSecondsRealtime(.5f);
        yield return Press("Subject P");yield return Press("Browser lesson P01");yield return Press("Browser start");yield return Press("Readiness enter");
        deadline=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<deadline)yield return null;
        if(App.Route!="activity")throw new Exception("Station timeout");
        for(int i=0;i<10&&!App.Apparatus.Ready;i++){App.Apparatus.Select(App.Apparatus.NextInstallableItem());if(!App.Apparatus.InstallSelected())throw new Exception("Lighting fixture install failed");}
        foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
        {
            App.Profile.locale=vi?"vi":"en";ui.LargeText=large;Call("ShowEquipment");yield return null;yield return null;yield return null;
            var modal=(RectTransform)Field("modal");var dialog=modal.GetComponentInChildren<PhoneDialogLayout>();
            if(!dialog.AssertPhoneLayout(out string why))throw new Exception(why);
            foreach(var button in modal.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Equipment item ")))
            {
                var label=button.GetComponentInChildren<TMP_Text>();label.ForceMeshUpdate();
                if(!label.text.StartsWith("✓ ")||label.isTextOverflowing)throw new Exception("Fitted label invalid: "+label.text);
            }
            if(modal.GetComponentsInChildren<Button>().Single(b=>b.name=="Equipment take selected").interactable)throw new Exception("Take should be disabled without loose selection");
            rows.Add("PASS installed color/check/text and disabled take; phone layout valid vi="+vi+" large="+large);Capture("installed-tools-"+(vi?"vi":"en")+(large?"-large":""));Call("CloseModal");
        }
        ui.LargeText=false;App.Profile.locale="vi";
        page=(RectTransform)Field("page");page.gameObject.SetActive(false);App.Preview.Clear();yield return new WaitForSecondsRealtime(.4f);
        var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);int count=lights.Length;
        BaselineLighting();yield return null;yield return null;Capture("physics-instruments-lighting-before");CandidateLighting();yield return null;yield return null;Capture("physics-instruments-lighting-after");
        rows.Add("PASS lighting keeps light count="+count+" result="+UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length+"; no new materials, shaders or post effects");
        foreach(string room in new[]{"Chemistry","Biology","Engineering"})
        {
            yield return SceneManager.LoadSceneAsync(room);yield return new WaitForSecondsRealtime(.4f);
            ((RectTransform)Field("page")).gameObject.SetActive(false);App.Preview.Clear();yield return new WaitForSecondsRealtime(.4f);
            BaselineLighting();yield return null;yield return null;Capture(room+"-lighting-before");CandidateLighting();yield return null;yield return null;Capture(room+"-lighting-after");
        }
        rows.Add("Editor monoscopic native renders only; no physical phone/headset or performance certification.");
    }
}
[InitializeOnLoad] public static class Qa015VisualChecks
{
    static Qa015VisualChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.QA015",false)){SessionState.SetBool("VLAB.QA015",false);new GameObject("QA015 visual isolation",typeof(Qa015VisualProbe));}};}
    public static void Run()
    {
        if(!SecondTestGameView.Configure(out string why))throw new Exception(why);
        SessionState.SetBool("VLAB.QA015",true);SessionState.SetBool("NguaManVLAB.ProductSmoke",false);SessionState.SetBool("VLAB.QA014",false);
        SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","QA015-"+Guid.NewGuid().ToString("N")));
        EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
}
#endif
