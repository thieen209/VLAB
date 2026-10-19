using UnityEngine;
public sealed class VlabFunnel : MonoBehaviour
{
 [SerializeField] Transform paperSocket,outlet;
 [SerializeField] Vector3 localOut;
 [SerializeField] Quaternion paperLocalRotation;
 public Ray Outflow=>new Ray(outlet.position,transform.TransformDirection(localOut).normalized);
 public bool TrySeatPaper(VlabFilterPaper paper){
 if(!isActiveAndEnabled||!paper||!paper.isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(paper.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
 var mf=paper.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh.name.Contains("FoldedCone"))return false;
 paper.transform.SetParent(transform,true);paper.transform.SetLocalPositionAndRotation(transform.InverseTransformPoint(paperSocket.position),paperLocalRotation);return true;
 }
 // shortcut: seats the verified folded paper and supplies the outlet ray; filtration rate and retained solute require lesson integration.
}
