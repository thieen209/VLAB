using UnityEngine;
public sealed class VlabCopperCoating : MonoBehaviour
{
 [SerializeField] Renderer shell;
 MaterialPropertyBlock block;
 bool configured;
 [SerializeField] float visualLevel=1;
 public float VisualLevel=>visualLevel;
 public Renderer Shell=>shell;
 public bool SetVisualLevel(float level)
 {
  if(!isActiveAndEnabled||!float.IsFinite(level)||level<0||level>1||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  block??=new MaterialPropertyBlock();shell.GetPropertyBlock(block);block.SetFloat("_Cutoff",1-level);shell.SetPropertyBlock(block);shell.enabled=level>0;visualLevel=level;configured=true;return true;
 }
 void Start(){if(!configured)SetVisualLevel(visualLevel);}
 // shortcut: visual threshold only; electrode immersion, charge, mass and wash/dry state require the C03 assembly.
}
