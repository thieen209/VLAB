using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VLAB.Core.Input
{
    public static class VLabPointerUi
    {
        // Project the actual controller ray onto each canvas, not an arbitrary screen depth.
        public static RaycastResult Raycast(Ray ray, PointerEventData pointer, List<RaycastResult> buffer)
        {
            RaycastResult best = default;
            foreach (var candidate in RaycasterManager.GetRaycasters())
            {
                if (!(candidate is GraphicRaycaster graphics) || !graphics.isActiveAndEnabled) continue;
                var canvas=graphics.GetComponent<Canvas>();
                if(canvas==null || !canvas.isActiveAndEnabled || canvas.renderMode!=RenderMode.WorldSpace || graphics.eventCamera==null)continue;
                var plane=new Plane(canvas.transform.forward,canvas.transform.position);
                if(!plane.Raycast(ray,out var distance) || distance<=0)continue;
                var point=ray.GetPoint(distance);
                pointer.position=graphics.eventCamera.WorldToScreenPoint(point);
                buffer.Clear();graphics.Raycast(pointer,buffer);
                if(buffer.Count==0)continue;
                var hit=buffer[0];hit.distance=distance;hit.worldPosition=point;hit.screenPosition=pointer.position;
                if(best.gameObject==null || hit.sortingOrder>best.sortingOrder || (hit.sortingOrder==best.sortingOrder && distance<best.distance))best=hit;
            }
            buffer.Clear();
            if(best.gameObject!=null)pointer.position=best.screenPosition;
            return best;
        }
    }
}
