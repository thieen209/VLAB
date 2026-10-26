using UnityEngine;

public class LabSink : MonoBehaviour
{
    public Transform water, stream, lever;
    public bool tapOn, drainClosed;
    [Range(0,1)] public float level;
    public float basinBottom=.675f, basinTop=.815f, nozzleHeight=1.072f;
    AudioSource flowSound;
    public void ToggleTap() { tapOn=!tapOn; }
    public void ToggleDrain() { drainClosed=!drainClosed; }
    public void Step(float seconds)
    {
        // shortcut: bounded water level models an overflow outlet; no fluid simulation.
        level=Mathf.Clamp01(level+((tapOn?.065f:0)-(drainClosed?0:.11f))*Mathf.Max(0,seconds));
        ApplyVisuals();
    }
    public void ApplyVisuals()
    {
        float surface=Mathf.Lerp(basinBottom,basinTop,level);
        if(water){water.gameObject.SetActive(level>.001f);var p=water.localPosition;p.y=surface;water.localPosition=p;}
        if(stream){stream.gameObject.SetActive(tapOn);var p=stream.localPosition;p.y=(nozzleHeight+surface)/2;stream.localPosition=p;var s=stream.localScale;s.y=(nozzleHeight-surface)/2;stream.localScale=s;}
        if(lever)lever.localRotation=Quaternion.Euler(0,0,tapOn?22:0);
    }
    void Update()
    {
        Step(Time.deltaTime);
        var app=Vlab.Product.VlabApp.Instance;
        bool flowing=tapOn&&app&&(app.Route=="activity"||app.Route=="explore")&&gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()&&AudioListener.volume>0;
        if(!flowSound){if(!flowing)return;flowSound=Vlab.Product.VlabAudio.CreateWaterSource(transform);if(!flowSound)return;}
        flowSound.transform.position=stream&&stream.parent?stream.parent.TransformPoint(new Vector3(stream.localPosition.x,nozzleHeight,stream.localPosition.z)):transform.TransformPoint(new Vector3(0,nozzleHeight,0));
        flowSound.volume=Mathf.MoveTowards(flowSound.volume,flowing?.45f:0,Time.unscaledDeltaTime*3);
        if(flowing&&!flowSound.isPlaying)flowSound.Play();else if(!flowing&&flowSound.volume==0)flowSound.Stop();
    }
    void OnDisable(){if(flowSound){flowSound.Stop();Destroy(flowSound.gameObject);flowSound=null;}}
}
