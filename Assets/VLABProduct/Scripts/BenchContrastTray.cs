using System;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    // Support geometry follows the supplied tray's floor sockets and two 270 x 90 mm safe bays.
    public sealed class BenchContrastTray:MonoBehaviour
    {
        public const float RestClearance=.002f;
        readonly Bounds[] bays=new Bounds[2];
        Material ownedMaterial;
        public Bounds OuterBounds{get;private set;}
        public int TargetBay{get;private set;}
        public Bounds Bay(int index)=>bays[index];
        static bool Contains(Bounds outer,Bounds inner,float tolerance=0)=>inner.min.x>=outer.min.x-tolerance&&inner.max.x<=outer.max.x+tolerance&&inner.min.z>=outer.min.z-tolerance&&inner.max.z<=outer.max.z+tolerance;
        static bool Overlap(Bounds a,Bounds b)=>a.min.x<b.max.x&&a.max.x>b.min.x&&a.min.z<b.max.z&&a.max.z>b.min.z;
        public bool CanSupport(Bounds item)
        {
            return bays.Any(bay=>Contains(bay,item,.0001f)&&item.min.y>=bay.max.y-.0005f&&item.min.y<=bay.max.y+RestClearance+.001f);
        }
        public bool BlocksPlacement(Bounds item)
        {
            if(!Overlap(OuterBounds,item)||item.min.y>=OuterBounds.max.y+.0005f||item.max.y<=OuterBounds.min.y)return false;
            return !CanSupport(item);
        }
        internal static bool TryPlace(Transform owner,Bounds bench,Bounds target,Bounds[] blockers,bool smoke,out BenchContrastTray result)
        {
            result=null;
            if(!Finite(bench)||!Finite(target)||bench.size.x<=0||bench.size.z<=0||target.size.x<=0||target.size.z<=0||blockers==null||blockers.Any(b=>!Finite(b)))return false;
            var prefab=Resources.Load<GameObject>("Equipment/VLAB_tray");if(!prefab)return false;
            for(int turn=0;turn<2;turn++)for(int bayIndex=0;bayIndex<2;bayIndex++)
            {
                var size=turn==0?new Vector3(.270f,0,.090f):new Vector3(.090f,0,.270f);
                if(target.size.x>size.x-.004f||target.size.z>size.z-.004f)continue;
                var go=Instantiate(prefab,owner);go.name="Optical contrast tray";
                go.transform.rotation=Quaternion.AngleAxis(turn*90,Vector3.up)*prefab.transform.rotation;
                var tray=go.AddComponent<BenchContrastTray>();tray.TargetBay=bayIndex;
                var sockets=go.GetComponentsInChildren<Transform>();
                var a=sockets.Single(t=>t.name.EndsWith("_ToolA_SOCKET",StringComparison.Ordinal));var b=sockets.Single(t=>t.name.EndsWith("_ToolB_SOCKET",StringComparison.Ordinal));
                var socket=bayIndex==0?a:b;
                var body=go.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name.EndsWith("_Tray_VIS",StringComparison.Ordinal));
                go.transform.position+=new Vector3(target.center.x-socket.position.x,bench.max.y-body.bounds.min.y,target.center.z-socket.position.z);
                tray.OuterBounds=body.bounds;
                foreach(var renderer in go.GetComponentsInChildren<Renderer>())tray.OuterBounds.Encapsulate(renderer.bounds);
                tray.bays[0]=new Bounds(a.position,size);tray.bays[1]=new Bounds(b.position,size);
                var seated=target;seated.center+=Vector3.up*(tray.bays[bayIndex].max.y+RestClearance-target.min.y);
                bool fits=Contains(bench,tray.OuterBounds)&&Mathf.Abs(tray.OuterBounds.min.y-bench.max.y)<.0005f&&tray.CanSupport(seated)&&
                    !blockers.Any(obstacle=>Overlap(tray.OuterBounds,obstacle)&&obstacle.min.y<tray.OuterBounds.max.y+.002f&&obstacle.max.y>tray.OuterBounds.min.y);
                if(!fits){Remove(go);continue;}
                tray.ownedMaterial=new Material(body.sharedMaterial){name=smoke?"Optical tray smoked blue":"Optical tray matte ivory"};
                var tone=smoke?new Color(.055f,.15f,.24f):new Color(.78f,.79f,.75f);
                // URP uses _BaseColor; keep the legacy property consistent on cloned materials.
                if(tray.ownedMaterial.HasProperty("_BaseColor"))tray.ownedMaterial.SetColor("_BaseColor",tone);
                if(tray.ownedMaterial.HasProperty("_Color"))tray.ownedMaterial.SetColor("_Color",tone);
                if(tray.ownedMaterial.HasProperty("_Metallic"))tray.ownedMaterial.SetFloat("_Metallic",0);
                if(tray.ownedMaterial.HasProperty("_Smoothness"))tray.ownedMaterial.SetFloat("_Smoothness",.12f);
                body.sharedMaterial=tray.ownedMaterial;
                foreach(var child in sockets)child.gameObject.layer=2;
                foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=true;
                result=tray;return true;
            }
            return false;
        }
        static bool Finite(Bounds value)=>float.IsFinite(value.center.x)&&float.IsFinite(value.center.y)&&float.IsFinite(value.center.z)&&float.IsFinite(value.size.x)&&float.IsFinite(value.size.y)&&float.IsFinite(value.size.z)&&value.size.x>=0&&value.size.y>=0&&value.size.z>=0;
        internal static void Remove(GameObject value)
        {
            if(!value)return;value.SetActive(false);
            if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
        }
        void OnDestroy(){if(ownedMaterial){if(Application.isPlaying)Destroy(ownedMaterial);else DestroyImmediate(ownedMaterial);}}
    }
}
