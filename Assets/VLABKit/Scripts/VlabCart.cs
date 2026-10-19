using System;
using UnityEngine;

public sealed class VlabCart : MonoBehaviour
{
    [SerializeField] private string cartId;
    [SerializeField] private Transform travelFrame;
    [SerializeField] private Quaternion bodyRotation;
    public string CartId=>cartId;
    public double MassKg {get;private set;}=.2;
    public double PositionM {get;private set;}
    public double VelocityMps {get;private set;}
    public double ElapsedSeconds {get;private set;}
    public double WorkJ {get;private set;}
    public double StopLossJ {get;private set;}
    public double ImpactVelocity {get;private set;}
    public bool AtLimit {get;private set;}
    public bool Paused {get;set;}=true;
    public double KineticJ=>.5*MassKg*VelocityMps*VelocityMps;
    public Transform TravelFrame=>travelFrame;
    public bool UnitFrame=>travelFrame!=null&&Vector3.Distance(transform.lossyScale,Vector3.one)<1e-5f&&Vector3.Distance(travelFrame.lossyScale,Vector3.one)<1e-5f;
    static void Range(double value,double low,double high){if(double.IsNaN(value)||double.IsInfinity(value)||value<low||value>high)throw new ArgumentOutOfRangeException(nameof(value));}
    public void BindTravelFrame(Transform frame)
    {
        if(frame==null||frame==transform||frame.IsChildOf(transform)||Vector3.Distance(frame.lossyScale,Vector3.one)>1e-5f)throw new ArgumentException("Independent unit travel frame required.");
        travelFrame=frame;Paused=true;ApplyPose();
    }
    public bool ResetTrial(double massKg,double position,double velocity)
    {
        Range(massKg,.01,2);Range(position,-.4,.4);Range(velocity,-5,5);
        if(!UnitFrame)return false;
        MassKg=massKg;PositionM=position;VelocityMps=velocity;ElapsedSeconds=WorkJ=StopLossJ=ImpactVelocity=0;AtLimit=false;Paused=true;ApplyPose();return true;
    }
    public void Advance(double dt,double forceN)
    {
        Range(dt,0,60);Range(forceN,-2,2);
        if(Paused||AtLimit||!isActiveAndEnabled||!UnitFrame||dt==0)return;
        double a=forceN/MassKg,step=dt;bool stopped=false;double boundary=0;
        void Candidate(double time,double end)
        {
            if(time>=0&&time<=step){step=time;boundary=end;stopped=true;}
        }
        foreach(double end in new[]{-.4,.4})
        {
            double delta=end-PositionM;
            if(Math.Abs(delta)<1e-12&&((end>0&&(VelocityMps>0||VelocityMps==0&&a>0))||(end<0&&(VelocityMps<0||VelocityMps==0&&a<0)))){Candidate(0,end);continue;}
            if(Math.Abs(a)<1e-12)
            {if(Math.Abs(VelocityMps)>1e-12){double time=delta/VelocityMps;if(time>1e-10)Candidate(time,end);}continue;}
            double disc=VelocityMps*VelocityMps+2*a*delta;if(disc<0)continue;
            double root=Math.Sqrt(disc),q=-.5*(VelocityMps+(VelocityMps>=0?root:-root));
            double t1=q/(.5*a),t2=q==0?0:-delta/q;
            if(t1>1e-10)Candidate(t1,end);if(t2>1e-10)Candidate(t2,end);
        }
        double next=PositionM+VelocityMps*step+.5*a*step*step;
        if(stopped)next=boundary;
        WorkJ+=forceN*(next-PositionM);PositionM=next;VelocityMps+=a*step;ElapsedSeconds+=step;
        // shortcut: review boundary removes kinetic energy; real end-stop/bumper response belongs to later assets.
        if(stopped){ImpactVelocity=VelocityMps;StopLossJ+=KineticJ;VelocityMps=0;AtLimit=true;Paused=true;}
        ApplyPose();
    }
    public void ApplyPose(){transform.SetPositionAndRotation(travelFrame.TransformPoint(new Vector3((float)PositionM,0,0)),travelFrame.rotation*bodyRotation);}
}
