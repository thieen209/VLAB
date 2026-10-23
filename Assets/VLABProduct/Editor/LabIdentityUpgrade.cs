#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

public static class LabIdentityUpgrade
{
    const string Output="Evidence/FinalPass/LabIdentity";
    const string Assets="Assets/Labs/Upgrade/LabIdentity/";
    static readonly string[] Rooms={"Physics","Chemistry","Biology","Engineering"};
    static readonly string[] Colors={"436F91","397F7C","6C8257","9A7647"};
    static Color Hex(string code){ColorUtility.TryParseHtmlString("#"+code,out var color);return color;}
    public static void Run()
    {
        Directory.CreateDirectory(Output+"/OriginalScenes");var lines=new List<string>();
        try
        {
            for(int i=0;i<Rooms.Length;i++)
            {
                string room=Rooms[i],path="Assets/Labs/Scenes/"+room+".unity",backup=Output+"/OriginalScenes/"+room+".unity";
                if(!File.Exists(backup))File.Copy(path,backup);
                var scene=EditorSceneManager.OpenScene(path);
                var previous=GameObject.Find("Subject learning wall");if(previous)UnityEngine.Object.DestroyImmediate(previous);
                var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
                int colliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
                int fixtures=UnityEngine.Object.FindObjectsByType<LabFixture>(FindObjectsSortMode.None).Length;
                var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name.Contains("Learner"));
                var originalPosition=camera.transform.position;var originalRotation=camera.transform.rotation;float originalFov=camera.fieldOfView;
                var accent=Finish(room+" casework",Hex(Colors[i]));
                var wall=Finish(room+" mineral wall",Color.Lerp(Hex(Colors[i]),Hex("F4F3EB"),.82f));
                var frame=Finish(room+" frame",Hex("394A50"));int tinted=0;
                foreach(var r in renderers)
                {
                    var materials=r.sharedMaterials;bool changed=false;
                    for(int m=0;m<materials.Length;m++)
                    {
                        if(!materials[m])continue;string name=materials[m].name;
                        if(name==room+"Finish"||name=="ChemistrySage"||name==room+" casework")
                        {materials[m]=accent;changed=true;tinted++;}
                        if(r.name.StartsWith("Wall-north")||((i==2||i==3)&&r.name.StartsWith("Wall-east")))
                        {materials[m]=wall;changed=true;}
                    }
                    if(changed){r.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                }
                if(tinted==0)throw new Exception(room+" has no identifiable casework");
                string artPath=Assets+room+".png";AssetDatabase.ImportAsset(artPath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(artPath);importer.mipmapEnabled=true;importer.isReadable=false;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ETC2_RGB4,compressionQuality=70});importer.SaveAndReimport();
                var printed=Finish(room+" learning print",Color.white);printed.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);EditorUtility.SetDirty(printed);
                var root=new GameObject("Subject learning wall").transform;
                root.position=i==0?new Vector3(2.55f,1.77f,3.56f):i==1?new Vector3(1.65f,1.83f,3.56f):i==2?new Vector3(4.56f,1.84f,2.02f):new Vector3(4.16f,1.84f,2.05f);
                root.rotation=Quaternion.Euler(0,i<2?0:90,0);
                float width=i==1?2f:2.2f,height=width*.5f;
                var backing=GameObject.CreatePrimitive(PrimitiveType.Cube);backing.name="Slim aluminium print frame";backing.transform.SetParent(root,false);backing.transform.localPosition=new Vector3(0,0,.012f);backing.transform.localScale=new Vector3(width+.04f,height+.04f,.028f);backing.GetComponent<Renderer>().sharedMaterial=frame;UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());
                var print=GameObject.CreatePrimitive(PrimitiveType.Quad);print.name="Scientific wall illustration";print.transform.SetParent(root,false);print.transform.localPosition=new Vector3(0,0,-.008f);print.transform.localScale=new Vector3(width,height,1);print.GetComponent<Renderer>().sharedMaterial=printed;UnityEngine.Object.DestroyImmediate(print.GetComponent<Collider>());
                foreach(var r in root.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
                Physics.SyncTransforms();
                if(UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length!=colliders||UnityEngine.Object.FindObjectsByType<LabFixture>(FindObjectsSortMode.None).Length!=fixtures)throw new Exception(room+" changed interaction geometry");
                if(root.GetComponentsInChildren<Collider>().Length!=0||root.position.y-height/2<1.1f)throw new Exception(room+" wall display obstructs worktop");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                // Capture after saving so runtime-only brand repair and camera framing never persist in the scene.
                RoomLighting.Apply(2);RoomBranding.Apply(scene,true);
                camera.transform.position=i==1?new Vector3(2.5f,1.6f,.82f):new Vector3(1.15f,1.6f,.85f);
                camera.transform.LookAt(i<2?new Vector3(2.35f,1.45f,3.25f):new Vector3(3.2f,1.45f,2.8f));camera.fieldOfView=78;
                LabPanoramaCapture.CaptureReview(camera,Output+"/"+room+"-room.png");
                camera.transform.position=root.position-root.forward*2.4f;camera.transform.LookAt(root.position);camera.fieldOfView=58;
                LabPanoramaCapture.CaptureReview(camera,Output+"/"+room+"-detail.png");
                camera.transform.SetPositionAndRotation(originalPosition,originalRotation);camera.fieldOfView=originalFov;
                lines.Add("PASS "+room+": distinct finish + scientific wall; original "+colliders+" colliders and "+fixtures+" fixtures preserved; no print collider; above working area; original camera saved; Android ETC2 texture.");
            }
            File.WriteAllLines(Output+"/checks.txt",lines);EditorApplication.Exit(0);
        }
        catch(Exception e){lines.Add("FAIL "+e);File.WriteAllLines(Output+"/checks.txt",lines);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Material Finish(string name,Color color)
    {
        string path=Assets+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.color=color;material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.19f);EditorUtility.SetDirty(material);return material;
    }
    public static void Inspect()
    {
        Directory.CreateDirectory(Output);var lines=new List<string>();
        foreach(var room in new[]{"Physics","Chemistry","Biology","Engineering"})
        {
            EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+room+".unity");lines.Add(room);
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(r.bounds.size.magnitude>.3f)lines.Add(r.name+" | center="+r.bounds.center.ToString("F2")+" | size="+r.bounds.size.ToString("F2")+" | mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")));
        }
        File.WriteAllLines(Output+"/geometry.txt",lines);EditorApplication.Exit(0);
    }
}
#endif
