using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VLAB.MainMenu;

public static class VLabStabilizationSetup
{
    public static void ConfigurePhysicsBench()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        const string root="Assets/VLAB/PhysicsLab/Scenes/";
        try
        {
            Vector3 position=Vector3.zero,scale=Vector3.one;Quaternion rotation=Quaternion.identity;
            var scenePaths=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/VLAB/PhysicsLab/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>System.IO.Path.GetFileName(p).StartsWith("Physics_")).ToArray();
            if(scenePaths.Length!=6)throw new System.InvalidOperationException("Expected all six Physics apparatus scenes before migrating their common bench.");
            foreach(var path in scenePaths)
            {
                var scene=EditorSceneManager.OpenScene(path);
                var table=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Experiment_Table");
                position=table.position;rotation=table.rotation;scale=table.lossyScale;
                table.gameObject.SetActive(false);EditorSceneManager.SaveScene(scene);
            }
            var baseScene=EditorSceneManager.OpenScene(root+"PhysicsLab_Base.unity");
            var existing=baseScene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Permanent Physics Workbench");
            var bench=existing??(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Prefabs/Experiment_Table.prefab"),baseScene);
            bench.name="Permanent Physics Workbench";bench.transform.SetPositionAndRotation(position,rotation);bench.transform.localScale=scale;
            EditorSceneManager.SaveScene(baseScene);
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [MenuItem("Tools/VLAB/Stabilization/Prepare original Physics controller rig")]
    public static void PrepareRig()
    {
        const string source="Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string destination="Assets/VLAB/MainMenu/Resources/VLAB Player Rig.prefab";
        var rig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
        try
        {
            rig.name="XR Origin (XR Rig)";
            foreach(var interactor in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true))
            {
                interactor.enableUIInteraction=true;interactor.enableFarCasting=true;
                if(interactor.farInteractionCaster is UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster caster)
                    caster.raycastMask=1|(1<<LayerMask.NameToLayer("Interactable"))|(1<<LayerMask.NameToLayer("UI"));
            }
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            assets.playerRig=PrefabUtility.SaveAsPrefabAsset(rig,destination);
            EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(rig); }
    }
    [MenuItem("Tools/VLAB/Stabilization/Configure permanent experiment stations")]
    public static void ConfigureStations()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var path in new[] { "Assets/ChemistryLab.unity", "Assets/VLAB/DemoLabs/Scenes/BiologyLab.unity", "Assets/VLAB/DemoLabs/Scenes/EngineeringLab.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var station = Object.FindAnyObjectByType<VLabExperimentStation>() ?? new GameObject("ExperimentContentAnchor").AddComponent<VLabExperimentStation>();
                GameObject[] content;
                Vector3 position;
                if (scene.name == "ChemistryLab")
                {
                    var names = new[] { "HandsOnTitrationKit", "TitrationLearningConsole", "ReagentsAndGlassware", "PersonalProtectiveEquipment" };
                    content = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => names.Contains(t.name)).Select(t => t.gameObject).ToArray();
                    var top = GameObject.Find("MainTitrationBench").transform.Find("Top").GetComponent<Collider>().bounds;
                    position = new Vector3(top.center.x, top.max.y + .035f, top.center.z);
                }
                else
                {
                    var root = scene.GetRootGameObjects().Single(r => r.name == scene.name).transform;
                    var permanent = new[] { "Worktop", "WorktopEdge", "ReusedExperimentTable", "BenchBrand", "PlayerView", "DemoInput", "EventSystem", "LabInterface" };
                    content = root.Cast<Transform>().Where(t => !permanent.Contains(t.name) &&
                        (t.GetComponentInChildren<VLAB.DemoLabs.VLabInteractable>(true)!=null || t.name=="ContextualHintRing" ||
                        (t.position.y > 1.10f && t.position.y < 3 && Mathf.Abs(t.position.x) < 2.25f && Mathf.Abs(t.position.z) < 1.1f))).Select(t => t.gameObject).ToArray();
                    position = new Vector3(0, 1.14f, 0);
                }
                station.Configure(position, content);
                EditorUtility.SetDirty(station);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
