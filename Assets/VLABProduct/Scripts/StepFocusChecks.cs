#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vlab.Product;
public static class StepFocusChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run(VlabApp app,Action<string,bool> check)
    {
        if(!app.Apparatus||!app.Apparatus.Ready)return;
        var camera=app.View;var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var origin=camera.transform.parent;var originPosition=origin.position;var originRotation=origin.rotation;var localPosition=camera.transform.localPosition;var localRotation=camera.transform.localRotation;
        var item=app.Apparatus.items.First(x=>x.installed&&x.gameObject.activeInHierarchy);var itemPosition=item.transform.position;var itemScale=item.transform.localScale;
        object Call(string method,params object[] args)=>typeof(VlabApp).GetMethod(method,Private).Invoke(app,args);
        bool SameView()=>Vector3.Distance(position,camera.transform.position)<.0001f&&Quaternion.Angle(rotation,camera.transform.rotation)<.001f&&Mathf.Abs(fov-camera.fieldOfView)<.001f;
        try
        {
            Call("RefreshActivity");check("Routine activity refresh preserves camera pose and lens",SameView());
            Call("ShowVariables");check("Opening controls preserves camera pose and lens",SameView());Call("CloseModal");check("Closing controls preserves camera pose and lens",SameView());
            app.ShowInstrument(item);check("Opening instrument preserves camera pose and lens",SameView());Call("CloseModal");check("Closing instrument preserves camera pose and lens",SameView());
            if(app.Input.VrMode||app.Input.SimulatorMode){Call("FocusInstrument",item,false);check("Phone focus cannot overwrite tracked camera",SameView());}
            else
            {
                Call("FocusStation");check("Explicit station focus preserves standing eye height",Mathf.Abs(camera.transform.position.y-position.y)<.0001f);
                check("Explicit phone framing stays within vertical FOV 70 to 120",camera.fieldOfView>=70&&camera.fieldOfView<=120);
                var focused=camera.transform.position;var aimed=camera.transform.rotation;
                Call("ZoomPhone",-1000f);check("Phone close inspection reaches 35 degrees without moving learner",Mathf.Abs(camera.fieldOfView-35)<.001f&&camera.transform.position==focused&&camera.transform.rotation==aimed);
                Call("ZoomPhone",1000f);check("Phone zoom clamps to 120 without moving learner",Mathf.Abs(camera.fieldOfView-120)<.001f&&camera.transform.position==focused&&camera.transform.rotation==aimed);
            }
            check("Camera controls preserve native apparatus placement and scale",item.transform.position==itemPosition&&item.transform.localScale==itemScale);
        }
        finally{origin.SetPositionAndRotation(originPosition,originRotation);camera.transform.SetLocalPositionAndRotation(localPosition,localRotation);if(!app.Input.VrMode&&!app.Input.SimulatorMode)app.Input.FocusAtWorldTarget(position+rotation*Vector3.forward);camera.fieldOfView=fov;}
    }
}
#endif
