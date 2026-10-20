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
        if(app.Input.VrMode||app.Input.SimulatorMode||!app.Apparatus||!app.Apparatus.Ready)return;
        var item=app.Apparatus.items.First(x=>x.installed&&x.gameObject.activeInHierarchy);
        var camera=app.View;var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        bool assist=app.Profile.settings.focusAssist;var field=typeof(VlabApp).GetField("lastFocusedTaskTarget",Private);var prior=field.GetValue(app);
        var auto=typeof(VlabApp).GetMethod("FocusStepTarget",Private);var explicitFocus=typeof(VlabApp).GetMethod("FocusInstrument",Private);
        var task=new GuideTask("instrument",item.assetId,"","","");var itemPosition=item.transform.position;var itemScale=item.transform.localScale;
        try
        {
            field.SetValue(app,null);app.Profile.settings.focusAssist=false;
            auto.Invoke(app,new object[]{task});check("Automatic step focus off preserves camera",camera.transform.position==position&&camera.transform.rotation==rotation&&camera.fieldOfView==fov);
            explicitFocus.Invoke(app,new object[]{item,false});check("Explicit inspection stays available with assist off without moving camera or model",camera.transform.position==position&&item.transform.position==itemPosition&&item.transform.localScale==itemScale&&camera.fieldOfView>=20&&camera.fieldOfView<=app.Profile.settings.cameraFov);
            field.SetValue(app,null);app.Profile.settings.focusAssist=true;auto.Invoke(app,new object[]{task});
            check("Automatic focus consumes the meaningful target once",(string)field.GetValue(app)==item.assetId);
            camera.transform.rotation=Quaternion.Euler(7,29,0);camera.fieldOfView=47;auto.Invoke(app,new object[]{task});
            check("Repeated refresh preserves manual look and zoom on same target",Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(7,29,0))<.001f&&Mathf.Abs(camera.fieldOfView-47)<.001f&&camera.transform.position==position);
            check("Step framing never rescales or moves native apparatus",item.transform.position==itemPosition&&item.transform.localScale==itemScale);
            if(app.Session.spec.id=="P03")
            {
                var meter=app.Apparatus.items.Single(x=>x.assetId=="meter");explicitFocus.Invoke(app,new object[]{meter,true});
                bool visible=true;
                foreach(var part in app.Apparatus.items.Where(x=>x.installed&&(x.assetId=="surface"||x.assetId=="block"||x.assetId=="meter")))
                    if(ApparatusRig.TryVisibleBounds(part,out var bounds))
                        for(int i=0;i<8;i++)
                        {
                            var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                            var v=camera.WorldToViewportPoint(point);visible&=v.z>0&&v.x>=.01f&&v.x<=.52f&&v.y>=.02f&&v.y<=.98f;
                        }
                check("P03 block, complete travel surface and force meter all fit beside controls",visible);
            }

            var variables=typeof(VlabApp).GetMethod("ShowVariables",Private);var close=typeof(VlabApp).GetMethod("CloseModal",Private);
            Quaternion returnRotation=camera.transform.rotation;float returnFov=camera.fieldOfView;
            variables.Invoke(app,null);
            check("Phone variable sheet remembers activity view",(bool)typeof(VlabApp).GetField("toolSheetFocusSaved",Private).GetValue(app));
            camera.transform.rotation=Quaternion.Euler(9,31,0);camera.fieldOfView=43;
            variables.Invoke(app,null);
            check("Variable rebuild preserves user view instead of reframing",Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(9,31,0))<.001f&&Mathf.Abs(camera.fieldOfView-43)<.001f);
            close.Invoke(app,null);
            check("Closing tool sheet restores prior activity view",Quaternion.Angle(camera.transform.rotation,returnRotation)<.001f&&Mathf.Abs(camera.fieldOfView-returnFov)<.001f);

        }
        finally{camera.transform.position=position;app.Input.FocusAtWorldTarget(position+rotation*Vector3.forward);camera.fieldOfView=fov;app.Profile.settings.focusAssist=assist;field.SetValue(app,prior);}
    }
}
#endif
