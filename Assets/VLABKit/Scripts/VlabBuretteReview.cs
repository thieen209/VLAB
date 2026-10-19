using UnityEngine;

public sealed class VlabBuretteReview : MonoBehaviour
{
    [SerializeField] private VlabBurette burette;
    [SerializeField] private VlabBeaker receiver;
    private Camera reviewCamera;
    private bool closeup,light=true;
    private string note="Trạng thái thử: đầu ống đã được nạp đầy.";
    private void Start(){reviewCamera=GetComponent<Camera>();ResetRun();}
    public void ResetRun(){receiver.Remove(receiver.VolumeMl);burette.Prepare(0,true);}
    private void Update()
    {
        burette.Advance(Time.deltaTime,receiver);
        float height=closeup?burette.Meniscus.position.y:.20f;
        reviewCamera.transform.position=new Vector3(0,height,-1);
        reviewCamera.transform.LookAt(new Vector3(0,height,0));reviewCamera.orthographicSize=closeup?.0325f:.34f;
    }
    private void OnGUI()
    {
        if(reviewCamera==null)return;
        GUI.Box(new Rect(12,12,350,344),"");
        GUI.Label(new Rect(24,22,330,24),"BURETTE 25 mL · Vạch 0,1 mL");
        GUI.Label(new Rect(24,50,330,24),$"Đầu {burette.InitialReadingMl:F2} → Hiện tại {burette.ReadingMl:F2} mL");
        GUI.Label(new Rect(24,76,330,24),$"Đã chuyển {burette.DeliveredMl:F2} · Cốc {receiver.VolumeMl:F2} mL");
        GUI.Label(new Rect(24,102,330,24),$"Mở {burette.ValveOpening*100:F0}% · {(burette.Paused?"Đang dừng":"Đang chạy")}");
        if(GUI.Button(new Rect(24,134,100,30),"Đóng khóa"))burette.SetValve(0);
        if(GUI.Button(new Rect(134,134,100,30),"Mở 50%"))burette.SetValve(.5);
        if(GUI.Button(new Rect(244,134,100,30),"Mở 100%"))burette.SetValve(1);
        if(GUI.Button(new Rect(24,172,100,30),"Chạy / Dừng"))burette.Paused=!burette.Paused;
        if(GUI.Button(new Rect(134,172,100,30),"Bước 0,2 s")){burette.Paused=false;burette.Advance(.2,receiver);burette.Paused=true;}
        if(GUI.Button(new Rect(244,172,100,30),"Nạp lại lượt")){ResetRun();note="Trạng thái thử: đầu ống đã được nạp đầy.";}
        if(GUI.Button(new Rect(24,210,155,30),"Cận mặt khum / Toàn"))closeup=!closeup;
        if(GUI.Button(new Rect(189,210,155,30),"Nền sáng / tối")){light=!light;reviewCamera.backgroundColor=light?new Color(.8f,.8f,.8f):new Color(.03f,.05f,.07f);}
        if(GUI.Button(new Rect(24,248,320,30),"Thử đầu ống chưa được nạp")){burette.Prepare(burette.ReadingMl,false);note="Chưa nạp đầu ống: chặn phép chuyển đo.";}
        GUI.Label(new Rect(24,286,330,24),note);
        GUI.Label(new Rect(24,310,330,40),"Tốc độ minh họa tối đa 0,5 mL/s.\nChưa mô phỏng tráng ống, giọt và phản ứng.");
    }
}
