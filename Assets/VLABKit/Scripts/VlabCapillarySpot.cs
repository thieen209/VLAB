using UnityEngine;

public sealed class VlabCapillarySpot : MonoBehaviour
{
    [SerializeField] private MeshCollider surface;
    [SerializeField] private Renderer spotVisual;
    [SerializeField] private Vector3 localNormal;
    public double AppliedUl {get;private set;}
    public bool Wet {get;private set;}
    public int SpotCount {get;private set;}
    public Vector3 Center=>surface.transform.TransformPoint(Vector3.zero);
    public Vector3 Normal=>surface.transform.TransformDirection(localNormal).normalized;
    void Awake(){if(surface==null||spotVisual==null||localNormal.sqrMagnitude<.99f)throw new System.InvalidOperationException("Spot surface frame missing.");}
    public bool CanAccept(VlabCapillary tool,double amount)
    {
        VlabCapillary.Amount(amount);
        if(tool==null||!isActiveAndEnabled||!surface.enabled||Wet||amount<=0||amount>1||AppliedUl+amount>2+1e-12||Vector3.Distance(transform.lossyScale,Vector3.one)>1e-5f)return false;
        Vector3 d=tool.Spot.position-Center;float height=Vector3.Dot(d,Normal);
        return height>=-.00001f&&height<=.0001f&&(d-Normal*height).magnitude<=.00025f&&Vector3.Dot(tool.Axis,Normal)>.9848f;
    }
    internal void Receive(double amount){AppliedUl+=amount;Wet=true;SpotCount++;Refresh();}
    // shortcut: explicit dry confirmation without invented drying kinetics; add solvent-specific timing in B06.
    public void ConfirmDry(){Wet=false;Refresh();}
    public void ClearTrial(){AppliedUl=0;Wet=false;SpotCount=0;Refresh();}
    void Start(){Refresh();}
    void Refresh()
    {
        spotVisual.gameObject.SetActive(AppliedUl>0);
        var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",Wet?new Color(.10f,.25f,.025f):new Color(.32f,.40f,.08f));spotVisual.SetPropertyBlock(block);
    }
}
