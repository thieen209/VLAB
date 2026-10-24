using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Non-interactive spotlight follows the actual target after responsive layout.
    public sealed class TourSpotlight:MaskableGraphic
    {
        RectTransform target;readonly Vector3[] corners=new Vector3[4];Rect hole;Color rim;
        public static TourSpotlight Attach(RectTransform page,RectTransform target,Color accent)
        {
            var go=new GameObject("Tour spotlight",typeof(RectTransform),typeof(CanvasRenderer));var rect=(RectTransform)go.transform;rect.SetParent(page,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var focus=go.AddComponent<TourSpotlight>();focus.target=target;focus.rim=Color.Lerp(accent,Color.white,.5f);focus.raycastTarget=false;return focus;
        }
        void LateUpdate()
        {
            if(!target){canvasRenderer.Clear();return;}
            target.GetWorldCorners(corners);var min=new Vector2(float.MaxValue,float.MaxValue);var max=-min;
            foreach(var corner in corners){Vector2 p=rectTransform.InverseTransformPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            var next=Rect.MinMaxRect(min.x-10,min.y-10,max.x+10,max.y+10);if(next!=hole){hole=next;SetVerticesDirty();}
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(!target||hole.width<=0)return;var root=(RectTransform)canvas.transform;root.GetWorldCorners(corners);Vector2 lo=rectTransform.InverseTransformPoint(corners[0]),hi=rectTransform.InverseTransformPoint(corners[2]);var all=Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);Color shade=new Color(.025f,.04f,.065f,.58f);
            Quad(mesh,new Rect(all.xMin,all.yMin,all.width,Mathf.Max(0,hole.yMin-all.yMin)),shade);
            Quad(mesh,new Rect(all.xMin,hole.yMax,all.width,Mathf.Max(0,all.yMax-hole.yMax)),shade);
            Quad(mesh,new Rect(all.xMin,hole.yMin,Mathf.Max(0,hole.xMin-all.xMin),hole.height),shade);
            Quad(mesh,new Rect(hole.xMax,hole.yMin,Mathf.Max(0,all.xMax-hole.xMax),hole.height),shade);
            const int segments=40;float radius=Mathf.Min(24,hole.height*.3f);
            for(int n=0;n<segments;n++)
            {
                Vector2 a=Edge(n,segments,radius),b=Edge(n+1,segments,radius);Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*3;
                int start=mesh.currentVertCount;mesh.AddVert(a,rim,Vector2.zero);mesh.AddVert(b,rim,Vector2.zero);mesh.AddVert(b+normal,rim,Vector2.zero);mesh.AddVert(a+normal,rim,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
        Vector2 Edge(int n,int segments,float radius)
        {
            float angle=2*Mathf.PI*n/segments;Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            return hole.center+new Vector2(Mathf.Sign(dir.x)*(hole.width*.5f-radius),Mathf.Sign(dir.y)*(hole.height*.5f-radius))+dir*radius;
        }
        static void Quad(VertexHelper mesh,Rect r,Color c)
        {
            if(r.width<=0||r.height<=0)return;int start=mesh.currentVertCount;
            mesh.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero);mesh.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
