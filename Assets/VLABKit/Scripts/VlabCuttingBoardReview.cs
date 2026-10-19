using UnityEngine;
public sealed class VlabCuttingBoardReview : MonoBehaviour
{
 [SerializeField] VlabCuttingBoard model;
 Pose pose;bool valid;string state;
 void Start()=>Select(Vector2.zero,"Center");
 void Select(Vector2 center,string label){valid=model.TryGetPlacement(center,new Vector2(.02f,.02f),out pose);state=label+(valid?": accepted":": rejected");}
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,285,160),GUI.skin.box);GUILayout.Label("Specimen board - 250 x 180 mm");GUILayout.Label("20 x 20 mm footprint check");GUILayout.BeginHorizontal();if(GUILayout.Button("Center"))Select(Vector2.zero,"Center");if(GUILayout.Button("Corner"))Select(new Vector2(.09f,.055f),"Corner");if(GUILayout.Button("Outside"))Select(new Vector2(.11f,0),"Outside");GUILayout.EndHorizontal();GUILayout.Label(state);GUILayout.Label("Marker only; specimen supplied separately.");GUILayout.EndArea();if(valid){var s=GetComponent<Camera>().WorldToScreenPoint(pose.position);if(s.z>0)GUI.Box(new Rect(s.x-8,Screen.height-s.y-8,16,16),"+");}}
}
