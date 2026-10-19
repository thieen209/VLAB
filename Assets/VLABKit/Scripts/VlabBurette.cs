using System;
using UnityEngine;

public sealed class VlabBurette : MonoBehaviour
{
    public const double CapacityMl=25, FullOpenRateMlPerSecond=.5;
    [SerializeField] private Transform handle, valveFront, valveBack, zero, rangeEnd, liquidSlot, meniscus;
    [SerializeField] private double crossSectionArea;
    private Quaternion openRotation;
    private Vector3 handleAxis;
    private bool initialized;
    public double ReadingMl { get; private set; }
    public double InitialReadingMl { get; private set; }
    public double DeliveredMl => ReadingMl-InitialReadingMl;
    public double RemainingMl => CapacityMl-ReadingMl;
    public double ValveOpening { get; private set; }
    public bool TipPrimed { get; private set; }
    public bool Paused { get; set; }=true;
    public Transform Handle => handle;
    public Transform Meniscus => meniscus;

    private static void Validate(double value,double maximum)
    {
        if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>maximum)throw new ArgumentOutOfRangeException(nameof(value));
    }
    private void Awake()
    {
        Validate(crossSectionArea,double.MaxValue);
        if(handle==null||valveFront==null||valveBack==null||zero==null||rangeEnd==null||liquidSlot==null||meniscus==null||crossSectionArea==0)throw new InvalidOperationException("Burette geometry metadata missing.");
        if(Math.Abs(Vector3.Distance(zero.position,rangeEnd.position)*crossSectionArea*1e6-CapacityMl)>.001)throw new InvalidOperationException("Calibrated range was scaled.");
        openRotation=handle.localRotation;handleAxis=handle.InverseTransformDirection(valveBack.position-valveFront.position).normalized;
        initialized=true;Prepare(0,false);
    }
    // shortcut: caller declares a prepared, primed starting state; integrate actual rinsing/priming with reagent and waste assets later.
    public void Prepare(double initialReading,bool tipPrimed)
    {
        Validate(initialReading,CapacityMl);
        ReadingMl=InitialReadingMl=initialReading;TipPrimed=tipPrimed;Paused=true;SetValve(0);RefreshLiquid();
    }
    public void SetValve(double opening)
    {
        Validate(opening,1);
        if(opening>0&&!isActiveAndEnabled)throw new InvalidOperationException("Disabled burette.");
        ValveOpening=opening;
        handle.localRotation=openRotation*Quaternion.AngleAxis((float)((1-opening)*90),handleAxis);
    }
    public double Advance(double seconds,VlabBeaker receiver)
    {
        Validate(seconds,double.MaxValue);
        if(receiver==null)throw new ArgumentNullException(nameof(receiver));
        if(!isActiveAndEnabled||!receiver.isActiveAndEnabled||Paused||!TipPrimed||ValveOpening==0)return 0;
        // shortcut: prescribed teaching flow, not a calibrated pressure/viscosity law; replace when hydrodynamics is in lesson scope.
        double requested=Math.Min(RemainingMl,seconds*(FullOpenRateMlPerSecond*ValveOpening));
        double moved=receiver.Add(requested);
        ReadingMl+=moved;RefreshLiquid();
        return moved;
    }
    // Product adapter for the delivered conical flask. The lesson places its real
    // outlet in the open receiving cavity; volume and meniscus advance together.
    public double Advance(double seconds,VlabFlask receiver,Vector3 outlet)
    {
        Validate(seconds,double.MaxValue);
        if(receiver==null)throw new ArgumentNullException(nameof(receiver));
        if(!isActiveAndEnabled||!receiver.ContainsPoint(outlet)||Paused||!TipPrimed||ValveOpening==0)return 0;
        double moved=Math.Min(Math.Min(RemainingMl,seconds*FullOpenRateMlPerSecond*ValveOpening),receiver.CapacityMl-receiver.VolumeMl);
        if(moved<=0||!receiver.TrySetVolume(receiver.VolumeMl+moved))return 0;
        ReadingMl+=moved;RefreshLiquid();return moved;
    }
    private void RefreshLiquid()
    {
        meniscus.gameObject.SetActive(true);liquidSlot.gameObject.SetActive(RemainingMl>0);
        meniscus.position=Vector3.Lerp(zero.position,rangeEnd.position,(float)(ReadingMl/CapacityMl));
        liquidSlot.localScale=new Vector3(1,1,(float)(RemainingMl*1e-6/crossSectionArea/.001));
    }
    private void OnDisable()
    {
        if(initialized)SetValve(0);
        Paused=true;
    }
}
