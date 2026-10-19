using UnityEngine;
public sealed class VlabCopperCoatingReview : MonoBehaviour
{
 [SerializeField] VlabCopperCoating coating;
 void OnGUI(){GUILayout.BeginArea(new Rect(20,20,280,160),GUI.skin.box);GUILayout.Label("Copper coating - visual threshold");GUILayout.Label("Not deposited mass or measured coverage");float level=GUILayout.HorizontalSlider(coating.VisualLevel,0,1);if(level!=coating.VisualLevel)coating.SetVisualLevel(level);GUILayout.BeginHorizontal();if(GUILayout.Button("Bare"))coating.SetVisualLevel(0);if(GUILayout.Button("Partial"))coating.SetVisualLevel(.5f);if(GUILayout.Button("Full"))coating.SetVisualLevel(1);GUILayout.EndHorizontal();GUILayout.Label("Visual level: "+coating.VisualLevel.ToString("F2"));GUILayout.EndArea();}
}
