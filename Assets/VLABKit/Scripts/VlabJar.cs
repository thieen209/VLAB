using System;
using UnityEngine;

public sealed class VlabJar : MonoBehaviour
{
    [SerializeField] VlabBeaker liquid;
    [SerializeField] Transform lid, lidClosed, lidOpen, sampleOrigin;
    [SerializeField] TextMesh solventCard;
    [SerializeField] Vector3 localUp;
    public VlabBeaker Liquid => liquid;
    public bool LidOpen { get; private set; }
    public string SolventSystem { get; private set; } = "";
    public Transform SampleOrigin => sampleOrigin;
    bool Ready => isActiveAndEnabled && (transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f;
    public bool SetLid(bool open)
    {
        if(!Ready) return false;
        lid.position=(open?lidOpen:lidClosed).position; LidOpen=open; return true;
    }
    public bool SetSolventSystem(string identityAndComposition)
    {
        if(!Ready || string.IsNullOrWhiteSpace(identityAndComposition) || identityAndComposition.Length>80) return false;
        foreach(char c in identityAndComposition) if(char.IsControl(c)) return false;
        SolventSystem=identityAndComposition.Trim();
        string lines="";for(int i=0;i<SolventSystem.Length;i+=28) lines+="\n"+SolventSystem.Substring(i,Math.Min(28,SolventSystem.Length-i));
        solventCard.text="SOLVENT SYSTEM"+lines+"\nSurface <= 17 mm\nSample origin: 18 mm";return true;
    }
    public bool TrySeatPaper(VlabChromatography paper)
    {
        if(!Ready || !LidOpen || paper==null || !paper.isActiveAndEnabled || paper.Phase!="Cellulose paper" || (paper.transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f) return false;
        var up=transform.TransformDirection(localUp).normalized;
        paper.transform.rotation=Quaternion.FromToRotation(paper.Axis,up)*paper.transform.rotation;
        paper.transform.position+=sampleOrigin.position-paper.Origin.position;return true;
    }
    public bool ReadyToDevelop(VlabChromatography paper)
    {
        return Ready && !LidOpen && SolventSystem.Length>0 && liquid.VolumeMl>0 && paper!=null && paper.isActiveAndEnabled && (paper.transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f && paper.Phase=="Cellulose paper" && Vector3.Distance(paper.Origin.position,sampleOrigin.position)<1e-5f && Vector3.Dot(paper.Axis,transform.TransformDirection(localUp))>.9999f && liquid.SurfaceHeightMeters<.018;
    }
}


