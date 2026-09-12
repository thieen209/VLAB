using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

public static class VLabUnifiedBuild
{
    [MenuItem("Tools/VLAB/Unified/Configure Android viewer")]
    public static void Configure()
    {
        const string settingsPath="Assets/XR/VLAB XR Settings.asset";
        Directory.CreateDirectory("Assets/XR");
        XRGeneralSettingsPerBuildTarget targets;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out targets))
        {
            targets=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(targets,settingsPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,targets,true);
        }
        if(!targets.HasSettingsForBuildTarget(BuildTargetGroup.Android)) targets.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if(!targets.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) targets.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var settings=targets.SettingsForBuildTarget(BuildTargetGroup.Android);
        settings.InitManagerOnStart=true;
        settings.Manager.automaticLoading=true;settings.Manager.automaticRunning=true;
        if(!XRPackageMetadataStore.AssignLoader(settings.Manager,"Google.XR.Cardboard.XRLoader",BuildTargetGroup.Android))
            throw new BuildFailedException("Could not assign Cardboard loader.");
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.optimizedFramePacing=false;
        PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.forceInternetPermission=true;
        var symbols=typeof(EditorUserBuildSettings).GetProperty("androidCreateSymbols",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);
        if(symbols!=null && symbols.PropertyType.IsEnum) symbols.SetValue(null,Enum.Parse(symbols.PropertyType,"Disabled"));
        // Use Input System's Unity-6-aware settings helper so the active build profile is respected.
        // The legacy Home scene is excluded from the APK; all production labs use Input System.
        var inputHelper=typeof(UnityEngine.InputSystem.InputSystem).Assembly.GetType("UnityEngine.InputSystem.Editor.EditorPlayerSettingHelpers");
        var oldBackend=inputHelper?.GetProperty("oldSystemBackendsEnabled",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
        var newBackend=inputHelper?.GetProperty("newSystemBackendsEnabled",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
        if(oldBackend==null||newBackend==null)throw new BuildFailedException("Cannot configure the pinned Input System Android backend.");
        newBackend.SetValue(null,true);oldBackend.SetValue(null,false);
        // Runtime-created menu materials need this shader even when no scene material references it.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(var name in new[]{"Unlit/Color","TextMeshPro/Mobile/Bitmap"})
        {
            var shader=Shader.Find(name);
            if(shader==null)throw new BuildFailedException("Missing runtime shader: "+name);
            if(Enumerable.Range(0,shaders.arraySize).Any(i=>shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader))continue;
            int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);EditorUtility.SetDirty(settings.Manager);EditorUtility.SetDirty(targets);
        AssetDatabase.SaveAssets();
        ValidateScenes();
        Debug.Log("VLAB Android configured: Cardboard, landscape, ARM64 IL2CPP, OpenGLES3, API 26–36.");
    }

    [MenuItem("Tools/VLAB/Unified/Repair missing Physics scenery")]
    public static void RepairPhysicsScenery()
    {
        var original=EditorSceneManager.GetSceneManagerSetup();
        const string root="Assets/VLAB/PhysicsLab/";
        try
        {
            var scene=EditorSceneManager.OpenScene(root+"Scenes/PhysicsLab_Base.unity",OpenSceneMode.Single);
            foreach(var window in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("ArchitecturalWindow_",StringComparison.Ordinal)))
                if(window.Find("GlassPane")==null) AddCube("GlassPane",window,new Vector3(0,.89f,-.018f),new Vector3(3.05f,1.50f,.012f),root+"Environment/Materials/VLAB_WindowGlass.mat",false);
            EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene(root+"Scenes/PhysicsLab_Hub.unity",OpenSceneMode.Single);
            var console=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="VLAB_ExperimentConsole");
            if(console.Find("Base")==null)AddCube("Base",console,new Vector3(0,.48f,0),new Vector3(2.7f,.96f,.85f),root+"Environment/Materials/VLAB_Navy.mat",true);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if(original.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(original); }
    }
    private static void AddCube(string name,Transform parent,Vector3 position,Vector3 scale,string material,bool collider)
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.SetParent(parent,false);
        cube.transform.localPosition=position;cube.transform.localScale=scale;
        cube.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(material);
        if(!collider)UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
    }

    [MenuItem("Tools/VLAB/Unified/Validate production scenes")]
    public static void ValidateScenes()
    {
        var original=EditorSceneManager.GetSceneManagerSetup();
        var findings=new List<string>();
        try
        {
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Single);
                int missing=0;
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var t in root.GetComponentsInChildren<Transform>(true)) missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                findings.Add(entry.path+": missing scripts="+missing+", dependencies="+AssetDatabase.GetDependencies(entry.path,true).Length);
                if(missing>0) throw new BuildFailedException(entry.path+" has "+missing+" missing scripts.");
            }
        }
        finally
        {
            Directory.CreateDirectory("TestResults/Unified");File.WriteAllLines("TestResults/Unified/scene-validation.txt",findings);
            if(original.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(original);
        }
    }

    [MenuItem("Tools/VLAB/Unified/Build Android APK")]
    public static void BuildAndroid()
    {
        Configure();
        Directory.CreateDirectory("Builds/Android");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled && s.path!="Assets/Home.unity").Select(s=>s.path).ToArray(),
            locationPathName="Builds/Android/VLAB.apk",target=BuildTarget.Android,
            options=Environment.GetCommandLineArgs().Contains("-vlabDevelopment") ? BuildOptions.Development : BuildOptions.None
        });
        Directory.CreateDirectory("TestResults/Unified");
        File.WriteAllText("TestResults/Unified/android-build.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nBytes: "+result.summary.totalSize+"\nDuration: "+result.summary.totalTime);
        if(result.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("VLAB Android build failed. See build log.");
    }
}
