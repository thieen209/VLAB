#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator GasPourJourney()
    {
        App.ShowHome();yield return Wait("home");yield return Tap("Subject C");yield return Wait("browser");yield return Tap("Browser lesson C07");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        var rig=App.Apparatus;for(int i=0;i<30&&!rig.Ready;i++){rig.Select(rig.NextInstallableItem());Check("C07 apparatus fixture installation",rig.InstallSelected());}
        var driver=(ChemistryDriver)rig.Driver;var flaskItem=rig.items.Single(i=>i.assetId=="flask");var flask=flaskItem.GetComponentInChildren<VlabFlask>();
        var acidItem=rig.items.Single(i=>i.assetId=="acid");var acid=acidItem.GetComponentInChildren<VlabReagentBottle>().Liquid;
        Check("C07 cannot prepare through a closed empty flask",!rig.PrepareNext(out _)&&flask.VolumeMl==0&&acid.VolumeMl==80);
        App.ShowActivity();Call("FocusStation");yield return new WaitForSecondsRealtime(.4f);
        Call("SelectWorkbenchTool",flaskItem,"");yield return Tap("Instrument control openGasFlask");yield return Tap("Close equipment controls");
        Check("C07 actual stopper control opens receiver",driver.GasReceiverOpen);
        Check("C07 stopper rests on clear real support",rig.TryRestInstalledTool(rig.items.Single(i=>i.assetId=="stopper"),out _));
        var hide=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Ẩn UI"||b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Hide UI");if(hide)yield return Tap(hide.name);
        var camera=App.View.transform.rotation;int finger=0;Vector2 pointer=default;
        IEnumerator PourUntil(Func<bool> done,string label)
        {
            pointer=ModelPoint(acidItem);finger=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.15f);
            Check("C07 touch owns actual HCl bottle",acidItem.Owner==EquipmentGrabOwner.Flagship);
            var screen=App.View.WorldToScreenPoint(flask.Mouth+Vector3.up*.10f);Check("C07 mouth target is visible",screen.z>0&&screen.x>0&&screen.x<Screen.width&&screen.y>0&&screen.y<Screen.height);
            var from=pointer;pointer=screen;for(int n=1;n<=12;n++){InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=Vector2.Lerp(from,pointer,n/12f)});yield return new WaitForSecondsRealtime(.03f);}
            float until=Time.realtimeSinceStartup+20;while(!done()&&Time.realtimeSinceStartup<until)yield return null;
            Check(label,done());Check("C07 camera stays under learner control",Quaternion.Angle(camera,App.View.transform.rotation)<.1f);
        }
        IEnumerator Release()
        {
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.2f);
            Check("C07 release clears ownership and stream",acidItem.Owner==EquipmentGrabOwner.None&&(!Read<VlabLiquidStream>("pourStream")||!Read<VlabLiquidStream>("pourStream").Flowing));
            Check("C07 finite source plus receiver volume conserved",Math.Abs(acid.VolumeMl+flask.VolumeMl-80)<1e-7);
        }
        yield return PourUntil(()=>flask.VolumeMl>=4,"C07 actual touch delivers partial acid");yield return Capture("C07-manual-acid-pour");yield return Release();
        double partial=flask.VolumeMl;yield return new WaitForSecondsRealtime(.3f);Check("C07 release preserves partial dose",flask.VolumeMl==partial&&acid.VolumeMl>0);
        Check("C07 incomplete dose cannot advance preparation",!rig.PrepareNext(out _)&&flask.VolumeMl==partial);
        yield return PourUntil(()=>driver.GasAcidComplete,"C07 regrab completes prescribed finite dose");yield return Release();
        Check("C07 completed dose cannot overpour",!driver.CanManualPour("acid")&&driver.TransferManualPour(1,out _)==0);
        Check("C07 confirmed dose permits existing weighing step",rig.PrepareNext(out _));Check("C07 existing circuit closes after actual dose",rig.PrepareNext(out _));Check("C07 receiver no longer open after sealing",!driver.GasReceiverOpen);
        Check("C07 existing contact step preserved",rig.PrepareNext(out _));Check("C07 starts after manual loading",App.Session.Start(out _));
        for(int i=0;i<300;i++)App.Session.Advance(1);
        Check("C07 measured gas result remains recordable",driver.CanRecord(out _));yield return Capture("C07-manual-acid-complete");
        rows.Add("Installation, later weighing/sealing/contact and 300-second clock are explicit API fixtures. Bottle and stopper interaction used native Editor touch; Android and physical devices remain unverified.");
    }
}
#endif
