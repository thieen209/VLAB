using UnityEngine;

public sealed class VlabAirHose : MonoBehaviour
{
    [SerializeField] private Transform connectorASocket;
    [SerializeField] private Transform connectorABase;
    private VlabAirBlower source;
    private Vector3 parkedPosition;
    private Quaternion parkedRotation;
    private const float InsertionDepth = .012f;

    public bool IsConnected => isActiveAndEnabled && source != null && source.isActiveAndEnabled && connectorASocket != null && connectorABase != null &&
        source.OutletSocket != null && Vector3.Distance(connectorASocket.position,
            source.OutletSocket.position-source.OutletAxis*InsertionDepth)<.001f &&
        Vector3.Dot((connectorASocket.position-connectorABase.position).normalized,source.OutletAxis)<-.99f;
    public bool HasSupplyAtA => IsConnected && source.IsOn;

    private void Awake()
    {
        parkedPosition=transform.position;
        parkedRotation=transform.rotation;
    }

    public bool Connect(VlabAirBlower blower)
    {
        if (!isActiveAndEnabled || blower==null || !blower.isActiveAndEnabled || blower.OutletSocket==null ||
            blower.OutletAxis.sqrMagnitude<.99f || connectorASocket==null || connectorABase==null) return false;
        Vector3 axis=(connectorASocket.position-connectorABase.position).normalized;
        if(axis.sqrMagnitude<.99f) return false;
        transform.rotation=Quaternion.FromToRotation(axis,-blower.OutletAxis)*transform.rotation;
        transform.position+=blower.OutletSocket.position-blower.OutletAxis*InsertionDepth-connectorASocket.position;
        source=blower;
        return IsConnected;
    }

    public void Disconnect()
    {
        source=null;
        transform.SetPositionAndRotation(parkedPosition,parkedRotation);
    }
}
