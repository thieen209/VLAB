using System;
using UnityEngine;

public sealed class VlabChromatography : MonoBehaviour
{
    [SerializeField] string stationaryPhase;
    [SerializeField] Transform origin,front;
    [SerializeField] Transform[] bands;
    [SerializeField] VlabCapillarySpot spot;
    [SerializeField] Vector3 localAxis,localBandOffset;
    [SerializeField] double heightM;
    double[] distances=Array.Empty<double>();
    public string Phase=>stationaryPhase;
    public VlabCapillarySpot Spot=>spot;
    public Transform Origin=>origin;
    public Transform Front=>front;
    public Transform Band(int index)=>bands[index];
    public Vector3 Axis=>transform.TransformDirection(localAxis).normalized;
    public double FrontDistanceM{get;private set;}
    public string Solvent{get;private set;}="";
    public bool HasMeasurement=>distances.Length>0;
    public int BandCount=>distances.Length;
    public double Rf(int index)=>distances[index]/FrontDistanceM;
    public bool SetMeasurements(string phase,string solvent,double frontDistanceM,double[] bandDistancesM,double solventHeightM)
    {
        if(phase!=stationaryPhase||string.IsNullOrWhiteSpace(solvent)||solvent.Length>120||bandDistancesM==null||bandDistancesM.Length<1||bandDistancesM.Length>bands.Length)return false;
        if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||spot.AppliedUl<=0||spot.Wet)return false;
        if(!Finite(frontDistanceM)||frontDistanceM<=0||frontDistanceM>heightM-.010-.002||!Finite(solventHeightM)||solventHeightM<0||solventHeightM>=.010)return false;
        foreach(double d in bandDistancesM)if(!Finite(d)||d<0||d>frontDistanceM)return false;
        distances=(double[])bandDistancesM.Clone();FrontDistanceM=frontDistanceM;Solvent=solvent;
        front.position=origin.position+Axis*(float)frontDistanceM;front.gameObject.SetActive(true);
        for(int i=0;i<bands.Length;i++){bands[i].gameObject.SetActive(i<distances.Length);if(i<distances.Length)bands[i].position=origin.position+Axis*(float)distances[i]+transform.TransformVector(localBandOffset);}
        return true;
    }
    static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
    public void ClearTrial(){distances=Array.Empty<double>();FrontDistanceM=0;Solvent="";front.gameObject.SetActive(false);foreach(var band in bands)band.gameObject.SetActive(false);spot.ClearTrial();}
    void Start()=>ClearTrial();
    // shortcut: measured/illustrative positions only; add solvent-specific transport after a calibrated dataset exists.
}
