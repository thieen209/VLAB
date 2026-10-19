using UnityEngine;

public sealed class VlabCamReview : MonoBehaviour
{
    [SerializeField] private VlabCam cam;
    [SerializeField] private Transform inspectionTip;
    public Transform InspectionTip=>inspectionTip;
    private bool oblique;
    void Start(){cam.Paused=true;cam.SetAngle(0);ApplyPose();}
    void Update(){cam.Advance(Time.deltaTime);ApplyPose();}
    public void ApplyPose()
    {
        inspectionTip.position=cam.ContactPoint;
        var c=GetComponent<Camera>();c.transform.position=oblique?new Vector3(.07f,.09f,-.20f):new Vector3(-.016f,.048f,-.22f);
        c.transform.LookAt(new Vector3(-.016f,.048f,0));c.orthographicSize=.053f;
    }
    static void Line(Vector2 a,Vector2 b,Color color)
    {
        var matrix=GUI.matrix;var old=GUI.color;GUI.color=color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
        GUI.DrawTexture(new Rect(a.x,a.y,(b-a).magnitude,1.5f),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=old;
    }
    void OnGUI()
    {
        GUI.Box(new Rect(12,12,330,440),"");
        GUI.Label(new Rect(24,22,306,24),"CAM · TIẾP XÚC ĐẦU NHỌN · LIFT 10 mm");
        GUI.Label(new Rect(24,49,306,24),$"θ = {cam.AngleDegrees:F2}°   ·   {cam.Phase}");
        GUI.Label(new Rect(24,73,306,24),$"Nâng hình học: {cam.LiftMeters*1000:F3} mm");
        GUI.Label(new Rect(24,97,306,24),$"Lý tưởng: {cam.IdealLiftMeters*1000:F3} mm · Δ {(cam.LiftMeters-cam.IdealLiftMeters)*1e6:F2} µm");
        float angle=GUI.HorizontalSlider(new Rect(24,132,302,20),(float)cam.AngleDegrees,0,359.999f);
        if(Mathf.Abs(angle-(float)cam.AngleDegrees)>.001f){cam.Paused=true;cam.SetAngle(angle);}
        for(int i=0;i<6;i++)if(GUI.Button(new Rect(24+i*51,160,47,27),(i*60)+"°")){cam.Paused=true;cam.SetAngle(i*60);}
        GUI.Label(new Rect(24,194,306,24),$"Tốc độ đặt: {cam.Rpm:F0} rpm (minh họa)");
        float rpm=GUI.HorizontalSlider(new Rect(24,220,302,20),(float)cam.Rpm,-120,120);if(Mathf.Abs(rpm-(float)cam.Rpm)>.01f)cam.SetSpeed(Mathf.Round(rpm));
        if(GUI.Button(new Rect(24,244,94,28),cam.Paused?"Chạy":"Dừng"))cam.Paused=!cam.Paused;
        if(GUI.Button(new Rect(128,244,94,28),"Bước +5°")){cam.Paused=true;cam.SetAngle(cam.AngleDegrees+5);}
        if(GUI.Button(new Rect(232,244,94,28),"Về 0°")){cam.Paused=true;cam.SetAngle(0);}
        if(GUI.Button(new Rect(24,280,302,28),oblique?"Nhìn chính diện":"Nhìn nghiêng"))oblique=!oblique;
        GUI.Label(new Rect(24,311,306,22),"Đường nâng theo góc · 0 đến 10 mm");
        Vector2 Point(double deg)=>new Vector2(28+(float)deg/360*292,396-(float)((cam.RadiusAt(deg)-.025)/.01)*57);
        Line(new Vector2(28,396),new Vector2(320,396),Color.gray);
        for(int i=0;i<180;i++)Line(Point(i*2),Point((i+1)*2),new Color(.4f,.85f,.85f));
        var p=Point(cam.AngleDegrees);Line(new Vector2(p.x,339),new Vector2(p.x,398),new Color(1,.75f,.3f));
        GUI.Label(new Rect(24,408,306,36),"Mũi trắng chỉ kiểm tra hình học. Chưa có lực lò xo hoặc mô phỏng mất tiếp xúc.",new GUIStyle(GUI.skin.label){wordWrap=true});
    }
}
