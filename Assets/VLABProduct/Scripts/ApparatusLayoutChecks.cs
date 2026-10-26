#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Vlab.Product;

// Run in each already loaded real room: P, C, B, M. No scene loading, cameras or sources are changed.
public static class ApparatusLayoutChecks
{
    public static IEnumerator RunCurrentRoom(string prefix,Action<string,bool> check)
    {
        var top=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>!r.GetComponentInParent<EquipmentHandle>()&&r.enabled&&r.bounds.max.y>.72f&&r.bounds.max.y<1.12f&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&
                (r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0))
            .OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).FirstOrDefault();
        check(prefix+" real source worktop exists",top);
        if(!top)yield break;
        Vector3 at=new Vector3(top.bounds.center.x,top.bounds.max.y+.005f,top.bounds.center.z);
        foreach(string id in LessonSpec.All.Keys.Where(id=>id.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(id=>id))
            yield return Fixture(id,at,false,check);
        if(prefix=="P")yield return P02ReceivingExtremes(at,check);
        if(prefix=="M")yield return BeamGaugeExtremes(at,check);
    }
    // Existing native subject fixtures use (5,.8,0) with no actual worktop there.
    // This support rectangle is explicitly synthetic and creates no replacement model.
    public static IEnumerator RunSynthetic(Action<string,bool> check)
    {
        foreach(string id in new[]{"P01","C01","B01","M05"})yield return Fixture(id,new Vector3(5,.8f,0),true,check);
    }
    static IEnumerator Fixture(string id,Vector3 at,bool synthetic,Action<string,bool> check)
    {
        var root=new GameObject("Layout acceptance "+id);ApparatusRig rig=null;string failure="";
        try{rig=root.AddComponent<ApparatusRig>();rig.Build(new ExperimentSession(id),at);}
        catch(Exception error){failure=error.ToString();}
        yield return null;yield return null; // inspect final post-Start geometry, not only clone-time bounds
        if(failure=="")try
        {
            check(id+" uses "+(synthetic?"explicit synthetic fixture":"actual room worktop"),rig.UsesSyntheticStorageSurface==synthetic);
            check(id+" initial loose source footprints rest inside tops without overlap: "+Reason(rig),rig.ValidateLooseLayout(out _));
            var poses=rig.items.ToDictionary(item=>item,item=>item.rotation);
            var scales=rig.items.ToDictionary(item=>item,item=>item.transform.localScale);
            var meshes=rig.GetComponentsInChildren<MeshFilter>(true).Where(mesh=>!NativeFluidProxy(mesh)).ToDictionary(mesh=>mesh,mesh=>mesh.sharedMesh);
            var visible=rig.items.FirstOrDefault(item=>!item.stowed&&ApparatusRig.TryVisibleBounds(item,out _));
            if(visible)
            {
                Vector3 position=visible.transform.position;visible.transform.position+=Vector3.forward*10;
                check(id+" layout check rejects an off-bench loose instrument",!rig.ValidateLooseLayout(out _));visible.transform.position=position;
                visible.stowed=true;
                check(id+" stored state cannot silently leave an instrument visible",!rig.ValidateLooseLayout(out _));visible.stowed=false;
            }
            int step=0;
            foreach(var item in rig.items.ToArray())
            {
                rig.Select(item);
                if(!rig.InstallSelected())throw new InvalidOperationException("Cannot install "+item.assetId);
                check(id+" loose layout remains supported after install "+(++step)+": "+Reason(rig),rig.ValidateLooseLayout(out _));
            }
            check(id+" all supplied instruments install, including stored parts",rig.Ready&&rig.StoredItemCount==0);
            if(id=="P01")
            {
                check("P01 original clamp bore does not penetrate the actual main-rod mesh: "+RodBoreReason(rig),ValidateRodBore(rig,out _));
                var stand=rig.items.Single(item=>item.assetId=="stand");var rod=stand.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Rod_VIS",StringComparison.Ordinal));
                var bore=rig.items.Single(item=>item.assetId=="clamp").GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_StandRod_SOCKET",StringComparison.Ordinal));
                check("P01 uses the delivered carriage stub and keeps suspension visibly beside the upright",Vector3.Distance(bore.position,stand.GetComponent<VlabStand>().Mount.position)<.001f&&Mathf.Abs(rig.Pivot.x-rod.bounds.center.x)>=.050f);
                check("P01 stock assembly retains full-range floor, worktop, carriage and endpoint clearance: "+AssemblyReason(rig),rig.ValidateAssembly(out _));
            }
            check(id+" stored orientations never replace installed source orientations",poses.All(pair=>pair.Key.rotation==pair.Value));
            check(id+" source model scales and mesh identities preserved",scales.All(pair=>pair.Key&&pair.Key.transform.localScale==pair.Value)&&meshes.All(pair=>pair.Key&&pair.Key.sharedMesh==pair.Value));
            if(id=="C08")
            {
                check("C08 unused thermometer and stir rod have stable original-geometry bench rests: "+CalorimetryRestReason(rig),rig.ValidateUnusedCalorimetryRest(out _));
                var rod=rig.items.Single(i=>i.assetId=="stir");var rotation=rod.transform.rotation;rod.transform.rotation=Quaternion.AngleAxis(90,Vector3.forward)*rotation;
                check("C08 unused-tool check rejects a rod balanced upright on its tip",!rig.ValidateUnusedCalorimetryRest(out _));rod.transform.rotation=rotation;
            }
            if(id=="B06"||id=="B07")
            {
                check(id+" unused original porcelain pestle has real head/handle rest contacts: "+PestleReason(rig),ValidatePestleRest(rig,out _));
                var pestle=rig.items.Single(i=>i.assetId=="pestle");var rotation=pestle.transform.rotation;pestle.transform.rotation=Quaternion.AngleAxis(90,Vector3.forward)*rotation;
                check(id+" unused-pestle check rejects balancing the rounded head upright",!ValidatePestleRest(rig,out _));pestle.transform.rotation=rotation;
            }
            if(id=="C06")
            {
                check("C06 original resting pipette glass stays clear of unrelated pH meter body: "+PipetteMeterReason(rig),ValidatePipetteMeterClearance(rig,out _));
                var pipette=rig.items.Single(i=>i.assetId=="pipette");var meter=rig.items.Single(i=>i.assetId=="probe").GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Body_VIS",StringComparison.Ordinal));
                var glass=pipette.GetComponentsInChildren<Renderer>().First(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.name.Contains("pipette_bulb",StringComparison.Ordinal));var position=pipette.transform.position;
                pipette.transform.position+=meter.bounds.center-glass.bounds.center;
                check("C06 unrelated-body clearance rejects pipette inserted through the meter",!ValidatePipetteMeterClearance(rig,out _));pipette.transform.position=position;
            }
            if(id=="P02")check("P02 actual imported barrel has its physical muzzle lip on the declared firing side: "+LauncherReason(rig),ValidateLauncherBarrel(rig,out _));
            foreach(var dropper in rig.items.Where(i=>i.GetComponentInChildren<VlabDropper>()))
            {
                var renderers=dropper.GetComponentsInChildren<Renderer>();var rubber=renderers.Single(r=>r.name.EndsWith("_Bulb_VIS",StringComparison.Ordinal));
                check(id+" "+dropper.assetId+" rests naturally on original glass and bulb instead of its narrow outlet",Mathf.Abs(Vector3.Dot(dropper.GetComponentInChildren<VlabDropper>().EmissionDirection,Vector3.up))<.27f&&Mathf.Abs(renderers.Where(r=>r!=rubber).Min(r=>r.bounds.min.y)-rubber.bounds.min.y)<.0005f);
            }
            if(id=="M01"||id=="M04")
            {
                var session=rig.Session;var driver=(PhysicsMechanicsDriver)rig.Driver;
                foreach(int input in id=="M01"?new[]{12,24}:new[]{40,80})
                {
                    session.parameters[id=="M01"?"driver":"d1"]=input;if(id=="M01")session.parameters["driven"]=input==12?24:36;else session.parameters["d2"]=input==40?80:40;
                    for(int configuration=0;configuration<(id=="M01"?1:4);configuration++)
                    {
                        if(id=="M04")session.parameters["crossed"]=configuration==3?1:0;
                        rig.RefreshConfiguration();check(id+" input "+input+" configuration "+configuration+" full bore engagement and supported carriers: "+TransmissionReason(rig),rig.ValidateTransmissionSupport(out _));
                        if(id=="M01")
                        {
                            var gear=rig.items.Single(i=>i.assetId=="gears").GetComponentInChildren<VlabGearSet>();
                            for(int angle=0;angle<360;angle+=15){gear.SetDriverAngle(angle);check("M01 original rotating bronze bores stay coaxial with real carriers at "+angle,rig.ValidateTransmissionSupport(out _));}
                        }
                        else if(configuration<3){if(!driver.Operate("belt-span",out string message))throw new InvalidOperationException(message);}
                    }
                }
                rig.RefreshConfiguration();var motor=rig.items.Single(i=>i.assetId=="motor");var point=motor.transform.position;motor.transform.position+=Vector3.up*.005f;
                check(id+" carrier validation rejects a detached original motor foot/shaft",!rig.ValidateTransmissionSupport(out _));motor.transform.position=point;
            }
            if(new[]{"P04","P07","P08"}.Contains(id))
            {
                check(id+" original photogate legs touch native or derived track seats at the true optical height: "+GateReason(rig),rig.ValidatePhotogateSupports(out _));
                var gate=rig.items.Single(i=>i.assetId=="gateA");Vector3 point=gate.transform.position;gate.transform.position+=Vector3.up*.005f;
                check(id+" physical gate check rejects a floating frame without changing measured speed",!rig.ValidatePhotogateSupports(out _));gate.transform.position=point;
            }
            if(id=="M06")
            {
                check("M06 derived riser and guide fork support original hardware: "+DerivedReason(rig),rig.ValidateDerivedM06Supports(out _));
                var riser=rig.GetComponentsInChildren<Renderer>().Single(r=>r.name.StartsWith("Derived M06 motor alignment riser",StringComparison.Ordinal));
                Vector3 position=riser.transform.position;riser.transform.position+=Vector3.up*.005f;
                check("M06 support validation rejects a floating derived motor riser",!rig.ValidateDerivedM06Supports(out _));riser.transform.position=position;
                var cam=rig.items.Single(item=>item.assetId=="cam").GetComponentInChildren<VlabCam>();double angle=cam.AngleDegrees;
                var driver=(PhysicsMechanicsDriver)rig.Driver;
                for(int variant=0;variant<2;variant++)
                {
                    var follower=rig.items.Where(item=>(item.assetId=="follower"||item.assetId=="roller")&&item.gameObject.activeInHierarchy).Single().GetComponentInChildren<VlabFollower>();
                    double minimum=10,maximum=0;
                    for(int degrees=0;degrees<360;degrees+=15)
                    {
                        if(!cam.SetAngle(degrees)||!follower.Follow(cam))throw new InvalidOperationException("Native M06 cam/follower pose failed at "+degrees);
                        driver.UpdateCamGaugeContact();var gauge=rig.items.Single(item=>item.assetId=="gauge").GetComponentInChildren<VlabDialGauge>();minimum=Math.Min(minimum,gauge.CompressionMm);maximum=Math.Max(maximum,gauge.CompressionMm);
                        check("M06 "+(follower.IsRoller?"roller":"knife")+" original tip contact and supported gauge at "+degrees+" degrees: "+GaugeReason(rig),rig.ValidateGaugeSupport(out _));
                        check("M06 derived supports clear actual cam and stem at "+degrees+" degrees: "+DerivedReason(rig),rig.ValidateDerivedM06Supports(out _));
                    }
                    check("M06 "+(follower.IsRoller?"roller":"knife")+" tip follows actual zero to 10 mm source stroke",minimum<.01&&maximum>9.98&&maximum<=10);
                    cam.SetAngle(0);follower.Follow(cam);driver.UpdateCamGaugeContact();
                    if(!driver.Operate("follower-type",out string reason))throw new InvalidOperationException(reason);
                }
                cam.SetAngle(angle);rig.RefreshConfiguration();
                var gaugeItem=rig.items.Single(item=>item.assetId=="gauge");Vector3 gaugePosition=gaugeItem.transform.position;gaugeItem.transform.position+=Vector3.right*.005f;
                check("M06 gauge validation rejects detached tip and mounting bore",!rig.ValidateGaugeSupport(out _));gaugeItem.transform.position=gaugePosition;
            }
            if(new[]{"C01","C05","C06"}.Contains(id))
            {
                var pipette=rig.items.Single(item=>item.assetId=="pipette");
                var tip=pipette.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_TipOutlet_SOCKET",StringComparison.Ordinal));
                var connection=pipette.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_BulbConnection_SOCKET",StringComparison.Ordinal));
                check(id+" unused empty pipette and attached bulb rest flat instead of balancing on the tip",Mathf.Abs(Vector3.Dot((connection.position-tip.position).normalized,Vector3.up))<.27f);
                var bulb=rig.items.Single(item=>item.assetId=="bulb").transform;
                var glass=pipette.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.transform.IsChildOf(bulb)).ToArray();
                var rubber=bulb.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                check(id+" separate original glass tip and bulb both contact the supported resting plane",glass.Length>0&&rubber.Length>0&&Mathf.Abs(glass.Min(r=>r.bounds.min.y)-rubber.Min(r=>r.bounds.min.y))<.0005f);
            }
            bool seated=rig.ValidateInstalledSurfaceLayout(out string supportFailure);
            check(id+" installed ordinary bases remain on real support (named mounted/hanging parts declared): "+supportFailure,seated);
            var seatedItem=rig.items.FirstOrDefault(item=>item.installed&&item.gameObject.activeInHierarchy&&rig.InstalledSupportException(item.assetId)==""&&ApparatusRig.TryVisibleBounds(item,out _));
            if(seatedItem)
            {
                Vector3 position=seatedItem.transform.position;seatedItem.transform.position+=Vector3.forward*10;
                check(id+" installed support check rejects an ordinary off-bench base",!rig.ValidateInstalledSurfaceLayout(out _));seatedItem.transform.position=position;
            }
        }
        catch(Exception error){failure=error.ToString();}
        check(id+" native layout fixture completes: "+failure,failure=="");
        UnityEngine.Object.Destroy(root);yield return null;
    }
    static IEnumerator BeamGaugeExtremes(Vector3 at,Action<string,bool> check)
    {
        foreach(double span in new[]{LessonSpec.All["M08"].parameters.Single(p=>p.key=="span").min,LessonSpec.All["M08"].parameters.Single(p=>p.key=="span").max})
        {
            var root=new GameObject("M08 actual support/gauge acceptance "+span);ApparatusRig rig=null;ExperimentSession session=null;string failure="";
            try
            {
                session=new ExperimentSession("M08");session.parameters["span"]=span;
                double inertia=.02*Math.Pow(.005,3)/12;session.parameters["load"]=Math.Min(5,Math.Floor(.9*.01*48*2e9*inertia/(span*span)));
                rig=root.AddComponent<ApparatusRig>();rig.Build(session,at);foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new InvalidOperationException("Install "+item.assetId);}
            }
            catch(Exception error){failure=error.ToString();}
            yield return null;yield return null;
            if(failure=="")try
            {
                rig.RefreshConfiguration();check("M08 "+span+" m original unloaded probe and both source feet have real support: "+GaugeReason(rig),rig.ValidateGaugeSupport(out _));
                if(span==LessonSpec.All["M08"].parameters.Single(p=>p.key=="span").max)
                {
                    double validLoad=session.parameters["load"];session.parameters["load"]=5;rig.RefreshConfiguration();
                    check("M08 maximum span retains the genuine 5 N small-deflection rejection without fabricated data",!rig.PrepareNext(out string domain)&&domain.Contains("delta/L<=0.01")&&session.Recorded==null);
                    session.parameters["load"]=validLoad;rig.RefreshConfiguration();
                }
                while(session.Operation<session.spec.operationsEn.Length)if(!rig.PrepareNext(out string reason))throw new InvalidOperationException(reason);
                check("M08 "+span+" m loaded underside probe maintains actual contact and housing clearance: "+GaugeReason(rig),rig.ValidateGaugeSupport(out _));
                if(!session.Start(out string start))throw new InvalidOperationException(start);session.Advance(.02);
                if(!rig.Driver.Operate("unload",out string unload))throw new InvalidOperationException(unload);
                check("M08 "+span+" m unloaded probe returns to actual zero contact: "+GaugeReason(rig),rig.ValidateGaugeSupport(out _)&&rig.items.Single(i=>i.assetId=="gauge").GetComponentInChildren<VlabDialGauge>().ReadingMm==0);
                var post=rig.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Derived M08 gauge stand rear post");Vector3 position=post.transform.position;post.transform.position+=Vector3.up*.005f;
                check("M08 "+span+" m support check rejects a detached gauge post",!rig.ValidateGaugeSupport(out _));post.transform.position=position;
            }
            catch(Exception error){failure=error.ToString();}
            check("M08 "+span+" m native gauge fixture completes: "+failure,failure=="");UnityEngine.Object.Destroy(root);yield return null;
        }
    }
    static string GaugeReason(ApparatusRig rig){rig.ValidateGaugeSupport(out string failure);return failure;}
    static string GateReason(ApparatusRig rig){rig.ValidatePhotogateSupports(out string failure);return failure;}
    static string TransmissionReason(ApparatusRig rig){rig.ValidateTransmissionSupport(out string failure);return failure;}
    static string CalorimetryRestReason(ApparatusRig rig){rig.ValidateUnusedCalorimetryRest(out string failure);return failure;}
    static string PipetteMeterReason(ApparatusRig rig){ValidatePipetteMeterClearance(rig,out string failure);return failure;}
    static string PestleReason(ApparatusRig rig){ValidatePestleRest(rig,out string failure);return failure;}
    public static bool ValidatePestleRest(ApparatusRig rig,out string failure)
    {
        var item=rig.items.Single(i=>i.assetId=="pestle");var sockets=item.GetComponentsInChildren<Transform>();var bottom=sockets.Single(t=>t.name.EndsWith("_Bottom_SOCKET",StringComparison.Ordinal));var grip=sockets.Single(t=>t.name.EndsWith("_Grip_SOCKET",StringComparison.Ordinal));var axis=(grip.position-bottom.position).normalized;
        float head=float.PositiveInfinity,handle=float.PositiveInfinity;Bounds actual=default;bool found=false;
        var mesh=item.GetComponentsInChildren<MeshFilter>().Single(m=>m.name.EndsWith("_Pestle_VIS",StringComparison.Ordinal));
        using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(mesh.sharedMesh))
        using(var vertices=new NativeArray<Vector3>(snapshot[0].vertexCount,Allocator.Temp))
        {
            snapshot[0].GetVertices(vertices);
            foreach(var vertex in vertices)
            {
                var point=mesh.transform.TransformPoint(vertex);if(found)actual.Encapsulate(point);else{actual=new Bounds(point,Vector3.zero);found=true;}
                float along=Vector3.Dot(point-grip.position,axis);if(along<-.065f)head=Mathf.Min(head,point.y);if(along>.010f)handle=Mathf.Min(handle,point.y);
            }
        }
        bool bounds=ApparatusRig.TryVisibleBounds(item,out var tight)&&Vector3.Distance(tight.min,actual.min)<.00005f&&Vector3.Distance(tight.max,actual.max)<.00005f;
        failure="actual source head/handle contact-height difference "+Mathf.Abs(head-handle).ToString("F6")+" m; original-profile tight bounds "+(bounds?"agree":"DISAGREE");
        return found&&bounds&&Mathf.Abs(Vector3.Dot(axis,Vector3.up))<.10f&&Mathf.Abs(head-handle)<.0001f;
    }
    public static bool ValidatePipetteMeterClearance(ApparatusRig rig,out string failure)
    {
        var pipette=rig.items.Single(i=>i.assetId=="pipette");var meter=rig.items.Single(i=>i.assetId=="probe").GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Body_VIS",StringComparison.Ordinal));
        foreach(var glass in pipette.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.name.Contains("pipette_bulb",StringComparison.Ordinal)))
        {
            var a=glass.bounds;var b=meter.bounds;var overlap=Vector3.Min(a.max,b.max)-Vector3.Max(a.min,b.min);
            if(overlap.x>.0002f&&overlap.y>.0002f&&overlap.z>.0002f){failure=glass.name+" conservatively overlaps the unrelated original meter body bounds.";return false;}
        }
        failure="Original visible glass/body bounds are separate (conservative bounds check, not exact triangle collision).";return true;
    }
    public static IEnumerator P02ReceivingExtremes(Vector3 worktop,Action<string,bool> check)
    {
        var cases=new[]{(angle:45d,speed:5d,valid:true),(angle:5d,speed:4d,valid:true),(angle:60d,speed:4d,valid:true),(angle:85d,speed:5d,valid:false),(angle:45d,speed:10d,valid:false),(angle:85d,speed:10d,valid:false),(angle:5d,speed:1d,valid:false)};
        foreach(var sample in cases)
        {
            string label="P02 floor "+sample.angle+" deg / "+sample.speed+" m/s";var root=new GameObject(label);ApparatusRig rig=null;ExperimentSession session=null;bool started=false,recorded=false;string failure="";
            try
            {
                session=new ExperimentSession("P02");session.parameters["angle"]=sample.angle;session.parameters["speed"]=sample.speed;rig=root.AddComponent<ApparatusRig>();rig.Build(session,worktop);
                foreach(var item in rig.items){rig.Select(item);if(!rig.InstallSelected())throw new InvalidOperationException("Cannot install "+item.assetId);}
            }
            catch(Exception error){failure=error.ToString();}
            yield return null;yield return null;
            if(failure=="")try
            {
                rig.RefreshConfiguration();var driver=(PhysicsMechanicsDriver)rig.Driver;
                check(label+" imported visible barrel and native ray agree: "+LauncherReason(rig),ValidateLauncherBarrel(rig,out _));
                check(label+" preserves the supplied speed and angle",session.parameters["speed"]==sample.speed&&session.parameters["angle"]==sample.angle);
                check(label+" actual floor/ceiling/worktop envelope validity: "+driver.ReceivingGeometryDiagnostic,driver.ReceivingGeometryValid==sample.valid);
                if(sample.angle==85&&sample.speed==5)check(label+" retains the native launch-bench lip rejection",driver.ReceivingGeometryDiagnostic.Contains("actual worktop"));
                if(sample.angle==85&&sample.speed==10)check(label+" retains the actual ceiling rejection",driver.ReceivingGeometryDiagnostic.Contains("ceiling"));
                if(sample.valid)
                {
                    check(label+" source mat fully rests on actual floor",rig.ValidateReceivingFloorTarget(out _));
                    while(session.Operation<session.spec.operationsEn.Length)if(!rig.PrepareNext(out var reason))throw new InvalidOperationException(reason);
                    if(sample.angle==45&&sample.speed==5)
                    {
                        session.SetParameter("speed",10,out _);rig.RefreshConfiguration();
                        check("P02 editing an already prepared trial beyond the room rejects safely without clamping",session.parameters["speed"]==10&&!session.Start(out string rejected)&&rejected==PhysicsMechanicsDriver.ReceivingFailure&&session.Recorded==null);
                        session.SetParameter("speed",sample.speed,out _);rig.RefreshConfiguration();
                    }
                    if(!session.Start(out var start))throw new InvalidOperationException(start);started=true;
                }
                else
                {
                    bool prepared=rig.PrepareNext(out string reason);check(label+" refuses unsafe preparation with stable translated reason",!prepared&&reason==PhysicsMechanicsDriver.ReceivingFailure);
                    check(label+" Vietnamese room-limit guidance explains adjustment and retained inputs",LessonGuide.ExplainFailure(reason,true).Contains("Đổi vận tốc")&&LessonGuide.ExplainFailure(reason,true).Contains("vẫn được giữ"));
                    check(label+" cannot start or fabricate a recorded unsupported shot",!session.Start(out _)&&!session.Measure(out _)&&session.Recorded==null);
                    if(sample.speed==10&&sample.angle==45)
                    {
                        var muzzle=rig.items.Single(item=>item.assetId=="launcher").GetComponentInChildren<VlabLauncher>().MuzzleRay;
                        check(label+" retains the full predicted range beyond ten metres",Vector3.ProjectOnPlane(driver.PredictedLandingPoint-muzzle.origin,Vector3.up).magnitude>10);
                    }
                    check(label+" unsupported settings keep the original mat in supported storage",rig.ValidateInstalledSurfaceLayout(out _));
                }
            }
            catch(Exception error){failure=error.ToString();}
            for(int tick=0;started&&failure==""&&tick<200&&!recorded;tick++)
            {
                try
                {
                    session.Advance(.02);
                    if(rig.Driver.CanRecord(out _))
                    {
                        if(!session.Measure(out var reason))throw new InvalidOperationException(reason);recorded=true;
                        check(label+" native first-floor contact is retained on the original mat",session.Recorded.Get("targetHit")>.5&&session.Recorded.Get("flightTime")>0&&session.Recorded.Get("range")>0);
                    }
                }
                catch(Exception error){failure=error.ToString();}
                yield return new WaitForFixedUpdate();
            }
            if(started)check(label+" produces a real native recorded floor trial",recorded);
            check(label+" receiving fixture completes: "+failure,failure=="");UnityEngine.Object.Destroy(root);yield return null;
        }
    }
    static string DerivedReason(ApparatusRig rig){rig.ValidateDerivedM06Supports(out string failure);return failure;}
    static bool NativeFluidProxy(MeshFilter mesh)
    {
        // These two source SDK APIs explicitly generate optical/volume meshes as liquid changes.
        // Glass, metal, hardware and every other original source mesh remain strict identities.
        return mesh.GetComponentsInParent<VlabFlask>(true).Any(flask=>flask.Liquid==mesh)||
               mesh.GetComponentsInParent<VlabLiquid>(true).Any(liquid=>liquid.Surface==mesh);
    }
    static string RodBoreReason(ApparatusRig rig){ValidateRodBore(rig,out string failure);return failure;}
    static string AssemblyReason(ApparatusRig rig){rig.ValidateAssembly(out string failure);return failure;}
    static string LauncherReason(ApparatusRig rig){ValidateLauncherBarrel(rig,out string diagnostic);return diagnostic;}
    public static bool ValidateLauncherBarrel(ApparatusRig rig,out string diagnostic)
    {
        var launcher=rig.items.Single(i=>i.assetId=="launcher").GetComponentInChildren<VlabLauncher>();
        var barrel=launcher.GetComponentsInChildren<MeshFilter>().Single(m=>m.name.EndsWith("_Barrel_VIS",StringComparison.Ordinal));var ray=launcher.MuzzleRay;
        float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;var inner=new System.Collections.Generic.List<Vector3>();var outer=new System.Collections.Generic.List<Vector3>();
        using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(barrel.sharedMesh))
        using(var vertices=new NativeArray<Vector3>(snapshot[0].vertexCount,Allocator.Temp))
        {
            snapshot[0].GetVertices(vertices);
            foreach(var vertex in vertices)
            {
                var world=barrel.transform.TransformPoint(vertex);float axial=Vector3.Dot(world-barrel.transform.position,ray.direction);minimum=Mathf.Min(minimum,axial);maximum=Mathf.Max(maximum,axial);
                if(Mathf.Abs(Vector3.Dot(world-ray.origin,ray.direction))>.00005f)continue;
                float radius=Vector3.ProjectOnPlane(world-ray.origin,ray.direction).magnitude;
                if(Mathf.Abs(radius-.0106f)<.00005f)inner.Add(world);if(Mathf.Abs(radius-.016f)<.00005f)outer.Add(world);
            }
        }
        float centreError=float.PositiveInfinity;
        if(inner.Count>0&&outer.Count>0)centreError=Mathf.Max(Vector3.Distance(inner.Aggregate(Vector3.zero,(sum,p)=>sum+p)/inner.Count,ray.origin),Vector3.Distance(outer.Aggregate(Vector3.zero,(sum,p)=>sum+p)/outer.Count,ray.origin));
        diagnostic="actual axial vertices ["+minimum.ToString("F6")+", "+maximum.ToString("F6")+"] m; true muzzle inner/outer lip vertices "+inner.Count+"/"+outer.Count+"; lip-centre error "+centreError.ToString("F6")+" m";
        return Mathf.Abs(minimum)<.0001f&&Mathf.Abs(maximum-.150f)<.0001f&&inner.Count>=32&&outer.Count>=32&&centreError<.00005f;
    }
    // Read the actual imported triangles without changing the source importer's Read/Write flag.
    // UnityEditor.MeshUtility.AcquireReadOnlyMeshData is the supported editor-only read path.
    public static bool ValidateRodBore(ApparatusRig rig,out string failure)
    {
        var support=rig.items.Single(item=>item.assetId=="stand");var clamp=rig.items.Single(item=>item.assetId=="clamp");
        var rod=support.GetComponentsInChildren<MeshFilter>().Single(mesh=>mesh.name.EndsWith("_Rod_VIS",StringComparison.Ordinal));
        var bottom=support.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_RodBase_SOCKET",StringComparison.Ordinal));
        var top=support.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_RodTop_SOCKET",StringComparison.Ordinal));
        if(Vector3.Dot((top.position-bottom.position).normalized,Vector3.up)<.9999f){failure="Main rod axis is not vertical.";return false;}
        float radius=0;
        using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(rod.sharedMesh))
        using(var vertices=new NativeArray<Vector3>(snapshot[0].vertexCount,Allocator.Temp))
        {
            snapshot[0].GetVertices(vertices);
            foreach(var v in vertices){var p=rod.transform.TransformPoint(v)-bottom.position;radius=Mathf.Max(radius,new Vector2(p.x,p.z).magnitude);}
        }
        float nearest=float.PositiveInfinity;string nearestMesh="";
        foreach(var mesh in clamp.GetComponentsInChildren<MeshFilter>())
        using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(mesh.sharedMesh))
        using(var vertices=new NativeArray<Vector3>(snapshot[0].vertexCount,Allocator.Temp))
        {
            var data=snapshot[0];data.GetVertices(vertices);
            for(int sub=0;sub<data.subMeshCount;sub++)
            using(var indices=new NativeArray<int>(data.GetSubMesh(sub).indexCount,Allocator.Temp))
            {
                data.GetIndices(indices,sub);
                for(int n=0;n+2<indices.Length;n+=3)
                {
                    var a=mesh.transform.TransformPoint(vertices[indices[n]]);var b=mesh.transform.TransformPoint(vertices[indices[n+1]]);var c=mesh.transform.TransformPoint(vertices[indices[n+2]]);
                    if(Mathf.Min(a.y,Mathf.Min(b.y,c.y))>top.position.y||Mathf.Max(a.y,Mathf.Max(b.y,c.y))<bottom.position.y)continue;
                    float distance=Mathf.Sqrt(TriangleOriginSquared(new Vector2(a.x-bottom.position.x,a.z-bottom.position.z),new Vector2(b.x-bottom.position.x,b.z-bottom.position.z),new Vector2(c.x-bottom.position.x,c.z-bottom.position.z)));
                    if(distance<nearest){nearest=distance;nearestMesh=mesh.name;}
                }
            }
        }
        if(radius<.0049f||radius>.0051f){failure="Original shaft is not the expected 10 mm mesh.";return false;}
        if(nearest+.000002f<radius){failure="Actual clamp triangle "+nearestMesh+" reaches "+nearest.ToString("F6")+" m from rod axis; rod radius "+radius.ToString("F6")+" m.";return false;}
        failure="10 mm rod radius "+radius.ToString("F6")+" m; minimum original clamp-mesh clearance "+(nearest-radius).ToString("F6")+" m";return true;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float SegmentOriginSquared(Vector2 a,Vector2 b)
    {var d=b-a;float t=d.sqrMagnitude<1e-16f?0:Mathf.Clamp01(-Vector2.Dot(a,d)/d.sqrMagnitude);return (a+d*t).sqrMagnitude;}
    static float TriangleOriginSquared(Vector2 a,Vector2 b,Vector2 c)
    {
        if(Mathf.Abs(Cross(b-a,c-a))>1e-12f)
        {
            float x=Cross(b-a,-a),y=Cross(c-b,-b),z=Cross(a-c,-c);
            if((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0))return 0;
        }
        return Mathf.Min(SegmentOriginSquared(a,b),Mathf.Min(SegmentOriginSquared(b,c),SegmentOriginSquared(c,a)));
    }
    static string Reason(ApparatusRig rig){rig.ValidateLooseLayout(out string failure);return failure;}
}
#endif
