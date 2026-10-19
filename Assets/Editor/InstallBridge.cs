using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.IO;
public static class InstallBridge {
 static AddRequest request;
 public static void Run() { request=Client.Add("com.unity.ai.assistant@2.20.0-pre.2"); EditorApplication.update+=Poll; }
 static void Poll() { if(!request.IsCompleted)return; EditorApplication.update-=Poll; File.WriteAllText("bridge-install.txt",request.Status==StatusCode.Success?"PASS":request.Error.message); EditorApplication.Exit(request.Status==StatusCode.Success?0:1); }
}
