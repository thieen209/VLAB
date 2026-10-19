using UnityEngine;
public sealed class VlabCrankReview : MonoBehaviour
{
 [SerializeField] VlabCrank model;
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,290,180),GUI.skin.box);GUILayout.Label("Indexed crank - 6 mm pin");GUILayout.Label("Radius: "+model.RadiusMm+" mm / Angle: "+model.AngleDegrees.ToString("F1")+" deg");GUILayout.BeginHorizontal();foreach(int mm in new[]{20,30,40})if(GUILayout.Button(mm+" mm"))model.Configure(mm,model.AngleDegrees);GUILayout.EndHorizontal();float a=GUILayout.HorizontalSlider((float)model.AngleDegrees,0,359.9f);if(System.Math.Abs(a-model.AngleDegrees)>.001)model.Configure(model.RadiusMm,a);GUILayout.BeginHorizontal();if(GUILayout.Button("+90 deg"))model.Configure(model.RadiusMm,model.AngleDegrees+90);if(GUILayout.Button("Reset"))model.Configure(30,0);GUILayout.EndHorizontal();GUILayout.Label("Prescribed rotation; no force simulation.");GUILayout.EndArea();}
}
