using UnityEngine;

public sealed class VlabChipsReview : MonoBehaviour
{
    [SerializeField] VlabChips chips;
    public VlabChips Chips=>chips;
    bool top;
    void Update()
    {
        var c=GetComponent<Camera>();var focus=new Vector3(-.006f,0,0);
        c.transform.position=focus+(top?new Vector3(0,.04f,-.0001f):new Vector3(.012f,.021f,-.031f));c.transform.LookAt(focus);c.orthographicSize=.012f;
    }
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12,12,340,400),GUI.skin.box);
        GUILayout.Label("HẠT CaCO₃ · CÙNG KHỐI LƯỢNG");
        GUILayout.BeginHorizontal();for(int i=0;i<3;i++)if(GUILayout.Button((1<<i)+" mm",GUILayout.Height(30)))chips.Configure(i,chips.BatchUnits);GUILayout.EndHorizontal();
        GUILayout.Label($"Cỡ lớn nhất: {1<<chips.SizeIndex} mm · {chips.Count} hạt");
        GUILayout.Label($"Khối lượng mẫu: {chips.MassGrams:F6} g");
        GUILayout.Label($"Diện tích hình học: {chips.ExteriorAreaM2*1e6:F3} mm²");
        GUILayout.Label("Cùng một mẫu: 64 hạt nhỏ = 8 vừa = 1 lớn.");
        GUILayout.BeginHorizontal();for(int i=1;i<=3;i++)if(GUILayout.Button(i+" mẫu",GUILayout.Height(27)))chips.Configure(chips.SizeIndex,i);GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();if(GUILayout.Button("Dọn mẫu"))chips.Configure(chips.SizeIndex,0);if(GUILayout.Button("Đặt lại"))chips.Configure(0,1);GUILayout.EndHorizontal();
        if(GUILayout.Button(top?"Góc nghiêng":"Nhìn từ trên",GUILayout.Height(28)))top=!top;
        GUILayout.Label("Khối lượng dùng mật độ thiết kế 2700 kg/m³, chưa hiệu chuẩn mẫu thật. Ba cỡ có diện tích hình học tổng tỉ lệ 4 : 2 : 1 khi cùng khối lượng.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.Label("Diện tích phản ứng cần dữ liệu riêng. Chưa mô phỏng hòa tan, tốc độ phản ứng, lượng khí hoặc thao tác bằng thìa/khay cân.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.EndArea();
    }
}
