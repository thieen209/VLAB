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
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Vlab.Product;

[InitializeOnLoad]
public static class LessonPreviewBake
{
    static LessonPreviewBake(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.LessonPhotos",false)){SessionState.SetBool("VLAB.LessonPhotos",false);new GameObject("Lesson photos",typeof(LessonPhotoProbe));}};}
    public static void Run(){SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","LessonPhotos-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.LessonPhotos",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class LessonPhotoProbe:MonoBehaviour
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Folder="Assets/VLABProduct/Resources/UI/Lessons";
    VlabApp App=>VlabApp.Instance;readonly List<string> receipt=new List<string>();
    object Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Private).Invoke(App,args);
    void Set(string name,object value)=>typeof(VlabApp).GetField(name,Private).SetValue(App,value);
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Folder);Directory.CreateDirectory("Evidence/DesignUpgrade");bool failed=false;var tasks=new Stack<IEnumerator>();tasks.Push(Run());
        while(tasks.Count>0){bool next=false;object value=null;try{next=tasks.Peek().MoveNext();if(next)value=tasks.Peek().Current;}catch(Exception e){receipt.Add("FAIL "+e);failed=true;break;}if(!next){tasks.Pop();continue;}if(value is IEnumerator nested&&!(value is CustomYieldInstruction))tasks.Push(nested);else yield return value;}
        File.WriteAllLines("Evidence/DesignUpgrade/preview-provenance.txt",receipt);SessionState.SetString("VLAB.TestProfile","");EditorApplication.Exit(failed?1:0);
    }
    IEnumerator Run()
    {
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;if(!App||App.Route!="home")throw new Exception("Boot did not complete");
        App.Profile.quickStartSeen=true;App.Profile.settings.reducedMotion=true;App.Profile.settings.hints=false;App.Profile.settings.SetGraphicsTier(2);Call("ApplySettings");
        var catalog=(BriefCatalog)typeof(VlabApp).GetField("catalog",Private).GetValue(App);
        string filter=Environment.GetEnvironmentVariable("VLAB_PHOTO_IDS");var selected=catalog.items.Where(x=>string.IsNullOrEmpty(filter)||filter.Split(',').Contains(x.id)).OrderBy(x=>x.id).ToArray();
        foreach(var brief in selected)
        {
            Set("subject",brief.id.Substring(0,1));Set("selected",brief);App.ShowBrowser();yield return (IEnumerator)Call("LoadRoom",true,false,null);
            if(App.Route!="activity"||!App.Apparatus)throw new Exception(brief.id+" missing actual room station");
            var rig=App.Apparatus;for(int n=0;n<60&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());if(!rig.InstallSelected())throw new Exception(brief.id+" cannot assemble preview fixture");}
            rig.RefreshConfiguration();Call("ClearGuideCue");App.Preview.Clear();yield return new WaitForSecondsRealtime(.4f);
            Capture(brief.id,rig);receipt.Add(brief.id+" | "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" | actual assembled station on its authored workbench | "+string.Join(", ",rig.items.Select(x=>x.assetId))+" | isolated photographic fixture, no user results");
            File.WriteAllLines("Evidence/DesignUpgrade/preview-progress.txt",receipt);
        }
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles(Folder,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ETC2_RGB4,compressionQuality=70});importer.SaveAndReimport();
        }
        if(receipt.Count!=selected.Length)throw new Exception("Missing photographs");
    }
    void Capture(string id,ApparatusRig rig)
    {
        // Frame the launcher on the workbench; the landing mat remains at its authored floor position.
        var renderers=rig.items.Where(item=>id!="P02"||item.assetId!="target").SelectMany(x=>x.GetComponentsInChildren<Renderer>()).Where(x=>(x is MeshRenderer||x is SkinnedMeshRenderer)&&x.enabled&&x.gameObject.activeInHierarchy&&x.bounds.size.magnitude>0).ToArray();if(renderers.Length==0)throw new Exception(id+" has no visible instruments");
        Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        File.WriteAllLines("Evidence/DesignUpgrade/"+id+"-photo-bounds.txt",renderers.Select(x=>x.name+" | "+x.bounds));
        bounds.Encapsulate(new Vector3(bounds.center.x,rig.Worktop.y-.08f,bounds.center.z));
        var camera=new GameObject("Cinematic bench photograph").AddComponent<Camera>();camera.CopyFrom(App.View);camera.enabled=false;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.targetTexture=null;
        var data=camera.gameObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask=1<<30;camera.allowHDR=true;
        var volume=new GameObject("Photographic tone").AddComponent<Volume>();volume.gameObject.layer=30;volume.isGlobal=true;volume.priority=100;var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);profile.Add<ColorAdjustments>().contrast.Override(6);volume.sharedProfile=profile;camera.orthographic=false;camera.fieldOfView=42;camera.aspect=2f;camera.nearClipPlane=.03f;camera.farClipPlane=40;camera.cullingMask&=~(1<<5);
        var rotation=Quaternion.LookRotation(-new Vector3(.85f,.7f,-1.4f).normalized,Vector3.up);float tangent=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f),distance=.3f;
        foreach(var renderer in renderers)for(int i=0;i<8;i++){var b=renderer.bounds;var point=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var v=Quaternion.Inverse(rotation)*(point-bounds.center);distance=Mathf.Max(distance,Mathf.Abs(v.x)/(tangent*camera.aspect)-v.z,Mathf.Abs(v.y)/tangent-v.z);}
        camera.transform.SetPositionAndRotation(bounds.center-rotation*Vector3.forward*(distance*1.18f+.12f),rotation);
        // A temporary photographic key adds contact shadows; it never ships as an extra mobile light.
        var key=new GameObject("Photographic soft key").AddComponent<Light>();key.type=LightType.Spot;key.range=8;key.spotAngle=100;key.innerSpotAngle=65;key.intensity=1.1f;key.color=new Color(1,.96f,.9f);key.shadows=LightShadows.Soft;key.shadowBias=.001f;key.shadowNormalBias=.005f;
        key.transform.position=bounds.center+new Vector3(-.5f,1.5f,-.6f);key.transform.LookAt(bounds.center);
        var target=RenderTexture.GetTemporary(1600,800,24);var previous=RenderTexture.active;Texture2D pixels=null;
        try{LabPanoramaCapture.RenderMono(camera,target);RenderTexture.active=target;pixels=new Texture2D(1600,800,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,800),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/"+id+".png",pixels.EncodeToPNG());}
        finally{RenderTexture.active=previous;if(pixels)Destroy(pixels);RenderTexture.ReleaseTemporary(target);Destroy(camera.gameObject);Destroy(key.gameObject);Destroy(volume.gameObject);Destroy(profile);}
    }
}
#endif
