using UnityEngine;

public class LabShade : MonoBehaviour
{
    public Transform fabric;
    public float width=1, fullDrop=1;
    public bool lowered;
    float drop=.025f;
    public void Toggle(){lowered=!lowered;}
    public void Step(float seconds){drop=Mathf.MoveTowards(drop,lowered?fullDrop:.025f,Mathf.Max(0,seconds)*.8f);Apply();}
    void Apply(){if(!fabric)return;fabric.localPosition=new Vector3(0,-drop/2,.075f);fabric.localScale=new Vector3(width,drop,.006f);}
    void Awake(){drop=lowered?fullDrop:.025f;Apply();}
    void Update(){Step(Time.deltaTime);}
    // shortcut: moving fabric casts real-time shadows; baked indirect daylight stays fixed.
}
