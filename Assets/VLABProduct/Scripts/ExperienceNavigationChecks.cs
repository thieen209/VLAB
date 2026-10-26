#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Vlab.Product;

public static class ExperienceNavigationChecks
{
    public static void Run(Action<string,bool> check)
    {
        check("Exit control rejects distant learner",!VlabApp.WithinExitReach(Vector3.zero,new Vector3(0,0,1.21f)));
        check("Exit control accepts learner within 1.2 metres",VlabApp.WithinExitReach(Vector3.zero,new Vector3(0,0,1.19f)));
        var go=new GameObject("Exit tap regression");var tap=go.AddComponent<VlabDoorTap>();
        try
        {
            var data=new PointerEventData(EventSystem.current){pointerId=13,position=new Vector2(100,100)};
            check("Exit requires a completed fresh tap",!tap.Consume());tap.OnPointerDown(data);check("Exit touch down alone cannot activate",!tap.Consume());
            data.position+=Vector2.right*30;tap.OnDrag(data);data.position=new Vector2(100,100);tap.OnPointerUp(data);check("Swipe returning to door button is not a tap",!tap.Consume());
            tap.OnPointerDown(data);tap.OnPointerUp(data);check("Completed door tap activates once",tap.Consume()&&!tap.Consume());
            tap.OnPointerDown(data);go.SetActive(false);go.SetActive(true);tap.OnPointerUp(data);check("Hidden exit control cannot retain a pressed finger",!tap.Consume());
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
        var app=VlabApp.Instance;if(!app)return;
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var fixtureGo=new GameObject("Raw exit regression");var fixture=fixtureGo.AddComponent<LabFixture>();fixture.purpose=LabFixture.Purpose.Exit;
        try
        {
            bool before=app.HasOpenModal;typeof(VlabApp).GetMethod("ActivateFixture",flags).Invoke(app,new object[]{fixture});
            check("Raw door fixture cannot open an exit modal",app.HasOpenModal==before);
        }
        finally{UnityEngine.Object.DestroyImmediate(fixtureGo);}
    }
}
#endif
