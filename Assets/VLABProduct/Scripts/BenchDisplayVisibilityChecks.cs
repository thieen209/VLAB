#if UNITY_EDITOR
using System;
using UnityEngine;
using Vlab.Product;

public static class BenchDisplayVisibilityChecks
{
    public static void Run(Action<string,bool> check)
    {
        var root=new GameObject("Isolated decorative bench visibility");
        GameObject Cube(string name,Vector3 position,Vector3 scale)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform);go.transform.position=position;go.transform.localScale=scale;return go;}
        try
        {
            var top=Cube("Test bench top",new Vector3(1000,.875f,1000),new Vector3(2,.05f,1));
            var display=Cube("Balance-body",new Vector3(1000,.95f,1000),new Vector3(.2f,.1f,.2f));
            var rim=Cube("Tray-rim.001",new Vector3(1000.3f,.92f,1000),new Vector3(.1f,.04f,.1f));
            var sink=Cube("Sink",new Vector3(999.5f,.95f,1000),new Vector3(.2f,.1f,.2f));
            var outside=Cube("Balance-pan",new Vector3(1003,.95f,1000),new Vector3(.2f,.1f,.2f));
            var inactive=Cube("Ready-tray",new Vector3(999.7f,.95f,1000),new Vector3(.2f,.1f,.2f));inactive.SetActive(false);
            var live=Cube("Balance-pan",new Vector3(1000.6f,.95f,1000),new Vector3(.2f,.1f,.2f));live.AddComponent<EquipmentHandle>();
            var visibility=root.AddComponent<BenchDisplayVisibility>();
            int count=visibility.ClearBench(new Vector3(1000,.905f,1000));
            check("Only two named active-bench display props are hidden, including their colliders",count==2&&!display.activeSelf&&!rim.activeSelf&&!display.GetComponent<Collider>().gameObject.activeInHierarchy);
            check("Worktop, sink, active lesson equipment, other benches and prior inactive states survive",top.activeSelf&&sink.activeSelf&&outside.activeSelf&&live.activeSelf&&!inactive.activeSelf);
            visibility.Restore();check("Leaving restores original active states",display.activeSelf&&rim.activeSelf&&!inactive.activeSelf);
            visibility.ClearBench(new Vector3(1000,.905f,1000));visibility.enabled=false;
            check("Disabling scope restores props",display.activeSelf&&rim.activeSelf&&!inactive.activeSelf);
            var cord=new GameObject("Cord-only hint fixture",typeof(LineRenderer),typeof(EquipmentHandle));cord.transform.SetParent(root.transform);
            cord.transform.position=new Vector3(1000,1,1000);var line=cord.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;
            line.SetPosition(0,Vector3.zero);line.SetPosition(1,Vector3.up*.3f);line.startWidth=line.endWidth=.003f;
            var item=cord.GetComponent<EquipmentHandle>();item.assetId="cord";item.installed=true;
            var cue=GuideCue.ShowEquipment(root.transform,item,true);
            check("Line-only cord receives a real model outline without changing source",cue.AssertPresentationOnly(null,out _)&&cue.transform.Find("Object highlight").GetComponent<LineRenderer>().enabled&&line.GetPosition(1)==Vector3.up*.3f);

        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
