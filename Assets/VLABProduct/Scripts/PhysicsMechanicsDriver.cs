using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    // Apparatus authority for the delivered stock instruments. All distances are metres.
    // The controls are explicit virtual operations; no mesh implies a measured mass,
    // friction coefficient, spring stiffness, torque or contact force.
    public sealed class PhysicsMechanicsDriver : LessonDriver
    {
        readonly string id;
        public const string ReceivingFailure="The predicted shot leaves the supported room area; adjust speed or angle.";
        public bool ReceivingGeometryValid{get;private set;}
        public Vector3 PredictedLandingPoint{get;private set;}
        public string ReceivingGeometryDiagnostic{get;private set;}="";
        float receivingPlane;
        int prepared,sampleIndex,pivotMark,beltIndex=1;
        bool configured,begun,paused=true,zeroed,contacted,ratcheted,hit,encounter,finished,steady,unloaded,rollerSelected;
        double clock,pull,blockX,blockV,staticPeak,steadySum,steadyTime,firstCross=-1,lastCross=-1;
        int cycles; double springPrevious,preA,preB,postA,postB,collisionTime,camZero,minSlider=double.PositiveInfinity,maxSlider=double.NegativeInfinity;
        double zeroReading,contactReading,beamReading,beamZero,ropeLength,ropeReference=-1; Vector3 blockOrigin,hangerOrigin,springTop,launchBase;
        double releasePosition,beltInputTurns,beltOutputTurns;readonly List<Sample> gatePositionTrace=new List<Sample>();
        readonly GateCapture captureA=new GateCapture(),captureB=new GateCapture();
        Transform cartFrame,sliderFrame,beltSmall,beltLarge;Quaternion beltSmallRest,beltLargeRest;Vector3 beltSmallAxis,beltLargeAxis; VlabCart cart,cartB; VlabFlag flag; VlabPhotogate gateA,gateB;
        VlabMotor motor; VlabProjectile ball; VlabLauncher launcher; VlabTarget target;
        readonly List<Sample> samples=new List<Sample>();
        [Serializable] public sealed class Sample { public double t,a,b,c; public Sample(double t,double a,double b=0,double c=0){this.t=t;this.a=a;this.b=b;this.c=c;} }
        [Serializable] sealed class GateCapture { public bool complete;public double entered,exited,midpointPosition,flagWidth,blockingSeconds,meanSpeed;public void Clear(){complete=false;entered=exited=midpointPosition=flagWidth=blockingSeconds=meanSpeed=0;} }
        [Serializable] sealed class Evidence { public string lesson,kind,assumptions,sampleId,beltConfiguration,followerType; public int pivot,cycles,sample,belt,gearDriverTeeth,gearDrivenTeeth; public bool contact,ratchet,unloaded,driveLarge;public Sample[] series;public GateCapture gateA,gateB; }
        [Serializable] sealed class Draft { public int sample,pivot,belt;public bool roller; }
        public PhysicsMechanicsDriver(ExperimentSession value):base(value){id=value.spec.id;if(id=="P01")throw new ArgumentException("P01 uses the accepted pendulum rig.");}
        KitItemSpec K(string key,string prefab,string vi,string en,float x,float z,float y=0,bool surface=true,string depends="")=>new KitItemSpec(key,prefab,vi,en,new Vector3(x,y,z),default,surface,depends);
        public override KitItemSpec[] Kit
        {
            get
            {
                switch(id)
                {
                    case "P02":return new[]{K("launcher","launcher","Bộ phóng","Launcher",-.55f,0),K("ball","ball","Viên bi","Projectile",-.65f,-.2f),K("target","target","Tấm hứng","Target",.3f,0),K("ruler","ruler","Thước","Ruler",.1f,-.3f)};
                    case "P03":return new[]{K("surface","surface_tiles","Mặt tiếp xúc","Surface tile",0,0),K("block","friction_block","Khối trượt","Friction block",0,0),K("meter","force_meter_10N","Lực kế 10 N","10 N force meter",.3f,0),K("cord","string","Dây kéo ngang","Horizontal pull cord",.15f,0)};
                    case "P04":return TrackKit(true,false,false);
                    case "P05":return new[]{K("stand","stand","Giá đỡ","Stand",-.15f,.1f),K("clamp","clamp","Kẹp","Clamp",-.15f,.1f),K("spring","spring","Lò xo","Spring",.05f,0),K("load","masses","Tải treo","Hanging load",.05f,0),K("ruler","ruler","Thước","Ruler",.35f,0),K("timer","timer","Đồng hồ","Timer",.3f,-.2f)};
                    case "P06":return TrackKit(false,true,false);
                    case "P07":return TrackKit(true,false,true);
                    case "P08":return TrackKit(true,false,false).Concat(new[]{K("incline","incline_base","Giá nghiêng","Incline base",-.6f,0),K("ruler","ruler","Thước cao","Height ruler",.4f,-.2f)}).ToArray();
                    case "M01":return new[]{K("board","pegboard","Bảng lắp cơ cấu","Mechanism board",0,.12f),K("gears","gear_set","Bộ 12–24–36 răng","12–24–36 gear train",0,0),K("motor","motor","Motor","Motor",-.15f,0),K("tach","tachometer","Tốc kế","Tachometer",.35f,-.2f)};
                    case "M02":return new[]{K("fulcrum","fulcrum","Điểm tựa","Fulcrum",0,0),K("lever","lever","Thanh đòn","Lever",0,0),K("left","masses","Tải trái","Left load",-.3f,0),K("right","masses","Tải phải","Right load",.3f,0)};
                    case "M03":return new[]{K("stand","stand","Giá đỡ","Stand",-.3f,.1f),K("fixedA","pulley","Ròng rọc cố định A","Fixed pulley A",-.1f,0,.65f,false),K("fixedB","pulley","Ròng rọc cố định B","Fixed pulley B",.04f,0,.65f,false),K("movingA","pulley","Ròng rọc động A","Moving pulley A",-.1f,0,.3f,false),K("movingB","pulley","Ròng rọc động B","Moving pulley B",.04f,0,.3f,false),K("load","masses","Tổng tải nâng","Total lifted load",-.03f,0,.2f,false),K("meter","force_meter_10N","Lực kế 10 N","10 N force meter",.22f,0,.4f,false),K("cord","string","Dây luồn","Routed cord",0,0,.7f,false),K("supportA","string","Dây treo cố định A","Fixed A suspension",0,0,.7f,false),K("supportB","string","Dây treo cố định B","Fixed B suspension",0,0,.7f,false),K("slingA","string","Dây móc tải A","Load sling A",0,0,.3f,false),K("slingB","string","Dây móc tải B","Load sling B",0,0,.3f,false)};
                    case "M04":return new[]{K("belt","belt_lesson","Bộ đai 40/80 mm","40/80 mm belt",0,0),K("pulleys","belt_pulley","Hai pulley đai","Belt pulleys",0,0),K("motor","motor","Motor","Motor",-.2f,0),K("tach","tachometer","Tốc kế","Tachometer",.3f,-.2f)};
                    case "M05":return new[]{K("motor","motor","Motor","Motor",-.15f,0),K("crank","crank","Tay quay","Crank",0,0),K("rod80","connecting_rod_80mm","Thanh truyền 80 mm","80 mm rod",0,0),K("rod100","connecting_rod","Thanh truyền 100 mm","100 mm rod",0,0),K("rod120","connecting_rod_120mm","Thanh truyền 120 mm","120 mm rod",0,0),K("rail","slider_rail","Ray con trượt","Slider guide",.1f,0),K("slider","slider","Con trượt","Slider",.1f,0)};
                    case "M06":return new[]{K("motor","motor","Motor","Motor",-.15f,0),K("cam","cam","Cam 10 mm","10 mm cam",0,0),K("follower","follower","Con đội dao","Knife follower",0,0),K("roller","follower_roller","Con đội lăn","Roller follower",.2f,0),K("guide","guide","Ống dẫn hướng","Follower guide",0,0),K("gauge","dial_gauge","Đồng hồ so","Dial gauge",.2f,0)};
                    case "M07":return new[]{K("caliper","caliper","Thước kẹp","Caliper",-.2f,0),K("micrometer","micrometer","Panme","Micrometer",.2f,0),K("samples","measurement_samples","Bộ mẫu có mã","Identified samples",0,-.2f)};
                    case "M08":return new[]{K("supports","support_pair","Hai gối tựa","Support pair",-.15f,0),K("beam","beam","Dầm polymer","Polymer beam",-.15f,0),K("gauge","dial_gauge","Đồng hồ so","Dial gauge",0,-.15f),K("load","masses","Tải giữa nhịp","Midspan load",0,0)};
                    default:throw new ArgumentException("No apparatus driver for "+id);
                }
            }
        }
        KitItemSpec[] TrackKit(bool gates,bool collision,bool hanging)
        {
            var a=new List<KitItemSpec>{K("track","track","Ray 1,2 m","1.2 m track",-.6f,0),K("cart","cart","Xe A","Cart A",0,0)};
            if(gates)a.AddRange(new[]{K("flag","flag","Bản chắn 50 mm","50 mm flag",0,0),K("gateA","photogate","Cổng A","Gate A",-.25f,0),K("gateB","photogate","Cổng B","Gate B",.25f,0),K("timer","timer","Đồng hồ cổng","Gate timer",.45f,-.2f)});
            if(collision)a.AddRange(new[]{K("cartB","cart_B","Xe B","Cart B",.25f,0),K("bumperA","elastic_bumper","Đầu đàn hồi A","Elastic attachment A",0,0),K("bumperB","elastic_bumper","Đầu đàn hồi B","Elastic attachment B",0,0),K("latch","latch","Khớp dính","Sticking attachment",0,0)});
            if(hanging)a.AddRange(new[]{K("pulley","pulley","Ròng rọc cuối ray","End pulley",.55f,0,.2f,false),K("load","masses","Tải treo","Hanging load",.6f,0,.2f,false),K("cord","string","Dây ngang","Horizontal cord",.2f,0),K("dropCord","string","Dây đứng","Vertical cord",.6f,0)});
            return a.ToArray();
        }
        T Maybe<T>(string key)where T:Component=>equipment!=null&&equipment.TryGetValue(key,out var e)?e.GetComponentInChildren<T>(true):null;
        bool AllInstalled=>equipment!=null&&Kit.All(k=>equipment.ContainsKey(k.key)&&equipment[k.key].installed);
        Transform S(string key,string suffix)=>Socket(key,suffix);
        void Move(string key,Vector3 position){equipment[key].transform.position=position;}
        void At(string key,string socket,Vector3 point){equipment[key].transform.position+=point-S(key,socket).position;}
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static bool Near(double a,double b,double tolerance=1e-6)=>Math.Abs(a-b)<tolerance;
        public override void Bind(IReadOnlyDictionary<string,EquipmentHandle> items,Vector3 at){base.Bind(items,at);}
        public override void Installed(string key){if(AllInstalled)Configure();}
        public override void Configure()
        {
            if(!AllInstalled||begun)return;int replay=session.Operation;
            configured=false;zeroed=contacted=ratcheted=hit=encounter=finished=steady=unloaded=false;prepared=0;
            samples.Clear();gatePositionTrace.Clear();captureA.Clear();captureB.Clear();clock=pull=blockX=blockV=staticPeak=steadySum=steadyTime=0;cycles=0;firstCross=lastCross=-1;
            if(id.StartsWith("P")&&(id=="P04"||id=="P06"||id=="P07"||id=="P08"))ConfigureTrack();
            switch(id)
            {
                case "P02":
                    launcher=Instrument<VlabLauncher>("launcher");ball=Instrument<VlabProjectile>("ball");target=Instrument<VlabTarget>("target");launchBase=launcher.transform.position;
                    launcher.ResetLauncher();ball.ResetFlight();ball.transform.position=equipment["ball"].slot;ball.IsPaused=true;launcher.SetAngle((float)P("angle"));target.Clear();ball.FirstContact-=OnImpact;ball.FirstContact+=OnImpact;
                    ConfigureReceivingFloor();break;
                case "P03":
                    Instrument<VlabSurfaceTile>("surface").TrySetFinish(0);Instrument<VlabFrictionBlock>("block").SetSurface("wood");
                    var surface=equipment["surface"].GetComponentsInChildren<Renderer>().First(x=>x.enabled).bounds;var body=equipment["block"].GetComponentsInChildren<Renderer>().First(x=>x.enabled).bounds;
                    equipment["block"].transform.position+=Vector3.up*(surface.max.y-body.min.y);blockOrigin=equipment["block"].transform.position;OrientMeter(-Vector3.right);Instrument<VlabForceMeter>("meter").SetForce(0);AlignFrictionCord();break;
                case "P05":
                    var stand=Instrument<VlabStand>("stand");Check(stand.TrySetHeight(.65f),"Stand carriage is unavailable.");Fit("clamp","_StandRod_SOCKET",stand.Mount);springTop=Instrument<VlabClamp>("clamp").Grip.position;SetSpring(.15);break;
                case "M01":ConfigureGear();break;
                case "M02":ConfigureLever();break;
                case "M03":ConfigurePulleys();break;
                case "M04":ConfigureBelt();break;
                case "M05":ConfigureCrank();break;
                case "M06":ConfigureCam();break;
                case "M07":Instrument<VlabCaliper>("caliper").Locked=false;Instrument<VlabCaliper>("caliper").SetOpening(0);Instrument<VlabMicrometer>("micrometer").TrySetOpening(0);Instrument<VlabMicrometer>("micrometer").TrySetRatchet(false);break;
                case "M08":ConfigureBeam(0);beamZero=0;break;
            }
            configured=true;paused=true;
            if(id=="P02"&&!ReceivingGeometryValid)return; // invalid edits are rejected by the authority, never thrown out of the UI
            for(int operation=1;operation<=Math.Min(replay,session.spec.operationsEn.Length);operation++)Check(Prepare(operation,out var reason),reason);
        }
        void ConfigureReceivingFloor()
        {
            ReceivingGeometryValid=false;ReceivingGeometryDiagnostic="No actual receiving floor is identified.";var rig=equipment["launcher"].GetComponentInParent<ApparatusRig>();
            equipment["target"].transform.SetPositionAndRotation(equipment["target"].slot,equipment["target"].rotation);
            if(!rig||!rig.TryReceivingFloor(out var floor))return;
            // Aim the unchanged seated launcher across the real room, toward its farthest safe corner.
            var corners=new[]{new Vector3(floor.min.x+.28f,0,floor.min.z+.18f),new Vector3(floor.max.x-.28f,0,floor.min.z+.18f),new Vector3(floor.min.x+.28f,0,floor.max.z-.18f),new Vector3(floor.max.x-.28f,0,floor.max.z-.18f)};
            for(int settle=0;settle<3;settle++)
            {
                rig.SeatLauncherAtLeftEdge(equipment["launcher"]);
                var corner=corners.OrderByDescending(p=>Vector3.ProjectOnPlane(p-launcher.transform.position,Vector3.up).sqrMagnitude).First();
                var heading=Vector3.ProjectOnPlane(corner-launcher.transform.position,Vector3.up).normalized;
                var forward=Vector3.ProjectOnPlane(launcher.MuzzleRay.direction,Vector3.up).normalized;
                launcher.transform.rotation=Quaternion.FromToRotation(forward,heading)*launcher.transform.rotation;launcher.SetAngle((float)P("angle"));
            }
            rig.SeatLauncherAtLeftEdge(equipment["launcher"]);
            var mat=equipment["target"];ApparatusRig.TryVisibleBounds(mat,out var mb);
            mat.transform.position+=Vector3.up*(floor.max.y+.002f-mb.min.y);receivingPlane=S("target","_Origin_SOCKET").position.y;
            var ray=launcher.MuzzleRay;float radius=ball.Diameter*.5f;var start=ray.origin+ray.direction*(radius+.0001f);
            session.parameters["height"]=Math.Max(0,ray.origin.y-receivingPlane);
            double vy=P("speed")*ray.direction.y,h=Math.Max(0,start.y-receivingPlane-radius),flight=(vy+Math.Sqrt(vy*vy+2*ScienceModels.G*h))/ScienceModels.G;
            var velocity=ray.direction*(float)P("speed");PredictedLandingPoint=start+velocity*(float)flight-Vector3.up*(float)(.5*ScienceModels.G*flight*flight);PredictedLandingPoint=new Vector3(PredictedLandingPoint.x,receivingPlane,PredictedLandingPoint.z);
            var tx=(S("target","_XEnd_SOCKET").position-S("target","_Origin_SOCKET").position).normalized;var tz=(S("target","_YEnd_SOCKET").position-S("target","_Origin_SOCKET").position).normalized;
            At("target","_Origin_SOCKET",PredictedLandingPoint-tx*.25f-tz*.15f);
            ReceivingGeometryValid=rig.ValidateReceivingFloorTarget(out var matFailure);
            ReceivingGeometryDiagnostic=ReceivingGeometryValid?"Supported floor trajectory.":matFailure;
            float ceiling=float.PositiveInfinity;
            var ceilings=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.GetComponentInParent<EquipmentHandle>()&&r.bounds.size.x>2&&r.bounds.size.z>2&&r.bounds.min.y>2&&r.name.IndexOf("ceil",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            if(ceilings.Length>0)ceiling=ceilings.Min(r=>r.bounds.min.y);
            double apex=start.y+Math.Max(0,vy)*Math.Max(0,vy)/(2*ScienceModels.G);
            if(ReceivingGeometryValid&&apex+radius>=ceiling-.002f)
            {ReceivingGeometryValid=false;ReceivingGeometryDiagnostic="Trajectory apex "+(apex+radius).ToString("F4")+" m reaches the actual ceiling underside "+ceiling.ToString("F4")+" m.";}
            var tops=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.GetComponentInParent<EquipmentHandle>()&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&r.bounds.max.y>.7f&&r.bounds.max.y<1.15f&&(r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            for(int n=0;ReceivingGeometryValid&&n<=128;n++)
            {
                double t=flight*n/128;var point=start+velocity*(float)t-Vector3.up*(float)(.5*ScienceModels.G*t*t);
                if(point.x-radius<floor.min.x||point.x+radius>floor.max.x||point.z-radius<floor.min.z||point.z+radius>floor.max.z)
                {ReceivingGeometryValid=false;ReceivingGeometryDiagnostic="Trajectory sphere at "+point.ToString("F4")+" leaves actual floor/wall envelope "+floor+".";}
                var struck=tops.FirstOrDefault(renderer=>{var top=renderer.bounds;return point.x>top.min.x-radius&&point.x<top.max.x+radius&&point.z>top.min.z-radius&&point.z<top.max.z+radius&&point.y-radius<top.max.y&&point.y+radius>top.min.y;});
                if(struck){ReceivingGeometryValid=false;ReceivingGeometryDiagnostic="Trajectory sphere at "+point.ToString("F4")+" intersects actual worktop "+struck.name+" "+struck.bounds+".";}
            }
            // Unsupported settings keep their declared numerical parameters, but the mat rests safely
            // in its original supported storage slot and the authority refuses a physical-room trial.
            if(!ReceivingGeometryValid)mat.transform.SetPositionAndRotation(mat.slot,mat.rotation);
        }
        void ConfigureTrack()
        {
            var track=Instrument<VlabTrack>("track");
            if(id=="P08"){var incline=Instrument<VlabInclineBase>("incline");Check(incline.SetAngle((float)Math.Min(30,P("angle"))),"Incline must be within 0–30 degrees.");Check(track.TryMountIncline(incline),"Track did not seat on incline.");}
            if(!cartFrame){var f=new GameObject("Lesson cart datum frame");f.transform.SetParent(equipment["track"].transform.parent,false);cartFrame=f.transform;}
            cartFrame.SetPositionAndRotation(track.CartFrame.position,track.CartFrame.rotation);
            cart=Instrument<VlabCart>("cart");cart.BindTravelFrame(cartFrame);cart.ResetTrial(id=="P06"?P("m1"):id=="P07"?P("cart"):id=="P08"?P("mass"):.2,0,0);
            cartFrame.position+=S("track","_TravelCenter_SOCKET").position-S("cart","_RailDatum_SOCKET").position;cart.ApplyPose();
            if(id=="P06")
            {
                cartB=Instrument<VlabCart>("cartB");cartB.BindTravelFrame(cartFrame);cart.ResetTrial(P("m1"),-.25,P("u1"));cartB.ResetTrial(P("m2"),.25,P("u2"));
                Instrument<VlabLatch>("latch").Release();equipment["latch"].gameObject.SetActive(P("elastic")<.5);equipment["bumperA"].gameObject.SetActive(P("elastic")>.5);equipment["bumperB"].gameObject.SetActive(P("elastic")>.5);
                MountBumper("bumperA","cart","_BumperFront_SOCKET",false);MountBumper("bumperB","cartB","_BumperBack_SOCKET",true);return;
            }
            flag=Instrument<VlabFlag>("flag");flag.transform.rotation=Quaternion.FromToRotation(flag.transform.right,cartFrame.right)*flag.transform.rotation;At("flag","_MountFoot_SOCKET",S("cart","_Flag_SOCKET").position);flag.transform.SetParent(cart.transform,true);
            gateA=Instrument<VlabPhotogate>("gateA");gateB=Instrument<VlabPhotogate>("gateB");MountGate("gateA",-.25);MountGate("gateB",.25);
            if(id=="P04")cart.ResetTrial(.2,-.36,Math.Sqrt(Math.Max(.0001,P("speed")*P("speed")-2*P("accel")*.11)));
            if(id=="P07")
            {
                cart.ResetTrial(P("cart"),-.35,0);Move("pulley",cartFrame.TransformPoint(new Vector3(.52f,0,0)));At("pulley","_CordLeft_SOCKET",S("cart","_Tow_SOCKET").position+cartFrame.right*.75f);
                var right=S("pulley","_CordRight_SOCKET").position;At("load","_HangPoint_SOCKET",right-Vector3.up*.09f);hangerOrigin=equipment["load"].transform.position;Instrument<VlabMassSet>("load").TrySetLoad(0,P("hanger")*1000);RouteNewton(0);
                // Both blocking cycles complete before the hanging mass bottom stop.
                MountGate("gateA",-.27);MountGate("gateB",-.35+Math.Min(.65,P("travel")*.7));
            }
            if(id=="P08"){double travel=P("height")/Math.Sin(P("angle")*Math.PI/180);cart.ResetTrial(P("mass"),Math.Min(.35,-.35+travel),0);releasePosition=cart.PositionM;MountGate("gateA",releasePosition-.04);MountGate("gateB",-.31);}
            gateA.ResetMeasurement();gateB.ResetMeasurement();
            equipment["track"].GetComponentInParent<ApparatusRig>().RefreshPhotogateFixtures();
        }
        void MountBumper(string key,string cartKey,string suffix,bool reverse)
        {
            var e=equipment[key];e.transform.SetParent(equipment[cartKey].transform,true);if(reverse)e.transform.rotation=Quaternion.AngleAxis(180,cartFrame.up)*e.transform.rotation;
            var pin=e.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.EndsWith("_Mount_SOCKET",StringComparison.Ordinal)||t.name.EndsWith("_Pin_SOCKET",StringComparison.Ordinal));
            if(pin)e.transform.position+=S(cartKey,suffix).position-pin.position;else e.transform.position=S(cartKey,suffix).position;
        }
        Vector3 FlagCentre=>(S("flag","_LeadingEdge_SOCKET").position+S("flag","_TrailingEdge_SOCKET").position)*.5f;
        void MountGate(string key,double x)
        {
            var gate=Instrument<VlabPhotogate>(key);var nativeUp=(S(key,"_BeamUp_SOCKET").position-S(key,"_BeamStart_SOCKET").position).normalized;
            var sourceFrame=Quaternion.LookRotation(Vector3.Cross(gate.TravelAxis,nativeUp),nativeUp);
            gate.transform.rotation=Quaternion.LookRotation(Vector3.Cross(cartFrame.right,cartFrame.up),cartFrame.up)*Quaternion.Inverse(sourceFrame)*gate.transform.rotation;
            // The delivered track explicitly replaces these feet with its real 117 mm gate seats.
            foreach(var t in equipment[key].GetComponentsInChildren<Transform>(true))if(t.name.EndsWith("_Foot_VIS",StringComparison.Ordinal)||t.name=="Foot collider")t.gameObject.SetActive(false);
            var beam=(S(key,"_BeamStart_SOCKET").position+S(key,"_BeamEnd_SOCKET").position)*.5f;
            var desired=FlagCentre+cartFrame.right*(float)(x-cart.PositionM);gate.transform.position+=desired-beam;gate.ResetMeasurement();
        }
        void AlignFrictionCord()
        {
            var block=Instrument<VlabFrictionBlock>("block");var meter=Instrument<VlabForceMeter>("meter");equipment["meter"].transform.position+=block.PullPoint+Vector3.right*.12f-meter.LoadPoint;
            Instrument<VlabString>("cord").TrySetEndpoints(block.PullPoint,meter.LoadPoint);
        }
        void OrientMeter(Vector3 towardLoad)
        {
            var meter=Instrument<VlabForceMeter>("meter");meter.SetForce(0);var zero=meter.LoadPoint;meter.SetForce(1);var axis=(meter.LoadPoint-zero).normalized;meter.SetForce(0);meter.transform.rotation=Quaternion.FromToRotation(axis,towardLoad)*meter.transform.rotation;
        }
        void SetSpring(double span)
        {
            Check(Instrument<VlabSpring>("spring").TrySetEndpoints(springTop,springTop-Vector3.up*(float)span),"Spring endpoint span must remain 100–200 mm.");
            At("load","_HangPoint_SOCKET",Instrument<VlabSpring>("spring").Bottom);Instrument<VlabMassSet>("load").TrySetLoad(0,P("mass")*1000);
        }
        void ConfigureGear()
        {
            motor=Instrument<VlabMotor>("motor");motor.Stop();motor.TrySetRpm(P("rpm"));motor.TrySetAngle(0);Instrument<VlabGearSet>("gears").SetDriverAngle(0);
            Check(Instrument<VlabPegboard>("board").TryGetSocket(8,7,.080f,out var mounting),"Board socket atlas is invalid.");
            var gears=equipment["gears"].transform;gears.rotation=Quaternion.FromToRotation(gears.up,-(mounting.rotation*Vector3.forward))*gears.rotation;At("gears","_Axis12_SOCKET",mounting.position);
            var gearAxis=S("gears","_Axis"+(int)P("driver")+"_SOCKET");AlignMotor(gearAxis.position,(S("gears","_Axis24_SOCKET").position-gearAxis.position),false);
            motor.transform.position-=motor.Axis*.006f;
            // The board exposes logical sockets; the stock train has its own accepted centres.
            Check(Instrument<VlabPegboard>("board").TryGetSocket(8,7,.080f,out _),"Board socket atlas is invalid.");
            equipment["motor"].GetComponentInParent<ApparatusRig>().RefreshTransmissionFixtures();
        }
        void AlignMotor(Vector3 axisPoint,Vector3 planeReference,bool deriveAxis=true)
        {
            if(!motor)return;Vector3 axis=deriveAxis?planeReference.normalized:-equipment["gears"].transform.up;
            motor.transform.rotation=Quaternion.FromToRotation(motor.Axis,axis)*motor.transform.rotation;motor.transform.position+=axisPoint-motor.Tip.position;
        }
        void ConfigureLever()
        {
            Check(Instrument<VlabLever>("lever").TryMount(Instrument<VlabFulcrum>("fulcrum"),pivotMark,0),"The lever pivot must use one of its three actual bores.");
            int left=pivotMark-(int)Math.Round(P("arm1")/.1),right=pivotMark+(int)Math.Round(P("arm2")/.1);
            PlaceLeverLoad("left",left,P("force1"));PlaceLeverLoad("right",right,P("force2"));
        }
        void PlaceLeverLoad(string key,int mark,double force)
        {
            var socket=equipment["lever"].GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Contains("Load")&&t.name.Contains(mark.ToString())&&t.name.EndsWith("_SOCKET",StringComparison.Ordinal));
            if(socket)At(key,"_HangPoint_SOCKET",socket.position);
            else {var lever=Instrument<VlabLever>("lever");var pivot=Instrument<VlabFulcrum>("fulcrum").PivotPoint;At(key,"_HangPoint_SOCKET",pivot+Vector3.right*((mark-pivotMark)*.1f));}
            Instrument<VlabMassSet>(key).TrySetLoad(0,force/ScienceModels.G*1000);
        }
        void ConfigurePulleys()
        {
            var stand=Instrument<VlabStand>("stand");stand.TrySetHeight(.65f);var top=stand.Mount.position;
            Move("fixedA",top+Vector3.right*.13f-Vector3.up*.08f);Move("fixedB",equipment["fixedA"].transform.position+Vector3.right*.074f);
            Instrument<VlabString>("supportA").TrySetEndpoints(top,S("fixedA","_TopHangPoint_SOCKET").position);Instrument<VlabString>("supportB").TrySetEndpoints(top,S("fixedB","_TopHangPoint_SOCKET").position);
            Instrument<VlabMassSet>("load").TrySetLoad(0,P("load")/ScienceModels.G*1000);hangerOrigin=equipment["load"].transform.position;OrientMeter(Vector3.up);ropeReference=-1;RoutePulley(0);
        }
        void RoutePulley(double lift)
        {
            int n=(int)P("strands");var fixedA=equipment["fixedA"].transform;var fixedB=equipment["fixedB"].transform;
            var movingA=equipment["movingA"].transform;var movingB=equipment["movingB"].transform;
            movingA.position=fixedA.position-Vector3.right*.037f-Vector3.up*(.3f-(float)lift);movingB.position=fixedA.position+Vector3.right*.037f-Vector3.up*(.3f-(float)lift);
            equipment["movingA"].gameObject.SetActive(n>=2);equipment["fixedB"].gameObject.SetActive(n==4);equipment["movingB"].gameObject.SetActive(n==4);
            equipment["supportB"].gameObject.SetActive(n==4);equipment["slingA"].gameObject.SetActive(n>=2);equipment["slingB"].gameObject.SetActive(n==4);
            var a=S("fixedA","_CordLeft_SOCKET").position;var b=S("fixedA","_CordRight_SOCKET").position;
            var points=new List<Vector3>();
            if(n==1){At("load","_HangPoint_SOCKET",a-Vector3.up*(.3f-(float)lift));points.Add(S("load","_HangPoint_SOCKET").position);points.Add(a);Arc(points,Instrument<VlabPulley>("fixedA"),true);points.Add(b-Vector3.up*(.13f+(float)lift));}
            else
            {
                var movingLeft=S("movingA","_CordLeft_SOCKET").position;points.Add(new Vector3(movingLeft.x,a.y,movingLeft.z));points.Add(movingLeft);Arc(points,Instrument<VlabPulley>("movingA"),false);points.Add(a);Arc(points,Instrument<VlabPulley>("fixedA"),true);
                if(n==4){points.Add(S("movingB","_CordLeft_SOCKET").position);Arc(points,Instrument<VlabPulley>("movingB"),false);points.Add(S("fixedB","_CordLeft_SOCKET").position);Arc(points,Instrument<VlabPulley>("fixedB"),true);b=S("fixedB","_CordRight_SOCKET").position;}
                points.Add(b-Vector3.up*(.13f+(float)(n*lift)));
                var hookA=S("movingA","_BottomHangPoint_SOCKET").position;var hookB=n==4?S("movingB","_BottomHangPoint_SOCKET").position:hookA;
                At("load","_HangPoint_SOCKET",(hookA+hookB)*.5f-Vector3.up*.06f);var loadPoint=S("load","_HangPoint_SOCKET").position;
                Instrument<VlabString>("slingA").TrySetEndpoints(hookA,loadPoint);if(n==4)Instrument<VlabString>("slingB").TrySetEndpoints(hookB,loadPoint);
            }
            ropeLength=0;for(int i=1;i<points.Count;i++)ropeLength+=Vector3.Distance(points[i-1],points[i]);if(ropeReference<0)ropeReference=ropeLength;Check(Math.Abs(ropeLength-ropeReference)<.0001,"The selected rope route does not conserve its measured polyline length.");
            var line=Instrument<VlabString>("cord").GetComponentInChildren<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Count;line.SetPositions(points.ToArray());
            var end=points[points.Count-1];var meter=Instrument<VlabForceMeter>("meter");equipment["meter"].transform.position+=end-meter.LoadPoint;meter.SetForce((float)(P("load")/(n*P("efficiency")/100)));
            foreach(string key in new[]{"fixedA","fixedB","movingA","movingB"}){var pulley=Maybe<VlabPulley>(key);if(pulley&&pulley.gameObject.activeInHierarchy)pulley.TrySetTurns(n*lift/(2*Math.PI*.0185));}
        }
        static void Arc(List<Vector3> points,VlabPulley pulley,bool upper)
        {
            Vector3 centre=pulley.Sheave.position,axis=pulley.Axis,right=Vector3.Cross(Vector3.up,axis).normalized;
            for(int i=0;i<=16;i++){float a=Mathf.PI*i/16;points.Add(centre+right*(-Mathf.Cos(a)*.0185f)+Vector3.up*(Mathf.Sin(a)*.0185f*(upper?1:-1)));}
        }
        void ConfigureBelt()
        {
            var belt=Instrument<VlabBelt>("belt");belt.Running=false;int index=P("crossed")>.5?Array.FindIndex(belt.Configurations,c=>c.crossed):Math.Max(0,Math.Min(2,beltIndex));
            if(!belt.Configurations[index].crossed&&P("crossed")>.5)throw new InvalidOperationException("Crossed stock belt is missing.");belt.SelectConfiguration(index);belt.ConfigureDrive(P("rpm"),P("slip")/100,Near(P("d1"),80));beltInputTurns=beltOutputTurns=0;motor=Instrument<VlabMotor>("motor");motor.Stop();motor.TrySetRpm(P("rpm"));motor.TrySetAngle(0);
            FitBeltPulleys();
        }
        void FitBeltPulleys()
        {
            var belt=Instrument<VlabBelt>("belt");var axes=equipment["pulleys"].GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("_SOCKET",StringComparison.Ordinal)&&t.name.Contains("Axis")).ToArray();
            var beltAxes=equipment["belt"].GetComponentsInChildren<Transform>(true).Where(t=>t.gameObject.activeInHierarchy&&t.name.EndsWith("_SOCKET",StringComparison.Ordinal)&&t.name.Contains("Pulley")).ToArray();
            if(axes.Length>=2&&beltAxes.Length>=2)equipment["pulleys"].transform.position+=beltAxes[0].position-axes[0].position;
            // Stock pulley SDK owns its exact pulley pose/angles when available; no scaled pitch diameters.
            var sockets=belt.Current.root.GetComponentsInChildren<Transform>(true);var a=sockets.Single(t=>t.name.EndsWith("_PulleyA_SOCKET",StringComparison.Ordinal));var b=sockets.Single(t=>t.name.EndsWith("_PulleyB_SOCKET",StringComparison.Ordinal));
            beltSmall=S("pulleys","_Pulley40_VIS");beltLarge=S("pulleys","_Pulley80_VIS");
            // Stock prefab contains rigid pulley meshes. Socket-pair axes reproduce the delivered VlabBeltPulley driver without an unbound scene reference.
            beltSmallAxis=beltSmall.InverseTransformDirection((S("pulleys","_40_Back_SOCKET").position-S("pulleys","_40_Front_SOCKET").position).normalized);beltLargeAxis=beltLarge.InverseTransformDirection((S("pulleys","_80_Back_SOCKET").position-S("pulleys","_80_Front_SOCKET").position).normalized);
            S("pulleys","_Bore_VIS").SetParent(beltSmall,true);S("pulleys","_Bore80_VIS").SetParent(beltLarge,true);
            beltSmallRest=beltSmall.localRotation;beltLargeRest=beltLarge.localRotation;beltSmall.position=a.position;beltLarge.position=b.position;
            Check(belt.Fits(belt.Current.centerDistance,.04,.08,belt.Current.length),"Stock belt pitch fit is invalid.");
            string input=Near(P("d1"),80)?"80":"40";AlignMotor(input=="80"?b.position:a.position,(S("pulleys","_"+input+"_Front_SOCKET").position-S("pulleys","_"+input+"_Back_SOCKET").position).normalized);
            motor.transform.position-=motor.Axis*.006f;
            equipment["motor"].GetComponentInParent<ApparatusRig>().RefreshTransmissionFixtures();
        }
        VlabConnectingRod ActiveRod=>Instrument<VlabConnectingRod>("rod"+(int)P("rod"));
        void ConfigureCrank()
        {
            motor=Instrument<VlabMotor>("motor");motor.Stop();motor.TrySetRpm(P("rpm"));motor.TrySetAngle(0);
            foreach(int length in new[]{80,100,120})equipment["rod"+length].gameObject.SetActive(length==(int)P("rod"));
            var crank=Instrument<VlabCrank>("crank");Check(crank.Configure((int)P("radius"),0),"Crank supports only 20/30/40 mm holes.");
            AlignMotor(crank.Shaft.position,crank.Axis);
            if(!sliderFrame){var f=new GameObject("Lesson inline slider frame");f.transform.SetParent(equipment["rail"].transform.parent,false);sliderFrame=f.transform;}
            var zero=S("rail","_TravelZero_SOCKET");var lengthDirection=(crank.RodPin.position-crank.Shaft.position);lengthDirection=Vector3.ProjectOnPlane(lengthDirection,crank.Axis).normalized;
            var rotation=Quaternion.LookRotation(Vector3.Cross(lengthDirection,crank.Axis),crank.Axis);equipment["rail"].transform.rotation=rotation;sliderFrame.SetPositionAndRotation(zero.position,rotation);
            var slider=Instrument<VlabSlider>("slider");Check(slider.TryBind(sliderFrame),"Slider guide binding failed.");
            var railAxis=sliderFrame.right;double lengthM=P("rod")*.001;
            // In the assembly's own plane the guide zero is l from the crank shaft.
            var desired=crank.RodPin.position+railAxis*(float)(lengthM-P("radius")*.001);
            var pinOffset=S("slider","_RodFace_SOCKET").position-sliderFrame.position;var shift=desired-pinOffset-sliderFrame.position;
            sliderFrame.position+=shift;equipment["rail"].transform.position+=shift;UpdateCrank(0);minSlider=double.PositiveInfinity;maxSlider=double.NegativeInfinity;
        }
        void UpdateCrank(double angle)
        {
            var crank=Instrument<VlabCrank>("crank");Check(crank.Configure((int)P("radius"),angle),"Crank indexed radius invalid.");
            double a=P("radius")*.001,l=P("rod")*.001,theta=angle*Math.PI/180,x=a*Math.Cos(theta)+Math.Sqrt(l*l-a*a*Math.Sin(theta)*Math.Sin(theta));
            var slider=Instrument<VlabSlider>("slider");Check(slider.TrySetTravel((float)(x-l)),"Slider exceeds its physical guide travel.");
            Check(ActiveRod.SetEndpoints(crank.RodPin.position,S("slider","_RodFace_SOCKET").position,crank.Axis),"Connecting rod endpoints do not match its rigid length. Align the crank and guide frames.");
            minSlider=Math.Min(minSlider,x);maxSlider=Math.Max(maxSlider,x);
        }
        void ConfigureCam()
        {
            motor=Instrument<VlabMotor>("motor");motor.Stop();motor.TrySetRpm(P("rpm"));motor.TrySetAngle(0);var cam=Instrument<VlabCam>("cam");cam.Paused=true;cam.SetAngle(0);cam.SetSpeed(P("rpm"));AlignMotor(cam.Center,cam.ShaftAxis);
            equipment["follower"].gameObject.SetActive(true);equipment["roller"].gameObject.SetActive(true);
            Check(Instrument<VlabFollower>("follower").Follow(cam),"Knife follower cannot contact the cam.");
            Check(Instrument<VlabFollower>("roller").Follow(cam),"Roller follower cannot contact the cam.");equipment["roller"].gameObject.SetActive(rollerSelected);equipment["follower"].gameObject.SetActive(!rollerSelected);
            var guide=equipment["guide"].transform;var axis=(S("guide","_Exit_SOCKET").position-S("guide","_Socket_SOCKET").position).normalized;guide.rotation=Quaternion.FromToRotation(axis,cam.FollowerDirection)*guide.rotation;
            At("guide","_Socket_SOCKET",cam.Center+cam.FollowerDirection*.065f);ZeroCamGauge();
        }
        void ZeroCamGauge()
        {
            var cam=Instrument<VlabCam>("cam");var follower=Instrument<VlabFollower>(Roller?"roller":"follower");
            camZero=Vector3.Dot(follower.transform.position-cam.Center,cam.FollowerDirection);
            var gauge=Instrument<VlabDialGauge>("gauge");gauge.SetCompression(0);var zero=gauge.Contact.position;gauge.SetCompression(1);
            var axis=(gauge.Contact.position-zero).normalized;gauge.SetCompression(0);
            gauge.transform.rotation=Quaternion.FromToRotation(axis,cam.FollowerDirection)*gauge.transform.rotation;
            gauge.transform.position+=S(Roller?"roller":"follower","_StemTop_SOCKET").position-gauge.Contact.position;
            equipment["gauge"].GetComponentInParent<ApparatusRig>().RefreshGaugeFixtures();
        }
        public void UpdateCamGaugeContact()
        {
            var cam=Instrument<VlabCam>("cam");var follower=Instrument<VlabFollower>(Roller?"roller":"follower");
            double mm=(Vector3.Dot(follower.transform.position-cam.Center,cam.FollowerDirection)-camZero)*1000;
            Check(Instrument<VlabDialGauge>("gauge").SetCompression(Math.Max(0,Math.Min(10,mm))),"Cam displacement exceeds the original gauge travel.");
        }
        void ConfigureBeam(double load)
        {
            var beam=Instrument<VlabBeam>("beam");beam.Configure(P("span"),.02,P("height")*.001,P("modulus")*1e9,load);
            var left=S("supports","_ContactLeft_SOCKET");var right=S("supports","_ContactRight_SOCKET");var axis=(right.position-left.position).normalized;
            // Delivered VlabBeam.Configure explicitly parameterizes alternate rectangular specimens.
            // Move complete rigid crowned supports by their actual contact datums; never scale a support.
            var lbody=S("supports","_SupportL_VIS");var rbody=S("supports","_SupportR_VIS");
            var centre=(left.position+right.position)*.5f;Vector3 lTarget=centre-axis*(float)(P("span")*.5),rTarget=centre+axis*(float)(P("span")*.5);
            var dl=lTarget-left.position;var dr=rTarget-right.position;lbody.position+=dl;rbody.position+=dr;
            foreach(var socket in equipment["supports"].GetComponentsInChildren<Transform>(true))
            {if(socket.name.EndsWith("_ContactLeft_SOCKET",StringComparison.Ordinal)||socket.name.EndsWith("_AxisLeft_SOCKET",StringComparison.Ordinal)||socket.name.EndsWith("_GripL_SOCKET",StringComparison.Ordinal))socket.position+=dl;else if(socket.name.EndsWith("_ContactRight_SOCKET",StringComparison.Ordinal)||socket.name.EndsWith("_AxisRight_SOCKET",StringComparison.Ordinal))socket.position+=dr;}
            var frame=beam.Frame;beam.transform.position+=left.position-frame.TransformPoint(new Vector3((float)((.3-P("span"))/2),0,0));
            var gauge=Instrument<VlabDialGauge>("gauge");gauge.SetCompression(0);var p0=gauge.Contact.position;gauge.SetCompression(1);var compression=(gauge.Contact.position-p0).normalized;gauge.SetCompression(0);
            gauge.transform.rotation=Quaternion.FromToRotation(compression,-frame.up)*gauge.transform.rotation;
            gauge.transform.position+=beam.Gauge.position+frame.up*(float)beam.MidDeflection-gauge.Contact.position;gauge.SetCompression(beam.MidDeflection*1000);
            // The mass is attached at the true midspan upper surface, independent of the gauge below.
            Instrument<VlabMassSet>("load").TrySetLoad(0,load/ScienceModels.G*1000);
            var loadBounds=equipment["load"].GetComponentsInChildren<Renderer>().Where(x=>x.enabled&&x.gameObject.activeInHierarchy).Select(x=>x.bounds).ToArray();if(loadBounds.Length>0){var bounds=loadBounds[0];foreach(var b in loadBounds)bounds.Encapsulate(b);var loadPoint=frame.TransformPoint(new Vector3(.15f,(float)(beam.Height-beam.MidDeflection),0));equipment["load"].transform.position+=loadPoint-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);}
            equipment["gauge"].GetComponentInParent<ApparatusRig>().RefreshGaugeFixtures();
        }
        void RouteNewton(double displacement)
        {
            var left=S("pulley","_CordLeft_SOCKET").position;var right=S("pulley","_CordRight_SOCKET").position;
            equipment["load"].transform.position=hangerOrigin-Vector3.up*(float)displacement;
            Instrument<VlabString>("cord").TrySetEndpoints(S("cart","_Tow_SOCKET").position,left);Instrument<VlabString>("dropCord").TrySetEndpoints(right,S("load","_HangPoint_SOCKET").position);Instrument<VlabPulley>("pulley").TrySetTurns(displacement/(2*Math.PI*.0185));
        }
        public override bool Prepare(int completedOperation,out string message)
        {
            message="";if(!configured){message="Install the complete apparatus first.";return false;}
            if(completedOperation!=prepared+1){message="Complete the current apparatus operation first.";return false;}
            try
            {
                if(completedOperation==1&&!ParameterLimits(out message))return false;
                switch(id)
                {
                    case "P02":if(completedOperation==3)Check(launcher.Loaded==ball||launcher.TryLoad(ball),"The ball cannot seat in the launcher.");break;
                    case "P03":if(completedOperation==3){Instrument<VlabForceMeter>("meter").SetForce(0);zeroed=true;}break;
                    case "P04":case "P07":case "P08":if(completedOperation==3){gateA.ResetMeasurement();gateB.ResetMeasurement();zeroed=true;}break;
                    case "P05":if(completedOperation==2){SetSpring(.15);equipment["load"].gameObject.SetActive(false);zeroed=true;}if(completedOperation==3){equipment["load"].gameObject.SetActive(true);SetSpring(.15+P("mass")*ScienceModels.G/P("k")+P("amplitude"));springPrevious=P("amplitude");}break;
                    case "P06":Instrument<VlabLatch>("latch").Release();break;
                case "M01":if(completedOperation==3){Check(Math.Abs(Vector3.Distance(S("gears","_Axis12_SOCKET").position,S("gears","_Axis24_SOCKET").position)-.036745522)<.0001,"12/24 shifted-gear centre distance is incorrect.");}break;
                    case "M02":ConfigureLever();break;
                    case "M03":RoutePulley(0);break;
                    case "M04":FitBeltPulleys();break;
                    case "M05":if(completedOperation==3){for(int a=0;a<=360;a+=15)UpdateCrank(a);UpdateCrank(0);}break;
                    case "M06":if(completedOperation==3){zeroed=true;Instrument<VlabDialGauge>("gauge").SetCompression(0);}break;
                    case "M07":if(completedOperation==1){CloseTool();zeroReading=P("zero");zeroed=true;}if(completedOperation==2)PlaceSample();if(completedOperation==3){if(!MakeContact(out message))return false;}break;
                    case "M08":if(completedOperation==2){ConfigureBeam(0);beamZero=Instrument<VlabDialGauge>("gauge").ReadingMm;zeroed=true;}if(completedOperation==3){ConfigureBeam(P("load"));beamReading=Instrument<VlabDialGauge>("gauge").ReadingMm;}break;
                }
                prepared=completedOperation;return true;
            }
            catch(Exception e){message=e.Message;return false;}
        }
        bool ParameterLimits(out string message)
        {
            message="";
            switch(id)
            {
                case "P02":return Require(ReceivingGeometryValid,ReceivingFailure,out message);
                case "P03":return Require(P("muK")<=P("muS")&&P("mass")*ScienceModels.G*P("muS")<9.8,"Choose coefficients muK <= muS and a static force within the 10 N meter.",out message);
                case "P04":return Require(Near(P("flag"),.05)&&Near(P("spacing"),.5)&&P("speed")*P("speed")>2*P("accel")*.135,"Stock flag is 50 mm and gate spacing is 500 mm. Reduce acceleration or increase approach speed so the cart reaches A from rest-compatible upstream travel.",out message);
                case "P05":double extension=P("mass")*ScienceModels.G/P("k");return Require(.15+extension+P("amplitude")<=.2&&.15+extension-P("amplitude")>=.1,"Stock spring allows only 100–200 mm total span; reduce load/amplitude or increase stiffness.",out message);
                case "P06":return Require(P("u1")>P("u2")&&CollisionReachable(),"Both carts must reach their attachment contact before either cart reaches a track end.",out message);
                case "P07":return Require(P("travel")<=.7&&P("travel")>=.16&&P("cart")*P("hanger")*ScienceModels.G/(P("cart")+P("hanger"))<=2,"Use at least 0.16 m hanging travel, at most 0.70 m; cart tension must remain within the SDK 2 N force range.",out message);
                case "P08":double theta=P("angle")*Math.PI/180,force=P("mass")*ScienceModels.G*(Math.Sin(theta)-P("mu")*Math.Cos(theta)),travel=P("height")/Math.Sin(theta);return Require(P("angle")>0&&P("angle")<=30&&travel>=.13&&travel<=.7&&force<=2,"Use 0.13–0.70 m descent for two complete 50 mm flag cycles, incline <=30 degrees and cart force <=2 N.",out message);
                case "M01":return Require((Near(P("driver"),12)||Near(P("driver"),24))&&(Near(P("driven"),12)||Near(P("driven"),24)||Near(P("driven"),36))&&!Near(P("driver"),P("driven"))&&Near(P("module"),2),"Stock module-2 train permits driver 12 or 24 and a different output 12/24/36; 12 to 36 uses the idler.",out message);
                case "M02":int l=pivotMark-(int)Math.Round(P("arm1")/.1),r=pivotMark+(int)Math.Round(P("arm2")/.1);return Require(Near(P("arm1")/.1,Math.Round(P("arm1")/.1))&&Near(P("arm2")/.1,Math.Round(P("arm2")/.1))&&l>=-4&&r<=4,"Use 0.10 m indexed positions within marks -4 to +4 around the selected pivot.",out message);
                case "M03":return Require((P("strands")==1||P("strands")==2||P("strands")==4)&&P("load")/(P("strands")*P("efficiency")/100)<=10&&P("height")<=.20,"Choose the explicit 1, 2 or 4-strand route, lift <=0.20 m and effort <=10 N.",out message);
                case "M04":return Require(StockBeltPair(P("d1"),P("d2")),"Choose the real 40→80 mm or 80→40 mm pitch-pulley pair.",out message);
                case "M05":return Require(new[]{20,30,40}.Contains((int)P("radius"))&&Near(P("radius"),(int)P("radius"))&&new[]{80,100,120}.Contains((int)P("rod"))&&Near(P("rod"),(int)P("rod")),"Use the real 20/30/40 mm crank holes and 80/100/120 mm rigid rods.",out message);
                case "M06":return Require(Near(P("lift"),10)&&Near(P("rise"),120),"This cam has a fixed 10 mm lift and 120/60/120/60 degree sectors.",out message);
                case "M08":double span=P("span"),height=P("height")*.001,I=.02*height*height*height/12,delta=P("load")*span*span*span/(48*P("modulus")*1e9*I);return Require(span<=.3&&height/span<=.1&&delta/span<=.01,"Use the delivered configurable beam: span <=0.300 m, h/L<=0.1 and delta/L<=0.01.",out message);
            }
            return true;
        }
        bool CollisionReachable(){double contact=P("elastic")>.5?.18:.1576,t=(.5-contact)/(P("u1")-P("u2"));return t>=0&&Math.Abs(-.25+P("u1")*t)<.39&&Math.Abs(.25+P("u2")*t)<.39;}
        public override bool CanStart(out string message)
        {
            if(id=="P02"&&!ReceivingGeometryValid)return Require(false,ReceivingFailure,out message);
            if(!Require(configured&&AllInstalled&&prepared>=session.spec.operationsEn.Length,"Complete every apparatus preparation operation.",out message))return false;
            if(!ParameterLimits(out message))return false;
            if(id=="P02")return Require(launcher.Loaded==ball,"Load the ball before firing.",out message);
            if(id=="M07")return Require(zeroed&&contacted&&(P("micrometer")<.5||ratcheted),"Record closed-tool zero, seat the identified sample and engage the virtual ratchet for micrometer contact.",out message);
            return true;
        }
        void Begin()
        {
            begun=true;finished=false;clock=0;samples.Clear();minSlider=double.PositiveInfinity;maxSlider=double.NegativeInfinity;
            if(cart)cart.Paused=false;if(cartB)cartB.Paused=false;
            if(id=="P02"){ball.IsPaused=false;Check(launcher.Fire((float)P("speed"),ScienceModels.G,receivingPlane+ball.Diameter*.5f),"Launcher refused the prepared shot.");}
            if(motor){motor.TrySetRpm(P("rpm"));Check(motor.TryStart(),"Motor interlock refused to start.");}
            if(id=="P04"||id=="P07"||id=="P08"){gatePositionTrace.Add(new Sample(0,cart.PositionM));gateA.TrySample(flag,0);gateB.TrySample(flag,0);}
            if(id=="P06"){preA=cart.VelocityMps;preB=cartB.VelocityMps;}
            if(id=="P05")springPrevious=P("amplitude");
        }
        public override void SetPaused(bool value)
        {
            paused=value;if(cart)cart.Paused=value;if(cartB)cartB.Paused=value;if(ball)ball.IsPaused=value;
            if(motor){if(value)motor.Stop();else if(begun&&!finished)motor.TryStart();}
            // Belt.Update is disabled through Running=false; this authority supplies one shared clock.
            var belt=Maybe<VlabBelt>("belt");if(belt)belt.Running=false;
        }
        public override void Advance(double seconds)
        {
            if(paused||!configured||seconds<=0)return;if(!begun)Begin();
            double left=seconds;while(left>1e-10){double dt=Math.Min(.001,left);left-=dt;clock+=dt;Step(dt);}
        }
        void Step(double dt)
        {
            switch(id)
            {
                case "P02":if(ball.Elapsed>0&&samples.Count<600&&(samples.Count==0||ball.Elapsed-samples[samples.Count-1].t>=.02))samples.Add(new Sample(ball.Elapsed,ball.transform.position.x,ball.transform.position.y,ball.transform.position.z));return; // Projectile has its own single FixedUpdate solver.
                case "P03":StepFriction(dt);break;
                case "P04":if(!finished){cart.Advance(dt,.2*P("accel"));SampleGates();if(gateB.HasMeasurement){finished=true;cart.Paused=true;}}break;
                case "P05":
                    double w=Math.Sqrt(P("k")/P("mass")),q=P("amplitude")*Math.Cos(w*clock);SetSpring(.15+P("mass")*ScienceModels.G/P("k")+q);
                    if(springPrevious>0&&q<=0){double cross=clock-dt+dt*springPrevious/(springPrevious-q);if(firstCross<0)firstCross=cross;else cycles++;lastCross=cross;}springPrevious=q;break;
                case "P06":StepCollision(dt);break;
                case "P07":
                    if(!finished){double a=P("hanger")*ScienceModels.G/(P("cart")+P("hanger")),distance=cart.PositionM+.35,remaining=P("travel")-.01-distance;
                        if(remaining<=0){finished=true;cart.Paused=true;}else {double step=Math.Min(dt,(Math.Sqrt(cart.VelocityMps*cart.VelocityMps+2*a*remaining)-cart.VelocityMps)/a);cart.Advance(step,P("cart")*a);RouteNewton(cart.PositionM+.35);SampleGates();}}
                    break;
                case "P08":
                    if(!finished){double angle=P("angle")*Math.PI/180,a=ScienceModels.G*(Math.Sin(angle)-P("mu")*Math.Cos(angle));if(a<=0){finished=true;cart.Paused=true;break;}
                        double remaining=cart.PositionM+.35;if(remaining<=1e-8){finished=true;cart.Paused=true;}else{double step=Math.Min(dt,(Math.Sqrt(cart.VelocityMps*cart.VelocityMps+2*a*remaining)+cart.VelocityMps)/a);cart.Advance(step,-P("mass")*a);SampleGates();if(cart.PositionM<=-.35+1e-8){finished=true;cart.Paused=true;}}}break;
                case "M01":motor.Advance(dt);Instrument<VlabGearSet>("gears").SetDriverAngle(motor.AngleDegrees/GearFactor((int)P("driver")));break;
                case "M02":break;
                case "M03":break; // Each pull control is a finite, rope-conserving quasi-static increment.
                case "M04":motor.Advance(dt);var belt=Instrument<VlabBelt>("belt");double inputBefore=belt.DriveLarge?belt.LargeTurns:belt.SmallTurns,outputBefore=belt.DriveLarge?belt.SmallTurns:belt.LargeTurns;belt.Advance(dt);beltInputTurns+=WrappedTurnDelta(inputBefore,belt.DriveLarge?belt.LargeTurns:belt.SmallTurns);beltOutputTurns+=WrappedTurnDelta(outputBefore,belt.DriveLarge?belt.SmallTurns:belt.LargeTurns);UpdateBeltPulley();break;
                case "M05":motor.Advance(dt);UpdateCrank(motor.AngleDegrees);break;
                case "M06":motor.Advance(dt);StepCam();break;
                case "M07":break;
                case "M08":break;
            }
            if(samples.Count<1200&&(samples.Count==0||clock-samples[samples.Count-1].t>=.025))AddSample();
        }
        void SampleGates()
        {
            // Retain the actual SDK positions at the common sampling clock so energy can
            // be compared at the midpoint of the same finite interval used for d/t.
            if(!captureA.complete||!captureB.complete)gatePositionTrace.Add(new Sample(clock,cart.PositionM));
            CaptureGate(gateA,captureA);CaptureGate(gateB,captureB);
        }
        void CaptureGate(VlabPhotogate gate,GateCapture captured)
        {
            bool before=gate.Blocked;Check(gate.TrySample(flag,clock),"The optical gate rejected the aligned flag or monotonic sample clock.");
            if(!before&&gate.Blocked){captured.Clear();captured.entered=clock;}
            if(before&&!gate.Blocked&&gate.HasMeasurement)
            {
                captured.complete=true;captured.exited=clock;captured.blockingSeconds=gate.DurationSeconds;captured.flagWidth=flag.BlockingWidthM;captured.meanSpeed=flag.BlockingWidthM/gate.DurationSeconds;
                double midpoint=(captured.entered+captured.exited)/2;captured.midpointPosition=PositionAtGateTime(midpoint);
            }
        }
        double PositionAtGateTime(double t)
        {
            for(int i=1;i<gatePositionTrace.Count;i++)if(gatePositionTrace[i].t>=t)
            {var a=gatePositionTrace[i-1];var b=gatePositionTrace[i];return a.a+(b.a-a.a)*(t-a.t)/(b.t-a.t);}
            throw new InvalidOperationException("The gate interval has no corresponding SDK position trace.");
        }
        double ActualGateSpacing=>Vector3.Distance((S("gateA","_BeamStart_SOCKET").position+S("gateA","_BeamEnd_SOCKET").position)*.5f,(S("gateB","_BeamStart_SOCKET").position+S("gateB","_BeamEnd_SOCKET").position)*.5f);
        public static double AccelerationFromGateIntervals(double width,double timeA,double timeB,double spacing)
        {
            if(!double.IsFinite(width)||!double.IsFinite(timeA)||!double.IsFinite(timeB)||!double.IsFinite(spacing)||width<=0||timeA<=0||timeB<=0||spacing<=0)throw new ArgumentOutOfRangeException("Full positive gate intervals, measured width and distinct gate centres are required.");
            double a=width/timeA,b=width/timeB;return (b*b-a*a)/(2*spacing);
        }
        public static double GateKineticEnergy(double mass,double width,double blockingTime)
        {
            if(!double.IsFinite(mass)||!double.IsFinite(width)||!double.IsFinite(blockingTime)||mass<=0||width<=0||blockingTime<=0)throw new ArgumentOutOfRangeException("Positive mass and a complete blocking interval are required.");double speed=width/blockingTime;return .5*mass*speed*speed;
        }
        static double GearFactor(int teeth)=>teeth==12?1:teeth==24?-.5:teeth==36?1d/3:throw new ArgumentOutOfRangeException(nameof(teeth));
        public static double StockGearRatio(int driver,int driven)
        {if((driver!=12&&driver!=24)||driver==driven)throw new ArgumentOutOfRangeException(nameof(driver));return GearFactor(driven)/GearFactor(driver);}
        public static bool StockBeltPair(double input,double output)=>(Near(input,40)&&Near(output,80))||(Near(input,80)&&Near(output,40));
        static double WrappedTurnDelta(double before,double after){double d=after-before;if(d>.5)d-=1;else if(d<-.5)d+=1;return d;}
        void StepFriction(double dt)
        {
            var block=Instrument<VlabFrictionBlock>("block");Check(block.TryFriction(P("mass")*ScienceModels.G,pull,blockV,P("muS"),P("muK"),out var friction,out bool sliding),"Friction inputs are invalid.");
            if(!sliding)staticPeak=Math.Max(staticPeak,pull);else if(staticPeak==0)staticPeak=P("mass")*ScienceModels.G*P("muS");
            double previousV=blockV;blockV+=(pull+friction)/P("mass")*dt;if(previousV>0&&blockV<0)blockV=0;blockX+=blockV*dt;
            if(blockX>.30){blockX=.30;blockV=0;finished=true;}
            if(steady&&sliding&&Math.Abs(pull+friction)<.0001&&!finished){steadySum+=pull*dt;steadyTime+=dt;}
            equipment["block"].transform.position=blockOrigin+Vector3.right*(float)blockX;Instrument<VlabForceMeter>("meter").SetForce((float)pull);AlignFrictionCord();
        }
        void StepCollision(double dt)
        {
            if(finished)return;
            double separation=P("elastic")>.5?ElasticSeparation():.1576;
            double approach=cart.VelocityMps-cartB.VelocityMps,until=approach>0?(cartB.PositionM-cart.PositionM-separation)/approach:double.PositiveInfinity;
            if(!encounter&&until>=-1e-8&&until<=dt)
            {
                cart.Advance(Math.Max(0,until),0);cartB.Advance(Math.Max(0,until),0);collisionTime=clock-dt+Math.Max(0,until);preA=cart.VelocityMps;preB=cartB.VelocityMps;
                samples.Add(new Sample(collisionTime,cart.PositionM,preA,preB));
                if(P("elastic")<.5){Check(Instrument<VlabLatch>("latch").TryLock(cart,cartB),"Sticking attachment did not meet both cart sockets at contact.");postA=cart.VelocityMps;postB=cartB.VelocityMps;}
                else{double m=cart.MassKg,n=cartB.MassKg;postA=((m-n)*preA+2*n*preB)/(m+n);postB=((n-m)*preB+2*m*preA)/(m+n);cart.ResetTrial(m,cart.PositionM,postA);cartB.ResetTrial(n,cartB.PositionM,postB);}
                encounter=true;cart.Paused=cartB.Paused=false;double rest=Math.Max(0,dt-until);cart.Advance(rest,0);cartB.Advance(rest,0);samples.Add(new Sample(collisionTime,cart.PositionM,postA,postB));
            }
            else{cart.Advance(dt,0);cartB.Advance(dt,0);}
            if(encounter&&clock-collisionTime>.05){finished=true;cart.Paused=cartB.Paused=true;}
        }
        double ElasticSeparation()
        {
            var a=Instrument<VlabElasticBumper>("bumperA").ContactFace.position;var b=Instrument<VlabElasticBumper>("bumperB").ContactFace.position;
            return cartB.PositionM-cart.PositionM-Vector3.Dot(b-a,cartFrame.right);
        }
        void UpdateBeltPulley(){var b=Instrument<VlabBelt>("belt");beltSmall.localRotation=beltSmallRest*Quaternion.AngleAxis(-(float)(b.SmallTurns*360),beltSmallAxis);beltLarge.localRotation=beltLargeRest*Quaternion.AngleAxis(-(float)(b.LargeTurns*360),beltLargeAxis);}
        bool Roller=>equipment.ContainsKey("roller")&&equipment["roller"].gameObject.activeSelf;
        void StepCam()
        {
            var cam=Instrument<VlabCam>("cam");cam.SetAngle(motor.AngleDegrees);var follower=Instrument<VlabFollower>(Roller?"roller":"follower");Check(follower.Follow(cam),"Follower lost geometric contact.");
            UpdateCamGaugeContact();
        }
        void OnImpact(Vector3 point){if(hit)return;hit=true;target.TryMarkWorld(point-Vector3.up*(ball.Diameter*.5f));finished=true;samples.Add(new Sample(ball.Elapsed,point.x,point.y,point.z));}
        void AddSample()
        {
            switch(id){case "P03":samples.Add(new Sample(clock,blockX,pull,blockV));break;case "P04":case "P07":case "P08":samples.Add(new Sample(clock,cart.PositionM,cart.VelocityMps,cart.WorkJ));break;case "P05":samples.Add(new Sample(clock,Instrument<VlabSpring>("spring").LengthM));break;case "P06":samples.Add(new Sample(clock,cart.PositionM,cart.VelocityMps,cartB.VelocityMps));break;case "M01":samples.Add(new Sample(clock,motor.AngleDegrees,OutputGearAngle));break;case "M04":samples.Add(new Sample(clock,motor.AngleDegrees,Instrument<VlabBelt>("belt").OutputRpm));break;case "M05":samples.Add(new Sample(clock,motor.AngleDegrees,Instrument<VlabSlider>("slider").TravelM*1000));break;case "M06":samples.Add(new Sample(clock,Instrument<VlabCam>("cam").AngleDegrees,Instrument<VlabDialGauge>("gauge").ReadingMm));break;}
        }
        double InputGearAngle=>Instrument<VlabGearSet>("gears").AngleDegrees*GearFactor((int)P("driver"));
        double OutputGearAngle=>Instrument<VlabGearSet>("gears").AngleDegrees*GearFactor((int)P("driven"));
        string SampleId=>new[]{"cylinder-cardinal-12.10","block-length-20.00","tube-cardinal-16.00"}[sampleIndex];
        double SampleSize=>new[]{12.1,20,16}[sampleIndex];
        void CloseTool(){var caliper=Instrument<VlabCaliper>("caliper");caliper.Locked=false;caliper.SetOpening(0);var mic=Instrument<VlabMicrometer>("micrometer");mic.TrySetOpening(0);mic.TrySetRatchet(false);contacted=ratcheted=false;}
        Collider SelectedShape=>equipment["samples"].GetComponentsInChildren<MeshCollider>(true).First(c=>c.name.EndsWith(new[]{"_Cylinder_VIS","_Block_VIS","_Tube_VIS"}[sampleIndex],StringComparison.Ordinal));
        void PlaceSample()
        {
            var shape=SelectedShape;var tool=P("micrometer")>.5?Instrument<VlabMicrometer>("micrometer").FixedFace:Instrument<VlabCaliper>("caliper").FixedFace;Vector3 axis=P("micrometer")>.5?Instrument<VlabMicrometer>("micrometer").Axis:Instrument<VlabCaliper>("caliper").Axis;
            // Source cardinal X is the exact diameter/length axis; retain the nonconvex collider.
            var root=equipment["samples"].transform;root.rotation=Quaternion.FromToRotation(root.right,axis)*root.rotation;Physics.SyncTransforms();root.position+=tool.position+axis*(float)(SampleSize*.0005)-shape.bounds.center;Physics.SyncTransforms();contacted=ratcheted=false;
        }
        bool MakeContact(out string message)
        {
            message="";if(!zeroed){message="Close the tool and record its zero before sample contact.";return false;}
            PlaceSample();if(P("micrometer")>.5){var mic=Instrument<VlabMicrometer>("micrometer");if(!mic.TryMeasureOutside(SelectedShape)){message="The identified sample is not aligned with both micrometer faces.";return false;}mic.TrySetRatchet(true);ratcheted=mic.RatchetEngaged;contactReading=mic.ReadingMm;}
            else{var cal=Instrument<VlabCaliper>("caliper");if(!cal.TryMeasureOutside(SelectedShape)){message="The identified sample is not aligned with both caliper faces.";return false;}cal.Locked=true;contactReading=cal.ReadingMm;}
            contacted=true;return true;
        }
        public override bool CanRecord(out string message)
        {
            if(!Require(begun,"Run the prepared apparatus before recording.",out message))return false;
            switch(id)
            {
                case "P02":return Require(hit&&!ball.IsFlying&&target.HasHit,"Wait for the first impact on the receiving mat.",out message);
                case "P03":return Require(staticPeak>0&&steadyTime>=.5,"Raise the horizontal pull through breakaway, then select steady pull and observe at least 0.5 s before the block reaches the tile end.",out message);
                case "P04":return Require(gateA.HasMeasurement&&gateB.HasMeasurement,"Both gates require a fresh unblocked → blocked → unblocked flag cycle.",out message);
                case "P05":return Require(zeroed&&cycles>=10,"Measure the unloaded length, then observe ten complete same-direction spring cycles.",out message);
                case "P06":return Require(encounter&&finished,"Wait for a single physical attachment encounter and both post-impact velocity samples.",out message);
                case "P07":return Require(captureA.complete&&captureB.complete&&gateA.HasMeasurement&&gateB.HasMeasurement&&!cart.AtLimit,"Complete both gates before the hanging mass reaches the bottom-stop margin.",out message);
                case "P08":
                    if(P("mu")>=Math.Tan(P("angle")*Math.PI/180))return Require(finished&&clock>=.5&&Math.Abs(cart.PositionM-releasePosition)<1e-8&&Math.Abs(cart.VelocityMps)<1e-8&&!gateA.Blocked&&!gateB.Blocked&&!captureA.complete&&!captureB.complete,"Observe the cart stationary for 0.5 s with neither downstream gate struck; no gate speed is available for a stalled trial.",out message);
                    return Require(finished&&captureA.complete&&captureB.complete&&gateA.HasMeasurement&&gateB.HasMeasurement&&!cart.AtLimit,"Complete both actual flag-blocking cycles before the end of the measured descent.",out message);
                case "M01":case "M04":return Require(clock>=60/P("rpm"),"Observe at least one complete input revolution.",out message);
                case "M03":return Require(blockX>=P("height")-1e-8,"Use the pull-rope control until the selected load rise is reached.",out message);
                case "M05":case "M06":return Require(clock>=60/P("rpm")&&samples.Count>=12,"Observe one complete cycle and retain its angle/displacement samples.",out message);
                case "M07":return Require(zeroed&&contacted&&(P("micrometer")<.5||ratcheted),"Record zero and obtain real sample contact with the appropriate locking/ratchet state.",out message);
                case "M08":return Require(zeroed&&unloaded,"Read the loaded gauge, remove the load and verify elastic return before recording.",out message);
                default:return Require(clock>=.25,"Allow the static apparatus observation to settle.",out message);
            }
        }
        ModelResult R()=>new ModelResult();
        ModelResult Reading(ModelResult r,string key,double value,string unit,string vi,string en)=>r.Add(key,value,unit,vi,en);
        public override ModelResult Observe()
        {
            if(!configured)return new ModelResult{note="Install and prepare the delivered instruments."};var r=R();
            switch(id)
            {
                case "P02":r.Add("flightTime",ball.Elapsed,"s","Thời gian bay","Flight time").Add("range",ball.HorizontalRange,"m","Tầm xa chạm đầu","First-impact range").Add("launchHeight",P("height"),"m","Độ cao nòng thực","Actual muzzle height").Add("targetHit",target.HasHit?1:0,"","Vết chạm trên tấm","Impact on mat");r.note="Analytic projectile sampled by the delivered single flight clock; first contact only, no air drag. Height is derived from the seated launcher muzzle and mat, not an unsupported floating-launcher input. Ball-centre offset at launch is retained.";break;
                case "P03":r.Add("normalForce",P("mass")*ScienceModels.G,"N","Phản lực","Normal force").Add("staticPeak",staticPeak,"N","Đỉnh trước trượt đã quan sát","Observed pre-slide peak").Add("slidingForce",steadyTime>0?steadySum/steadyTime:0,"N","Lực kéo đều trung bình","Mean steady pull").Add("currentPull",pull,"N","Lực kéo hiện tại","Current pull").Add("steadyInterval",steadyTime,"s","Khoảng kéo đều","Steady interval");r.note="Declared Coulomb coefficients and nominal mass; horizontal pull only. The static peak is the last learner-controlled force sample before slip; its increment bounds the threshold resolution. The explicit slow-steady control idealizes the learner braking to at most 0.05 m/s and matching pull to kinetic friction; this hand-control transient is not dynamically simulated.";break;
                case "P04":case "P07":
                    r.Add("flagWidth",flag.BlockingWidthM,"m","Bề rộng bản chắn thực","Actual flag width").Add("gateSpacing",ActualGateSpacing,"m","Khoảng tâm cổng thực","Actual gate centre spacing");
                    if(captureA.complete)r.Add("gateA",captureA.blockingSeconds,"s","Thời gian che A","A blocking interval").Add("speedA",captureA.meanSpeed,"m/s","Tốc độ d/t trung bình tại A","Measured mean A speed d/t");
                    if(captureB.complete)r.Add("gateB",captureB.blockingSeconds,"s","Thời gian che B","B blocking interval").Add("speedB",captureB.meanSpeed,"m/s","Tốc độ d/t trung bình tại B","Measured mean B speed d/t");
                    double measuredA=captureA.complete&&captureB.complete?AccelerationFromGateIntervals(flag.BlockingWidthM,captureA.blockingSeconds,captureB.blockingSeconds,ActualGateSpacing):0;
                    if(captureA.complete&&captureB.complete)r.Add("acceleration",measuredA,"m/s²","Gia tốc suy từ hai cổng","Acceleration derived from two gates");
                    if(id=="P07"){double prediction=P("hanger")*ScienceModels.G/(P("cart")+P("hanger"));r.Add("predictedAcceleration",prediction,"m/s²","Gia tốc dự đoán lý tưởng","Predicted ideal acceleration").Add("externalForce",P("hanger")*ScienceModels.G,"N","Ngoại lực theo tải đã khai báo","Declared system driving weight").Add("predictedTension",P("cart")*prediction,"N","Lực căng dự đoán","Predicted ideal tension").Add("fall",cart.PositionM+.35,"m","Độ rơi đã quan sát","Observed fall");if(captureA.complete&&captureB.complete)r.Add("tension",P("cart")*measuredA,"N","Lực căng suy từ gia tốc đo","Tension inferred from measured acceleration");}
                    r.note="Actual 50 mm flag width, actual gate centre spacing and fresh optical blocking intervals on the 1 ms common clock. Reported acceleration comes from (vB²−vA²)/(2s) using measured d/t means, so finite flag/timestamp errors remain; the ideal coupled prediction is labelled separately. No instantaneous-centre-speed or hardware timing claim.";break;
                case "P05":double extension=P("mass")*ScienceModels.G/P("k"),period=cycles>0?(lastCross-firstCross)/cycles:0;r.Add("unloadedLength",.15,"m","Chiều dài không tải","Unloaded length").Add("loadedLength",.15+extension,"m","Chiều dài cân bằng","Equilibrium length").Add("extension",extension,"m","Độ giãn tĩnh","Static extension").Add("timedCycles",cycles,"","Chu kỳ đã đếm","Timed cycles").Add("period",period,"s","Chu kỳ đo qua mốc","Crossing-measured period").Add("staticStiffness",P("mass")*ScienceModels.G/extension,"N/m","Độ cứng từ tĩnh","Static stiffness").Add("dynamicStiffness",period>0?4*Math.PI*Math.PI*P("mass")/(period*period):0,"N/m","Độ cứng từ chu kỳ","Period-derived stiffness");r.note="Massless linear spring and nominal hanging mass; no damping. Endpoint geometry changes through the stock 100–200 mm UV spring API; hook/wire dimensions remain rigid.";break;
                case "P06":double m=P("m1"),n=P("m2");r.Add("u1",preA,"m/s","Vận tốc A trước","A before velocity").Add("u2",preB,"m/s","Vận tốc B trước","B before velocity").Add("velocityA",postA,"m/s","Vận tốc A sau","A after velocity").Add("velocityB",postB,"m/s","Vận tốc B sau","B after velocity").Add("momentumBefore",m*preA+n*preB,"kg·m/s","Động lượng trước","Momentum before").Add("momentumAfter",m*postA+n*postB,"kg·m/s","Động lượng sau","Momentum after").Add("energyBefore",.5*m*preA*preA+.5*n*preB*preB,"J","Động năng trước","Energy before").Add("energyAfter",.5*m*postA*postA+.5*n*postB*postB,"J","Động năng sau","Energy after").Add("encounters",encounter?1:0,"","Số lần va chạm","Encounter count");r.note="Signed velocities from the common-clock cart states at the single attachment-contact checkpoint. Ideal 1D elastic impulse or stock latch locking; translational energy loss is a model quantity.";break;
                case "P08":
                    bool stalled=P("mu")>=Math.Tan(P("angle")*Math.PI/180);r.Add("stalled",stalled?1:0,"","Không tự trượt","Stalled").Add("observationTime",clock,"s","Thời gian đã quan sát","Observation duration").Add("observedDisplacement",releasePosition-cart.PositionM,"m","Dịch chuyển xe thực","Observed cart displacement").Add("hasGateMeasurement",captureB.complete?1:0,"","Có số đo cổng hoàn chỉnh","Complete gate measurement available");
                    if(captureA.complete)r.Add("gateA",captureA.blockingSeconds,"s","Thời gian che A","A blocking interval").Add("speedA",captureA.meanSpeed,"m/s","Tốc độ d/t tại A","Measured mean A speed d/t");
                    if(captureB.complete)
                    {
                        double theta=P("angle")*Math.PI/180,distance=releasePosition-captureB.midpointPosition,h=distance*Math.Sin(theta),ke=GateKineticEnergy(P("mass"),captureB.flagWidth,captureB.blockingSeconds),loss=P("mu")*P("mass")*ScienceModels.G*Math.Cos(theta)*distance;
                        r.Add("gateB",captureB.blockingSeconds,"s","Thời gian che B","B blocking interval").Add("flagWidth",captureB.flagWidth,"m","Bề rộng chắn thực","Actual flag width").Add("speed",captureB.meanSpeed,"m/s","Tốc độ đo d/t tại B","Measured mean B speed d/t").Add("verticalDrop",h,"m","Độ giảm cao tới giữa lần che B","Drop at B blocking-time midpoint").Add("potentialEnergy",P("mass")*ScienceModels.G*h,"J","Thế năng giảm tới cùng mốc đo","Potential decrease at matching measurement time").Add("kineticEnergy",ke,"J","Động năng suy từ tốc độ cổng B","Kinetic energy from B gate speed").Add("frictionLoss",loss,"J","Công ma sát dự đoán tới mốc đo","Predicted frictional work to measurement time").Add("predictedKineticEnergy",Math.Max(0,P("mass")*ScienceModels.G*h-loss),"J","Động năng dự đoán lý tưởng","Predicted ideal kinetic energy").Add("energyResidual",P("mass")*ScienceModels.G*h-ke-loss,"J","Sai lệch cân bằng năng lượng đo","Measured energy balance residual");
                    }
                    r.note=stalled?"Stationary trial: require 0.5 s actual unchanged SDK cart position/velocity and neither downstream gate struck. There is no measured gate speed or kinetic-energy reading; absent intervals are not reported as measured zeros.":"Two complete actual optical cycles are required. K=0.5*m*(flagWidth/blockingTimeB)^2; height is taken from retained SDK positions at the midpoint of that same blocking interval. Finite flag/1 ms timing error is retained. Declared sliding-friction prediction is separate; no wheel rotational energy.";break;
                case "M01":double ratio=StockGearRatio((int)P("driver"),(int)P("driven")),inputRpm=clock>0?InputGearAngle/(clock*6):0,outputRpm=clock>0?OutputGearAngle/(clock*6):0;r.Add("inputSpeed",inputRpm,"rpm","Tốc độ vào từ góc SDK đã quan sát","Input speed from observed SDK angle").Add("outputSpeed",outputRpm,"rpm","Tốc độ ra từ góc SDK đã quan sát","Output speed from observed SDK angle").Add("signedRatio",clock>0?OutputGearAngle/InputGearAngle:ratio,"","Tỷ số góc có dấu","Signed observed angle ratio").Add("inputTurns",InputGearAngle/360,"rev","Vòng vào đã quan sát","Observed input turns").Add("outputTurns",OutputGearAngle/360,"rev","Vòng ra đã quan sát","Observed output turns").Add("centreDistance12to24",Vector3.Distance(S("gears","_Axis12_SOCKET").position,S("gears","_Axis24_SOCKET").position)*1000,"mm","Khoảng tâm 12–24","12–24 centre distance");Instrument<VlabTachometer>("tach").TrySetRpm(outputRpm);r.note="The real module-2 12–24–36 SDK train is driven at the selected 12 or 24 shaft. Unwrapped input/output SDK angles give signed speeds and ratios, including 24→12 speed increase and the 12↔36 idler direction. Actual shifted-tooth centres remain unchanged; no torque is inferred.";break;
                case "M02":Instrument<VlabLever>("lever").TryMoments(pivotMark-(int)Math.Round(P("arm1")/.1),pivotMark+(int)Math.Round(P("arm2")/.1),P("force1"),P("force2"),.5,out double torque,out double support);r.Add("netMoment",torque,"N·m","Tổng mômen có dấu","Signed net moment").Add("supportForce",support,"N","Phản lực điểm tựa","Support force").Add("beamWeight",.5,"N","Trọng lượng thanh giả định","Declared beam weight").Add("pivotMark",pivotMark,"","Vạch điểm tựa","Pivot mark").Add("balanced",Math.Abs(torque)<.005?1:0,"","Cân bằng mômen","Moment balance");r.note="Two nominal downward forces at real 0.1 m marks plus explicit 0.5 N uniform beam weight. Horizontal supported observation; no automatic dynamic balance claim.";break;
                case "M03":double effort=P("load")/(P("strands")*P("efficiency")/100);r.Add("effortForce",Instrument<VlabForceMeter>("meter").ForceN,"N","Lực kéo đọc được","Effort meter").Add("supportingStrands",P("strands"),"","Nhánh đỡ tải","Supporting strands").Add("lift",blockX,"m","Độ nâng tải","Load rise").Add("pullDistance",blockX*P("strands"),"m","Quãng kéo đầu dây","Free-end pull").Add("loadWork",P("load")*blockX,"J","Công có ích","Load work").Add("inputWork",effort*blockX*P("strands"),"J","Công đầu vào","Input work").Add("routedLength",ropeLength,"m","Dài dây theo tuyến thực","Actual routed polyline length").Add("ropeLengthResidual",ropeLength-ropeReference,"m","Sai lệch bảo toàn dài dây","Rope length residual");r.note="Explicit fixed/moving block route with checked geometric rope conservation s=N*h. Quasi-static nominal total lifted load includes moving-block weight by definition; declared efficiency affects effort only.";break;
                case "M04":var belt=Instrument<VlabBelt>("belt");double beltOutputRpm=clock>0?beltOutputTurns*60/clock:0;r.Add("driverPitch",P("d1"),"mm","Đường kính chia chủ động","Driving pitch diameter").Add("drivenPitch",P("d2"),"mm","Đường kính chia bị động","Driven pitch diameter").Add("inputSpeed",clock>0?beltInputTurns*60/clock:0,"rpm","Tốc độ vào từ vòng SDK đã quan sát","Input speed from observed SDK turns").Add("outputSpeed",beltOutputRpm,"rpm","Tốc độ ra từ vòng SDK đã quan sát","Signed output speed from observed SDK turns").Add("predictedOutputSpeed",belt.OutputRpm,"rpm","Tốc độ ra dự đoán theo cấu hình","Predicted configured output speed").Add("inputTurns",beltInputTurns,"rev","Vòng vào SDK đã quan sát","Observed input SDK turns").Add("outputTurns",beltOutputTurns,"rev","Vòng ra SDK đã quan sát","Observed output SDK turns").Add("centreDistance",belt.Current.centerDistance*1000,"mm","Khoảng tâm bộ đai","Belt centre distance").Add("beltLength",belt.Current.length,"m","Dài đường tâm đai","Measured mesh centreline length");if(Math.Abs(beltInputTurns)>1e-12)r.Add("signedRatio",beltOutputTurns/beltInputTurns,"","Tỷ số vòng có dấu","Signed observed turn ratio");Instrument<VlabTachometer>("tach").TrySetRpm(beltOutputRpm);r.note="Selected authored fixed-length belt and actual 40/80 mm pitch pulleys; either the small or large pulley is connected to the motor at its real shaft socket. SDK ConfigureDrive selects DriveLarge for 80→40 speed increase. Observed unwrapped SDK turns give speed/direction; prescribed fractional slip acts at output. No tension/torque inference.";break;
                case "M05":r.Add("angle",motor.AngleDegrees%360,"°","Góc tay quay","Crank angle").Add("sliderPosition",Instrument<VlabSlider>("slider").TravelM*1000,"mm","Vị trí từ gốc ray","Position from guide origin").Add("observedStroke",double.IsInfinity(minSlider)?0:(maxSlider-minSlider)*1000,"mm","Hành trình đã lấy mẫu","Sampled stroke");r.note="Inline rigid linkage; the guide coordinate is x-l. Endpoint-constrained stock connecting rod, indexed crank holes, unit-scale guide; kinematics only.";break;
                case "M06":var cam=Instrument<VlabCam>("cam");r.Add("angle",cam.AngleDegrees,"°","Góc cam","Cam angle").Add("lift",Instrument<VlabDialGauge>("gauge").ReadingMm,"mm","Độ nâng tiếp xúc","Contact displacement").Add("idealKnifeLift",cam.IdealLiftMeters*1000,"mm","Luật dao lý tưởng","Ideal knife law").Add("roller",Roller?1:0,"","Con đội lăn","Roller follower");r.note="Stock 25 mm base/10 mm lift, 120/60/120/60 sectors. Geometric follower contact; roller centre curve is recomputed from polygon contact and differs from the knife law. The brief's h/rise-sector variations require a newly authored cam and remain unsupported under the original-model constraint. No spring-force or separation claim.";break;
                case "M07":double resolution=P("micrometer")>.5?.01:.02,raw=contactReading+P("zero");r.Add("rawReading",raw,"mm","Số chỉ có sai lệch zero","Biased contact indication").Add("zeroError",zeroReading,"mm","Sai lệch khi đóng","Closed-tool zero error").Add("correctedReading",raw-zeroReading,"mm","Kích thước hiệu chỉnh","Corrected contact dimension").Add("resolution",resolution,"mm","Độ chia","Scale resolution").Add("sampleIndex",sampleIndex,"","Mã mẫu","Sample index");r.note=SampleId+"; actual nonconvex sample contact raycasts. Virtual bias is consistently added after contact quantization and at closed zero; resolution is not a certified accuracy. Ratchet state is virtual, not sensed force.";break;
                case "M08":var beam=Instrument<VlabBeam>("beam");r.Add("deflection",beamReading-beamZero,"mm","Độ võng đọc khi tải","Loaded gauge deflection").Add("returnReading",Instrument<VlabDialGauge>("gauge").ReadingMm-beamZero,"mm","Số chỉ sau tháo tải","Unloaded return reading").Add("sectionMoment",beam.Inertia,"m⁴","Mômen tiết diện","Section second moment").Add("supportSpan",beam.Span,"m","Khẩu độ hai gối","Support span").Add("unloaded",unloaded?1:0,"","Đã thử hồi phục","Unload check complete");r.note="Simply supported central load, declared illustrative modulus, small deflection delta/L<=0.01. Crowned rigid supports match the selected span; gauge follows actual beam underside datum. Self-weight, shear, strength and fracture are omitted.";break;
            }
            r.detailJson=JsonUtility.ToJson(new Evidence{lesson=id,kind="delivered-apparatus-virtual-observation",assumptions=r.note,sampleId=id=="M07"?SampleId:"",sample=sampleIndex,pivot=pivotMark,belt=id=="M04"?Instrument<VlabBelt>("belt").Selected:beltIndex,beltConfiguration=id=="M04"?Instrument<VlabBelt>("belt").Current.name:"",driveLarge=id=="M04"&&Instrument<VlabBelt>("belt").DriveLarge,gearDriverTeeth=id=="M01"?(int)P("driver"):0,gearDrivenTeeth=id=="M01"?(int)P("driven"):0,followerType=id=="M06"?(Roller?"roller-offset-contact":"knife-radial-contact"):"",cycles=cycles,contact=contacted,ratchet=ratcheted,unloaded=unloaded,series=samples.ToArray(),gateA=id=="P04"||id=="P07"||id=="P08"?captureA:null,gateB=id=="P04"||id=="P07"||id=="P08"?captureB:null});return r;
        }
        public override bool TryMeasure(out ModelResult result,out string message){result=null;if(!CanRecord(out message))return false;result=Observe();SetPaused(true);return true;}
        public override LessonControl[] Controls(string key)
        {
            bool editable=!begun;var a=new List<LessonControl>();
            if(id=="P03"&&(key=="meter"||key=="block")){a.Add(new LessonControl("pull+","Tăng lực kéo 0,05 N","Raise pull by 0.05 N",begun&&!finished));a.Add(new LessonControl("steady","Kéo chậm và đều","Pull slowly and steadily",begun&&blockV>0&&!finished));a.Add(new LessonControl("resetPull","Đưa khối về đầu / đo lại","Return block / repeat pull",begun&&finished));}
            if(id=="M02"&&key=="fulcrum")a.Add(new LessonControl("pivot","Chuyển lỗ điểm tựa −1/0/+1","Cycle actual pivot bores",editable));
            if(id=="M03"&&(key=="meter"||key=="cord"))a.Add(new LessonControl("pull-rope","Kéo đầu dây 10 mm","Pull free end 10 mm",begun&&!finished));
            if(id=="M04"&&key=="belt")a.Add(new LessonControl("belt-span","Chọn bộ đai mở 100/140/180","Cycle fixed open belt length",editable&&P("crossed")<.5));
            if(id=="M06"&&(key=="follower"||key=="roller"))a.Add(new LessonControl("follower-type","Đổi đầu dao / con lăn","Switch knife / roller contact",editable));
            if(id=="M07"&&(key=="caliper"||key=="micrometer"||key=="samples")){a.Add(new LessonControl("sample","Chọn mẫu trụ / khối / ống","Cycle cylinder / block / tube",editable));a.Add(new LessonControl("zero","Đóng dụng cụ và ghi zero","Close tool and record zero",editable));a.Add(new LessonControl("contact","Đặt mẫu và tiếp xúc nhẹ","Seat sample and make contact",zeroed));}
            if(id=="M08"&&(key=="load"||key=="gauge")){a.Add(new LessonControl("unload","Tháo tải và kiểm tra hồi phục","Remove load and verify return",begun&&!unloaded));}
            return a.ToArray();
        }
        public override bool Operate(string action,out string message)
        {
            message="";try
            {
                switch(action)
                {
                    case "resetPull":
                        if(id!="P03"||!begun||!finished)return Require(false,"Repeat the pull only after the block reaches the tile end.",out message);
                        pull=blockX=blockV=staticPeak=steadySum=steadyTime=0;steady=finished=false;samples.Clear();
                        equipment["block"].transform.position=blockOrigin;Instrument<VlabForceMeter>("meter").SetForce(0);AlignFrictionCord();return true;
                    case "pull+":if(!begun||finished)return Require(false,"Run the prepared pull before changing force.",out message);pull=Math.Min(9.9,pull+.05);steady=false;return true;
                    case "steady":
                        if(blockV<=0)return Require(false,"Increase pull until the block begins sliding.",out message);
                        // The explicit hand-control action slows the block before matching its sliding friction.
                        blockV=Math.Min(blockV,.05);pull=P("muK")*P("mass")*ScienceModels.G;steady=true;return true;
                    case "pivot":if(begun)return false;pivotMark=pivotMark==1?-1:pivotMark+1;ConfigureLever();return true;
                    case "pull-rope":if(!begun||finished)return false;blockX=Math.Min(P("height"),blockX+.01/P("strands"));RoutePulley(blockX);samples.Add(new Sample(clock,blockX,blockX*P("strands"),Instrument<VlabForceMeter>("meter").ForceN));finished=blockX>=P("height")-1e-8;return true;
                    case "belt-span":if(begun)return false;beltIndex=(beltIndex+1)%3;ConfigureBelt();return true;
                    case "follower-type":if(begun)return false;bool next=!Roller;rollerSelected=next;equipment["roller"].gameObject.SetActive(next);equipment["follower"].gameObject.SetActive(!next);var cam=Instrument<VlabCam>("cam");var f=Instrument<VlabFollower>(next?"roller":"follower");f.Follow(cam);ZeroCamGauge();return true;
                    case "sample":if(begun)return false;sampleIndex=(sampleIndex+1)%3;CloseTool();return true;
                    case "zero":if(begun)return false;CloseTool();zeroReading=P("zero");zeroed=true;return true;
                    case "contact":return MakeContact(out message);
                    case "unload":if(!begun)return false;ConfigureBeam(0);unloaded=Math.Abs(Instrument<VlabDialGauge>("gauge").ReadingMm-beamZero)<.011;return true;
                    default:return base.Operate(action,out message);
                }
            }
            catch(Exception e){message=e.Message;return false;}
        }
        public override bool Validate(out string message)
        {
            if(!Require(AllInstalled&&configured,"Install and configure every delivered instrument.",out message))return false;
            if(!ParameterLimits(out message))return false;
            if(cart&&!Require(cart.UnitFrame,"Cart requires an independent unit-scale travel frame.",out message))return false;
            return true;
        }
        public override string SaveState()=>JsonUtility.ToJson(new Draft{sample=sampleIndex,pivot=pivotMark,belt=beltIndex,roller=rollerSelected});
        public override void RestoreState(string json)
        {
            if(string.IsNullOrEmpty(json))return;var d=JsonUtility.FromJson<Draft>(json);sampleIndex=Math.Max(0,Math.Min(2,d.sample));pivotMark=Math.Max(-1,Math.Min(1,d.pivot));beltIndex=Math.Max(0,Math.Min(2,d.belt));rollerSelected=d.roller;
            // Safe draft only: a saved running mechanism is always reinstalled/prepared from rest.
            begun=false;prepared=0;paused=true;finished=false;SetPaused(true);
        }
    }
}
