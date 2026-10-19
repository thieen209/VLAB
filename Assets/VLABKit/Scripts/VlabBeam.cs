using System;
using UnityEngine;

public sealed class VlabBeam : MonoBehaviour
{
    [SerializeField] private MeshFilter body;
    [SerializeField] private Transform frame;
    [SerializeField] private Transform supportLeft, supportRight, loadPoint, gauge;
    private Mesh mesh;
    private Vector3[] rest, deformed;
    public double Span { get; private set; }=.3;
    public double Width { get; private set; }=.02;
    public double Height { get; private set; }=.005;
    public double YoungModulus { get; private set; }=2e9;
    public double Load { get; private set; }
    public double Inertia => Width*Height*Height*Height/12;
    public double MidDeflection => Load*Span*Span*Span/(48*YoungModulus*Inertia);
    public Transform Gauge => gauge;
    public MeshFilter Body => body;
    public Transform Frame => frame;

    private static bool Finite(double value) => !double.IsNaN(value)&&!double.IsInfinity(value);
    private void Awake()
    {
        if(body==null||frame==null||supportLeft==null||supportRight==null||loadPoint==null||gauge==null) throw new InvalidOperationException("Beam references missing.");
        mesh=Instantiate(body.sharedMesh); mesh.name="Beam solver instance"; body.sharedMesh=mesh;
        var vertices=mesh.vertices; rest=new Vector3[vertices.Length]; deformed=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i++) rest[i]=frame.InverseTransformPoint(body.transform.TransformPoint(vertices[i]));
        Configure(.3,.02,.005,2e9,0);
    }

    public void Configure(double span,double width,double height,double youngModulus,double load)
    {
        if(!Finite(span)||!Finite(width)||!Finite(height)||!Finite(youngModulus)||!Finite(load)||
           span<=0||span>.3||width<=0||width>.3||height<=0||youngModulus<=0||load<0) throw new ArgumentOutOfRangeException("Invalid beam parameters.");
        double inertia=width*height*height*height/12;
        double deflection=load*span*span*span/(48*youngModulus*inertia);
        if(!Finite(deflection)||height/span>.1||deflection/span>.01) throw new InvalidOperationException("Outside configured slender/small-deflection range.");
        Span=span; Width=width; Height=height; YoungModulus=youngModulus; Load=load;
        RefreshShape();
    }

    private (double value,double slope) Curve(double x)
    {
        double local=x-(.3-Span)/2;
        double endSlope=Load*3*Span*Span/(48*YoungModulus*Inertia);
        // shortcut: unloaded overhangs extend the end tangent; add self-weight/shear only with the later lesson model.
        if(local<0) return (local*endSlope,endSlope);
        if(local>Span) return (-(local-Span)*endSlope,-endSlope);
        double s=Math.Min(local,Span-local);
        return (Load*s*(3*Span*Span-4*s*s)/(48*YoungModulus*Inertia),
                Load*(3*Span*Span-12*s*s)/(48*YoungModulus*Inertia)*(local<=Span/2?1:-1));
    }

    private void RefreshShape()
    {
        for(int i=0;i<rest.Length;i++)
        {
            var p=rest[i]; var curve=Curve(p.x);
            double q=p.y*Height/.005-Height/2, normal=Math.Sqrt(1+curve.slope*curve.slope);
            var point=new Vector3((float)(p.x+q*curve.slope/normal),(float)(Height/2-curve.value+q/normal),(float)(p.z*Width/.02));
            deformed[i]=body.transform.InverseTransformPoint(frame.TransformPoint(point));
        }
        mesh.vertices=deformed; mesh.RecalculateBounds(); mesh.RecalculateNormals();
        var collider=body.GetComponent<MeshCollider>();
        if(collider!=null) { collider.sharedMesh=null; collider.sharedMesh=mesh; }
        supportLeft.position=frame.TransformPoint(new Vector3((float)((.3-Span)/2),0,0));
        supportRight.position=frame.TransformPoint(new Vector3((float)((.3+Span)/2),0,0));
        loadPoint.position=frame.TransformPoint(new Vector3(.15f,(float)(Height-MidDeflection),0));
        gauge.position=frame.TransformPoint(new Vector3(.15f,(float)-MidDeflection,0));
    }

    private void OnDestroy() { if(mesh!=null) Destroy(mesh); }
}
