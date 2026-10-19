using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VlabAssetReviewInput : MonoBehaviour
{
    [SerializeField] private VlabAirBlower blower;
    [SerializeField] private Collider switchCollider;
    [SerializeField] private VlabAirHose hose;
    private Camera viewCamera;
    private InputAction powerAction;
    private InputAction hoseAction;

    private void Awake()
    {
        viewCamera=GetComponent<Camera>();
        powerAction=new InputAction("Power",InputActionType.Button,"<Keyboard>/space");
        hoseAction=new InputAction("Connect hose",InputActionType.Button,"<Keyboard>/h");
        powerAction.performed+=_=>{ if(blower!=null && blower.isActiveAndEnabled) blower.Toggle(); };
        hoseAction.performed+=_=>
        {
            if(hose==null || blower==null || !blower.isActiveAndEnabled) return;
            if(hose.IsConnected) hose.Disconnect();
            else hose.Connect(blower);
        };
    }

    private void OnEnable() { powerAction.Enable(); hoseAction.Enable(); }
    private void OnDisable() { powerAction.Disable(); hoseAction.Disable(); }
    private void OnDestroy() { powerAction.Dispose(); hoseAction.Dispose(); }

    private void Update()
    {
        if (blower == null || !blower.isActiveAndEnabled) return;
        if (Mouse.current?.leftButton.wasPressedThisFrame == true && viewCamera != null)
        {
            Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray,out var hit,3) && hit.collider == switchCollider) blower.Toggle();
        }
    }

    private void OnGUI()
    {
        if (blower == null) return;
        GUI.Label(new Rect(20,20,380,28),"MÁY THỔI KHÍ · " + (blower.IsOn ? "ĐANG BẬT" : "ĐANG TẮT"));
        GUI.Label(new Rect(20,48,520,28),"Nhấn công tắc hoặc phím Space để bật / tắt.");
        if(hose!=null)
        {
            GUI.Label(new Rect(20,76,620,28),"H: gắn / tháo ống · Đầu A " + (hose.IsConnected ? "đã gắn" : "chưa gắn"));
            GUI.Label(new Rect(20,104,620,28),"Đầu B chưa nối ray. " + (hose.HasSupplyAtA ? "Có nguồn khí tại đầu A." : "Chưa có nguồn khí tại đầu A."));
            if(GUI.Button(new Rect(20,140,160,32),hose.IsConnected ? "Tháo ống khí" : "Gắn ống khí"))
            {
                if(hose.IsConnected) hose.Disconnect();
                else hose.Connect(blower);
            }
        }
        if(GUI.Button(new Rect(192,140,160,32),blower.IsOn ? "Tắt máy" : "Bật máy")) blower.Toggle();
    }
}
