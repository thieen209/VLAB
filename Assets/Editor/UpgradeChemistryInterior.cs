using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class UpgradeChemistryInterior
{
    static Material Mat(string key)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Labs/Upgrade/Materials/"+key+".mat");
    static void SizeAxis(Transform t,int worldAxis,float factor)
    {
        var dir=worldAxis==0?Vector3.right:worldAxis==1?Vector3.up:Vector3.forward;
        var local=t.InverseTransformDirection(dir);int a=Mathf.Abs(local.x)>Mathf.Abs(local.y)?0:1;if(Mathf.Abs(local.z)>Mathf.Abs(local[a]))a=2;
        var s=t.localScale;s[a]*=factor;t.localScale=s;
    }
    static void Panel(string name,Transform parent,Vector3 pos,Vector3 size,Material m)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;
    }
    public static void Run()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Chemistry")throw new Exception("Open Chemistry first");
        if(GameObject.Find("Chemistry fitted furniture"))throw new Exception("Furniture already upgraded");
        var root=new GameObject("Chemistry fitted furniture").transform;
        var room=GameObject.Find("Chemistry Room");var renderers=room.GetComponentsInChildren<MeshRenderer>();
        foreach(var front in renderers.Where(r=>r.name.Contains("-door")))
        {
            bool west=front.name.StartsWith("CHEM-GLASS");int axis=west?2:0;float middle=west?.725f:3.45f;
            var b=front.bounds;bool low=b.center[axis]<middle;SizeAxis(front.transform,axis,(b.size[axis]+.022f)/b.size[axis]);
            var pivot=new GameObject(front.name+" hinge").transform;pivot.SetParent(root,false);var p=b.center;p[axis]+= (low?-1:1)*(b.size[axis]+.022f)/2;pivot.position=p;
            front.transform.SetParent(pivot,true);
            string prefix=front.name.Substring(0,front.name.IndexOf("-door"));
            var pull=renderers.Where(r=>r.name.StartsWith(prefix+"-pull")&&!r.GetComponentInParent<LabFixture>()).OrderBy(r=>Vector3.Distance(r.bounds.center,b.center)).First();pull.transform.SetParent(pivot,true);
            var f=pivot.gameObject.AddComponent<LabFixture>();f.movingPart=pivot;f.label="Open / close cabinet";f.openAngles=new Vector3(0,low?100:-100,0);
            foreach(var t in pivot.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        }
        foreach(var front in renderers.Where(r=>r.name.Contains("-drawer")))
        {
            var b=front.bounds;bool south=front.name.StartsWith("CHEM-REAGENTS");float sign=south?1:-1;
            var drawer=new GameObject(front.name+" sliding box").transform;drawer.SetParent(root,false);drawer.position=b.center;
            front.transform.SetParent(drawer,true);string prefix=front.name.Substring(0,front.name.IndexOf("-drawer"));
            var pull=renderers.Where(r=>r.name.StartsWith(prefix+"-pull")&&!r.GetComponentInParent<LabFixture>()).OrderBy(r=>Vector3.Distance(r.bounds.center,b.center)).First();pull.transform.SetParent(drawer,true);
            float w=b.size.x-.035f,h=b.size.y-.025f,d=.35f;
            Panel("Drawer bottom",drawer,new Vector3(0,-h/2,-sign*d/2),new Vector3(w,.012f,d),Mat("Oak"));
            foreach(int side in new[]{-1,1})Panel("Drawer side",drawer,new Vector3(side*(w-.012f)/2,0,-sign*d/2),new Vector3(.012f,h,d),Mat("Oak"));
            Panel("Drawer back",drawer,new Vector3(0,0,-sign*d),new Vector3(w,h,.012f),Mat("Oak"));
            var f=drawer.gameObject.AddComponent<LabFixture>();f.action=LabFixture.Action.Drawer;f.movingPart=drawer;f.openOffset=new Vector3(0,0,sign*.26f);f.label="Open / close drawer";
            foreach(var t in drawer.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        }
        // Fixed shelves must not intersect the newly fitted drawer boxes.
        foreach(var r in renderers.Where(r=>(r.name.StartsWith("CHEM-REAGENTS-shelf")||r.name.StartsWith("CHEM-WET-end-shelf"))))UnityEngine.Object.DestroyImmediate(r.gameObject);
        foreach(var r in renderers.Where(r=>r&&r.name.EndsWith("-top")))r.sharedMaterial=Mat("Top");
        foreach(var r in renderers.Where(r=>r&&r.name.Contains("-leg"))) {SizeAxis(r.transform,0,1.2f);SizeAxis(r.transform,2,1.2f);}
        // Keep each room's palette isolated while the Chemistry treatment is under review.
        var wall=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Labs/Materials/Wall.mat"));wall.name="Warm plaster";wall.SetColor("_BaseColor",new Color(.84f,.84f,.79f));AssetDatabase.CreateAsset(wall,"Assets/Labs/Upgrade/Materials/WarmPlaster.mat");
        foreach(var r in renderers.Where(r=>r&&(r.name.StartsWith("Wall-")||r.name.StartsWith("Ceiling"))))r.sharedMaterial=wall;
        foreach(var r in renderers.Where(r=>r&&r.name.StartsWith("Window-jamb")))SizeAxis(r.transform,1,1.045f);
        foreach(var r in renderers.Where(r=>r&&r.name.StartsWith("Window-rail")))SizeAxis(r.transform,2,1.06f);
        Panel("Window oak sill",root,new Vector3(4.12f,1.18f,2.325f),new Vector3(.24f,.035f,.76f),Mat("Oak"));
        var skyMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));skyMat.SetColor("_BaseColor",new Color(.73f,.85f,.91f));AssetDatabase.CreateAsset(skyMat,"Assets/Labs/Upgrade/Materials/WindowSky.mat");
        Panel("Daylight beyond glazing",root,new Vector3(4.6f,1.7f,2.325f),new Vector3(.02f,2.4f,2),skyMat);
        RenderSettings.ambientSkyColor=new Color(.72f,.77f,.83f);RenderSettings.ambientEquatorColor=new Color(.60f,.60f,.58f);RenderSettings.ambientGroundColor=new Color(.34f,.34f,.32f);
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(l.type==LightType.Spot){l.intensity=2.1f;l.shadowStrength=.65f;}else l.intensity=.5f;}
        var sun=new GameObject("Soft east daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(38,270,0);sun.intensity=1.4f;sun.color=new Color(1,.97f,.91f);sun.shadows=LightShadows.Soft;sun.shadowBias=.02f;sun.shadowNormalBias=.12f;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();var grading=profile.Add<ColorAdjustments>();grading.postExposure.Override(.35f);var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);AssetDatabase.CreateAsset(profile,"Assets/Labs/Upgrade/Materials/ChemistryGrading.asset");foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);
        var volume=new GameObject("Chemistry daylight grading").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var probe=new GameObject("Chemistry reflection").AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(2.1f,1.5f,1.8f);probe.size=new Vector3(4.2f,2.8f,3.6f);probe.boxProjection=true;probe.resolution=128;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.RenderProbe();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
}
