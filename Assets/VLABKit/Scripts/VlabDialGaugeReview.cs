using UnityEngine;
public sealed class VlabDialGaugeReview : MonoBehaviour
{
 [SerializeField] VlabDialGauge model;
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,300,160),GUI.skin.box);GUILayout.Label("Dial indicator: 0-10 mm / 0.01 mm");GUILayout.Label("Reading: "+model.ReadingMm.ToString("F2")+" mm");float mm=GUILayout.HorizontalSlider((float)model.CompressionMm,0,10);if(Mathf.Abs(mm-(float)model.CompressionMm)>.00001f)model.SetCompression(mm);GUILayout.BeginHorizontal();if(GUILayout.Button("Zero"))model.SetCompression(0);if(GUILayout.Button("5.25 mm"))model.SetCompression(5.25);if(GUILayout.Button("10 mm"))model.SetCompression(10);GUILayout.EndHorizontal();GUILayout.Label("Small pointer tracks full millimetres.");GUILayout.EndArea();}
}
