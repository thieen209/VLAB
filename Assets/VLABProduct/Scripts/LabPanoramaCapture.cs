#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Vlab.Product;

public static class LabPanoramaCapture
{
    const string Folder="Assets/VLABProduct/Resources/UI/";
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Capture requires Edit Mode.");
        if(!Application.isBatchMode&&Enumerable.Range(0,SceneManager.sceneCount).Any(index=>SceneManager.GetSceneAt(index).isDirty))throw new InvalidOperationException("Save open scene changes before taking the room photographs.");
        var setup=EditorSceneManager.GetSceneManagerSetup().Where(scene=>!string.IsNullOrEmpty(scene.path)&&File.Exists(scene.path)).ToArray();var receipt=new List<string>{"Static monoscopic room photographs; no runtime scene rendering.","Unity "+Application.unityVersion};double began=EditorApplication.timeSinceStartup;
        try
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Evidence/PhotoReview");
            foreach(string room in new[]{"Home","Physics","Chemistry","Biology","Engineering"})
            {
                string sceneRoom=room=="Home"?"Physics":room;
                string path="Assets/Labs/Scenes/"+sceneRoom+".unity";EditorSceneManager.OpenScene(path,OpenSceneMode.Single);RoomLighting.Apply();RoomBranding.Apply(SceneManager.GetActiveScene());RoomBrandingChecks.Run((label,passed)=>{if(!passed)throw new InvalidOperationException(room+" branding: "+label);receipt.Add("PASS "+room+" branding: "+label);},SceneManager.GetActiveScene());
                // Home supplies the foreground brand; avoid a second logo directly behind it in these preview photographs.
                RoomBranding.SetVisible(SceneManager.GetActiveScene(),false);
                var learner=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(camera=>camera.name.Contains("Learner"));
                var floor=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(item=>item.name.StartsWith("Floor",StringComparison.Ordinal)).OrderByDescending(item=>item.bounds.size.x*item.bounds.size.z).First().bounds;
                Vector3 position=new Vector3(floor.min.x+.8f,1.65f,floor.min.z+.75f);
                float yaw=Quaternion.LookRotation(new Vector3(floor.min.x+floor.size.x*.67f,1.65f,floor.min.z+floor.size.z*.73f)-position).eulerAngles.y;
                foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
                var captureObject=new GameObject("Temporary static panorama capture",typeof(Camera),typeof(UniversalAdditionalCameraData));var capture=captureObject.GetComponent<Camera>();capture.CopyFrom(learner);capture.enabled=false;capture.stereoTargetEye=StereoTargetEyeMask.None;capture.allowHDR=false;capture.allowMSAA=false;capture.nearClipPlane=.04f;capture.farClipPlane=40;capture.targetTexture=null;capture.transform.SetPositionAndRotation(position,Quaternion.identity);capture.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
                var cube=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32){dimension=TextureDimension.Cube,name="Temporary room cubemap"};var panorama=new RenderTexture(2048,1024,0,RenderTextureFormat.ARGB32){name="Temporary room panorama"};Texture2D pixels=null;var active=RenderTexture.active;
                try
                {
                    capture.transform.rotation=Quaternion.Euler(5,yaw,0);string review="Evidence/PhotoReview/"+room+".png";CaptureReview(capture,review);capture.transform.rotation=Quaternion.identity;
                    if(room!="Home")
                    {
                        string still=Folder+"Preview_"+room+".png";File.Copy(review,still,true);AssetDatabase.ImportAsset(still,ImportAssetOptions.ForceSynchronousImport);
                        var photoImporter=(TextureImporter)AssetImporter.GetAtPath(still);photoImporter.textureType=TextureImporterType.Default;photoImporter.isReadable=false;photoImporter.mipmapEnabled=true;photoImporter.maxTextureSize=1024;photoImporter.wrapMode=TextureWrapMode.Clamp;photoImporter.filterMode=FilterMode.Trilinear;
                        photoImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ETC2_RGB4,compressionQuality=50});photoImporter.SaveAndReimport();
                        receipt.Add("PASS "+room+" browser still uses the same current-project capture pose as its panorama;1024px Android ETC2 RGB.");
                    }
                    cube.Create();panorama.Create();if(!capture.RenderToCubemap(cube,63,Camera.MonoOrStereoscopicEye.Mono))throw new InvalidOperationException("RenderToCubemap failed for "+room);
                    cube.ConvertToEquirect(panorama,Camera.MonoOrStereoscopicEye.Mono);BlurPhotograph(panorama);RenderTexture.active=panorama;pixels=new Texture2D(2048,1024,TextureFormat.RGB24,false,false);pixels.ReadPixels(new Rect(0,0,2048,1024),0,0);pixels.Apply();
                    string image=Folder+"Panorama_"+room+".png";File.WriteAllBytes(image,pixels.EncodeToPNG());File.WriteAllText(Folder+"Panorama_"+room+"_pose.json",JsonUtility.ToJson(new LabPanoramaPreview.CapturePose{room=room,position=position,yaw=yaw},true));
                    AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(image);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.None;importer.isReadable=false;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Compressed;
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ETC2_RGB4,compressionQuality=50});importer.SaveAndReimport();
                    var imported=AssetDatabase.LoadAssetAtPath<Texture2D>(image);var android=importer.GetPlatformTextureSettings("Android");
                    if(!imported||imported.width!=2048||imported.height!=1024||importer.isReadable||!importer.mipmapEnabled||!android.overridden||android.maxTextureSize!=2048||android.format!=TextureImporterFormat.ETC2_RGB4)throw new InvalidOperationException("Panorama import contract failed: "+room);
                    receipt.Add("PASS "+room+" | source="+path+" | learner="+position.ToString("F3")+" | sourceYaw="+yaw.ToString("0.0",CultureInfo.InvariantCulture)+" |2048x1024, Android ETC2 RGB, mipmaps, no CPU readback copy | pngBytes="+new FileInfo(image).Length);
                }
                finally{RenderTexture.active=active;if(pixels)UnityEngine.Object.DestroyImmediate(pixels);cube.Release();panorama.Release();UnityEngine.Object.DestroyImmediate(cube);UnityEngine.Object.DestroyImmediate(panorama);UnityEngine.Object.DestroyImmediate(captureObject);}
            }
            AssetDatabase.Refresh();receipt.Insert(0,"PASS");
        }
        catch(Exception error){receipt.Insert(0,"FAIL");receipt.Add(error.ToString());throw;}
        finally
        {
            if(setup.Length>0){if(!setup.Any(scene=>scene.isActive))setup[0].isActive=true;EditorSceneManager.RestoreSceneManagerSetup(setup);}else EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");
            Directory.CreateDirectory("Evidence");receipt.Add("Elapsed seconds: "+(EditorApplication.timeSinceStartup-began).ToString("0.0",CultureInfo.InvariantCulture));File.WriteAllLines("Evidence/panorama-capture.txt",receipt);
        }
    }
    static void BlurPhotograph(RenderTexture photograph)
    {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/VLABProduct/Editor/LabPhotoBlur.shader");
        if(!shader||!shader.isSupported)throw new InvalidOperationException("Missing or unsupported photograph blur shader.");
        var material=new Material(shader);var temporary=RenderTexture.GetTemporary(photograph.width,photograph.height,0,RenderTextureFormat.ARGB32);
        try
        {
            photograph.filterMode=temporary.filterMode=FilterMode.Bilinear;
            // Bake the blur once; the phone still samples a single static room texture.
            for(int pass=0;pass<4;pass++)
            {
                material.SetVector("_Direction",new Vector4(1f/photograph.width,0,0,0));Graphics.Blit(photograph,temporary,material);
                material.SetVector("_Direction",new Vector4(0,1f/photograph.height,0,0));Graphics.Blit(temporary,photograph,material);
            }
        }
        finally{RenderTexture.ReleaseTemporary(temporary);UnityEngine.Object.DestroyImmediate(material);}
    }
    public static void CaptureReview(Camera camera,string path)
    {
        var target=RenderTexture.GetTemporary(1600,1000,24);var active=RenderTexture.active;Texture2D pixels=null;
        try{RenderMono(camera,target);RenderTexture.active=target;pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=active;if(pixels)UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.ReleaseTemporary(target);}
    }
    // URP's supported off-loop API; captures are explicitly monoscopic.
    // https://docs.unity3d.com/6000.0/Documentation/Manual/urp/User-Render-Requests.html
    public static void RenderMono(Camera camera,RenderTexture target)
    {
        var oldTarget=camera.targetTexture;
        try
        {
            var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target};
            if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("Active pipeline does not support a single-camera render request.");
            RenderPipeline.SubmitRenderRequest(camera,request);
        }
        finally{camera.targetTexture=oldTarget;}
    }
    // Run on the real Home preview component after capture assets have been imported.
    public static IEnumerator Verify(LabPanoramaPreview preview,Camera view,Action<string,bool> check)
    {
        var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).handle).ToArray();var position=view.transform.position;var rotation=view.transform.rotation;
        preview.Initialize(view);check("Static panorama contains no collider or camera",preview.Shell.GetComponentsInChildren<Collider>(true).Length==0&&preview.Shell.GetComponentsInChildren<Camera>(true).Length==0);
        check("Static panorama geometry stays within1600 triangles",preview.TriangleCount<=1600&&preview.TriangleCount>0);
        foreach(string room in new[]{"Physics","Chemistry","Biology","Engineering"})
        {
            check("Captured panorama exists: "+room,preview.Show(room));yield return new WaitForSecondsRealtime(.25f);
            check("Hover fades in actual captured texture: "+room,preview.CurrentTexture&&preview.CurrentTexture.name=="Panorama_"+room&&preview.Alpha>.99f);
            check("Panorama GPU texture has a bounded footprint: "+room,Profiler.GetRuntimeMemorySizeLong(preview.CurrentTexture)<16*1024*1024);
            var shell=preview.Shell;Quaternion fixedRotation=shell.rotation;view.transform.rotation*=Quaternion.Euler(0,25,0);view.transform.position+=Vector3.right*.05f;yield return null;yield return null;
            check("Preview follows translation without following head yaw",Quaternion.Angle(fixedRotation,shell.rotation)<.01f&&Vector3.Distance(shell.position,view.transform.position)<.001f);view.transform.SetPositionAndRotation(position,rotation);
        }
        preview.Clear();yield return new WaitForSecondsRealtime(.25f);check("Clear focus fades and releases photograph",preview.Alpha==0&&preview.CurrentTexture==null);
        check("Static previews never load or replace a scene",scenes.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).handle)));
    }
}
#endif
