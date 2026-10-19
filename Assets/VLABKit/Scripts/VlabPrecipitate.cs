using System.Linq;
using UnityEngine;
public sealed class VlabPrecipitate : MonoBehaviour
{
 [SerializeField] Renderer sediment;
 [SerializeField] double vesselMl;
 [SerializeField] Quaternion bodyInVessel;
 [SerializeField] Vector3 vesselLocalUp,heightAxis;
 public string SpeciesId=>"AgCl";
 public double AmountMol{get;private set;}
 public float VisualLevel{get;private set;}
 public Renderer Sediment=>sediment;
 public bool QualitativePresent{get;private set;}
 // Explicit qualitative API: no invented molar amount is fed to the old API.
 public bool TrySetQualitative(bool present,float level=0.35f)
 {
  if(!Ready||!float.IsFinite(level)||level<0||level>1)return false;
  AmountMol=0;QualitativePresent=present;VisualLevel=present?level:0;
  sediment.transform.localScale=Vector3.one+heightAxis*(Mathf.Max(VisualLevel,.0001f)-1);sediment.enabled=present;return true;
 }
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TrySetState(string speciesId,double amountMol,float visualLevel){if(!Ready||speciesId!="AgCl"||!double.IsFinite(amountMol)||amountMol<0||!float.IsFinite(visualLevel)||visualLevel<0||visualLevel>1||amountMol==0&&visualLevel>0)return false;QualitativePresent=false;AmountMol=amountMol;VisualLevel=visualLevel;sediment.transform.localScale=Vector3.one+heightAxis*(Mathf.Max(visualLevel,.0001f)-1);sediment.enabled=amountMol>0&&visualLevel>0;return true;}
 public bool TryAttachTo(VlabBeaker cup){if(!Ready||!cup||!cup.isActiveAndEnabled||cup.CapacityMl!=vesselMl||(cup.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||cup.transform.IsChildOf(transform))return false;var floor=cup.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name=="VLAB_beaker_InteriorBase_SOCKET");if(!floor||!float.IsFinite(floor.position.sqrMagnitude))return false;transform.SetPositionAndRotation(floor.position+cup.transform.TransformDirection(vesselLocalUp)*.00005f,cup.transform.rotation*bodyInVessel);transform.SetParent(cup.transform,true);return true;}
 void OnDisable(){if(sediment)sediment.enabled=false;}
 void OnEnable(){if(sediment)sediment.enabled=QualitativePresent||AmountMol>0&&VisualLevel>0;}
 // shortcut: AgCl amount is supplied data; separate0..1 illustrative height is not density/volume calibration or reaction kinetics.
}
