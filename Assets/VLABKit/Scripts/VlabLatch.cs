using System;
using System.Linq;
using UnityEngine;
public sealed class VlabLatch : MonoBehaviour
{
 [SerializeField] GameObject connector;
 [SerializeField] Quaternion modelRotation;
 public bool Locked { get; private set; }
 public double DissipatedJ { get; private set; }
 public double PairVelocityMps { get; private set; }
 public bool TryLock(VlabCart a,VlabCart b)
 {
  if(Locked||!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f||a==null||b==null||a==b||a.CartId!="A"||b.CartId!="B"||!a.isActiveAndEnabled||!b.isActiveAndEnabled||!a.UnitFrame||!b.UnitFrame||a.TravelFrame!=b.TravelFrame||a.VelocityMps<=b.VelocityMps)return false;
  if(Math.Abs(b.PositionM-a.PositionM-.1576)>1e-5)return false;
  var front=a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="VLAB_cart_BumperFront_SOCKET");var back=b.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="VLAB_cart_BumperBack_SOCKET");
  if(front==null||back==null||Vector3.Distance(back.position,front.position+a.TravelFrame.right*.0176f)>1e-5f)return false;
  double before=a.KineticJ+b.KineticJ;double velocity=(a.MassKg*a.VelocityMps+b.MassKg*b.VelocityMps)/(a.MassKg+b.MassKg);
  a.ResetTrial(a.MassKg,a.PositionM,velocity);b.ResetTrial(b.MassKg,b.PositionM,velocity);
  PairVelocityMps=velocity;DissipatedJ=Math.Max(0,before-a.KineticJ-b.KineticJ);transform.SetPositionAndRotation(front.position,a.TravelFrame.rotation*modelRotation);Locked=true;connector.SetActive(true);return true;
 }
 public void Release(){Locked=false;DissipatedJ=PairVelocityMps=0;if(connector!=null)connector.SetActive(false);}
 void Awake()=>Release();
 void OnDisable()=>Release();
 // shortcut: the impact checkpoint pauses both carts; shared post-impact track motion belongs to the P06 pair controller.
}
