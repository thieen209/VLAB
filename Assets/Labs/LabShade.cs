using UnityEngine;

public class LabShade : MonoBehaviour
{
    public Transform fabric;
    public Transform bottomBar;
    public float fabricScaleY=1, bottomY=-1;
    public float width=1, fullDrop=1;
    public bool lowered;
    public Light[] daylight=new Light[0];
    public float[] daylightOpenIntensities=new float[0];
    public ReflectionProbe roomReflection;
    bool reflectionPending=true;
    public float OpenFraction=>1-Mathf.InverseLerp(.025f,Mathf.Max(.026f,fullDrop),drop);
    public void RefreshDaylight(){for(int i=0;i<daylight.Length&&i<daylightOpenIntensities.Length;i++)if(daylight[i])daylight[i].intensity=daylightOpenIntensities[i]*OpenFraction;}
    float drop=.025f;
    float startDrop=.025f,transitionTarget=.025f,travel=1;
    public void Toggle(){lowered=!lowered;reflectionPending=true;}
    public void Step(float seconds)
    {
        float target=lowered?fullDrop:.025f;
        if(target!=transitionTarget){startDrop=drop;transitionTarget=target;travel=0;}
        travel=Mathf.MoveTowards(travel,1,Mathf.Max(0,seconds)/(Mathf.Abs(transitionTarget-startDrop)/.8f+.2f));
        drop=Mathf.Lerp(startDrop,transitionTarget,Mathf.SmoothStep(0,1,travel));Apply();
    }
    void Apply(){RefreshDaylight();if(!fabric)return;if(bottomBar){float fraction=drop/Mathf.Max(.025f,fullDrop);fabric.localScale=new Vector3(1,fabricScaleY*fraction,1);var p=bottomBar.localPosition;p.y=bottomY*fraction;bottomBar.localPosition=p;return;}fabric.localPosition=new Vector3(0,-drop/2,.075f);fabric.localScale=new Vector3(width,drop,.006f);}
    void Awake(){drop=lowered?fullDrop:.025f;startDrop=transitionTarget=drop;travel=1;Apply();}
    void Update(){float target=lowered?fullDrop:.025f;if(drop!=target)Step(Time.deltaTime);if(reflectionPending&&drop==target){reflectionPending=false;if(roomReflection&&roomReflection.enabled)roomReflection.RenderProbe();}}
    // Rooms with daylight bindings bake indoor light only; legacy scenes retain their authored lighting.
}
