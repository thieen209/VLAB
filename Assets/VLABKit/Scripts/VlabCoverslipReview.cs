using UnityEngine;
public sealed class VlabCoverslipReview : MonoBehaviour
{
 [SerializeField] VlabCoverslip model;
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,290,150),GUI.skin.box);GUILayout.Label("Coverslip 22 x 22 x 0.15 mm");GUILayout.Label("Lowering angle: "+model.AngleDegrees.ToString("F1")+" deg");float a=GUILayout.HorizontalSlider(model.AngleDegrees,0,60);if(a!=model.AngleDegrees)model.SetAngle(a);GUILayout.BeginHorizontal();if(GUILayout.Button("Flat"))model.SetAngle(0);if(GUILayout.Button("30 deg"))model.SetAngle(30);if(GUILayout.Button("Raised"))model.SetAngle(60);GUILayout.EndHorizontal();GUILayout.Label("Separate trigger; no glass thickness inflation.");GUILayout.EndArea();}
}
