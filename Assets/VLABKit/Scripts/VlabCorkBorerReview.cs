using UnityEngine;
public sealed class VlabCorkBorerReview : MonoBehaviour
{
 [SerializeField] VlabCorkBorer model;
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,280,170),GUI.skin.box);GUILayout.Label("8 mm sample borer / 100 mm tube");GUILayout.Label("Plunger stroke: "+(model.StrokeM*1000).ToString("F1")+" mm");float s=GUILayout.HorizontalSlider(model.StrokeM,0,.035f);if(s!=model.StrokeM)model.SetStroke(s);GUILayout.BeginHorizontal();if(GUILayout.Button("Retract"))model.SetStroke(0);if(GUILayout.Button("Half"))model.SetStroke(.0175f);if(GUILayout.Button("Push"))model.SetStroke(.035f);GUILayout.EndHorizontal();GUILayout.Label("Geometry demonstration; cutting and sample transfer pending.",new GUIStyle(GUI.skin.label){wordWrap=true});GUILayout.EndArea();}
}
