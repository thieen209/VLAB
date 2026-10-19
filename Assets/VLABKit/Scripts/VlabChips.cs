using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class VlabChips : MonoBehaviour
{
    [SerializeField] Mesh[] variants;
    [SerializeField] Material[] materials;
    [SerializeField] double[] particleMassG,particleAreaM2;
    [SerializeField] float[] spans;
    [SerializeField] Quaternion meshRotation=Quaternion.identity;
    readonly Matrix4x4[] matrices=new Matrix4x4[192];
    public int SizeIndex {get;private set;}
    public int BatchUnits {get;private set;}
    public int Count {get;private set;}
    public double MassGrams=>Count*particleMassG[SizeIndex];
    public double ExteriorAreaM2=>Count*particleAreaM2[SizeIndex];
    public Mesh SharedMesh=>variants[SizeIndex];
    public bool ValidFrame=>(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
    public Matrix4x4 MatrixAt(int index){if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));return matrices[index];}
    public bool Configure(int size,int units)
    {
        if(size<0||size>2||units<0||units>3)throw new ArgumentOutOfRangeException("Use three sizes and 0–3 batches.");
        if(!isActiveAndEnabled||!ValidFrame)return false;
        SizeIndex=size;BatchUnits=units;Count=(64>>(3*size))*units;BuildMatrices();return true;
    }
    void Awake()
    {
        if(variants==null||variants.Length!=3||materials==null||materials.Length!=3||particleMassG==null||particleMassG.Length!=3||particleAreaM2==null||particleAreaM2.Length!=3||spans==null||spans.Length!=3)
            throw new InvalidOperationException("Chip metadata references missing.");
        for(int i=0;i<3;i++)if(variants[i]==null||materials[i]==null||!materials[i].enableInstancing||!(particleMassG[i]>0)||double.IsInfinity(particleMassG[i])||!(particleAreaM2[i]>0)||double.IsInfinity(particleAreaM2[i])||!(spans[i]>0)||float.IsInfinity(spans[i]))
            throw new InvalidOperationException("Invalid chip metadata/material.");
        Configure(0,1);
    }
    void BuildMatrices()
    {
        int columns=Mathf.CeilToInt(Mathf.Sqrt(Count)),rows=columns==0?0:Mathf.CeilToInt((float)Count/columns);float spacing=spans[SizeIndex]*1.3f;
        for(int i=0;i<Count;i++)
        {
            var p=new Vector3((i%columns-(columns-1)*.5f)*spacing,0,(i/columns-(rows-1)*.5f)*spacing);
            matrices[i]=Matrix4x4.TRS(transform.TransformPoint(p),transform.rotation*meshRotation,Vector3.one);
        }
    }
    void Update()
    {
        if(Count==0||!ValidFrame)return;
        BuildMatrices();
        for(int sub=0;sub<materials.Length;sub++)
        {
            var rp=new RenderParams(materials[sub]){worldBounds=new Bounds(transform.position,Vector3.one*.05f),shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};
            Graphics.RenderMeshInstanced(rp,SharedMesh,sub,matrices,Count);
        }
    }
    // shortcut: exterior geometric area only; use measured accessible area and calibrated kinetics for chemical rates.
}
