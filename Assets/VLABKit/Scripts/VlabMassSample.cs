using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class VlabMassSample : MonoBehaviour
{
    internal static readonly HashSet<VlabMassSample> Active = new HashSet<VlabMassSample>();
    [SerializeField] private double massGrams;
    [SerializeField] private Collider shape;
    public double MassGrams => massGrams;
    public Collider Shape => shape;
    public bool IsHeld { get; set; }
    public bool TryBindShape(Collider actualShape)
    {
        if(!actualShape||!actualShape.enabled||actualShape.isTrigger||!actualShape.transform.IsChildOf(transform))return false;
        shape=actualShape;return true;
    }

    public void SetMass(double grams)
    {
        if(double.IsNaN(grams)||double.IsInfinity(grams)||grams<0) throw new ArgumentOutOfRangeException(nameof(grams));
        massGrams=grams;
    }

    private void OnEnable() { SetMass(massGrams); Active.Add(this); }
    private void OnDisable() => Active.Remove(this);
}
