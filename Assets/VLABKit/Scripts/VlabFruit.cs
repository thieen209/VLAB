using UnityEngine;
public sealed class VlabFruit : MonoBehaviour
{
 [SerializeField] GameObject whole,pieces;
 [SerializeField] BoxCollider shape;
 [SerializeField] Bounds wholeBounds,piecesBounds;
 [SerializeField] VlabMassSample sample;
 [SerializeField] string sampleId="";
 [SerializeField] bool crushed;
 public string SampleId=>sampleId;
 public double MassGrams=>sample.MassGrams;
 public bool Crushed=>crushed;
 bool Usable=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<=1e-10f;
 public bool ConfigureSample(string id,double grams){
 if(!Usable||string.IsNullOrWhiteSpace(id)||id.Length>128||!double.IsFinite(grams)||grams<=0)return false;
 sample.SetMass(grams);sampleId=id;return true;
 }
 public bool SetCrushed(bool value){
 if(!Usable)return false;whole.SetActive(!value);pieces.SetActive(value);var b=value?piecesBounds:wholeBounds;shape.center=b.center;shape.size=b.size;crushed=value;return true;
 }
 // shortcut: visual crushing keeps assigned sample mass; transfer losses and extraction chemistry require B07 integration.
}
