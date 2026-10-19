using System;
using UnityEngine;
public sealed class VlabRuler : MonoBehaviour
{
 [SerializeField] Transform zero,end,back;
 static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
 public bool TryReadMillimetres(Vector3 point,out int millimetres){millimetres=0;if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!Finite(point)||!Finite(zero.position)||!Finite(end.position)||!Finite(back.position))return false;var axis=end.position-zero.position;var width=back.position-zero.position;if(Mathf.Abs(axis.magnitude-1)>1e-6f||Mathf.Abs(width.magnitude-.030f)>1e-6f||Mathf.Abs(Vector3.Dot(axis.normalized,width.normalized))>1e-4f)return false;axis.Normalize();width.Normalize();var delta=point-zero.position;float x=Vector3.Dot(delta,axis),y=Vector3.Dot(delta,width),plane=Vector3.Dot(delta,Vector3.Cross(axis,width));if(x< -1e-6f||x>1.000001f||y< -1e-6f||y>.030001f||Mathf.Abs(plane)>.001f)return false;millimetres=(int)Math.Round(Mathf.Clamp01(x)*1000.0,MidpointRounding.AwayFromZero);return true;}
 public bool TryDistanceMetres(Vector3 a,Vector3 b,out double metres){metres=0;if(!TryReadMillimetres(a,out int first)||!TryReadMillimetres(b,out int last))return false;metres=Math.Abs(last-first)*.001;return true;}
 // shortcut:1mm point projection on the actual ruler plane, not arbitrary3D object size or parallax/camera correction.
}
