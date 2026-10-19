using System;
using UnityEngine;

public sealed class VlabCam : MonoBehaviour
{
    [SerializeField] private Transform body,axisSocket,frameZero,frameQuarter;
    [SerializeField] private double[] profileRadii;
    [SerializeField] private double baseRadius,lift;
    private Quaternion rest;
    private Vector3 rotationAxis;
    public Transform Body=>body;
    public Vector3 Center=>axisSocket.position;
    public Vector3 FollowerDirection=>(frameZero.position-Center).normalized;
    public Vector3 ShaftAxis=>Vector3.Cross(FollowerDirection,(frameQuarter.position-Center).normalized).normalized;
    public Vector3 ContactPoint=>Center+FollowerDirection*(float)RadiusAt(AngleDegrees);
    public double AngleDegrees{get;private set;}
    public double Rpm{get;private set;}=30;
    public bool Paused{get;set;}=true;
    public double LiftMeters=>RadiusAt(AngleDegrees)-baseRadius;
    public double IdealLiftMeters=>IdealLiftAt(AngleDegrees);
    public string Phase=>AngleDegrees<120?"Nâng":AngleDegrees<180?"Nghỉ cao":AngleDegrees<300?"Hạ":"Nghỉ thấp";

    static void Finite(double v){if(double.IsNaN(v)||double.IsInfinity(v))throw new ArgumentOutOfRangeException(nameof(v));}
    static double Wrap(double d){Finite(d);return (d%360+360)%360;}
    bool UnitFrame()=>Vector3.Distance(transform.lossyScale,Vector3.one)<1e-5f&&Vector3.Distance(body.localScale,Vector3.one)<1e-5f;
    void Awake()
    {
        if(body==null||axisSocket==null||frameZero==null||frameQuarter==null||profileRadii==null||profileRadii.Length!=180)throw new InvalidOperationException("Cam profile/frame missing.");
        Finite(baseRadius);Finite(lift);if(baseRadius<=0||lift<0)throw new InvalidOperationException("Invalid radii.");
        foreach(double r in profileRadii){Finite(r);if(r<baseRadius-1e-9||r>baseRadius+lift+1e-9)throw new InvalidOperationException("Profile radius outside authored range.");}
        rest=body.localRotation;rotationAxis=body.parent.InverseTransformDirection(ShaftAxis);
    }
    public double RadiusAt(double degrees)
    {
        double t=Wrap(degrees),step=360d/profileRadii.Length;int i=(int)(t/step);
        double f=(t-i*step)*Math.PI/180,d=step*Math.PI/180,r0=profileRadii[i],r1=profileRadii[(i+1)%profileRadii.Length];
        return r0*r1*Math.Sin(d)/(r1*Math.Sin(d-f)+r0*Math.Sin(f));
    }
    public double IdealLiftAt(double degrees)
    {
        double t=Wrap(degrees);
        if(t<120)return lift*(1-Math.Cos(Math.PI*t/120))/2;
        if(t<180)return lift;
        if(t<300)return lift*(1+Math.Cos(Math.PI*(t-180)/120))/2;
        return 0;
    }
    public bool SetAngle(double degrees)
    {
        double value=Wrap(degrees);if(!isActiveAndEnabled||!UnitFrame())return false;
        AngleDegrees=value;body.localRotation=Quaternion.AngleAxis(-(float)value,rotationAxis)*rest;return true;
    }
    public void SetSpeed(double rpm){Finite(rpm);if(Math.Abs(rpm)>120)throw new ArgumentOutOfRangeException(nameof(rpm));Rpm=rpm;}
    public void Advance(double seconds)
    {
        Finite(seconds);if(seconds<0||seconds>3600)throw new ArgumentOutOfRangeException(nameof(seconds));
        if(!Paused&&isActiveAndEnabled)SetAngle(AngleDegrees+Rpm*6*seconds);
    }
    void OnDisable(){Paused=true;}
}
