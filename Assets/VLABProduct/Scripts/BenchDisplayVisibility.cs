using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    // Temporarily clear only identified decorative props; authored rooms and equipment stay intact.
    public sealed class BenchDisplayVisibility : MonoBehaviour
    {
        readonly List<GameObject> hidden=new List<GameObject>();
        Renderer worktopRenderer;Material[] originalSurface,ownedSurface;
        public static bool NeedsLightSurface(string lesson)=>lesson=="C01"||lesson=="C02"||lesson=="C03"||lesson=="C04"||lesson=="C05"||lesson=="C06"||lesson=="B06";
        public void SetSurface(string lesson,Vector3 worktop)
        {
            RestoreSurface();if(!NeedsLightSurface(lesson))return;
            worktopRenderer=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.GetComponentInParent<EquipmentHandle>()&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&Mathf.Abs(r.bounds.max.y-worktop.y)<.025f&&
                (r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)&&worktop.x>=r.bounds.min.x&&worktop.x<=r.bounds.max.x&&worktop.z>=r.bounds.min.z&&worktop.z<=r.bounds.max.z).OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).FirstOrDefault();
            if(!worktopRenderer)return;
            originalSurface=worktopRenderer.sharedMaterials;ownedSurface=originalSurface.Select(source=>new Material(source)).ToArray();
            // Off-white matte preserves graduations and indicator colour without a bright specular glare.
            foreach(var material in ownedSurface){material.color=new Color(.78f,.79f,.77f);if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.12f);}
            worktopRenderer.sharedMaterials=ownedSurface;
        }
        void RestoreSurface(){if(worktopRenderer&&originalSurface!=null)worktopRenderer.sharedMaterials=originalSurface;if(ownedSurface!=null)foreach(var material in ownedSurface)if(material)Destroy(material);worktopRenderer=null;originalSurface=ownedSurface=null;}
        internal static bool IsDisplayProp(string name)
        {
            if(name=="Balance-body"||name=="Balance-pan")return true;
            foreach(var stem in new[]{"Ready-tray","Tray-rim"})
                if(name==stem||(name.Length>stem.Length+1&&name.StartsWith(stem+".",StringComparison.Ordinal)&&name.Substring(stem.Length+1).All(char.IsDigit)))return true;
            return false;
        }
        public int ClearBench(Vector3 worktop)
        {
            Restore();
            var renderers=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var top=renderers.Where(r=>r.enabled&&!r.GetComponentInParent<EquipmentHandle>()&&
                r.bounds.size.x>.65f&&r.bounds.size.z>.3f&&Mathf.Abs(r.bounds.max.y-worktop.y)<.025f&&
                (r.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||r.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)&&
                worktop.x>=r.bounds.min.x&&worktop.x<=r.bounds.max.x&&worktop.z>=r.bounds.min.z&&worktop.z<=r.bounds.max.z)
                .OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).FirstOrDefault();
            if(!top)return 0;
            var bench=top.bounds;
            foreach(var renderer in renderers)
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.GetComponentInParent<EquipmentHandle>()||!IsDisplayProp(renderer.name))continue;
                var b=renderer.bounds;
                if(b.min.x<bench.min.x-.05f||b.max.x>bench.max.x+.05f||b.min.z<bench.min.z-.05f||b.max.z>bench.max.z+.05f||
                    b.min.y<bench.max.y-.04f||b.max.y>bench.max.y+.5f)continue;
                hidden.Add(renderer.gameObject);renderer.gameObject.SetActive(false);
            }
            return hidden.Count;
        }
        public void Restore(){RestoreSurface();foreach(var item in hidden)if(item)item.SetActive(true);hidden.Clear();}
        void OnDisable()=>Restore();
        void OnDestroy()=>Restore();
    }
}
