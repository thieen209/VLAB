using System;
using UnityEngine;

public sealed class VlabBeaker : MonoBehaviour
{
    [SerializeField] private Transform liquidSlot;
    [SerializeField] private double capacityMl;
    [SerializeField] private double crossSectionArea;
    [SerializeField] private double interiorBottom;
    public double CapacityMl => capacityMl;
    public double CrossSectionArea => crossSectionArea;
    public double InteriorBottom => interiorBottom;
    public double VolumeMl { get; private set; }
    public double SurfaceHeightMeters => interiorBottom+VolumeMl*1e-6/crossSectionArea;
    public bool LiquidVisible => liquidSlot.gameObject.activeSelf;

    private static void Validate(double amount)
    {
        if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<0) throw new ArgumentOutOfRangeException(nameof(amount));
    }

    private void Awake()
    {
        Validate(capacityMl); Validate(crossSectionArea); Validate(interiorBottom);
        if(liquidSlot==null||capacityMl==0||crossSectionArea==0) throw new InvalidOperationException("Beaker geometry metadata missing.");
        RefreshLiquid();
    }

    public double Add(double requestedMl)
    {
        Validate(requestedMl);
        double accepted=Math.Min(requestedMl,capacityMl-VolumeMl);
        VolumeMl+=accepted; RefreshLiquid(); return accepted;
    }

    public double Remove(double requestedMl)
    {
        Validate(requestedMl);
        double removed=Math.Min(requestedMl,VolumeMl);
        VolumeMl-=removed; RefreshLiquid(); return removed;
    }

    public double TransferTo(VlabBeaker receiver,double requestedMl)
    {
        Validate(requestedMl);
        if(receiver==null) throw new ArgumentNullException(nameof(receiver));
        if(receiver==this) return 0;
        double moved=Math.Min(Math.Min(requestedMl,VolumeMl),receiver.capacityMl-receiver.VolumeMl);
        VolumeMl-=moved; receiver.VolumeMl+=moved;
        RefreshLiquid(); receiver.RefreshLiquid(); return moved;
    }

    private void RefreshLiquid()
    {
        // shortcut: upright volume preview only; tilt, composition and pouring streams belong to later lesson integration.
        liquidSlot.gameObject.SetActive(VolumeMl>0);
        liquidSlot.localScale=new Vector3(1,1,(float)(VolumeMl*1e-6/crossSectionArea/.001));
    }
}
