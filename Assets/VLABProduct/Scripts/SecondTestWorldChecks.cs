#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Vlab.Product;

// Inspects the room reached by the UI harness; never installs or prepares apparatus.
public static class SecondTestWorldChecks
{
    // Direct state regression; this does not replace the P03 touchscreen journey.
    public static void FrictionRetry(Action<string,bool> check)
    {
        var root=new GameObject("Isolated P03 pull retry");
        try
        {
            var session=new ExperimentSession("P03");var rig=root.AddComponent<ApparatusRig>();
            rig.Build(session,new Vector3(1000,.8f,1000));
            foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new InvalidOperationException("P03 fixture install failed: "+item.assetId);}
            while(session.Phase==ExperimentPhase.Preparing)if(!rig.PrepareNext(out var prepReason))throw new InvalidOperationException(prepReason);
            if(!session.Start(out var startWhy))throw new InvalidOperationException(startWhy);
            session.Advance(.001); // Authority begins on its first running frame, as the app does.
            var driver=rig.Driver;
            check("P03 retry rejects reset before tile end",!driver.Controls("meter").Single(c=>c.id=="resetPull").enabled&&!driver.Operate("resetPull",out _));
            int increments=(int)Math.Floor(session.parameters["mass"]*ScienceModels.G*session.parameters["muS"]/.05)+1;
            for(int i=0;i<increments;i++){if(!driver.Operate("pull+",out var firstPullReason))throw new InvalidOperationException(firstPullReason);session.Advance(.001);}
            session.Advance(2);
            check("P03 missed steady-pull window stays unrecordable and offers retry",!driver.CanRecord(out _)&&driver.Controls("meter").Single(c=>c.id=="resetPull").enabled);
            check("P03 explicit retry succeeds",driver.Operate("resetPull",out _));
            var reset=driver.Observe();
            check("P03 retry clears old force, peak and steady measurements",reset.Get("currentPull")==0&&reset.Get("staticPeak")==0&&reset.Get("steadyInterval")==0&&!driver.CanRecord(out _));
            session.Advance(.6);
            check("P03 waiting after reset cannot fabricate a new measurement",!driver.CanRecord(out _));
            for(int i=0;i<increments;i++){if(!driver.Operate("pull+",out var repeatPullReason))throw new InvalidOperationException(repeatPullReason);session.Advance(.001);}
            session.Advance(.2); // Allow real acceleration before the learner chooses slow, steady hand control.
            check("P03 repeat must genuinely break away before steady control",driver.Controls("meter").Single(c=>c.id=="steady").enabled&&driver.Operate("steady",out _));
            check("P03 new steady action alone is insufficient",!driver.CanRecord(out _));
            var block=rig.items.Single(item=>item.assetId=="block");var before=block.transform.position;
            session.Advance(.6);
            float travel=Vector3.Distance(before,block.transform.position);
            check("P03 slow-steady hand control preserves visible motion at at most 0.05 m/s",travel>0&&travel<=.0301f);
            check("P03 repeat records only after new peak and measured steady interval",driver.CanRecord(out _)&&driver.Observe().Get("staticPeak")>0&&driver.Observe().Get("steadyInterval")>=.5);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);Physics.SyncTransforms();}
    }
    // Direct API regression for the shared carry state, separate from the UI journey matrix.
    public static void StoredRetrieval(Action<string,bool> check)
    {
        var root=new GameObject("Isolated B03 stored-item retrieval");
        try
        {
            var rig=root.AddComponent<ApparatusRig>();var session=new ExperimentSession("B03");
            rig.Build(session,new Vector3(1000,.8f,1000));
            var item=rig.items.Single(i=>i.assetId=="ruler");var other=rig.items.First(i=>i!=item);
            var pointer=new Ray(new Vector3(1000,1.5f,999),Vector3.forward);
            rig.Select(item);var tray=item.trayPosition;var rotation=item.trayRotation;int operation=session.Operation;
            item.stowed=true;item.gameObject.SetActive(false);
            check("Stored ruler can be taken into the hand",rig.TakeSelected(pointer,out _)&&rig.IsCarrying&&item.gameObject.activeSelf&&!item.stowed&&!item.installed&&Vector3.Distance(item.transform.position,pointer.GetPoint(.65f))<.001f);
            check("Taking again does not lose storage return state",rig.TakeSelected(pointer,out _));
            rig.CancelGrab();
            check("Cancel restores stored/inactive ruler and original tray pose",item.stowed&&!item.gameObject.activeSelf&&item.transform.position==tray&&item.transform.rotation==rotation&&!rig.IsCarrying&&rig.Selected==null);
            check("Storage retrieval never advances preparation",session.Operation==operation);
            rig.Select(item);item.stowed=true;item.gameObject.SetActive(false);rig.TakeSelected(pointer,out _);rig.Select(other);
            check("Changing selection returns previously stored ruler to storage",item.stowed&&!item.gameObject.activeSelf&&!rig.IsCarrying&&rig.Selected==other);
            rig.Select(item);item.stowed=true;item.gameObject.SetActive(false);rig.TakeSelected(pointer,out _);
            check("Stored ruler still uses validated explicit installation",rig.CanInstallSelected(out _)&&rig.InstallSelected()&&item.installed&&!item.stowed&&item.gameObject.activeSelf&&!rig.IsCarrying);
            rig.CancelGrab();
            check("Cancel after installation cannot re-stow fitted ruler",item.installed&&!item.stowed&&item.gameObject.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);Physics.SyncTransforms();}
    }
    public static void Run(VlabApp app,Action<string,bool> check)
    {
        Movement(check);
        foreach(var fixture in UnityEngine.Object.FindObjectsByType<LabFixture>(FindObjectsSortMode.None))
        {
            if(fixture.purpose==LabFixture.Purpose.Exit||fixture.purpose==LabFixture.Purpose.Decoration)
            {
                bool open=fixture.isOpen;fixture.Activate();
                check(fixture.name+" exit/decoration cannot open the physical fixture",fixture.isOpen==open);
                check(fixture.name+" decoration has no interact affordance",fixture.purpose!=LabFixture.Purpose.Decoration||!fixture.CanInteract);
            }
        }
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.name=="Soft daylight"||l.name=="Ceiling illumination"))
            check(light.name+" finite nonnegative intensity",!float.IsNaN(light.intensity)&&!float.IsInfinity(light.intensity)&&light.intensity>=0);

        var rig=app.Apparatus;if(!rig)return;
        foreach(var item in rig.items.Where(i=>i.GetComponentInChildren<VlabBalance>()))
        {
            var balance=item.GetComponentInChildren<VlabBalance>();
            var source=Resources.Load<GameObject>("Equipment/VLAB_balance");
            check(rig.Session.spec.id+" balance keeps native unit scale",source&&(item.transform.localScale-source.transform.localScale).sqrMagnitude<1e-8f);
            check(rig.Session.spec.id+" balance pan is level",Vector3.Dot(balance.PanCenter.up,Vector3.up)>.999f);
            check(rig.Session.spec.id+" balance display keeps authored facing",source&&Quaternion.Angle(item.transform.rotation,source.transform.rotation)<.1f);
            if(!item.gameObject.activeInHierarchy||rig.IsCarrying)continue;
            bool supported=false;
            if(ApparatusRig.TryVisibleBounds(item,out var bounds))
            {
                foreach(var top in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.GetComponentInParent<EquipmentHandle>()&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&r.bounds.max.y>.70f&&r.bounds.max.y<1.15f&&(r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)))
                {
                    var b=top.bounds;
                    if(bounds.min.x>=b.min.x-.003f&&bounds.max.x<=b.max.x+.003f&&bounds.min.z>=b.min.z-.003f&&bounds.max.z<=b.max.z+.003f&&Mathf.Abs(bounds.min.y-b.max.y)<=.012f){supported=true;break;}
                }
            }
            check(rig.Session.spec.id+" visible balance rests wholly on an actual room bench",supported);
        }
    }
    // Small isolated physics regressions; these are not phone-input or UI playthroughs.
    public static void Movement(Action<string,bool> check)
    {
        foreach(int yaw in new[]{0,90,180,270})foreach(int pitch in new[]{-89,0,89})
        {
            var view=Quaternion.Euler(pitch,yaw,0);
            var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            var right=Vector3.Cross(Vector3.up,forward);
            check("Movement yaw "+yaw+" pitch "+pitch+" stays horizontal and camera-relative",
                Vector3.Distance(PhoneLocomotion.Direction(view,Vector2.up),forward)<.001f&&
                Vector3.Distance(PhoneLocomotion.Direction(view,Vector2.right),right)<.001f&&
                Vector3.Distance(PhoneLocomotion.Direction(view,Vector2.down),-forward)<.001f&&
                Vector3.Distance(PhoneLocomotion.Direction(view,Vector2.left),-right)<.001f);
        }
        var origin=new GameObject("Isolated movement origin");
        var wall=new GameObject("Isolated movement wall");
        var equipment=new GameObject("Isolated movement held equipment");
        var floor=new GameObject("Isolated movement floor");
        try
        {
            var at=new Vector3(1000,2,1000);origin.transform.position=at;
            wall.transform.position=at+new Vector3(.5f,-.6f,0);
            var box=wall.AddComponent<BoxCollider>();box.size=new Vector3(.05f,2,4);
            Physics.SyncTransforms();
            var moved=PhoneLocomotion.Move(origin.transform,new Vector3(1,0,1),out var hit);
            check("Movement preserves wall blocking and slides along its face",hit==box&&moved.x<at.x+.31f&&moved.z>at.z+.9f);
            box.enabled=false;
            equipment.transform.position=at+new Vector3(.5f,-.6f,0);
            equipment.AddComponent<EquipmentHandle>();equipment.AddComponent<BoxCollider>().size=Vector3.one;
            Physics.SyncTransforms();
            moved=PhoneLocomotion.Move(origin.transform,Vector3.right,out hit);
            check("Equipment collider cannot become an invisible movement wall",hit==null&&Vector3.Distance(moved,at+Vector3.right)<.001f);
            floor.transform.position=new Vector3(1000,-.05f,1000);floor.AddComponent<BoxCollider>().size=new Vector3(20,.1f,20);origin.transform.position=new Vector3(1000,1.1f,1000);Physics.SyncTransforms();
            moved=PhoneLocomotion.Move(origin.transform,Vector3.right,out hit);check("Low bench camera can walk without floor overlap trapping capsule",hit==null&&Vector3.Distance(moved,origin.transform.position+Vector3.right)<.001f);
            box.enabled=true;Physics.SyncTransforms();moved=PhoneLocomotion.Move(origin.transform,Vector3.right,out hit);check("Low bench camera still respects real furniture walls",hit==box&&moved.x<origin.transform.position.x+.31f);
            box.enabled=false;floor.transform.position+=Vector3.up*.6f;Physics.SyncTransforms();moved=PhoneLocomotion.Move(origin.transform,Vector3.right,out hit);check("Low camera walking uses actual raised floor height",hit==null&&Vector3.Distance(moved,origin.transform.position+Vector3.right)<.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(origin);UnityEngine.Object.DestroyImmediate(wall);UnityEngine.Object.DestroyImmediate(equipment);UnityEngine.Object.DestroyImmediate(floor);Physics.SyncTransforms();
        }
    }
}
#endif
