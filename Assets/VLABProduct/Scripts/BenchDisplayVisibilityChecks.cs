#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

public static class BenchDisplayVisibilityChecks
{
    public static void Run(Action<string,bool> check)
    {
        var root=new GameObject("Isolated decorative bench visibility");
        GameObject Cube(string name,Vector3 position,Vector3 scale)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform);go.transform.position=position;go.transform.localScale=scale;return go;}
        try
        {
            var top=Cube("Test bench top",new Vector3(1000,.875f,1000),new Vector3(2,.05f,1));
            var display=Cube("Balance-body",new Vector3(1000,.95f,1000),new Vector3(.2f,.1f,.2f));
            var rim=Cube("Tray-rim.001",new Vector3(1000.3f,.92f,1000),new Vector3(.1f,.04f,.1f));
            var sink=Cube("Sink",new Vector3(999.5f,.95f,1000),new Vector3(.2f,.1f,.2f));
            var outside=Cube("Balance-pan",new Vector3(1003,.95f,1000),new Vector3(.2f,.1f,.2f));
            var inactive=Cube("Ready-tray",new Vector3(999.7f,.95f,1000),new Vector3(.2f,.1f,.2f));inactive.SetActive(false);
            var live=Cube("Balance-pan",new Vector3(1000.6f,.95f,1000),new Vector3(.2f,.1f,.2f));live.AddComponent<EquipmentHandle>();
            var visibility=root.AddComponent<BenchDisplayVisibility>();
            int count=visibility.ClearBench(new Vector3(1000,.905f,1000));
            check("Only two named active-bench display props are hidden, including their colliders",count==2&&!display.activeSelf&&!rim.activeSelf&&!display.GetComponent<Collider>().gameObject.activeInHierarchy);
            check("Worktop, sink, active lesson equipment, other benches and prior inactive states survive",top.activeSelf&&sink.activeSelf&&outside.activeSelf&&live.activeSelf&&!inactive.activeSelf);
            visibility.Restore();check("Leaving restores original active states",display.activeSelf&&rim.activeSelf&&!inactive.activeSelf);
            var sourceTop=top.GetComponent<Renderer>().sharedMaterials;visibility.SetSurface("C05",new Vector3(1000,.905f,1000));
            check("Temporary bench treatment owns separate materials",!ReferenceEquals(top.GetComponent<Renderer>().sharedMaterial,sourceTop[0]));
            visibility.Restore();check("Leaving restores the exact authored bench materials",sourceTop.SequenceEqual(top.GetComponent<Renderer>().sharedMaterials));
            visibility.ClearBench(new Vector3(1000,.905f,1000));visibility.enabled=false;
            check("Disabling scope restores props",display.activeSelf&&rim.activeSelf&&!inactive.activeSelf);
            var cord=new GameObject("Cord-only hint fixture",typeof(LineRenderer),typeof(EquipmentHandle));cord.transform.SetParent(root.transform);
            cord.transform.position=new Vector3(1000,1,1000);var line=cord.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;
            line.SetPosition(0,Vector3.zero);line.SetPosition(1,Vector3.up*.3f);line.startWidth=line.endWidth=.003f;
            var item=cord.GetComponent<EquipmentHandle>();item.assetId="cord";item.installed=true;
            var cue=GuideCue.ShowEquipment(root.transform,item,true);
            check("Line-only cord receives a real model outline without changing source",cue.AssertPresentationOnly(null,out _)&&cue.transform.Find("Object highlight").GetComponent<LineRenderer>().enabled&&line.GetPosition(1)==Vector3.up*.3f);
            cue.Dispose();var mesh=Cube("Visible source mesh",cord.transform.position,new Vector3(.03f,.03f,.03f));mesh.transform.SetParent(cord.transform,true);
            var originalMesh=mesh.GetComponent<MeshFilter>().sharedMesh;cue=GuideCue.ShowEquipment(root.transform,item,true);
            check("Mixed mesh and cord keeps the source mesh unobscured and unchanged",!cue.transform.Find("Object highlight").GetComponent<LineRenderer>().enabled&&mesh.GetComponent<MeshFilter>().sharedMesh==originalMesh);
            TrayGeometry(check);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void TrayGeometry(Action<string,bool> check)
    {
        var root=new GameObject("Isolated native contrast tray checks");
        try
        {
            var visibility=root.AddComponent<BenchDisplayVisibility>();var bench=new Bounds(new Vector3(2000,.875f,2000),new Vector3(2,.05f,1));
            var target=new Bounds(new Vector3(2000,.9495f,2000),new Vector3(.071f,.095f,.075f));
            var source=Resources.Load<GameObject>("Equipment/VLAB_tray");var sourceMeshes=source.GetComponentsInChildren<MeshFilter>().Select(mesh=>mesh.sharedMesh).ToArray();
            var sourceMaterials=source.GetComponentsInChildren<Renderer>().SelectMany(renderer=>renderer.sharedMaterials).ToArray();var sourceColours=sourceMaterials.Select(material=>material.color).ToArray();
            check("Original authored contrast tray fits a real-sized beaker footprint",visibility.TryAddContrastTray("C04",bench,target,Array.Empty<Bounds>(),out var tray));
            if(!tray)return;
            check("Tray keeps native dimensions, meshes and original source scale",tray.transform.localScale==Vector3.one&&sourceMeshes.SequenceEqual(tray.GetComponentsInChildren<MeshFilter>().Select(mesh=>mesh.sharedMesh))&&Mathf.Abs(tray.OuterBounds.size.y-.025f)<.001f);
            CheckTrayTone(tray,check);
            var floor=tray.Bay(tray.TargetBay);var seated=target;seated.center+=Vector3.up*(floor.max.y+BenchContrastTray.RestClearance-target.min.y);
            check("Tray floor is the authored four-millimetre socket plane",Mathf.Abs(floor.max.y-bench.max.y-.004f)<.0002f&&Mathf.Abs(tray.OuterBounds.min.y-bench.max.y)<.0002f);
            check("Glass seats on the actual tray floor instead of intersecting it",tray.CanSupport(seated)&&!tray.CanSupport(target)&&tray.BlocksPlacement(target));
            var divider=seated;divider.center=new Vector3(tray.OuterBounds.center.x,seated.center.y,tray.OuterBounds.center.z);
            check("Divider and rim space cannot masquerade as a supported bay",!tray.CanSupport(divider)&&tray.BlocksPlacement(divider));
            var collider=tray.GetComponentInChildren<MeshCollider>();Physics.SyncTransforms();
            check("Native floor collider supports real contact without entering default pointer raycasts",collider.enabled&&collider.gameObject.layer==2&&collider.Raycast(new Ray(floor.center+Vector3.up*.1f,Vector3.down),out var hit,.2f)&&Mathf.Abs(hit.point.y-floor.max.y)<.0005f);
            var tooWide=target;tooWide.size=new Vector3(.28f,.095f,.1f);
            check("Oversized groups are refused instead of stretching the source tray",!visibility.TryAddContrastTray("C04",bench,tooWide,Array.Empty<Bounds>(),out _));
            check("Occupied tray space is rejected",!visibility.TryAddContrastTray("C04",bench,target,new[]{new Bounds(target.center,new Vector3(1,.2f,1))},out _));
            visibility.Restore();check("Leaving removes owned trays and preserves all authored material colours",(!tray||!tray.gameObject.activeSelf)&&visibility.ContrastTrays.Count==0&&sourceMaterials.Select(material=>material.color).SequenceEqual(sourceColours));
            foreach(string id in LessonSpec.All.Keys)check("Contrast tray eligibility follows chemistry/biology scope "+id,BenchDisplayVisibility.SupportsContrastTray(id)==(id[0]=='C'||id[0]=='B'));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void CheckTrayTone(BenchContrastTray tray,Action<string,bool> check)
    {
        var material=tray.GetComponentsInChildren<Renderer>().Single(renderer=>renderer.name.EndsWith("_Tray_VIS",StringComparison.Ordinal)).sharedMaterial;
        var tone=material.GetColor("_BaseColor");
        check("Native tray uses a dark saturated blue URP base colour, distinct from the pale bench",tone.r<.09f&&tone.g<.2f&&tone.b>.2f&&tone.b>tone.r*2&&tone.a==1&&(!material.HasProperty("_Color")||material.GetColor("_Color")==tone));
        check("Tray finish remains matte and non-metallic",material.GetFloat("_Metallic")==0&&material.GetFloat("_Smoothness")<=.15f);
    }
    public static IEnumerator RunLessons(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        Run(check);int covered=0,totalTrays=0;
        foreach(string id in LessonSpec.All.Keys.Where(id=>id[0]=='C'||id[0]=='B').OrderBy(id=>id))
        {
            app.ShowHome();yield return null;
            yield return Press(app,"Subject "+id[0]);yield return Route(app,"browser");
            yield return Press(app,"Browser lesson "+id);yield return Press(app,"Browser start");yield return Route(app,"readiness");
            yield return Press(app,"Readiness enter");yield return Route(app,"activity");yield return null;yield return null;
            var rig=app.Apparatus;check(id+" uses authored room worktops",!rig.UsesSyntheticStorageSurface);
            check(id+" loose native instruments have disjoint supported footprints: "+LooseFailure(rig),rig.ValidateLooseLayout(out _));
            var visibility=rig.GetComponent<BenchDisplayVisibility>();var trays=visibility?visibility.ContrastTrays.ToArray():Array.Empty<BenchContrastTray>();totalTrays+=trays.Length;
            for(int n=0;n<40&&!rig.Ready;n++){var next=rig.NextInstallableItem();check(id+" installs native "+(next?next.assetId:"missing part"),next);rig.Select(next);check(id+" install keeps valid support",rig.InstallSelected());}
            yield return null;yield return null;check(id+" complete apparatus is installed",rig.Ready);
            check(id+" installed instruments retain real support: "+InstalledFailure(rig),rig.ValidateInstalledSurfaceLayout(out _));
            if(id=="C01"||id=="C06")
            {
                var pipette=rig.items.Single(item=>item.assetId=="pipette");var bulb=rig.items.Single(item=>item.assetId=="bulb");
                check(id+" final pipette rest includes its real attached bulb",bulb.transform.IsChildOf(pipette.transform));
                check(id+" complete pipette and bulb fit clear supported space after assembly",ApparatusRig.TryVisibleBounds(pipette,out var assembled)&&rig.TryGetRestingSupport(pipette,assembled,out _,out _));
            }
            foreach(var tray in trays)
            {
                CheckTrayTone(tray,check);check(id+" tray keeps native size and two separated bays",tray.transform.localScale==Vector3.one&&Mathf.Abs(Vector3.Distance(tray.Bay(0).center,tray.Bay(1).center)-.104f)<.0005f);
                for(int bay=0;bay<2;bay++)
                {
                    var floor=tray.Bay(bay);Physics.SyncTransforms();
                    check(id+" actual authored bay floor matches selected support plane",tray.GetComponentsInChildren<Collider>().Any(collider=>collider.enabled&&collider.Raycast(new Ray(floor.center+Vector3.up*.08f,Vector3.down),out var hit,.1f)&&Mathf.Abs(hit.point.y-floor.max.y)<.0005f));
                }
                foreach(var item in rig.items.Where(item=>item.gameObject.activeInHierarchy))
                    if(ApparatusRig.TryVisibleBounds(item,out var bounds))check(id+" "+item.assetId+" clears tray walls, divider and floor",!tray.BlocksPlacement(bounds));
            }
            Debug.Log("VLAB contrast tray coverage "+id+": "+trays.Length+" native trays; "+string.Join(", ",rig.items.Where(item=>ApparatusRig.TryVisibleBounds(item,out var b)&&trays.Any(tray=>tray.CanSupport(b))).Select(item=>item.assetId)));
            if(id=="C01"||id=="C05"||id=="B07")
            {
                check(id+" visible contrast capture contains an actual native tray",trays.Length>0);
                typeof(VlabApp).GetMethod("FocusStation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(app,null);
                yield return null;yield return new WaitForSecondsRealtime(.3f);capture(id+"-native-contrast-trays");yield return null;yield return null;
            }
            visibility?.Restore();yield return null;
            check(id+" leaving removes every owned contrast tray",trays.All(tray=>!tray||!tray.gameObject.activeInHierarchy));covered++;
        }
        check("Geometry and teardown cover all sixteen chemistry and biology lessons",covered==16&&totalTrays>0);app.ShowHome();yield return null;
    }
    static string LooseFailure(ApparatusRig rig){rig.ValidateLooseLayout(out var failure);return failure;}
    static string InstalledFailure(ApparatusRig rig){rig.ValidateInstalledSurfaceLayout(out var failure);return failure;}
    static IEnumerator Press(VlabApp app,string name)
    {
        yield return new WaitForSecondsRealtime(.28f);
        var button=app.GetComponentsInChildren<Button>().SingleOrDefault(button=>button.name==name&&button.isActiveAndEnabled&&button.interactable);
        if(!button)throw new InvalidOperationException("Contrast tray route control missing: "+name);button.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.28f);
    }
    static IEnumerator Route(VlabApp app,string route)
    {
        float deadline=Time.realtimeSinceStartup+30;while(app.Route!=route&&Time.realtimeSinceStartup<deadline)yield return null;
        if(app.Route!=route)throw new InvalidOperationException("Contrast tray route did not reach "+route+"; current "+app.Route);yield return null;
    }
}
#endif
