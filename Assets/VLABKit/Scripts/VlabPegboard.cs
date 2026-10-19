using UnityEngine;
public sealed class VlabPegboard : MonoBehaviour
{
 public const int Columns=21,Rows=14;
 [SerializeField] Transform origin,rightStep,upStep;
 public bool TryGetSocket(int column,int row,float standOffMeters,out Pose pose){pose=default;if(!isActiveAndEnabled||column<0||column>=Columns||row<0||row>=Rows||!float.IsFinite(standOffMeters)||standOffMeters<0||standOffMeters>.2f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;var right=rightStep.position-origin.position;var up=upStep.position-origin.position;if(Mathf.Abs(right.magnitude-.020f)>1e-6f||Mathf.Abs(up.magnitude-.020f)>1e-6f||Mathf.Abs(Vector3.Dot(right.normalized,up.normalized))>1e-5f)return false;var inward=Vector3.Cross(right,up).normalized;pose=new Pose(origin.position+right*column+up*row-inward*standOffMeters,Quaternion.LookRotation(inward,up.normalized));return true;}
 // shortcut: logical atlas sockets on a solid board; physical mount adapters, bolt penetration and safe powered-device editing need the mechanism controller.
}
