using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class ApparatusRig
    {
        static float SurfaceInset(StorageSurface surface)=>surface.trayBay ? .001f : StorageInset;
        static bool LearnerPlaced(EquipmentHandle item)=>item.GetComponent<ReleasedEquipment>()?.LearnerPlaced==true;
        float StorageValidationMargin(Bounds bounds)=>storageSurfaces.Any(surface=>surface.trayBay&&surface.tray&&surface.tray.CanSupport(bounds)) ? .0015f : StorageGap*.25f;
        List<Rect> ClearSurfaceAreas(StorageSurface surface,GameObject instrument,float height)
        {
            var free=new List<Rect>{Footprint(surface.bounds,-SurfaceInset(surface))};
            var visibility=GetComponent<BenchDisplayVisibility>();
            if(!surface.trayBay&&visibility)foreach(var tray in visibility.ContrastTrays)if(tray&&tray.gameObject.activeInHierarchy)free=Subtract(free,Footprint(tray.OuterBounds,.002f));
            foreach(var other in items.Where(item=>item.installed&&item.gameObject.activeInHierarchy&&!item.transform.IsChildOf(instrument.transform)&&!instrument.transform.IsChildOf(item.transform)))
                if(TryVisibleBounds(other,out var occupied)&&occupied.max.y>surface.bounds.max.y&&occupied.min.y<surface.bounds.max.y+StorageLift+height)
                    free=Subtract(free,Footprint(occupied,surface.trayBay ? .003f : StorageGap*.5f));
            return free;
        }
        bool NeedsContrast(EquipmentHandle item)
        {
            string source=Driver?.Kit.FirstOrDefault(spec=>spec.key==item.assetId)?.prefab??"";
            return new[]{"beaker","beaker_100ml","flask","volumetric_flask","volumetric_flask_50ml","slide","coverslip","pipette","dropper","capillary","stir_rod","funnel","jar","cylinder","gas_syringe","salt_bridge"}.Contains(source);
        }
        void ConfigureContrastTrays()
        {
            if(!BenchDisplayVisibility.SupportsContrastTray(session.spec.id)||UsesSyntheticStorageSurface)return;
            var visibility=GetComponent<BenchDisplayVisibility>();if(!visibility)visibility=gameObject.AddComponent<BenchDisplayVisibility>();
            var bounds=new Dictionary<EquipmentHandle,Bounds>();foreach(var item in items)if(TryVisibleBounds(item,out var b))bounds.Add(item,b);
            // A bare pipette can fit a bay before its bulb is attached. Reserve the final
            // assembly for the bench; its installed rest is recomputed from both meshes.
            // C01 moves its flask beside the stand after aliquoting. A tray at its staging
            // slot would obstruct that later fit; the remaining glassware still uses trays.
            var candidates=items.Where(item=>NeedsContrast(item)&&bounds.ContainsKey(item)&&!(session.spec.id=="C01"&&item.assetId=="flask")&&Driver.Kit.Any(spec=>spec.key==item.assetId&&spec.onSurface&&string.IsNullOrEmpty(spec.dependsOn))&&!Driver.Kit.Any(spec=>spec.dependsOn==item.assetId&&spec.prefab=="pipette_bulb")).OrderByDescending(item=>bounds[item].size.x*bounds[item].size.z).ToArray();
            var seated=new HashSet<EquipmentHandle>();var benches=storageSurfaces.ToArray();
            foreach(var first in candidates)
            {
                if(seated.Contains(first))continue;
                var partners=candidates.Where(other=>other!=first&&!seated.Contains(other)&&Mathf.Abs(bounds[other].min.y-bounds[first].min.y)<.001f).Cast<EquipmentHandle>().Concat(new EquipmentHandle[]{null});
                foreach(var second in partners)
                {
                    var target=bounds[first];if(second)target.Encapsulate(bounds[second]);
                    var bench=benches.FirstOrDefault(surface=>ContainsRect(Footprint(surface.bounds),Footprint(target))&&Mathf.Abs(target.min.y-surface.bounds.max.y-StorageLift)<.001f);if(bench==null)continue;
                    var blockers=bounds.Where(pair=>pair.Key!=first&&pair.Key!=second).Select(pair=>pair.Value).Concat(visibility.ContrastTrays.Where(existing=>existing).Select(existing=>existing.OuterBounds)).ToArray();
                    if(!visibility.TryAddContrastTray(session.spec.id,bench.bounds,target,blockers,out var tray))continue;
                    foreach(var item in new[]{first,second}.Where(item=>item))
                    {
                        var offset=Vector3.up*(tray.Bay(tray.TargetBay).max.y+StorageLift-bounds[item].min.y);item.transform.position+=offset;item.slot=item.transform.position;
                        var raised=bounds[item];raised.center+=offset;bounds[item]=raised;seated.Add(item);
                    }
                    for(int bay=0;bay<2;bay++)storageSurfaces.Add(new StorageSurface{bounds=tray.Bay(bay),name="Contrast tray bay "+bay,tray=tray,trayBay=true});
                    break;
                }
            }
        }
        void ReserveContrastTrays(List<Rect>[] free)
        {
            var visibility=GetComponent<BenchDisplayVisibility>();if(!visibility)return;
            for(int i=0;i<free.Length;i++)if(!storageSurfaces[i].trayBay)foreach(var tray in visibility.ContrastTrays)if(tray&&tray.gameObject.activeInHierarchy)free[i]=Subtract(free[i],Footprint(tray.OuterBounds,.002f));
        }
        bool CanUseStorageSurface(StorageSurface surface,Bounds placed)
        {
            if(surface.trayBay&&(!surface.tray||!surface.tray.gameObject.activeInHierarchy||!surface.tray.CanSupport(placed)))return false;
            var visibility=GetComponent<BenchDisplayVisibility>();
            return !visibility||visibility.ContrastTrays.All(tray=>!tray||!tray.gameObject.activeInHierarchy||!tray.BlocksPlacement(placed));
        }
        bool ExpectedVesselInsertion(EquipmentHandle item,EquipmentHandle other)
        {
            if(session?.spec.id!="C01"||item.assetId!="flask"||other.assetId!="burette"||!other.installed)return false;
            var flask=item.GetComponentInChildren<VlabFlask>();var outlet=other.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.EndsWith("_Outlet_SOCKET",StringComparison.Ordinal));
            return flask&&outlet&&flask.ContainsPoint(outlet.position);
        }
        internal bool TryGetRestingSupport(EquipmentHandle item,Bounds poseBounds,out Bounds support,out string reason)
        {
            support=default;reason="Place the complete item over a clear bench or tray bay.";
            if(!item||!float.IsFinite(poseBounds.center.x)||!float.IsFinite(poseBounds.center.y)||!float.IsFinite(poseBounds.center.z))return false;
            foreach(var surface in storageSurfaces.OrderBy(s=>Mathf.Abs(s.bounds.max.y-poseBounds.min.y)))
            {
                if(!ContainsRect(Footprint(surface.bounds,-(surface.trayBay ? .001f : .005f)),Footprint(poseBounds))||poseBounds.min.y<surface.bounds.max.y-.08f||poseBounds.min.y>surface.bounds.max.y+.9f)continue;
                var placed=poseBounds;placed.center+=Vector3.up*(surface.bounds.max.y+StorageLift-poseBounds.min.y);
                if(!CanUseStorageSurface(surface,placed)){reason="The instrument overlaps a tray wall, divider or floor.";continue;}
                var collision=placed;collision.Expand(-.002f);
                var obstruction=items.FirstOrDefault(other=>other&&other!=item&&other.gameObject.activeInHierarchy&&!other.transform.IsChildOf(item.transform)&&!item.transform.IsChildOf(other.transform)&&!ExpectedVesselInsertion(item,other)&&TryVisibleBounds(other,out var occupied)&&collision.Intersects(occupied));
                if(obstruction){reason="The instrument overlaps "+EquipmentLabel(obstruction,false)+"; choose clear supported space.";continue;}
                support=surface.bounds;reason="";return true;
            }
            return false;
        }
        public bool TryRestInstalledTool(EquipmentHandle item,out string reason)
        {
            reason="The complete instrument needs a clear support surface.";
            if(!item||!TryVisibleBounds(item,out var bounds)||!TryGetRestingSupport(item,bounds,out var support,out reason))return false;
            item.transform.position+=Vector3.up*(support.max.y+StorageLift-bounds.min.y);item.slot=item.transform.position;Physics.SyncTransforms();return true;
        }
    }
}
