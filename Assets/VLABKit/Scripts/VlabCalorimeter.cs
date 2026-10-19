using System;
using UnityEngine;

public sealed class VlabCalorimeter : MonoBehaviour
{
    [SerializeField] private VlabBeaker vessel;
    [SerializeField] private Transform lid, closedSocket, liftSocket;
    public VlabBeaker Vessel => vessel;
    public Transform Lid => lid;
    public bool LidOpen { get; private set; }
    public bool Paused { get; set; } = true;
    public bool LossEnabled { get; set; }
    public bool Started { get; private set; }
    public bool Loaded { get; private set; }
    public double TemperatureC { get; private set; }
    public double InitialTemperatureC { get; private set; }
    public double PeakTemperatureC { get; private set; }
    public double CupHeatCapacity { get; private set; }
    public double TotalHeatCapacity { get; private set; }
    public double LimitingMoles { get; private set; }
    public double ReactionHeatJ => LimitingMoles*57000;
    public double ReleasedHeatJ { get; private set; }
    public double LostHeatJ { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public double AdiabaticTemperatureC => InitialTemperatureC+ReactionHeatJ/TotalHeatCapacity;
    public double StoredHeatJ => TotalHeatCapacity*(TemperatureC-InitialTemperatureC);
    public double AmbientC => 25;
    public double HeatLeakWattsPerK => LossEnabled?(LidOpen?.6:.15):0;
    private double trialVolume;

    static void Range(double value,double min,double max)
    {
        if(double.IsNaN(value)||double.IsInfinity(value)||value<min||value>max)throw new ArgumentOutOfRangeException(nameof(value));
    }

    public void LoadTrial(double acidMl,double acidM,double acidC,double baseMl,double baseM,double baseC,double cupJPerK,double cupC)
    {
        if(!isActiveAndEnabled)throw new InvalidOperationException("Calorimeter is disabled.");
        Range(acidMl,0,100);Range(baseMl,0,100);Range(acidM,0,1);Range(baseM,0,1);
        Range(acidC,0,100);Range(baseC,0,100);Range(cupC,0,100);Range(cupJPerK,0,1000);
        double volume=acidMl+baseMl;if(volume<=0||volume>vessel.CapacityMl)throw new ArgumentOutOfRangeException(nameof(acidMl));
        // shortcut: dilute strong acid/base, density 1 g/mL and c=4.18 J/g/K; extend with solution data for other chemistry.
        double capacity=volume*4.18+cupJPerK;
        double initial=(acidMl*4.18*acidC+baseMl*4.18*baseC+cupJPerK*cupC)/capacity;
        double moles=Math.Min(acidMl*acidM,baseMl*baseM)/1000;
        if(initial+moles*57000/capacity>100)throw new ArgumentOutOfRangeException(nameof(acidC),"Boiling is outside this model.");
        vessel.Remove(vessel.VolumeMl);vessel.Add(volume);trialVolume=volume;
        CupHeatCapacity=cupJPerK;TotalHeatCapacity=capacity;LimitingMoles=moles;
        InitialTemperatureC=TemperatureC=PeakTemperatureC=initial;
        ReleasedHeatJ=LostHeatJ=ElapsedSeconds=0;Started=false;Paused=true;Loaded=true;
    }

    public bool StartMix()
    {
        if(!isActiveAndEnabled||!Loaded||Started)return false;
        Started=true;Paused=false;return true;
    }

    public void SetLid(bool open)
    {
        if(!isActiveAndEnabled)return;
        lid.position=open?liftSocket.position:closedSocket.position;LidOpen=open;
    }

    public void Advance(double seconds)
    {
        Range(seconds,0,1e6);
        if(!isActiveAndEnabled||!Loaded||!Started||Paused||seconds==0)return;
        if(Math.Abs(vessel.VolumeMl-trialVolume)>1e-8)throw new InvalidOperationException("Reload thermal state after changing solution volume.");
        // shortcut: illustrative 2 s mixing and Newton heat leak; measured response must replace these demonstration coefficients.
        const double alpha=.5;
        double lambda=HeatLeakWattsPerK/TotalHeatCapacity,theta=TemperatureC-AmbientC;
        double remaining=Math.Max(0,ReactionHeatJ-ReleasedHeatJ),rise=remaining/TotalHeatCapacity;
        double Theta(double t)
        {
            double decay=Math.Exp(-lambda*t);
            return Math.Abs(alpha-lambda)<1e-10?decay*(theta+rise*alpha*t):theta*decay+rise*alpha/(alpha-lambda)*(decay-Math.Exp(-alpha*t));
        }
        double next=AmbientC+Theta(seconds),peak=Math.Max(TemperatureC,next);
        if(lambda>0&&rise>0)
        {
            double candidate;
            if(Math.Abs(alpha-lambda)<1e-10)candidate=(1-theta/rise)/alpha;
            else
            {
                double a=theta+rise*alpha/(alpha-lambda),b=-rise*alpha/(alpha-lambda);
                double ratio=-alpha*b/(lambda*a);candidate=ratio>0?Math.Log(ratio)/(alpha-lambda):-1;
            }
            if(candidate>0&&candidate<seconds)peak=Math.Max(peak,AmbientC+Theta(candidate));
        }
        double released=remaining*(1-Math.Exp(-alpha*seconds));
        LostHeatJ+=released-TotalHeatCapacity*(next-TemperatureC);
        ReleasedHeatJ+=released;TemperatureC=next;PeakTemperatureC=Math.Max(PeakTemperatureC,peak);ElapsedSeconds+=seconds;
    }

    private void OnDisable(){Paused=true;}
}
