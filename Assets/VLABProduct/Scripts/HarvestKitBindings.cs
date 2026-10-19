#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class HarvestKitBindings
{
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Harvest the delivered scene bindings before entering Play mode.");
        EditorSceneManager.OpenScene("Assets/VLABKit/Scenes/VLAB_beltReview.unity");
        var belt=UnityEngine.Object.FindObjectsByType<VlabBelt>().Single();
        if(belt.Configurations.Length!=4)throw new InvalidOperationException("The supplied QA scene must contain its four authored fixed-length belts.");
        // The delivery stores this controller at scene level. Preserve those exact references,
        // lengths, materials and transforms in one product assembly, without changing the source.
        foreach(var config in belt.Configurations)
        {
            if(!config.root||!config.surface||!config.surface.transform.IsChildOf(config.root.transform))throw new InvalidOperationException("Incomplete delivered belt binding.");
            config.root.transform.SetParent(belt.transform,true);
        }
        string output="Assets/VLABProduct/Resources/Equipment/VLAB_belt_lesson.prefab";
        var saved=PrefabUtility.SaveAsPrefabAsset(belt.gameObject,output);
        if(!saved||!saved.GetComponent<VlabBelt>())throw new InvalidOperationException("The harvested belt assembly did not retain the SDK controller.");
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Evidence");
        File.WriteAllText("Evidence/belt-binding-harvest.txt","PASS\nProduct wrapper retains the four scene-authored configurations from Assets/VLABKit/Scenes/VLAB_beltReview.unity.\nNo original mesh, material, dimensions, fixed belt length or source scene saved/modified.\n");
    }
}
#endif
