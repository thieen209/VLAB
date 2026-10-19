using System.Linq;
using UnityEngine;

public sealed class VlabBeltPulley : MonoBehaviour
{
    [SerializeField] private VlabBelt belt;
    [SerializeField] private Transform small,large,smallFront,smallBack,largeFront,largeBack;
    private Quaternion smallRest,largeRest;
    private Vector3 smallAxis,largeAxis;
    private Transform anchorA,anchorB;
    private int selected=-1;
    public Transform Small=>small;
    public Transform Large=>large;
    public Quaternion SmallRest=>smallRest;
    public Quaternion LargeRest=>largeRest;
    public Vector3 SmallLocalAxis=>smallAxis;
    public Vector3 LargeLocalAxis=>largeAxis;
    private void Awake()
    {
        smallRest=small.localRotation;largeRest=large.localRotation;
        smallAxis=small.InverseTransformDirection((smallBack.position-smallFront.position).normalized);
        largeAxis=large.InverseTransformDirection((largeBack.position-largeFront.position).normalized);
        ApplyPose();
    }
    private void LateUpdate()=>ApplyPose();
    public void ApplyPose()
    {
        if(selected!=belt.Selected)
        {
            var sockets=belt.Current.root.GetComponentsInChildren<Transform>(true);
            anchorA=sockets.Single(t=>t.name.EndsWith("_PulleyA_SOCKET"));
            anchorB=sockets.Single(t=>t.name.EndsWith("_PulleyB_SOCKET"));selected=belt.Selected;
        }
        small.position=anchorA.position;large.position=anchorB.position;
        // Blender X/Z path winding is negative rotation about the authored +Y shaft axis.
        small.localRotation=smallRest*Quaternion.AngleAxis(-(float)(belt.SmallTurns*360),smallAxis);
        large.localRotation=largeRest*Quaternion.AngleAxis(-(float)(belt.LargeTurns*360),largeAxis);
    }
}
