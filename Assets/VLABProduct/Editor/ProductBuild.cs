#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using TMPro;
using Vlab.Product;

public static class VlabProductBuild
{
    const string Root="Assets/VLABProduct/";
    public static void Setup()
    {
        Directory.CreateDirectory(Root+"Scenes");Directory.CreateDirectory("Evidence");
        string gradingPath=Root+"Resources/UI/VlabColor.asset";
        var gradingProfile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(gradingPath);
        if(!gradingProfile)
        {
            var profile=ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();profile.name="VLAB daylight grading";
            var tone=profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
            var color=profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);color.postExposure.Override(-.7f);
            AssetDatabase.CreateAsset(profile,gradingPath);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,gradingPath);
        }
        // Repair detached subassets from an earlier profile creation, then persist the component list.
        gradingProfile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(gradingPath);
        foreach(var component in AssetDatabase.LoadAllAssetsAtPath(gradingPath).OfType<UnityEngine.Rendering.VolumeComponent>())if(!gradingProfile.components.Contains(component))gradingProfile.components.Add(component);
        EditorUtility.SetDirty(gradingProfile);AssetDatabase.SaveAssets();
        var font=AssetDatabase.LoadAssetAtPath<Font>(Root+"Resources/UI/Inter-Regular.ttf");
        if(!font)throw new InvalidOperationException("Supplied Inter font is missing.");
        string fontPath=Root+"Resources/UI/VlabInter.asset";
        if(!AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath))
        {
            var asset=TMP_FontAsset.CreateFontAsset(font,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            asset.name="VLAB Inter";AssetDatabase.CreateAsset(asset,fontPath);AssetDatabase.AddObjectToAsset(asset.material,fontPath);
            foreach(var texture in asset.atlasTextures)if(texture)AssetDatabase.AddObjectToAsset(texture,fontPath);
            asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ°²³⁴µπ×·–—−→‹›✓○☐",out var missing);
            if(!string.IsNullOrEmpty(missing))Debug.LogWarning("Font does not contain: "+missing);
            EditorUtility.SetDirty(asset);
        }
        var logo=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Resources/UI/vlab-banner-original.png");
        var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(logo)) as TextureImporter;bool wasReadable=importer.isReadable;
        if(!wasReadable){importer.isReadable=true;importer.SaveAndReimport();logo=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Resources/UI/vlab-banner-original.png");}
        int minX=logo.width,minY=logo.height,maxX=0,maxY=0;var pixels=logo.GetPixels32();for(int y=0;y<logo.height;y++)for(int x=0;x<logo.width;x++)if(pixels[y*logo.width+x].a>20){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
        File.WriteAllText(Root+"Resources/UI/logo-crop.txt",string.Join(",",new[]{(double)minX/logo.width,(double)minY/logo.height,(double)(maxX-minX+1)/logo.width,(double)(maxY-minY+1)/logo.height}.Select(x=>x.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        if(!wasReadable){importer.isReadable=false;importer.SaveAndReimport();}
        foreach(var room in new[]{"Physics","Chemistry","Biology","Engineering"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+room+".unity");var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name.Contains("Learner"));
            RoomLighting.Apply();camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
            BuildLabs.Capture(camera,Root+"Resources/UI/Preview_"+room+".png",1400,875);
        }
        EditorSceneManager.OpenScene("Assets/Labs/Scenes/Physics.unity");new GameObject("VLAB Application",typeof(VlabApp));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/VLAB_Boot.unity");
        EditorBuildSettings.scenes=new[]{Root+"Scenes/VLAB_Boot.unity","Assets/Labs/Scenes/Physics.unity","Assets/Labs/Scenes/Chemistry.unity","Assets/Labs/Scenes/Biology.unity","Assets/Labs/Scenes/Engineering.unity"}.Select(x=>new EditorBuildSettingsScene(x,true)).ToArray();
        ConfigureXR();
        PlayerSettings.productName="VLAB";PlayerSettings.companyName="Ngua Man VLAB";PlayerSettings.bundleVersion="0.1.6";PlayerSettings.Android.bundleVersionCode=17;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.nguaman.vlab");PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARMv7|AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.optimizedFramePacing=false;PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;PlayerSettings.Android.forceInternetPermission=true;
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.runInBackground=false;
        AssetDatabase.Refresh();AssetDatabase.SaveAssets();File.WriteAllText("Evidence/setup.txt","Startup: "+Root+"Scenes/VLAB_Boot.unity\nUnity "+Application.unityVersion+"\nCardboard loader assigned for Android; startup remains flat.\n");
        Debug.Log("VLAB PRODUCT SETUP COMPLETE");EditorApplication.Exit(0);
    }
    static void ConfigureXR()
    {
        const string path="Assets/XR/VlabXRSettings.asset";Directory.CreateDirectory("Assets/XR");
        var settings=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
        if(!settings){settings=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(settings,path);}
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey,settings,true);
        foreach(var group in new[]{BuildTargetGroup.Android,BuildTargetGroup.Standalone})
        {
            if(!settings.HasManagerSettingsForBuildTarget(group))settings.CreateDefaultManagerSettingsForBuildTarget(group);
            var general=settings.SettingsForBuildTarget(group);general.InitManagerOnStart=false;general.Manager.automaticLoading=false;general.Manager.automaticRunning=false;EditorUtility.SetDirty(general);
        }
        if(!XRPackageMetadataStore.AssignLoader(settings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android),"Google.XR.Cardboard.XRLoader",BuildTargetGroup.Android))throw new InvalidOperationException("Could not assign Cardboard loader.");
        EditorUtility.SetDirty(settings);
    }
    public static void AndroidDevelopment()
    {
        var destination=Path.GetFullPath("Builds/Android/NguaManVLAB-development.apk");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray(),locationPathName=destination,target=BuildTarget.Android,options=BuildOptions.Development});
        File.WriteAllText("Evidence/android-build.txt",report.summary.result+"\n"+destination+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize);
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
    public static void CoreAndPreviews()
    {
        foreach(var room in new[]{"Physics","Chemistry","Biology","Engineering"})
        {
            EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+room+".unity");RoomLighting.Apply();
            var camera=UnityEngine.Object.FindObjectsByType<Camera>().First(c=>c.name.Contains("Learner"));
            camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
            BuildLabs.Capture(camera,Root+"Resources/UI/Preview_"+room+".png",1400,875);
        }
        AssetDatabase.Refresh();VlabCoreChecks.Run();
    }
    public static void ConfigureAndroidIcons()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Branding/AppIcon.png");if(!icon)throw new InvalidOperationException("The original VLAB application icon is missing.");
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
        string backgroundPath=Root+"Branding/AppIconBackground.png";
        if(!File.Exists(backgroundPath))
        {
            var solid=new Texture2D(2,2,TextureFormat.RGBA32,false);
            solid.SetPixels(Enumerable.Repeat(new Color(.055f,.10f,.15f,1),4).ToArray());solid.Apply();
            File.WriteAllBytes(backgroundPath,solid.EncodeToPNG());UnityEngine.Object.DestroyImmediate(solid);
            AssetDatabase.ImportAsset(backgroundPath,ImportAssetOptions.ForceSynchronousImport);
        }
        var background=AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
        string foregroundPath=Root+"Branding/AppIconAdaptive.png";
        var previousTarget=RenderTexture.active;var target=RenderTexture.GetTemporary(512,512,0,RenderTextureFormat.ARGB32);var padded=new Texture2D(512,512,TextureFormat.RGBA32,false);
        try
        {
            RenderTexture.active=target;GL.Clear(true,true,Color.clear);GL.PushMatrix();
            try{GL.LoadPixelMatrix(0,512,512,0);Graphics.DrawTexture(new Rect(102,102,308,308),icon);}finally{GL.PopMatrix();}
            padded.ReadPixels(new Rect(0,0,512,512),0,0);padded.Apply();
            if(padded.GetPixel(0,0).a>.001f||!padded.GetPixels().Any(pixel=>pixel.a>.5f))throw new InvalidOperationException("VLAB adaptive safe-area rendering failed.");
            File.WriteAllBytes(foregroundPath,padded.EncodeToPNG());
        }
        finally{RenderTexture.active=previousTarget;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(padded);}
        AssetDatabase.ImportAsset(foregroundPath,ImportAssetOptions.ForceSynchronousImport);var foreground=AssetDatabase.LoadAssetAtPath<Texture2D>(foregroundPath);
        // Android adaptive slots override the default application icon.
        foreach(var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        {
            var slots=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
            foreach(var slot in slots)
            {
                var layers=Enumerable.Repeat(icon,slot.maxLayerCount).ToArray();
                if(layers.Length>1){layers[0]=background;layers[layers.Length-1]=foreground;}
                slot.SetTextures(layers);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,slots);
            if(PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind).Any(slot=>slot.GetTexture(slot.maxLayerCount-1)!=(slot.maxLayerCount>1?foreground:icon)))
                throw new InvalidOperationException("Android VLAB icon assignment failed.");
        }
    }
    public static void AndroidRelease()
    {
        string path=Environment.GetEnvironmentVariable("VLAB_RELEASE_KEYSTORE"),password=Environment.GetEnvironmentVariable("VLAB_RELEASE_PASSWORD");
        if(string.IsNullOrEmpty(path)||!File.Exists(path)||string.IsNullOrEmpty(password))throw new InvalidOperationException("Release signing must be supplied through the local build environment.");
        ConfigureAndroidIcons();
        PlayerSettings.productName="VLAB";PlayerSettings.bundleVersion="0.1.6";PlayerSettings.Android.bundleVersionCode=17;
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARMv7|AndroidArchitecture.ARM64;
        EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ETC2;
        var destination=Path.GetFullPath("Builds/Android/VLAB-"+PlayerSettings.bundleVersion+"-android.apk");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        bool previous=PlayerSettings.Android.useCustomKeystore;string previousName=PlayerSettings.Android.keystoreName,previousAlias=PlayerSettings.Android.keyaliasName;
        int exit=1;
        try
        {
            PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=path;PlayerSettings.Android.keyaliasName="nguaman-vlab";
            PlayerSettings.Android.keystorePass=password;PlayerSettings.Android.keyaliasPass=password;EditorUserBuildSettings.development=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray(),locationPathName=destination,target=BuildTarget.Android,subtarget=(int)MobileTextureSubtarget.ETC2,options=BuildOptions.None});
            Directory.CreateDirectory("Evidence");File.WriteAllText("Evidence/android-release-build.txt",report.summary.result+"\n"+destination+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nSerialized bytes: "+report.summary.totalSize+"\nArchitectures: "+PlayerSettings.Android.targetArchitectures+"\nTexture subtarget: "+EditorUserBuildSettings.androidBuildSubtarget+"\nRelease signing: local private keystore; credentials excluded from source.\n");
            exit=report.summary.result==BuildResult.Succeeded?0:1;
            if(exit==0&&EditorUserBuildSettings.androidBuildSubtarget!=MobileTextureSubtarget.ETC2)throw new InvalidOperationException("Completed Android build did not retain the required ETC2 subtarget.");
        }
        finally
        {
            PlayerSettings.Android.keystorePass="";PlayerSettings.Android.keyaliasPass="";PlayerSettings.Android.useCustomKeystore=previous;PlayerSettings.Android.keystoreName=previousName;PlayerSettings.Android.keyaliasName=previousAlias;AssetDatabase.SaveAssets();
        }
        EditorApplication.Exit(exit);
    }
}
#endif
