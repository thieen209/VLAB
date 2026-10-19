#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

// Focused, read-only geometry acceptance. Use only in a dedicated batch Editor process.
public static class LauncherMeshProbe
{
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Launcher mesh probe requires a dedicated batch Editor.");
        var report=new StringBuilder();GameObject fixture=null;bool passed=true;
        try
        {
            EditorSceneManager.OpenScene("Assets/Labs/Scenes/Physics.unity",OpenSceneMode.Single);
            var top=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&r.bounds.max.y>.72f&&r.bounds.max.y<1.12f&&(r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)).OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).First();
            var at=new Vector3(top.bounds.center.x,top.bounds.max.y+.005f,top.bounds.center.z);
            fixture=new GameObject("Isolated original P02 imported-mesh probe");var rig=fixture.AddComponent<ApparatusRig>();rig.Build(new ExperimentSession("P02"),at);
            foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new InvalidOperationException("Cannot install original "+item.assetId);}
            var launcher=rig.items.Single(i=>i.assetId=="launcher").GetComponentInChildren<VlabLauncher>();
            var mesh=launcher.GetComponentsInChildren<MeshFilter>().Single(m=>m.name.EndsWith("_Barrel_VIS",StringComparison.Ordinal));
            report.AppendLine("Original scene: Assets/Labs/Scenes/Physics.unity; worktop: "+top.name);
            report.AppendLine("Original imported barrel: "+AssetDatabase.GetAssetPath(mesh.sharedMesh));
            foreach(float angle in new[]{0f,45f,85f,90f})
            {
                if(!launcher.SetAngle(angle))throw new InvalidOperationException("Native launcher refuses angle "+angle);
                bool valid=ApparatusLayoutChecks.ValidateLauncherBarrel(rig,out string diagnostic);passed&=valid;
                report.AppendLine((valid?"PASS ":"FAIL ")+angle+" degrees: "+diagnostic);
            }
        }
        catch(Exception error){passed=false;report.AppendLine("FAIL "+error);}
        finally
        {
            if(fixture)UnityEngine.Object.DestroyImmediate(fixture);
            string directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Evidence");Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"launcher-mesh-probe.txt"),(passed?"PASS":"FAIL")+Environment.NewLine+report);
        }
        EditorApplication.Exit(passed?0:1);
    }
}
#endif
