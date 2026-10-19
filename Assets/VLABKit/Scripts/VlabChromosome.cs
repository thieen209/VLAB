using UnityEngine;

public sealed class VlabChromosome : MonoBehaviour
{
    public enum Stage { Interphase, Prophase, Metaphase, Anaphase, Telophase }
    [SerializeField] Transform[] chromatids,centromeres,guides;
    [SerializeField] Vector3[] chromatidPositions,centromerePositions;
    [SerializeField] Quaternion[] chromatidRotations,centromereRotations;
    [SerializeField] Vector3 localVertical,localSeparationAxis;
    public Stage Phase {get;private set;}
    public float Separation {get;private set;}
    public Transform Chromatid(int i)=>chromatids[i];
    public Transform Centromere(int i)=>centromeres[i];
    public Transform Guide(int i)=>guides[i];
    public bool SetStage(Stage stage,float separation=.045f)
    {
        if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(int)stage<0||(int)stage>4||float.IsNaN(separation)||float.IsInfinity(separation)||separation<.045f||separation>.075f)return false;
        Phase=stage;Separation=stage==Stage.Anaphase?separation:0;
        bool condensed=stage!=Stage.Interphase&&stage!=Stage.Telophase;
        for(int i=0;i<2;i++)
        {
            float sign=i==0?-1:1;
            chromatids[i].gameObject.SetActive(condensed);centromeres[i].gameObject.SetActive(condensed);
            guides[i].gameObject.SetActive(stage==Stage.Metaphase||stage==Stage.Anaphase);
            var offset=localSeparationAxis*(sign*Separation);
            chromatids[i].localPosition=chromatidPositions[i]+offset;centromeres[i].localPosition=centromerePositions[i]+offset;
            var turn=stage==Stage.Anaphase?Quaternion.AngleAxis(180,localVertical):Quaternion.identity;
            chromatids[i].localRotation=turn*chromatidRotations[i];centromeres[i].localRotation=turn*centromereRotations[i];
            guides[i].localScale=new Vector3((.103f-Separation)/.103f,1,1);
        }
        return true;
    }
    void Start()=>SetStage(Stage.Metaphase);
    // shortcut: discrete explanatory poses, not molecular motion; replace only with validated biological animation.
}
