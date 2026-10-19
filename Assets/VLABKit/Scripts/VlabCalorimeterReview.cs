using UnityEngine;

public sealed class VlabCalorimeterReview : MonoBehaviour
{
    [SerializeField] private VlabCalorimeter calorimeter;
    private double cupCapacity=12;
    private bool unequal;
    private Vector2 scroll;
    private string note="Nạp mẫu rồi trộn để quan sát nhiệt độ theo thời gian.";
    void Start(){Load();Frame();}
    void Update(){calorimeter.Advance(Time.deltaTime);Frame();}
    void Load(){calorimeter.LoadTrial(25,.5,unequal?20:25,25,.5,unequal?30:25,cupCapacity,25);}
    void Frame()
    {
        var camera=GetComponent<Camera>();camera.transform.position=new Vector3(.13f,.20f,-.20f);camera.transform.LookAt(new Vector3(0,.067f,0));
        camera.orthographicSize=.093f;
    }
    void OnGUI()
    {
        GUI.skin.label.fontSize=15;GUI.skin.button.fontSize=14;GUI.skin.toggle.fontSize=14;
        GUILayout.BeginArea(new Rect(15,15,370,Mathf.Min(650,Screen.height-30)),GUI.skin.box);
        scroll=GUILayout.BeginScrollView(scroll);
        GUILayout.Label("NHIỆT LƯỢNG KẾ · 100 mL");
        GUILayout.Label($"T = {calorimeter.TemperatureC:F3} °C    t = {calorimeter.ElapsedSeconds:F1} s");
        GUILayout.Label($"Đỉnh T = {calorimeter.PeakTemperatureC:F3} °C");
        GUILayout.Label($"Thể tích {calorimeter.Vessel.VolumeMl:F0} mL · n giới hạn {calorimeter.LimitingMoles:F4} mol");
        GUILayout.Label($"Nhiệt đã tỏa {calorimeter.ReleasedHeatJ:F2} / {calorimeter.ReactionHeatJ:F2} J");
        GUILayout.Label($"Nhiệt ra môi trường {calorimeter.LostHeatJ:F2} J");
        GUILayout.Label($"T đoạn nhiệt dự kiến {calorimeter.AdiabaticTemperatureC:F3} °C");
        GUILayout.Space(8);
        GUILayout.Label("25 mL HCl + 25 mL NaOH, cùng 0,5 M");
        if(GUILayout.Button(unequal?"Nhiệt độ đầu: 20 / 30 °C":"Nhiệt độ đầu: 25 / 25 °C")){unequal=!unequal;Load();}
        GUILayout.Label($"Ccal minh họa = {cupCapacity:F0} J/K (chưa hiệu chuẩn)");
        GUILayout.BeginHorizontal();foreach(double c in new[]{0d,12d,40d})if(GUILayout.Button(c+" J/K")){cupCapacity=c;Load();}GUILayout.EndHorizontal();
        calorimeter.LossEnabled=GUILayout.Toggle(calorimeter.LossEnabled,"Thất thoát nhiệt minh họa");
        GUILayout.Label($"k = {calorimeter.HeatLeakWattsPerK:F2} W/K · môi trường 25 °C");
        if(GUILayout.Button(calorimeter.LidOpen?"Đóng nắp":"Mở nắp"))calorimeter.SetLid(!calorimeter.LidOpen);
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Trộn"))note=calorimeter.StartMix()?"Đang trộn; không cộng nhiệt phản ứng lần hai.":"Mẫu đã trộn. Nạp lại để làm lượt mới.";
        if(GUILayout.Button(calorimeter.Paused?"Tiếp tục":"Tạm dừng"))calorimeter.Paused=!calorimeter.Paused;
        if(GUILayout.Button("Nạp lại")){Load();note="Đã nạp mẫu mới.";}
        GUILayout.EndHorizontal();
        if(GUILayout.Button("Bước +10 s (rồi dừng)")){calorimeter.StartMix();calorimeter.Paused=false;calorimeter.Advance(10);calorimeter.Paused=true;}
        GUILayout.Label(note);
        GUILayout.Space(8);GUILayout.Label("Mô hình minh họa dung dịch loãng: ρ = 1 g/mL; c = 4,18 J/g/K; ΔH = −57 kJ/mol. Thời gian trộn giả định 2 s.");
        GUILayout.Label("T hiển thị là trạng thái mô phỏng. Chưa lắp nhiệt kế, đũa khuấy hoặc mô phỏng sai số cảm biến.");
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
}
