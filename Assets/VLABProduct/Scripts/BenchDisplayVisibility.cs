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
        public void Restore(){foreach(var item in hidden)if(item)item.SetActive(true);hidden.Clear();}
        void OnDisable()=>Restore();
        void OnDestroy()=>Restore();
    }
}
