using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class VlabCable : MonoBehaviour
{
    [SerializeField] private Transform[] plugs,contacts,exits;
    [SerializeField] private MeshFilter cableGuide;
    private readonly VlabElectricalTerminal[] terminals=new VlabElectricalTerminal[2];
    private readonly Vector3[] points=new Vector3[49],tangents=new Vector3[49],normals=new Vector3[49];
    private Mesh mesh;
    private Vector2[] uv;
    private Vector3[] vertices,vertexNormals;
    private Vector3 normalInExit;
    private float windingSign;
    public bool RouteValid { get; private set; }
    public float RouteLength { get; private set; }
    public float MinimumBendRadius { get; private set; }
    public bool IsConducting => isActiveAndEnabled&&RouteValid&&Matched(0)&&Matched(1);
    public VlabElectricalTerminal Terminal(int end){CheckEnd(end);return terminals[end];}
    public Transform Plug(int end){CheckEnd(end);return plugs[end];}
    public Transform Contact(int end){CheckEnd(end);return contacts[end];}
    public Transform Exit(int end){CheckEnd(end);return exits[end];}
    private static void CheckEnd(int end){if(end<0||end>1)throw new ArgumentOutOfRangeException(nameof(end));}
    private static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
    private void Awake()
    {
        if(plugs==null||contacts==null||exits==null||plugs.Length!=2||contacts.Length!=2||exits.Length!=2||cableGuide==null)throw new InvalidOperationException("Cable Blender references missing.");
        mesh=Instantiate(cableGuide.sharedMesh);mesh.name="Runtime deformation of Blender cable";mesh.MarkDynamic();cableGuide.sharedMesh=mesh;
        uv=mesh.uv;vertices=mesh.vertices;vertexNormals=new Vector3[vertices.Length];
        Vector3 normal=Vector3.zero,side=Vector3.zero;
        for(int i=0;i<uv.Length;i++)if(uv[i].x<.0001f)
        {
            if(Mathf.Abs(uv[i].y)<.0001f)normal=(cableGuide.transform.TransformPoint(vertices[i])-exits[0].position).normalized;
            if(Mathf.Abs(uv[i].y-.25f)<.0001f)side=(cableGuide.transform.TransformPoint(vertices[i])-exits[0].position).normalized;
        }
        if(normal.sqrMagnitude<.9f||side.sqrMagnitude<.9f)throw new InvalidOperationException("Blender radial UV convention missing.");
        windingSign=Mathf.Sign(Vector3.Dot(Vector3.Cross((exits[0].position-contacts[0].position).normalized,normal),side));
        normalInExit=exits[0].InverseTransformDirection(normal);RefreshRoute();
    }
    private bool Matched(int end)
    {
        var terminal=terminals[end];return terminal!=null&&terminal.Compatible&&Vector3.Distance(contacts[end].position,terminal.InsertedTip)<.0001f&&Vector3.Dot((contacts[end].position-exits[end].position).normalized,-terminal.Axis)>.999f;
    }
    private void Snap(int end,VlabElectricalTerminal terminal)
    {
        Vector3 axis=(contacts[end].position-exits[end].position).normalized;
        plugs[end].rotation=Quaternion.FromToRotation(axis,-terminal.Axis)*plugs[end].rotation;
        plugs[end].position+=terminal.InsertedTip-contacts[end].position;
    }
    public bool Connect(int end,VlabElectricalTerminal terminal)
    {
        CheckEnd(end);
        if(!isActiveAndEnabled||terminal==null||!terminal.Claim(this,end))return false;
        var old=terminals[end];Vector3 position=plugs[end].position;Quaternion rotation=plugs[end].rotation;
        Snap(end,terminal);
        if(!RefreshRoute())
        {
            plugs[end].SetPositionAndRotation(position,rotation);RefreshRoute();
            if(old!=terminal)terminal.Release(this,end);
            return false;
        }
        if(old!=null&&old!=terminal)old.Release(this,end);
        terminals[end]=terminal;return true;
    }
    // A lesson positions both plugs as one explicit wiring action. Validate the
    // completed physical route atomically: a remote free end in the tray must
    // not make a valid final cable impossible to install. No route guard changes.
    public bool TryConnectPair(VlabElectricalTerminal a,VlabElectricalTerminal b)
    {
        if(!isActiveAndEnabled||!a||!b||a==b||!a.Compatible||!b.Compatible)return false;
        var previousA=terminals[0];var previousB=terminals[1];
        if(!a.Claim(this,0))return false;
        if(!b.Claim(this,1)){if(previousA!=a)a.Release(this,0);return false;}
        var p0=plugs[0].position;var q0=plugs[0].rotation;var p1=plugs[1].position;var q1=plugs[1].rotation;
        Snap(0,a);Snap(1,b);
        if(!RefreshRoute())
        {
            plugs[0].SetPositionAndRotation(p0,q0);plugs[1].SetPositionAndRotation(p1,q1);RefreshRoute();
            if(previousA!=a)a.Release(this,0);if(previousB!=b)b.Release(this,1);return false;
        }
        if(previousA&&previousA!=a)previousA.Release(this,0);if(previousB&&previousB!=b)previousB.Release(this,1);
        terminals[0]=a;terminals[1]=b;return IsConducting;
    }
    public void Disconnect(int end)
    {
        CheckEnd(end);var terminal=terminals[end];terminals[end]=null;
        if(terminal!=null)
        {
            terminal.Release(this,end);plugs[end].position+=terminal.Axis*.020f;
            if(mesh!=null)RefreshRoute();
        }
    }
    public bool MoveFreeEnd(int end,Vector3 position,Quaternion rotation)
    {
        CheckEnd(end);
        if(!isActiveAndEnabled||terminals[end]!=null||!Finite(position)||!Finite(new Vector3(rotation.x,rotation.y,rotation.z))||float.IsNaN(rotation.w)||float.IsInfinity(rotation.w)||Mathf.Abs(Quaternion.Dot(rotation,rotation)-1)>.001f)return false;
        Vector3 oldPosition=plugs[end].position;Quaternion oldRotation=plugs[end].rotation;
        plugs[end].SetPositionAndRotation(position,rotation);
        if(RefreshRoute())return true;
        plugs[end].SetPositionAndRotation(oldPosition,oldRotation);RefreshRoute();return false;
    }
    public bool RefreshRoute()
    {
        if(mesh==null)return false;
        Vector3 p0=exits[0].position,p3=exits[1].position;
        float distance=Vector3.Distance(p0,p3);
        bool valid=Finite(p0)&&Finite(p3)&&distance>=.035f&&distance<=.60f&&Vector3.Distance(plugs[0].lossyScale,Vector3.one)<.0001f&&Vector3.Distance(plugs[1].lossyScale,Vector3.one)<.0001f;
        RouteLength=0;MinimumBendRadius=float.PositiveInfinity;
        if(valid)
        {
            float arm=distance*.5f+.06f;
            Vector3 p1=p0+(p0-contacts[0].position).normalized*arm,p2=p3+(p3-contacts[1].position).normalized*arm;
            for(int i=0;i<=48;i++)
            {
                float t=i/48f,u=1-t;
                points[i]=p0*(u*u*u)+p1*(3*u*u*t)+p2*(3*u*t*t)+p3*(t*t*t);
                var derivative=3*(p1-p0)*(u*u)+6*(p2-p1)*(u*t)+3*(p3-p2)*(t*t);
                if(derivative.sqrMagnitude<1e-10f||!Finite(points[i]))valid=false;
                tangents[i]=derivative.normalized;
                if(i>0)RouteLength+=Vector3.Distance(points[i],points[i-1]);
                if(i>1)
                {
                    var a=points[i-1]-points[i-2];var b=points[i]-points[i-1];float cross=Vector3.Cross(a,b).magnitude;
                    if(cross>1e-10f)MinimumBendRadius=Mathf.Min(MinimumBendRadius,a.magnitude*b.magnitude*(a+b).magnitude/(2*cross));
                }
            }
            valid&=RouteLength<=.60f&&MinimumBendRadius>=.006f;
            for(int i=0;i<=48&&valid;i++)for(int j=i+3;j<=48;j++)if(Vector3.Distance(points[i],points[j])<.0033f)valid=false;
        }
        RouteValid=valid;cableGuide.GetComponent<Renderer>().enabled=valid;
        if(!valid)return false;
        normals[0]=Vector3.ProjectOnPlane(exits[0].TransformDirection(normalInExit),tangents[0]).normalized;
        for(int i=1;i<=48;i++)normals[i]=(Quaternion.FromToRotation(tangents[i-1],tangents[i])*normals[i-1]).normalized;
        for(int i=0;i<vertices.Length;i++)
        {
            int row=Mathf.Clamp(Mathf.RoundToInt(uv[i].x*48),0,48);float angle=uv[i].y*Mathf.PI*2;
            var radial=normals[row]*Mathf.Cos(angle)+Vector3.Cross(tangents[row],normals[row])*windingSign*Mathf.Sin(angle);
            vertices[i]=cableGuide.transform.InverseTransformPoint(points[row]+radial*.00165f);vertexNormals[i]=cableGuide.transform.InverseTransformDirection(radial);
        }
        mesh.vertices=vertices;mesh.normals=vertexNormals;mesh.RecalculateBounds();return true;
    }
    public bool SyncConnections()
    {
        for(int end=0;end<2;end++)if(terminals[end]!=null)
        {
            if(terminals[end].Compatible)Snap(end,terminals[end]);else Disconnect(end);
        }
        return RefreshRoute();
    }
    private void LateUpdate()=>SyncConnections();
    private void OnDisable(){Disconnect(0);Disconnect(1);}
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);}

    public static bool AreConnected(VlabElectricalTerminal a,VlabElectricalTerminal b,IEnumerable<VlabCable> cables)
    {
        if(cables==null)throw new ArgumentNullException(nameof(cables));
        if(a==null||b==null||!a.Compatible||!b.Compatible)return false;
        var visited=new HashSet<string>{a.NodeId};var queue=new Queue<string>();queue.Enqueue(a.NodeId);
        while(queue.Count>0)
        {
            string node=queue.Dequeue();if(node==b.NodeId)return true;
            foreach(var cable in cables)
            {
                if(cable==null||!cable.IsConducting)continue;
                string x=cable.terminals[0].NodeId,y=cable.terminals[1].NodeId;
                string neighbor=x==node?y:(y==node?x:null);
                if(neighbor!=null&&visited.Add(neighbor))queue.Enqueue(neighbor);
            }
        }
        return false;
    }
}
