using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Editor review controls; production Cardboard/BLE adapters are a separate hardware integration.
public class LabReview:MonoBehaviour {
 public string[] roomNames; public Vector3[] positions; public int roomIndex; int node;
 public void Teleport(int index){if(index<0||index>=positions.Length)return;node=index;transform.position=positions[node];}
 void Update(){if(Keyboard.current==null)return;if(Keyboard.current.tabKey.wasPressedThisFrame)Teleport((node+1)%positions.Length);if(Mouse.current!=null&&Mouse.current.rightButton.isPressed){var delta=Mouse.current.delta.ReadValue();var e=transform.eulerAngles;e.y+=delta.x*.12f;e.x-=delta.y*.12f;transform.eulerAngles=e;}}
 void OnGUI(){GUILayout.BeginArea(new Rect(16,16,470,95),GUI.skin.box);GUILayout.Label("VLAB · "+roomNames[roomIndex]+" · Room review / Xem phòng");GUILayout.BeginHorizontal();for(int i=0;i<roomNames.Length;i++){int j=i;if(GUILayout.Button(roomNames[j]))SceneManager.LoadScene(roomNames[j]);}GUILayout.EndHorizontal();GUILayout.Label("TAB: next viewpoint / đổi vị trí · Right mouse: look / nhìn");GUILayout.EndArea();}
}
