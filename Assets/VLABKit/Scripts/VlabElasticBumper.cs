using UnityEngine;
public sealed class VlabElasticBumper : MonoBehaviour
{
 [SerializeField] Transform spring,contact,contactFace;
 [SerializeField] Vector3 contactRest,faceRest,localAxis;
 [SerializeField,Range(0,1)] float restitution=1;
 public float Restitution=>restitution;
 public float CompressionM{get;private set;}
 public Transform ContactFace=>contactFace;
 public Vector3 Axis=>contact.parent.TransformDirection(localAxis).normalized;
 public bool SetCompression(float metres)
 {
  if(!isActiveAndEnabled||!float.IsFinite(metres)||metres<0||metres>.008f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  spring.localScale=new Vector3(1-metres/.024f,1,1);contact.localPosition=contactRest-localAxis*metres;contactFace.localPosition=faceRest-localAxis*metres;CompressionM=metres;return true;
 }
 // shortcut: prescribed visual compression and contact trigger; the P06 pair solver must apply restitution once, not infer stiffness from the coil.
}
