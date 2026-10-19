using UnityEngine;

public sealed class VlabBeakerReview : MonoBehaviour
{
    [SerializeField] private VlabBeaker large;
    [SerializeField] private VlabBeaker small;
    private void OnGUI()
    {
        GUI.Label(new Rect(20,20,900,28),"CỐC THỦY TINH · Vạch gần đúng, không dùng định mức");
        GUI.Label(new Rect(20,48,900,28),"Thử thể tích khi đặt thẳng; chưa mô phỏng thành phần hay phản ứng hóa học.");
        GUI.Label(new Rect(20,80,900,28),"Cốc 250 mL: "+large.VolumeMl.ToString("F1")+" mL     Cốc 100 mL: "+small.VolumeMl.ToString("F1")+" mL");
        if(GUI.Button(new Rect(20,112,135,32),"Cốc lớn +25 mL")) large.Add(25);
        if(GUI.Button(new Rect(167,112,135,32),"Cốc nhỏ +25 mL")) small.Add(25);
        if(GUI.Button(new Rect(20,154,135,32),"25 mL: lớn → nhỏ")) large.TransferTo(small,25);
        if(GUI.Button(new Rect(167,154,135,32),"25 mL: nhỏ → lớn")) small.TransferTo(large,25);
        if(GUI.Button(new Rect(20,196,135,32),"Làm rỗng hai cốc")) { large.Remove(large.VolumeMl); small.Remove(small.VolumeMl); }
    }
}
