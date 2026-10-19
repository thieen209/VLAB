using System;
using UnityEngine;

public sealed class VlabCaliper : MonoBehaviour
{
    [SerializeField] private Transform slider, fixedFace, movingFace, travelEnd, depthBase, depthTip, vernierZero;
    [SerializeField] private Vector3 closedLocalPosition, localTravelAxis;
    public bool Locked { get; set; }
    public Transform FixedFace => fixedFace;
    public Transform MovingFace => movingFace;
    public Transform VernierZero => vernierZero;
    public Vector3 Axis => (travelEnd.position-fixedFace.position).normalized;
    public double OpeningMm => Vector3.Dot(movingFace.position-fixedFace.position,Axis)*1000d;
    public double DepthExtensionMm => Vector3.Dot(depthTip.position-depthBase.position,Axis)*1000d;
    public double ReadingMm => Valid ? Math.Round(OpeningMm/.02,MidpointRounding.AwayFromZero)*.02 : double.NaN;
    public int MainMillimeters => Valid ? (int)Math.Floor(ReadingMm+1e-7) : -1;
    public int VernierIndex => Valid ? (int)Math.Round((ReadingMm-MainMillimeters)/.02) : -1;
    public bool Valid
    {
        get
        {
            if(!isActiveAndEnabled||slider==null||fixedFace==null||movingFace==null||travelEnd==null||depthBase==null||depthTip==null||vernierZero==null)return false;
            if((slider.lossyScale-Vector3.one).sqrMagnitude>1e-8f||(fixedFace.lossyScale-Vector3.one).sqrMagnitude>1e-8f)return false;
            if(Math.Abs(Vector3.Distance(travelEnd.position,fixedFace.position)-.15f)>.00001)return false;
            var delta=movingFace.position-fixedFace.position;double opening=OpeningMm;
            return !double.IsNaN(opening)&&!double.IsInfinity(opening)&&opening>=-.0001&&opening<=150.0001&&Vector3.Cross(delta,Axis).magnitude<.00001f;
        }
    }
    public bool SetOpening(double millimeters)
    {
        if(double.IsNaN(millimeters)||double.IsInfinity(millimeters)||millimeters<0||millimeters>150)throw new ArgumentOutOfRangeException(nameof(millimeters));
        if(Locked||!Valid)return false;
        slider.localPosition=closedLocalPosition+localTravelAxis*(float)(millimeters*.001);
        return true;
    }
    public bool TryMeasureOutside(Collider sample)
    {
        if(Locked||!Valid||sample==null||!sample.enabled||!sample.gameObject.activeInHierarchy||sample.isTrigger||sample.transform.IsChildOf(transform))return false;
        Physics.SyncTransforms();Vector3 axis=Axis,reference=fixedFace.position;
        // shortcut: one contact line, appropriate for aligned samples; add surface fitting for arbitrary placement.
        if(!sample.Raycast(new Ray(reference-axis*.001f,axis),out var left,.152f)||
           !sample.Raycast(new Ray(reference+axis*.151f,-axis),out var right,.152f))return false;
        if(Vector3.Distance(left.point,reference)>.00001f)return false;
        double distance=Vector3.Dot(right.point-reference,axis)*1000d;
        if(distance<=0||distance>150)return false;
        return SetOpening(distance);
    }
}
