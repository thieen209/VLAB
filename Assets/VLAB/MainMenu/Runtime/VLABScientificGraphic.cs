using UnityEngine;
using UnityEngine.UI;

namespace VLAB.MainMenu
{
    // A single static UI mesh supplies depth without particles, blur or an Update loop.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VLABScientificGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var center = rect.center;
            float size = Mathf.Min(rect.width, rect.height) * .36f;
            for (int orbit = 0; orbit < 3; orbit++)
            {
                float rotation = orbit * Mathf.PI / 3;
                for (int i = 0; i < 100; i++)
                {
                    Vector2 a = Point(i * Mathf.PI * 2 / 100, rotation, size) + center;
                    Vector2 b = Point((i + 1) * Mathf.PI * 2 / 100, rotation, size) + center;
                    Line(vh, a, b, 1.5f, new Color(.0f, .71f, .85f, .55f));
                }
                Vector2 dot = Point(orbit * 1.8f + .7f, rotation, size) + center;
                Disk(vh, dot, 7, new Color(.03f, .84f, .63f));
            }
            Disk(vh, center, 13, new Color(.0f, .71f, .85f));
            for (int i = 0; i < 8; i++)
            {
                float y = rect.yMin + rect.height * (i + 1) / 9;
                Line(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), .5f, new Color(.3f,.6f,.75f,.1f));
            }
        }
        private static Vector2 Point(float t, float rotation, float size)
        {
            var p = new Vector2(Mathf.Cos(t) * size, Mathf.Sin(t) * size * .38f);
            return new Vector2(p.x * Mathf.Cos(rotation) - p.y * Mathf.Sin(rotation), p.x * Mathf.Sin(rotation) + p.y * Mathf.Cos(rotation));
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            var n = new Vector2(-(b-a).y, (b-a).x).normalized * width / 2;
            int v = vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero); vh.AddVert(a+n,color,Vector2.zero);
            vh.AddVert(b+n,color,Vector2.zero); vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
        }
        private static void Disk(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            int v=vh.currentVertCount;
            vh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<=20;i++) vh.AddVert(center+new Vector2(Mathf.Cos(i*Mathf.PI/10),Mathf.Sin(i*Mathf.PI/10))*radius,color,Vector2.zero);
            for(int i=0;i<20;i++) vh.AddTriangle(v,v+i+1,v+i+2);
        }
    }
}
