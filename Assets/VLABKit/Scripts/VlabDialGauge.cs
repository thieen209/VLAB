using System;
using UnityEngine;
public sealed class VlabDialGauge : MonoBehaviour
{
 [SerializeField] Transform needle,counter,plunger,contact;
 [SerializeField] Vector3 spindleAxis,spinAxis,plungerBase,contactBase;
 [SerializeField] Quaternion needleRest,counterRest;
 public double CompressionMm{get;private set;}
 public double ReadingMm=>Math.Round(CompressionMm,2,MidpointRounding.AwayFromZero);
 public Transform Contact=>contact;
 public bool SetCompression(double mm){if(!isActiveAndEnabled||!double.IsFinite(mm)||mm<0||mm>10||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;plunger.localPosition=plungerBase+spindleAxis*(float)(mm*.001);contact.localPosition=contactBase+spindleAxis*(float)(mm*.001);needle.localRotation=Quaternion.AngleAxis((float)((mm%1)*360),spinAxis)*needleRest;counter.localRotation=Quaternion.AngleAxis((float)(mm*30),spinAxis)*counterRest;CompressionMm=mm;return true;}
 void Start()=>SetCompression(0);
 // shortcut: ideal axial displacement; mounting/contact forces and arbitrary preload zeroing belong to the assembled M08 experiment.
}
