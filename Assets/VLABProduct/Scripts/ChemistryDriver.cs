using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    // Composition, sample identity and topology belong to this lesson authority.
    // Delivered SDK components own instrument geometry, transfer and readouts.
    public sealed partial class ChemistryDriver : LessonDriver
    {
        [Serializable] public sealed class Titre
        { public double initial,final,delivered,acidM;public bool valid;public string reason; }
        [Serializable] public sealed class Sample
        { public string id,solute,sourceSampleId;public double moles,ml,temperatureC; }
        [Serializable] public sealed class Point { public double seconds,value; }
        [Serializable] public sealed class Draft
        { public int version=1;public string lesson;public List<Titre> titres=new List<Titre>();public List<Sample> samples=new List<Sample>(); }
        [Serializable] sealed class Details
        {
            public string lesson,assumptions,sampleId,indicatorInterval;
            public List<Titre> titres=new List<Titre>();public List<Sample> samples=new List<Sample>();public List<Point> curve=new List<Point>();
            public double initialMassG,finalMassG,actualCurrentA,chargeC,initialMeniscus,finalMeniscus,deliveredMl;
            public double acidMl,baseMl,acidM,baseM,acidC,baseC,cupC,cupJPerK,temperatureC,peakC,releasedJ,lostJ;
            public int naclUnits,nitrateUnits,waterUnits;public bool naclSolid,nitrateSolid,waterSolid;
            public bool reversedLeads,saltBridgeImmersed,waterObserved;public double waterPh;
        }
        readonly string id;
        readonly List<Titre> titres=new List<Titre>();readonly List<Sample> samples=new List<Sample>();readonly List<Point> curve=new List<Point>();
        readonly Dictionary<string,Collider> regions=new Dictionary<string,Collider>();
        readonly Dictionary<string,double> regionVolumes=new Dictionary<string,double>();
        int prepared;bool configured,paused=true,failed,powerHasStarted,powerResume;string failure="";
        double clock,delivered,initialMeniscus,finalMeniscusRead,mixedAt=-1,mixedDose=-1,sampleMoles;
        bool initialRead,finalRead,indicator,mixed,bridgeOn=true,leadsConnected,reversed,electrodesIn,initialWeighed,dry,finalWeighed,powerArmed;
        bool waterObserved;double waterPh;
        bool calibrated,rinsed,probeIn,paperUsed,stockRinsed,dilutionTransferred,filledMark,labels,gasSealed,contact,temperaturesRead;
        double charge,initialMass=10,finalMass,gasMoles,lastSample=-10,additionalDilution=1;int qualitativeSelection,unitsLeft=12,naclUnits,nitrateUnits,waterUnits;
        bool naclSolid;string currentFlask="vol50";string sampleId="";
        string configurationStamp="";
        readonly Dictionary<string,ScientificFixtureSupport> electrodeHolders=new Dictionary<string,ScientificFixtureSupport>();
        public ChemistryDriver(ExperimentSession session):base(session){id=session.spec.id;}
        public override bool CanEditParameter(string key)=>id=="C01"&&key=="flow";
        KitItemSpec K(string key,string prefab,string vi,string en,float x,float z,float y=0,bool surface=true,string depends="")=>new KitItemSpec(key,prefab,vi,en,new Vector3(x,y,z),default,surface,depends);
        public override KitItemSpec[] Kit
        {
            get
            {
                switch(id)
                {
                    case "C01":return new[]{K("stand","stand","Giá burette","Burette stand",-.12f,.12f),K("clamp","clamp","Kẹp burette","Burette clamp",-.12f,.12f,.40f,false,"stand"),K("burette","burette","Burette 25 mL","25 mL burette",0,0,.18f,false,"clamp"),K("flask","flask","Bình chuẩn độ","Titration flask",0,0),K("pipette","pipette","Pipette 10 mL","10 mL pipette",-.35f,-.18f),K("bulb","pipette_bulb","Quả bóp","Pipette bulb",-.35f,-.30f,0,true,"pipette"),K("sample","reagent_bottle_250ml","Mẫu giấm pha loãng","Diluted vinegar sample",-.45f,.18f),K("base","reagent_bottle_250ml","NaOH","NaOH",.35f,.18f),K("indicator","dropper","Phenolphthalein","Phenolphthalein dropper",.28f,-.18f),K("waste","beaker","Cốc thải","Waste beaker",.43f,-.1f)};
                    case "C02":return new[]{K("znCup","beaker","ZnSO4","ZnSO4 half-cell",-.055f,0),K("cuCup","beaker","CuSO4","CuSO4 half-cell",.055f,0),K("zn","electrodes_Zn","Điện cực Zn","Zinc electrode",-.08f,.12f),K("cu","electrodes_Cu","Điện cực Cu","Copper electrode",.08f,.12f),K("bridge","salt_bridge","Cầu muối KNO3","KNO3 salt bridge",0,.2f),K("meter","multimeter","Vôn kế","Voltmeter",0,-.24f),K("leadA","cables","Dây COM","COM lead",-.3f,-.1f),K("leadB","cables","Dây V","Voltage lead",.3f,-.1f)};
                    case "C03":return new[]{K("bath","beaker","CuSO4","Copper sulfate bath",0,0),K("cathode","electrodes_Cu","Cathode đồng","Copper cathode",-.025f,.16f),K("anode","electrodes","Anode","Anode",.025f,.16f),K("supply","power_supply","Nguồn DC","DC supply",.25f,-.1f),K("leadA","cables","Dây âm","Negative cable",-.3f,.1f),K("leadB","cables","Dây dương","Positive cable",.3f,.1f),K("balance","balance","Cân 0,001 g","0.001 g balance",-.35f,-.2f),K("timer","timer","Đồng hồ","Stopwatch",.3f,.2f),K("rinse","wash_bottle","Rửa điện cực","Electrode rinse",-.5f,.12f),K("drying","drying_station","Trạm làm khô","Drying station",-.15f,-.25f),K("waste","beaker","Nước rửa thải","Rinse waste",-.48f,.30f)};
                    case "C04":return new[]{K("nacl","beaker","Mẫu NaCl","NaCl sample",-.16f,0),K("nitrate","beaker","Đối chứng NaNO3","NaNO3 control",0,0),K("water","beaker","Mẫu trắng nước","Water blank",.16f,0),K("reagent","reagent_bottle","AgNO3 · đơn vị mô phỏng","AgNO3 · simulation units",.35f,.15f),K("dropper","dropper","Chọn rồi nhỏ thuốc thử","Select then dispense reagent",-.3f,.15f),K("solidNaCl","precipitate_beaker250","AgCl định tính","Qualitative AgCl",-.16f,0,0,true,"nacl"),K("solidNitrate","precipitate_beaker250","Đối chứng không cặn","Control sediment",0,0,0,true,"nitrate"),K("solidWater","precipitate_beaker250","Mẫu trắng không cặn","Blank sediment",.16f,0,0,true,"water")};
                    case "C05":return new[]{K("stock","reagent_bottle_250ml","Dung dịch NaCl gốc","NaCl stock",-.4f,.12f),K("pipette","pipette","Pipette 10 mL","10 mL pipette",-.2f,-.2f),K("bulb","pipette_bulb","Quả bóp","Pipette bulb",-.25f,-.3f,0,true,"pipette"),K("vol50","volumetric_flask_50ml","Bình định mức 50 mL","50 mL volumetric flask",0,0),K("vol100","volumetric_flask","Bình định mức 100 mL","100 mL volumetric flask",.18f,0),K("wash","wash_bottle","Nước cất","Distilled water",.35f,.12f),K("waste","beaker","Cốc tráng","Rinse waste",-.4f,-.15f),K("label","label","Nhãn mẫu","Sample label",.18f,-.2f)};
                    case "C06":return new[]{K("test","beaker","Mẫu acid/base","Acid/base sample",0,0),K("water","beaker","Nước đối chứng","Water control",.16f,0),K("buffer4","beaker","Buffer pH 4","pH 4 buffer",-.16f,.16f),K("buffer7","beaker","Buffer pH 7","pH 7 buffer",0,.16f),K("rinse","beaker","Nước rửa đầu dò","Probe rinse water",.16f,.16f),K("probe","ph_probe","Máy pH","pH meter",-.26f,-.18f),K("paper","indicator_paper","Giấy BTB","BTB paper",.25f,-.18f),K("pipette","pipette","Pipette 10 mL","10 mL pipette",.05f,-.28f),K("bulb","pipette_bulb","Quả bóp","Pipette bulb",-.4f,-.22f,0,true,"pipette"),K("vol100","volumetric_flask","Bình pha loãng 100 mL","100 mL dilution flask",.33f,.14f),K("wash","wash_bottle","Nước cất","Distilled water",.5f,.14f),K("waste","beaker","Nước rửa thải","Rinse waste",.5f,-.13f)};
                    case "C07":return new[]{K("flask","flask","Bình phản ứng","Reaction flask",-.08f,0),K("stopper","stopper_25_1","Nút một lỗ","One-hole stopper",-.08f,0,.09f,false,"flask"),K("tube","tubing","Ống dẫn kín","Sealed tubing",0,.16f),K("syringe","gas_syringe","Syringe 100 mL","100 mL gas syringe",.18f,0),K("chips","chipsBatch","CaCO3 đồng khối lượng","Equal-mass CaCO3 chips",-.3f,-.15f),K("boat","weigh_boat","Thuyền cân hạt","Chip weigh boat",-.3f,-.22f),K("balance","balance","Cân CaCO3","Carbonate balance",-.35f,.15f),K("timer","timer","Đồng hồ","Stopwatch",.35f,-.18f),K("acid","reagent_bottle","HCl","HCl",.4f,.15f)};
                    case "C08":return new[]{K("cal","calorimeter","Cốc cách nhiệt","Calorimeter",0,0),K("acidCup","beaker_100ml","Acid đã đong","Measured acid",-.2f,0),K("baseCup","beaker_100ml","Base đã đong","Measured base",.2f,0),K("thermometer","thermometer","Nhiệt kế","Thermometer",-.32f,-.14f),K("timer","timer","Đồng hồ","Stopwatch",.32f,-.14f),K("stir","stir_rod","Que khuấy","Stirring rod",.35f,.16f)};
                    default:throw new ArgumentException("Unknown chemistry lesson "+id);
                }
            }
        }
        T I<T>(string key) where T:Component=>Instrument<T>(key);
        bool Has(string key)=>equipment!=null&&equipment.ContainsKey(key);
        bool AllInstalled=>equipment!=null&&equipment.Count>0&&equipment.Values.All(e=>e&&e.installed);
        double V(string key,double fallback)=>session.parameters.TryGetValue(key,out var v)?v:fallback;
        static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        static void Must(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        Transform Named(string key,string suffix)=>Socket(key,suffix);
        void Empty(VlabBeaker b){b.Remove(b.VolumeMl);}
        void Fill(VlabBeaker b,double ml){Empty(b);Must(Math.Abs(b.Add(ml)-ml)<1e-8,"Vessel capacity exceeded.");}
        void Bottle(string key,string species,double concentration,double ml,string unit="mol/L")
        {var b=I<VlabReagentBottle>(key);Must(b.TrySetCapOpen(true)&&b.Liquid.TrySetVolume(0)&&b.TryConfigure(species,concentration,unit)&&b.TrySetVolume(ml),"Reagent bottle is unavailable or not upright.");}
        void ConsumeBottle(string key,double ml)
        {var b=I<VlabReagentBottle>(key);Must(b.CapOpen&&b.Liquid.VolumeMl+1e-8>=ml&&b.TrySetVolume(b.Liquid.VolumeMl-ml),"Open the reagent bottle; insufficient reagent remains.");}
        public override void Bind(IReadOnlyDictionary<string,EquipmentHandle> items,Vector3 at){base.Bind(items,at);configured=false;}
        public override void Installed(string key)
        {
            // Derived assemblies follow delivered socket positions at scale one.
            if(id=="C01"&&key=="clamp")
            {I<VlabStand>("stand").TrySetHeight(.35f);I<VlabClamp>("clamp").SetOpening(.014);AlignTitrationSupport();}
            if(id=="C01"&&key=="burette")AlignTitrationSupport();
            if(key=="bulb"&&equipment["pipette"].installed){Must(I<VlabPipetteBulb>("bulb").TryAttach(I<VlabFlask>("pipette")),"Attach the bulb at the pipette connection.");RestPipette();}
            if(id=="C04"&&key.StartsWith("solid",StringComparison.Ordinal))
            {string cup=key=="solidNaCl"?"nacl":key=="solidNitrate"?"nitrate":"water";Must(I<VlabPrecipitate>(key).TryAttachTo(I<VlabBeaker>(cup)),"Sediment must be attached to its actual 250 mL beaker.");I<VlabPrecipitate>(key).TrySetQualitative(false);}
            if(id=="C07"&&key=="stopper")
            {var mouth=Named("flask","_NeckSocket_SOCKET");var seat=Named("stopper","_Seat_SOCKET");equipment[key].transform.position+=mouth.position-seat.position;}
        }
        public override void Configure()
        {
            if(!AllInstalled)return;
            string stamp=string.Join(";",session.parameters.Where(x=>x.Key!="flow").OrderBy(x=>x.Key).Select(x=>x.Key+"="+x.Value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            if(prepared>0){if(stamp!=configurationStamp){failed=true;failure="A controlled variable changed after preparation. Start a fresh attempt and prepare the actual solutions again.";}return;}
            switch(id)
            {
                case "C01":Bottle("sample","CH3COOH",P("acid"),120);Bottle("base","NaOH",P("base"),120);I<VlabFlask>("flask").TrySetVolume(0);I<VlabBurette>("burette").Prepare(0,false);break;
                case "C02":Fill(I<VlabBeaker>("znCup"),120);Fill(I<VlabBeaker>("cuCup"),120);I<VlabElectrode>("zn").SetElectrode("Zn");I<VlabElectrode>("cu").SetElectrode("Cu");reversed=V("polarity",1)<0;break;
                case "C03":Fill(I<VlabBeaker>("bath"),100);I<VlabElectrode>("cathode").SetElectrode("Cu");I<VlabElectrode>("anode").SetElectrode(P("copperAnode")>.5?"Cu":"graphite");I<VlabPowerSupply>("supply").TrySetPower(false);I<VlabPowerSupply>("supply").TryConfigureCurrentMax(2);I<VlabPowerSupply>("supply").TrySetControls(6,P("current"));I<VlabBalance>("balance").SetResolution(.001);break;
                case "C04":foreach(var cup in new[]{"nacl","nitrate","water"})Fill(I<VlabBeaker>(cup),20);Bottle("reagent","AgNO3",2,20,"sim units/transfer");break;
                case "C05":Bottle("stock","NaCl",P("stock"),120);currentFlask=V("final",50)<75?"vol50":"vol100";foreach(var f in new[]{"vol50","vol100"}){I<VlabVolumetricFlask>(f).TrySetStopperOpen(true);I<VlabVolumetricFlask>(f).TrySetVolume(0);}I<VlabWashBottle>("wash").TrySetWaterAmount(250);break;
                case "C06":foreach(var cup in new[]{"test","water","buffer4","buffer7","rinse"})Fill(I<VlabBeaker>(cup),50);I<VlabVolumetricFlask>("vol100").TrySetStopperOpen(true);I<VlabVolumetricFlask>("vol100").TrySetVolume(0);I<VlabWashBottle>("wash").TrySetWaterAmount(250);break;
                case "C07":int size=V("surface",1)>=3?0:V("surface",1)>=1.5?1:2;I<VlabChips>("chips").Configure(size,(int)V("batches",2));Bottle("acid","HCl",P("acid"),80);I<VlabFlask>("flask").TrySetVolume(0);I<VlabGasSyringe>("syringe").SetVolume(0);break;
                case "C08":Fill(I<VlabBeaker>("acidCup"),V("acidMl",25));Fill(I<VlabBeaker>("baseCup"),V("baseMl",25));I<VlabCalorimeter>("cal").SetLid(true);break;
            }
            configurationStamp=stamp;configured=true;
        }
        public override bool Prepare(int completedOperation,out string message)
        {
            message="";
            try
            {
                Must(AllInstalled,"Install all apparatus before preparing solutions.");if(!configured)Configure();
                Must(completedOperation==prepared+1,"Complete the current preparation action in order.");
                switch(id)
                {
                    case "C01":PrepareTitration(completedOperation);break;
                    case "C02":
                        if(completedOperation==1){ImmerseElectrode("zn","znCup",-.012f);ImmerseElectrode("cu","cuCup",.012f);electrodesIn=true;}
                        if(completedOperation==2)InstallBridge();
                        if(completedOperation==3)ConnectMeter();break;
                    case "C03":
                        if(completedOperation==1){WeighElectrode(false);initialWeighed=true;}
                        if(completedOperation==2){ImmerseElectrode("cathode","bath",-.02f);ImmerseElectrode("anode","bath",.02f);electrodesIn=true;dry=false;}
                        if(completedOperation==3)ConnectSupply();break;
                    case "C04":
                        if(completedOperation==1){labels=true;sampleId="NaCl / NaNO3 / water";}
                        if(completedOperation==2)Must(nitrateUnits==2&&waterUnits==2,"Select AgNO3 before each transfer; test both NaNO3 and water controls first.");
                        if(completedOperation==3)Must(naclUnits==2,"Select AgNO3, then dispense exactly 2 simulation units into NaCl.");break;
                    case "C05":
                        if(completedOperation==1){RinsePipette("stock");stockRinsed=true;}
                        if(completedOperation==2){Must(stockRinsed,"Rinse the pipette first.");sampleMoles=P("stock")*.010;TransferPipette("stock",I<VlabVolumetricFlask>(currentFlask).Liquid);dilutionTransferred=true;}
                        if(completedOperation==3)Must(filledMark&&mixed,"Add water to the actual mark, close the stopper and invert to mix using flask controls.");break;
                    case "C06":
                        if(completedOperation==1){labels=true;sampleId=NewSampleId("pH");samples.Add(new Sample{id=sampleId,solute=P("base")>.5?"NaOH":"HCl",moles=P("concentration")/P("dilution")*.050,ml=50,temperatureC=25});}
                        if(completedOperation==2)Must(calibrated&&rinsed,"Observe both actual pH 4 and pH 7 buffers and rinse the probe using its controls.");
                        if(completedOperation==3)Must(probeIn&&paperUsed,"Immerse the probe in the sample and dip the BTB paper before continuing.");break;
                    case "C07":
                        if(completedOperation==1){WeighChips();ConsumeBottle("acid",P("volume"));Must(I<VlabFlask>("flask").TrySetVolume(P("volume")),"Reaction flask must be upright.");}
                        if(completedOperation==2)SealGas();
                        if(completedOperation==3){Must(gasSealed,"Seal both gas ports first.");contact=true;MoveChipsToFlask();I<VlabTimer>("timer").TrySetMode(0);I<VlabTimer>("timer").ResetTimer();}break;
                    case "C08":
                        if(completedOperation==1){Must(V("acidMl",25)+V("baseMl",25)<=I<VlabCalorimeter>("cal").Vessel.CapacityMl,"Combined solution volume exceeds the calorimeter.");}
                        if(completedOperation==2)Must(temperaturesRead,"Measure the actual acid and base cup temperatures with the thermometer controls.");
                        if(completedOperation==3)LoadCalorimeter();break;
                }
                prepared=completedOperation;return true;
            }
            catch(Exception ex){message=ex.Message;return false;}
        }
        string NewSampleId(string prefix)=>id+"-"+prefix+"-"+(samples.Count+1).ToString("D3");
        void PrepareTitration(int step)
        {
            var b=I<VlabBurette>("burette");
            if(step==1){ConsumeBottle("base",28);I<VlabBeaker>("waste").Add(3);b.Prepare(0,false);}
            if(step==2){b.Prepare(0,true);b.SetValve(1);b.Paused=false;double waste=b.Advance(2,I<VlabBeaker>("waste"));Must(Math.Abs(waste-1)<1e-8,"Insufficient waste capacity to prime the tip.");b.Prepare(b.ReadingMl,true);initialRead=false;}
            if(step==3){Must(initialRead,"Read and retain the initial meniscus using the burette control.");RinsePipette("sample");TransferPipette("sample",I<VlabFlask>("flask"));sampleMoles=P("acid")*.010;}
            if(step==4){Must(I<VlabFlask>("flask").VolumeMl>=10-1e-8,"A 10 mL aliquot is required.");Must(I<VlabDropper>("indicator").SetSqueeze(.5f),"Indicator dropper unavailable.");indicator=true;PositionBuretteOutlet();}
        }
        void PipetteAt(Vector3 point)
        {
            var tool=I<VlabFlask>("pipette");var tip=Named("pipette","_TipOutlet_SOCKET");var top=Named("pipette","_BulbConnection_SOCKET");
            tool.transform.rotation=Quaternion.FromToRotation((top.position-tip.position).normalized,Vector3.up)*tool.transform.rotation;
            tool.transform.position+=point-tip.position;
        }
        void RestPipette()
        {
            if(!Has("pipette")||I<VlabFlask>("pipette").VolumeMl>1e-8)return;
            var item=equipment["pipette"];item.transform.SetPositionAndRotation(item.slot,item.rotation);
            if(Has("bulb")&&I<VlabPipetteBulb>("bulb").Attached)
            {
                var bulb=equipment["bulb"].transform;
                var glass=item.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.transform.IsChildOf(bulb)).ToArray();
                var rubber=bulb.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                if(glass.Length>0&&rubber.Length>0)
                {
                    Quaternion flat=item.rotation;var axis=(Named("pipette","_BulbConnection_SOCKET").position-Named("pipette","_TipOutlet_SOCKET").position).normalized;
                    var tiltAxis=Vector3.Cross(axis,Vector3.up).normalized;float lo=0,hi=15;
                    // Natural two-point rest: the wider bulb and glass tip both touch the surface.
                    // Only the rigid original assembly rotates; no holder, scale or fluid change.
                    for(int n=0;n<24;n++)
                    {
                        float angle=(lo+hi)*.5f;item.transform.rotation=Quaternion.AngleAxis(angle,tiltAxis)*flat;
                        if(glass.Min(r=>r.bounds.min.y)>rubber.Min(r=>r.bounds.min.y))lo=angle;else hi=angle;
                    }
                    item.transform.rotation=Quaternion.AngleAxis((lo+hi)*.5f,tiltAxis)*flat;
                }
            }
            var rig=item.GetComponentInParent<ApparatusRig>();if(rig)rig.RestInstalledTool(item);
        }
        void Aspirate(string bottle)
        {
            var source=I<VlabReagentBottle>(bottle);var bulb=I<VlabPipetteBulb>("bulb");
            Must(source.CapOpen,"Open the source bottle first.");
            if(!bulb.Attached)Must(bulb.TryAttach(I<VlabFlask>("pipette")),"Pipette bulb attachment failed.");
            PipetteAt(source.Liquid.CavityCentreAt(source.Liquid.SurfaceHeightMeters*.55));
            Must(bulb.TrySetControl(2)&&bulb.TryAspirate(source.Liquid,10,out var moved)&&Math.Abs(moved-10)<1e-8,"Immerse the pipette tip in sufficient stock before aspirating 10 mL.");
        }
        void TransferPipette(string source,VlabFlask receiver)
        {
            Aspirate(source);PipetteAt(receiver.CavityCentreAt(.02));var bulb=I<VlabPipetteBulb>("bulb");
            Must(bulb.TrySetControl(3)&&bulb.TryDispense(receiver,10,out var moved)&&Math.Abs(moved-10)<1e-8,"Place the pipette outlet inside the actual receiving flask.");bulb.TrySetControl(0);RestPipette();
        }
        void RinsePipette(string source)
        {
            Aspirate(source);var waste=I<VlabBeaker>("waste");var region=BeakerRegion("waste",true);
            PipetteAt(waste.transform.position+Vector3.up*.02f);var bulb=I<VlabPipetteBulb>("bulb");
            Must(bulb.TrySetControl(3)&&bulb.TryDispense(waste,region,10,out var moved)&&Math.Abs(moved-10)<1e-8,"Rinse disposal requires the actual waste cavity.");bulb.TrySetControl(0);RestPipette();
        }
        void PositionBuretteOutlet()
        {
            AlignTitrationSupport();var outlet=Named("burette","_Outlet_SOCKET");var flask=I<VlabFlask>("flask");
            var stand=I<VlabStand>("stand");float desired=worktop.y+.04f;
            Must(stand.TrySetHeight(stand.HeightM+desired-outlet.position.y),"The supplied stand cannot support the burette outlet at the flask height.");AlignTitrationSupport();
            var shift=outlet.position-flask.CavityCentreAt(.04);shift.y=0;flask.transform.position+=shift;
            Must(flask.ContainsPoint(outlet.position),"Burette outlet must be inside the receiving flask cavity.");
        }
        void AlignTitrationSupport()
        {
            var stand=I<VlabStand>("stand");var clamp=I<VlabClamp>("clamp");
            equipment["clamp"].transform.position+=stand.Mount.position-Named("clamp","_StandRod_SOCKET").position;
            if(equipment["burette"].installed)equipment["burette"].transform.position+=clamp.Grip.position-Named("burette","_Clamp_SOCKET").position;
        }
        // A convex cylinder strictly INSIDE the delivered cylindrical beaker. Radius
        // is derived from its calibrated liquid area; height is true liquid depth.
        Collider BeakerRegion(string key,bool cavity=false)
        {
            var b=I<VlabBeaker>(key);string regionKey=key+(cavity?":cavity":":liquid");
            double volume=cavity?b.CapacityMl:b.VolumeMl;
            if(regions.TryGetValue(regionKey,out var cached)&&cached&&regionVolumes.TryGetValue(regionKey,out var oldVolume)&&Math.Abs(volume-oldVolume)<1e-9)return cached;
            if(!regions.TryGetValue(regionKey,out var collider)||!collider)
            {var go=new GameObject("Derived actual "+regionKey);go.transform.SetParent(b.transform,false);collider=go.AddComponent<MeshCollider>();((MeshCollider)collider).convex=true;collider.isTrigger=true;regions[regionKey]=collider;}
            // Source metadata: source beaker is 250 mL, interior bottom 3 mm,
            // polygon area .0034114325598632684 m2; all chemistry cups use that asset.
            double depth=(cavity?b.CapacityMl:b.VolumeMl)*1e-6/b.CrossSectionArea;
            float radius=(float)Math.Sqrt(b.CrossSectionArea/(16*Math.Sin(Math.PI/16)))-.00015f;
            var vertices=new Vector3[64];var tris=new List<int>();
            for(int end=0;end<2;end++)for(int j=0;j<32;j++)
            {float a=2*Mathf.PI*j/32;Vector3 world=b.transform.position+new Vector3(radius*Mathf.Cos(a),(float)b.InteriorBottom+.00005f+(end==1?(float)Math.Max(.0001,depth-.0001):0),radius*Mathf.Sin(a));vertices[end*32+j]=collider.transform.InverseTransformPoint(world);}
            for(int j=0;j<32;j++){int k=(j+1)%32;tris.AddRange(new[]{j,k,k+32,j,k+32,j+32});}
            for(int j=1;j<31;j++){tris.AddRange(new[]{0,j+1,j,32,32+j,33+j});}
            var mesh=new Mesh{name="Derived beaker interior "+key};mesh.vertices=vertices;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();
            var mc=(MeshCollider)collider;var old=mc.sharedMesh;mc.sharedMesh=mesh;if(old)UnityEngine.Object.Destroy(old);regionVolumes[regionKey]=volume;return collider;
        }
        void ImmerseElectrode(string key,string cup,float lateral)
        {
            var e=I<VlabElectrode>(key);var b=I<VlabBeaker>(cup);
            var bottom=Named(key,e.ElectrodeID=="graphite"?"_RodBottom_SOCKET":"_PlateBottom_SOCKET");
            e.transform.position+=b.transform.position+new Vector3(lateral,(float)b.InteriorBottom+.006f,0)-bottom.position;
            Must(ElectrodeWet(key,cup),"Electrode must intersect its own solution and remain separated.");
            var holder=ScientificFixtureSupport.Get(equipment[key].transform.parent,id+"-"+key);
            holder.HoldElectrode(b,e);electrodeHolders[key]=holder;
        }
        bool ElectrodeWet(string key,string cup)
        {
            var e=I<VlabElectrode>(key);var region=BeakerRegion(cup);
            if(e.Contact.position.y<=I<VlabBeaker>(cup).transform.position.y+I<VlabBeaker>(cup).SurfaceHeightMeters)return false;
            // The SDK authors physical lower-end and electrical contact sockets.
            // These survive GPU-only mesh import, unlike Mesh.vertices. Require
            // a positive length of actual electrode beneath the liquid surface.
            var bottom=Named(key,e.ElectrodeID=="graphite"?"_RodBottom_SOCKET":"_PlateBottom_SOCKET");
            var axis=(e.Contact.position-bottom.position).normalized;
            foreach(float distance in new[]{.0005f,.002f,.005f})
            {var p=bottom.position+axis*distance;if((p-region.ClosestPoint(p)).sqrMagnitude>1e-12f)return false;}
            return true;
        }
        void InstallBridge()
        {
            var a=I<VlabBeaker>("znCup");var b=I<VlabBeaker>("cuCup");
            var left=Named("bridge","_EndL_SOCKET");var right=Named("bridge","_EndR_SOCKET");
            var desired=b.transform.position-a.transform.position;
            equipment["bridge"].transform.rotation=Quaternion.FromToRotation(right.position-left.position,desired)*equipment["bridge"].transform.rotation;
            equipment["bridge"].transform.position+=(a.transform.position+b.transform.position)*.5f+Vector3.up*(float)(a.SurfaceHeightMeters*.65)-(left.position+right.position)*.5f;
            var bridge=I<VlabSaltBridge>("bridge");Must(bridge.TryConfigure("KNO3",1,"mol/L")&&bridge.TrySetLoaded(true)&&bridge.TryAssignRegions(BeakerRegion("znCup"),BeakerRegion("cuCup"))&&bridge.BothEndsImmersed,"Fully immerse both salt bridge plugs in their own half-cell.");bridgeOn=true;
        }
        void MoveMeterProbe(bool positive,Transform contactPoint)
        {var tip=Named("meter",positive?"_ProbeBTip_SOCKET":"_ProbeATip_SOCKET");tip.parent.position+=contactPoint.position-tip.position;}
        void ConnectMeter()
        {
            var meter=I<VlabMultimeter>("meter");var a=I<VlabCable>("leadA");var b=I<VlabCable>("leadB");meter.TrySetMode(0);
            MoveMeterProbe(false,I<VlabElectrode>(reversed?"cu":"zn").Contact);MoveMeterProbe(true,I<VlabElectrode>(reversed?"zn":"cu").Contact);
            Must(meter.TryAssignLeads(a,b),"Assign distinct physical leads with the meter switched off.");
            Must(a.TryConnectPair(meter.Port(0),meter.ProbeRear(false)),"COM route rejected: "+CableGeometry(a,meter.Port(0),meter.ProbeRear(false)));
            Must(b.TryConnectPair(meter.Port(1),meter.ProbeRear(true)),"V route rejected: "+CableGeometry(b,meter.Port(1),meter.ProbeRear(true)));
            Must(meter.TrySetMode(1)&&meter.ConnectionsValid,"Select DC voltage after physically connecting COM and V leads.");leadsConnected=true;
        }
        bool MeterContacts()
        {return (Named("meter","_ProbeATip_SOCKET").position-I<VlabElectrode>(reversed?"cu":"zn").Contact.position).sqrMagnitude<1e-8f&&(Named("meter","_ProbeBTip_SOCKET").position-I<VlabElectrode>(reversed?"zn":"cu").Contact.position).sqrMagnitude<1e-8f;}
        string CableGeometry(VlabCable cable,VlabElectricalTerminal a,VlabElectricalTerminal b)
        {return "terminal distance="+Vector3.Distance(a.InsertedTip,b.InsertedTip).ToString("F4")+"m, axis="+a.Axis.ToString("F3")+"/"+b.Axis.ToString("F3")+", compatible="+a.Compatible+"/"+b.Compatible+", route length="+cable.RouteLength.ToString("F4")+"m bend="+cable.MinimumBendRadius.ToString("F4")+"m";}
        string SupportGeometry(VlabBalance balance,VlabMassSample sample)
        {
            var normal=balance.PanCenter.up;var bounds=sample.Shape.bounds;var delta=bounds.center-balance.PanCenter.position;
            float half=Vector3.Dot(new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z)),bounds.extents);
            return "sample active="+sample.isActiveAndEnabled+", shape="+sample.Shape.name+"/"+sample.Shape.GetType().Name+", enabled="+sample.Shape.enabled+", normal="+normal.ToString("F3")+", extents="+bounds.extents.ToString("F4")+", clearance="+(Vector3.Dot(delta,normal)-half).ToString("F5")+"m, lateral="+Vector3.ProjectOnPlane(delta,normal).magnitude.ToString("F4")+"m, XZ footprint="+Mathf.Sqrt(bounds.extents.x*bounds.extents.x+bounds.extents.z*bounds.extents.z).ToString("F4")+"m";
        }
        VlabElectricalTerminal ElectrodeTerminal(string key)
        {
            var e=I<VlabElectrode>(key);var terminal=e.Contact.GetComponent<VlabElectricalTerminal>();if(terminal)return terminal;
            terminal=e.Contact.gameObject.AddComponent<VlabElectricalTerminal>();var rear=new GameObject("Derived 4mm electrode clip back").transform;rear.SetParent(e.Contact,false);rear.position=e.Contact.position-Vector3.up*.015f;
            Must(terminal.TryConfigureContact(id+":"+key,e.Contact,rear),"Electrode clip geometry unavailable.");return terminal;
        }
        void ConnectSupply()
        {
            var p=I<VlabPowerSupply>("supply");Must(!p.On,"Switch power off before connecting electrodes.");
            var a=I<VlabCable>("leadA");var b=I<VlabCable>("leadB");
            Must(a.TryConnectPair(p.Port(false),ElectrodeTerminal("cathode"))&&b.TryConnectPair(p.Port(true),ElectrodeTerminal("anode")),"Connect valid negative-to-cathode and positive-to-anode routes.");
            Must(p.TryConfigureCurrentMax(2)&&p.TrySetControls(6,P("current")),"Set a valid current limit with power off.");powerArmed=leadsConnected=true;
        }
        bool SupplyCircuit()=>I<VlabCable>("leadA").IsConducting&&I<VlabCable>("leadB").IsConducting&&ElectrodeWet("cathode","bath")&&ElectrodeWet("anode","bath")&&ValidateScientificFixtures(out _)&&Vector3.Distance(I<VlabElectrode>("cathode").Contact.position,I<VlabElectrode>("anode").Contact.position)>.015f;
        VlabMassSample MassFor(string key,double grams)
        {
            var go=equipment[key].gameObject;var m=go.GetComponent<VlabMassSample>();if(!m)m=go.AddComponent<VlabMassSample>();
            var shape=go.GetComponentsInChildren<Collider>(true).FirstOrDefault(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger);
            if(!shape&&go.GetComponentInChildren<VlabElectrode>())
            {
                // Delivered rectangular metal plates have no source collider.
                // Add their exact cuboid contact geometry from mesh-local bounds;
                // no visual mesh, material or world scale is changed.
                var plate=Named(key,"_Plate_VIS");var mesh=plate.GetComponent<MeshFilter>();Must(mesh&&mesh.sharedMesh,"Electrode plate geometry missing.");
                var box=plate.gameObject.GetComponent<BoxCollider>();if(!box)box=plate.gameObject.AddComponent<BoxCollider>();box.center=mesh.sharedMesh.bounds.center;box.size=mesh.sharedMesh.bounds.size;shape=box;
            }
            Must(shape,"Delivered apparatus has no weighing collider.");
            // Public binding is in the staged SDK copy; no collider or mass proxy.
            Must(m.TryBindShape(shape),"Mass sample geometry unavailable.");m.enabled=true;m.SetMass(grams);return m;
        }
        void PutOnPan(string key,VlabMassSample m)
        {
            Physics.SyncTransforms();var pan=I<VlabBalance>("balance").PanCenter;var bounds=m.Shape.bounds;
            var normal=pan.up;float half=Vector3.Dot(new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z)),bounds.extents);
            equipment[key].transform.position+=pan.position+normal*(half+.0001f)-bounds.center;
            Physics.SyncTransforms();var balance=I<VlabBalance>("balance");balance.ClearTare();balance.RefreshReading();
            Must(balance.Supports(m),"Place the whole dry sample on the balance pan. "+SupportGeometry(balance,m));
        }
        void WeighElectrode(bool after)
        {
            Must(!after||dry,"Rinse and dry the cathode before weighing.");var m=MassFor("cathode",initialMass+(after?charge/(2*ScienceModels.F)*63.546:0));PutOnPan("cathode",m);
            var balance=I<VlabBalance>("balance");balance.RefreshReading();if(after){finalMass=balance.ReadingGrams;finalWeighed=true;}else initialMass=balance.ReadingGrams;
        }
        void WeighChips()
        {
            var chips=I<VlabChips>("chips");
            // Batch rendering has no collider. Use the delivered weigh boat as
            // required load carrier (added to the kit below), with declared load.
            var m=MassFor("boat",chips.MassGrams);PutOnPan("boat",m);chips.transform.position=m.Shape.bounds.center+Vector3.up*.003f;
            I<VlabBalance>("balance").RefreshReading();
        }
        void MoveChipsToFlask(){var flask=I<VlabFlask>("flask");I<VlabChips>("chips").transform.position=flask.CavityCentreAt(.006);equipment["boat"].GetComponent<VlabMassSample>().SetMass(0);I<VlabBalance>("balance").RefreshReading();}
        void SealGas()
        {
            var tube=I<VlabTubing>("tube");Must(tube.TrySetSpan(.16f,.12f)&&tube.TrySetPorts(id+":flask:gas",id+":syringe:inlet",true,true),"Gas tubing route cannot seal both named ports.");
            var outlet=Named("stopper","_Port1Top_SOCKET");
            var inlet=equipment["syringe"].GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Contains("Inlet")&&t.name.EndsWith("SOCKET"));
            Must(outlet&&inlet,"Delivered gas apparatus port sockets are missing.");
            var a=Named("tube","_GasA_SOCKET");var b=Named("tube","_GasB_SOCKET");
            Must(a&&b,"Tubing connector sockets are missing.");equipment["tube"].transform.position+=outlet.position-a.position;equipment["syringe"].transform.position+=b.position-inlet.position;gasSealed=tube.LogicalEdgeEnabled;
        }
        void LoadCalorimeter()
        {
            var c=I<VlabCalorimeter>("cal");Must(temperaturesRead,"Read both initial temperatures first.");
            c.LoadTrial(V("acidMl",25),V("acidM",.5),V("acidC",25),V("baseMl",25),V("baseM",.5),V("baseC",25),V("cup",20),V("cupC",25));
            Empty(I<VlabBeaker>("acidCup"));Empty(I<VlabBeaker>("baseCup"));c.LossEnabled=V("loss",0)>.5;c.SetLid(false);
            var probePort=Named("cal","_ProbeExit_SOCKET");var sensorPoint=probePort.position;sensorPoint.y=c.Vessel.transform.position.y+(float)((c.Vessel.InteriorBottom+c.Vessel.SurfaceHeightMeters)*.5);
            PlaceThermometer(sensorPoint);mixed=true;
        }
        void PlaceThermometer(Vector3 point)
        {
            var t=I<VlabThermometer>("thermometer");var top=Named("thermometer","_SensorTop_SOCKET");var tip=Named("thermometer","_Tip_SOCKET");
            if(top&&tip)t.transform.rotation=Quaternion.FromToRotation((top.position-tip.position).normalized,Vector3.up)*t.transform.rotation;
            if(top&&tip)t.transform.position+=point-(top.position+tip.position)*.5f;
            else {var low=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).OrderBy(r=>r.bounds.min.y).First();t.transform.position+=point-new Vector3(low.bounds.center.x,low.bounds.min.y+.003f,low.bounds.center.z);}
        }
        void ProbeAt(string cup)
        {
            var p=I<VlabPhProbe>("probe");var b=I<VlabBeaker>(cup);p.ProbeAssembly.position+=b.transform.position+Vector3.up*(float)(b.SurfaceHeightMeters*.55)-p.Tip.GetComponent<Renderer>().bounds.center;
            Must(p.Immersed(BeakerRegion(cup)),"The entire probe sensor must be inside the true liquid region.");
        }
        void WaterToMark()
        {
            Must(dilutionTransferred,"Transfer the 10 mL aliquot first.");var f=I<VlabVolumetricFlask>(currentFlask);Must(f.StopperOpen,"Open the stopper before adding water.");
            var w=I<VlabWashBottle>("wash");var target=f.Liquid.CavityCentreAt(f.Liquid.SurfaceHeightMeters+.001);
            var jetTarget=target+Vector3.up*.08f;w.transform.rotation=Quaternion.FromToRotation(w.OutletDirection,Vector3.down)*w.transform.rotation;w.transform.position+=jetTarget-w.Outlet;
            double required=(currentFlask=="vol50"?50:100)-f.Liquid.VolumeMl;
            Must(Math.Abs(w.TryDispenseTo(f.Liquid,required)-required)<1e-8,"Aim the water jet into the actual open volumetric flask.");w.StopFlow();filledMark=true;
        }
        public override bool CanStart(out string message)
        {
            message="";try
            {
                Must(AllInstalled&&prepared>=session.spec.operationsEn.Length,"Finish assembly and apparatus preparation first.");Must(!failed,failure);
                if(id=="C01"){Must(Math.Abs(V("sample",10)-10)<1e-8,"The supplied volumetric pipette transfers exactly 10 mL.");Must(sampleMoles/P("base")*1000<=I<VlabBurette>("burette").RemainingMl,"The expected titre exceeds the remaining 25 mL burette range. Select a stronger base or weaker sample.");Must(initialRead&&indicator,"Read the initial meniscus and add indicator.");}
                if(id=="C02")Must(CellReady(),"Complete the correct wet half-cells, salt bridge, probes and COM/V routes.");
                if(id=="C03")Must(initialWeighed&&powerArmed&&SupplyCircuit(),"Weigh a dry cathode and connect separated immersed electrodes with power off.");
                if(id=="C05"){Must(Math.Abs(V("aliquot",10)-10)<1e-8&&(V("final",50)==50||V("final",50)==100),"This kit supplies a 10 mL pipette and 50/100 mL volumetric flasks.");Must(filledMark&&mixed,"Fill to the actual calibration mark and mix.");}
                if(id=="C06")Must(calibrated&&rinsed&&probeIn&&paperUsed,"Calibrate, rinse and immerse the pH probe; observe BTB paper.");
                if(id=="C07"){Must(V("surface",1)==1||V("surface",1)==2||V("surface",1)==4,"The three delivered equal-mass chip groups have relative surface areas 1, 2 and 4 only.");Must(contact&&gasSealed&&I<VlabTubing>("tube").LogicalEdgeEnabled,"Seal the gas circuit before bringing reactants into contact.");}
                if(id=="C08")Must(I<VlabCalorimeter>("cal").Loaded&&mixed,"Load explicit acid/base C, V and T and immerse the thermometer.");return true;
            }catch(Exception ex){message=ex.Message;return false;}
        }
        bool CellReady()=>electrodesIn&&leadsConnected&&bridgeOn&&I<VlabSaltBridge>("bridge").BothEndsImmersed&&I<VlabMultimeter>("meter").ConnectionsValid&&MeterContacts()&&ElectrodeWet("zn","znCup")&&ElectrodeWet("cu","cuCup")&&ValidateScientificFixtures(out _);
        public bool ValidateScientificFixtures(out string message)
        {
            message="";if(id!="C02"&&id!="C03")return true;
            string[] keys=id=="C02"?new[]{"zn","cu"}:new[]{"cathode","anode"};
            foreach(var key in keys)if(!electrodeHolders.TryGetValue(key,out var holder)||!holder||!holder.Validate(out message)){if(message=="")message="Prepare both real electrode holders before measurement.";return false;}
            return true;
        }
        public override void SetPaused(bool value)
        {
            bool wasPaused=paused;paused=value;if(!AllInstalled)return;
            if(id=="C01"){var b=I<VlabBurette>("burette");b.Paused=value;if(value){b.SetValve(0);session.parameters["flow"]=0;}}
            if(id=="C03")
            {
                var supply=I<VlabPowerSupply>("supply");
                if(value){if(!wasPaused)powerResume=supply.On;supply.TrySetPower(false);}
                else
                {
                    bool restore=!powerHasStarted||(wasPaused&&powerResume);
                    if(restore&&powerArmed&&!finalWeighed&&SupplyCircuit())supply.TrySetPower(true);
                    powerHasStarted=true;powerResume=false;
                }
            }
            if(id=="C08"){var c=I<VlabCalorimeter>("cal");if(!value&&c.Loaded&&!c.Started)c.StartMix();c.Paused=value;}
            if(Has("timer")){var t=I<VlabTimer>("timer");if(value&&t.Running)t.TryStop(clock);else if(!value&&!t.Running)t.TryStart(clock);}
        }
        public override void InputLost(){SetPaused(true);powerResume=false;}
        public override void Advance(double seconds)
        {
            if(!Finite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));if(paused||failed||!AllInstalled)return;
            if(id=="C01")
            {var b=I<VlabBurette>("burette");b.SetValve(V("flow",0)/100);b.Paused=false;double moved=b.Advance(seconds,I<VlabFlask>("flask"),Named("burette","_Outlet_SOCKET").position);delivered+=moved;if(moved>0){mixed=false;finalRead=false;}if(delivered>sampleMoles/P("base")*1000+.10+1e-7){b.SetValve(0);session.parameters["flow"]=0;}}
            if(id=="C02"&&CellReady())I<VlabMultimeter>("meter").TrySetReading(CellVoltage());
            if(id=="C03")AdvanceElectrolysis(seconds);
            if(id=="C07")AdvanceGas(seconds);
            if(id=="C08")
            {var c=I<VlabCalorimeter>("cal");c.Advance(seconds);var region=CalRegion();Must(I<VlabThermometer>("thermometer").TryObserve(region,(float)c.TemperatureC),"Thermometer sensor left the calorimeter liquid.");}
            clock+=seconds;if(Has("timer")&&I<VlabTimer>("timer").Running)I<VlabTimer>("timer").TrySample(clock);
            if((id=="C07"||id=="C08")&&clock-lastSample>=10-1e-8){curve.Add(new Point{seconds=clock,value=id=="C07"?I<VlabGasSyringe>("syringe").VolumeMl:I<VlabCalorimeter>("cal").TemperatureC});lastSample=clock;}
        }
        void AdvanceElectrolysis(double dt)
        {
            var p=I<VlabPowerSupply>("supply");if(!p.On)return;
            if(!SupplyCircuit()){p.TrySetPower(false);failed=true;failure="The wet electrode circuit is open or shorted. Prepare a fresh attempt.";return;}
            double available=P("copperAnode")>.5?double.MaxValue:Math.Max(0,P("moles")*2*ScienceModels.F-charge);
            double active=Math.Min(dt,Math.Max(0,P("time")-clock));double q=Math.Min(available,P("current")*active);charge+=q;
            double amps=active>0?q/active:0;p.TryObserveOperatingPoint(6,amps,amps>0&&Math.Abs(amps-P("current"))<1e-8);
            I<VlabElectrode>("cathode").Coating.SetVisualLevel((float)Math.Min(1,charge/(2*ScienceModels.F)*63.546/.1));
            if(clock+dt>=P("time")||available-q<=1e-10)p.TrySetPower(false);
        }
        void AdvanceGas(double dt)
        {
            if(!gasSealed||!I<VlabTubing>("tube").LogicalEdgeEnabled){failed=true;failure="Gas collection leaked; retain this invalid trial and prepare a new one.";return;}
            var chips=I<VlabChips>("chips");double max=Math.Min(chips.MassGrams/100.0869,P("acid")*P("volume")/2000);
            double area=chips.SizeIndex==0?4:chips.SizeIndex==1?2:1;
            // Declared illustrative finite-substrate kinetics, not a measured rate law.
            gasMoles=max-(max-gasMoles)*Math.Exp(-.04*area*P("acid")*dt);
            double ml=gasMoles*.082057*298.15*1000;var syringe=I<VlabGasSyringe>("syringe");syringe.SetVolume((float)Math.Min(syringe.CapacityMl,ml));
            if(ml>=syringe.CapacityMl){failed=true;failure="Syringe capacity reached; later gas volume is unavailable.";}
        }
        double CellVoltage()=> (1.1+8.314462618*(P("temperature")+273.15)/(2*ScienceModels.F)*Math.Log(P("copper")/P("zinc")))*(reversed?-1:1);
        double Ph()
        {double c=P("concentration")/(P("dilution")*additionalDilution),h=(c+Math.Sqrt(c*c+4e-14))/2,p=-Math.Log10(h);return P("base")>.5?14-p:p;}
        Collider CalRegion()
        {
            // Reuse the true SDK calorimeter vessel (same calibrated beaker asset).
            var vessel=I<VlabCalorimeter>("cal").Vessel;
            string key="cal"; // Cal's child VlabBeaker is found by Instrument.
            return BeakerRegion(key);
        }
        ModelResult R(string note="")=>new ModelResult{note=note};
        public override ModelResult Observe()
        {
            if(!AllInstalled)return R("Install the apparatus to obtain instrument observations.");
            var r=R();switch(id)
            {
                case "C01":var b=I<VlabBurette>("burette");r.Add("meniscus",b.ReadingMl,"mL","Mặt khum burette","Burette meniscus").Add("delivered",delivered,"mL","Thể tích đã chuyển","Transferred volume").Add("validTitres",titres.Count(x=>x.valid),"","Lượt hợp lệ","Valid titres");if(mixed&&indicator)r.Add("pH",Kernel(delivered).Get("pH"),"","pH sau trộn","Mixed solution pH");r.note=mixed?EndpointText():"Swirl after delivery before judging the indicator.";break;
                case "C02":if(CellReady()){I<VlabMultimeter>("meter").TrySetReading(CellVoltage());r.Add("voltage",I<VlabMultimeter>("meter").Reading,"V","Điện áp pin","Cell voltage");}else r.note="Open ionic/electrical circuit: no valid voltage reading.";break;
                case "C03":r.Add("charge",charge,"C","Điện lượng thực","Actual integrated charge").Add("copperMass",charge/(2*ScienceModels.F)*63.546,"g","Đồng bám tính theo điện lượng","Charge-derived copper deposit").Add("initialMass",initialMass,"g","Khối lượng cân đầu","Initial balance mass").Add("anodeMassLoss",P("copperAnode")>.5?charge/(2*ScienceModels.F)*63.546:0,"g","Khối lượng anode hòa tan","Dissolved anode mass").Add("oxygenMoles",P("copperAnode")>.5?0:charge/(4*ScienceModels.F),"mol","Oxy ở anode trơ","Inert-anode oxygen").Add("remainingCopper",P("copperAnode")>.5?P("moles"):Math.Max(0,P("moles")-charge/(2*ScienceModels.F)),"mol","Cu2+ còn trong dung dịch","Copper ions remaining");if(finalWeighed)r.Add("finalMass",finalMass,"g","Khối lượng cân sau khô","Final dry balance mass").Add("massChange",finalMass-initialMass,"g","Độ tăng khối lượng cân","Observed balance mass change");r.note="100% current efficiency; virtual clean cathode starts at 10.000 g; deposited mass and 0.001 g balance resolution are recorded separately.";break;
                case "C04":r.Add("NaClSolid",naclSolid?1:0,"presence","NaCl: cặn trắng","NaCl: white solid").Add("NaNO3Solid",0,"presence","NaNO3: cặn trắng","NaNO3: white solid").Add("waterSolid",0,"presence","Nước: cặn trắng","Water: white solid");r.note="Qualitative only. Each deliberate transfer is 2 simulation units; no molar quantity or mL conversion is inferred.";break;
                case "C05":var f=I<VlabVolumetricFlask>(currentFlask).Liquid;r.Add("aliquot",dilutionTransferred?10:0,"mL","Aliquot đã chuyển","Transferred aliquot").Add("solute",sampleMoles,"mol","Mol chất tan bảo toàn","Conserved solute amount").Add("finalVolume",f.VolumeMl,"mL","Thể tích thực trong bình","Actual flask volume");if(f.VolumeMl>0)r.Add("concentration",sampleMoles/(f.VolumeMl/1000),"mol/L","Nồng độ mẫu","Sample concentration");r.note=sampleId;break;
                case "C06":var probe=I<VlabPhProbe>("probe");if(waterObserved)r.Add("waterControlPh",waterPh,"","pH mẫu nước đã đo","Observed water control pH");r.Add("dilutionFactor",P("dilution")*additionalDilution,"","Tổng hệ số pha loãng","Total dilution factor").Add("concentration",P("concentration")/(P("dilution")*additionalDilution),"mol/L","Nồng độ mẫu","Sample concentration");if(probeIn&&calibrated&&rinsed&&probe.TryObserve(BeakerRegion("test"),sampleId,(float)Ph()))r.Add("pH",probe.Reading,"","pH đầu dò","Probe pH");if(paperUsed){int s=I<VlabIndicatorPaper>("paper").State;r.Add("BTBstate",s,"interval","Khoảng màu BTB","BTB colour interval");r.note=I<VlabIndicatorPaper>("paper").IntervalLabel;}break;
                case "C07":var ch=I<VlabChips>("chips");double max=Math.Min(ch.MassGrams/100.0869,P("acid")*P("volume")/2000);r.Add("gasVolume",I<VlabGasSyringe>("syringe").VolumeMl,"mL","Thể tích CO2 syringe","Syringe CO2 volume").Add("gasLimit",max*.082057*298.15*1000,"mL","Plateau khí lý tưởng","Ideal gas plateau").Add("carbonateMass",ch.MassGrams,"g","Khối lượng hạt thực","Actual chip mass").Add("particleCount",ch.Count,"","Số hạt","Particle count").Add("area",ch.ExteriorAreaM2,"m²","Diện tích hình học","Geometric exterior area").Add("time",clock,"s","Thời gian phản ứng","Reaction time");r.note="Illustrative k=0.04/(relative-area·mol/L·s); ideal gas at 298.15 K and 1 atm. Only sealed collection is valid.";break;
                case "C08":var c=I<VlabCalorimeter>("cal");if(c.Loaded){r.Add("temperature",c.TemperatureC,"°C","Nhiệt độ dung dịch","Solution temperature").Add("initialTemperature",c.InitialTemperatureC,"°C","Nhiệt độ cân bằng đầu","Initial equilibrium temperature").Add("peakTemperature",c.PeakTemperatureC,"°C","Nhiệt độ cao nhất","Peak temperature").Add("releasedHeat",c.ReleasedHeatJ,"J","Nhiệt đã tỏa","Released reaction heat").Add("lostHeat",c.LostHeatJ,"J","Nhiệt thất thoát ròng","Net heat lost").Add("reactedAmount",c.LimitingMoles*(1-Math.Exp(-c.ElapsedSeconds/2)),"mol","Mol đã phản ứng","Reacted amount").Add("heatCapacity",c.TotalHeatCapacity,"J/K","Tổng nhiệt dung","Total heat capacity");if(c.LimitingMoles>0)r.Add("apparentEnthalpy",-c.TotalHeatCapacity*(c.PeakTemperatureC-c.InitialTemperatureC)/c.LimitingMoles/1000,"kJ/mol","Enthalpy từ Tmax","Peak-derived enthalpy");}r.note="SDK model: −57.0 kJ/mol; density 1 g/mL; c=4.18 J/(g K); illustrative 2 s mixing and optional lid-dependent heat leak.";break;
            }
            if(failed)r.note=failure;r.detailJson=DetailJson();return r;
        }
        string EndpointText()
        {double eq=sampleMoles/P("base")*1000;if(delivered>eq+.10)return "Deep pink / overshot: retain the rejected titre and prepare a fresh aliquot.";if(delivered<eq-.05)return "Colourless / endpoint not reached.";return clock-mixedAt>=30?"Persistent pale pink for 30 s: close valve and read final meniscus.":"Pale pink: observe persistence for 30 s with the valve closed.";}
        bool TitreValid(out string reason)
        {
            double eq=sampleMoles/P("base")*1000;
            if(!initialRead||!finalRead){reason="Read both initial and final menisci with the valve closed.";return false;}
            if(!indicator||!mixed||Math.Abs(mixedDose-delivered)>1e-8){reason="Add indicator and mix the latest delivery before recording.";return false;}
            if(V("flow",0)>0||I<VlabBurette>("burette").ValveOpening>0){reason="Close the burette valve.";return false;}
            if(delivered<eq-.05){reason="Endpoint not reached.";return false;}
            if(delivered>eq+.10){reason="Endpoint overshot; retain this rejected titre.";return false;}
            if(clock-mixedAt<30){reason="Observe pale pink persistence for 30 s after the last swirl.";return false;}
            reason="";return true;
        }
        public override bool CanRecord(out string message)
        {
            message="";if(failed){message=failure;return false;}if(!AllInstalled){message="Apparatus is disconnected.";return false;}
            switch(id)
            {
                case "C01":var valid=titres.Where(t=>t.valid).ToList();if(valid.Count<3){message="Retain three valid titres with the burette control, preparing a fresh aliquot between each.";return false;}var last=valid.Skip(valid.Count-3).ToArray();if(last.Max(t=>t.delivered)-last.Min(t=>t.delivered)>.10+1e-8){message="The three latest valid titres must agree within 0.10 mL.";return false;}return true;
                case "C02":return Require(CellReady()&&clock>=2,"Restore both wet half-cells, salt bridge and correct meter contacts before recording.",out message);
                case "C03":return Require(finalWeighed&&dry&&!I<VlabPowerSupply>("supply").On&&clock+1e-8>=P("time"),"Finish the timed current interval, switch off, rinse, dry and weigh the cathode.",out message);
                case "C04":return Require(labels&&naclUnits==2&&nitrateUnits==2&&waterUnits==2,"All three labelled samples need one deliberate 2-unit AgNO3 transfer.",out message);
                case "C05":return Require(filledMark&&mixed&&samples.Any(s=>s.id==sampleId),"Fill to mark, close and mix the labelled sample.",out message);
                case "C06":return Require(calibrated&&rinsed&&probeIn&&paperUsed&&!float.IsNaN(I<VlabPhProbe>("probe").Reading),"A rinsed calibrated probe must remain fully immersed; observe BTB separately.",out message);
                case "C07":double max=Math.Min(I<VlabChips>("chips").MassGrams/100.0869,P("acid")*P("volume")/2000);return Require(gasSealed&&curve.Count>=3&&max>0&&gasMoles>=.99*max,"Continue sealed sampling until at least three time points and 99% of the predicted plateau.",out message);
                case "C08":return Require(clock>=20&&I<VlabCalorimeter>("cal").Loaded&&!float.IsNaN(I<VlabThermometer>("thermometer").ReadingC),"Observe the immersed thermometer for at least 20 s.",out message);
                default:message="Unknown lesson.";return false;
            }
        }
        public override bool TryMeasure(out ModelResult result,out string message)
        {result=null;if(!CanRecord(out message))return false;result=RecordableObservation();SetPaused(true);return true;}
        LessonControl C(string action,string vi,string en,bool enabled=true)=>new LessonControl(action,vi,en,enabled);
        public override LessonControl[] Controls(string key)
        {
            if(!AllInstalled)return Array.Empty<LessonControl>();
            switch(id)
            {
                case "C01":if(key=="burette")return new[]{C("initial","Đọc mặt khum đầu","Read initial meniscus",prepared==2),C("close","Đóng khóa","Close valve"),C("final","Đọc mặt khum cuối","Read final meniscus",indicator),C("titre","Giữ lượt chuẩn độ","Retain titre",indicator),C("fresh","Mẫu mới / lượt kế","Fresh aliquot / next titre",titres.Count>0||delivered>0)};if(key=="flask")return new[]{C("aliquot","Pipette mẫu 10 mL","Pipette 10 mL aliquot",prepared==2&&initialRead&&session.Operation>=4),C("indicator","Thêm phenolphthalein","Add phenolphthalein",prepared==3&&session.Operation>=4),C("swirl","Lắc trộn bình","Swirl flask",indicator)};break;
                case "C02":if(key=="bridge")return new[]{C("bridgeOff","Tháo cầu muối","Lift salt bridge",bridgeOn),C("bridgeOn","Ngâm lại cầu","Re-immerse salt bridge",!bridgeOn)};if(key=="meter")return new[]{C("reverse","Đảo đầu đo","Reverse meter probes",leadsConnected)};break;
                case "C03":if(key=="supply")return new[]{C("off","Tắt nguồn","Switch off"),C("on","Bật nguồn","Switch on",powerArmed&&!finalWeighed)};if(key=="cathode"||key=="balance")return new[]{C("rinseCathode","Rửa cathode","Rinse cathode",charge>0&&!I<VlabPowerSupply>("supply").On),C("dryCathode","Làm khô cathode","Dry cathode",charge>0&&!I<VlabPowerSupply>("supply").On),C("weighFinal","Cân cathode khô","Weigh dry cathode",dry)};break;
                case "C04":if(key=="reagent"||key=="dropper")return new[]{C("selectAg","Chọn AgNO3","Select AgNO3",labels&&unitsLeft>=2)};if(key=="nacl"||key=="nitrate"||key=="water")return new[]{C("dose:"+key,"Thêm 2 đơn vị mô phỏng","Add 2 simulation units",labels&&qualitativeSelection==1)};break;
                case "C05":if(key==currentFlask||key=="wash")return new[]{C("waterMark","Thêm nước tới vạch","Add water to mark",dilutionTransferred&&!filledMark),C("invert","Đậy và đảo trộn","Stopper and invert to mix",filledMark&&!mixed),C("serial","Lấy 10 mL pha nối tiếp","Take 10 mL for serial dilution",mixed&&samples.Count<3)};break;
                case "C06":if(key=="probe")return new[]{C("buffer4","Nhúng buffer pH 4","Immerse in pH 4 buffer",labels),C("buffer7","Nhúng buffer pH 7","Immerse in pH 7 buffer",labels),C("rinseProbe","Rửa đầu dò","Rinse probe",labels),C("probeSample","Nhúng vào mẫu","Immerse in sample",calibrated&&rinsed),C("probeWater","Đo mẫu nước","Measure water control",calibrated&&rinsed)};if(key=="paper")return new[]{C("paper","Nhúng giấy BTB mới","Dip fresh BTB paper",labels)};if(key=="test")return new[]{C("dilute10","Pipette 10 mL, pha tới 100 mL","Pipette 10 mL, dilute to 100 mL",labels&&additionalDilution<100&&I<VlabBeaker>("test").VolumeMl>=20)};break;
                case "C07":if(key=="tube"||key=="stopper")return new[]{C("unseal","Mở cổng khí","Open gas port",gasSealed)};break;
                case "C08":if(key=="thermometer")return new[]{C("acidT","Đo T acid","Measure acid temperature",prepared<=1),C("baseT","Đo T base","Measure base temperature",prepared<=1)};if(key=="cal")return new[]{C("lidOpen","Mở nắp","Open lid"),C("lidClose","Đóng nắp","Close lid")};break;
            }
            return Array.Empty<LessonControl>();
        }
        bool buffer4Seen,buffer7Seen,cathodeRinsed,acidTempSeen,baseTempSeen;
        public override bool Operate(string action,out string message)
        {
            message="";try
            {
                Must(AllInstalled,"Install apparatus first.");if(!configured)Configure();
                switch(action)
                {
                    case "initial":Must(id=="C01"&&prepared==2&&delivered==0,"Read the initial meniscus after priming and before sample delivery.");initialMeniscus=I<VlabBurette>("burette").ReadingMl;initialRead=true;break;
                    case "close":Must(id=="C01","Wrong instrument.");I<VlabBurette>("burette").SetValve(0);session.parameters["flow"]=0;break;
                    case "swirl":Must(id=="C01"&&indicator,"Prepare the aliquot and indicator first.");mixed=true;mixedDose=delivered;mixedAt=clock;break;
                    case "final":Must(id=="C01"&&indicator&&V("flow",0)==0&&I<VlabBurette>("burette").ValveOpening==0,"Close the burette before reading.");finalMeniscusRead=I<VlabBurette>("burette").ReadingMl;finalRead=true;break;
                    case "aliquot":Must(id=="C01"&&prepared==2&&initialRead&&session.Operation>=4,"Read the new initial meniscus first.");PrepareTitration(3);prepared=3;break;
                    case "indicator":Must(id=="C01"&&prepared==3&&session.Operation>=4,"Transfer the new aliquot first.");PrepareTitration(4);prepared=4;break;
                    case "titre":RetainTitre();break;
                    case "fresh":FreshTitration();break;
                    case "bridgeOff":Must(id=="C02","Wrong lesson.");equipment["bridge"].transform.position+=Vector3.up*.15f;bridgeOn=false;break;
                    case "bridgeOn":Must(id=="C02","Wrong lesson.");InstallBridge();break;
                    case "reverse":Must(id=="C02"&&leadsConnected,"Connect the voltmeter first.");reversed=!reversed;I<VlabCable>("leadA").Disconnect(0);I<VlabCable>("leadA").Disconnect(1);I<VlabCable>("leadB").Disconnect(0);I<VlabCable>("leadB").Disconnect(1);ConnectMeter();break;
                    case "off":Must(id=="C03","Wrong lesson.");I<VlabPowerSupply>("supply").TrySetPower(false);break;
                    case "on":Must(id=="C03"&&powerArmed&&SupplyCircuit()&&!paused&&!finalWeighed,"Start the prepared experiment before switching on the valid circuit.");Must(I<VlabPowerSupply>("supply").TrySetPower(true),"Source refused power.");break;
                    case "rinseCathode":RinseCathode();break;
                    case "dryCathode":Must(id=="C03"&&cathodeRinsed&&!I<VlabPowerSupply>("supply").On,"Rinse the cathode with power off before drying.");dry=true;break;
                    case "weighFinal":Must(id=="C03"&&dry,"Dry the cathode first.");WeighElectrode(true);break;
                    case "selectAg":Must(id=="C04"&&labels&&unitsLeft>=2,"Label the samples; insufficient reagent remains.");qualitativeSelection=1;break;
                    case "waterMark":Must(id=="C05","Wrong lesson.");WaterToMark();break;
                    case "serial":SerialDilution();break;
                    case "invert":Must(id=="C05"&&filledMark&&!mixed,"Fill to the mark first; each sample is labelled once.");Must(I<VlabVolumetricFlask>(currentFlask).TrySetStopperOpen(false),"Stopper closure failed.");mixed=true;sampleId=NewSampleId("NaCl");Must(I<VlabLabel>("label").SetIds("NaCl",sampleId),"Sample label unavailable.");samples.Add(new Sample{id=sampleId,solute="NaCl",moles=sampleMoles,sourceSampleId=serialSourceId,ml=I<VlabVolumetricFlask>(currentFlask).Liquid.VolumeMl,temperatureC=25});break;
                    case "buffer4":Must(id=="C06"&&labels&&(!buffer7Seen||rinsed),"Rinse before changing calibration buffers.");ProbeAt("buffer4");Must(I<VlabPhProbe>("probe").TryConfigureBuffers(4,7,25)&&I<VlabPhProbe>("probe").TryObserve(BeakerRegion("buffer4"),id+"-buffer4",4),"Read the immersed pH 4 buffer.");buffer4Seen=true;rinsed=false;probeIn=false;break;
                    case "buffer7":Must(id=="C06"&&labels&&buffer4Seen&&rinsed,"Observe pH 4 and rinse before changing to pH 7 buffer.");ProbeAt("buffer7");Must(I<VlabPhProbe>("probe").TryObserve(BeakerRegion("buffer7"),id+"-buffer7",7),"Read the immersed pH 7 buffer.");buffer7Seen=true;rinsed=false;probeIn=false;break;
                    case "rinseProbe":Must(id=="C06","Wrong lesson.");ProbeAt("rinse");rinsed=true;probeIn=false;if(buffer4Seen&&buffer7Seen){Must(I<VlabPhProbe>("probe").TryConfigureBuffers(4,7,25),"pH calibration metadata unavailable.");calibrated=true;}break;
                    case "probeSample":Must(id=="C06"&&calibrated&&rinsed,"Calibrate and rinse first.");ProbeAt("test");Must(I<VlabPhProbe>("probe").TryObserve(BeakerRegion("test"),sampleId,(float)Ph()),"Actual probe immersion is invalid.");probeIn=true;break;
                    case "probeWater":Must(id=="C06"&&calibrated&&rinsed,"Calibrate and rinse first.");ProbeAt("water");Must(I<VlabPhProbe>("probe").TryObserve(BeakerRegion("water"),id+"-water",7),"Water probe immersion invalid.");waterObserved=true;waterPh=I<VlabPhProbe>("probe").Reading;probeIn=false;rinsed=false;message="Water control: pH 7.00 at 25 °C. Rinse before changing samples.";break;
                    case "paper":Must(id=="C06"&&labels,"Prepare labelled samples first.");var paper=I<VlabIndicatorPaper>("paper");var sample=I<VlabBeaker>("test");var sheet=paper.GetComponentsInChildren<Renderer>().First(r=>r.enabled);paper.transform.position+=sample.transform.position+Vector3.up*(float)(sample.SurfaceHeightMeters-.002)-sheet.bounds.center;Must(paper.ResetReading()&&paper.ObservePh((float)Ph()),"BTB paper unavailable.");paperUsed=true;break;
                    case "dilute10":DilutePhSample();break;
                    case "unseal":Must(id=="C07","Wrong lesson.");I<VlabTubing>("tube").ClearPorts();gasSealed=false;if(contact){failed=true;failure="Gas port opened after contact: collected gas volume is invalid.";}break;
                    case "acidT":MeasureInitialTemperature(true);break;
                    case "baseT":MeasureInitialTemperature(false);break;
                    case "lidOpen":Must(id=="C08","Wrong lesson.");I<VlabCalorimeter>("cal").SetLid(true);break;
                    case "lidClose":Must(id=="C08","Wrong lesson.");I<VlabCalorimeter>("cal").SetLid(false);break;
                    default:if(action.StartsWith("dose:",StringComparison.Ordinal))QualitativeDose(action.Substring(5));else throw new InvalidOperationException("Unknown chemistry instrument action.");break;
                }
                return true;
            }catch(Exception ex){message=ex.Message;return false;}
        }
        void RetainTitre()
        {
            Must(id=="C01"&&indicator,"Prepare the titration first.");bool valid=TitreValid(out var reason);
            Must(valid||delivered>sampleMoles/P("base")*1000+.10,"Finish the endpoint observations before retaining this titre: "+reason);
            Must(!titres.Any(t=>Math.Abs(t.initial-initialMeniscus)<1e-8&&Math.Abs(t.final-I<VlabBurette>("burette").ReadingMl)<1e-8&&t.reason=="current"),"This titre was already retained; prepare a fresh aliquot.");
            titres.Add(new Titre{initial=initialMeniscus,final=I<VlabBurette>("burette").ReadingMl,delivered=delivered,acidM=P("base")*delivered/10,valid=valid,reason=valid?"current":reason});
        }
        void FreshTitration()
        {
            Must(id=="C01"&&indicator,"Prepare the first titration before requesting another aliquot.");
            foreach(var t in titres)if(t.reason=="current")t.reason="accepted";
            var flask=I<VlabFlask>("flask");var waste=I<VlabBeaker>("waste");Must(waste.CapacityMl-waste.VolumeMl>=flask.VolumeMl+14,"Waste is full; start a fresh attempt with an empty waste vessel.");
            waste.Add(flask.VolumeMl);Must(flask.TrySetVolume(0),"Place the flask upright before emptying.");
            delivered=0;mixed=false;initialRead=finalRead=false;indicator=false;mixedAt=-1;session.parameters["flow"]=0;
            PrepareTitration(1);PrepareTitration(2);initialMeniscus=I<VlabBurette>("burette").ReadingMl;
            // A fresh trial remains a deliberate sequence: initial reading,
            // transfer and indicator controls are exposed below via next actions.
            prepared=2;
        }
        void QualitativeDose(string cup)
        {
            Must(id=="C04"&&labels&&qualitativeSelection==1&&unitsLeft>=2,"Select AgNO3 again before each transfer.");
            Must(cup=="nacl"||cup=="nitrate"||cup=="water","Unknown sample.");int existing=cup=="nacl"?naclUnits:cup=="nitrate"?nitrateUnits:waterUnits;Must(existing==0,"This sample already received its prescribed 2 simulation units.");
            var drop=I<VlabDropper>("dropper");var b=I<VlabBeaker>(cup);var restPosition=drop.transform.position;var restRotation=drop.transform.rotation;
            drop.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.down)*drop.transform.rotation;
            drop.transform.position+=b.transform.position+Vector3.up*.11f-drop.Tip.position;
            try
            {
                Must(Vector3.Dot(drop.EmissionDirection,Vector3.down)>.999f&&drop.SetSqueeze(1),"Aim the dropper outlet downward over the selected beaker.");
                var solid=I<VlabPrecipitate>(cup=="nacl"?"solidNaCl":cup=="nitrate"?"solidNitrate":"solidWater");
                Must(solid.TrySetQualitative(cup=="nacl",.8f),"Place the precipitate upright inside its sample cup.");
                qualitativeSelection=0;unitsLeft-=2;if(cup=="nacl"){naclUnits=2;naclSolid=true;}else if(cup=="nitrate")nitrateUnits=2;else waterUnits=2;
            }
            finally{drop.SetSqueeze(0);drop.transform.SetPositionAndRotation(restPosition,restRotation);}
        }
        void MeasureInitialTemperature(bool acid)
        {
            Must(id=="C08"&&prepared<=1,"Read initial temperatures before mixing.");string cup=acid?"acidCup":"baseCup";var b=I<VlabBeaker>(cup);
            var instrument=I<VlabThermometer>("thermometer");var restPosition=instrument.transform.position;var restRotation=instrument.transform.rotation;
            try{PlaceThermometer(b.transform.position+Vector3.up*(float)((b.InteriorBottom+b.SurfaceHeightMeters)*.5));Must(instrument.TryObserve(BeakerRegion(cup),(float)V(acid?"acidC":"baseC",25)),"The whole temperature sensor must be immersed.");if(acid)acidTempSeen=true;else baseTempSeen=true;temperaturesRead=acidTempSeen&&baseTempSeen;}
            finally{instrument.transform.SetPositionAndRotation(restPosition,restRotation);}
        }
        void RinseCathode()
        {
            Must(id=="C03"&&charge>0&&!I<VlabPowerSupply>("supply").On,"Switch off before removing the cathode.");
            I<VlabCable>("leadA").Disconnect(1);var waste=I<VlabBeaker>("waste");var electrode=I<VlabElectrode>("cathode");
            electrode.transform.position+=waste.transform.position+Vector3.up*.11f-electrode.Contact.position;
            var w=I<VlabWashBottle>("rinse");var target=waste.transform.position+Vector3.up*(float)(waste.SurfaceHeightMeters+.002);
            w.transform.rotation=Quaternion.FromToRotation(w.OutletDirection,Vector3.down)*w.transform.rotation;w.transform.position+=target+Vector3.up*.08f-w.Outlet;
            Must(Math.Abs(w.TryDispenseTo(waste,5)-5)<1e-8,"Aim 5 mL rinse water through the removed cathode into the waste vessel.");w.StopFlow();
            var rest=Named("drying","_PlateRest_SOCKET");electrode.transform.position+=rest.position-Named("cathode","_PlateBottom_SOCKET").position;
            cathodeRinsed=true;dry=false;powerArmed=false;
        }
        string serialSourceId="";
        void DilutePhSample()
        {
            Must(id=="C06"&&labels&&additionalDilution<100,"Prepare a labelled sample before making a tenfold dilution.");
            var source=I<VlabBeaker>("test");var waste=I<VlabBeaker>("waste");var flask=I<VlabVolumetricFlask>("vol100");var bulb=I<VlabPipetteBulb>("bulb");
            Must(source.VolumeMl>=20&&waste.CapacityMl-waste.VolumeMl>=source.VolumeMl,"Insufficient sample or waste capacity for dilution.");
            Must(flask.TrySetStopperOpen(true)&&flask.TrySetVolume(0),"Open and empty the 100 mL flask.");
            var region=BeakerRegion("test");PipetteAt(source.transform.position+Vector3.up*(float)(source.InteriorBottom+(source.SurfaceHeightMeters-source.InteriorBottom)*.5));
            Must(bulb.TrySetControl(2)&&bulb.TryAspirate(source,region,10,out var rinse)&&Math.Abs(rinse-10)<1e-8,"Immerse the pipette in the actual sample for rinsing.");
            PipetteAt(waste.transform.position+Vector3.up*.02f);Must(bulb.TrySetControl(3)&&bulb.TryDispense(waste,BeakerRegion("waste",true),10,out var rinsedMl)&&Math.Abs(rinsedMl-10)<1e-8,"Dispose of the measured rinse.");
            region=BeakerRegion("test");PipetteAt(source.transform.position+Vector3.up*(float)(source.InteriorBottom+(source.SurfaceHeightMeters-source.InteriorBottom)*.5));
            Must(bulb.TrySetControl(2)&&bulb.TryAspirate(source,region,10,out var aliquot)&&Math.Abs(aliquot-10)<1e-8,"Aspirate the ten-millilitre sample aliquot.");
            PipetteAt(flask.Liquid.CavityCentreAt(.02));Must(bulb.TrySetControl(3)&&bulb.TryDispense(flask.Liquid,10,out var moved)&&Math.Abs(moved-10)<1e-8,"Transfer into the actual 100 mL flask.");bulb.TrySetControl(0);RestPipette();
            string oldId=sampleId;double n=P("concentration")/(P("dilution")*additionalDilution)*.010;
            currentFlask="vol100";dilutionTransferred=true;WaterToMark();Must(flask.TrySetStopperOpen(false),"Close the flask before mixing.");
            // Transfer the homogeneous dilute solution back to the emptied sample
            // cup, conserving its 100 mL and the measured aliquot's solute amount.
            waste.Add(source.Remove(source.VolumeMl));Fill(source,100);Must(flask.TrySetStopperOpen(true)&&flask.TrySetVolume(0),"Complete the dilution transfer before measuring.");
            additionalDilution*=10;sampleId=NewSampleId("pH");samples.Add(new Sample{id=sampleId,sourceSampleId=oldId,solute=P("base")>.5?"NaOH":"HCl",moles=n,ml=100,temperatureC=25});
            probeIn=paperUsed=rinsed=false;I<VlabIndicatorPaper>("paper").ResetReading();ProbeAt("rinse");
        }
        void SerialDilution()
        {
            Must(id=="C05"&&mixed&&samples.Count<3,"Finish and label the current solution before taking another serial aliquot.");
            string oldKey=currentFlask,newKey=currentFlask=="vol50"?"vol100":"vol50";
            var source=I<VlabVolumetricFlask>(oldKey);var receiver=I<VlabVolumetricFlask>(newKey);var waste=I<VlabBeaker>("waste");
            Must(source.Liquid.VolumeMl>=20&&waste.CapacityMl-waste.VolumeMl>=receiver.Liquid.VolumeMl+10,"Insufficient source solution or waste capacity for serial transfer.");
            Must(source.TrySetStopperOpen(true)&&receiver.TrySetStopperOpen(true),"Open both volumetric stoppers.");
            waste.Add(receiver.Liquid.VolumeMl);Must(receiver.TrySetVolume(0),"Empty the receiving flask before serial dilution.");
            double concentration=sampleMoles/(source.Liquid.VolumeMl/1000);serialSourceId=sampleId;
            // Rinse once with 10 mL of the identified previous sample.
            var bulb=I<VlabPipetteBulb>("bulb");PipetteAt(source.Liquid.CavityCentreAt(source.Liquid.SurfaceHeightMeters*.55));
            Must(bulb.TrySetControl(2)&&bulb.TryAspirate(source.Liquid,10,out var rinse)&&Math.Abs(rinse-10)<1e-8,"Serial-source pipette rinse failed.");
            PipetteAt(waste.transform.position+Vector3.up*.02f);Must(bulb.TrySetControl(3)&&bulb.TryDispense(waste,BeakerRegion("waste",true),10,out var disposal)&&Math.Abs(disposal-10)<1e-8,"Serial rinse disposal failed.");
            PipetteAt(source.Liquid.CavityCentreAt(source.Liquid.SurfaceHeightMeters*.55));Must(bulb.TrySetControl(2)&&bulb.TryAspirate(source.Liquid,10,out var aliquot)&&Math.Abs(aliquot-10)<1e-8,"Serial 10 mL aspiration failed.");
            PipetteAt(receiver.Liquid.CavityCentreAt(.02));Must(bulb.TrySetControl(3)&&bulb.TryDispense(receiver.Liquid,10,out var transferred)&&Math.Abs(transferred-10)<1e-8,"Serial transfer must enter the actual receiving flask.");bulb.TrySetControl(0);RestPipette();
            // Archive retains composition at production; consumption is separately
            // identifiable by sourceSampleId and the real source vessel volume.
            sampleMoles=concentration*.010;currentFlask=newKey;filledMark=mixed=false;dilutionTransferred=true;sampleId="";
        }
        string DetailJson()
        {
            var d=new Details{lesson=id,titres=new List<Titre>(titres),samples=new List<Sample>(samples),curve=new List<Point>(curve),sampleId=sampleId,initialMassG=initialMass,finalMassG=finalMass,actualCurrentA=id=="C03"?P("current"):0,chargeC=charge,initialMeniscus=initialMeniscus,finalMeniscus=id=="C01"?I<VlabBurette>("burette").ReadingMl:0,deliveredMl=delivered,naclUnits=naclUnits,nitrateUnits=nitrateUnits,waterUnits=waterUnits,naclSolid=naclSolid};
            if(id=="C06")d.indicatorInterval=I<VlabIndicatorPaper>("paper").IntervalLabel;
            if(id=="C02"){d.reversedLeads=reversed;d.saltBridgeImmersed=bridgeOn&&I<VlabSaltBridge>("bridge").BothEndsImmersed;}
            if(id=="C06"){d.waterObserved=waterObserved;d.waterPh=waterPh;}
            if(id=="C08"){var c=I<VlabCalorimeter>("cal");d.acidMl=V("acidMl",25);d.baseMl=V("baseMl",25);d.acidM=V("acidM",.5);d.baseM=V("baseM",.5);d.acidC=V("acidC",25);d.baseC=V("baseC",25);d.cupC=V("cupC",25);d.cupJPerK=V("cup",20);d.temperatureC=c.TemperatureC;d.peakC=c.PeakTemperatureC;d.releasedJ=c.ReleasedHeatJ;d.lostJ=c.LostHeatJ;}
            d.assumptions=id=="C04"?"Qualitative; 2 simulation units per transfer, no molar conversion":id=="C07"?"Finite-substrate illustrative kinetics; calibrated chip geometry; ideal gas 298.15K/1atm":id=="C08"?"SDK57000J/mol;rho1g/mL;c4.18J/g/K;2s mixing;lid thermal leak":"Ideal dilute aqueous lesson; delivered instrument limits apply";return JsonUtility.ToJson(d);
        }
        public override string SaveState()=>JsonUtility.ToJson(new Draft{lesson=id,titres=new List<Titre>(titres),samples=new List<Sample>(samples)});
        public override void RestoreState(string json)
        {
            // Never restore running flow, power, pressure, calibration or fluid.
            if(string.IsNullOrWhiteSpace(json)||json.Length>100000)return;
            try{var d=JsonUtility.FromJson<Draft>(json);if(d==null||d.version!=1||d.lesson!=id)return;titres.Clear();samples.Clear();
                if(d.titres!=null)foreach(var t in d.titres.Take(100))if(t!=null&&Finite(t.initial)&&Finite(t.final)&&Finite(t.delivered)&&Finite(t.acidM)&&t.initial>=0&&t.final<=25&&t.final>=t.initial&&Math.Abs(t.final-t.initial-t.delivered)<1e-6&&t.acidM>=0){if(t.reason=="current")t.reason="accepted";titres.Add(t);}
                if(d.samples!=null)foreach(var s in d.samples.Take(100))if(s!=null&&!string.IsNullOrEmpty(s.id)&&s.id.Length<=128&&Finite(s.moles)&&Finite(s.ml)&&s.moles>=0&&s.ml>0&&s.ml<=100)samples.Add(s);
            }catch(ArgumentException){}paused=true;
        }
    }
}
