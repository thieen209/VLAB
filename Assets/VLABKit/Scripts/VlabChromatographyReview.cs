using System.Collections.Generic;
using UnityEngine;

public sealed class VlabChromatographyReview : MonoBehaviour
{
    [SerializeField] VlabChromatography[] sheets;
    [SerializeField] VlabCapillary tool;
    [SerializeField] VlabBeaker reservoir;
    readonly List<string> records=new List<string>();
    int selected;
    bool close;
    string message="Chấm mẫu và xác nhận khô trước khi đặt vị trí minh họa.";
    public VlabChromatography Active=>sheets[selected];
    public VlabCapillary Tool=>tool;
    public VlabBeaker Reservoir=>reservoir;
    public int RecordCount=>records.Count;
    public void Select(int index){if(index<0||index>=sheets.Length)throw new System.ArgumentOutOfRangeException(nameof(index));selected=index;for(int i=0;i<sheets.Length;i++)sheets[i].gameObject.SetActive(i==selected);ResetTrial();}
    public void ResetTrial(){Active.ClearTrial();tool.ClearTrial();reservoir.Remove(reservoir.VolumeMl);reservoir.Add(.010);Place(false);}
    public void Place(bool contact){tool.transform.rotation=Quaternion.FromToRotation(tool.Axis,Active.Spot.Normal)*tool.transform.rotation;tool.transform.position+=Active.Spot.Center+Active.Spot.Normal*(contact?0:.008f)-tool.Spot.position;}
    public bool Example(){double f=selected==0?.080:.060;return Active.SetMeasurements(Active.Phase,"Minh họa hình học; không phải dữ liệu sắc tố",f,new[]{f*.75,f*.40},.005);}
    public bool SaveTrial(){if(!Active.HasMeasurement||records.Count>=32)return false;string values="";for(int i=0;i<Active.BandCount;i++)values+=(i==0?"":", ")+Active.Rf(i).ToString("F2");records.Add($"{Active.Phase}: {Active.FrontDistanceM*1000:F1} mm; Rf {values} · minh họa");return true;}
    void Start()=>Select(0);
    void Update(){var c=GetComponent<Camera>();var focus=Active.Origin.position+Active.Axis*(close?.004f:.035f)+Vector3.left*.024f;c.transform.position=focus+new Vector3(.025f,.015f,-.20f);c.transform.LookAt(focus);c.orthographicSize=close?.025f:.065f;}
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12,12,350,490),GUI.skin.box);
        GUILayout.Label("SẮC KÝ · ĐO TỪ VẠCH XUẤT PHÁT");
        GUILayout.BeginHorizontal();if(GUILayout.Button("Giấy cellulose",GUILayout.Height(26)))Select(0);if(GUILayout.Button("TLC silica",GUILayout.Height(26)))Select(1);GUILayout.EndHorizontal();
        GUILayout.Label(Active.Phase+" · bộ dữ liệu sắc tố: chưa có");
        GUILayout.Label($"Ống {tool.VolumeUl:F2} µL · đã chấm {Active.Spot.AppliedUl:F2} µL · "+(Active.Spot.Wet?"ướt":"khô/chưa chấm"));
        GUILayout.BeginHorizontal();if(GUILayout.Button("Nạp 2 µL"))tool.LoadFrom(reservoir,2);if(GUILayout.Button("Đầu chạm"))Place(true);if(GUILayout.Button("Nhấc 8 mm"))Place(false);GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();if(GUILayout.Button("Chấm 0,2 µL",GUILayout.Height(26)))message=tool.TrySpot(Active.Spot,.2)?"Đã chấm; cần xác nhận khô.":"Chưa chấm: kiểm tra đầu chạm, lượng mẫu, vết ướt.";if(GUILayout.Button("Xác nhận khô",GUILayout.Height(26)))Active.Spot.ConfirmDry();GUILayout.EndHorizontal();
        if(GUILayout.Button("Đặt hai băng minh họa · Rf 0,75 / 0,40",GUILayout.Height(28)))message=Example()?"Vị trí minh họa đã đặt; không mô phỏng tốc độ dung môi.":"Cần mẫu đã chấm và khô.";
        if(Active.HasMeasurement){GUILayout.Label($"Mặt trước: {Active.FrontDistanceM*1000:F1} mm từ vạch gốc");for(int i=0;i<Active.BandCount;i++)GUILayout.Label($"Băng {i+1}: Rf = {Active.Rf(i):F3}");}
        GUILayout.BeginHorizontal();if(GUILayout.Button("Lưu lượt"))message=SaveTrial()?"Đã ghi lượt minh họa.":"Chưa có số đo / đã đủ 32 lượt.";if(GUILayout.Button("Giấy sạch"))ResetTrial();if(GUILayout.Button(close?"Toàn tấm":"Cận vạch"))close=!close;GUILayout.EndHorizontal();
        GUILayout.Label($"Đã lưu {records.Count} lượt · giữ lại khi làm sạch giấy");if(records.Count>0)GUILayout.Label(records[records.Count-1]);
        GUILayout.Label(message,new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.Label("Vị trí hai băng chỉ minh họa phép tính. Không dùng Rf để gọi tên sắc tố; không chuyển dữ liệu TLC sang giấy. Chưa có bình, dung môi hay động học chạy sắc ký.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.EndArea();
    }
}
