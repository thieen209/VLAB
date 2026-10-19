using UnityEngine;
public sealed class VlabClamp : MonoBehaviour
{
    [SerializeField] Transform movingJaw,screw,grip;
    [SerializeField] Vector3 jawAtZero,gripAtZero,localAxis;
    [SerializeField] Quaternion screwAtZero;
    public double OpeningM{get;private set;}
    public const double PitchM=.002;
    public Transform MovingJaw=>movingJaw;
    public Transform Screw=>screw;
    public Transform Grip=>grip;
    public Vector3 Axis=>movingJaw.parent.TransformDirection(localAxis).normalized;
    public bool SetOpening(double metres)
    {
        if(!isActiveAndEnabled||double.IsNaN(metres)||double.IsInfinity(metres)||metres<0||metres>.050||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
        OpeningM=metres;
        movingJaw.localPosition=jawAtZero+localAxis*(float)metres;
        grip.localPosition=gripAtZero+localAxis*(float)(metres/2);
        screw.localRotation=Quaternion.AngleAxis((float)((metres/PitchM*360)%360),localAxis)*screwAtZero;
        return true;
    }
    public bool Turn(double revolutions)=>SetOpening(OpeningM+revolutions*PitchM);
    void Start()=>SetOpening(.025);
    // shortcut: pitch controls geometry only; calibrated grip force and stand constraints require their own validated integration.
}
