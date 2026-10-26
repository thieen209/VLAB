#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

public static class LiquidPourTransferChecks
{
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    [MenuItem("VLAB/Checks/Liquid transfer core (not lesson input)")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play mode so delivered beaker Awake has initialized geometry.");
        GameObject a=null,b=null,block=null,bottleObject=null;
        try
        {
            var prefab=Resources.Load<GameObject>("Equipment/VLAB_beaker_100ml");
            Check(prefab,"Delivered 100 mL beaker is missing.");
            a=UnityEngine.Object.Instantiate(prefab);b=UnityEngine.Object.Instantiate(prefab);
            var source=a.GetComponent<VlabBeaker>();var target=b.GetComponent<VlabBeaker>();
            var spout=a.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith("_PourSpout_SOCKET",StringComparison.Ordinal));
            Vector3 mouthLocal=new Vector3(0,0,-.0695f); // Authored 100 mL beaker lip.
            var s=new LiquidPourTransfer.Vessel{liquid=source,localUp=Vector3.back,localMouth=mouthLocal,outlet=spout,mouthRadiusM=(float)Math.Sqrt(source.CrossSectionArea/Math.PI),open=true};
            var t=new LiquidPourTransfer.Vessel{liquid=target,localUp=Vector3.back,localMouth=mouthLocal,mouthRadiusM=(float)Math.Sqrt(target.CrossSectionArea/Math.PI),open=true};
            b.transform.position=new Vector3(100,10,100); // Isolate from scene apparatus.
            a.transform.rotation=Quaternion.Euler(180,0,0);
            a.transform.position+=b.transform.TransformPoint(mouthLocal)+Vector3.up*.12f-spout.position;
            source.Add(25);target.Add(90);
            double Move(double amount)=>LiquidPourTransfer.Transfer(s,t,amount,out _);
            Check(Math.Abs(LiquidPourTransfer.AllowedVolume(s,t,20,out _)-10)<1e-8,"Read-only preflight caps the allowed dose.");
            Check(source.VolumeMl==25&&target.VolumeMl==90,"Read-only geometry preflight does not mutate either volume.");
            Check(Move(double.NaN)==0,"Invalid volume must not mutate state.");
            t.open=false;Check(Move(5)==0,"Closed receiver must reject flow.");t.open=true;
            a.transform.position+=Vector3.right*.08f;Check(Move(5)==0,"A stream outside the mouth must not transfer.");a.transform.position-=Vector3.right*.08f;
            var tilted=a.transform.rotation;a.transform.rotation=Quaternion.Euler(90,0,0);Check(Move(5)==0,"Upright source must not pour.");a.transform.rotation=tilted;
            block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*.025f;block.transform.position=spout.position+Vector3.down*.06f;
            Check(Move(5)==0,"An intervening collider must stop flow.");
            block.transform.position=spout.position;Check(Move(5)==0,"Contact enclosing the outlet must stop flow.");block.SetActive(false);
            Check(Math.Abs(source.VolumeMl-25)<1e-8&&Math.Abs(target.VolumeMl-90)<1e-8,"Rejected geometry must preserve both volumes.");
            Check(Math.Abs(Move(20)-10)<1e-8,"Receiver capacity must cap accepted transfer.");
            Check(Math.Abs(source.VolumeMl+target.VolumeMl-115)<1e-8&&Math.Abs(source.VolumeMl-15)<1e-8,"Accepted transfer must conserve finite volume.");
            Check(Move(1)==0,"Full receiver must stop flow.");target.Remove(100);
            Check(Math.Abs(Move(100)-15)<1e-8&&source.VolumeMl==0,"Source exhaustion must cap transfer without creating liquid.");
            var bottlePrefab=Resources.Load<GameObject>("Equipment/VLAB_reagent_bottle_250ml");
            Check(bottlePrefab,"Delivered stock bottle is missing.");bottleObject=UnityEngine.Object.Instantiate(bottlePrefab);
            var bottle=bottleObject.GetComponent<VlabReagentBottle>();
            Check(bottle.TrySetCapOpen(true)&&bottle.TryConfigure("NaCl",1,"mol/L")&&bottle.TrySetVolume(25),"Upright stock-bottle fixture must accept its explicit volume.");
            bottleObject.transform.rotation=Quaternion.AngleAxis(90,Vector3.right)*bottleObject.transform.rotation;
            Check(!bottle.TrySetVolume(20)&&Math.Abs(bottle.Liquid.VolumeMl-25)<1e-8,"Documented integration blocker: existing flask-backed stock bottle rejects tilted volume mutation without losing liquid.");
            Debug.Log("LIQUID_TRANSFER_CORE_PASS: geometry/contact/capacity/conservation and stock-bottle tilt limitation only; NOT WIRED to lesson input or chemistry.");
        }
        finally{if(a)UnityEngine.Object.DestroyImmediate(a);if(b)UnityEngine.Object.DestroyImmediate(b);if(block)UnityEngine.Object.DestroyImmediate(block);if(bottleObject)UnityEngine.Object.DestroyImmediate(bottleObject);}
    }
}
#endif
