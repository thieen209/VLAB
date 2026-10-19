using UnityEngine;

public sealed class VlabElectricalTerminal : MonoBehaviour
{
    [SerializeField] private string nodeId;
    [SerializeField] private float nominalDiameter=.004f;
    [SerializeField] private Transform front,back;
    private VlabCable owner;
    private int ownerEnd;
    public string NodeId => nodeId;
    // A lesson may author a connector at a delivered electrode contact socket.
    // Geometry remains in metres and the same cable insertion checks apply.
    public bool TryConfigureContact(string id,Transform contact,Transform rear)
    {
        if(Occupied||!contact||!rear||contact==rear)return false;
        front=contact;back=rear;nominalDiameter=.004f;return TrySetNodeId(id)&&Compatible;
    }
    public bool TrySetNodeId(string id)
    {
        if(!enabled||!gameObject.activeInHierarchy||Occupied||string.IsNullOrWhiteSpace(id)||id.Length>128)return false;
        foreach(char c in id)if(char.IsControl(c))return false;
        nodeId=id;return true;
    }
    public Vector3 Axis => front!=null&&back!=null?(front.position-back.position).normalized:Vector3.zero;
    public Vector3 InsertedTip => front.position-Axis*.012f;
    public bool Compatible => isActiveAndEnabled&&!string.IsNullOrWhiteSpace(nodeId)&&front!=null&&back!=null&&Mathf.Abs(nominalDiameter-.004f)<.000001f&&(front.position-back.position).sqrMagnitude>.000001f&&Vector3.Distance(front.lossyScale,Vector3.one)<.0001f;
    public bool Occupied => owner!=null;
    internal bool Claim(VlabCable cable,int end)
    {
        if(!Compatible||(owner!=null&&(owner!=cable||ownerEnd!=end)))return false;
        owner=cable;ownerEnd=end;return true;
    }
    internal void Release(VlabCable cable,int end){if(owner==cable&&ownerEnd==end)owner=null;}
    private void OnDisable(){if(owner!=null)owner.Disconnect(ownerEnd);}
}

