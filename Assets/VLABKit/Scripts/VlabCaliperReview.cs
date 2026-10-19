using UnityEngine;

public sealed class VlabCaliperReview : MonoBehaviour
{
    [SerializeField] private VlabCaliper caliper;
    [SerializeField] private SphereCollider sample;
    private Camera reviewCamera;
    private int view;
    private bool sampleVisible;
    private string note="Kéo thanh hoặc chọn độ mở. Khóa để giữ số đọc.";
    private void Start(){reviewCamera=GetComponent<Camera>();sample.gameObject.SetActive(false);caliper.Locked=false;caliper.SetOpening(12.34);}
    public void PlaceSample(bool shifted)
    {
        sampleVisible=true;sample.gameObject.SetActive(true);
        sample.transform.position=caliper.FixedFace.position+caliper.Axis*(.008f+(shifted?.002f:0));
        note=shifted?"Bi chưa tì ngàm cố định: phép đo phải bị chặn.":"Bi Ø16 mm đã tì ngàm cố định.";
    }
    public void Measure(){note=caliper.TryMeasureOutside(sample)?$"Phép đo vừa thực hiện: {caliper.ReadingMm:F2} mm.":"Không đo được: kiểm tra khóa và vị trí mẫu.";}
    private void Update()
    {
        if(view==1)
        {
            var point=caliper.VernierZero.position+caliper.Axis*.025f;
            reviewCamera.transform.position=new Vector3(point.x-.014f,.4f,.0f);reviewCamera.transform.rotation=Quaternion.Euler(90,0,0);reviewCamera.orthographicSize=.045f;
        }
        else if(view==2)
        {
            var point=(caliper.FixedFace.position+caliper.MovingFace.position)*.5f;
            reviewCamera.transform.position=point+new Vector3(-.025f,.2f,-.04f);reviewCamera.transform.LookAt(point-new Vector3(.025f,0,0));reviewCamera.orthographicSize=.07f;
        }
        else
        {
            reviewCamera.transform.position=new Vector3(.12f,.5f,-.13f);reviewCamera.transform.LookAt(new Vector3(.10f,0,0));reviewCamera.orthographicSize=.30f/Mathf.Max(1,reviewCamera.aspect);
        }
    }
    private void OnGUI()
    {
        if(reviewCamera==null)return;
        GUI.Box(new Rect(12,12,325,390),"");GUI.Label(new Rect(24,22,305,24),"THƯỚC CẶP · 0–150 mm · Du xích 0,02 mm");
        GUI.Label(new Rect(24,50,305,24),$"Số đọc: {caliper.ReadingMm:F2} mm   {(caliper.Locked?"ĐÃ KHÓA":"MỞ KHÓA")}");
        GUI.Label(new Rect(24,76,305,24),$"Thang chính {caliper.MainMillimeters} mm + vạch {caliper.VernierIndex} × 0,02");
        float opening=GUI.HorizontalSlider(new Rect(24,110,297,20),(float)caliper.OpeningMm,0,150);
        if(Mathf.Abs(opening-(float)caliper.OpeningMm)>.001f)caliper.SetOpening(opening);
        for(int i=0;i<3;i++)if(GUI.Button(new Rect(24+i*102,140,95,28),new[]{"0 mm","75 mm","150 mm"}[i]))caliper.SetOpening(i*75);
        if(GUI.Button(new Rect(24,176,95,28),"−0,02 mm"))caliper.SetOpening(System.Math.Max(0,caliper.OpeningMm-.02));
        if(GUI.Button(new Rect(126,176,95,28),"+0,02 mm"))caliper.SetOpening(System.Math.Min(150,caliper.OpeningMm+.02));
        if(GUI.Button(new Rect(228,176,95,28),"Khóa / Mở"))caliper.Locked=!caliper.Locked;
        if(GUI.Button(new Rect(24,212,145,28),"Đặt bi Ø16 mm"))PlaceSample(false);
        if(GUI.Button(new Rect(178,212,145,28),"Đo bi"))Measure();
        if(GUI.Button(new Rect(24,248,145,28),"Thử bi chưa tì"))PlaceSample(true);
        if(GUI.Button(new Rect(178,248,145,28),"Ẩn / Hiện bi")){sampleVisible=!sampleVisible;sample.gameObject.SetActive(sampleVisible);}
        if(GUI.Button(new Rect(24,284,299,28),new[]{"Xem cận thang","Xem cận ngàm","Xem toàn thước"}[view]))view=(view+1)%3;
        GUI.Label(new Rect(24,320,302,40),note,new GUIStyle(GUI.skin.label){wordWrap=true});
        GUI.Label(new Rect(24,360,302,32),"Đo theo một đường tiếp xúc. Chưa đo lỗ/lòng sâu.");
    }
}
