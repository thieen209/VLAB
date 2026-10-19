using System;
using UnityEngine;

public sealed class VlabBeamReview : MonoBehaviour
{
    [SerializeField] private VlabBeam beam;
    private string feedback="";
    private void Set(double span,double width,double height,double load)
    {
        try { beam.Configure(span,width,height,beam.YoungModulus,load); feedback=""; }
        catch(InvalidOperationException) { feedback="Đã chặn: ngoài phạm vi dầm mảnh / độ võng nhỏ (δ/L ≤ 1%)."; }
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(20,20,950,28),"DẦM HAI GỐI · Tải giữa · E = 2 GPa (cấu hình minh họa)");
        GUI.Label(new Rect(20,48,950,28),"F = "+beam.Load.ToString("F1")+" N · L = "+(beam.Span*1000).ToString("F0")+" mm · b × h = "+(beam.Width*1000).ToString("F0")+" × "+(beam.Height*1000).ToString("F0")+" mm");
        GUI.Label(new Rect(20,76,950,28),"δ = "+(beam.MidDeflection*1000).ToString("F4")+" mm · Không suy tải phá hủy từ E.");
        if(GUI.Button(new Rect(20,112,120,32),"Thêm 0,5 N")) Set(beam.Span,beam.Width,beam.Height,beam.Load+.5);
        if(GUI.Button(new Rect(152,112,120,32),"Tháo tải")) Set(beam.Span,beam.Width,beam.Height,0);
        if(GUI.Button(new Rect(284,112,120,32),"Dày 5 / 10 mm")) Set(beam.Span,.02,beam.Height==.005?.01:.005,beam.Load);
        if(GUI.Button(new Rect(20,154,120,32),"L = 200 / 300")) Set(beam.Span==.3?.2:.3,beam.Width,beam.Height,beam.Load);
        if(GUI.Button(new Rect(152,154,120,32),"Xoay tiết diện")) Set(beam.Span,beam.Height,beam.Width,beam.Load);
        GUI.Label(new Rect(20,198,950,28),feedback);
        GUI.Label(new Rect(20,230,950,28),"Dấu A/B là vị trí gối; bộ gối, quả tải và đồng hồ so sẽ ghép ở prompt tương ứng.");
        var left=beam.Frame.TransformPoint(new Vector3((float)((.3-beam.Span)/2),0,0));
        var right=beam.Frame.TransformPoint(new Vector3((float)((.3+beam.Span)/2),0,0));
        Mark(left,"▲ A"); Mark(right,"▲ B"); Mark(beam.Gauge.position,"δ");
    }
    private static void Mark(Vector3 world,string text)
    {
        var p=Camera.main.WorldToScreenPoint(world);
        if(p.z>0) GUI.Label(new Rect(p.x-8,Screen.height-p.y+4,90,24),text);
    }
}
