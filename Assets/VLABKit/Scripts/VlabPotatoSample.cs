using UnityEngine;
public sealed class VlabPotatoSample : MonoBehaviour
{
 [SerializeField] VlabMassSample sample;
 public string SampleId{get;private set;}
 public double InitialMassGrams{get;private set;}=double.NaN;
 public double CurrentMassGrams=>double.IsNaN(InitialMassGrams)?double.NaN:sample.MassGrams;
 public double RelativeMassChange=>double.IsNaN(InitialMassGrams)?double.NaN:(sample.MassGrams-InitialMassGrams)/InitialMassGrams;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TryConfigure(string id,double initialGrams){if(!Ready||string.IsNullOrWhiteSpace(id)||id.Length>128||!double.IsFinite(initialGrams)||initialGrams<=0)return false;foreach(char ch in id)if(char.IsControl(ch))return false;SampleId=id.Normalize();InitialMassGrams=initialGrams;sample.SetMass(initialGrams);sample.enabled=true;return true;}
 public bool TrySetMeasuredMass(double grams){if(!Ready||double.IsNaN(InitialMassGrams)||!double.IsFinite(grams)||grams<0||!double.IsFinite((grams-InitialMassGrams)/InitialMassGrams))return false;sample.SetMass(grams);return true;}
 void OnDisable(){if(sample)sample.enabled=false;}
 void OnEnable(){if(sample)sample.enabled=!double.IsNaN(InitialMassGrams);}
 // shortcut: explicit m0/m1 metadata only; osmosis, permeability, drying protocol and preparation yield belong to B03/B04.
}
