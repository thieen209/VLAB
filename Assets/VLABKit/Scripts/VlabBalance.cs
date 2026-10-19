using System;
using System.Globalization;
using UnityEngine;

public sealed class VlabBalance : MonoBehaviour
{
    [SerializeField] private Transform panCenter;
    [SerializeField] private TextMesh readout;
    [SerializeField] private Transform tareButton;
    [SerializeField] private Vector3 buttonTravelLocal;
    [SerializeField] private double resolutionGrams=.001;
    private Vector3 buttonRest;
    private float pressTime=-1;
    public double GrossGrams { get; private set; }
    public Transform PanCenter=>panCenter;
    public double TareGrams { get; private set; }
    public double RawNetGrams => GrossGrams-TareGrams;
    public double ResolutionGrams => resolutionGrams;
    public double ReadingGrams => Math.Round(RawNetGrams/resolutionGrams,MidpointRounding.AwayFromZero)*resolutionGrams;
    public string DisplayText => (ReadingGrams==0?0:ReadingGrams).ToString(resolutionGrams==.001?"F3":"F2",CultureInfo.InvariantCulture)+" g";

    private void Awake()
    {
        if(panCenter==null || readout==null || tareButton==null) { enabled=false; Debug.LogError("Balance references missing.",this); return; }
        buttonRest=tareButton.localPosition;
        SetResolution(resolutionGrams);
    }

    public void SetResolution(double grams)
    {
        if(grams!=.001 && grams!=.01) throw new ArgumentOutOfRangeException(nameof(grams),"Use 0.001 g or 0.01 g.");
        resolutionGrams=grams;
        RefreshReading();
    }

    public bool Supports(VlabMassSample sample)
    {
        if(panCenter==null || sample==null || !sample.isActiveAndEnabled || sample.IsHeld || sample.Shape==null || !sample.Shape.enabled) return false;
        var bounds=sample.Shape.bounds;
        var normal=panCenter.up;
        var delta=bounds.center-panCenter.position;
        float halfHeight=Vector3.Dot(new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z)),bounds.extents);
        float clearance=Vector3.Dot(delta,normal)-halfHeight;
        if(clearance<-.0005f || clearance>.001f) return false;
        if(sample.Shape is MeshCollider mesh && mesh.sharedMesh!=null && mesh.sharedMesh.isReadable)
        {
            foreach(var vertex in mesh.sharedMesh.vertices)
                if(Vector3.ProjectOnPlane(mesh.transform.TransformPoint(vertex)-panCenter.position,normal).sqrMagnitude>.0485f*.0485f) return false;
            return true;
        }
        float footprint;
        if(sample.Shape is SphereCollider sphere)
            footprint=sphere.radius*Mathf.Max(Mathf.Abs(sphere.transform.lossyScale.x),Mathf.Abs(sphere.transform.lossyScale.y),Mathf.Abs(sphere.transform.lossyScale.z));
        else
            footprint=Mathf.Sqrt(bounds.extents.x*bounds.extents.x+bounds.extents.z*bounds.extents.z);
        // shortcut: direct support on a level pan; stacked containers require a later load-transfer model.
        return Vector3.ProjectOnPlane(delta,normal).magnitude+footprint<=.0485f;
    }

    public void RefreshReading()
    {
        double sum=0;
        foreach(var sample in VlabMassSample.Active) if(Supports(sample)) sum+=sample.MassGrams;
        GrossGrams=sum;
        if(readout!=null) readout.text=DisplayText;
    }

    public void Tare()
    {
        RefreshReading();
        TareGrams=GrossGrams;
        pressTime=Time.unscaledTime;
        RefreshReading();
    }

    public void ClearTare() { TareGrams=0; RefreshReading(); }
    private void FixedUpdate() => RefreshReading();
    private void Update()
    {
        if(tareButton!=null)
        {
            float phase=pressTime<0?1:Mathf.Clamp01((Time.unscaledTime-pressTime)/.18f);
            tareButton.localPosition=buttonRest+buttonTravelLocal*Mathf.Sin(phase*Mathf.PI);
        }
    }
}
