using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed class ReleasedEquipment:MonoBehaviour
    {
        Rigidbody body;Collider[] nativeColliders;BoxCollider proxy;GameObject supportObject;
        float bottomOffset,supportHeight;int ticks;
        public bool IsSettling{get;private set;}
        public Vector3 SupportedPosition{get;private set;}
        public bool LearnerPlaced{get;private set;}

        internal bool Begin(Bounds bounds,Bounds support)
        {
            if(IsSettling)StopSettling();
            if(GetComponentsInChildren<Rigidbody>(true).Any(r=>r!=body))return false;
            supportHeight=support.max.y;bottomOffset=bounds.min.y-transform.position.y;
            SupportedPosition=transform.position+Vector3.up*(supportHeight+.002f-bounds.min.y);
            nativeColliders=GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c!=proxy).ToArray();
            foreach(var collider in nativeColliders)collider.enabled=false;
            if(!body)body=gameObject.AddComponent<Rigidbody>();
            if(!proxy)
            {
                var go=new GameObject("Released tool collision envelope");go.layer=gameObject.layer;
                proxy=go.AddComponent<BoxCollider>();go.transform.SetParent(transform,false);
            }
            proxy.transform.SetPositionAndRotation(bounds.center,Quaternion.identity);
            proxy.transform.localScale=Vector3.one;proxy.size=bounds.size;proxy.contactOffset=.0005f;proxy.enabled=true;
            supportObject=new GameObject("Measured resting surface");supportObject.layer=2;
            supportObject.transform.position=new Vector3(bounds.center.x,supportHeight-.01f,bounds.center.z);
            var floor=supportObject.AddComponent<BoxCollider>();floor.size=new Vector3(bounds.size.x+.02f,.02f,bounds.size.z+.02f);floor.contactOffset=.0005f;
            // shortcut: upright vertical resting uses a collision envelope; upgrade for tipping, spills or throwing.
            body.constraints=RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
            body.mass=.1f;body.linearDamping=0;body.angularDamping=0;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic=false;body.useGravity=true;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            ticks=0;LearnerPlaced=IsSettling=true;enabled=true;Physics.SyncTransforms();body.WakeUp();return true;
        }
        void FixedUpdate()
        {
            if(!IsSettling||!body)return;
            ticks++;
            float clearance=transform.position.y+bottomOffset-supportHeight;
            if(ticks>2&&clearance>=-.001f&&clearance<.006f&&Mathf.Abs(body.linearVelocity.y)<.025f)
            {
                var item=GetComponent<EquipmentHandle>();
                if(item&&!item.installed){item.trayPosition=transform.position;item.trayRotation=transform.rotation;}
                SupportedPosition=transform.position;StopSettling();
                VlabAudio.At(VlabSound.Placement,transform.position);
            }
        }
        internal void StopSettling()
        {
            if(!IsSettling)return;IsSettling=false;
            if(body){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.useGravity=false;body.isKinematic=true;}
            if(proxy)proxy.enabled=false;
            if(nativeColliders!=null)foreach(var collider in nativeColliders)if(collider)collider.enabled=true;
            if(supportObject){supportObject.SetActive(false);Destroy(supportObject);supportObject=null;}
            enabled=false;
        }
        void OnDisable(){StopSettling();}
        void OnDestroy(){if(supportObject){supportObject.SetActive(false);Destroy(supportObject);}}
    }
}
