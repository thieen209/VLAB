using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Native vector geometry, deliberately symbolic rather than a rendered apparatus.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SciencePortalGraphic:MaskableGraphic
    {
        public int Subject{get;private set;}public bool IsFrame{get;private set;}public bool IsPlate{get;private set;}
        public int DrawnVertexCount{get;private set;}public Rect DrawnBounds{get;private set;}
        Outline focus;bool focused;Color restingFill,focusedFill;VertexHelper mesh;
        public void Configure(int subject,Color accent,bool frame=false,Outline focusEdge=null)
        {Subject=subject;IsFrame=frame;focus=focusEdge;accent.a=1;color=accent;raycastTarget=false;SetVerticesDirty();}
        public void ConfigurePlate(int subject,Color ink,Outline focusEdge,Color accent){Subject=subject;IsPlate=true;focus=focusEdge;ink.a=accent.a=UiTheme.Opacity;restingFill=ink;focusedFill=Color.Lerp(ink,accent,.18f);color=ink;raycastTarget=false;SetVerticesDirty();}
        void LateUpdate()
        {
            if((!IsFrame&&!IsPlate)||!focus)return;bool next=focus.effectDistance.sqrMagnitude>8;
            if(IsPlate){var target=next?focusedFill:restingFill;target.a=UiTheme.Opacity;var value=Color.Lerp(color,target,1-Mathf.Exp(-Time.unscaledDeltaTime/.1f));value.a=UiTheme.Opacity;if(Mathf.Abs(value.r-color.r)+Mathf.Abs(value.g-color.g)+Mathf.Abs(value.b-color.b)+Mathf.Abs(value.a-color.a)>.0001f)color=value;return;}
            if(next!=focused){focused=next;SetVerticesDirty();}
        }
        Vector2 P(Vector2 point)
        {var r=rectTransform.rect;return new Vector2(r.xMin+point.x*r.width/268,r.yMax-point.y*r.height/432);}
        void Triangle(Vector2 a,Vector2 b,Vector2 c,Color ink)
        {Triangle(a,b,c,ink,ink,ink);}
        void Triangle(Vector2 a,Vector2 b,Vector2 c,Color aColor,Color bColor,Color cColor)
        {int n=mesh.currentVertCount;mesh.AddVert(P(a),aColor,Vector2.zero);mesh.AddVert(P(b),bColor,Vector2.zero);mesh.AddVert(P(c),cColor,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);}
        void Path(IList<Vector2> points,float width=3,bool closed=false)
        {
            if(points.Count<2)return;int count=points.Count;var sides=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                Vector2 before=points[i]-points[i==0?(closed?count-1:0):i-1],after=points[i==count-1?(closed?0:i):i+1]-points[i];
                if(before.sqrMagnitude<.001f)before=after;if(after.sqrMagnitude<.001f)after=before;before.Normalize();after.Normalize();
                Vector2 normal=new Vector2(-after.y,after.x),join=new Vector2(-(before+after).y,(before+after).x).normalized;
                float denominator=Vector2.Dot(join,normal);sides[i]=join*(width*.5f/Mathf.Max(.34f,denominator));
            }
            int segments=closed?count:count-1;
            var edge=color;edge.a=0;
            for(int i=0;i<segments;i++)
            {
                int j=(i+1)%count;Vector2 a=points[i]+sides[i],b=points[i]-sides[i],c=points[j]-sides[j],d=points[j]+sides[j];Triangle(a,b,c,color);Triangle(a,c,d,color);
                // One logical pixel of edge coverage; the entire primary face stays opaque.
                Vector2 outerI=sides[i]*(1+2/width),outerJ=sides[j]*(1+2/width),aa=points[i]+outerI,dd=points[j]+outerJ,bb=points[i]-outerI,cc=points[j]-outerJ;
                Triangle(a,d,dd,color,color,edge);Triangle(a,dd,aa,color,edge,edge);
                Triangle(b,bb,cc,color,edge,edge);Triangle(b,cc,c,color,edge,color);
            }
        }
        void Line(Vector2 a,Vector2 b,float width=3)=>Path(new[]{a,b},width);
        static Vector2 Rotate(Vector2 point,float degrees)
        {float angle=degrees*Mathf.Deg2Rad;return new Vector2(point.x*Mathf.Cos(angle)-point.y*Mathf.Sin(angle),point.x*Mathf.Sin(angle)+point.y*Mathf.Cos(angle));}
        static void Arc(List<Vector2> points,Vector2 centre,float xRadius,float yRadius,float start,float end,int steps=32,float tilt=0)
        {for(int i=0;i<=steps;i++){float angle=Mathf.Lerp(start,end,i/(float)steps)*Mathf.Deg2Rad;points.Add(centre+Rotate(new Vector2(Mathf.Cos(angle)*xRadius,Mathf.Sin(angle)*yRadius),tilt));}}
        void Ellipse(Vector2 centre,float xRadius,float yRadius,float tilt=0,float width=3)
        {var points=new List<Vector2>();Arc(points,centre,xRadius,yRadius,0,360,64,tilt);points.RemoveAt(points.Count-1);Path(points,width,true);}
        void Disk(Vector2 centre,float radius,Color fill)
        {for(int i=0;i<64;i++){float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;Triangle(centre,centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,centre+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,fill);}}
        static void Bezier(List<Vector2> points,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {for(int i=1;i<=16;i++){float t=i/16f,u=1-t;points.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}}
        void Frame()
        {
            foreach(var segment in Geometry(Subject).frame)Path(segment,focused?3.5f:2);
        }
        void Plate()
        {
            var points=Geometry(Subject).triangles;for(int i=0;i<points.Count;i+=3)Triangle(points[i],points[i+1],points[i+2],color);
        }
        sealed class CutGeometry{public readonly List<Vector2> triangles=new List<Vector2>();public readonly List<Vector2[]> frame=new List<Vector2[]>();}
        sealed class Edge{public Vector2 a,b;public float X(float y)=>a.x+(b.x-a.x)*(y-a.y)/(b.y-a.y);}
        static readonly Dictionary<int,CutGeometry> cutCache=new Dictionary<int,CutGeometry>();
        static readonly Vector2[] cardContour={new Vector2(16,0),new Vector2(252,0),new Vector2(268,16),new Vector2(268,416),new Vector2(252,432),new Vector2(16,432),new Vector2(0,416),new Vector2(0,16)};
        static List<Edge> Edges(IList<Vector2> loop){var result=new List<Edge>();for(int i=0;i<loop.Count;i++)result.Add(new Edge{a=loop[i],b=loop[(i+1)%loop.Count]});return result;}
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool Intersection(Edge a,Edge b,out float t)
        {var r=a.b-a.a;var s=b.b-b.a;float d=Cross(r,s);t=0;if(Mathf.Abs(d)<.00001f)return false;t=Cross(b.a-a.a,s)/d;float u=Cross(b.a-a.a,r)/d;return t>=0&&t<=1&&u>=0&&u<=1;}
        static bool Inside(IList<Vector2> loop,Vector2 p)
        {bool inside=false;for(int i=0,j=loop.Count-1;i<loop.Count;j=i++){var a=loop[i];var b=loop[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}return inside;}
        static bool InCut(List<Vector2[]> cuts,Vector2 p){foreach(var cut in cuts)if(Inside(cut,p))return true;return false;}
        // Open every edge-crossing figure to the lower/right exterior. The
        // original contour remains the inner boundary; no rectangle returns
        // behind the overflowing portion of a figure.
        static Vector2[] OpenCorner(Vector2[] loop)
        {
            float bottomAt=-1,rightAt=-1,bottomX=float.PositiveInfinity,rightY=float.PositiveInfinity;var list=Edges(loop);Vector2 rightPoint=Vector2.zero;
            for(int i=0;i<list.Count;i++)
            {
                var edge=list[i];float dy=edge.b.y-edge.a.y,dx=edge.b.x-edge.a.x;
                if(Mathf.Abs(dy)>.00001f){float t=(432-edge.a.y)/dy;if(t>=0&&t<=1){float x=Mathf.Lerp(edge.a.x,edge.b.x,t);if(x>=0&&x<=268&&x<bottomX){bottomX=x;bottomAt=i+t;}}}
                if(Mathf.Abs(dx)>.00001f){float t=(268-edge.a.x)/dx;if(t>=0&&t<=1){float y=Mathf.Lerp(edge.a.y,edge.b.y,t);if(y>=0&&y<=416&&y<rightY){rightY=y;rightAt=i+t;rightPoint=new Vector2(268,y);}}}
                var bevel=new Edge{a=new Vector2(268,416),b=new Vector2(252,432)};if(Intersection(edge,bevel,out float bevelT)){var point=Vector2.Lerp(edge.a,edge.b,bevelT);if(point.y<rightY){rightY=point.y;rightAt=i+bevelT;rightPoint=point;}}
            }
            if(bottomAt<0||rightAt<0)return null;
            var start=new Vector2(bottomX,432);var end=rightPoint;
            var forward=new List<Vector2>{start};float stop=rightAt;if(stop<bottomAt)stop+=loop.Length;
            for(int vertex=Mathf.FloorToInt(bottomAt)+1;vertex<stop;vertex++)forward.Add(loop[vertex%loop.Length]);forward.Add(end);
            var backward=new List<Vector2>{start};stop=rightAt;if(stop>bottomAt)stop-=loop.Length;
            for(int vertex=Mathf.CeilToInt(bottomAt)-1;vertex>stop;vertex--)backward.Add(loop[(vertex%loop.Length+loop.Length)%loop.Length]);backward.Add(end);
            float a=0,b=0;foreach(var p in forward)a+=p.x/268+p.y/432;foreach(var p in backward)b+=p.x/268+p.y/432;
            var outer=a/forward.Count>b/backward.Count?forward:backward;
            outer.Add(new Vector2(400,rightY));outer.Add(new Vector2(400,600));outer.Add(new Vector2(bottomX,600));return outer.ToArray();
        }
        static List<Vector2[]> CutLoops(int subject)
        {
            var cuts=new List<Vector2[]>();
            if(subject==0){var points=new List<Vector2>();Arc(points,new Vector2(146,330),150,85,0,360,96,-38);points.RemoveAt(points.Count-1);cuts.Add(points.ToArray());}
            else if(subject==1)cuts.Add(FlaskContour().ToArray());
            else if(subject==2)
            {
                var left=new List<Vector2>();var right=new List<Vector2>();for(int i=0;i<=120;i++){float y=Mathf.Lerp(200,442,i/120f),half=Mathf.Abs(35*Mathf.Cos((y-200)/242*Mathf.PI*2.8f))+2.5f;left.Add(new Vector2(210,329)+Rotate(new Vector2(-half,y-321),-16));right.Add(new Vector2(210,329)+Rotate(new Vector2(half,y-321),-16));}right.Reverse();left.AddRange(right);cuts.Add(left.ToArray());
            }
            else{cuts.Add(CogContour(new Vector2(207,378),82,12,0).ToArray());cuts.Add(CogContour(new Vector2(112,271),62,8,.25f).ToArray());}
            int originals=cuts.Count;for(int i=0;i<originals;i++){var opening=OpenCorner(cuts[i]);if(opening!=null)cuts.Add(opening);}return cuts;
        }
        // Exact polygon subtraction by horizontal slabs. Vertex/intersection heights
        // split every topology change; the cached triangles leave genuine empty space.
        static CutGeometry Geometry(int subject)
        {
            if(cutCache.TryGetValue(subject,out var cached))return cached;var result=new CutGeometry();var cuts=CutLoops(subject);var loops=new List<Vector2[]>{cardContour};loops.AddRange(cuts);var edges=new List<List<Edge>>();var heights=new List<float>{0,432};
            foreach(var loop in loops){edges.Add(Edges(loop));foreach(var point in loop)if(point.y>0&&point.y<432)heights.Add(point.y);}
            for(int i=0;i<edges.Count;i++)for(int j=i+1;j<edges.Count;j++)foreach(var a in edges[i])foreach(var b in edges[j])if(Intersection(a,b,out float crossT)){float y=Mathf.Lerp(a.a.y,a.b.y,crossT);if(y>0&&y<432)heights.Add(y);}
            heights.Sort();for(int row=0;row+1<heights.Count;row++)
            {
                float top=heights[row],bottom=heights[row+1],middle=(top+bottom)*.5f;if(bottom-top<.0001f)continue;var active=new List<Edge>();
                foreach(var list in edges)foreach(var edge in list)if(middle>Mathf.Min(edge.a.y,edge.b.y)&&middle<Mathf.Max(edge.a.y,edge.b.y))active.Add(edge);active.Sort((a,b)=>a.X(middle).CompareTo(b.X(middle)));
                for(int i=0;i+1<active.Count;i++)
                {
                    var a=active[i];var b=active[i+1];var midpoint=new Vector2((a.X(middle)+b.X(middle))*.5f,middle);if(!Inside(cardContour,midpoint)||InCut(cuts,midpoint))continue;
                    Vector2 tl=new Vector2(a.X(top),top),tr=new Vector2(b.X(top),top),br=new Vector2(b.X(bottom),bottom),bl=new Vector2(a.X(bottom),bottom);result.triangles.AddRange(new[]{tl,tr,br,tl,br,bl});
                }
            }
            foreach(var edge in edges[0])
            {
                var stops=new List<float>{0,1};for(int i=1;i<edges.Count;i++)foreach(var cut in edges[i])if(Intersection(edge,cut,out float frameT))stops.Add(frameT);stops.Sort();
                for(int i=0;i+1<stops.Count;i++)if(stops[i+1]-stops[i]>.0001f&&!InCut(cuts,Vector2.Lerp(edge.a,edge.b,(stops[i]+stops[i+1])*.5f)))result.frame.Add(new[]{Vector2.Lerp(edge.a,edge.b,stops[i]),Vector2.Lerp(edge.a,edge.b,stops[i+1])});
            }
            cutCache.Add(subject,result);return result;
        }
        void BlackHole()
        {
            var centre=new Vector2(146,330);Ellipse(centre,150,85,-38,2.5f);
            Disk(centre,60,VlabUi.Hex("243342"));Ellipse(centre,62,62,0,3.5f);
            var lens=new List<Vector2>();Arc(lens,centre,78,78,195,375,48);Path(lens,3);
            var front=new List<Vector2>();Arc(front,centre,150,85,0,180,64,-38);Path(front,3.5f);
        }
        static List<Vector2> FlaskContour()
        {
            var points=new List<Vector2>{new Vector2(170,200),new Vector2(204,200),new Vector2(204,285),new Vector2(278,410)};
            Bezier(points,new Vector2(278,410),new Vector2(290,428),new Vector2(278,442),new Vector2(256,442));points.Add(new Vector2(118,442));
            Bezier(points,new Vector2(118,442),new Vector2(96,442),new Vector2(84,428),new Vector2(96,410));points.Add(new Vector2(170,285));return points;
        }
        void Flask()
        {
            Path(FlaskContour(),3.5f,true);
            Line(new Vector2(163,200),new Vector2(211,200),3.5f);Line(new Vector2(171,214),new Vector2(203,214),3);
            var liquid=new List<Vector2>();for(int i=0;i<=40;i++){float t=i/40f;liquid.Add(new Vector2(Mathf.Lerp(115,259,t),386+Mathf.Sin(t*Mathf.PI*2)*3));}Path(liquid,4);
            Ellipse(new Vector2(172,411),5,5,0,3);Ellipse(new Vector2(220,417),8,8,0,3);
        }
        Vector2 DnaPoint(float y,float sign)
        {float phase=(y-200)/242*Mathf.PI*2.8f;return new Vector2(210,329)+Rotate(new Vector2(sign*35*Mathf.Cos(phase),y-321),-16);}
        void Dna()
        {
            foreach(float sign in new[]{-1f,1f}){var points=new List<Vector2>();for(int i=0;i<=120;i++)points.Add(DnaPoint(Mathf.Lerp(200,442,i/120f),sign));Path(points,3.5f);}
            for(float y=209;y<440;y+=16){Vector2 a=DnaPoint(y,-1),b=DnaPoint(y,1);if(Vector2.Distance(a,b)>12){Vector2 centre=(a+b)*.5f;Line(a,Vector2.Lerp(a,centre,.92f),3);Line(Vector2.Lerp(b,centre,.92f),b,3);}}
        }
        static List<Vector2> CogContour(Vector2 centre,float radius,int teeth,float phase)
        {
            var points=new List<Vector2>();for(int tooth=0;tooth<teeth;tooth++)for(int corner=0;corner<4;corner++)
            {float angle=(tooth+(corner==0?-.43f:corner==1?-.24f:corner==2?.24f:.43f))*Mathf.PI*2/teeth+phase;float r=radius*(corner==0||corner==3?.83f:1);points.Add(centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r);}
            return points;
        }
        void Cog(Vector2 centre,float radius,int teeth,float phase)
        {
            Path(CogContour(centre,radius,teeth,phase),3.5f,true);Ellipse(centre,radius*.29f,radius*.29f,0,3);
            for(int spoke=0;spoke<3;spoke++){float angle=phase+spoke*Mathf.PI*2/3;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));Line(centre+direction*radius*.44f,centre+direction*radius*.66f,3);}
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();mesh=helper;if(IsPlate)Plate();else if(IsFrame)Frame();else switch(Subject){case 0:BlackHole();break;case 1:Flask();break;case 2:Dna();break;default:Cog(new Vector2(207,378),82,12,0);Cog(new Vector2(112,271),62,8,.25f);break;}
            DrawnVertexCount=helper.currentVertCount;var vertex=UIVertex.simpleVert;Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
            for(int i=0;i<DrawnVertexCount;i++){helper.PopulateUIVertex(ref vertex,i);min=Vector2.Min(min,vertex.position);max=Vector2.Max(max,vertex.position);}DrawnBounds=DrawnVertexCount>0?new Rect(min,max-min):new Rect();mesh=null;
        }
    }
}
