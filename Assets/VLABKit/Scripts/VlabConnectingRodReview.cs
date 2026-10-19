using UnityEngine;
public sealed class VlabConnectingRodReview : MonoBehaviour
{
 [SerializeField] VlabConnectingRod[] rods;
 float angle;
 void OnGUI()
 {
  GUILayout.BeginArea(new Rect(20,20,290,160),GUI.skin.box);
  GUILayout.Label("Connecting rods: 80 / 100 / 120 mm");
  GUILayout.Label("Rigid endpoint pose - no force simulation");
  float next=GUILayout.HorizontalSlider(angle,-25,25);
  if(GUILayout.Button("Reset pose"))next=0;
  if(next!=angle){angle=next;for(int i=0;i<rods.Length;i++){var a=new Vector3(0,.03f-i*.03f,0);var d=Quaternion.AngleAxis(angle,Vector3.forward)*Vector3.right;rods[i].SetEndpoints(a,a+d*rods[i].LengthM,Vector3.forward);}}
  GUILayout.Label("Angle: "+angle.ToString("F1")+" deg");GUILayout.EndArea();
 }
}
