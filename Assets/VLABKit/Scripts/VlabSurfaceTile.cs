using UnityEngine;
public sealed class VlabSurfaceTile:MonoBehaviour{
 [SerializeField]Renderer tile;[SerializeField]Material[] finishes;public int Finish{get;private set;}
 public string FinishId=>new[]{"wood","plastic","abrasive"}[Finish];
 public bool TrySetFinish(int index){if(!isActiveAndEnabled||index<0||index>=finishes.Length||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;var mats=tile.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=finishes[index];tile.sharedMaterials=mats;Finish=index;return true;}
 // shortcut: appearance and pair ID only; calibrated pair coefficients and contact forces remain explicit inputs to the existing friction-block API.
}
