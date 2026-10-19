using UnityEngine;
using UnityEngine.InputSystem;

public class LabInteractor : MonoBehaviour
{
    Camera view;
    LabFixture focus;
    void Awake(){view=GetComponent<Camera>();}
    void Update()
    {
        focus=null;
        if(!view||Mouse.current==null)return;
        var pointer=Mouse.current.position.ReadValue();
        if(pointer.x<490&&pointer.y>Screen.height-125)return;
        if(Physics.Raycast(view.ScreenPointToRay(pointer),out var hit,2.5f))focus=hit.collider.GetComponentInParent<LabFixture>();
        if(focus&&Mouse.current.leftButton.wasPressedThisFrame)focus.Activate();
    }
    void OnGUI(){if(focus)GUI.Box(new Rect(Screen.width/2-180,Screen.height-65,360,38),"Click · "+focus.Hint);}
}

