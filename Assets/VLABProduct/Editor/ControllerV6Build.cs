#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class ControllerV6Build
{
    public static void Run()
    {
        string key=Environment.GetEnvironmentVariable("VLAB_RELEASE_KEYSTORE"),password=Environment.GetEnvironmentVariable("VLAB_RELEASE_PASSWORD");
        if(string.IsNullOrEmpty(password)||!File.Exists(key))throw new InvalidOperationException("Local signing environment missing");
        string output=Path.GetFullPath("Builds/Android/Candidates/VLAB-0.1.10-dual-v6-code30.apk");Directory.CreateDirectory(Path.GetDirectoryName(output));
        if(File.Exists(output))throw new IOException("Candidate exists; refusing overwrite");
        bool custom=PlayerSettings.Android.useCustomKeystore;string name=PlayerSettings.Android.keystoreName,alias=PlayerSettings.Android.keyaliasName,version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode,exit=1;
        try {
            PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=key;PlayerSettings.Android.keyaliasName="nguaman-vlab";PlayerSettings.Android.keystorePass=password;PlayerSettings.Android.keyaliasPass=password;
            PlayerSettings.bundleVersion="0.1.10";PlayerSettings.Android.bundleVersionCode=30;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output,target=BuildTarget.Android,subtarget=(int)MobileTextureSubtarget.ETC2,options=BuildOptions.None});
            Directory.CreateDirectory("Evidence/ControllerV6");File.WriteAllText("Evidence/ControllerV6/android-build.txt",report.summary.result+"\n"+output+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize+"\n");exit=report.summary.result==BuildResult.Succeeded?0:1;
        }finally {
            PlayerSettings.Android.keystorePass="";PlayerSettings.Android.keyaliasPass="";PlayerSettings.Android.useCustomKeystore=custom;PlayerSettings.Android.keystoreName=name;PlayerSettings.Android.keyaliasName=alias;
            PlayerSettings.bundleVersion=version;PlayerSettings.Android.bundleVersionCode=code;AssetDatabase.SaveAssets();
        }
        EditorApplication.Exit(exit);
    }
}
#endif
