using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class UpgradeChemistry
{
    const string Root="Assets/Labs/Upgrade/";
    static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+name+".mat");
    public static void Sink()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode before installing fixtures");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene before installation");
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene("Assets/Labs/Scenes/Chemistry.unity");
        if(GameObject.Find("Working wash station"))throw new Exception("Wash station already installed");
        var room=GameObject.Find("Chemistry Room");
        if(PrefabUtility.IsPartOfPrefabInstance(room))PrefabUtility.UnpackPrefabInstance(room,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        foreach(var t in room.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("CHEM-WASH")||t.name.StartsWith("Tap-")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        var sink=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"ReusedSink.fbx"));sink.name="Working wash station";
        sink.transform.SetPositionAndRotation(new Vector3(.325f,0,2.15f),Quaternion.Euler(0,90,0));
        foreach(var r in sink.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>Material(m.name.StartsWith("Top")?"Top":m.name.StartsWith("Metal")?"Metal":m.name.StartsWith("Sage")?"Sage":"Oak")).ToArray();
            r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
        }
        foreach(var h in sink.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("SinkHinge")).ToArray())
        {
            var pivot=new GameObject(h.name+" Motion").transform;pivot.SetParent(sink.transform,false);pivot.position=h.position;
            foreach(var child in h.Cast<Transform>().ToArray())child.SetParent(pivot,true);
            var f=pivot.gameObject.AddComponent<LabFixture>();f.label="Open / close sink cabinet";f.movingPart=pivot;f.openAngles=new Vector3(0,h.name.EndsWith("Left")?100:-100,0);
        }
        var controller=sink.AddComponent<LabSink>();
        var waterMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));waterMat.name="Water";waterMat.SetColor("_BaseColor",new Color(.19f,.63f,.73f,.6f));waterMat.SetFloat("_Smoothness",.92f);waterMat.SetFloat("_Surface",1);waterMat.SetFloat("_SrcBlend",5);waterMat.SetFloat("_DstBlend",10);waterMat.SetFloat("_ZWrite",0);waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");waterMat.renderQueue=3000;AssetDatabase.CreateAsset(waterMat,Root+"Materials/Water.mat");
        controller.water=Primitive("Basin water",PrimitiveType.Cube,sink.transform,new Vector3(0,.675f,0),new Vector3(.51f,.003f,.37f),waterMat);
        controller.stream=Primitive("Running water",PrimitiveType.Cylinder,sink.transform,new Vector3(0,.87f,-.025f),new Vector3(.011f,.2f,.011f),waterMat);
        var lever=sink.GetComponentsInChildren<Transform>().First(t=>t.name=="Mixer lever");
        var leverPivot=new GameObject("Lever control").transform;leverPivot.SetParent(sink.transform,false);leverPivot.position=lever.position;lever.SetParent(leverPivot,true);controller.lever=leverPivot;
        var tap=leverPivot.gameObject.AddComponent<LabFixture>();tap.action=LabFixture.Action.Tap;tap.label="Tap on / off";tap.sink=controller;
        var tapHit=leverPivot.gameObject.AddComponent<BoxCollider>();tapHit.size=new Vector3(.12f,.09f,.09f);
        var drain=new GameObject("Drain control");drain.transform.SetParent(sink.transform,false);drain.transform.localPosition=new Vector3(0,.69f,0);var drainHit=drain.AddComponent<BoxCollider>();drainHit.size=new Vector3(.1f,.04f,.1f);var plug=drain.AddComponent<LabFixture>();plug.action=LabFixture.Action.Drain;plug.label="Plug / unplug drain";plug.sink=controller;
        controller.ApplyVisuals();
        var cam=Camera.main;if(!cam.GetComponent<LabInteractor>())cam.gameObject.AddComponent<LabInteractor>();
        PrefabUtility.SaveAsPrefabAsset(sink,Root+"WorkingSink.prefab");
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CaptureSink();
        Debug.Log("Donor sink installed, saved and captured");
    }
    static Transform Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material mat)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;return o.transform;
    }
    public static void CaptureSink()
    {
        var cam=Camera.main;var p=cam.transform.position;var q=cam.transform.rotation;var f=cam.fieldOfView;
        cam.transform.position=new Vector3(1.7f,1.5f,1.55f);cam.transform.LookAt(new Vector3(.325f,.63f,2.15f));cam.fieldOfView=57;
        BuildLabs.Capture(cam,"../Evidence/Chemistry-upgrade-sink.png",1200,1000);
        cam.transform.SetPositionAndRotation(p,q);cam.fieldOfView=f;
    }
    public static void CheckSink()
    {
        var sink=UnityEngine.Object.FindFirstObjectByType<LabSink>();if(!sink)throw new Exception("Missing sink");
        var initialLevel=sink.level;var initialTap=sink.tapOn;var initialDrain=sink.drainClosed;
        sink.level=0;sink.tapOn=true;sink.drainClosed=true;sink.Step(8);if(sink.level<.5f||!sink.stream.gameObject.activeSelf)throw new Exception("Fill failed");
        sink.Step(100);if(sink.level!=1)throw new Exception("Overflow limit failed");
        sink.ToggleTap();if(sink.tapOn)throw new Exception("Tap off failed");sink.ToggleDrain();sink.Step(15);if(sink.level!=0||sink.stream.gameObject.activeSelf)throw new Exception("Drain failed");
        foreach(var f in sink.GetComponentsInChildren<LabFixture>().Where(f=>f.action==LabFixture.Action.Hinge))
        {
            f.Initialize();var q=f.movingPart.localRotation;f.Activate();f.Animate(2);if(Quaternion.Angle(q,f.movingPart.localRotation)<90)throw new Exception("Door failed to open");f.Activate();f.Animate(2);if(Quaternion.Angle(q,f.movingPart.localRotation)>.1f)throw new Exception("Door failed to close");
        }
        sink.level=initialLevel;sink.tapOn=initialTap;sink.drainClosed=initialDrain;sink.ApplyVisuals();
        File.WriteAllText("../Evidence/sink-checks.txt","PASS: fill, tap off, drain, overflow limit, both cabinet hinges open and close. Component-level checks; pointer interaction and visual inspection remain separate.");
        Debug.Log("Sink component checks passed");
    }
}
