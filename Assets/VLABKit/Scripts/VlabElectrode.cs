using UnityEngine;
public sealed class VlabElectrode : MonoBehaviour
{
 [SerializeField] Renderer plate,rod;
 [SerializeField] Material copper,zinc,graphite;
 [SerializeField] Transform plateContact,rodContact;
 [SerializeField] VlabCopperCoating coating;
 [SerializeField] string electrodeID="Cu";
 public string ElectrodeID=>electrodeID;
 public Transform Contact=>ElectrodeID=="graphite"?rodContact:plateContact;
 public VlabCopperCoating Coating=>coating;
 public bool SetElectrode(string id)
 {
  if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(id!="Cu"&&id!="Zn"&&id!="graphite"))return false;
  coating.gameObject.SetActive(true);coating.SetVisualLevel(0);
  bool carbon=id=="graphite";plate.gameObject.SetActive(!carbon);rod.gameObject.SetActive(carbon);coating.gameObject.SetActive(!carbon);
  plate.sharedMaterial=id=="Zn"?zinc:copper;rod.sharedMaterial=graphite;electrodeID=id;return true;
 }
 // shortcut: identity and visual coating only; charge, mass and cleaning belong to C02/C03 simulation.
}
