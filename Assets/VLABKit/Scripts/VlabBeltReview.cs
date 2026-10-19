using UnityEngine;

public sealed class VlabBeltReview : MonoBehaviour
{
    [SerializeField]private VlabBelt belt;
    private string feedback="";
    private void OnGUI()
    {
        GUI.Label(new Rect(20,20,980,26),"ĐAI MỞ / CHÉO · Rộng 6 mm · D vòng chia 40 / 80 mm");
        GUI.Label(new Rect(20,48,980,26),"L = "+(belt.Current.length*1000).ToString("F1")+" mm · Tâm = "+(belt.Current.centerDistance*1000).ToString("F0")+" mm · Mỗi cấu hình là một đai riêng.");
        for(int i=0;i<belt.Configurations.Length;i++)
            if(GUI.Button(new Rect(20+i*126,84,116,30),belt.Configurations[i].name)){belt.SelectConfiguration(i);feedback="";}
        if(GUI.Button(new Rect(20,126,116,30),belt.Running?"Tạm dừng":"Chạy 60 rpm")){if(belt.InputRpm==0)belt.ConfigureDrive(60,belt.Slip,belt.DriveLarge);belt.Running=!belt.Running;}
        if(GUI.Button(new Rect(146,126,116,30),"Đảo chiều"))belt.ConfigureDrive(-belt.InputRpm,belt.Slip,belt.DriveLarge);
        if(GUI.Button(new Rect(272,126,116,30),"Trượt 0 / 5%"))belt.ConfigureDrive(belt.InputRpm,belt.Slip==0?.05:0,belt.DriveLarge);
        if(GUI.Button(new Rect(398,126,116,30),"Đổi bánh dẫn"))belt.ConfigureDrive(belt.InputRpm,belt.Slip,!belt.DriveLarge);
        if(GUI.Button(new Rect(20,168,116,30),"Reset"))belt.ResetDrive();
        if(GUI.Button(new Rect(146,168,180,30),"Thử đai ngắn hơn 10 mm"))feedback=belt.Fits(belt.Current.centerDistance,.04,.08,belt.Current.length-.01)?"Lỗi nhận đai":"Đã từ chối đai ngắn; không kéo giãn model.";
        GUI.Label(new Rect(20,212,980,26),"Dẫn "+(belt.DriveLarge?"80":"40")+" mm: "+belt.InputRpm.ToString("F1")+" rpm → "+belt.OutputRpm.ToString("F1")+" rpm · Trượt đầu ra giả định "+(belt.Slip*100).ToString("F0")+"%.");
        GUI.Label(new Rect(20,240,980,26),feedback);
        GUI.Label(new Rect(20,270,980,26),"Vạch vàng theo vòng đai · Trượt là tham số minh họa · Chưa suy mômen / lực căng.");
    }
}
