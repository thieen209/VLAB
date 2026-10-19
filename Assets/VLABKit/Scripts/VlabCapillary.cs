using System;
using UnityEngine;

public sealed class VlabCapillary : MonoBehaviour
{
    [SerializeField] private Transform spot,back,liquidSlot;
    [SerializeField] private double liquidArea;
    public const double SampleLimitUl=2;
    public double VolumeUl {get;private set;}
    public Transform Spot=>spot;
    public Vector3 Axis=>(back.position-spot.position).normalized;
    public bool UnitFrame=>Vector3.Distance(transform.lossyScale,Vector3.one)<1e-5f&&Math.Abs(Vector3.Distance(spot.position,back.position)-.07)<1e-6;
    public bool LiquidVisible=>liquidSlot.gameObject.activeSelf;
    internal static void Amount(double v){if(double.IsNaN(v)||double.IsInfinity(v)||v<0)throw new ArgumentOutOfRangeException(nameof(v));}
    void Awake(){if(spot==null||back==null||liquidSlot==null||double.IsNaN(liquidArea)||double.IsInfinity(liquidArea)||liquidArea<=0)throw new InvalidOperationException("Capillary geometry missing.");Refresh();}
    public double LoadFrom(VlabBeaker source,double requestedUl)
    {
        Amount(requestedUl);if(source==null)throw new ArgumentNullException(nameof(source));
        if(!isActiveAndEnabled||!UnitFrame||!source.isActiveAndEnabled)return 0;
        // shortcut: explicitly supplied single-sample reservoir; validate immersion/composition in the full B06 workflow.
        double moved=source.Remove(Math.Min(requestedUl,SampleLimitUl-VolumeUl)/1000)*1000;
        VolumeUl+=moved;Refresh();return moved;
    }
    public bool TrySpot(VlabCapillarySpot target,double requestedUl)
    {
        Amount(requestedUl);if(target==null)throw new ArgumentNullException(nameof(target));
        if(!isActiveAndEnabled||!UnitFrame||requestedUl<=0||requestedUl>VolumeUl+1e-12||!target.CanAccept(this,requestedUl))return false;
        double moved=Math.Min(requestedUl,VolumeUl);target.Receive(moved);VolumeUl=Math.Max(0,VolumeUl-moved);Refresh();return true;
    }
    public void ClearTrial(){VolumeUl=0;Refresh();}
    void Refresh(){liquidSlot.gameObject.SetActive(VolumeUl>1e-12);liquidSlot.localScale=new Vector3(1,1,(float)(VolumeUl*1e-9/liquidArea/.001));}
}
