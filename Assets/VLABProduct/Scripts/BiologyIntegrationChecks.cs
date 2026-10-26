#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Vlab.Product;

// Invoke Run() on Unity's main thread while Application.isPlaying. This is a synchronous
// apparatus/lesson integration test: explicit scientific clocks advance, never preset readings.
// It does not certify user input, rendered frames, Android, optical calibration or real biology.
public static class BiologyIntegrationChecks
{
    static readonly List<string> lines=new List<string>();
    static readonly List<string> failures=new List<string>();
    static string evidence,runFolder;static int assertions;
    sealed class Case:IDisposable
    {
        public readonly GameObject root;public readonly ExperimentSession session;public readonly ApparatusRig rig;
        public LessonDriver driver=>rig.Driver;
        public Case(string id,Action<ExperimentSession> configure=null,SessionCheckpoint checkpoint=null)
        {
            session=checkpoint==null?new ExperimentSession(id,73):ExperimentSession.Restore(checkpoint);
            configure?.Invoke(session);root=new GameObject("Integration test "+id);rig=root.AddComponent<ApparatusRig>();
            try{rig.Build(session,new Vector3(0,3,10));C(driver is BiologyDriver,id+" actual biology authority");}
            catch{UnityEngine.Object.DestroyImmediate(root);throw;}
        }
        public void Install()
        {
            C(!session.Start(out _),session.spec.id+" start blocked before kit installation");
            foreach(var item in rig.items.ToArray())
            {rig.Select(item);C(rig.InstallSelected(),session.spec.id+" installed "+item.assetId);C((item.transform.lossyScale-Vector3.one).sqrMagnitude<1e-9f,"Native unit scale "+item.assetId);}
            C(rig.Ready&&rig.ValidateAssembly(out _),session.spec.id+" actual kit validated");
        }
        public T Tool<T>(string key)where T:Component=>rig.items.Single(x=>x.assetId==key).GetComponentInChildren<T>(true);
        public EquipmentHandle Handle(string key)=>rig.items.Single(x=>x.assetId==key);
        public void Prep(){C(rig.PrepareNext(out var m),session.spec.id+" preparation "+session.Operation+": "+m);}
        public void AllPrep(){while(session.Operation<session.spec.operationsEn.Length)Prep();}
        public void Act(string id){C(driver.Operate(id,out var m),session.spec.id+" action "+id+": "+m);}
        public void Point(Vector2 uv){C(driver.PointOnInspection("specimen",uv,out var m),session.spec.id+" actual source UV "+uv+": "+m);}
        public void Start(){C(session.Start(out var m),session.spec.id+" started: "+m);}
        public BiologyLessonState State()=>JsonUtility.FromJson<BiologyLessonState>(driver.SaveState());
        public void Dispose(){if(root)UnityEngine.Object.DestroyImmediate(root);}
    }
    static void C(bool condition,string detail)
    {assertions++;if(!condition)throw new InvalidOperationException(detail);lines.Add("PASS "+detail);}
    static void Near(double a,double b,double tolerance,string detail)=>C(Math.Abs(a-b)<=tolerance,detail+" ["+a+", "+b+"]");
    static void RunCase(string name,Action action)
    {
        lines.Add("\nCASE "+name);
        try{action();lines.Add("COMPLETE "+name);}
        catch(Exception e){failures.Add(name+": "+e.Message);lines.Add("FAIL "+name+"\n"+e);}
    }
    public static string Run()
    {
        lines.Clear();failures.Clear();assertions=0;
        evidence=Path.GetFullPath(Path.Combine(Application.dataPath,"../Evidence/Biology"));Directory.CreateDirectory(evidence);
        runFolder=Path.Combine(evidence,"run-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(runFolder);
        lines.Add("Biology apparatus integration | UTC "+DateTime.UtcNow.ToString("O"));
        lines.Add("Native Play-mode API tests with actual Equipment Resources; synchronous explicit experiment clock. No preset measured states.");
        if(!Application.isPlaying)failures.Add("Play mode is required; no cases executed.");
        else
        {
            RunCase("B01 onion preparation and actual structure picking",B01);
            RunCase("B02 source-cell field, salt and actual water replacement",B02);
            RunCase("B03 three independent balance samples",()=>B03(.2,false));
            RunCase("B04 actual bath, aliquot and active gas curve",()=>Gas("B04",false));
            RunCase("B05 actual yeast lag and active gas curve",()=>Gas("B05",false));
            RunCase("B06 capillary, paper and closed developing jar",B06);
            RunCase("B07 full extraction, alcohol layer and gentle spool",()=>B07(false));
            RunCase("B08 all 100 actual atlas IDs and duplicate-safe crops",B08);
            RunCase("B03 fresh water control group",()=>B03(0,true));
            RunCase("B04 heat-inactivated control",()=>Gas("B04",true));
            RunCase("B05 inactive yeast control distinct from zero yeast",()=>Gas("B05",true));
            RunCase("B07 no-alcohol omitted-step control",()=>B07(true));
        }
        string result=(failures.Count==0?"PASS":"FAIL")+" | "+assertions+" assertions | "+failures.Count+" failed cases";
        lines.Add("\n"+result);foreach(var f in failures)lines.Add(f);
        lines.Add("Limits: source instrument/lesson state exercised; native input, actual UI/modal picking, frame rendering, Android/headset, physical optical and biological calibration NOT TESTED.");
        File.WriteAllText(Path.Combine(evidence,"integration-checks.txt"),string.Join("\n",lines),new UTF8Encoding(false));
        if(failures.Count>0)throw new InvalidOperationException(result+". Evidence/Biology/integration-checks.txt\n"+string.Join("\n",failures));
        return result+". Evidence/Biology/integration-checks.txt";
    }
    static Texture2D SourceTexture(Case c,string suffix)
    {
        var renderer=c.Handle("specimen").GetComponentsInChildren<Renderer>(true).Single(x=>x.name.EndsWith(suffix,StringComparison.Ordinal));
        foreach(var m in renderer.sharedMaterials)if(m&&m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")is Texture2D t)return t;
        throw new InvalidOperationException("Supplied texture missing: "+suffix);
    }
    static void PauseAndInputLoss(Case c)
    {
        var before=c.State();double elapsed=c.session.Elapsed;c.session.Pause();c.session.Advance(123);
        var paused=c.State();Near(c.session.Elapsed,elapsed,1e-9,"Pause freezes session time");Near(paused.clock,before.clock,1e-9,"Pause freezes authority clock");
        Near(paused.gasVolume,before.gasVolume,1e-9,"Pause freezes gas reaction");Near(paused.frontMm,before.frontMm,1e-9,"Pause freezes solvent front");Near(paused.sampleC,before.sampleC,1e-9,"Pause freezes sample heat transfer");
        if(c.rig.items.Any(x=>x.assetId=="bath"))C(!c.Tool<VlabWaterBath>("bath").HeaterOn,"Paused bath heater off");
        c.session.Resume();C(c.session.Phase==ExperimentPhase.Running,"Explicit resume returns to running");
        c.session.InputLost();c.session.Advance(37);Near(c.State().clock,before.clock,1e-9,"Input loss freezes authority");C(c.session.Phase==ExperimentPhase.Paused,"Input loss requires explicit resume");c.session.Resume();
    }
    static AttemptRecord Finish(Case c,string description)
    {
        C(c.driver.TryMeasure(out var result,out var diagnostic),c.session.spec.id+" source-backed TryMeasure: "+diagnostic);
        C(result!=null&&result.readings.Count>0&&!string.IsNullOrEmpty(result.detailJson),"Detailed source observation present");
        C(c.session.Measure(out var message),"Session records authority result: "+message);C(c.session.Phase==ExperimentPhase.Measured,"Measured phase reached");
#if UNITY_EDITOR
        SubjectRecordVerifier.Verify(c.session);
#endif
        var record=c.session.Record(description);C(record.observation==description&&record.detailJson==result.detailJson,"Learner observation and detail JSON retained");
        C(record.readings.All(x=>double.IsFinite(x.value)&&!string.IsNullOrEmpty(x.labelEn)&&!string.IsNullOrEmpty(x.labelVi)),"Finite bilingual observation readings");
        // Separate evidence-only profile; never reads or writes the user's real profile.
        string folder=Path.Combine(runFolder,c.session.spec.id+"-"+record.attemptId);var store=new ProfileStore(folder);
        var profile=new LocalProfile{locale="en",checkpoint=c.session.Checkpoint()};profile.attempts.Add(record);
        C(store.Save(profile,out var saveError),"Evidence-only profile saved: "+saveError);var loaded=store.Load();
        C(!loaded.writeProtected&&loaded.profile.attempts.Count==1,"Evidence profile reload accepted");
        var saved=loaded.profile.attempts[0];C(saved.detailJson==record.detailJson&&saved.observation==description&&saved.readings.Count==record.readings.Count,"Reload preserves detailed observation");
        File.WriteAllText(Path.Combine(folder,"attempt.json"),JsonUtility.ToJson(record,true));
        using(var fresh=new Case(c.session.spec.id,null,loaded.profile.checkpoint))
        {
            C(fresh.session.Phase==ExperimentPhase.Preparing&&!fresh.session.ApparatusReady&&fresh.rig.items.All(x=>!x.installed),"Reload requires freshly installed apparatus");
            var state=fresh.State();C(state.clock==0&&state.prep==0&&!state.mounted&&!state.reacting&&!state.alcoholAdded&&!state.spooled,"Reload does not resume unsafe live state");
            C(!fresh.session.Start(out _),"Reload cannot start without fresh preparation");
            if(c.session.spec.id=="B08")C(state.cells.Count==100&&state.cells.Select(x=>x.id).Distinct().Count()==100,"Fixed atlas draft restores 100 unique learner answers");
            if(c.session.spec.id=="B03")C(state.samples.Count==0,"Reload creates no pretend soaked or weighed samples");
        }
        return record;
    }
    static void B01()
    {
        C(LessonSpec.All["B01"].parameters.Single(x=>x.key=="objective").min<=4,"B01 specification permits actual 4× objective");
        using(var c=new Case("B01"))
        {
            c.Install();C(!c.driver.Operate("obj40",out _),"High power blocked before mounted/focused slide");
            var movable=c.Handle("dropper");var original=movable.transform.position;
            C(c.rig.LiftForInspection(movable,new Ray(original+Vector3.up,Vector3.down)),"Movable tool can be lifted");
            movable.transform.position=c.rig.Worktop+new Vector3(.85f,.2f,.3f);float placedX=movable.transform.position.x;
            C(c.rig.ReleaseInspected(out _),"Movable tool can settle on a clear supported bench location");
            C(movable.Owner==EquipmentGrabOwner.None&&Math.Abs(movable.transform.position.x-placedX)<.0001f&&Vector3.Distance(original,movable.transform.position)>.2f,"Normal release retains the new supported location and releases ownership");
            var placed=movable.transform.position;C(c.rig.LiftForInspection(movable,new Ray(placed+Vector3.up,Vector3.down)),"Tool can be picked up again");c.rig.CancelGrab();C(Vector3.Distance(placed,movable.transform.position)<.0001f,"Cancellation returns to the last supported pickup location");
            c.Prep();c.Prep();C(!c.rig.PrepareNext(out _),"B01 panel cannot mount and close the slide automatically");
            var biology=(BiologyDriver)c.driver;C(!biology.PlaceSlideAtCurrentPose(out _),"B01 rejects a slide still on the tray");
            var seat=c.Handle("slide").GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_Base_SOCKET",StringComparison.Ordinal));
            var target=c.Handle("microscope").GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_SlideCenter_SOCKET",StringComparison.Ordinal));
            c.Handle("slide").transform.position+=target.position-seat.position;
            C(biology.PlaceSlideAtCurrentPose(out _),"B01 accepts the actual slide at its stage socket");
            C(!c.rig.PrepareNext(out _),"B01 requires deliberate clip closure");c.Act("clips");c.Prep();c.Act("obj4");
            C(!c.rig.PrepareNext(out _),"Optional 4× does not complete the required 10× step");c.Act("obj10");c.Prep();
            C(c.Tool<VlabOnion>("onion").State==2&&!c.Tool<VlabMicroscope>("microscope").ClipsOpen,"Single epidermis state and both clips closed");
            C(c.driver.Inspection("specimen")!=null,"Actual source inspection texture exposed");c.Start();C(!c.session.Measure(out _),"No automatic learner structure observations");
            var mask=SourceTexture(c,"_CellMasks_VIS");C(mask.isReadable,"Categorical onion mask readable");var pixels=mask.GetPixels32();var selected=new Dictionary<int,Vector2>();
            for(int i=0;i<pixels.Length&&selected.Count<3;i++)
            {var p=pixels[i];if(p.r>=1&&p.r<=64&&p.g>=1&&p.g<=3&&!selected.ContainsKey(p.g))selected[p.g]=new Vector2((i%mask.width+.5f)/mask.width,(i/mask.width+.5f)/mask.height);}
            C(selected.Count==3,"Three actual source structure regions located by test fixture");
            foreach(var pair in selected){c.Point(pair.Value);c.Act("structure:"+(pair.Key-1));}
            c.Act("clips");C(!c.driver.CanRecord(out _)&&c.State().structures.Count==3,"Opening clips blocks recording while retaining the learner's classifications");c.Act("clips");
            c.Act("obj10");C(c.Tool<VlabMicroscope>("microscope").ObjectiveIndex==1,"10× source turret index 1");c.Act("obj40");C(c.Tool<VlabMicroscope>("microscope").ObjectiveIndex==2,"40× source turret index 2");
            float focus=c.Tool<VlabMicroscope>("microscope").FocusMm;C(!c.driver.Operate("focus+",out _)&&c.Tool<VlabMicroscope>("microscope").FocusMm==focus,"40× rejects coarse focus without moving stage");
            for(int n=0;n<20;n++)c.Act("focusFine+");C(!c.driver.TryMeasure(out _,out _),"Out-of-focus high-power record blocked");for(int n=0;n<20;n++)c.Act("focusFine-");PauseAndInputLoss(c);
            var record=Finish(c,"Test learner: wall, membrane and cytoplasm identified in supplied schematic; no real magnification calibration.");
            C(record.readings.Single(x=>x.key=="magnification").value==400,"Nominal magnification correctly reported at 40× objective");
        }
    }
    static Dictionary<int,Vector2> PlasmolysisUvs(VlabPlasmolysis source)
    {
        var uv=new Dictionary<int,Vector2>();for(int y=0;y<128;y++)for(int x=0;x<128;x++)
        {var p=new Vector2((x+.5f)/128,(y+.5f)/128);if(source.TryGetCell(p,out int id)&&!uv.ContainsKey(id))uv.Add(id,p);}
        C(uv.Count==32,"All 32 stable source plasmolysis IDs resolved");return uv;
    }
    static void B02()
    {
        using(var c=new Case("B02"))
        {
            c.Install();c.Prep();C(!c.rig.PrepareNext(out _),"Unmarked field cannot be locked");
            var uv=PlasmolysisUvs(c.Tool<VlabPlasmolysis>("specimen")).OrderBy(x=>x.Key).Take(20).ToArray();
            foreach(var pair in uv){c.Point(pair.Value);c.Act("cellNormal");}c.Prep();c.Prep();c.Start();
            C(!c.driver.Operate("waterReplace",out _),"Water before salt interval rejected");c.session.Advance(c.session.parameters["duration"]+1);
            C(!c.driver.Operate("waterReplace",out _),"Water without manual salt counts rejected");
            // Explicit scripted learner answers test storage and count arithmetic, not texture-derived scientific truth.
            foreach(var pair in uv){c.Point(pair.Value);c.Act(pair.Key%3==0?"cellContracted":"cellNormal");}
            PauseAndInputLoss(c);c.Act("waterReplace");double replacedAt=c.State().waterAt;
            C(c.Tool<VlabPlasmolysis>("specimen").StateIndex==3,"Actual source recovery texture selected");
            foreach(var pair in uv){c.Point(pair.Value);c.Act("cellNormal");}
            C(!c.driver.TryMeasure(out _,out _),"Recovery observation interval starts at real water replacement");c.session.Advance(10.1);
            Near(c.State().clock-replacedAt,10.1,1e-7,"Recovery clock measured from actual replacement");
            var record=Finish(c,"Test learner: same 20 IDs counted at baseline, salt and recovery; source views are untimed illustrations.");
            var state=JsonUtility.FromJson<BiologyLessonState>(record.detailJson);C(state.cells.Count==20&&state.cells.All(x=>x.baseline==0&&x.salt>=0&&x.recovery==0),"Tracked ID/count table preserved through record");
        }
    }
    static readonly HashSet<string> priorPotatoIds=new HashSet<string>();
    static void B03(double concentration,bool waterControl)
    {
        using(var c=new Case("B03",s=>
        {C(s.SetParameter("duration",60,out _),"Set one-minute test soak");C(s.SetParameter("concentration",concentration,out _),"Set concentration group");}))
        {
            c.Install();C(!c.rig.PrepareNext(out _),"Cannot skip three actual initial balance readings");
            C(!c.driver.Operate("weighFinal:0",out _),"Final mass blocked before soak and blot");
            for(int i=0;i<3;i++)c.Act("weighInitial:"+i);
            var initial=c.State();C(initial.samples.Count==3&&initial.samples.Select(x=>x.id).Distinct().Count()==3,"Three independent source sample IDs");
            foreach(var sample in initial.samples){C(!priorPotatoIds.Contains(sample.id),"Fresh concentration group receives new sample ID "+sample.id);priorPotatoIds.Add(sample.id);}
            c.Act("weighInitial:0");C(c.State().samples.Count==3,"Technical reweigh does not create biological replicate");
            c.AllPrep();C(c.Tool<VlabBeaker>("solution").VolumeMl==100&&c.Tool<VlabBeaker>("water").VolumeMl==100,"Actual solution and water reference volumes filled");
            c.Start();C(!c.driver.Operate("blot:0",out _),"Early blotting blocked before sample interval");c.session.Advance(60.1);PauseAndInputLoss(c);
            for(int i=0;i<3;i++)
            {
                C(!c.driver.Operate("weighFinal:"+i,out _),"Unblotted sample "+i+" cannot supply final mass");
                c.Act("blot:"+i);c.Act("weighFinal:"+i);
                var potato=c.Tool<VlabPotatoSample>("sample"+i);C(potato.SampleId==initial.samples[i].id,"Actual source sample ID retained for paired mass "+i);
                if(i<2)C(!c.driver.TryMeasure(out _,out _),"Fewer than three independent paired masses cannot complete group");
            }
            var record=Finish(c,waterControl?"Test learner: fresh independent water control group, same blot protocol.":"Test learner: three separate matched potato samples paired by ID before and after sucrose soaking.");
            var paired=JsonUtility.FromJson<BiologyLessonState>(record.detailJson);
            C(paired.samples.All(x=>x.initialRead&&x.finalRead&&x.blotted&&x.initial>0&&x.final>0&&x.concentration==concentration),"Saved sample ledger contains three complete pairs at one concentration");
            for(int i=0;i<3;i++)Near(record.readings.Single(x=>x.key=="massChange"+(i+1)).value,100*(paired.samples[i].final-paired.samples[i].initial)/paired.samples[i].initial,1e-9,"Mass-change arithmetic from balance pair "+i);
            if(waterControl)C(record.readings.Single(x=>x.key=="meanMassChange").value>0,"Illustrative water control gains mass under configured response");
        }
    }
    static void Gas(string id,bool inactive)
    {
        if(id=="B05")C(LessonSpec.All[id].parameters.Any(x=>x.key=="inactive"),"B05 spec exposes inactive yeast control separately from amount");
        using(var c=new Case(id,s=>
        {C(s.SetParameter("inactive",inactive?1:0,out _),"Select active or inactivated source aliquot");C(s.SetParameter("temperature",35,out _),"Set bath initial-temperature group");}))
        {
            c.Install();c.AllPrep();var bath=c.Tool<VlabWaterBath>("bath");
            C(bath.Sample==c.Tool<VlabBeaker>("aliquot")&&bath.LidOpen,"Actual native beaker mounted in open source bath");
            Near(bath.Sample.CapacityMl,250,1e-9,"Validated native 250 mL beaker used in bath seat");
            Near(bath.Sample.VolumeMl,30,1e-9,"Larger vessel does not invent aliquot prefill or dilution");
            C(c.Tool<VlabTubing>("tube").LogicalEdgeEnabled,"Actual tubing has explicit distinct sealed port IDs");
            Near(bath.BathC,35,1e-6,"Bath temperature set independently");Near(bath.SampleC,25,1e-6,"Sample begins at room temperature");
            c.Start();C(!c.driver.Operate("mix",out _),"Mix blocked before actual sample equilibrates");
            C(!c.driver.Operate("gasRead",out _),"Gas reading blocked before reaction contact");c.session.Advance(20);PauseAndInputLoss(c);c.Act("mix");
            C(bath.Sample==null&&c.Tool<VlabBeaker>("aliquot").VolumeMl==0,"Equilibrated aliquot removed and source beaker emptied at mixing");
            Near(c.Tool<VlabFlask>("flask").VolumeMl,30,1e-9,"Actual reaction flask receives transferred 30 mL");
            c.Act("gasRead");C(!c.driver.Operate("gasRead",out _),"Duplicate same-time gas readings rejected");
            c.session.Advance(10);c.Act("gasRead");
            if(id=="B05")Near(c.Tool<VlabGasSyringe>("syringe").VolumeMl,0,1e-9,"Actual syringe respects illustrative 15 s lag");
            C(!c.driver.TryMeasure(out _,out _),"Insufficient reaction-time series cannot complete");
            c.session.Advance(20.1);c.Act("gasRead");PauseAndInputLoss(c);
            var state=c.State();C(state.gas.Count==3&&state.gas.All(p=>p.sampleId==state.groupId),"Three explicit timed readings share one aliquot ID");
            C(state.gas.Zip(state.gas.Skip(1),(a,b)=>b.seconds>a.seconds&&b.volumeMl>=a.volumeMl).All(x=>x),"Gas time series monotonic and nonnegative");
            if(inactive)Near(c.Tool<VlabGasSyringe>("syringe").VolumeMl,0,1e-8,"Inactive control has zero modeled gas while source amount remains nonzero");
            else C(c.Tool<VlabGasSyringe>("syringe").VolumeMl>0,"Active sample actually moves delivered gas-syringe piston");
            if(id=="B05")C(c.session.parameters["yeast"]>0,"Inactivated yeast is distinct from zero yeast amount");
            var record=Finish(c,"Test learner: three explicit syringe/time readings from "+(inactive?"inactivated":"active")+" identified sample; bath and transferred sample temperatures retained.");
            var b=state.gas[state.gas.Count-1];var a=state.gas[state.gas.Count-2];Near(record.readings.Single(x=>x.key=="intervalRate").value,(b.volumeMl-a.volumeMl)/(b.seconds-a.seconds),1e-8,"ΔV/Δt computed from actual saved interval readings");
        }
    }
    static void B06()
    {
        C(LessonSpec.All["B06"].parameters.Single(x=>x.key=="front").max<=88,"B06 front spec stays within source 88 mm paper limit");
        using(var c=new Case("B06"))
        {
            c.Install();C(!c.driver.Operate("loadCapillary",out _),"Cannot load capillary before extract is filtered");c.Prep();c.Prep();
            var paper=c.Handle("strip").GetComponentsInChildren<VlabChromatography>(true).Single(x=>x.Phase=="Cellulose paper");
            var capillary=c.Tool<VlabCapillary>("capillary");var jar=c.Tool<VlabJar>("jar");
            C(c.Tool<VlabFilterPaper>("filter").IsWet&&c.Tool<VlabBeaker>("receiver").VolumeMl>0,"Actual wet source filter and retained extract");
            C(!c.rig.PrepareNext(out _),"Unspotted strip cannot pass preparation");c.Act("loadCapillary");Near(capillary.VolumeUl,1,1e-8,"Actual 1 µL capillary load");
            c.Act("deposit");C(paper.Spot.Wet&&paper.Spot.SpotCount==1,"Actual paper spot wet after first microdeposit");
            c.Act("loadCapillary");C(!c.driver.Operate("deposit",out _),"Second deposit rejected while actual spot wet");
            c.Act("dry");c.Act("deposit");c.Act("dry");Near(paper.Spot.AppliedUl,2,1e-8,"Source paper cumulative spot limit reached at 2 µL");
            c.Act("loadCapillary");C(!c.driver.Operate("deposit",out _),"Source spot refuses cumulative volume above 2 µL");
            c.Prep();c.Prep();C(jar.ReadyToDevelop(paper)&&!jar.LidOpen,"Actual cellulose paper seated in closed source developing jar");
            C(jar.Liquid.SurfaceHeightMeters<.018,"Actual solvent surface below source paper origin");c.Start();c.session.Advance(30.1);PauseAndInputLoss(c);
            C(paper.HasMeasurement&&paper.FrontDistanceM>0&&paper.FrontDistanceM<=.088,"Actual source front/bands inside physical paper limit");
            C(!c.driver.TryMeasure(out _,out _),"Unmarked solvent front cannot be recorded");c.Act("markFront");double front=paper.FrontDistanceM;c.session.Advance(10);Near(paper.FrontDistanceM,front,1e-9,"Marked front stops development");
            var record=Finish(c,"Test learner: two dried 1 µL deposits; solvent front and illustrative bands measured from pencil origin. No pigment identification claimed.");
            C(record.readings.Where(x=>x.key.StartsWith("rf",StringComparison.Ordinal)).All(x=>x.value>=0&&x.value<=1),"Actual source Rf ratios remain within 0–1");
        }
    }
    static void B07(bool omitAlcohol)
    {
        C(LessonSpec.All["B07"].parameters.Any(x=>x.key=="alcohol"),"B07 specification exposes no-alcohol control");
        using(var c=new Case("B07",s=>C(s.SetParameter("alcohol",omitAlcohol?0:1,out _),"Choose full extraction or omitted-alcohol control")))
        {
            c.Install();C(!c.driver.Operate("spool",out _),"Spooling cannot create material before extraction");c.Prep();
            C(c.Tool<VlabFruit>("tissue").Crushed,"Actual source fruit crushed state");c.Prep();c.Prep();
            var receiver=c.Tool<VlabBeaker>("receiver");C(c.Tool<VlabFilterPaper>("filter").IsWet&&receiver.VolumeMl==20,"Actual filtration retains 20 mL in receiving beaker");
            c.AllPrep();var layer=c.Tool<VlabLiquid>("layer");
            Near(layer.Volume.VolumeMl,omitAlcohol?0:10,1e-9,"Actual delivered liquid asset reflects alcohol-layer control");
            c.Start();C(!c.driver.Operate("spool",out _),"Spooling blocked before precipitation interval");c.session.Advance(10.1);PauseAndInputLoss(c);
            var crude=c.Tool<VlabDnaPrecipitate>("dna");
            if(omitAlcohol){C(!crude.Present,"No-alcohol control shows no source strands");C(!c.driver.Operate("spool",out _),"Absent crude material cannot be spooled");}
            else
            {
                C(crude.Present,"Actual source coarse-strand renderer appears only after full extraction");C(!c.driver.TryMeasure(out _,out _),"Full extraction requires actual gentle spool action");
                c.Act("spool");C(crude.transform.IsChildOf(c.Handle("rod").transform),"Actual precipitate attaches to delivered rod tip");
            }
            var record=Finish(c,omitAlcohol?"Test learner: no-alcohol control retained filtrate; no strands observed.":"Test learner: crude white material at alcohol/filtrate interface gently spooled; no purity or yield inferred.");
            Near(record.readings.Single(x=>x.key=="crudePrecipitate").value,omitAlcohol?0:1,1e-9,"Recorded qualitative control matches actual source renderer");
        }
    }
    sealed class AtlasCell{public int id;public Vector2 uv;public string stage;public bool scorable;}
    static void B08()
    {
        using(var c=new Case("B08"))
        {
            c.Install();c.AllPrep();c.Start();C(!c.driver.TryMeasure(out _,out _),"Atlas never autocompletes learner counts");
            var atlas=c.Tool<VlabMitosisAtlas>("specimen");var cells=new Dictionary<int,AtlasCell>();
            // Test-only expected-answer fixture discovered from the supplied API. Production code has no auto-classifier.
            for(int y=0;y<160;y++)for(int x=0;x<160;x++)
            {var uv=new Vector2((x+.5f)/160,(y+.5f)/160);if(atlas.TryResolve(uv,out int id,out string stage,out bool scorable)&&!cells.ContainsKey(id))cells[id]=new AtlasCell{id=id,uv=uv,stage=stage,scorable=scorable};}
            C(cells.Count==100,"All 100 distinct actual atlas IDs resolved through supplied mask");
            var stages=new[]{"interphase","prophase","metaphase","anaphase","telophase","unclear"};
            foreach(var cell in cells.Values.OrderBy(x=>x.uv.x))
            {
                c.Act(cell.uv.x<.5f?"field2":"field3");var crop=c.driver.InspectionUv("specimen");C(crop.Contains(cell.uv),"Source UV lies within selected crop for global ID "+cell.id);
                c.Point(cell.uv);int answer=cell.scorable?Array.IndexOf(stages,cell.stage):5;C(answer>=0,"Source fixture uses known stage or unclear");c.Act("stage:"+answer);
                if(cell.scorable){C(atlas.TryCheck(cell.id,stages[answer],out bool correct)&&correct,"Source checker validates test fixture for global ID "+cell.id);c.Act("checkStage");}
                if(c.State().cells.Count==99)C(!c.driver.TryMeasure(out _,out _),"99 actual IDs cannot satisfy 100-cell group");
            }
            var first=cells.Values.First();c.Act("field1");c.Point(first.uv);int priorCount=c.State().cells.Count;
            c.Act("stage:"+(first.scorable?Array.IndexOf(stages,first.stage):5));C(c.State().cells.Count==priorCount&&priorCount==100,"Same global ID in overlapping full crop edits answer without duplicate count");
            PauseAndInputLoss(c);var state=c.State();int mitotic=state.cells.Count(x=>x.answer!="interphase"&&x.answer!="unclear"),unclear=state.cells.Count(x=>x.answer=="unclear");
            var record=Finish(c,"Test learner fixture: systematically classified 100 unique atlas IDs across left/right crops, with unclear retained for unscorable source cells. Schematic, not micrograph.");
            Near(record.readings.Single(x=>x.key=="mitoticIndex").value,mitotic,1e-9,"100-cell MI comes from learner classifications");
            Near(record.readings.Single(x=>x.key=="miUpperWithUnclear").value,mitotic+unclear,1e-9,"Unclear cells retained in MI uncertainty bound");
            C(state.cells.Count==cells.Count&&state.cells.Select(x=>x.id).Distinct().Count()==100,"Saved global atlas IDs deduplicated across crops");
        }
    }
}

#endif
