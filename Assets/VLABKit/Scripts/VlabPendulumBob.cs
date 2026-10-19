using System;
using System.Globalization;
using UnityEngine;
[RequireComponent(typeof(VlabMassSample))]
public sealed class VlabPendulumBob : MonoBehaviour
{
 [SerializeField] VlabMassSample sample;
 [SerializeField] Transform eye;
 [SerializeField] TextMesh label;
 public Transform Eye=>eye;
 public Vector3 Center=>transform.position;
 public double MassGrams=>sample.enabled?sample.MassGrams:double.NaN;
 public bool TryConfigureMass(double grams){if(!isActiveAndEnabled||!double.IsFinite(grams)||grams<=0||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;sample.SetMass(grams);sample.enabled=true;label.text=grams.ToString(grams<1000?"0.###":"0.###E+0",CultureInfo.InvariantCulture)+" g";label.characterSize=.0007f;var size=label.GetComponent<Renderer>().localBounds.size;if(size.x>.008f||size.y>.0035f)label.characterSize*=Mathf.Min(.0078f/size.x,.0033f/size.y);return true;}
 // shortcut: explicit bob mass and eye-loop reference only; cord knot, pivot, center-distance and oscillation dynamics belong to P01 assembly.
}
