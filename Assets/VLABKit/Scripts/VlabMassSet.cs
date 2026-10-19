using UnityEngine;
[RequireComponent(typeof(VlabMassSample))]
public sealed class VlabMassSet : MonoBehaviour
{
 [SerializeField] Transform[] discs;
 [SerializeField] Vector3 localUp;
 VlabMassSample sample;
 public int SelectedMask{get;private set;}
 public double HangerGrams{get;private set;}
 public double TotalGrams=>(sample?sample:(sample=GetComponent<VlabMassSample>())).MassGrams;
 public bool TrySetLoad(int mask,double hangerGrams){if(!isActiveAndEnabled||mask<0||mask>7||!double.IsFinite(hangerGrams)||hangerGrams<0||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;double total=hangerGrams;float z=.002f;for(int i=0;i<3;i++){bool active=(mask&(1<<i))!=0;discs[i].gameObject.SetActive(active);if(active){discs[i].localPosition=localUp*z;z+=i==0?.004f:i==1?.006f:.010f;total+=i==0?50:i==1?100:200;}}(sample?sample:(sample=GetComponent<VlabMassSample>())).SetMass(total);sample.enabled=true;SelectedMask=mask;HangerGrams=hangerGrams;return true;}
 // shortcut: prescribed nominal disc grams plus explicit hanger mass; no geometric density or certified hardware calibration.
}

