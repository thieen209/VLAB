#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

public sealed class VlabStockStandAudit:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        var checks=new List<string>();
        try
        {
            var top=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(x=>x.bounds.max.y>.72f&&x.bounds.max.y<1.12f&&x.bounds.size.x>.65f&&x.bounds.size.z>.3f&&(x.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||x.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)).OrderByDescending(x=>x.bounds.size.x*x.bounds.size.z).First().bounds;
            var at=new Vector3(top.center.x,top.max.y+.005f,top.center.z);
            var session=new ExperimentSession("P01",1);var rig=new GameObject("Stock apparatus audit").AddComponent<ApparatusRig>();rig.Build(session,at);
            foreach(var item in rig.items){rig.Select(item);if(!rig.InstallSelected())throw new Exception("Install failed: "+item.assetId);}
            var support=rig.items.Single(x=>x.assetId=="stand");var prefab=Resources.Load<GameObject>("Equipment/VLAB_stand");
            foreach(var mesh in support.GetComponentsInChildren<MeshFilter>())
            {
                var original=prefab.GetComponentsInChildren<MeshFilter>().Single(x=>x.name==mesh.name);
                if(mesh.sharedMesh!=original.sharedMesh||(mesh.transform.localScale-original.transform.localScale).sqrMagnitude>1e-12f)throw new Exception("Stock geometry changed: "+mesh.name);
                checks.Add("Unchanged source mesh and local scale: "+mesh.name);
            }
            var baseBounds=support.GetComponentsInChildren<Renderer>().Single(x=>x.name.EndsWith("_Base_VIS",StringComparison.Ordinal)).bounds;
            float supportedDepth=Mathf.Max(0,Mathf.Min(baseBounds.max.z,top.max.z)-Mathf.Max(baseBounds.min.z,top.min.z));
            if(supportedDepth/baseBounds.size.z<.999f||baseBounds.min.x<top.min.x-.001f||baseBounds.max.x>top.max.x+.001f)throw new Exception("Stand base insufficiently supported at bench edge.");
            checks.Add("Entire original base rests on the worktop; supported base depth "+(supportedDepth/baseBounds.size.z*100).ToString("F1")+"%.");
            for(int step=0;step<session.spec.operationsEn.Length;step++)if(!session.Prepare(step,out var prep))throw new Exception(prep);
            rig.ApplyPreparation(session.Operation);
            if(!ApparatusLayoutChecks.ValidateRodBore(rig,out var bore))throw new Exception(bore);checks.Add("Actual imported clamp triangles clear the original 10 mm main rod: "+bore);
            var range=session.spec.parameters.Single(parameter=>parameter.key=="length");
            foreach(double length in new[]{range.min,.4,.6,.8,1.0,range.max}.Where(value=>value>=range.min&&value<=range.max).Distinct())
            {
                if(!session.SetParameter("length",length,out var change))throw new Exception(change);rig.RefreshConfiguration();
                if(!rig.ValidateAssembly(out var failure))throw new Exception(length+" m: "+failure);
                checks.Add(length.ToString("F2")+" m brief length: original 720 mm shaft, socket fit, cord endpoints, swing clearance and floor clearance PASS.");
            }
            File.WriteAllLines("Evidence/stock-stand-audit.txt",new[]{"PASS","Original supplied geometry; original clamp bore fits the main 10 mm rod, unused carriage remains lower, entire base seated."}.Concat(checks));
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllLines("Evidence/stock-stand-audit.txt",new[]{"FAILED",e.ToString()}.Concat(checks));EditorApplication.Exit(1);}
    }
}
[InitializeOnLoad]public static class VlabStockStandCheck
{
    static VlabStockStandCheck(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Vlab.StockStandAudit",false)){SessionState.SetBool("Vlab.StockStandAudit",false);new GameObject("Stock support verification",typeof(VlabStockStandAudit));}};}
    public static void Run(){SessionState.SetBool("Vlab.StockStandAudit",true);EditorSceneManager.OpenScene("Assets/Labs/Scenes/Physics.unity");EditorApplication.EnterPlaymode();}
}
#endif
