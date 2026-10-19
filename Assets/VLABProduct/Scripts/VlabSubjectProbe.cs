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

public sealed class VlabSubjectProbe:MonoBehaviour
{
    readonly List<string> receipt=new List<string>();
    IEnumerator Start()
    {
        yield return null;
        SubjectRecordVerifier.Begin();
        try{RunPendulum();receipt.Add("P01: PASS native apparatus, crossing measurement and file persistence");}catch(Exception e){receipt.Add("FAIL P01 "+e);}
        try{BiologyIntegrationChecks.Run();receipt.Add("Biology helper completed; inspect its per-journey assertions.");}catch(Exception e){receipt.Add("FAIL Biology "+e);}
        try{ChemistryIntegrationChecks.Run();receipt.Add("Chemistry helper completed; inspect its per-journey assertions.");}catch(Exception e){receipt.Add("FAIL Chemistry "+e);}
        var routine=PhysicsMechanicsSmoke.Run(message=>
        {
            receipt.Add(message);if(!message.Contains(": PASS"))return;
            try{var rig=GameObject.Find("Smoke "+message.Substring(0,3)).GetComponent<ApparatusRig>();SubjectRecordVerifier.Verify(rig.Session);}catch(Exception e){receipt.Add("FAIL offline "+message.Substring(0,3)+" "+e);}
        });
        while(true)
        {
            bool next=false;object current=null;
            try{next=routine.MoveNext();if(next)current=routine.Current;}catch(Exception e){receipt.Add("FAIL Physics/mechanics helper "+e);}
            if(!next)break;yield return current;
        }
        routine=PhysicsMechanicsSmoke.RunFocused(message=>receipt.Add(message));
        while(true)
        {
            bool next=false;object current=null;
            try{next=routine.MoveNext();if(next)current=routine.Current;}catch(Exception e){receipt.Add("FAIL focused Physics/mechanics helper "+e);}
            if(!next)break;yield return current;
        }
        Directory.CreateDirectory("Evidence/Subjects");File.WriteAllLines("Evidence/Subjects/first-integration.txt",receipt);
        try{SubjectRecordVerifier.Complete();}catch(Exception e){receipt.Add("FAIL offline coverage "+e);File.WriteAllLines("Evidence/Subjects/first-integration.txt",receipt);}
        var chemistry=File.Exists("Evidence/Chemistry/integration-checks.txt")?File.ReadAllText("Evidence/Chemistry/integration-checks.txt"):"FAIL missing chemistry receipt";
        EditorApplication.Exit(receipt.Any(x=>x.Contains("Exception")||x.StartsWith("FAIL")||(!x.Contains("PASS")&&!x.Contains("helper completed")))||chemistry.Contains("FAIL")?1:0);
    }
    void RunPendulum()
    {
        var bench=GameObject.CreatePrimitive(PrimitiveType.Cube);bench.name="Isolated test worktop";bench.transform.position=new Vector3(0,.745f,0);bench.transform.localScale=new Vector3(2,.1f,.8f);
        try
        {
            foreach(double length in new[]{.4,.6,.8,1.0})for(int repeat=0;repeat<3;repeat++)
            {
            var root=new GameObject("P01 native subject fixture");try{
            var session=new ExperimentSession("P01");session.parameters["length"]=length;var rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(0,.8f,0));
            foreach(var item in rig.items){rig.Select(item);if(!rig.InstallSelected())throw new Exception("Installation failed "+item.assetId);}
            while(session.Operation<session.spec.operationsEn.Length)if(!rig.PrepareNext(out var why))throw new Exception(why);
            if(!rig.ValidateAssembly(out var assembly)||!session.Start(out assembly))throw new Exception(assembly);
            session.Advance(35);if(!session.Measure(out var measurement))throw new Exception(measurement);SubjectRecordVerifier.Verify(session);
            }finally{DestroyImmediate(root);}
            }
        }
        finally{DestroyImmediate(bench);}
    }
}
[InitializeOnLoad]public static class VlabSubjectChecks
{
    static VlabSubjectChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Vlab.SubjectProbe",false)){SessionState.SetBool("Vlab.SubjectProbe",false);new GameObject("Subject apparatus integration",typeof(VlabSubjectProbe));}};}
    public static void Run(){HarvestKitBindings.Run();SessionState.SetBool("Vlab.SubjectProbe",true);EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
}
#endif
