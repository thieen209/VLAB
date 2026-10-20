using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Vlab.Product
{
    // Delivered instruments own apparatus state; rates/images remain explicitly illustrative.
    public sealed class BiologyDriver : LessonDriver
    {
        BiologyLessonState s; readonly KitItemSpec[] kit; Transform coverFrame;
        Vector2 b01ViewCenter=new Vector2(.5f,.5f);Vector2? b01SelectedUv;
        readonly Dictionary<string,Vector3> rest=new Dictionary<string,Vector3>();
        ScientificFixtureSupport filtrationSupport;
        static readonly string[] Stages={"interphase","prophase","metaphase","anaphase","telophase","unclear"};
        static readonly string[] StageVi={"Kỳ trung gian","Kỳ đầu","Kỳ giữa","Kỳ sau","Kỳ cuối","Chưa rõ"};
        static readonly string[] Structures={"cell_wall","plasma_membrane","cytoplasm","central_vacuole","nucleus","nucleolus"};
        static readonly string[] StructureVi={"Thành tế bào","Màng sinh chất","Tế bào chất","Không bào trung tâm","Nhân","Hạch nhân"};
        string Id=>session.spec.id;
        bool Running=>session.Phase==ExperimentPhase.Running&&!s.paused;
        bool Fresh=>session.Phase==ExperimentPhase.Preparing||session.Phase==ExperimentPhase.Ready;
        bool Has(string key)=>equipment!=null&&equipment.ContainsKey(key);
        bool On(string key)=>Has(key)&&equipment[key].installed;
        double V(string key,double fallback=0)=>session.parameters.TryGetValue(key,out var value)?value:fallback;
        public BiologyDriver(ExperimentSession session):base(session){s=NewState();kit=MakeKit();}
        BiologyLessonState NewState()=>new BiologyLessonState{lessonId=Id,groupId=Id+"-"+Guid.NewGuid().ToString("N").Substring(0,10)};
        public override KitItemSpec[] Kit=>kit;
        KitItemSpec Item(string key,string prefab,string vi,string en,float x,float z)=>new KitItemSpec(key,prefab,vi,en,new Vector3(x,0,z));
        KitItemSpec[] MakeKit()
        {
            var k=new List<KitItemSpec>();
            if(Id=="B01"||Id=="B02"||Id=="B08")
            {
                k.Add(Item("microscope","microscope","Kính hiển vi","Microscope",0,0));
                k.Add(Item("slide","slide","Lam kính","Slide",-.25f,-.20f));
                k.Add(Item("coverslip","coverslip","Lamen","Coverslip",-.15f,-.24f));
                k.Add(Item("specimen",Id=="B01"?"specimen_onion":Id=="B02"?"specimen_plasmolysis":"specimen_mitosis","Tiêu bản sơ đồ","Schematic specimen",.32f,.03f));
                if(Id!="B08")
                {
                    k.Add(Item("onion","onion","Mẫu hành","Onion sample",-.35f,.08f));
                    k.Add(Item("dropper","dropper","Ống nhỏ giọt","Dropper",-.35f,-.10f));
                    k.Add(Item("paper","filter_paper","Giấy thấm","Blotting paper",.27f,-.20f));
                    k.Add(Item("forceps","forceps","Kẹp gắp","Forceps",-.23f,-.32f));
                }
            }
            else if(Id=="B03")
            {
                k.Add(Item("balance","balance","Cân","Balance",-.28f,.06f));
                k.Add(Item("solution","beaker","Nhóm dung dịch sucrose","Sucrose group",.12f,.08f));
                k.Add(Item("water","beaker","Nước đối chứng","Water reference",.38f,.08f));
                for(int i=0;i<3;i++)k.Add(Item("sample"+i,"potato_sample","Mẫu khoai độc lập "+(i+1),"Independent potato "+(i+1),-.1f+i*.12f,-.22f));
                k.Add(Item("paper","filter_paper","Giấy thấm","Blotting paper",.38f,-.22f));
                k.Add(Item("ruler","ruler","Thước","Ruler",-.28f,-.30f));
                k.Add(Item("timer","timer","Đồng hồ","Timer",.34f,.26f));
            }
            else if(Id=="B04"||Id=="B05")
            {
                k.Add(Item("bath","water_bath","Bể ổn nhiệt","Water bath",-.25f,.08f));
                // The delivered bath's validated seat accepts the native 250 mL beaker exterior.
                // Vessel capacity does not change the deliberately prepared 30 mL reaction aliquot.
                k.Add(Item("aliquot","beaker","Aliquot 30 mL trong cốc 250 mL","30 mL aliquot in 250 mL beaker",-.1f,-.23f));
                k.Add(Item("flask","flask","Bình phản ứng","Reaction flask",.13f,.06f));
                k.Add(Item("stopper","stopper_25_1","Nút một cổng","Single port stopper",.13f,-.16f));
                k.Add(Item("tube","tubing","Đường thu khí","Gas route",.28f,.07f));
                k.Add(Item("syringe","gas_syringe","Syringe khí","Gas syringe",.43f,.04f));
                k.Add(Item("timer","timer","Đồng hồ","Timer",.4f,-.22f));
                k.Add(Item(Id=="B05"?"yeast":"potato",Id=="B05"?"yeast":"potato_sample",Id=="B05"?"Nấm men":"Mẫu khoai enzyme",Id=="B05"?"Yeast":"Potato enzyme",-.35f,-.24f));
            }
            else
            {
                k.Add(Item("mortar","mortar","Cối","Mortar",-.30f,.06f));
                k.Add(Item("pestle","pestle","Chày","Pestle",-.30f,-.20f));
                k.Add(Item("tissue",Id=="B06"?"leaf":"fruit",Id=="B06"?"Lá":"Mô quả",Id=="B06"?"Leaf":"Fruit tissue",-.40f,.25f));
                k.Add(Item("extract","beaker","Dịch chiết","Extract",-.02f,-.12f));
                k.Add(Item("receiver","beaker","Dịch lọc giữ lại","Retained filtrate",.17f,.10f));
                k.Add(Item("funnel","funnel","Phễu","Funnel",.15f,-.12f));
                k.Add(Item("filter","filter_paper_FoldedCone","Giấy lọc gấp","Folded filter",.31f,-.15f));
                if(Id=="B06")
                {
                    k.Add(Item("capillary","capillary","Ống mao dẫn","Capillary",-.04f,-.30f));
                    k.Add(Item("strip","chromatography_paper","Giấy sắc ký","Chromatography paper",.36f,.05f));
                    k.Add(Item("jar","jar","Bình triển khai","Developing jar",.43f,.26f));
                    k.Add(Item("ruler","ruler","Thước","Ruler",-.35f,-.33f));
                }
                else
                {
                    k.Add(Item("alcohol","reagent_bottle","Alcohol lạnh","Cold alcohol",.38f,.27f));
                    k.Add(Item("layer","liquid_beaker250","Lớp alcohol định tính","Qualitative alcohol layer",.45f,.12f));
                    k.Add(Item("rod","stir_rod","Đũa cuốn nhẹ","Gentle spooling rod",.37f,-.26f));
                    k.Add(Item("dna","dna_precipitate","Kết tủa thô","Crude precipitate",.43f,-.1f));
                }
            }
            return k.ToArray();
        }
        public override void Bind(IReadOnlyDictionary<string,EquipmentHandle> items,Vector3 at)
        {base.Bind(items,at);foreach(var pair in items)rest[pair.Key]=pair.Value.transform.position;}
        Texture2D Image(string suffix)
        {
            var r=equipment["specimen"].GetComponentsInChildren<Renderer>(true).FirstOrDefault(x=>x.name.EndsWith(suffix,StringComparison.Ordinal));
            if(!r)return null;foreach(var m in r.sharedMaterials)if(m&&m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap") is Texture2D t)return t;
            return r.sharedMaterial?r.sharedMaterial.mainTexture as Texture2D:null;
        }
        void Check(bool ok,string failure){if(!ok)throw new InvalidOperationException(failure);}
        public override void Installed(string key)
        {
            rest[key]=equipment[key].transform.position;
            if(key=="dna")Instrument<VlabDnaPrecipitate>(key).SetPresent(false);
            if(key=="layer")Check(Instrument<VlabLiquid>(key).TrySetVolume(0),"Alcohol layer reset failed.");
            if(key=="syringe")Check(Instrument<VlabGasSyringe>(key).SetVolume(0),"Gas syringe could not reset.");
            if(key=="microscope"){var m=Instrument<VlabMicroscope>(key);Check(m.TrySetClips(true)&&m.TrySetObjective(0)&&m.TrySetFocus(6.5f),"Microscope setup failed.");}
            if(key=="onion")Check(Instrument<VlabOnion>(key).TrySetState(0),"Onion preparation state failed.");
            if(key=="strip")Paper.ClearTrial();
        }
        void MountSlide()
        {
            var m=Instrument<VlabMicroscope>("microscope");Check(m.TrySetClips(true),"Open clips first.");
            equipment["slide"].transform.SetParent(m.Stage,true);Fit("slide","_Base_SOCKET",Socket("microscope","_SlideCenter_SOCKET"));
            LowerCover();
            Check(m.TrySetClips(false),"Both microscope clips must close.");s.mounted=true;ShowView();
        }
        void LowerCover()
        {
            if(!coverFrame){coverFrame=new GameObject("Coverslip lowering frame").transform;coverFrame.SetParent(equipment["slide"].transform,true);}
            coverFrame.SetPositionAndRotation(Socket("slide","_CoverBase_SOCKET").position,Quaternion.identity);
            var cover=Instrument<VlabCoverslip>("coverslip");cover.transform.SetParent(coverFrame,false);cover.transform.localScale=Vector3.one;
            // The SDK's first Start resets this local hinge pose; enable only in its real slide frame.
            cover.enabled=true;
            Check(cover.SetAngle(35)&&cover.SetAngle(0),"Coverslip lowering failed.");s.lowered=true;
        }
        void ShowView()
        {
            if(!s.mounted)return;var image=Id=="B02"?Instrument<VlabPlasmolysis>("specimen").CurrentTexture:Image(Id=="B08"?"_Atlas_VIS":"_Texture_VIS");
            Check(image&&Instrument<VlabMicroscope>("microscope").TrySetEyepieceImage(image),"Source microscope image unavailable.");
        }
        void Fill(string key,double ml){var b=Instrument<VlabBeaker>(key);b.Remove(b.VolumeMl);Check(Math.Abs(b.Add(ml)-ml)<1e-8,"Insufficient beaker capacity.");}
        public override bool Prepare(int n,out string message)
        {
            message="";if(!Validate(out message))return false;
            try
            {
                if(Id=="B01")switch(n)
                {
                    case 1: Check(Instrument<VlabOnion>("onion").TrySetState(2),"Separate one epidermis layer.");s.wet=true;
                        Check(Instrument<VlabOnion>("onion").TryConfigureSample(s.groupId,Image("_Texture_VIS")),"Onion image mapping failed.");break;
                    case 2: Check(s.wet,"Prepare a wet layer first.");LowerCover();break;
                    case 3: Check(s.lowered,"Lower and blot the coverslip first.");MountSlide();break;
                    case 4: Check(s.objective==0&&Sharpness()>=.7,"Focus at actual 4× objective first using microscope controls.");break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                else if(Id=="B02")switch(n)
                {
                    case 1:s.wet=s.lowered=true;MountSlide();Check(Instrument<VlabPlasmolysis>("specimen").TrySetState(0),"Water view unavailable.");ShowView();break;
                    case 2:Check(s.cells.Count>=20&&s.cells.Count<=30&&s.cells.All(c=>c.baseline>=0),"Mark 20–30 actual source IDs and classify baseline before locking the field.");s.baselineLocked=true;break;
                    case 3:Check(s.baselineLocked,"Lock baseline field first.");s.saltAdded=true;s.saltAt=s.clock;Check(Instrument<VlabPlasmolysis>("specimen").TrySetState(V("salt")>0?1:0),"Salt view unavailable.");ShowView();break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                else if(Id=="B03")switch(n)
                {
                    case 1:EnsureSamples();Check(s.samples.All(x=>x.initialRead),"Read three independent initial masses using the sample balance controls.");break;
                    case 2:Fill("solution",100);Fill("water",100);break;
                    case 3:Check(s.samples.All(x=>x.initialRead),"Initial sample masses missing.");for(int i=0;i<3;i++){s.samples[i].soakStart=s.clock;PlaceSampleInSolution(i);}break;
                    case 4:Check(V("blot")>.5,"Choose the shared blotting protocol; actual blotting occurs after soaking.");break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                else if(Id=="B04"||Id=="B05")switch(n)
                {
                    case 1:Fill("aliquot",30);s.inactive=V("inactive")>.5;if(Id=="B05")Check(Instrument<VlabYeast>("yeast").TryConfigureSample(s.groupId),"Yeast identity failed.");break;
                    case 2:var bath=Instrument<VlabWaterBath>("bath");Check(bath.TrySetLidOpen(true),"Open bath lid.");Check(bath.Sample||bath.TryMountSample(Instrument<VlabBeaker>("aliquot")),"Beaker does not fit bath at native scale.");s.sampleC=25;s.bathC=V("temperature");Check(bath.TrySetBathC((float)s.bathC)&&bath.TrySetSampleC(25)&&bath.TrySetHeater(true),"Bath state rejected.");break;
                    case 3:SealRoute();break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                else if(Id=="B06")switch(n)
                {
                    case 1:s.ground=true;Fill("extract",10);break;
                    case 2:FilterExtract();break;
                    case 3:Check(Paper.Spot.AppliedUl>0&&!Paper.Spot.Wet,"Load capillary, deposit ≤1 µL and confirm dry first.");break;
                    case 4:SeatStrip();break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                else if(Id=="B07")switch(n)
                {
                    case 1:var fruit=Instrument<VlabFruit>("tissue");Check(fruit.ConfigureSample(s.groupId,10)&&fruit.SetCrushed(true),"Fruit preparation failed.");s.ground=true;break;
                    case 2:Check(s.ground,"Mash tissue first.");Fill("extract",20);s.bufferAdded=true;break;
                    case 3:FilterExtract();break;
                    case 4:LayerAlcohol();break;
                    case 5:Check(s.filtered&&(s.alcoholAdded||s.noAlcohol),"Select alcohol or its omitted-step control.");break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                else if(Id=="B08")switch(n)
                {
                    case 1:MountSlide();break;
                    case 2:case 3:Check(s.mounted,"Mount slide before choosing a systematic counting protocol.");break;
                    default:return Require(false,"Unknown preparation operation.",out message);
                }
                s.prep=Math.Max(s.prep,n);Configure();return true;
            }
            catch(Exception e){message=e.Message;return false;}
        }
        void EnsureSamples()
        {
            if(s.samples.Count==3)return;s.samples.Clear();
            for(int i=0;i<3;i++)
            {
                var sample=new BiologySample{id=s.groupId+"-P"+(i+1),initial=V("mass"),concentration=V("concentration")};
                Check(Instrument<VlabPotatoSample>("sample"+i).TryConfigure(sample.id,sample.initial),"Potato identity/mass rejected.");s.samples.Add(sample);
            }
        }
        void PlaceSampleInSolution(int i)
        {
            var t=equipment["sample"+i].transform;var p=Socket("solution","_InteriorBase_SOCKET").position;
            t.position+=p+new Vector3((i-1)*.012f,.004f,0)-Socket("sample"+i,"_CoreBase_SOCKET").position;
            Instrument<VlabMassSample>("sample"+i).IsHeld=false;
        }
        void FilterExtract()
        {
            Check(s.ground,"Prepare tissue before filtering.");
            var from=Instrument<VlabBeaker>("extract");var to=Instrument<VlabBeaker>("receiver");double extractMl=from.VolumeMl;
            Check(extractMl>0&&to.CapacityMl-to.VolumeMl+1e-8>=extractMl,"Retain enough empty receiver capacity before filtering; no extract has been transferred.");
            filtrationSupport=ScientificFixtureSupport.Get(equipment["funnel"].transform.parent,Id+"-filtration");
            filtrationSupport.SeatFunnel(Instrument<VlabBeaker>("receiver"),Instrument<VlabFunnel>("funnel"),Instrument<VlabFilterPaper>("filter"));
            Check(ValidateScientificFixtures(out var message),message);
            Check(filtrationSupport.ValidateProspectiveVolume(to.VolumeMl+extractMl,out message),message);
            Check(Instrument<VlabFilterPaper>("filter").SetWet(true),"Filter wetting failed.");
            // Every capacity/support/clearance check completes BEFORE the only volume mutation.
            // SDK transfer is synchronous and exact after this preflight; no post-transfer failure gate.
            from.TransferTo(to,extractMl);
            s.filtered=true;if(Id=="B06")s.extracted=true;
        }
        public bool ValidateScientificFixtures(out string message)
        {
            message="";if(Id!="B06"&&Id!="B07")return true;
            if(!filtrationSupport){message="Prepare the supported funnel over its actual receiver first.";return false;}
            if(!filtrationSupport.Validate(out message))return false;
            if(equipment["filter"].transform.parent!=Instrument<VlabFunnel>("funnel").transform){message="Folded filter is not seated in the source funnel.";return false;}
            return true;
        }
        VlabChromatography Paper=>equipment["strip"].GetComponentsInChildren<VlabChromatography>(true).Single(x=>x.Phase=="Cellulose paper");
        void LayerAlcohol()
        {
            var receiver=Instrument<VlabBeaker>("receiver");Check(s.filtered&&receiver.VolumeMl>0,"Retain filtrate first.");s.noAlcohol=V("alcohol",1)<.5;
            var layer=Instrument<VlabLiquid>("layer");Check(layer.TrySetVolume(0),"Clear previous layer first.");
            if(!s.noAlcohol)
            {
                var bottle=Instrument<VlabReagentBottle>("alcohol");
                Check(bottle.TryConfigure("Cold alcohol (scenario)",1,"qualitative")&&bottle.TrySetCapOpen(true)&&bottle.TrySetVolume(20),"Prepare labelled alcohol reservoir.");
                // Two supplied, unscaled volume assets provide the interface; no custom mesh/material or molecular inference.
                equipment["layer"].transform.SetPositionAndRotation(equipment["receiver"].transform.position+Vector3.up*(float)(receiver.SurfaceHeightMeters-.003),equipment["receiver"].transform.rotation);
                Check(layer.TrySetVolume(10)&&bottle.TrySetVolume(10),"Slow layering transfer failed.");
                Check(bottle.TrySetCapOpen(false),"Close alcohol reservoir.");
            }
            s.alcoholAdded=!s.noAlcohol;s.reactionAt=s.clock;
        }
        void SeatStrip()
        {
            Check(V("submerged")<.5,"Sample origin submerged; use fresh paper above solvent.");
            var jar=Instrument<VlabJar>("jar");var strip=Paper;
            Check(jar.SetLid(true)&&jar.SetSolventSystem(V("solvent")>.5?"Illustrative system B":"Illustrative system A"),"Jar setup failed.");
            jar.Liquid.Remove(jar.Liquid.VolumeMl);jar.Liquid.Add(2);
            Check(jar.TrySeatPaper(strip)&&jar.SetLid(false)&&jar.ReadyToDevelop(strip),"Keep solvent below origin, seat paper and close jar.");s.seated=true;
        }
        void SealRoute()
        {
            var tube=Instrument<VlabTubing>("tube");Check(tube.TrySetSpan(.16f,.12f),"Gas route bend invalid.");
            Fit("stopper","_Seat_SOCKET",Socket("flask","_NeckSocket_SOCKET"));
            var a=Socket("tube","_GasA_SOCKET");var b=Socket("tube","_GasB_SOCKET");
            equipment["tube"].transform.position+=Socket("stopper","_Port1Top_SOCKET").position-a.position;
            equipment["syringe"].transform.position+=b.position-Socket("syringe","_Inlet_SOCKET").position;
            Check(tube.TrySetPorts(s.groupId+"/flask",s.groupId+"/syringe",true,true)&&tube.LogicalEdgeEnabled,"Gas route seals failed.");
            Check(Instrument<VlabFlask>("flask").TrySetVolume(0),"Reaction flask volume reset failed.");
        }
        public override void Configure()
        {
            if(equipment==null)return;
            if(Has("microscope")&&On("microscope"))
            {
                var m=Instrument<VlabMicroscope>("microscope");s.focus=(float)V("focus",s.focus);
                double objective=V("objective",s.objective==0?4:s.objective==1?10:40);s.objective=objective>=25?2:objective>=7?1:0;
                if(session.parameters.ContainsKey("objective"))session.parameters["objective"]=s.objective==0?4:s.objective==1?10:40;
                if(Id=="B01"&&m.ObjectiveIndex!=s.objective&&b01SelectedUv.HasValue)b01ViewCenter=b01SelectedUv.Value;
                Check(m.TrySetFocus(s.focus)&&m.TrySetObjective(s.objective),"Microscope mechanical setting rejected.");ShowView();
            }
            if(Id=="B02"&&session.parameters.ContainsKey("water"))session.parameters["water"]=s.waterAdded?1:0;
            if(Id=="B03"&&equipment.Values.All(x=>x.installed))
            {
                // Changing a group variable invalidates its baseline measurements; a sample never silently changes concentration mid-soak.
                if(Fresh&&s.samples.Any(x=>Math.Abs(x.concentration-V("concentration"))>1e-8||Math.Abs(Instrument<VlabPotatoSample>("sample"+s.samples.IndexOf(x)).InitialMassGrams-V("mass"))>1e-8))
                {if(s.samples.Any(x=>x.soakStart>=0)){session.Invalidate("Concentration or initial mass changed after immersion; prepare a fresh group.");return;}s.samples.Clear();}
                EnsureSamples();
            }
            if((Id=="B04"||Id=="B05")&&s.prep>=2){s.bathC=V("temperature");Instrument<VlabWaterBath>("bath").TrySetBathC((float)s.bathC);}
            if(Id=="B07"&&On("dna"))Instrument<VlabDnaPrecipitate>("dna").SetPresent(CrudePresent());
        }
        double Sharpness()=>Math.Exp(-Math.Pow((s.focus-6.5)/1.2,2));
        public override bool CanEditParameter(string key)=>Id=="B01"&&(key=="focus"||key=="objective")||Id=="B08"&&key=="field";
        public override bool CanStart(out string message)
        {
            if(!Validate(out message))return false;
            if(Id=="B01"||Id=="B02"||Id=="B08")return Require(s.mounted&&!Instrument<VlabMicroscope>("microscope").ClipsOpen&&Sharpness()>=.7,"Mount and focus slide with both clips closed.",out message);
            if(Id=="B03")return Require(s.samples.Count==3&&s.samples.All(x=>x.initialRead&&x.soakStart>=0),"Prepare three independently identified and initially weighed samples.",out message);
            if(Id=="B04"||Id=="B05")return Require(s.prep>=3&&Instrument<VlabTubing>("tube").LogicalEdgeEnabled,"Prepare aliquot, mount bath and seal syringe route.",out message);
            if(Id=="B06")return Require(s.seated&&Instrument<VlabJar>("jar").ReadyToDevelop(Paper)&&ValidateScientificFixtures(out _),"Prepare supported filtration and the sealed development jar.",out message);
            return Require(s.filtered&&(s.alcoholAdded||s.noAlcohol)&&ValidateScientificFixtures(out _),"Retain supported filtrate and select alcohol or omitted-alcohol control.",out message);
        }
        public override void SetPaused(bool paused)
        {
            s.paused=paused;
            if(Has("timer")&&On("timer"))
            {var timer=Instrument<VlabTimer>("timer");if(paused&&timer.Running)timer.TryStop(s.clock);else if(!paused&&!timer.Running)timer.TryStart(s.clock);}
            if(Has("bath")&&On("bath"))Instrument<VlabWaterBath>("bath").TrySetHeater(!paused&&s.prep>=2);
        }
        public override void Advance(double seconds)
        {
            if(!Running||seconds<=0||!double.IsFinite(seconds))return;
            if(Id=="B04"||Id=="B05")
            {while(seconds>1e-9){double h=Math.Min(.1,seconds);AdvanceStep(h);seconds-=h;}}
            else AdvanceStep(seconds);
        }
        void AdvanceStep(double seconds)
        {
            s.clock+=seconds;
            if(Has("timer")){var t=Instrument<VlabTimer>("timer");if(!t.Running)t.TryStart(s.clock-seconds);t.TrySample(s.clock);}
            if(Id=="B04"||Id=="B05")
            {
                var bath=Instrument<VlabWaterBath>("bath");
                // The source bath accepts beakers, not this flask. The equilibrated aliquot is transferred at mixing;
                // the flask then cools toward the room rather than pretending it remains inside the bath.
                s.sampleC+=((s.reacting?25:s.bathC)-s.sampleC)*(1-Math.Exp(-seconds/(s.reacting?120:6)));
                if(bath.Sample)bath.TrySetSampleC((float)s.sampleC);
                if(s.sampleC>=70&&Id=="B04")s.thermalDose+=seconds;if(s.thermalDose>=10)s.inactive=true;
                if(s.reacting&&Instrument<VlabTubing>("tube").LogicalEdgeEnabled)
                {
                    double time=s.clock-s.reactionAt,limit,rate,activeSeconds=seconds;
                    if(Id=="B04")
                    {limit=V("peroxide")*.5*24.465;double activity=s.inactive?0:Math.Exp(-Math.Pow((s.sampleC-38)/22,2))*Math.Exp(-Math.Max(0,s.sampleC-55)*.12);rate=.015*V("enzyme")*activity;}
                    else
                    {limit=V("sugar")/180.156*2*24465;double activity=s.inactive?0:Math.Exp(-Math.Pow((s.sampleC-32)/17,2))*Math.Exp(-Math.Max(0,s.sampleC-45)*.15);rate=.006*V("yeast")*activity;activeSeconds=Math.Min(seconds,Math.Max(0,time-15));}
                    s.gasVolume=Math.Min(limit,limit-(limit-s.gasVolume)*Math.Exp(-rate*activeSeconds));
                    var syringe=Instrument<VlabGasSyringe>("syringe");syringe.SetVolume((float)Math.Min(syringe.CapacityMl,s.gasVolume));
                }
            }
            if(Id=="B06"&&!s.frontMarked)
            {
                var strip=Paper;if(!Instrument<VlabJar>("jar").ReadyToDevelop(strip))return;
                s.frontMm=Math.Min(88,V("front",70))*(1-Math.Exp(-s.clock/20));double rf=V("solvent")>.5?.68:.55;
                strip.SetMeasurements(strip.Phase,V("solvent")>.5?"Illustrative B":"Illustrative A",s.frontMm/1000,new[]{s.frontMm*rf/1000,s.frontMm*.85/1000},.002);
            }
            if(Id=="B07")Instrument<VlabDnaPrecipitate>("dna").SetPresent(CrudePresent());
        }
        bool CrudePresent()=>s.ground&&s.bufferAdded&&s.filtered&&s.alcoholAdded&&!s.noAlcohol&&V("detergent")>.5&&V("salt")>.5&&s.clock-s.reactionAt>=8;
        public override Texture Inspection(string key)
        {
            if((key!="microscope"&&key!="specimen")||!s.mounted)return null;
            return Id=="B02"?Instrument<VlabPlasmolysis>("specimen").CurrentTexture:Image(Id=="B08"?"_Atlas_VIS":"_Texture_VIS");
        }
        public bool HasB01Presentation=>Id=="B01"&&On("microscope");
        public int MicroscopeObjectiveIndex=>HasB01Presentation?Instrument<VlabMicroscope>("microscope").ObjectiveIndex:0;
        public float MicroscopeFocusMm=>HasB01Presentation?Instrument<VlabMicroscope>("microscope").FocusMm:6.5f;
        public float MicroscopeFocusQuality=>Mathf.Exp(-Mathf.Pow((MicroscopeFocusMm-6.5f)/1.2f,2));
        // Relative field cropping is illustrative, not a pixel-to-micrometre calibration.
        public static Rect B01InspectionCrop(int objectiveIndex,Vector2 center)
        {
            float field=objectiveIndex<=0?1:objectiveIndex==1?.4f:.1f,half=field*.5f;
            center=new Vector2(Mathf.Clamp(center.x,half,1-half),Mathf.Clamp(center.y,half,1-half));return new Rect(center.x-half,center.y-half,field,field);
        }
        public bool TryB01SourceIdentity(Vector2 uv,out int cell,out int structure)
        {
            cell=structure=0;if(Id!="B01"||!Has("specimen")||uv.x<0||uv.x>=1||uv.y<0||uv.y>=1)return false;
            var mask=Image("_CellMasks_VIS");if(!mask||!mask.isReadable)return false;Color32 value=mask.GetPixel((int)(uv.x*mask.width),(int)(uv.y*mask.height));
            cell=value.r;structure=value.g;return cell>=1&&cell<=64&&structure>=1&&structure<=6;
        }
        public override Rect InspectionUv(string key)
        {if(Id=="B01")return B01InspectionCrop(MicroscopeObjectiveIndex,b01ViewCenter);if(Id!="B08")return new Rect(0,0,1,1);int field=(int)V("field",1);return field==2?new Rect(0,0,.5f,1):field==3?new Rect(.5f,0,.5f,1):new Rect(0,0,1,1);}
        public override bool PointOnInspection(string key,Vector2 uv,out string message)
        {
            message="";if(!s.mounted||Sharpness()<.7)return Require(false,"Focus mounted specimen first.",out message);
            if(Id=="B02")
            {
                if(!Instrument<VlabPlasmolysis>("specimen").TryGetCell(uv,out int id))return Require(false,"Point inside a source cell.",out message);
                s.selectedCell=id;var cell=s.cells.FirstOrDefault(c=>c.id==id);
                if(cell==null){if(s.baselineLocked)return Require(false,"Cell outside locked 20–30-cell field.",out message);if(s.cells.Count>=30)return Require(false,"Track at most 30 cells.",out message);s.cells.Add(new BiologyCellAnswer{id=id});}
                message="Cell "+id+" selected. Classify visible state; selection alone does not count it.";return true;
            }
            if(Id=="B08")
            {
                if(!Instrument<VlabMitosisAtlas>("specimen").TryResolve(uv,out int id,out _,out _))return Require(false,"Point inside a source atlas cell.",out message);
                s.selectedCell=id;var prior=s.cells.FirstOrDefault(c=>c.id==id);message="Global cell "+id+(prior==null?" selected; choose stage or unclear.":" already classified as "+prior.answer+". Choosing again edits this ID without duplicate counting.");return true;
            }
            if(Id=="B01")
            {
                var mask=Image("_CellMasks_VIS");if(!mask||!mask.isReadable)return Require(false,"Source categorical mask unavailable.",out message);
                if(uv.x<0||uv.x>=1||uv.y<0||uv.y>=1)return false;Color32 p=mask.GetPixel((int)(uv.x*mask.width),(int)(uv.y*mask.height));
                if(p.r<1||p.r>64||p.g<1||p.g>6)return Require(false,"Point inside a cell structure.",out message);
                b01SelectedUv=uv;s.selectedCell=p.r;s.selectedStructure=p.g;message="Cell "+p.r+": choose the visible structure in this schematic.";return true;
            }
            return false;
        }
        public override LessonControl[] Controls(string key)
        {
            var c=new List<LessonControl>();
            void Add(string id,string vi,string en,bool enabled=true)=>c.Add(new LessonControl(id,vi,en,enabled));
            if(key=="microscope"||key=="specimen")
            {
                Add("focus-","Lấy nét −0,5 mm","Focus −0.5 mm");Add("focus+","Lấy nét +0,5 mm","Focus +0.5 mm");
                Add("obj4","Vật kính 4×","4× objective");Add("obj10","Vật kính 10×","10× objective");Add("obj40","Vật kính 40×","40× objective");
                if(Id=="B01")for(int i=0;i<Structures.Length;i++)Add("structure:"+i,StructureVi[i],Structures[i].Replace('_',' '),s.selectedStructure>0);
                if(Id=="B02")
                {
                    Add("cellNormal","Chưa co","Not plasmolysed",s.selectedCell>0);Add("cellContracted","Đã co","Plasmolysed",s.selectedCell>0);
                    Add("removeCell","Bỏ ô đã chọn khỏi nhóm","Remove selected baseline cell",!s.baselineLocked&&s.selectedCell>0);
                    Add("saltView1","Sơ đồ co mức 1","Illustrative salt state 1",s.saltAdded&&!s.waterAdded);
                    Add("saltView2","Sơ đồ co mức 2","Illustrative salt state 2",s.saltAdded&&!s.waterAdded);
                    Add("waterReplace","Thay nước tại mép lamen","Replace salt with water at coverslip edge",Running&&s.saltAdded&&!s.waterAdded&&s.clock-s.saltAt>=V("duration"));
                }
                if(Id=="B08")
                {
                    Add("field1","Toàn bộ atlas","Whole atlas");Add("field2","Nửa trái atlas","Left half");Add("field3","Nửa phải atlas","Right half");
                    for(int i=0;i<Stages.Length;i++)Add("stage:"+i,StageVi[i],Stages[i],s.selectedCell>0);
                    Add("checkStage","Kiểm tra lập luận ô đã chọn","Check selected stage after classification",s.cells.Any(x=>x.id==s.selectedCell&&x.answer!="unclear"));
                }
            }
            if(Id=="B03"&&(key.StartsWith("sample",StringComparison.Ordinal)||key=="balance"||key=="paper"))
            {
                for(int i=0;i<3;i++)
                {Add("weighInitial:"+i,"Cân đầu mẫu "+(i+1),"Read initial sample "+(i+1),Fresh);Add("blot:"+i,"Thấm mẫu "+(i+1),"Blot sample "+(i+1),Running&&s.clock>=V("duration"));Add("weighFinal:"+i,"Cân cuối mẫu "+(i+1),"Read final sample "+(i+1),Running&&s.clock>=V("duration"));}
            }
            if((Id=="B04"||Id=="B05")&&(key=="flask"||key=="bath"||key=="syringe"||key=="timer"))
            {
                Add("mix",Id=="B04"?"Thêm H2O2 và bắt đầu phản ứng":"Trộn nấm men và đường",Id=="B04"?"Add peroxide and start reaction":"Mix yeast and sugar",Running&&!s.reacting&&Math.Abs(s.sampleC-s.bathC)<.5);
                Add("gasRead","Đọc thể tích và ghi mốc","Read syringe and append time point",Running&&s.reacting);
            }
            if(Id=="B06")
            {
                if(key=="capillary"||key=="strip"||key=="receiver")
                {Add("loadCapillary","Hút 1 µL dịch lọc","Load 1 µL filtered extract",Fresh&&s.filtered);Add("deposit","Chấm 1 µL tại gốc","Deposit 1 µL at origin",Fresh&&s.filtered);Add("dry","Xác nhận vết đã khô","Confirm spot is dry",Fresh&&s.filtered);}
                if(key=="strip"||key=="jar"||key=="ruler")Add("markFront","Dừng và đánh dấu mặt trước","Stop and mark solvent front",Running&&s.frontMm>0&&!s.frontMarked);
            }
            if(Id=="B07"&&(key=="rod"||key=="dna"||key=="receiver"))Add("spool","Cuốn nhẹ vật liệu thô","Gently spool crude material",Running&&CrudePresent()&&!s.spooled);
            return c.ToArray();
        }
        public override bool Operate(string id,out string message)
        {
            message="";
            try
            {
                if(id=="focus-"||id=="focus+")
                {
                    s.focus=Mathf.Clamp(s.focus+(id=="focus+"?.5f:-.5f),0,13);
                    if(session.parameters.ContainsKey("focus"))session.parameters["focus"]=s.focus;
                    Check(Instrument<VlabMicroscope>("microscope").TrySetFocus(s.focus),"Focus travel rejected.");return true;
                }
                if(id=="obj4"||id=="obj10"||id=="obj40")
                {
                    int objective=id=="obj4"?0:id=="obj10"?1:2;
                    if(objective>0)Check(s.mounted&&Sharpness()>=.7,"Find and focus mounted specimen at low power first.");
                    Check(Instrument<VlabMicroscope>("microscope").TrySetObjective(objective),"Objective change rejected.");s.objective=objective;
                    if(Id=="B01"&&b01SelectedUv.HasValue)b01ViewCenter=b01SelectedUv.Value;
                    if(session.parameters.ContainsKey("objective"))session.parameters["objective"]=objective==0?4:objective==1?10:40;return true;
                }
                if(id.StartsWith("structure:",StringComparison.Ordinal)&&Id=="B01")
                {
                    int answer=Index(id,6);Check(s.selectedCell>0&&s.selectedStructure>0,"Select actual source structure first.");
                    bool correct=answer+1==s.selectedStructure;
                    s.structures.RemoveAll(x=>x.cell==s.selectedCell&&x.region==s.selectedStructure);s.structures.Add(new BiologyStructureAnswer{cell=s.selectedCell,region=s.selectedStructure,answer=Structures[answer],correct=correct});
                    message=correct?"Structure matches source schematic.":"Reconsider its position and boundary in this schematic; this is not a microscopy-resolution claim.";return true;
                }
                if(Id=="B02")
                {
                    if(id=="cellNormal"||id=="cellContracted")
                    {
                        var cell=s.cells.FirstOrDefault(x=>x.id==s.selectedCell);Check(cell!=null,"Select tracked source cell.");int value=id=="cellContracted"?1:0;
                        if(!s.saltAdded)cell.baseline=value;else if(!s.waterAdded)cell.salt=value;else cell.recovery=value;return true;
                    }
                    if(id=="removeCell"){Check(!s.baselineLocked,"Baseline field locked.");return s.cells.RemoveAll(x=>x.id==s.selectedCell)>0;}
                    if(id=="saltView1"||id=="saltView2"){Check(s.saltAdded&&!s.waterAdded,"Salt exposure must be current.");Check(Instrument<VlabPlasmolysis>("specimen").TrySetState(id=="saltView1"?1:2),"Illustration state failed.");ShowView();return true;}
                    if(id=="waterReplace")
                    {
                        Check(Running&&s.saltAdded&&!s.waterAdded&&s.clock-s.saltAt>=V("duration"),"Observe actual salt interval first.");
                        Check(s.cells.All(x=>x.salt>=0),"Classify same tracked cells under salt before exchanging water.");
                        s.waterAdded=true;s.waterAt=s.clock;if(session.parameters.ContainsKey("water"))session.parameters["water"]=1;
                        Check(Instrument<VlabPlasmolysis>("specimen").TrySetState(3),"Recovery view unavailable.");ShowView();return true;
                    }
                }
                if(Id=="B08")
                {
                    if(id=="field1"||id=="field2"||id=="field3"){session.parameters["field"]=id[id.Length-1]-'0';return true;}
                    if(id.StartsWith("stage:",StringComparison.Ordinal))
                    {
                        int index=Index(id,Stages.Length);Check(s.selectedCell>=1&&s.selectedCell<=100,"Select actual source cell.");
                        var cell=s.cells.FirstOrDefault(c=>c.id==s.selectedCell);if(cell==null){cell=new BiologyCellAnswer{id=s.selectedCell};s.cells.Add(cell);}cell.answer=Stages[index];return true;
                    }
                    if(id=="checkStage")
                    {
                        var cell=s.cells.FirstOrDefault(c=>c.id==s.selectedCell);Check(cell!=null&&cell.answer!="unclear","Classify cell first.");
                        bool scorable=Instrument<VlabMitosisAtlas>("specimen").TryCheck(cell.id,cell.answer,out bool correct);
                        message=!scorable?"Source cell is unscorable; retain unclear and state why.":correct?"Your classification matches source atlas.":"Reconsider chromosome arrangement. Your answer is retained until you edit it.";return true;
                    }
                }
                if(Id=="B03")
                {
                    EnsureSamples();
                    if(id.StartsWith("weighInitial:",StringComparison.Ordinal))
                    {
                        Check(Fresh,"Record initial masses before soaking.");int i=Index(id,3);var sample=s.samples[i];Check(sample.soakStart<0,"Sample already entered soaking interval.");
                        sample.initial=ReadMass(i,false);sample.initialRead=true;return true;
                    }
                    if(id.StartsWith("blot:",StringComparison.Ordinal))
                    {
                        int i=Index(id,3);Check(Running&&s.samples[i].soakStart>=0&&s.clock-s.samples[i].soakStart>=V("duration"),"Wait for this sample's soak duration.");
                        Check(V("blot")>.5,"Use agreed blotting protocol.");s.samples[i].blotted=true;equipment["sample"+i].transform.position=rest["sample"+i];return true;
                    }
                    if(id.StartsWith("weighFinal:",StringComparison.Ordinal))
                    {
                        int i=Index(id,3);Check(Running&&s.samples[i].blotted,"Remove and blot this ID after soaking first.");
                        s.samples[i].final=ReadMass(i,true);s.samples[i].finalRead=true;return true;
                    }
                }
                if(id=="mix"&&(Id=="B04"||Id=="B05"))
                {
                    Check(Running&&!s.reacting&&Math.Abs(s.sampleC-s.bathC)<.5,"Wait for sample temperature to approach bath setting.");Check(Instrument<VlabTubing>("tube").LogicalEdgeEnabled,"Gas path disconnected.");
                    var aliquot=Instrument<VlabBeaker>("aliquot");Check(aliquot.VolumeMl>=30,"The identified aliquot is missing.");
                    Check(Instrument<VlabWaterBath>("bath").TryRemoveSample(),"Remove equilibrated aliquot from bath.");
                    equipment["aliquot"].transform.SetParent(equipment["bath"].transform.parent,true);
                    Check(Instrument<VlabFlask>("flask").TrySetVolume(30),"Reaction transfer rejected.");aliquot.Remove(30);equipment["aliquot"].transform.position=rest["aliquot"];
                    s.reacting=true;s.reactionAt=s.clock;s.gasVolume=0;return true;
                }
                if(id=="gasRead"&&(Id=="B04"||Id=="B05"))
                {
                    Check(Running&&s.reacting,"Start reaction before reading.");Check(s.gas.Count==0||s.clock-s.gas[s.gas.Count-1].seconds>=1,"Separate readings by ≥1 simulated second.");
                    s.gas.Add(new BiologyGasPoint{seconds=s.clock,volumeMl=Instrument<VlabGasSyringe>("syringe").VolumeMl,sampleC=s.sampleC,bathC=s.bathC,sampleId=s.groupId});return true;
                }
                if(Id=="B06")
                {
                    var strip=Paper;var tool=Instrument<VlabCapillary>("capillary");
                    if(id=="loadCapillary"){Check(Fresh&&s.filtered,"Filter extract first.");Check(Math.Abs(tool.LoadFrom(Instrument<VlabBeaker>("receiver"),1)-1)<1e-9,"Capillary load rejected.");return true;}
                    if(id=="deposit")
                    {
                        Check(Fresh&&s.filtered,"Prepare and filter extract first.");var target=strip.Spot;
                        equipment["capillary"].transform.rotation=Quaternion.FromToRotation(tool.Axis,target.Normal)*equipment["capillary"].transform.rotation;
                        equipment["capillary"].transform.position+=target.Center+target.Normal*.00005f-tool.Spot.position;
                        Check(tool.TrySpot(target,1),"Spot must be dry; each deposit ≤1 µL, cumulative ≤2 µL.");return true;
                    }
                    if(id=="dry"){Check(Fresh&&strip.Spot.AppliedUl>0,"Deposit first.");strip.Spot.ConfirmDry();return true;}
                    if(id=="markFront"){Check(Running&&!s.frontMarked&&s.frontMm>0,"Develop paper first.");s.frontMarked=true;return true;}
                }
                if(id=="spool"&&Id=="B07")
                {Check(Running&&CrudePresent()&&!s.spooled,"No crude material available for gentle spooling.");Check(Instrument<VlabDnaPrecipitate>("dna").AttachTo(Socket("rod","_Tip_SOCKET")),"Spooling attachment rejected.");s.spooled=true;return true;}
                return Require(false,"Action unavailable in this lesson.",out message);
            }
            catch(Exception e){message=e.Message;return false;}
        }
        static int Index(string id,int count)
        {if(!int.TryParse(id.Substring(id.IndexOf(':')+1),out int i)||i<0||i>=count)throw new ArgumentException("Invalid apparatus selection.");return i;}
        double ReadMass(int i,bool final)
        {
            var record=s.samples[i];var potato=Instrument<VlabPotatoSample>("sample"+i);var mass=Instrument<VlabMassSample>("sample"+i);
            Check(potato.SampleId==record.id,"Initial/final readings must use same sample ID.");
            if(final)
            {
                double noise=(((session.Seed+i*5)%17)/16.0-.5)*.015;
                double response=(.18*Math.Tanh((.25-record.concentration)*5)+noise)*(1-Math.Exp(-Math.Min(s.clock-record.soakStart,V("duration"))/500));
                Check(potato.TrySetMeasuredMass(record.initial*(1+response)),"Modeled sample mass rejected.");
            }
            var balance=Instrument<VlabBalance>("balance");balance.ClearTare();var pan=Socket("balance","_PanCenter_SOCKET");
            for(int j=0;j<3;j++)if(j!=i)equipment["sample"+j].transform.position=rest["sample"+j];
            mass.IsHeld=false;Physics.SyncTransforms();var b=mass.Shape.bounds;mass.transform.position+=pan.position-new Vector3(b.center.x,b.min.y,b.center.z);
            Physics.SyncTransforms();Check(balance.Supports(mass),"Release sample squarely on actual pan.");balance.RefreshReading();double read=balance.ReadingGrams;Check(read>0,"Initial mass must be positive.");
            equipment["sample"+i].transform.position=rest["sample"+i];Physics.SyncTransforms();balance.RefreshReading();return read;
        }
        public override ModelResult Observe()
        {
            var r=new ModelResult();
            if(Id=="B01")
            {
                r.Add("objective",s.objective==0?4:s.objective==1?10:40,"×","Vật kính","Objective").Add("magnification",s.objective==0?40:s.objective==1?100:400,"×","Phóng đại danh nghĩa","Nominal magnification")
                 .Add("focusQuality",Sharpness(),"","Độ nét mô hình","Model focus quality").Add("identifiedStructures",s.structures.Count,"","Cấu trúc đã phân loại","Classified structures");
                r.note="64-cell bulb-epidermis schematic. Nominal objectives do not calibrate this image in micrometres. No real micrograph or chloroplast inference.";
            }
            else if(Id=="B02")
            {
                int n=s.cells.Count,baseline=s.cells.Count(x=>x.baseline==1),salt=s.cells.Count(x=>x.salt==1),recovery=s.cells.Count(x=>x.recovery==1);
                r.Add("trackedCells",n,"","Tế bào có mã đã chọn","Selected source cell IDs").Add("baselineCount",baseline,"","Số co ban đầu do người học đếm","Learner baseline count").Add("saltCount",salt,"","Số co trong muối do người học đếm","Learner salt count").Add("recoveryCount",recovery,"","Số còn co sau thay nước","Learner count after water")
                 .Add("saltExposure",s.saltAdded?(s.waterAdded?s.waterAt:s.clock)-s.saltAt:0,"s","Thời gian tiếp xúc muối","Actual salt exposure")
                 .Add("recoveryTime",s.waterAdded?s.clock-s.waterAt:0,"s","Thời gian từ lúc thay nước","Time since actual water replacement");
                if(n>0)r.Add("plasmolysisPercent",100.0*(s.waterAdded?recovery:salt)/n,"%","Tỷ lệ co từ số đếm","Plasmolysis from learner counts");
                r.note="Same stable source IDs, manually classified. Four source views are untimed illustrative states; elapsed times do not validate biological kinetics.";
            }
            else if(Id=="B03")
            {
                var done=s.samples.Where(x=>x.finalRead).ToArray();r.Add("independentSamples",s.samples.Count,"","Mẫu độc lập","Independent samples").Add("completedSamples",done.Length,"","Mẫu đã cân hai lần","Paired independent samples");
                foreach(var sample in s.samples){int i=s.samples.IndexOf(sample)+1;if(sample.initialRead)r.Add("initialMass"+i,sample.initial,"g","Khối lượng đầu mẫu "+i,"Sample "+i+" initial mass");if(sample.finalRead)r.Add("finalMass"+i,sample.final,"g","Khối lượng cuối mẫu "+i,"Sample "+i+" final mass").Add("massChange"+i,100*(sample.final-sample.initial)/sample.initial,"%","Biến đổi mẫu "+i,"Sample "+i+" mass change");}
                if(done.Length>0){var changes=done.Select(x=>100*(x.final-x.initial)/x.initial).ToArray();double mean=changes.Average();r.Add("meanMassChange",mean,"%","Thay đổi trung bình","Mean mass change");if(done.Length>1)r.Add("sampleSd",Math.Sqrt(changes.Sum(x=>(x-mean)*(x-mean))/(done.Length-1)),"%","Độ lệch chuẩn mẫu","Sample standard deviation");}
                r.note="Seeded illustrative osmosis response read through actual balance support. Compare fresh concentration groups and a water group in notebook; three reads of one ID are not biological replication.";
            }
            else if(Id=="B04"||Id=="B05")
            {
                double volume=Has("syringe")&&On("syringe")?Instrument<VlabGasSyringe>("syringe").VolumeMl:0;
                r.Add("gasVolume",volume,"mL",Id=="B04"?"Thể tích O2 mô hình":"Thể tích CO2 mô hình",Id=="B04"?"Modeled oxygen volume":"Modeled carbon dioxide volume")
                 .Add("bathTemperature",s.bathC,"°C","Nhiệt độ bể","Bath temperature").Add("sampleTemperature",s.sampleC,"°C","Nhiệt độ mẫu","Sample temperature").Add("inactiveControl",s.inactive?1:0,"","Mẫu bất hoạt","Inactivated sample")
                 .Add("reactionTime",s.reacting?s.clock-s.reactionAt:0,"s","Thời gian từ lúc trộn","Time since actual mixing").Add("timePoints",s.gas.Count,"","Mốc do người học đọc","Learner time points");
                if(s.gas.Count>=2){var a=s.gas[s.gas.Count-2];var b=s.gas[s.gas.Count-1];r.Add("intervalRate",(b.volumeMl-a.volumeMl)/(b.seconds-a.seconds),"mL/s","Tốc độ ΔV/Δt đã đo","Measured interval ΔV/Δt");}
                r.note="Illustrative finite-substrate kinetics. Equilibrated beaker aliquot transfers to flask at mixing; its modeled temperature then cools toward room (25 °C, 120 s time constant). Bath temperature is distinct. "+(Id=="B05"?"15 s illustrative lag; inactivated yeast differs from zero yeast. Zero-sugar baseline excludes endogenous reserves. Gas identity/fermentation is an assumption, not measured assay.":"Exposure ≥70 °C for 10 simulated seconds irreversibly inactivates this configured illustrative enzyme model.");
                if(s.gasVolume>=100)r.note+=" Syringe capacity reached; later production is not a readable volume.";
            }
            else if(Id=="B06")
            {
                var strip=Paper;r.Add("solventFront",s.frontMm,"mm","Quãng mặt trước từ gốc","Solvent travel from origin").Add("appliedUl",strip.Spot.AppliedUl,"µL","Tổng lượng chấm","Cumulative spot volume");
                if(strip.HasMeasurement)for(int i=0;i<strip.BandCount;i++)r.Add("rf"+(i+1),strip.Rf(i),"","Rf băng "+(i+1),"Band "+(i+1)+" Rf");
                r.note="Explicit illustrative A/B band dataset on cellulose paper; system labels are educational scenarios, not validated compositions or pigment identification. Front limited to 88 mm.";
            }
            else if(Id=="B07")
            {
                r.Add("crudePrecipitate",CrudePresent()?1:0,"presence","Vật liệu kết tủa thô","Crude precipitate present").Add("alcoholIncluded",s.alcoholAdded?1:0,"presence","Có lớp alcohol","Alcohol layered").Add("filtrateRetained",s.filtered?1:0,"presence","Giữ dịch lọc","Filtrate retained").Add("gentlySpooled",s.spooled?1:0,"presence","Đã cuốn nhẹ","Gently spooled");
                r.note="Qualitative crude material may contain DNA and contaminants; no mass, purity, concentration, molecular length or individual helix is inferred. Omitted alcohol prevents visible strands in this illustration.";
            }
            else if(Id=="B08")
            {
                int total=s.cells.Count,unclear=s.cells.Count(x=>x.answer=="unclear"),mitotic=s.cells.Count(x=>x.answer!="interphase"&&x.answer!="unclear");
                r.Add("totalCells",total,"","Tế bào riêng biệt đã phân loại","Distinct manually classified cells");
                for(int i=0;i<Stages.Length;i++)r.Add(Stages[i],s.cells.Count(x=>x.answer==Stages[i]),"",StageVi[i],Stages[i]);
                if(total>0)r.Add("mitoticIndex",100.0*mitotic/total,"%","MI từ số đếm người học","MI from learner counts").Add("miUpperWithUnclear",100.0*(mitotic+unclear)/total,"%","MI tối đa nếu nhóm chưa rõ đang phân bào","Upper MI if unclear cells are mitotic");
                r.note="100-ID explanatory schematic atlas, not micrograph or biological phase-frequency sample. Global IDs deduplicate overlapping crops. MI uses learner answers; unclear remains in denominator and uncertainty bound.";
            }
            r.detailJson=JsonUtility.ToJson(s);return r;
        }
        public override bool CanRecord(out string message)
        {
            if(session.Phase!=ExperimentPhase.Running&&session.Phase!=ExperimentPhase.Paused)return Require(false,"Run prepared experiment before recording.",out message);
            if(Id=="B01")return Require(Sharpness()>=.7&&s.structures.Count>=3,"Focus image and identify ≥3 source structures before recording.",out message);
            if(Id=="B02")return Require(s.waterAdded&&s.clock-s.waterAt>=10&&s.cells.Count>=20&&s.cells.All(x=>x.baseline>=0&&x.salt>=0&&x.recovery>=0),"Classify same 20–30 IDs at baseline, salt and recovery; observe ≥10 s after actual water replacement.",out message);
            if(Id=="B03")return Require(s.samples.Count==3&&s.samples.All(x=>x.initialRead&&x.finalRead&&x.blotted),"Record paired initial/final readings for three independent blotted samples.",out message);
            if(Id=="B04"||Id=="B05")return Require(s.reacting&&s.clock-s.reactionAt>=30&&s.gas.Count>=3&&s.gas.Last().seconds-s.gas.First().seconds>=20,"Collect ≥3 manual syringe readings spanning ≥20 s; observe ≥30 s after mixing.",out message);
            if(Id=="B06")return Require(s.frontMarked&&s.clock>=30&&Paper.HasMeasurement,"Develop ≥30 s, then stop and mark actual front.",out message);
            if(Id=="B07")return Require(s.clock-s.reactionAt>=10&&s.filtered&&(!CrudePresent()||s.spooled),"Observe retained filtrate ≥10 s; gently spool any crude material present.",out message);
            return Require(s.cells.Count>=100&&s.cells.Select(x=>x.id).Distinct().Count()==s.cells.Count&&s.cells.All(x=>Stages.Contains(x.answer)),"Manually classify all 100 distinct source IDs across crops; use unclear when appropriate.",out message);
        }
        public override bool TryMeasure(out ModelResult result,out string message)
        {result=null;if(!CanRecord(out message))return false;result=Observe();return true;}
        public override string SaveState()=>JsonUtility.ToJson(s);
        public override void RestoreState(string json)
        {
            // Only fixed-atlas classifications resume. Reaction clocks, sample masses and route seals restart fresh.
            if(string.IsNullOrWhiteSpace(json)||json.Length>150000)return;BiologyLessonState saved;
            try{saved=JsonUtility.FromJson<BiologyLessonState>(json);}catch{return;}
            if(saved==null||saved.version!=1||saved.lessonId!=Id)return;var fresh=NewState();
            if(Id=="B08"&&saved.cells!=null)
                foreach(var cell in saved.cells.Take(101))
                    if(cell!=null&&cell.id>=1&&cell.id<=100&&Stages.Contains(cell.answer)&&!fresh.cells.Any(x=>x.id==cell.id))fresh.cells.Add(new BiologyCellAnswer{id=cell.id,answer=cell.answer});
            if(Id=="B01"&&saved.structures!=null)
                foreach(var a in saved.structures.Take(384))
                    if(a!=null&&a.cell>=1&&a.cell<=64&&a.region>=1&&a.region<=6&&Structures.Contains(a.answer)&&!fresh.structures.Any(x=>x.cell==a.cell&&x.region==a.region))fresh.structures.Add(new BiologyStructureAnswer{cell=a.cell,region=a.region,answer=a.answer,correct=Structures[a.region-1]==a.answer});
            s=fresh;
        }
    }
}
