using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LabSmoke {
 static double deadline; static int room=-1; static readonly List<string> results=new List<string>();
 static LabSmoke(){EditorApplication.playModeStateChanged+=State;}
 public static void Run(){SessionState.SetBool("VLAB.Smoke",true);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("VLAB.Smoke",false))return;room=-1;results.Clear();deadline=EditorApplication.timeSinceStartup+1;EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.timeSinceStartup<deadline)return;deadline=EditorApplication.timeSinceStartup+1;
  try {
   if(room>=0){var nav=Object.FindFirstObjectByType<LabReview>();if(nav==null)throw new System.Exception("Missing navigation");for(int i=0;i<nav.positions.Length;i++){nav.Teleport(i);if(Vector3.Distance(nav.transform.position,nav.positions[i])>.001f)throw new System.Exception("Teleport mismatch");}int fixtureCount=0;foreach(var f in Object.FindObjectsByType<LabFixture>(FindObjectsSortMode.None)){if(!f.movingPart)continue;foreach(var gate in f.requiredOpen){gate.Activate();gate.Animate(3);}var p0=f.movingPart.localPosition;var q0=f.movingPart.localRotation;f.Activate();f.Animate(3);if(Vector3.Distance(f.movingPart.localPosition,p0+f.openOffset)>.001f||Quaternion.Angle(f.movingPart.localRotation,q0*Quaternion.Euler(f.openAngles))>.1f)throw new System.Exception("Fixture open failed: "+f.name);f.Activate();f.Animate(3);if(Vector3.Distance(f.movingPart.localPosition,p0)>.001f||Quaternion.Angle(f.movingPart.localRotation,q0)>.1f)throw new System.Exception("Fixture close failed: "+f.name);foreach(var gate in f.requiredOpen){gate.Activate();gate.Animate(3);}fixtureCount++;}results.Add(SceneManager.GetActiveScene().name+": PASS camera, viewpoint navigation, scene load and "+fixtureCount+" moving fixtures");}
   room++;if(room<4){SceneManager.LoadScene(room);return;}
  }catch(System.Exception e){results.Add("FAIL "+e.Message);}
  EditorApplication.update-=Tick;File.WriteAllLines("../Evidence/play-checks.txt",results);SessionState.SetBool("VLAB.Smoke",false);EditorApplication.isPlaying=false;
 }
}


