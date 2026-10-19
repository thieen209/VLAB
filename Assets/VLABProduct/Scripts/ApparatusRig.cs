using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Vlab.Product
{
    public sealed class EquipmentHandle:MonoBehaviour
    {public string assetId;public Vector3 slot,trayPosition;public Quaternion rotation,trayRotation;public bool installed,stowed;}
    public sealed class ApparatusRig:MonoBehaviour
    {
        public readonly List<EquipmentHandle> items=new List<EquipmentHandle>();
        public Vector3 Worktop{get;private set;}public Vector3 Pivot{get;private set;}
        public event Action Changed;
        public bool Ready=>items.Count>0&&items.All(x=>x.installed);
        public EquipmentHandle Selected{get;private set;}
        public LessonDriver Driver{get;private set;}
        public ExperimentSession Session=>session;
        ExperimentSession session;Transform bob;VlabString cord;VlabPendulumBob bobController;VlabTimer timer;bool timerCompleted;
        // Preserve the delivered 720 mm stand. Height changes use its native carriage only.
        const float RodBottom=.020f,SupportTop=.720f,PendulumClampHeight=.660f;
        Bounds stationTop;bool hasStationTop;
        sealed class StorageSurface {public Bounds bounds;public string name;}
        readonly List<StorageSurface> storageSurfaces=new List<StorageSurface>();
        const float StorageInset=.012f,StorageGap=.018f,StorageLift=.002f;
        public bool UsesSyntheticStorageSurface{get;private set;}
        public int StoredItemCount=>items.Count(x=>!x.installed&&x.stowed);
        Vector3 centre;double lastAngle;bool carrying,pullingBob;float carryDistance;
        readonly Dictionary<EquipmentHandle,LineRenderer> targets=new Dictionary<EquipmentHandle,LineRenderer>();
        Material targetMaterial;PendulumDynamics timedDynamics;
        Renderer motorRiser;readonly List<Renderer> guideBracket=new List<Renderer>();
        Renderer gaugeBase,gaugePost,gaugePin;readonly List<Renderer> beamRisers=new List<Renderer>();
        const float BeamGaugeRiserHeight=.070f;
        readonly Dictionary<string,Renderer[]> photogateSeats=new Dictionary<string,Renderer[]>();
        readonly Dictionary<string,Renderer[]> transmissionCarriers=new Dictionary<string,Renderer[]>();
        readonly List<Renderer> boardFeet=new List<Renderer>();
        public void Build(ExperimentSession value,Vector3 at)
        {
            session=value;Worktop=at;centre=at;Pivot=centre+new Vector3(0,1.45f,0);FindStorageSurfaces();
            if(value.spec.id!="P01"){BuildLesson();return;}
            string[] assets=value.spec.id=="P01"?new[]{"stand","clamp","pendulum","string","ruler","timer"}:new string[0];
            for(int i=0;i<assets.Length;i++)
            {
                var prefab=Resources.Load<GameObject>("Equipment/VLAB_"+assets[i]);if(!prefab)throw new InvalidOperationException("Required equipment missing: "+assets[i]);
                var go=Instantiate(prefab,transform);go.name="Equipment · "+assets[i];
                var handle=go.AddComponent<EquipmentHandle>();handle.assetId=assets[i];
                if(assets[i]=="stand"||assets[i]=="clamp")go.transform.rotation=Quaternion.AngleAxis(assets[i]=="stand"?-90:90,Vector3.up)*go.transform.rotation;
                handle.rotation=go.transform.rotation;
                if(assets[i]=="stand")ConfigureSupport(handle);
                handle.slot=Slot(assets[i]);handle.installed=false;
                items.Add(handle);
                if(assets[i]=="pendulum")
                {
                    bob=go.transform;bobController=go.GetComponent<VlabPendulumBob>();
                    // A forgiving ray target does not change the delivered bob's visible or physical dimensions.
                    var target=go.AddComponent<SphereCollider>();target.isTrigger=true;target.radius=.05f;
                }
                if(assets[i]=="string"){cord=go.GetComponent<VlabString>();var line=go.GetComponentInChildren<LineRenderer>();if(line)line.widthMultiplier*=2;}
                if(assets[i]=="timer"){timer=go.GetComponentInChildren<VlabTimer>();if(timer){timer.TrySetMode(0);timer.ResetTimer();}}
            }
            ArrangeLooseItems();
            CreateTargets();UpdateTargets();session.SetApparatusReady(false);Changed?.Invoke();
        }
        void BuildLesson()
        {
            Driver=session.spec.id.StartsWith("C",StringComparison.Ordinal)?(LessonDriver)new ChemistryDriver(session):session.spec.id.StartsWith("B",StringComparison.Ordinal)?new BiologyDriver(session):new PhysicsMechanicsDriver(session);
            var indexed=new Dictionary<string,EquipmentHandle>();
            foreach(var spec in Driver.Kit)
            {
                if(indexed.ContainsKey(spec.key))throw new InvalidOperationException("Duplicate apparatus key: "+spec.key);
                var prefab=Resources.Load<GameObject>("Equipment/VLAB_"+spec.prefab);if(!prefab)throw new InvalidOperationException("Delivered instrument missing: "+spec.prefab);
                var go=Instantiate(prefab,transform);go.name="Equipment · "+spec.key;
                var looseCrank=go.GetComponentInChildren<VlabCrank>();if(looseCrank)looseCrank.Configure(30,0);
                // The delivered coverslip's Start assumes a mounted hinge parent. Defer that
                // lifecycle until LowerCover supplies the actual slide frame; keep loose glass flat.
                var looseCover=go.GetComponentInChildren<VlabCoverslip>();
                if(looseCover){looseCover.SetAngle(0);looseCover.enabled=false;}
                go.transform.SetPositionAndRotation(Worktop+spec.position,Quaternion.Euler(spec.euler)*go.transform.rotation);
                if(spec.prefab=="pipette")go.transform.rotation=Quaternion.AngleAxis(90,Vector3.forward)*go.transform.rotation;
                if(spec.prefab=="dropper")
                {
                    var drop=go.GetComponentInChildren<VlabDropper>();go.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.left)*go.transform.rotation;
                    var renderers=go.GetComponentsInChildren<Renderer>();var bulb=renderers.Single(r=>r.name.EndsWith("_Bulb_VIS",StringComparison.Ordinal));var glass=renderers.Where(r=>r!=bulb).ToArray();
                    var flat=go.transform.rotation;float lo=0,hi=15;
                    for(int step=0;step<24;step++){float angle=(lo+hi)*.5f;go.transform.rotation=Quaternion.AngleAxis(angle,Vector3.forward)*flat;if(glass.Min(r=>r.bounds.min.y)>bulb.bounds.min.y)lo=angle;else hi=angle;}
                    go.transform.rotation=Quaternion.AngleAxis((lo+hi)*.5f,Vector3.forward)*flat;
                }
                if(spec.prefab=="thermometer")
                {
                    var sockets=go.GetComponentsInChildren<Transform>();var tip=sockets.Single(t=>t.name.EndsWith("_Tip_SOCKET",StringComparison.Ordinal));var sensor=sockets.Single(t=>t.name.EndsWith("_SensorTop_SOCKET",StringComparison.Ordinal));
                    go.transform.rotation=Quaternion.FromToRotation((sensor.position-tip.position).normalized,Vector3.right)*go.transform.rotation;
                    var renderers=go.GetComponentsInChildren<Renderer>();var body=renderers.Single(r=>r.name.EndsWith("_Body_VIS",StringComparison.Ordinal));var probe=renderers.Single(r=>r.name.EndsWith("_Probe_VIS",StringComparison.Ordinal));
                    var flat=go.transform.rotation;float lo=0,hi=15;
                    for(int step=0;step<24;step++){float angle=(lo+hi)*.5f;go.transform.rotation=Quaternion.AngleAxis(angle,Vector3.forward)*flat;if(probe.bounds.min.y>body.bounds.min.y)lo=angle;else hi=angle;}
                    go.transform.rotation=Quaternion.AngleAxis((lo+hi)*.5f,Vector3.forward)*flat;
                }
                if(spec.prefab=="stir_rod")
                {
                    var sockets=go.GetComponentsInChildren<Transform>();var tip=sockets.Single(t=>t.name.EndsWith("_Tip_SOCKET",StringComparison.Ordinal));var top=sockets.Single(t=>t.name.EndsWith("_Top_SOCKET",StringComparison.Ordinal));
                    go.transform.rotation=Quaternion.FromToRotation((top.position-tip.position).normalized,Vector3.right)*go.transform.rotation;
                }
                if(spec.prefab=="pestle")
                {
                    var sockets=go.GetComponentsInChildren<Transform>();var bottom=sockets.Single(t=>t.name.EndsWith("_Bottom_SOCKET",StringComparison.Ordinal));var grip=sockets.Single(t=>t.name.EndsWith("_Grip_SOCKET",StringComparison.Ordinal));
                    go.transform.rotation=Quaternion.FromToRotation((grip.position-bottom.position).normalized,Vector3.right)*go.transform.rotation;
                    // Source head r10/z-76 and handle flare r6/z23 share the lower support line.
                    go.transform.rotation=Quaternion.AngleAxis(-Mathf.Atan(.004f/.099f)*Mathf.Rad2Deg,Vector3.forward)*go.transform.rotation;
                }
                if(spec.onSurface)PlaceInstalledOnSurface(go,Worktop+spec.position);
                if(spec.prefab=="launcher")SeatLauncherFootAtLeftEdge(go);
                // The 55 mm source crowns cannot clear the 103 mm underside gauge body.
                // Separate derived blocks raise the unchanged support pair, never the beam section.
                if(session.spec.id=="M08"&&spec.key=="supports")go.transform.position+=Vector3.up*BeamGaugeRiserHeight;
                if(session.spec.id=="M04"&&spec.key=="belt")go.transform.position+=Vector3.up*.012f;
                if(session.spec.id=="M01"&&spec.key=="board")go.transform.position+=Vector3.up*.012f;
                var handle=go.AddComponent<EquipmentHandle>();handle.assetId=spec.key;handle.slot=go.transform.position;handle.rotation=go.transform.rotation;
                items.Add(handle);indexed.Add(spec.key,handle);
            }
            ArrangeLooseItems();Driver.Bind(indexed,Worktop);Driver.RestoreState(session.RestoredDetailJson);session.Authority=Driver;session.SetApparatusReady(false);Changed?.Invoke();
        }
        public string EquipmentLabel(EquipmentHandle item,bool vietnamese)
        {
            var spec=Driver?.Kit.FirstOrDefault(x=>x.key==item.assetId);if(spec!=null)return vietnamese?spec.vi:spec.en;
            switch(item.assetId)
            {
                case "stand":return vietnamese?"Giá đỡ thí nghiệm":"Laboratory stand";
                case "clamp":return vietnamese?"Kẹp giữ dây":"Suspension clamp";
                case "pendulum":return vietnamese?"Quả nặng con lắc":"Pendulum bob";
                case "string":return vietnamese?"Dây treo":"Suspension cord";
                case "ruler":return vietnamese?"Thước đo chiều dài":"Length ruler";
                case "timer":return vietnamese?"Đồng hồ đo thời gian":"Interval timer";
                default:return item.assetId.Replace('_',' ');
            }
        }
        public bool PrepareNext(out string message)
        {
            message="";if(!Ready){message="Install every required instrument before preparing the trial.";return false;}
            if(Driver!=null&&!Driver.Prepare(session.Operation+1,out message))return false;
            if(!session.Prepare(session.Operation,out message))return false;
            if(Driver==null)ApplyPreparation(session.Operation);return true;
        }
        void ConfigureSupport(EquipmentHandle item)
        {
            var stand=item.GetComponent<VlabStand>();
            // The separate clamp has its own real 10 mm bore and mounts directly on the main rod.
            // Keep the unused native carriage below it so the two physical bosses cannot overlap.
            if(!stand||!stand.TrySetHeight(.450f))throw new InvalidOperationException("Delivered stand carriage is unavailable.");
            // The brief's long strings hang outside the tabletop, rather than stretching the stand.
            var top=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>!r.transform.IsChildOf(transform)&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&Mathf.Abs(r.bounds.max.y-(Worktop.y-.005f))<.015f)
                .OrderBy(r=>(r.bounds.center-new Vector3(Worktop.x,r.bounds.center.y,Worktop.z)).sqrMagnitude).FirstOrDefault();
            if(top){stationTop=top.bounds;hasStationTop=true;}
            else if(UsesSyntheticStorageSurface){stationTop=storageSurfaces[0].bounds;hasStationTop=true;}
        }
        void PositionStockSupportAtEdge()
        {
            if(!hasStationTop)throw new InvalidOperationException("P01 requires an identified worktop edge for long-string clearance.");
            var support=items.First(x=>x.assetId=="stand");var clampItem=items.First(x=>x.assetId=="clamp");
            var clamp=clampItem.GetComponent<VlabClamp>();clamp.SetOpening(.002);
            // Reverse the original stand so its main rod is nearest the edge; the independently
            // fitted clamp points outboard. The complete original base now rests on the bench.
            // 28 mm keeps the 15 mm-radius bob and its swing outside the worktop with margin.
            float shift=stationTop.min.z-.028f-clamp.Grip.position.z;
            support.transform.position+=Vector3.forward*shift;clampItem.transform.position+=Vector3.forward*shift;
            support.slot=support.transform.position;clampItem.slot=clampItem.transform.position;Pivot=clamp.Grip.position;
            var foot=support.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Base_VIS",StringComparison.Ordinal));
            if(!ContainsRect(Footprint(stationTop),Footprint(foot.bounds),.001f))throw new InvalidOperationException("The original pendulum base must fit completely on the identified worktop.");
        }
        Vector3 MainRodClampPoint()
        {
            var support=items.First(x=>x.assetId=="stand");
            var bottom=support.GetComponentsInChildren<Transform>().Single(x=>x.name.EndsWith("_RodBase_SOCKET",StringComparison.Ordinal));
            var top=support.GetComponentsInChildren<Transform>().Single(x=>x.name.EndsWith("_RodTop_SOCKET",StringComparison.Ordinal));
            return bottom.position+(top.position-bottom.position).normalized*(PendulumClampHeight-RodBottom);
        }
        Vector3 Slot(string id)
        {
            switch(id)
            {
                case "stand":var placed=items.FirstOrDefault(x=>x.assetId=="stand"&&x.installed);return placed?placed.transform.position:centre+new Vector3(-.10f,0,0);
                case "clamp":
                    var support=items.FirstOrDefault(x=>x.assetId=="stand");
                    if(support)return MainRodClampPoint();
                    return Pivot;
                case "pendulum":return Pivot-Vector3.up*(float)session.parameters["length"];
                case "string":return Pivot;
                case "ruler":return centre+new Vector3(-.40f,.004f,-.28f);
                case "timer":return centre+new Vector3(.38f,.004f,-.10f);
                default:return centre;
            }
        }
        static void PlaceOnSurface(GameObject go,Vector3 surface)
        {
            var renderers=go.GetComponentsInChildren<Renderer>().Where(x=>!(x is LineRenderer)&&x.enabled).ToArray();
            if(renderers.Length==0){go.transform.position=surface;return;}
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            go.transform.position+=surface-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        void PlaceInstalledOnSurface(GameObject go,Vector3 desired)
        {
            PlaceOnSurface(go,desired);
            var renderers=go.GetComponentsInChildren<Renderer>().Where(r=>!(r is LineRenderer)&&r.enabled).ToArray();
            if(renderers.Length==0)return;
            Quaternion original=go.transform.rotation,bestRotation=original;Vector3 bestDelta=Vector3.zero;float best=float.PositiveInfinity;
            foreach(var surface in storageSurfaces)for(int turn=0;turn<2;turn++)
            {
                go.transform.rotation=Quaternion.AngleAxis(turn*90,Vector3.up)*original;
                var b=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))b.Encapsulate(renderer.bounds);if(TryPestleBounds(go,out var pestleBounds))b=pestleBounds;
                var area=Footprint(surface.bounds,-StorageInset);
                if(b.size.x>area.width||b.size.z>area.height)continue;
                float x=Mathf.Clamp(desired.x,area.xMin+b.extents.x,area.xMax-b.extents.x),z=Mathf.Clamp(desired.z,area.yMin+b.extents.z,area.yMax-b.extents.z);
                float score=new Vector2(x-desired.x,z-desired.z).sqrMagnitude+turn*.0001f;
                if(score>=best)continue;best=score;bestRotation=go.transform.rotation;
                bestDelta=new Vector3(x-b.center.x,surface.bounds.max.y+StorageLift-b.min.y,z-b.center.z);
            }
            go.transform.rotation=bestRotation;if(best<float.PositiveInfinity)go.transform.position+=bestDelta;
        }
        void SeatLauncherFootAtLeftEdge(GameObject source)
        {
            var foot=source.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Base_VIS",StringComparison.Ordinal));
            var top=hasStationTop?stationTop:storageSurfaces[0].bounds;
            if(foot.bounds.size.x+2*StorageInset>top.size.x||foot.bounds.size.z+2*StorageInset>top.size.z)
                throw new InvalidOperationException("The original launcher base does not fit the selected worktop.");
            float x=top.min.x+StorageInset+foot.bounds.extents.x,z=Mathf.Clamp(Worktop.z,top.min.z+StorageInset+foot.bounds.extents.z,top.max.z-StorageInset-foot.bounds.extents.z);
            source.transform.position+=new Vector3(x-foot.bounds.center.x,top.max.y+StorageLift-foot.bounds.min.y,z-foot.bounds.center.z);
        }
        public void SeatLauncherAtLeftEdge(EquipmentHandle item)
        {
            SeatLauncherFootAtLeftEdge(item.gameObject);item.slot=item.transform.position;
        }
        public bool TryReceivingFloor(out Bounds floor)
        {
            var source=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.GetComponentInParent<EquipmentHandle>()&&r.name.IndexOf("floor",StringComparison.OrdinalIgnoreCase)>=0&&r.bounds.size.x>2&&r.bounds.size.z>2&&r.bounds.max.y<.25f&&r.bounds.size.y<.4f)
                .OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).FirstOrDefault();
            floor=source?source.bounds:default;return source;
        }
        public bool ValidateReceivingFloorTarget(out string failure)
        {
            failure="";if(!TryReceivingFloor(out var floor)){failure="The room has no identified receiving floor.";return false;}
            var item=items.FirstOrDefault(x=>x.assetId=="target");
            if(!item||!TryVisibleBounds(item,out var b)||!ContainsRect(Footprint(floor),Footprint(b),.001f)||Mathf.Abs(b.min.y-(floor.max.y+StorageLift))>.001f)
            {failure="The original receiving mat must rest fully on the actual floor.";return false;}
            return true;
        }
        public void RestInstalledTool(EquipmentHandle item)
        {
            PlaceInstalledOnSurface(item.gameObject,item.slot);item.slot=item.transform.position;
        }
        public bool ValidateUnusedCalorimetryRest(out string failure)
        {
            failure="";if(session.spec.id!="C08"||!Ready){failure="Install C08 before checking unused tool rests.";return false;}
            foreach(string key in new[]{"thermometer","stir"})
            {
                var item=items.Single(i=>i.assetId==key);var sockets=item.GetComponentsInChildren<Transform>();var tip=sockets.Single(t=>t.name.EndsWith("_Tip_SOCKET",StringComparison.Ordinal));
                var top=sockets.Single(t=>t.name.EndsWith(key=="stir"?"_Top_SOCKET":"_SensorTop_SOCKET",StringComparison.Ordinal));
                if(Mathf.Abs(Vector3.Dot((top.position-tip.position).normalized,Vector3.up))>.27f||!TryVisibleBounds(item,out var b)||!storageSurfaces.Any(s=>ContainsRect(Footprint(s.bounds),Footprint(b),.001f)&&Mathf.Abs(b.min.y-(s.bounds.max.y+StorageLift))<.001f))
                {failure="Unused "+key+" must rest nearly flat, completely supported by the actual bench.";return false;}
                if(key=="thermometer")
                {
                    var body=item.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Body_VIS",StringComparison.Ordinal));var probe=item.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Probe_VIS",StringComparison.Ordinal));
                    if(Mathf.Abs(body.bounds.min.y-probe.bounds.min.y)>.0005f){failure="Original thermometer housing and probe must share its natural two-point rest plane.";return false;}
                }
            }
            return true;
        }
        Renderer DerivedSolid(Renderer existing,string name,Vector3 point,Vector3 size,Quaternion rotation,Material material)
        {
            if(!existing)
            {
                var solid=GameObject.CreatePrimitive(PrimitiveType.Cube);solid.name=name;solid.transform.SetParent(transform,true);existing=solid.GetComponent<Renderer>();
                existing.sharedMaterial=material;
            }
            existing.transform.SetPositionAndRotation(point,rotation);existing.transform.localScale=size;return existing;
        }
        void SynchronizeDerivedFixtures()
        {
            RefreshGaugeFixtures();
            RefreshPhotogateFixtures();
            RefreshTransmissionFixtures();
            if(session.spec.id!="M06"||!Ready)return;
            var motor=items.Single(item=>item.assetId=="motor");var cam=items.Single(item=>item.assetId=="cam").GetComponentInChildren<VlabCam>();
            var mount=motor.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Mount_VIS",StringComparison.Ordinal));
            var guide=items.Single(item=>item.assetId=="guide");TryVisibleBounds(guide,out var sleeve);
            var top=storageSurfaces.OrderBy(surface=>XZDistance(surface.bounds.center,mount.bounds.center)).First().bounds;
            float floor=top.max.y+StorageLift,height=mount.bounds.min.y-floor;
            if(height<=.001f)throw new InvalidOperationException("M06 derived riser requires the actual measured motor-to-worktop gap.");
            motorRiser=DerivedSolid(motorRiser,"Derived M06 motor alignment riser - native 40 mm cam / 17 mm motor axes",
                new Vector3(mount.bounds.center.x,floor+height*.5f,mount.bounds.center.z),new Vector3(mount.bounds.size.x+.008f,height,mount.bounds.size.z+.008f),Quaternion.identity,mount.sharedMaterial);
            // A fork bears the original lower guide flange; its 7 mm open slot clears the 6 mm stem.
            // Rear columns lie behind the native 6 mm-thick cam's swept plane.
            var up=cam.FollowerDirection.normalized;var shaft=cam.ShaftAxis.normalized;
            if(Vector3.Dot(up,Vector3.up)<.999f||Mathf.Abs(shaft.y)>.001f)throw new InvalidOperationException("M06 bracket requires the delivered upright cam frame.");
            if(Vector3.Dot(shaft,mount.bounds.center-cam.Center)<0)shaft=-shaft;
            var across=Vector3.Cross(up,shaft).normalized;var rotation=Quaternion.LookRotation(shaft,up);
            var basePoint=new Vector3(sleeve.center.x,floor,sleeve.center.z);float plateTop=sleeve.min.y,plate=.004f,column=plateTop-plate-floor;
            if(column<=.01f)throw new InvalidOperationException("M06 sleeve support height is invalid.");
            while(guideBracket.Count<4)guideBracket.Add(null);
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;
                guideBracket[side]=DerivedSolid(guideBracket[side],"Derived M06 guide fork rear column "+(side+1),
                    basePoint+across*(sign*.032f)+shaft*.040f+up*(column*.5f),new Vector3(.006f,column,.016f),rotation,mount.sharedMaterial);
                guideBracket[side+2]=DerivedSolid(guideBracket[side+2],"Derived M06 guide flange fork arm "+(side+1)+" - 7 mm open stem slot",
                    basePoint+across*(sign*.01925f)+shaft*.020f+up*(plateTop-plate*.5f-floor),new Vector3(.0315f,plate,.064f),rotation,mount.sharedMaterial);
            }
        }
        sealed class BearingDatum{public string key;public Vector3 point;public Renderer bore;public bool driven;}
        List<BearingDatum> TransmissionBearings()
        {
            var result=new List<BearingDatum>();bool gears=session.spec.id=="M01";var item=items.Single(i=>i.assetId==(gears?"gears":"pulleys"));
            int driver=gears?(int)session.parameters["driver"]:items.Single(i=>i.assetId=="belt").GetComponentInChildren<VlabBelt>().DriveLarge?80:40;
            foreach(int size in gears?new[]{12,24,36}:new[]{40,80})
            {
                string socket=gears?"_Axis"+size+"_SOCKET":"_"+size+"_Axis_SOCKET";string bore=(size==12||size==40)?"_Bore_VIS":"_Bore"+size+"_VIS";
                result.Add(new BearingDatum{key=size.ToString(),point=item.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith(socket,StringComparison.Ordinal)).position,bore=item.GetComponentsInChildren<Renderer>(true).Single(r=>r.name.EndsWith(bore,StringComparison.Ordinal)),driven=size==driver});
            }
            return result;
        }
        public void RefreshTransmissionFixtures()
        {
            if(!Ready||(session.spec.id!="M01"&&session.spec.id!="M04"))return;
            var motor=items.Single(i=>i.assetId=="motor").GetComponentInChildren<VlabMotor>();var mount=motor.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Mount_VIS",StringComparison.Ordinal));
            var top=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,mount.bounds.center)).First().bounds;float floor=top.max.y+StorageLift,height=mount.bounds.min.y-floor;
            if(height<.001f)throw new InvalidOperationException("Transmission motor requires a positive measured foot riser.");
            motorRiser=DerivedSolid(motorRiser,"Derived "+session.spec.id+" motor foot alignment riser",new Vector3(mount.bounds.center.x,floor+height*.5f,mount.bounds.center.z),new Vector3(mount.bounds.size.x+.008f,height,mount.bounds.size.z+.008f),Quaternion.identity,mount.sharedMaterial);
            var bearings=TransmissionBearings();var rear=session.spec.id=="M01"?motor.Axis:-motor.Axis;var rotation=Quaternion.LookRotation(rear,Vector3.up);
            if(session.spec.id=="M01")
            {
                while(boardFeet.Count<6)boardFeet.Add(null);
                var board=items.Single(i=>i.assetId=="board").GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Board_VIS",StringComparison.Ordinal));var across=Vector3.Cross(Vector3.up,rear).normalized;
                for(int foot=0;foot<2;foot++)
                {
                    var xz=board.bounds.center+across*(foot==0?-.15f:.15f);var bench=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,xz)).First().bounds;float bottom=bench.max.y+StorageLift;
                    boardFeet[foot*3]=DerivedSolid(boardFeet[foot*3],"Derived M01 board broad footing "+foot,new Vector3(xz.x,bottom+.006f,xz.z),new Vector3(.050f,.012f,.120f),rotation,mount.sharedMaterial);
                    for(int side=0;side<2;side++)boardFeet[foot*3+side+1]=DerivedSolid(boardFeet[foot*3+side+1],"Derived M01 board lower-edge guide "+foot+" "+side,new Vector3(xz.x,bottom+.022f,xz.z)+rear*((side==0?-1:1)*.010f),new Vector3(.050f,.020f,.010f),rotation,mount.sharedMaterial);
                }
            }
            foreach(var bearing in bearings)
            {
                if(!transmissionCarriers.TryGetValue(bearing.key,out var parts)){parts=new Renderer[3];transmissionCarriers.Add(bearing.key,parts);}
                foreach(var part in parts)if(part)part.gameObject.SetActive(!bearing.driven);
                if(bearing.driven)continue;
                var supportTop=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,bearing.point)).First().bounds;float baseY=supportTop.max.y+StorageLift,plate=.008f;
                parts[0]=DerivedSolid(parts[0],"Derived "+session.spec.id+" bearing "+bearing.key+" carrier base",new Vector3(bearing.point.x,baseY+plate*.5f,bearing.point.z)+rear*.020f,new Vector3(.026f,plate,.080f),rotation,mount.sharedMaterial);
                float postHeight=bearing.point.y+.006f-(baseY+plate);var postXZ=bearing.point+rear*.040f;
                if(postHeight<=0)throw new InvalidOperationException("Original bearing axis needs clearance above its support plate.");
                parts[1]=DerivedSolid(parts[1],"Derived "+session.spec.id+" bearing "+bearing.key+" carrier post",new Vector3(postXZ.x,baseY+plate+postHeight*.5f,postXZ.z),new Vector3(.012f,postHeight,.012f),rotation,mount.sharedMaterial);
                if(!parts[2]){var pin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pin.name="Derived "+session.spec.id+" bearing "+bearing.key+" 6 mm axle";pin.transform.SetParent(transform,true);parts[2]=pin.GetComponent<Renderer>();parts[2].sharedMaterial=mount.sharedMaterial;}
                parts[2].transform.SetPositionAndRotation(bearing.point+rear*.020f,Quaternion.FromToRotation(Vector3.up,rear));parts[2].transform.localScale=new Vector3(.006f,.026f,.006f);
                foreach(var part in parts)part.gameObject.SetActive(true);
            }
        }
        static Vector3 BoreAxis(Renderer bore)
        {
            var size=bore.localBounds.size;var axis=size.x<size.y&&size.x<size.z?Vector3.right:size.y<size.z?Vector3.up:Vector3.forward;
            return bore.transform.TransformDirection(axis).normalized;
        }
        public bool ValidateTransmissionSupport(out string failure)
        {
            failure="";if(!Ready||(session.spec.id!="M01"&&session.spec.id!="M04")){failure="Install the complete transmission before its support check.";return false;}
            bool OnBench(Bounds b)=>storageSurfaces.Any(s=>ContainsRect(Footprint(s.bounds),Footprint(b),.001f)&&Mathf.Abs(b.min.y-(s.bounds.max.y+StorageLift))<.001f);
            var motor=items.Single(i=>i.assetId=="motor").GetComponentInChildren<VlabMotor>();var mount=motor.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Mount_VIS",StringComparison.Ordinal));
            if(!motorRiser||!OnBench(motorRiser.bounds)||!ContainsRect(Footprint(motorRiser.bounds),Footprint(mount.bounds),.0005f)||Mathf.Abs(motorRiser.bounds.max.y-mount.bounds.min.y)>.0005f)
            {failure="Transmission motor foot must contact a complete real-bench-supported riser.";return false;}
            var bearings=TransmissionBearings();var driven=bearings.Single(b=>b.driven);var shaft=motor.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Shaft_VIS",StringComparison.Ordinal));
            if(Vector3.Distance(motor.Tip.position,driven.point-motor.Axis*.006f)>.0001f||Mathf.Abs(Vector3.Dot(BoreAxis(driven.bore),motor.Axis))<.9999f||Vector3.ProjectOnPlane(driven.bore.bounds.center-motor.Tip.position,motor.Axis).magnitude>.0001f)
            {failure="Original motor shaft must be coaxial with the chosen real bronze bore and project 6 mm past its centre.";return false;}
            if(RendererMinimum(shaft,motor.Axis)>RendererMinimum(driven.bore,motor.Axis)+.0001f||-RendererMinimum(shaft,-motor.Axis)<-RendererMinimum(driven.bore,-motor.Axis)-.0001f)
            {failure="Original 6 mm motor shaft must engage the full axial bronze bearing thickness.";return false;}
            foreach(var bearing in bearings.Where(b=>!b.driven))
            {
                if(!transmissionCarriers.TryGetValue(bearing.key,out var parts)||parts.Any(p=>!p||!p.gameObject.activeInHierarchy)||!OnBench(parts[0].bounds)||!ContainsRect(Footprint(parts[0].bounds),Footprint(parts[1].bounds),.0005f)||Mathf.Abs(parts[1].bounds.min.y-parts[0].bounds.max.y)>.0005f)
                {failure="Bearing "+bearing.key+" requires complete carrier base/post support on the actual bench.";return false;}
                var axis=parts[2].transform.up;
                if(Mathf.Abs(Vector3.Dot(BoreAxis(bearing.bore),axis))<.9999f||Vector3.ProjectOnPlane(bearing.bore.bounds.center-parts[2].transform.position,axis).magnitude>.0001f||Mathf.Abs(parts[2].transform.localScale.x-.006f)>.00001f||!parts[2].bounds.Intersects(parts[1].bounds)||RendererMinimum(parts[2],axis)>RendererMinimum(bearing.bore,axis)+.0001f||-RendererMinimum(parts[2],-axis)<-RendererMinimum(bearing.bore,-axis)-.0001f)
                {failure="Bearing "+bearing.key+" original bore needs a coaxial 6 mm pin through its complete thickness and into the real post.";return false;}
            }
            if(session.spec.id=="M04")
            {
                var belt=items.Single(i=>i.assetId=="belt").GetComponentInChildren<VlabBelt>();
                foreach(var bearing in bearings)
                {
                    string suffix=bearing.key=="40"?"_PulleyA_SOCKET":"_PulleyB_SOCKET";var point=belt.Current.root.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith(suffix,StringComparison.Ordinal)).position;
                    if(Vector3.Distance(point,bearing.point)>.0001f){failure="Original belt pitch datums must coincide with both physically supported original pulley bores.";return false;}
                }
                float bench=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,belt.transform.position)).First().bounds.max.y;
                if(RendererMinimum(belt.Current.surface,Vector3.up)<bench+.010f){failure="Moving original belt must clear the new carrier plates and the actual worktop.";return false;}
            }
            if(session.spec.id=="M01")
            {
                var board=items.Single(i=>i.assetId=="board").GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Board_VIS",StringComparison.Ordinal));
                if(boardFeet.Count!=6||boardFeet.Any(r=>!r)){failure="Original upright board requires both broad separate footings and lower-edge guides.";return false;}
                for(int foot=0;foot<2;foot++)
                {
                    var plate=boardFeet[foot*3];if(!OnBench(plate.bounds)||Mathf.Abs(plate.bounds.max.y-board.bounds.min.y)>.0005f||!Footprint(plate.bounds).Overlaps(Footprint(board.bounds)))
                    {failure="Original board lower edge must actually bear on both complete bench-supported footings.";return false;}
                    for(int side=0;side<2;side++)if(Mathf.Abs(boardFeet[foot*3+side+1].bounds.min.y-plate.bounds.max.y)>.0005f)
                    {failure="Board edge guides must join their actual footing, not float beside the source panel.";return false;}
                    var normal=motor.Axis;
                    if(Mathf.Abs(-RendererMinimum(boardFeet[foot*3+1],-normal)-RendererMinimum(board,normal))>.0005f||Mathf.Abs(RendererMinimum(boardFeet[foot*3+2],normal)+RendererMinimum(board,-normal))>.0005f)
                    {failure="Both derived board guides must contact the original solid panel faces at its lower edge.";return false;}
                }
                if(motor.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.Intersects(board.bounds))||transmissionCarriers.Values.SelectMany(p=>p).Any(r=>r&&r.gameObject.activeInHierarchy&&r.bounds.Intersects(board.bounds)))
                {failure="Motor and separate carriers must clear the solid pegboard; its texture holes are not openings.";return false;}
            }
            return true;
        }
        public void RefreshGaugeFixtures()
        {
            if(!Ready||(session.spec.id!="M06"&&session.spec.id!="M08"))return;
            var item=items.Single(x=>x.assetId=="gauge");var gauge=item.GetComponentInChildren<VlabDialGauge>();
            Transform Socket(string suffix)=>item.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith(suffix,StringComparison.Ordinal));
            var bolt=Socket("_StandBolt_SOCKET");var face=Socket("_DialCenter_SOCKET");
            var rear=Vector3.ProjectOnPlane(bolt.position-face.position,Vector3.up).normalized;
            if(rear.sqrMagnitude<.99f)throw new InvalidOperationException("Gauge mounting bore must remain horizontal.");
            var top=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,bolt.position)).First().bounds;
            float floor=top.max.y+StorageLift,baseHeight=.010f;var material=item.GetComponentsInChildren<Renderer>().First(r=>r.name.EndsWith("_StandMount_VIS",StringComparison.Ordinal)).sharedMaterial;
            var postXZ=bolt.position+rear*.040f;var rotation=Quaternion.LookRotation(rear,Vector3.up);
            gaugeBase=DerivedSolid(gaugeBase,"Derived "+session.spec.id+" gauge stand base",new Vector3(postXZ.x,floor+baseHeight*.5f,postXZ.z),new Vector3(.060f,baseHeight,.055f),rotation,material);
            float bottom=floor+baseHeight,topY=bolt.position.y+.006f;
            if(topY<=bottom)throw new InvalidOperationException("Gauge stand has no supported post height.");
            gaugePost=DerivedSolid(gaugePost,"Derived "+session.spec.id+" gauge stand rear post",new Vector3(postXZ.x,(bottom+topY)*.5f,postXZ.z),new Vector3(.012f,topY-bottom,.012f),rotation,material);
            if(!gaugePin)
            {
                var pin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pin.name="Derived "+session.spec.id+" gauge 6 mm bore mounting pin";pin.transform.SetParent(transform,true);gaugePin=pin.GetComponent<Renderer>();gaugePin.sharedMaterial=material;
            }
            gaugePin.transform.SetPositionAndRotation(bolt.position+rear*.020f,Quaternion.FromToRotation(Vector3.up,rear));gaugePin.transform.localScale=new Vector3(.006f,.026f,.006f);
            if(session.spec.id=="M08")
            {
                while(beamRisers.Count<2)beamRisers.Add(null);
                var supports=items.Single(x=>x.assetId=="supports");int index=0;
                foreach(var source in supports.GetComponentsInChildren<Renderer>().Where(r=>r.name.EndsWith("_SupportL_VIS",StringComparison.Ordinal)||r.name.EndsWith("_SupportR_VIS",StringComparison.Ordinal)).OrderBy(r=>r.name))
                {
                    var b=source.bounds;var supportTop=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,b.center)).First().bounds;float baseY=supportTop.max.y+StorageLift,height=b.min.y-baseY;
                    if(height<.001f)throw new InvalidOperationException("Original beam support must bear on its separate riser.");
                    beamRisers[index]=DerivedSolid(beamRisers[index],"Derived M08 original support riser "+(index+1),new Vector3(b.center.x,baseY+height*.5f,b.center.z),new Vector3(b.size.x+.008f,height,b.size.z+.008f),Quaternion.identity,material);index++;
                }
            }
        }
        public bool ValidateGaugeSupport(out string failure)
        {
            failure="";if(!Ready||(session.spec.id!="M06"&&session.spec.id!="M08")){failure="Install the complete gauge assembly first.";return false;}
            bool OnBench(Bounds b)=>storageSurfaces.Any(s=>ContainsRect(Footprint(s.bounds),Footprint(b),.001f)&&Mathf.Abs(b.min.y-(s.bounds.max.y+StorageLift))<.001f);
            if(!gaugeBase||!gaugePost||!gaugePin||!OnBench(gaugeBase.bounds)||!ContainsRect(Footprint(gaugeBase.bounds),Footprint(gaugePost.bounds),.0005f)||Mathf.Abs(gaugePost.bounds.min.y-gaugeBase.bounds.max.y)>.0005f)
            {failure="Gauge stand needs a complete supported base and actual base/post contact.";return false;}
            var item=items.Single(x=>x.assetId=="gauge");var gauge=item.GetComponentInChildren<VlabDialGauge>();
            var bolt=item.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_StandBolt_SOCKET",StringComparison.Ordinal));
            var pinAxis=gaugePin.transform.up;var offset=bolt.position-gaugePin.bounds.center;
            if(Vector3.ProjectOnPlane(offset,pinAxis).magnitude>.0001f||Mathf.Abs(Vector3.Dot(offset,pinAxis))>.026f||Mathf.Abs(gaugePin.transform.localScale.x-.006f)>.00001f||!gaugePin.bounds.Intersects(gaugePost.bounds))
            {failure="The real 6.246 mm gauge bore must be coaxial with its 6 mm pin and supported rear post.";return false;}
            float bench=storageSurfaces.OrderBy(s=>XZDistance(s.bounds.center,gauge.Contact.position)).First().bounds.max.y;
            if(item.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Any(r=>r.bounds.min.y<bench+StorageLift-.0005f))
            {failure="Original gauge body or spindle intersects the real worktop.";return false;}
            if(session.spec.id=="M06")
            {
                var follower=items.Single(x=>(x.assetId=="follower"||x.assetId=="roller")&&x.gameObject.activeInHierarchy);
                var contact=follower.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_StemTop_SOCKET",StringComparison.Ordinal));
                if(Vector3.Distance(gauge.Contact.position,contact.position)>.0001f){failure="Original gauge tip must touch the active follower stem top throughout its cycle.";return false;}
            }
            else
            {
                var beam=items.Single(x=>x.assetId=="beam").GetComponentInChildren<VlabBeam>();
                if(Vector3.Distance(gauge.Contact.position,beam.Gauge.position)>.0001f){failure="Original underside probe must touch the current beam midspan datum.";return false;}
                var supports=items.Single(x=>x.assetId=="supports").GetComponentsInChildren<Renderer>().Where(r=>r.name.EndsWith("_SupportL_VIS",StringComparison.Ordinal)||r.name.EndsWith("_SupportR_VIS",StringComparison.Ordinal)).OrderBy(r=>r.name).ToArray();
                if(supports.Length!=2||beamRisers.Count!=2){failure="Both original support feet need independent risers.";return false;}
                for(int i=0;i<2;i++)if(!beamRisers[i]||!OnBench(beamRisers[i].bounds)||!ContainsRect(Footprint(beamRisers[i].bounds),Footprint(supports[i].bounds),.0005f)||Mathf.Abs(beamRisers[i].bounds.max.y-supports[i].bounds.min.y)>.0005f)
                {failure="Original support foot must contact its full bench-supported riser.";return false;}
            }
            return true;
        }
        public void RefreshPhotogateFixtures()
        {
            if(!Ready||(session.spec.id!="P07"&&session.spec.id!="P08"))return;
            var track=items.Single(i=>i.assetId=="track").GetComponentInChildren<VlabTrack>();var up=track.CartFrame.up;var along=track.CartFrame.right;var across=Vector3.Cross(along,up).normalized;var rotation=Quaternion.LookRotation(across,up);
            var material=track.GetComponentsInChildren<Renderer>().First(r=>r.name.EndsWith("_GateSockets_VIS",StringComparison.Ordinal)).sharedMaterial;
            foreach(string key in new[]{"gateA","gateB"})
            {
                var gate=items.Single(i=>i.assetId==key);Transform Socket(string suffix)=>gate.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith(suffix,StringComparison.Ordinal));
                var beam=(Socket("_BeamStart_SOCKET").position+Socket("_BeamEnd_SOCKET").position)*.5f;
                if(!photogateSeats.TryGetValue(key,out var parts)){parts=new Renderer[5];photogateSeats.Add(key,parts);}
                // A separate rigid saddle bears on the original rail underside (beam147 - underside60 = 87 mm).
                parts[0]=DerivedSolid(parts[0],"Derived "+session.spec.id+" "+key+" rail underside saddle",beam-up*.090f,new Vector3(.028f,.006f,.132f),rotation,material);
                for(int side=0;side<2;side++)
                {
                    float sign=side==0?-1:1;
                    parts[side+1]=DerivedSolid(parts[side+1],"Derived "+session.spec.id+" "+key+" seat upright "+side,beam-up*.0615f+across*(sign*.060f),new Vector3(.028f,.051f,.012f),rotation,material);
                    parts[side+3]=DerivedSolid(parts[side+3],"Derived "+session.spec.id+" "+key+" native frame seat "+side,beam-up*.033f+across*(sign*.046f),new Vector3(.028f,.006f,.042f),rotation,material);
                }
            }
        }
        static float RendererMinimum(Renderer renderer,Vector3 axis)
        {
            var bounds=renderer.localBounds;float result=float.PositiveInfinity;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                result=Mathf.Min(result,Vector3.Dot(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))),axis));
            return result;
        }
        public bool ValidatePhotogateSupports(out string failure)
        {
            failure="";if(!Ready||!new[]{"P04","P07","P08"}.Contains(session.spec.id)){failure="Install the complete track/photogate assembly first.";return false;}
            var track=items.Single(i=>i.assetId=="track").GetComponentInChildren<VlabTrack>();var up=track.CartFrame.up;var along=track.CartFrame.right;var across=Vector3.Cross(along,up).normalized;
            var rail=track.GetComponentsInChildren<MeshCollider>().Single(c=>c.name.EndsWith("_Rail_VIS",StringComparison.Ordinal));
            Physics.SyncTransforms();
            foreach(string key in new[]{"gateA","gateB"})
            {
                var item=items.Single(i=>i.assetId==key);Transform Socket(string suffix)=>item.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith(suffix,StringComparison.Ordinal));
                var gate=item.GetComponentInChildren<VlabPhotogate>();var beam=(Socket("_BeamStart_SOCKET").position+Socket("_BeamEnd_SOCKET").position)*.5f;
                var frame=item.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Frame_VIS",StringComparison.Ordinal));
                if(Vector3.Dot(gate.TravelAxis,along)<.9999f||Mathf.Abs(RendererMinimum(frame,up)-(Vector3.Dot(beam,up)-.030f))>.0001f)
                {failure=key+" source frame legs and optical beam must retain the real 30 mm seating height and track orientation.";return false;}
                var underside=beam-up*.087f;
                if(!rail.Raycast(new Ray(underside-up*.003f,up),out var hit,.006f)||Vector3.Distance(hit.point,underside)>.0002f)
                {failure=key+" beam height must agree with the actual original rail underside, not a free-space coordinate.";return false;}
                Collider nativeSeats=null;Renderer[] parts=null;
                if(session.spec.id=="P04")nativeSeats=track.GetComponentsInChildren<MeshCollider>().Single(c=>c.name.EndsWith("_GateSockets_VIS",StringComparison.Ordinal));
                else if(!photogateSeats.TryGetValue(key,out parts)||parts.Any(p=>!p))
                {failure=key+" needs its complete separately derived movable-position saddle.";return false;}
                if(parts!=null)
                {
                    if(Vector3.Distance(parts[0].transform.position+up*.003f,underside)>.0001f)
                    {failure=key+" saddle upper surface must actually touch the rail underside.";return false;}
                    for(int side=0;side<2;side++)
                    {
                        var post=parts[side+1];var seat=parts[side+3];
                        if(Mathf.Abs(Vector3.Dot(post.transform.position-up*(post.transform.localScale.y*.5f)-(parts[0].transform.position+up*.003f),up))>.0001f||Mathf.Abs(Vector3.Dot(post.transform.position+up*(post.transform.localScale.y*.5f)-(seat.transform.position-up*.003f),up))>.0001f)
                        {failure=key+" saddle, upright and seat lack actual connected support planes.";return false;}
                    }
                }
                for(int side=0;side<2;side++)
                {
                    float sign=side==0?-1:1;var foot=beam-up*.030f+across*(sign*.031f);Collider seat=parts==null?nativeSeats:parts[side+3].GetComponent<Collider>();
                    if(!seat.Raycast(new Ray(foot+up*.002f,-up),out var contact,.004f)||Vector3.Distance(contact.point,foot)>.0002f)
                    {failure=key+" both original frame legs must contact actual seat geometry.";return false;}
                }
            }
            return true;
        }
        public bool ValidateDerivedM06Supports(out string failure)
        {
            failure="";if(session.spec.id!="M06"||!Ready){failure="Install M06 before checking its explicitly derived supports.";return false;}
            var motor=items.Single(item=>item.assetId=="motor").GetComponentInChildren<VlabMotor>();var cam=items.Single(item=>item.assetId=="cam").GetComponentInChildren<VlabCam>();
            var mount=motor.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Mount_VIS",StringComparison.Ordinal));
            var guide=items.Single(item=>item.assetId=="guide");TryVisibleBounds(guide,out var sleeve);
            if(!motorRiser||guideBracket.Count!=4||guideBracket.Any(r=>!r)){failure="The derived motor riser or guide fork is missing.";return false;}
            bool OnBench(Bounds b)=>storageSurfaces.Any(surface=>ContainsRect(Footprint(surface.bounds),Footprint(b),.001f)&&Mathf.Abs(b.min.y-(surface.bounds.max.y+StorageLift))<.001f);
            if(!OnBench(motorRiser.bounds)||!ContainsRect(Footprint(motorRiser.bounds),Footprint(mount.bounds),.001f)||Mathf.Abs(mount.bounds.min.y-motorRiser.bounds.max.y)>.0005f)
            {failure="Motor foot must contact the riser top, with the complete riser foot on the real bench.";return false;}
            if(Vector3.Distance(motor.Tip.position,cam.Center)>.0005f||Vector3.Dot(motor.Axis,cam.ShaftAxis)<.9999f)
            {failure="Original 6 mm motor shaft and cam bore axes do not coincide.";return false;}
            for(int side=0;side<2;side++)
            {
                var post=guideBracket[side];var arm=guideBracket[side+2];
                if(!OnBench(post.bounds)||Mathf.Abs(post.bounds.max.y-arm.bounds.min.y)>.0005f||Mathf.Abs(arm.bounds.max.y-sleeve.min.y)>.0005f)
                {failure="Guide fork columns, fork arms and original lower flange lack actual vertical contact.";return false;}
                if(!Footprint(arm.bounds).Overlaps(Footprint(sleeve))){failure="Guide flange does not overlap both supporting fork arms.";return false;}
            }
            var right=guideBracket[2].transform.right;var a=guideBracket[2];var b=guideBracket[3];
            float gap=Mathf.Abs(Vector3.Dot(a.bounds.center-b.bounds.center,right))-(a.transform.localScale.x+b.transform.localScale.x)*.5f;
            if(gap<.0068f){failure="Derived fork's open slot must clear the original 6 mm follower stem.";return false;}
            var camBody=cam.Body.GetComponent<Renderer>();
            var stem=items.Where(item=>(item.assetId=="follower"||item.assetId=="roller")&&item.gameObject.activeInHierarchy)
                .SelectMany(item=>item.GetComponentsInChildren<Renderer>()).Single(r=>r.name.EndsWith("_Stem_VIS",StringComparison.Ordinal));
            foreach(var fixture in guideBracket)
            {
                if(camBody&&fixture.bounds.Intersects(camBody.bounds)){failure="Derived guide bracket intersects the original moving cam body.";return false;}
                if(fixture.bounds.Intersects(stem.bounds)){failure="Derived guide bracket intersects the original moving follower stem.";return false;}
            }
            return true;
        }
        // Bounds come from the current room's delivered worktops, never a procedural table.
        // The non-rendered synthetic rectangle exists only for isolated native fixtures.
        void FindStorageSurfaces()
        {
            var tops=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>
                !r.GetComponentInParent<EquipmentHandle>()&&!r.transform.IsChildOf(transform)&&r.enabled&&
                r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&r.bounds.max.y>.70f&&r.bounds.max.y<1.15f&&
                (r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0))
                .OrderBy(r=>XZDistance(r.bounds.center,Worktop)).ToArray();
            var primary=tops.FirstOrDefault(r=>ContainsXZ(r.bounds,Worktop,.01f)&&Mathf.Abs(r.bounds.max.y-Worktop.y)<.025f);
            if(primary)
            {
                stationTop=primary.bounds;hasStationTop=true;
                foreach(var top in tops.OrderBy(r=>r==primary?0:1).ThenBy(r=>XZDistance(r.bounds.center,Worktop)))
                {
                    if(XZDistance(top.bounds.center,primary.bounds.center)>3.0f||Mathf.Abs(top.bounds.max.y-primary.bounds.max.y)>.15f)continue;
                    if(storageSurfaces.Any(x=>(x.bounds.center-top.bounds.center).sqrMagnitude<.0001f))continue;
                    storageSurfaces.Add(new StorageSurface{bounds=top.bounds,name=top.name});
                }
                return;
            }
            UsesSyntheticStorageSurface=true;
            storageSurfaces.Add(new StorageSurface{bounds=new Bounds(Worktop-Vector3.up*.015f,new Vector3(2.4f,.02f,1.2f)),name="Synthetic fixture support (no room worktop)"});
        }
        static float XZDistance(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
        static bool ContainsXZ(Bounds b,Vector3 p,float tolerance)=>p.x>=b.min.x-tolerance&&p.x<=b.max.x+tolerance&&p.z>=b.min.z-tolerance&&p.z<=b.max.z+tolerance;
        static Rect Footprint(Bounds b,float margin=0)=>new Rect(b.min.x-margin,b.min.z-margin,b.size.x+2*margin,b.size.z+2*margin);
        static bool ContainsRect(Rect outer,Rect inner,float tolerance=0)=>inner.xMin>=outer.xMin-tolerance&&inner.xMax<=outer.xMax+tolerance&&inner.yMin>=outer.yMin-tolerance&&inner.yMax<=outer.yMax+tolerance;
        static List<Rect> Subtract(List<Rect> free,Rect blocked)
        {
            var result=new List<Rect>();
            foreach(var r in free)
            {
                if(!r.Overlaps(blocked)){result.Add(r);continue;}
                float x0=Mathf.Max(r.xMin,blocked.xMin),x1=Mathf.Min(r.xMax,blocked.xMax),z0=Mathf.Max(r.yMin,blocked.yMin),z1=Mathf.Min(r.yMax,blocked.yMax);
                if(x0>r.xMin)result.Add(Rect.MinMaxRect(r.xMin,r.yMin,x0,r.yMax));
                if(x1<r.xMax)result.Add(Rect.MinMaxRect(x1,r.yMin,r.xMax,r.yMax));
                if(z0>r.yMin)result.Add(Rect.MinMaxRect(x0,r.yMin,x1,z0));
                if(z1<r.yMax)result.Add(Rect.MinMaxRect(x0,z1,x1,r.yMax));
            }
            return result.Where(r=>r.width>.003f&&r.height>.003f).ToList();
        }
        static void RefreshInstances(EquipmentHandle item)
        {
            foreach(var chips in item.GetComponentsInChildren<VlabChips>())chips.Configure(chips.SizeIndex,chips.BatchUnits);
        }
        public static bool TryVisibleBounds(EquipmentHandle item,out Bounds bounds)
        {
            if(TryPestleBounds(item.gameObject,out bounds))return true;
            RefreshInstances(item);bounds=default;bool found=false;
            foreach(var r in item.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
                Bounds b;
                if(r is LineRenderer line)
                {
                    if(line.positionCount==0)continue;
                    Vector3 p=line.GetPosition(0);if(!line.useWorldSpace)p=line.transform.TransformPoint(p);b=new Bounds(p,Vector3.zero);
                    for(int n=1;n<line.positionCount;n++){p=line.GetPosition(n);if(!line.useWorldSpace)p=line.transform.TransformPoint(p);b.Encapsulate(p);}
                    b.Expand(Mathf.Max(line.startWidth,line.endWidth)*line.widthMultiplier);
                }
                else b=r.bounds;
                if(found)bounds.Encapsulate(b);else{bounds=b;found=true;}
            }
            foreach(var chips in item.GetComponentsInChildren<VlabChips>())
            {
                if(!chips.isActiveAndEnabled||!chips.SharedMesh)continue;
                var b=chips.SharedMesh.bounds;
                for(int n=0;n<chips.Count;n++)for(int corner=0;corner<8;corner++)
                {
                    var p=b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    p=chips.MatrixAt(n).MultiplyPoint3x4(p);if(found)bounds.Encapsulate(p);else{bounds=new Bounds(p,Vector3.zero);found=true;}
                }
            }
            return found;
        }
        static bool TryPestleBounds(GameObject source,out Bounds bounds)
        {
            bounds=default;var pestle=source.GetComponentInChildren<VlabPestle>();if(!pestle||!pestle.gameObject.activeInHierarchy)return false;
            var mesh=pestle.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name.EndsWith("_Pestle_VIS",StringComparison.Ordinal));if(!mesh.enabled)return false;
            var sockets=pestle.GetComponentsInChildren<Transform>();var bottom=sockets.Single(t=>t.name.EndsWith("_Bottom_SOCKET",StringComparison.Ordinal));var grip=sockets.Single(t=>t.name.EndsWith("_Grip_SOCKET",StringComparison.Ordinal));
            var axis=(grip.position-bottom.position).normalized;var radial=Vector3.ProjectOnPlane(mesh.transform.right,axis).normalized;var other=Vector3.Cross(axis,radial).normalized;
            // Exact original 32-sided turned profile from Production_Evidence/build_pestle.py.
            // This bounds the unchanged source mesh; it creates no geometry or collision substitute.
            var profile=new[]{new Vector2(.0006f,-.085f),new Vector2(.005f,-.084f),new Vector2(.009f,-.081f),new Vector2(.010f,-.076f),new Vector2(.0095f,-.068f),new Vector2(.007f,-.050f),new Vector2(.0055f,-.015f),new Vector2(.0055f,.015f),new Vector2(.006f,.023f),new Vector2(.004f,.025f)};
            bool found=false;
            foreach(var ring in profile)for(int side=0;side<32;side++)
            {
                float angle=2*Mathf.PI*side/32;var point=grip.position+axis*ring.y+(radial*Mathf.Cos(angle)+other*Mathf.Sin(angle))*ring.x;
                if(found)bounds.Encapsulate(point);else{bounds=new Bounds(point,Vector3.zero);found=true;}
            }
            return true;
        }
        void PrepareLoosePose(EquipmentHandle item)
        {
            item.gameObject.SetActive(true);item.stowed=false;item.transform.rotation=item.rotation;
            var source=Driver?.Kit.FirstOrDefault(k=>k.key==item.assetId)?.prefab??item.assetId;
            // Unsupported narrow glass is stored flat. Installed sockets retain the captured source pose.
            bool flat=source=="pipette"||source=="burette"||source=="thermometer"||source=="stir_rod"||source=="capillary"||source=="ruler";
            if(flat&&TryVisibleBounds(item,out var b)&&b.size.y>Mathf.Max(b.size.x,b.size.z)*1.5f)
                item.transform.rotation=Quaternion.AngleAxis(90,Vector3.forward)*item.rotation;
            foreach(var line in item.GetComponentsInChildren<VlabString>())line.TrySetEndpoints(item.transform.position,item.transform.position+Vector3.right*.16f);
            item.trayRotation=item.transform.rotation;
        }
        void ArrangeLooseItems(EquipmentHandle preferred=null)
        {
            var free=storageSurfaces.Select(s=>new List<Rect>{Footprint(s.bounds,-StorageInset)}).ToArray();
            foreach(var installed in items.Where(x=>x.installed&&x.gameObject.activeInHierarchy))
                if(TryVisibleBounds(installed,out var occupied))for(int n=0;n<free.Length;n++)
                    if(occupied.max.y>=storageSurfaces[n].bounds.max.y-.01f&&occupied.min.y<=storageSurfaces[n].bounds.max.y+.10f)
                        free[n]=Subtract(free[n],Footprint(occupied,StorageGap*.5f));
            var loose=items.Where(x=>!x.installed).ToArray();foreach(var item in loose)PrepareLoosePose(item);
            var ordered=loose.OrderByDescending(x=>x==preferred).ThenByDescending(x=>TryVisibleBounds(x,out var b)?Mathf.Max(b.size.x,b.size.z):0).ThenByDescending(x=>TryVisibleBounds(x,out var b)?b.size.x*b.size.z:0).ToArray();
            foreach(var item in ordered)
            {
                if(!TryVisibleBounds(item,out var original))
                {
                    // State-only native components (e.g. an absent precipitate) have no visible footprint.
                    item.trayPosition=item.transform.position=Worktop;continue;
                }
                int bestSurface=-1;Rect bestRect=default;Quaternion bestRotation=item.trayRotation;float bestScore=float.PositiveInfinity;Bounds bestBounds=default;
                // Keep tall originals facing their authored direction; turning a thin pegboard
                // sideways may pack efficiently but makes the actual instrument unreadable.
                int turns=item.GetComponentInChildren<VlabCrank>()||(original.size.y>.12f&&original.size.y>Mathf.Min(original.size.x,original.size.z)*1.5f)?1:2;
                for(int surface=0;surface<free.Length;surface++)for(int turn=0;turn<turns;turn++)
                {
                    item.transform.rotation=Quaternion.AngleAxis(turn*90,Vector3.up)*item.trayRotation;
                    if(!TryVisibleBounds(item,out var b))continue;
                    float width=Mathf.Max(.008f,b.size.x)+StorageGap,depth=Mathf.Max(.008f,b.size.z)+StorageGap;
                    foreach(var r in free[surface])
                    {
                        if(width>r.width+.0001f||depth>r.height+.0001f)continue;
                        float score=surface*1000+Mathf.Min(r.width-width,r.height-depth)+.01f*(r.width*r.height-width*depth);
                        if(score>=bestScore)continue;
                        bestScore=score;bestSurface=surface;bestRect=new Rect(r.xMax-width,r.yMax-depth,width,depth);bestRotation=item.transform.rotation;bestBounds=b;
                    }
                }
                if(bestSurface<0)
                {
                    item.transform.rotation=item.trayRotation;item.trayPosition=item.transform.position;item.stowed=true;item.gameObject.SetActive(false);continue;
                }
                item.transform.rotation=bestRotation;TryVisibleBounds(item,out bestBounds);
                var point=new Vector3(bestRect.xMin+bestRect.width*.5f,storageSurfaces[bestSurface].bounds.max.y+StorageLift,bestRect.yMin+bestRect.height*.5f);
                item.transform.position+=point-new Vector3(bestBounds.center.x,bestBounds.min.y,bestBounds.center.z);
                item.trayPosition=item.transform.position;item.trayRotation=item.transform.rotation;RefreshInstances(item);
                for(int n=0;n<free.Length;n++)free[n]=Subtract(free[n],bestRect);
            }
        }
        public bool ValidateLooseLayout(out string failure)
        {
            var visible=new List<KeyValuePair<string,Bounds>>();
            foreach(var item in items.Where(x=>!x.installed))
            {
                if(item.stowed){if(item.gameObject.activeSelf){failure=item.assetId+" is stored but still visible.";return false;}continue;}
                if(!item.gameObject.activeInHierarchy){failure=item.assetId+" is neither visible nor disclosed as stored.";return false;}
                if(!TryVisibleBounds(item,out var b))
                {
                    if(InvisibleNativeItemReason(item)!="")continue;
                    failure=session.spec.id+" loose "+item.assetId+" has no visible source geometry and no declared empty-state reason.";return false;
                }
                bool supported=storageSurfaces.Any(s=>ContainsRect(Footprint(s.bounds),Footprint(b),.002f)&&Mathf.Abs(b.min.y-(s.bounds.max.y+StorageLift))<=.004f);
                if(!supported){failure=session.spec.id+" loose "+item.assetId+" is outside a worktop or not resting on it: "+b;return false;}
                foreach(var other in visible)if(Footprint(other.Value,StorageGap*.25f).Overlaps(Footprint(b,StorageGap*.25f)))
                {failure=session.spec.id+" loose footprints overlap: "+other.Key+" / "+item.assetId;return false;}
                visible.Add(new KeyValuePair<string,Bounds>(item.assetId,b));
            }
            failure="";return true;
        }
        static string InvisibleNativeItemReason(EquipmentHandle item)
        {
            if(item.GetComponent<VlabPrecipitate>())return "Native AgCl visual is absent until the reaction produces qualitative sediment.";
            if(item.GetComponent<VlabDnaPrecipitate>())return "Native DNA visual is absent until the qualitative extraction step.";
            if(item.GetComponent<VlabLiquid>())return "Native liquid-only proxy is empty before a transfer.";
            return "";
        }
        // Only named source assemblies can be above/below the table; ordinary seated items stay checked.
        public string InstalledSupportException(string key)
        {
            string id=session.spec.id;
            if(id=="P02"&&key=="ball")
            {
                var launcher=items.First(item=>item.assetId=="launcher").GetComponentInChildren<VlabLauncher>();var ball=items.First(item=>item.assetId=="ball").GetComponentInChildren<VlabProjectile>();
                var load=launcher.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.EndsWith("_Load_SOCKET",StringComparison.Ordinal));
                if(launcher.Loaded==ball&&load&&Vector3.Distance(ball.transform.position,load.position)<.001f)return "Actual ball is seated at the supplied launcher's load socket.";
                if(ball.IsFlying)return "Actual native projectile is in its permitted analytic flight, not a static seated instrument.";
                var target=items.First(item=>item.assetId=="target").GetComponentInChildren<VlabTarget>();
                if(ball.Elapsed>0&&target.HasHit&&Vector3.Distance(ball.transform.position-Vector3.up*(ball.Diameter*.5f),target.Marker.position)<.001f)return "Native first-floor contact rests on the original receiving mat.";
            }
            if(id=="P01"&&new[]{"clamp","pendulum","string"}.Contains(key))return "Stock clamp socket and intentionally hanging pendulum/string; ValidateAssembly checks clearance.";
            if(Driver!=null&&Driver.Kit.Any(k=>k.key==key&&!k.onSurface))return "Delivered KitItemSpec explicitly marks this attached/suspended item onSurface=false.";
            var exceptions=new Dictionary<string,string>{
                {"P03","block,meter,cord"},{"P04","cart,flag,gateA,gateB"},{"P05","clamp,spring,load"},{"P06","cart,cartB,bumperA,bumperB,latch"},{"P07","cart,flag,gateA,gateB,cord,dropCord"},{"P08","track,cart,flag,gateA,gateB"},
                {"M01","gears,motor"},{"M02","lever,left,right"},{"M03","fixedA,fixedB,movingA,movingB,load,meter,cord,supportA,supportB,slingA,slingB"},{"M04","pulleys,motor"},{"M05","crank,rod80,rod100,rod120,slider"},{"M06","cam,follower,roller"},{"M08","beam,load"},
                {"C01","clamp,burette,bulb"},{"C02","zn,cu,bridge,leadA,leadB"},{"C03","cathode,anode,leadA,leadB"},{"C04","solidNaCl,solidNitrate,solidWater"},{"C05","bulb"},{"C06","bulb"},{"C07","stopper,tube,chips"},{"C08","thermometer,stir"},
                {"B01","slide,coverslip,specimen"},{"B02","slide,coverslip,specimen"},{"B04","aliquot,stopper,tube"},{"B05","aliquot,stopper,tube"},{"B06","funnel,filter,strip"},{"B07","funnel,filter,layer,dna"},{"B08","slide,coverslip,specimen"}};
            return exceptions.TryGetValue(id,out var keys)&&keys.Split(',').Contains(key)?"Named native nested/mounted part; source driver owns its sockets/seat, not a free-standing worktop item.":"";
        }
        public bool ValidateInstalledSurfaceLayout(out string failure)
        {
            foreach(var item in items.Where(x=>x.installed&&x.gameObject.activeInHierarchy))
            {
                if(session.spec.id=="P02"&&item.assetId=="target"&&Driver is PhysicsMechanicsDriver projectile&&projectile.ReceivingGeometryValid)
                {if(!ValidateReceivingFloorTarget(out failure))return false;continue;}
                if(session.spec.id=="M06"&&(item.assetId=="motor"||item.assetId=="guide"))
                {if(!ValidateDerivedM06Supports(out failure))return false;continue;}
                if(((session.spec.id=="M06"||session.spec.id=="M08")&&item.assetId=="gauge")||(session.spec.id=="M08"&&item.assetId=="supports"))
                {if(!ValidateGaugeSupport(out failure))return false;continue;}
                if(new[]{"P04","P07","P08"}.Contains(session.spec.id)&&(item.assetId=="gateA"||item.assetId=="gateB"))
                {if(!ValidatePhotogateSupports(out failure))return false;continue;}
                if((session.spec.id=="M01"&&(item.assetId=="board"||item.assetId=="gears"||item.assetId=="motor"))||(session.spec.id=="M04"&&(item.assetId=="belt"||item.assetId=="pulleys"||item.assetId=="motor")))
                {if(!ValidateTransmissionSupport(out failure))return false;continue;}
                if(InstalledSupportException(item.assetId)!="")continue;
                if(!TryVisibleBounds(item,out var b))
                {
                    if(InvisibleNativeItemReason(item)!="")continue;
                    failure=session.spec.id+" installed "+item.assetId+" has no visible source geometry.";return false;
                }
                if(item.assetId=="stand"||item.assetId=="launcher")
                {
                    // Articulated barrels and stand arms may overhang; their original complete bases must be supported.
                    var foot=item.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.enabled&&r.name.EndsWith("_Base_VIS",StringComparison.Ordinal));if(foot)b=foot.bounds;
                }
                if(!storageSurfaces.Any(s=>ContainsRect(Footprint(s.bounds),Footprint(b),.003f)&&Mathf.Abs(b.min.y-s.bounds.max.y)<=.012f))
                {failure=session.spec.id+" installed "+item.assetId+" lacks real worktop support: "+b;return false;}
            }
            failure="";return true;
        }
        public void Select(EquipmentHandle handle)
        {
            if(handle&&handle.stowed)ArrangeLooseItems(handle);
            Selected=handle;UpdateTargets();Changed?.Invoke();
        }
        public void BeginGrab(EquipmentHandle item,Ray pointer)
        {
            if(session.Phase!=ExperimentPhase.Preparing&&session.Phase!=ExperimentPhase.Ready)return;
            Select(item);pullingBob=Driver==null&&item.installed&&item.assetId=="pendulum"&&session.Phase==ExperimentPhase.Ready;
            carrying=!item.installed;carryDistance=Mathf.Clamp(Vector3.Distance(pointer.origin,item.transform.position),.35f,2.5f);
        }
        public void UpdateGrab(ActionFrame frame)
        {
            if(!Selected)return;
            if(frame.lost){CancelGrab();return;}
            if(pullingBob)
            {
                var plane=new Plane(Vector3.forward,Pivot);if(plane.Raycast(frame.pointer,out float distance))
                {var point=frame.pointer.GetPoint(distance)-Pivot;double angle=Math.Max(2,Math.Min(60,Math.Atan2(Math.Max(.001,point.x),-point.y)*180/Math.PI));session.SetParameter("angle",angle,out _);RefreshConfiguration();}
                if(frame.released){pullingBob=false;Selected=null;session.Start(out _);Changed?.Invoke();}return;
            }
            if(carrying)
            {
                carryDistance=Mathf.Clamp(carryDistance+frame.adjustment*.05f,.35f,2.5f);Selected.transform.position=frame.pointer.GetPoint(carryDistance);
                if(frame.released){carrying=false;if(Vector3.Distance(Selected.transform.position,Selected.slot)<.22f)InstallSelected();Changed?.Invoke();}
            }
        }
        public void CancelGrab(){if(Selected&&!Selected.installed)Selected.transform.SetPositionAndRotation(Selected.trayPosition,Selected.trayRotation);Selected=null;carrying=pullingBob=false;UpdateTargets();Changed?.Invoke();}
        public bool InstallSelected()
        {
            if(!Selected||Selected.installed)return false;
            var item=Selected;
            if(Driver!=null)
            {
                var spec=Driver.Kit.Single(x=>x.key==item.assetId);
                if(!string.IsNullOrEmpty(spec.dependsOn)&&!items.Single(x=>x.assetId==spec.dependsOn).installed)return false;
                item.gameObject.SetActive(true);item.stowed=false;item.transform.SetPositionAndRotation(item.slot,item.rotation);item.installed=true;Driver.Installed(item.assetId);item.slot=item.transform.position;
                if(Ready){Driver.Configure();SynchronizeDerivedFixtures();}else ArrangeLooseItems();session.SetApparatusReady(Ready);Selected=null;Changed?.Invoke();return true;
            }
            if(item.assetId!="stand"&&!items.First(x=>x.assetId=="stand").installed)return false;
            item.gameObject.SetActive(true);item.stowed=false;item.transform.SetPositionAndRotation(item.slot,item.rotation);item.installed=true;
            if(item.assetId=="clamp")
            {
                var clamp=item.GetComponent<VlabClamp>();
                // The main source rod and the clamp's actual bored boss are both 10 mm fittings.
                // Seat the bore centre on the rod axis; never align the instrument's root to it.
                var bore=item.GetComponentsInChildren<Transform>().Single(x=>x.name.EndsWith("_StandRod_SOCKET",StringComparison.Ordinal));
                item.transform.position+=MainRodClampPoint()-bore.position;
                if(clamp)Pivot=clamp.Grip.position;
                PositionStockSupportAtEdge();
                foreach(var part in items)part.slot=Slot(part.assetId);
            }
            if(item.assetId=="stand")
            {
                var foot=item.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("_Base_VIS",StringComparison.Ordinal));
                item.transform.position+=Vector3.up*(stationTop.max.y+StorageLift-foot.bounds.min.y);
                item.slot=item.transform.position;foreach(var part in items)part.slot=Slot(part.assetId);
            }
            if(item.assetId=="ruler"||item.assetId=="timer")PlaceOnSurface(item.gameObject,item.slot);
            if(Ready)UpdatePendulum(0);else ArrangeLooseItems();session.SetApparatusReady(Ready);Selected=null;UpdateTargets();Changed?.Invoke();return true;
        }
        void CreateTargets()
        {
            var source=cord?cord.GetComponentInChildren<LineRenderer>():null;if(!source||!source.sharedMaterial)return;
            targetMaterial=new Material(source.sharedMaterial){name="P01 selected snap target"};
            if(targetMaterial.HasProperty("_BaseColor"))targetMaterial.SetColor("_BaseColor",new Color(.30f,.72f,1f));
            if(targetMaterial.HasProperty("_Color"))targetMaterial.color=new Color(.30f,.72f,1f);
            foreach(var item in items)
            {
                var go=new GameObject("Snap target · "+item.assetId);go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=targetMaterial;line.useWorldSpace=true;line.loop=true;line.positionCount=48;line.startWidth=line.endWidth=.003f;
                line.startColor=line.endColor=new Color(.30f,.72f,1f);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;targets.Add(item,line);
            }
        }
        void UpdateTargets()
        {
            var active=Selected&&!Selected.installed?Selected:items.FirstOrDefault(x=>!x.installed);
            foreach(var pair in targets)
            {
                var item=pair.Key;var line=pair.Value;line.enabled=item==active&&!item.installed;
                bool surface=item.assetId=="stand"||item.assetId=="ruler"||item.assetId=="timer";
                float radius=surface?.09f:.035f;var origin=item.slot+(surface?Vector3.up*.006f:Vector3.zero);
                for(int i=0;i<line.positionCount;i++){float a=2*Mathf.PI*i/line.positionCount;line.SetPosition(i,origin+Vector3.right*(Mathf.Cos(a)*radius)+(surface?Vector3.forward:Vector3.up)*(Mathf.Sin(a)*radius));}
            }
        }
        void OnDestroy(){if(targetMaterial)Destroy(targetMaterial);}
        public void ApplyPreparation(int operation)
        {
            if(Driver!=null)return;
            var clamp=items.Select(x=>x.GetComponent<VlabClamp>()).FirstOrDefault(x=>x);
            if(clamp)clamp.SetOpening(.002);
            if(bobController)bobController.TryConfigureMass(session.parameters["mass"]*1000);
            if(timer&&operation==3)timer.ResetTimer();
            UpdatePendulum(session.Phase==ExperimentPhase.Ready?session.parameters["angle"]*Math.PI/180:0);
        }
        public void RefreshConfiguration(){if(Driver!=null){Driver.Configure();SynchronizeDerivedFixtures();return;}if(Ready)UpdatePendulum(session.Phase==ExperimentPhase.Ready?session.parameters["angle"]*Math.PI/180:0);}
        void Update()
        {
            if(session==null||!Ready)return;
            if(Driver!=null)return;
            if(!ReferenceEquals(timedDynamics,session.Pendulum)){timedDynamics=session.Pendulum;timerCompleted=false;if(timer)timer.ResetTimer();}
            if(timer&&session.Pendulum!=null&&session.Pendulum.FirstCrossing>=0&&!timerCompleted)
            {
                if(!timer.Running)timer.TryStart(session.Pendulum.FirstCrossing);
                if(session.Pendulum.TargetCrossing>=0){timer.TryStop(session.Pendulum.TargetCrossing);timerCompleted=true;}
                else timer.TrySample(session.Pendulum.Time);
            }
            double angle=session.Pendulum!=null?session.Pendulum.Angle:session.Phase==ExperimentPhase.Ready?session.parameters["angle"]*Math.PI/180:0;
            if(Math.Abs(lastAngle-angle)>.000001||session.Phase==ExperimentPhase.Ready)UpdatePendulum(angle);
        }
        void UpdatePendulum(double angle)
        {
            var clamp=items.First(x=>x.assetId=="clamp").GetComponent<VlabClamp>();if(clamp)Pivot=clamp.Grip.position;
            lastAngle=angle;float length=(float)session.parameters["length"];
            if(bob){bob.position=Pivot+new Vector3((float)Math.Sin(angle)*length,-(float)Math.Cos(angle)*length,0);bob.rotation=Quaternion.AngleAxis((float)(angle*180/Math.PI),Vector3.forward)*items.First(x=>x.assetId=="pendulum").rotation;}
            if(cord&&bob)cord.TrySetEndpoints(Pivot,bobController&&bobController.Eye?bobController.Eye.position:bob.position);
        }
        public bool ValidateAssembly(out string failure)
        {
            failure="";if(!Ready){failure="Install every required piece before validating the assembly.";return false;}
            if(Driver!=null)return Driver.Validate(out failure);
            var support=items.First(x=>x.assetId=="stand");var stand=support.GetComponent<VlabStand>();
            var rods=support.GetComponentsInChildren<Transform>().Where(x=>x.name.EndsWith("_Rod_VIS",StringComparison.Ordinal)).ToArray();
            if(rods.Length!=1){failure="Support must contain one continuous rod.";return false;}
            var rod=rods[0];var renderer=rod.GetComponent<Renderer>();var collider=rod.GetComponent<MeshCollider>();
            if(!renderer||!collider||!collider.enabled||collider.sharedMesh!=rod.GetComponent<MeshFilter>().sharedMesh){failure="Support surface and collision mesh disagree.";return false;}
            var bounds=renderer.bounds;
            if(Mathf.Abs(bounds.min.y-support.transform.position.y-RodBottom)>.001f||Mathf.Abs(bounds.max.y-support.transform.position.y-SupportTop)>.001f){failure="Stand differs from the delivered 720 mm geometry.";return false;}
            var clampItem=items.First(x=>x.assetId=="clamp");var bore=clampItem.GetComponentsInChildren<Transform>().Single(x=>x.name.EndsWith("_StandRod_SOCKET",StringComparison.Ordinal));
            if(Vector3.Distance(bore.position,MainRodClampPoint())>.001f){failure="Clamp bore is detached from the original main rod axis.";return false;}
            var clampBounds=clampItem.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).ToArray();
            var carriage=support.GetComponentsInChildren<Renderer>().Where(r=>r.name.EndsWith("_Boss_VIS",StringComparison.Ordinal)||r.name.EndsWith("_ClampMount_VIS",StringComparison.Ordinal)).ToArray();
            if(carriage.Length==0||clampBounds.Length==0||carriage.Max(r=>r.bounds.max.y)+.01f>clampBounds.Min(b=>b.min.y))
            {failure="Unused native carriage is not clear of the directly fitted clamp.";return false;}
            if(!ValidateInstalledSurfaceLayout(out failure))return false;
            // Swing is in world XY. The support and its horizontal arm sit behind that plane.
            // Sample every prescribed length and angle, including both extremes, without moving the lesson.
            float rodZ=bounds.center.z;
            var lengthRange=session.spec.parameters.Single(x=>x.key=="length");
            var angleRange=session.spec.parameters.Single(x=>x.key=="angle");
            int lengthSamples=(int)Math.Ceiling((lengthRange.max-lengthRange.min)/.010);
            int angleSamples=(int)Math.Ceiling(angleRange.max);
            for(int sample=0;sample<=lengthSamples;sample++)
            for(int degrees=-angleSamples;degrees<=angleSamples;degrees++)
            {
                // Include equilibrium (0 degrees): every released swing passes through it.
                float length=(float)Math.Min(lengthRange.max,lengthRange.min+sample*.010),theta=Mathf.Clamp(degrees,-(float)angleRange.max,(float)angleRange.max)*Mathf.Deg2Rad;
                var position=Pivot+new Vector3(Mathf.Sin(theta)*length,-Mathf.Cos(theta)*length,0);
                if(position.y-.015f<.050f){failure="Bob approaches the room floor within the allowed range.";return false;}
                if(!hasStationTop||position.z+.015f>stationTop.min.z-.005f){failure="Bob swing does not clear the worktop edge.";return false;}
                if(Mathf.Abs(position.z-rodZ)<.025f){failure="Pendulum swing plane intersects the support rod.";return false;}
            }
            if(!bobController||!bobController.Eye||!cord||Vector3.Distance(cord.Top,Pivot)>.001f||Vector3.Distance(cord.Bottom,bobController.Eye.position)>.001f){failure="Cord endpoints are detached from suspension or bob eye.";return false;}
            if(Mathf.Abs(Vector3.Distance(Pivot,bob.position)-(float)session.parameters["length"])>.001f){failure="Selected centre-to-pivot length disagrees with the assembled pendulum.";return false;}
            return true;
        }
    }
}
