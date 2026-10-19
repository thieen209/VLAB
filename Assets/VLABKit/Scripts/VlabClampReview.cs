using UnityEngine;
public sealed class VlabClampReview : MonoBehaviour
{
    [SerializeField] VlabClamp model;
    public VlabClamp Model=>model;
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12,12,330,300),GUI.skin.box);
        GUILayout.Label("KẸP ĐIỀU CHỈNH · HÀM SONG SONG");
        GUILayout.Label($"Khoảng mở giữa hai đệm: {model.OpeningM*1000:F2} mm");
        float value=GUILayout.HorizontalSlider((float)model.OpeningM,0,.05f);if(System.Math.Abs(value-model.OpeningM)>1e-8)model.SetOpening(System.Math.Min(.050,value));
        GUILayout.BeginHorizontal();if(GUILayout.Button("Đóng"))model.SetOpening(0);if(GUILayout.Button("25 mm"))model.SetOpening(.025);if(GUILayout.Button("50 mm"))model.SetOpening(.050);GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();if(GUILayout.Button("−¼ vòng"))model.Turn(-.25);if(GUILayout.Button("+¼ vòng"))model.Turn(.25);GUILayout.EndHorizontal();
        GUILayout.Label("1 vòng = 2 mm theo bước vít thiết kế.");
        GUILayout.Label("Socket giữ dụng cụ luôn ở giữa hai đệm. Trục vít và ray phía sau chừa đường cho ống đứng.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.Label("Chưa mô phỏng lực ép, ma sát, biến dạng hoặc khóa lên giá thật. Độ mở hình học không phải chứng nhận độ chính xác dụng cụ.",new GUIStyle(GUI.skin.label){wordWrap=true});
        GUILayout.EndArea();
    }
}
