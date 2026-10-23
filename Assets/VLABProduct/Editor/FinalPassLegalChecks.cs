#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

[InitializeOnLoad]
public static class FinalPassLegalChecks
{
    static FinalPassLegalChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.FinalPassLegal",false)){SessionState.SetBool("VLAB.FinalPassLegal",false);new GameObject("Legal PDF checks",typeof(FinalPassLegalProbe));}};}
    public static void Run(){Directory.CreateDirectory("Evidence/FinalPass");SecondTestGameView.Configure(out _);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","FinalPassLegal-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.FinalPassLegal",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class FinalPassLegalProbe:MonoBehaviour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;readonly List<string> rows=new List<string>();int failures;
    VlabApp App=>VlabApp.Instance;
    void Check(string name,bool value){rows.Add((value?"PASS ":"FAIL ")+name);if(!value)failures++;}
    Button Button(LegalPdfView view,string name)=>view.GetComponentsInChildren<Button>().First(b=>b.name=="PDF "+name);
    void Press(LegalPdfView view,string name){var button=Button(view,name);if(!button.IsInteractable())throw new Exception("Disabled PDF action "+name);button.onClick.Invoke();}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){rows.Add("BLOCKER "+error);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllLines(Path.GetFullPath("Evidence/FinalPass/legal-checks.txt"),rows);SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;if(!App||App.Route!="home")throw new Exception("Home timeout");App.Profile.settings.gazeAutoSelect=false;
        Check("PDF key rejects traversal",LegalPdfView.Metadata("../terms_vi")==null);
        foreach(string key in new[]{"privacy_vi","privacy_en","terms_vi","terms_en"})
        {
            var metadata=LegalPdfView.Metadata(key);Check(key+" manifest available",metadata!=null);App.Profile.locale=key.EndsWith("_vi")?"vi":"en";
            typeof(VlabApp).GetMethod("ShowLegal",Flags).Invoke(App,new object[]{key.StartsWith("privacy")?"privacy":"terms",false});yield return null;yield return new WaitForSecondsRealtime(.5f);
            var view=App.GetComponentInChildren<LegalPdfView>();Check(key+" actual UI ready at first page",view&&view.Ready&&view.PageIndex==0&&view.PageCount==metadata.pageCount);
            using(var sha=SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath,"Legal",metadata.sourceFile)))).Replace("-","").ToLowerInvariant();Check(key+" original PDF hash unchanged",hash==metadata.sourceSha256);}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Evidence/FinalPass/pdf-"+key+".png"));
            for(int i=0;i<metadata.pageCount;i++)
            {
                Check(key+" page "+(i+1)+" loads",view.Ready&&view.CurrentTexture.name=="page-"+(i+1).ToString("D4"));
                var path=AssetDatabase.GetAssetPath(view.CurrentTexture);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Check(key+" page "+(i+1)+" text texture lossless",importer.textureCompression==TextureImporterCompression.Uncompressed&&!importer.mipmapEnabled);
                if(i+1<metadata.pageCount){Press(view,"next");yield return null;}
            }
            Check(key+" last page disables next",!Button(view,"next").interactable);Press(view,"zoom in");Check(key+" zoom enlarges actual page",view.Zoom==1.5f);Press(view,"down");Press(view,"right");Press(view,"zoom out");
            while(view.PageIndex>0){Press(view,"previous");yield return null;}Check(key+" returns to first page",!Button(view,"previous").interactable);
            view.gameObject.SetActive(false);Check(key+" releases page while hidden",!view.CurrentTexture);view.gameObject.SetActive(true);yield return null;Check(key+" reloads after showing",view.Ready);
            App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.3f);Check(key+" persists into Editor VR simulation",view.Ready);App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.3f);Check(key+" restores phone page",view.Ready);
        }
    }
}
#endif
