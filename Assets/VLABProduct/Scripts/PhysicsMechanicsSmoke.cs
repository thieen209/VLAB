#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Vlab.Product {
// Invoke from a play-mode smoke runner. No Unity launch, copy or source-project mutation is performed by this helper.
public static class PhysicsMechanicsSmoke {
 sealed class Trial {public string id,name;public Dictionary<string,double> values;public Trial(string id,string name="",params (string key,double value)[] values){this.id=id;this.name=id+name;this.values=values.ToDictionary(x=>x.key,x=>x.value);}}
 public static IEnumerator Run(Action<string> report)=>RunCases(new[]{"P02","P03","P04","P05","P06","P07","P08","M01","M02","M03","M04","M05","M06","M07","M08"}.Select(id=>new Trial(id)).ToArray(),report);
 public static IEnumerator RunFocused(Action<string> report)=>RunCases(new[]{new Trial("P07"),new Trial("P08"),new Trial("P08"," stalled",("mu",.8)),new Trial("M01"," 24→12",("driver",24),("driven",12)),new Trial("M01"," 24→36",("driver",24),("driven",36)),new Trial("M04"," 80→40 open",("d1",80),("d2",40)),new Trial("M04"," 80→40 crossed",("d1",80),("d2",40),("crossed",1))},report);
 static IEnumerator RunCases(Trial[] cases,Action<string> report) {
  foreach(var trial in cases) {string id=trial.id;
   var root=new GameObject("Smoke "+id);bool syntheticReceiving=false;
   if(id=="P02"&&!UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Any(r=>r.name.IndexOf("floor",StringComparison.OrdinalIgnoreCase)>=0&&r.bounds.size.x>2&&r.bounds.size.z>2&&r.bounds.max.y<.25f)) {syntheticReceiving=true;var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Explicit synthetic P02 receiving floor - isolated numerical fixture";floor.transform.SetParent(root.transform,false);floor.transform.position=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(30,.1f,30);}
   var session=new ExperimentSession(id,1);var rig=root.AddComponent<ApparatusRig>();string failure="";bool started=false;
   try {foreach(var p in trial.values)session.parameters[p.Key]=p.Value;rig.Build(session,new Vector3(0,.8f,0));foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new InvalidOperationException("Could not install "+item.assetId);}}catch(Exception e){failure=e.ToString();}
   yield return null; // Delivered Start callbacks must settle before final lesson configuration.
   if(failure=="")try {rig.RefreshConfiguration();while(session.Operation<session.spec.operationsEn.Length)if(!rig.PrepareNext(out var reason))throw new InvalidOperationException(reason);if(!session.Start(out var startReason))throw new InvalidOperationException(startReason);started=true;}catch(Exception e){failure=e.ToString();}
   bool steady=false,measured=false;
   for(int tick=0;started&&failure==""&&tick<6000&&!measured;tick++) {
    try {
     session.Advance(.01);
     if(id=="P03") {if(!steady){var control=rig.Driver.Controls("meter").First(x=>x.id=="steady");if(control.enabled){if(!rig.Driver.Operate("steady",out var reason))throw new InvalidOperationException(reason);steady=true;}else if(!rig.Driver.Operate("pull+",out var reason))throw new InvalidOperationException(reason);}}
     if(id=="M03"&&!rig.Driver.CanRecord(out _))if(!rig.Driver.Operate("pull-rope",out var reason))throw new InvalidOperationException(reason);
     if(id=="M08"&&tick==1)if(!rig.Driver.Operate("unload",out var reason))throw new InvalidOperationException(reason);
     if(rig.Driver.CanRecord(out _)){if(!session.Measure(out var reason))throw new InvalidOperationException(reason);measured=true;if(session.Recorded.readings.Count==0||string.IsNullOrEmpty(session.Recorded.detailJson))throw new InvalidOperationException("Missing readings/evidence series.");CheckMeasurements(session,rig);}
    }catch(Exception e){failure=e.ToString();}
    if(id=="P02")yield return new WaitForFixedUpdate(); // The delivered projectile owns FixedUpdate; never tick it a second time.
   }
   if(!measured&&failure==""){rig.Driver.CanRecord(out failure);failure="Measurement gate: "+failure;}
   report(trial.name+": "+(failure==""?"PASS prepared/action/observation/record journey and measurement provenance"+(syntheticReceiving?" [explicit isolated receiving-floor fixture; actual room envelope checked separately]":""):failure));UnityEngine.Object.Destroy(root);yield return null;
  }
 }
 static void CheckMeasurements(ExperimentSession s,ApparatusRig rig){
  double Read(string key)=>s.Recorded.Get(key);void Equal(double a,double b,string label){if(Math.Abs(a-b)>1e-7)throw new InvalidOperationException(label+": "+a+" != "+b);}
  if(s.spec.id=="P07")Equal(Read("acceleration"),PhysicsMechanicsDriver.AccelerationFromGateIntervals(Read("flagWidth"),Read("gateA"),Read("gateB"),Read("gateSpacing")),"P07 acceleration provenance");
  if(s.spec.id=="P08"){
   if(Read("stalled")>.5){if(s.Recorded.readings.Any(r=>r.key=="speed"||r.key=="kineticEnergy"))throw new InvalidOperationException("Stalled trial fabricated a gate reading");if(Read("observationTime")<.5)throw new InvalidOperationException("Stationary observation too short");Equal(Read("observedDisplacement"),0,"Actual stationary cart");}
   else{Equal(Read("kineticEnergy"),PhysicsMechanicsDriver.GateKineticEnergy(s.parameters["mass"],Read("flagWidth"),Read("gateB")),"P08 energy provenance");if(Read("gateA")<=0||Read("gateB")<=0)throw new InvalidOperationException("P08 missing optical cycle");}
  }
  if(s.spec.id=="M01"){
   Equal(Read("signedRatio"),PhysicsMechanicsDriver.StockGearRatio((int)s.parameters["driver"],(int)s.parameters["driven"]),"Signed observed gear ratio");
   var gear=rig.items.Single(i=>i.assetId=="gears").GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith("_Axis"+(int)s.parameters["driver"]+"_SOCKET",StringComparison.Ordinal));var motor=rig.items.Single(i=>i.assetId=="motor").GetComponentInChildren<VlabMotor>();if(Vector3.Distance(motor.Tip.position,gear.position-motor.Axis*.006f)>.0001f||!rig.ValidateTransmissionSupport(out _))throw new InvalidOperationException("Motor must fully engage the selected gear bore on real supports");
  }
  if(s.spec.id=="M04"){
   var belt=rig.items.Single(i=>i.assetId=="belt").GetComponentInChildren<VlabBelt>();Equal(Read("outputSpeed"),belt.OutputRpm,"Observed belt output turns");if(belt.DriveLarge!=(s.parameters["d1"]==80))throw new InvalidOperationException("Wrong belt input pulley");
   var anchor=belt.Current.root.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith(belt.DriveLarge?"_PulleyB_SOCKET":"_PulleyA_SOCKET",StringComparison.Ordinal));var motor=rig.items.Single(i=>i.assetId=="motor").GetComponentInChildren<VlabMotor>();if(Vector3.Distance(motor.Tip.position,anchor.position-motor.Axis*.006f)>.0001f||!rig.ValidateTransmissionSupport(out _))throw new InvalidOperationException("Motor must fully engage the selected belt bore on real supports");
  }
 }
}
}

#endif
