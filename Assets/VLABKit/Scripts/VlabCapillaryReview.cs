using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VlabCapillaryReview : MonoBehaviour
{
    [SerializeField] private VlabCapillary tool;
    [SerializeField] private VlabCapillarySpot target;
    [SerializeField] private VlabBeaker reservoir;
    [SerializeField] private CapsuleCollider selection;
    public VlabCapillary Tool=>tool;
    public VlabCapillarySpot Target=>target;
    public VlabBeaker Reservoir=>reservoir;
    public CapsuleCollider Selection=>selection;
    bool full,selected;
    string message="Nạp mẫu kiểm tra, đặt đầu chạm rồi chấm.";
    void Start(){ResetTrial();Frame();}
    public void ResetTrial(){tool.ClearTrial();target.ClearTrial();reservoir.Remove(reservoir.VolumeMl);reservoir.Add(.010);Place(false);}
    public void Place(bool contact){tool.transform.position=target.Center+target.Normal*(contact?0:.003f);}
    void Update()
    {
        var mouse=Mouse.current;if(mouse==null||!mouse.leftButton.wasPressedThisFrame)return;
        var p=mouse.position.ReadValue();if(p.x<350)return;
        if(selection.Raycast(GetComponent<Camera>().ScreenPointToRay(p),out _,2)){selected=!selected;message=selected?"Đã chọn ống · kích thước mesh giữ nguyên.":"Bỏ chọn ống.";}
    }
    void Frame()
    {
        var c=GetComponent<Camera>();var focus=full?new Vector3(-.012f,.035f,0):new Vector3(-.003f,.002f,0);
        c.transform.position=focus+(full?new Vector3(.015f,.018f,-.09f):new Vector3(.003f,.008f,-.018f));c.transform.LookAt(focus);c.orthographicSize=full?.043f:.009f;
    }
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12,12,330,465),GUI.skin.box);
        GUILayout.Label("ỐNG MAO QUẢN · CHẤM MẪU NHỎ");
        GUILayout.Label("70 mm · Ø1,5 mm · không phải pipette 10 mL");
        GUILayout.Space(8);GUILayout.Label($"Trong ống: {tool.VolumeUl:F3} / 2 µL");
        GUILayout.Label($"Nguồn kiểm tra: {reservoir.VolumeMl*1000:F3} µL");
        GUILayout.Label($"Đã chấm cộng dồn: {target.AppliedUl:F3} µL · {target.SpotCount} lần");
        GUILayout.Label(target.Wet?"Vết đang ướt · cần khô trước lần tiếp":"Vết đã khô / chưa chấm");
        if(GUILayout.Button("Nạp tối đa 2 µL từ nguồn kiểm tra",GUILayout.Height(28)))message=$"Đã nạp {tool.LoadFrom(reservoir,2):F3} µL.";
        GUILayout.BeginHorizontal();if(GUILayout.Button("Đặt đầu chạm",GUILayout.Height(28))){Place(true);message="Đầu ống ở tâm vị trí kiểm tra.";}if(GUILayout.Button("Nhấc 3 mm",GUILayout.Height(28)))Place(false);GUILayout.EndHorizontal();
        if(GUILayout.Button("Chấm 0,2 µL",GUILayout.Height(28)))message=tool.TrySpot(target,.2)?"Đã chấm 0,2 µL · chờ khô.":"Chưa chấm: kiểm tra tiếp xúc, lượng mẫu hoặc vết ướt.";
        if(GUILayout.Button("Xác nhận vết đã khô (kiểm tra)",GUILayout.Height(28))){target.ConfirmDry();message="Đã xác nhận khô; không giả lập thời gian bay hơi.";}
        GUILayout.BeginHorizontal();if(GUILayout.Button(full?"Cận đầu chấm":"Xem toàn ống",GUILayout.Height(28))){full=!full;Frame();}if(GUILayout.Button("Đặt lại lượt",GUILayout.Height(28))){ResetTrial();message="Lượt mới: nguồn kiểm tra 10 µL.";}GUILayout.EndHorizontal();
        GUILayout.Label(message,new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.Label("Click ống để chọn bằng vùng bắt rộng hơn. Miếng trắng chỉ kiểm tra tiếp xúc; giấy sắc ký và mao dẫn thực chưa tích hợp.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.EndArea();
    }
}
