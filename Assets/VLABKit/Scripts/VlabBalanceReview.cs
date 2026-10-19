using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VlabBalanceReview : MonoBehaviour
{
    [SerializeField] private VlabBalance balance;
    [SerializeField] private VlabMassSample sample;
    [SerializeField] private Transform panCenter;
    [SerializeField] private Collider tareCollider;
    private Camera viewCamera;
    private bool placed;
    private void Awake() { viewCamera=GetComponent<Camera>(); sample.gameObject.SetActive(false); }

    public void PlaceSample()
    {
        sample.gameObject.SetActive(true);
        sample.transform.position=panCenter.position+panCenter.up*.008f;
        sample.SetMass(.0395);
        placed=true;
        Physics.SyncTransforms(); balance.RefreshReading();
    }

    public void RemoveSample() { sample.gameObject.SetActive(false); placed=false; balance.RefreshReading(); }

    private void Update()
    {
        if(Mouse.current?.leftButton.wasPressedThisFrame==true &&
           Physics.Raycast(viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue()),out var hit,3) && hit.collider==tareCollider)
            balance.Tare();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20,20,620,28),"CÂN ĐIỆN TỬ · "+balance.DisplayText);
        GUI.Label(new Rect(20,48,650,28),"Mẫu kiểm thử mô phỏng: 0,0395 g · chưa phải dữ liệu thí nghiệm.");
        if(GUI.Button(new Rect(20,84,165,32),placed?"Lấy mẫu khỏi đĩa":"Đặt mẫu lên đĩa")) { if(placed) RemoveSample(); else PlaceSample(); }
        if(GUI.Button(new Rect(197,84,120,32),"TARE")) balance.Tare();
        if(GUI.Button(new Rect(329,84,120,32),"Xóa tare")) balance.ClearTare();
        if(GUI.Button(new Rect(20,126,165,32),"Độ đọc 0,001 g")) balance.SetResolution(.001);
        if(GUI.Button(new Rect(197,126,165,32),"Độ đọc 0,01 g")) balance.SetResolution(.01);
    }
}
