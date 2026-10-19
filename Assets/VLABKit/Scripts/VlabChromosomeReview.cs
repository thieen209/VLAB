using UnityEngine;
public sealed class VlabChromosomeReview : MonoBehaviour
{
    [SerializeField] VlabChromosome model;
    readonly string[] names={"Trung gian","Kỳ đầu","Kỳ giữa","Kỳ sau","Kỳ cuối"};
    public VlabChromosome Model=>model;
    void OnGUI()
    {
        var wrap=new GUIStyle(GUI.skin.label){wordWrap=true};
        GUILayout.BeginArea(new Rect(12,12,340,440),GUI.skin.box);
        GUILayout.Label("NHIỄM SẮC THỂ · SƠ ĐỒ GIẢI THÍCH");
        GUILayout.Label("KHÔNG THEO TỶ LỆ SINH HỌC",wrap);
        for(int i=0;i<names.Length;i++)if(GUILayout.Button(names[i],GUILayout.Height(27)))model.SetStage((VlabChromosome.Stage)i);
        GUILayout.Label("Đang xem: "+names[(int)model.Phase]);
        if(model.Phase==VlabChromosome.Stage.Anaphase)
        {
            GUILayout.Label("Hai NST con; vùng tâm động đi trước về hai cực.",wrap);
            float next=GUILayout.HorizontalSlider(model.Separation,.045f,.075f);if(next!=model.Separation)model.SetStage(model.Phase,next);
            GUILayout.Label("Khoảng tách sơ đồ; không phải thời gian hay tốc độ thật.",wrap);
        }
        else if(model.Phase==VlabChromosome.Stage.Interphase||model.Phase==VlabChromosome.Stage.Telophase)
            GUILayout.Label("Ẩn dạng NST co xoắn. Chất nhiễm sắc ở dạng giãn; không vẽ mọi tế bào thành chữ X. Trung gian không phải một kỳ nguyên phân.",wrap);
        else GUILayout.Label("Một NST đã nhân đôi: hai chromatid chị em còn gắn nhau. Kỳ đầu: sơ đồ dạng đã co xoắn.",wrap);
        GUILayout.Label("Xanh: chromatid/NST con · Vàng: vùng tâm động · Đường mảnh: hướng thoi phân bào qua kinetochore.",wrap);
        GUILayout.Label("Chỉ minh họa một cặp bản sao; không phải bộ NST đầy đủ hay ảnh tiêu bản. Không dùng hình này để đếm tế bào hoặc suy chỉ số phân bào.",wrap);
        GUILayout.EndArea();
    }
}
