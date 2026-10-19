using UnityEngine;

public sealed class VlabBearingReview : MonoBehaviour
{
    [SerializeField] private VlabBearing bearing;
    [SerializeField] private Transform pin, endA, endB;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private string feedback="";
    private void Awake(){restPosition=pin.position;restRotation=pin.rotation;}
    private void ResetPin(){pin.SetPositionAndRotation(restPosition,restRotation);feedback="";}
    private void OnGUI()
    {
        bool fits=bearing.AcceptsAxle(endA.position,endB.position,.006f);
        GUI.Label(new Rect(20,20,900,28),"Ổ ĐỠ Ø6 mm · Chốt kiểm tra được dựng trong Blender");
        GUI.Label(new Rect(20,48,900,28),fits?"Đúng tâm, đủ chiều dài, có khe hở: có thể quay.":"Không lắp được: lệch tâm hoặc nghiêng vượt khe hở.");
        if(GUI.Button(new Rect(20,86,110,32),"Quay 90°")) feedback=bearing.RotateAxle(pin,endA.position,endB.position,.006f,90)?"Đã quay 90° quanh tâm ổ.":"Đã chặn chuyển động.";
        if(GUI.Button(new Rect(142,86,110,32),"Lệch 1 mm")){ResetPin();pin.position+=Vector3.up*.001f;}
        if(GUI.Button(new Rect(264,86,110,32),"Nghiêng 5°")){ResetPin();pin.RotateAround(bearing.Center,Vector3.up,5);}
        if(GUI.Button(new Rect(386,86,110,32),"Căn lại")) ResetPin();
        GUI.Label(new Rect(20,130,900,28),feedback);
        GUI.Label(new Rect(20,160,900,28),"Chốt này chỉ dùng kiểm lỗ; trục và bảng lắp sẽ ghép ở prompt tương ứng.");
    }
}
