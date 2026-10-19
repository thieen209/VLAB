using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    // Separately derived insulating fixtures. Never changes a supplied mesh or its scale.
    // Source dimensions: electrodes v001 handoff; beaker/funnel v001 metadata and real colliders.
    public sealed class ScientificFixtureSupport : MonoBehaviour
    {
        readonly List<Renderer> solids=new List<Renderer>();
        Material polymer;Transform vessel,instrument;Renderer glass,body;VlabElectrode electrode;VlabFunnel funnel;
        Renderer bridge,rear,front,join,hanger;readonly List<Renderer> forks=new List<Renderer>(),posts=new List<Renderer>();
        Vector3[] contactPoints,rimPoints;bool filtration;
        public bool Insulating=>polymer&&polymer.GetFloat("_Metallic")==0;
        public static ScientificFixtureSupport Get(Transform parent,string key)
        {
            var found=parent.GetComponentsInChildren<ScientificFixtureSupport>(true).FirstOrDefault(x=>x.name=="Derived insulating fixture · "+key);
            if(found)return found;
            var go=new GameObject("Derived insulating fixture · "+key);go.transform.SetParent(parent,false);return go.AddComponent<ScientificFixtureSupport>();
        }
        Material Polymer
        {
            get
            {
                if(!polymer){polymer=new Material(Shader.Find("Universal Render Pipeline/Lit"));polymer.name="Derived insulating polymer (not a conductive busbar)";polymer.SetColor("_BaseColor",new Color(.36f,.42f,.40f));polymer.SetFloat("_Metallic",0);polymer.SetFloat("_Smoothness",.22f);}
                return polymer;
            }
        }
        Renderer Box(string label,Vector3 center,Vector3 size)
        {
            var r=solids.FirstOrDefault(x=>x.name==label);
            if(!r){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=label;go.transform.SetParent(transform,true);r=go.GetComponent<Renderer>();r.sharedMaterial=Polymer;solids.Add(r);}
            r.transform.SetPositionAndRotation(center,Quaternion.identity);r.transform.localScale=size;return r;
        }
        static Renderer Glass(Transform item)=>item.GetComponentsInChildren<Renderer>(true).Single(r=>r.name.EndsWith("_GlassBody_VIS",StringComparison.Ordinal));
        static bool Near(float a,float b)=>Mathf.Abs(a-b)<.0006f;
        static bool Touch(Bounds a,Bounds b,float tolerance=.0006f)=>a.SqrDistance(b.ClosestPoint(a.center))<=tolerance*tolerance;
        static bool CoversXZ(Bounds b,Vector3 point)=>point.x>=b.min.x-.0003f&&point.x<=b.max.x+.0003f&&point.z>=b.min.z-.0003f&&point.z<=b.max.z+.0003f;
        Vector3 RimContact(float side,float z)
        {
            Physics.SyncTransforms();var shape=glass.GetComponent<MeshCollider>();var b=glass.bounds;
            if(!shape)throw new InvalidOperationException("Original hollow beaker rim collider is missing.");
            for(int n=0;n<=40;n++)
            {
                float x=vessel.position.x+side*b.extents.x*(.85f+n*.15f/40);
                if(shape.Raycast(new Ray(new Vector3(x,b.max.y+.01f,z),Vector3.down),out var hit,.02f)&&hit.point.y>b.max.y-.002f)return hit.point;
            }
            throw new InvalidOperationException("Derived support did not find actual glass rim contact.");
        }
        bool RimStillTouches(Vector3 point,float bottom)
        {
            var shape=glass.GetComponent<MeshCollider>();return shape&&shape.Raycast(new Ray(point+Vector3.up*.01f,Vector3.down),out var hit,.02f)&&Near(hit.point.y,bottom);
        }
        bool NativeFunnelStillTouches(int index)
        {
            var b=forks[index].bounds;var point=new Vector3(index==0?b.max.x:b.min.x,b.max.y,b.center.z);
            var ray=new Ray(point-Vector3.up*.01f,Vector3.up);
            return funnel.GetComponentsInChildren<MeshCollider>().Where(c=>c.name.EndsWith("_Funnel_VIS",StringComparison.Ordinal)).Any(c=>c.Raycast(ray,out var hit,.02f)&&Near(hit.point.y,forks[index].bounds.max.y));
        }
        public void HoldElectrode(VlabBeaker cup,VlabElectrode plate)
        {
            filtration=false;vessel=cup.transform;instrument=plate.transform;electrode=plate;glass=Glass(vessel);
            body=plate.GetComponentsInChildren<Renderer>().Single(r=>r.gameObject.activeInHierarchy&&r.name.EndsWith(plate.ElectrodeID=="graphite"?"_Rod_VIS":"_Plate_VIS",StringComparison.Ordinal));
            var b=body.bounds;float rim=glass.bounds.max.y,radius=.035f,thickness=b.size.z;
            if(thickness>.012f)throw new InvalidOperationException("Electrode fixture requires the original upright plate/rod orientation.");
            float gripY=plate.Contact.position.y-.008f,gripX=b.center.x,gripZ=b.center.z;
            float rearZ=gripZ-thickness*.5f-.0015f,frontZ=gripZ+thickness*.5f+.0015f;
            float bridgeZ=vessel.position.z-.006f;
            rimPoints=new[]{RimContact(-1,bridgeZ),RimContact(1,bridgeZ)};
            rim=(rimPoints[0].y+rimPoints[1].y)*.5f;
            // One shared physical bridge per vessel, including Cu/Cu electrolysis.
            // Each electrode retains its separate insulating hanger/jaws.
            bridge=transform.parent.GetComponentsInChildren<ScientificFixtureSupport>().Where(x=>x!=this&&!x.filtration&&x.vessel==vessel&&x.bridge).Select(x=>x.bridge).FirstOrDefault();
            if(!bridge)bridge=Box("Insulating rim bridge",new Vector3(vessel.position.x,rim+.002f,bridgeZ),new Vector3(radius*2+.006f,.004f,.012f));
            rear=Box("Insulating rear electrode jaw",new Vector3(gripX,gripY,rearZ),new Vector3(b.size.x+.004f,.008f,.003f));
            front=Box("Insulating front electrode jaw",new Vector3(gripX,gripY,frontZ),new Vector3(b.size.x+.004f,.008f,.003f));
            join=Box("Insulating jaw side connection",new Vector3(b.max.x+.002f,gripY,gripZ),new Vector3(.004f,.008f,thickness+.006f));
            float low=gripY-.004f,high=rim;
            if(low<=vessel.position.y+(float)cup.SurfaceHeightMeters+.004f||high<=low)throw new InvalidOperationException("Insulating jaws must grip above liquid below the rim; do not hide a conductive short in solution.");
            hanger=Box("Insulating downward electrode hanger",new Vector3(gripX,(low+high)*.5f,gripZ-thickness*.5f-.005f),new Vector3(.006f,high-low,.004f));
            if(!Validate(out var message))throw new InvalidOperationException(message);
        }
        public void SeatFunnel(VlabBeaker receiver,VlabFunnel value,VlabFilterPaper paper)
        {
            filtration=true;vessel=receiver.transform;instrument=value.transform;funnel=value;glass=Glass(vessel);
            rimPoints=new[]{RimContact(-1,vessel.position.z),RimContact(1,vessel.position.z)};float rim=(rimPoints[0].y+rimPoints[1].y)*.5f;
            // First keep the native outlet well above even a full working extract, then sample
            // the real glass underside at the 10 mm fork opening (8 mm source stem clears it).
            value.transform.position+=new Vector3(vessel.position.x,rim+.030f,vessel.position.z)-value.Outflow.origin;
            Physics.SyncTransforms();contactPoints=new Vector3[2];
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;var ray=new Ray(value.Outflow.origin+new Vector3(sign*.005f,-.005f,0),Vector3.up);
                var hits=value.GetComponentsInChildren<MeshCollider>().Where(c=>c.name.EndsWith("_Funnel_VIS",StringComparison.Ordinal)).Select(c=>{bool hit=c.Raycast(ray,out var result,.2f);return (hit,result);}).Where(x=>x.hit).OrderBy(x=>x.result.distance).ToArray();
                if(hits.Length==0)throw new InvalidOperationException("The original funnel glass underside did not meet its derived support fork.");
                contactPoints[i]=hits[0].result.point;
            }
            float offset=Mathf.Min(contactPoints[0].y,contactPoints[1].y)-(rim+.015f);
            value.transform.position-=Vector3.up*offset;for(int i=0;i<2;i++)contactPoints[i]-=Vector3.up*offset;
            forks.Clear();posts.Clear();
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;float outer=.035f,inner=.005f,top=contactPoints[i].y;
                forks.Add(Box("Insulating funnel fork "+i,new Vector3(vessel.position.x+sign*(outer+inner)*.5f,top-.0015f,vessel.position.z),new Vector3(outer-inner,.003f,.008f)));
                posts.Add(Box("Insulating funnel rim riser "+i,new Vector3(rimPoints[i].x,(rim+top-.003f)*.5f,vessel.position.z),new Vector3(.006f,top-.003f-rim,.008f)));
            }
            if(!value.TrySeatPaper(paper))throw new InvalidOperationException("Native folded paper could not seat in the supported funnel.");
            Physics.SyncTransforms();if(!Validate(out var message))throw new InvalidOperationException(message);
        }
        public bool Validate(out string failure)
        {
            failure="";if(!vessel||!instrument||!glass||!Insulating){failure="Derived fixture has lost its source instrument or insulating material.";return false;}
            if((vessel.lossyScale-Vector3.one).sqrMagnitude>1e-8f||(instrument.lossyScale-Vector3.one).sqrMagnitude>1e-8f){failure="Original instrument scale changed.";return false;}
            if(!filtration)
            {
                var b=body.bounds;float rim=bridge.bounds.min.y;
                if(rimPoints.Any(p=>!RimStillTouches(p,rim)||!CoversXZ(bridge.bounds,p))||!Near(hanger.bounds.max.y,rim)||!Touch(bridge.bounds,hanger.bounds)||!Touch(hanger.bounds,rear.bounds)||!Touch(rear.bounds,join.bounds)||!Touch(front.bounds,join.bounds))failure="Electrode holder lacks continuous rim/bridge/hanger/jaw contact.";
                else if(!Near(rear.bounds.center.x,b.center.x)||!Near(front.bounds.center.x,b.center.x)||rear.bounds.min.x>b.min.x||rear.bounds.max.x<b.max.x||front.bounds.min.x>b.min.x||front.bounds.max.x<b.max.x||!Near(rear.bounds.max.z,b.min.z)||!Near(front.bounds.min.z,b.max.z)||rear.bounds.max.y>b.max.y||rear.bounds.min.y<b.min.y)failure="Insulating jaws no longer touch and span the actual electrode faces.";
                else if(electrode.Contact.position.y<=rear.bounds.max.y+.001f)failure="Electrode electrical terminal is obstructed by the holder.";
            }
            else
            {
                var cup=vessel.GetComponent<VlabBeaker>();var ray=funnel.Outflow;float liquid=vessel.position.y+(float)cup.SurfaceHeightMeters;
                if(Vector3.Dot(ray.direction,Vector3.down)<.999f||Mathf.Abs(ray.origin.x-vessel.position.x)>.001f||Mathf.Abs(ray.origin.z-vessel.position.z)>.001f||ray.origin.y<=liquid+.005f)failure="Funnel outlet must point into its receiver above retained liquid.";
                else if(forks[0].bounds.max.x>ray.origin.x-.0049f||forks[1].bounds.min.x<ray.origin.x+.0049f||!Near(forks[0].bounds.center.z,ray.origin.z)||!Near(forks[1].bounds.center.z,ray.origin.z))failure="The split support no longer clears the original 8 mm stem.";
                else for(int i=0;i<2;i++)if(!RimStillTouches(rimPoints[i],posts[i].bounds.min.y)||!CoversXZ(posts[i].bounds,rimPoints[i])||!Touch(posts[i].bounds,forks[i].bounds)||!NativeFunnelStillTouches(i)){failure="Funnel support must seat on actual rim and touch native glass while clearing the stem.";break;}
            }
            return failure=="";
        }
        public bool ValidateProspectiveVolume(double volumeMl,out string failure)
        {
            if(!Validate(out failure))return false;
            if(!filtration)return true;
            var cup=vessel.GetComponent<VlabBeaker>();
            if(double.IsNaN(volumeMl)||double.IsInfinity(volumeMl)||volumeMl<0||volumeMl>cup.CapacityMl){failure="The receiving beaker lacks capacity for this extract.";return false;}
            double surface=vessel.position.y+cup.InteriorBottom+volumeMl*1e-6/cup.CrossSectionArea;
            if(funnel.Outflow.origin.y<=surface+.005){failure="The retained extract would submerge the funnel outlet.";return false;}
            return true;
        }
        void OnDestroy(){if(polymer)Destroy(polymer);}
    }
}
