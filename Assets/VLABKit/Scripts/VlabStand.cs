using UnityEngine;
public sealed class VlabStand:MonoBehaviour{
 [SerializeField] Transform boss,mount;[SerializeField] Vector3 axisZero,localUp;
 public Transform Mount=>mount;public float HeightM{get;private set;}=.45f;
 public bool TrySetHeight(float metres){if(!isActiveAndEnabled||!boss||!float.IsFinite(metres)||metres<.060f||metres>.660f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;boss.localPosition=axisZero+localUp*metres;HeightM=metres;return true;}
 // shortcut: prescribed rigid carriage height; attached tools must use the validated10mm mount and their own collision checks, no holding force or loaded stability simulation.
}
