using UnityEngine;

public sealed class VlabBearing : MonoBehaviour
{
    [SerializeField] private Transform entry, exit;
    public Vector3 Axis => (exit.position-entry.position).normalized;
    public Vector3 Center => (entry.position+exit.position)*.5f;
    public Transform Entry => entry;
    public Transform Exit => exit;
    public const float BoreRadius=.0031f;
    public bool AcceptsAxle(Vector3 a,Vector3 b,float diameter)
    {
        if(!Finite(a)||!Finite(b)||!float.IsFinite(diameter)||Mathf.Abs(diameter-.006f)>1e-6f) return false;
        var delta=b-a; float length=delta.magnitude;
        if(length<1e-6f) return false;
        var direction=delta/length; float cosine=Vector3.Dot(direction,Axis);
        if(Mathf.Abs(cosine)<.001f) return false;
        float clearance=BoreRadius*Mathf.Cos(Mathf.PI/24)-diameter*.5f/Mathf.Abs(cosine);
        if(clearance<0) return false;
        foreach(var point in new[]{entry.position,exit.position})
        {
            float distance=Vector3.Dot(point-a,Axis)/cosine;
            if(distance<0||distance>length||(a+direction*distance-point).magnitude>clearance) return false;
        }
        return true;
    }
    public bool RotateAxle(Transform axle,Vector3 a,Vector3 b,float diameter,float degrees)
    {
        if(axle==null||!float.IsFinite(degrees)||!AcceptsAxle(a,b,diameter)) return false;
        // shortcut: prescribed rotation without friction or inertia; replace when the shaft drive is integrated.
        axle.RotateAround(Center,Axis,degrees); return true;
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
}
