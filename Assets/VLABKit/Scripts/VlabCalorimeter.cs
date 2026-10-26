using System;
using System.Linq;
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
    public double AdiabaticTemperatureC => TotalHeatCapacity>0?InitialTemperatureC+ReactionHeatJ/TotalHeatCapacity:InitialTemperatureC;
    public double StoredHeatJ => TotalHeatCapacity*(TemperatureC-InitialTemperatureC);
    public double AmbientC => 25;
    public double HeatLeakWattsPerK => LossEnabled?(LidOpen?.6:.15):0;
    public double AcidReceivedMl { get; private set; }
    public double BaseReceivedMl { get; private set; }
    public double AcidMoles { get; private set; }
    public double BaseMoles { get; private set; }
    public double ReactedMoles => ReleasedHeatJ/57000;
    public double LastPourSeconds { get; private set; }
    public double PeakAfterLastPourC { get; private set; }
    public double PeakAfterLastPourSeconds { get; private set; }
    private double trialVolume,initialSensibleJ;
    private bool acceptingPours;
    private bool hasLidRest;private Vector3 lidRestLocal;
    public bool HasOpenLidRest=>hasLidRest;

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
        AcidReceivedMl=acidMl;BaseReceivedMl=baseMl;AcidMoles=acidMl*acidM/1000;BaseMoles=baseMl*baseM/1000;
        initialSensibleJ=capacity*initial;LastPourSeconds=PeakAfterLastPourSeconds=0;PeakAfterLastPourC=initial;acceptingPours=false;
    }

    public void BeginPourTrial(double cupJPerK,double cupC)
    {
        if(!isActiveAndEnabled)throw new InvalidOperationException("Calorimeter is disabled.");
        Range(cupJPerK,0,1000);Range(cupC,0,100);
        vessel.Remove(vessel.VolumeMl);trialVolume=0;CupHeatCapacity=TotalHeatCapacity=cupJPerK;
        initialSensibleJ=cupJPerK*cupC;InitialTemperatureC=TemperatureC=PeakTemperatureC=PeakAfterLastPourC=cupC;
        AcidReceivedMl=BaseReceivedMl=AcidMoles=BaseMoles=LimitingMoles=0;
        ReleasedHeatJ=LostHeatJ=ElapsedSeconds=LastPourSeconds=PeakAfterLastPourSeconds=0;
        Started=false;Paused=true;Loaded=acceptingPours=true;
    }

    // The caller validates the real stream path; this transaction owns volume, mol and heat together.
    public double TryPourFrom(VlabBeaker source,bool isAcid,double concentrationM,double sourceC,double requestedMl,out string reason)
    {
        reason="";
        if(!isActiveAndEnabled||!Loaded||!acceptingPours||!Started||Paused){reason="Start the pouring trial before adding solution.";return 0;}
        if(!LidOpen){reason="Open the calorimeter lid first.";return 0;}
        if(!source||!source.isActiveAndEnabled||source==vessel||!vessel.isActiveAndEnabled||
            !double.IsFinite(concentrationM)||concentrationM<0||concentrationM>1||!double.IsFinite(sourceC)||sourceC<0||sourceC>100||!double.IsFinite(requestedMl)||requestedMl<=0)
        {reason="Invalid source, concentration, temperature or requested volume.";return 0;}
        if((source.transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f||(vessel.transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f)
        {reason="Use the supplied vessels at their original scale.";return 0;}
        if(Math.Abs(vessel.VolumeMl-trialVolume)>1e-8){reason="Solution volume changed outside the thermal transaction.";return 0;}
        double amount=Math.Min(requestedMl,Math.Min(source.VolumeMl,vessel.CapacityMl-trialVolume));
        if(amount<=0){reason="Source empty or receiver full.";return 0;}
        double capacity=TotalHeatCapacity+amount*4.18;
        double temperature=(TotalHeatCapacity*TemperatureC+amount*4.18*sourceC)/capacity;
        double acid=AcidMoles+(isAcid?amount*concentrationM/1000:0),baseAmount=BaseMoles+(isAcid?0:amount*concentrationM/1000);
        double limiting=Math.Min(acid,baseAmount);
        if(temperature+Math.Max(0,limiting*57000-ReleasedHeatJ)/capacity>100)
        {reason="Boiling is outside this model.";return 0;}
        double moved=source.TransferTo(vessel,amount);
        if(moved!=amount)throw new InvalidOperationException("The validated solution transfer changed unexpectedly.");
        trialVolume+=moved;initialSensibleJ+=moved*4.18*sourceC;TotalHeatCapacity=capacity;
        InitialTemperatureC=initialSensibleJ/capacity;TemperatureC=temperature;PeakTemperatureC=Math.Max(PeakTemperatureC,temperature);
        AcidMoles=acid;BaseMoles=baseAmount;LimitingMoles=limiting;
        if(isAcid)AcidReceivedMl+=moved;else BaseReceivedMl+=moved;
        LastPourSeconds=PeakAfterLastPourSeconds=ElapsedSeconds;PeakAfterLastPourC=temperature;
        return moved;
    }

    public bool StartMix()
    {
        if(!isActiveAndEnabled||!Loaded||Started)return false;
        Started=true;Paused=false;return true;
    }

    public void SetLid(bool open)
    {
        if(!isActiveAndEnabled)return;
        lid.position=open?(hasLidRest?transform.TransformPoint(lidRestLocal):liftSocket.position):closedSocket.position;LidOpen=open;
    }

    public bool TrySetOpenLidRest(Bounds support,Bounds[] occupied)
    {
        bool Finite(Bounds b)=>float.IsFinite(b.center.x)&&float.IsFinite(b.center.y)&&float.IsFinite(b.center.z)&&float.IsFinite(b.size.x)&&float.IsFinite(b.size.y)&&float.IsFinite(b.size.z)&&b.size.x>=0&&b.size.y>=0&&b.size.z>=0;
        if(!isActiveAndEnabled||!Finite(support)||support.size.x<=0||support.size.z<=0||occupied==null||occupied.Any(b=>!Finite(b)))return false;
        var lids=lid.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        var cup=GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!r.transform.IsChildOf(lid)).ToArray();
        if(lids.Length==0||cup.Length==0)return false;
        var cover=lids[0].bounds;foreach(var r in lids.Skip(1))cover.Encapsulate(r.bounds);
        var body=cup[0].bounds;foreach(var r in cup.Skip(1))body.Encapsulate(r.bounds);
        foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left})
        {
            var centre=body.center+Vector3.Scale(direction,body.extents+cover.extents+Vector3.one*.02f);
            var delta=new Vector3(centre.x-cover.center.x,support.max.y+.002f-cover.min.y,centre.z-cover.center.z);
            var placed=cover;placed.center+=delta;
            if(placed.min.x<support.min.x+.005f||placed.max.x>support.max.x-.005f||placed.min.z<support.min.z+.005f||placed.max.z>support.max.z-.005f||occupied.Any(placed.Intersects))continue;
            lidRestLocal=transform.InverseTransformPoint(lid.position+delta);hasLidRest=true;return true;
        }
        return false;
    }

    public void Advance(double seconds)
    {
        Range(seconds,0,1e6);
        if(!isActiveAndEnabled||!Loaded||!Started||Paused||seconds==0)return;
        if(Math.Abs(vessel.VolumeMl-trialVolume)>1e-8)throw new InvalidOperationException("Reload thermal state after changing solution volume.");
        if(TotalHeatCapacity==0){ElapsedSeconds+=seconds;return;}
        // shortcut: illustrative 2 s mixing and Newton heat leak; measured response must replace these demonstration coefficients.
        const double alpha=.5;
        double lambda=HeatLeakWattsPerK/TotalHeatCapacity,theta=TemperatureC-AmbientC;
        double remaining=Math.Max(0,ReactionHeatJ-ReleasedHeatJ),rise=remaining/TotalHeatCapacity;
        double Theta(double t)
        {
            double decay=Math.Exp(-lambda*t);
            return Math.Abs(alpha-lambda)<1e-10?decay*(theta+rise*alpha*t):theta*decay+rise*alpha/(alpha-lambda)*(decay-Math.Exp(-alpha*t));
        }
        double next=AmbientC+Theta(seconds),peak=Math.Max(TemperatureC,next),peakOffset=next>TemperatureC?seconds:0;
        if(lambda>0&&rise>0)
        {
            double candidate;
            if(Math.Abs(alpha-lambda)<1e-10)candidate=(1-theta/rise)/alpha;
            else
            {
                double a=theta+rise*alpha/(alpha-lambda),b=-rise*alpha/(alpha-lambda);
                double ratio=-alpha*b/(lambda*a);candidate=ratio>0?Math.Log(ratio)/(alpha-lambda):-1;
            }
            if(candidate>0&&candidate<seconds&&AmbientC+Theta(candidate)>peak){peak=AmbientC+Theta(candidate);peakOffset=candidate;}
        }
        double released=remaining*(1-Math.Exp(-alpha*seconds));
        LostHeatJ+=released-TotalHeatCapacity*(next-TemperatureC);
        if(peak>PeakAfterLastPourC){PeakAfterLastPourC=peak;PeakAfterLastPourSeconds=ElapsedSeconds+peakOffset;}
        ReleasedHeatJ+=released;TemperatureC=next;PeakTemperatureC=Math.Max(PeakTemperatureC,peak);ElapsedSeconds+=seconds;
    }

    private void OnDisable(){Paused=true;}
}
