using UnityEngine;
public sealed class VlabCylinderReview : MonoBehaviour
{
 [SerializeField] VlabBeaker large,small;
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,310,220),GUI.skin.box);GUILayout.Label("Graduated cylinders - upright volumes");GUILayout.Label("100 mL: "+large.VolumeMl.ToString("F1")+"     50 mL: "+small.VolumeMl.ToString("F1"));GUILayout.BeginHorizontal();if(GUILayout.Button("100: +25 mL"))large.Add(25);if(GUILayout.Button("50: +10 mL"))small.Add(10);GUILayout.EndHorizontal();if(GUILayout.Button("Transfer 10 mL: 100 -> 50"))large.TransferTo(small,10);if(GUILayout.Button("Fill both")){large.Add(large.CapacityMl);small.Add(small.CapacityMl);}if(GUILayout.Button("Empty both")){large.Remove(large.VolumeMl);small.Remove(small.VolumeMl);}GUILayout.Label("Flat surface preview; no tilt or chemistry.");GUILayout.EndArea();}
}
