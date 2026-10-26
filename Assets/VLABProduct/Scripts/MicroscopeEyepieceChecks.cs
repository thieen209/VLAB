#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Vlab.Product;

public static class MicroscopeEyepieceChecks
{
    // Caller supplies a freshly prepared mounted B01 eyepiece; this runner never bypasses preparation.
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        Run(app,check);var driver=(BiologyDriver)app.Apparatus.Driver;
        var layout=app.GetComponentsInChildren<MicroscopeEyepieceLayout>().Single();
        var sdk=app.Apparatus.items.Single(item=>item.assetId=="microscope").GetComponentInChildren<VlabMicroscope>();
        string before=driver.SaveState();int operation=app.Session.Operation;var phase=app.Session.Phase;
        yield return Click(app,"Microscope toggle controls",check);check("Pointer hides microscope controls",!layout.ControlsVisible);capture("microscope-eyepiece-controls-hidden");
        yield return Click(app,"Microscope toggle controls",check);check("Pointer restores microscope controls",layout.ControlsVisible);
        check("Pointer hide/show preserves lesson state and pause",driver.SaveState()==before&&app.Session.Operation==operation&&app.Session.Phase==phase);
        int originalObjective=sdk.ObjectiveIndex;float originalFocus=sdk.FocusMm;
        yield return Click(app,"Instrument control obj4",check);check("Pointer objective control reaches actual SDK",sdk.ObjectiveIndex==0);
        var image=layout.GetComponentInChildren<InstrumentInspection>().GetComponent<UnityEngine.UI.RawImage>();Rect low=image.uvRect;
        yield return Click(app,"Instrument control obj10",check);check("Pointer higher objective changes actual source field",sdk.ObjectiveIndex==1&&image.uvRect.width<low.width);
        yield return Click(app,"Instrument control focusFine+",check);check("Pointer fine focus reaches actual stage",Mathf.Abs(sdk.FocusMm-originalFocus-.05f)<.0001f);
        yield return Click(app,"Instrument control focusFine-",check);check("Pointer fine focus returns actual stage",Mathf.Abs(sdk.FocusMm-originalFocus)<.0001f);
        if(driver.HasB01Presentation)
        {
            Vector2 point=Vector2.zero;int cell=0,structure=0;Rect uv=image.uvRect;
            for(int y=2;y<18&&cell==0;y++)for(int x=2;x<18&&cell==0;x++)
            {Vector2 local=new Vector2(x/20f,y/20f),candidate=uv.min+Vector2.Scale(local,uv.size);if(InstrumentInspection.InsideEyepiece(local)&&driver.TryB01SourceIdentity(candidate,out int id,out int part)){point=local;cell=id;structure=part;}}
            check("Focused eyepiece contains an identifiable original source region",cell>0);
            int classified=JsonUtility.FromJson<BiologyLessonState>(driver.SaveState()).structures.Count;
            yield return ClickImage(image,point,check);
            var selected=JsonUtility.FromJson<BiologyLessonState>(driver.SaveState());
            check("Native eyepiece pointer preserves original cell and structure identity",selected.selectedCell==cell&&selected.selectedStructure==structure);
            check("Native image selection never classifies or advances the lesson",selected.structures.Count==classified&&app.Session.Operation==operation);
        }
        capture("microscope-eyepiece-controls-visible");
        yield return Click(app,"Instrument control "+(originalObjective==0?"obj4":originalObjective==1?"obj10":"obj40"),check);
        check("Eyepiece checks retain original focus and objective",sdk.ObjectiveIndex==originalObjective&&Mathf.Abs(sdk.FocusMm-originalFocus)<.0001f);
    }
    static IEnumerator Click(VlabApp app,string name,Action<string,bool> check)
    {
        yield return new WaitForSecondsRealtime(.26f);
        var button=app.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name&&b.isActiveAndEnabled);check("Native pointer target enabled: "+name,button.interactable);
        Pointer(button.GetComponent<RectTransform>(),Vector2.one*.5f,check);yield return null;yield return null;
    }
    static IEnumerator ClickImage(UnityEngine.UI.RawImage image,Vector2 normalized,Action<string,bool> check)
    {yield return new WaitForSecondsRealtime(.26f);Pointer(image.rectTransform,normalized,check);yield return null;yield return null;}
    static void Pointer(RectTransform rect,Vector2 normalized,Action<string,bool> check)
    {
        var canvas=rect.GetComponentInParent<Canvas>();Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector2 local=rect.rect.min+Vector2.Scale(normalized,rect.rect.size);
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);var hit=hits.FirstOrDefault();
        bool reaches=hit.gameObject&&(hit.gameObject.transform==rect||hit.gameObject.transform.IsChildOf(rect));check("Native UI raycast reaches "+rect.name,reaches);
        if(!reaches)throw new InvalidOperationException("Eyepiece pointer blocked: "+rect.name);
        ExecuteEvents.Execute(rect.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(rect.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(rect.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    public static void Run(VlabApp app,Action<string,bool> check)
    {
        var layout=app.GetComponentsInChildren<MicroscopeEyepieceLayout>().Single();var driver=(BiologyDriver)app.Apparatus.Driver;
        var selection=layout.GetComponentInChildren<InstrumentInspection>();var image=selection.GetComponent<UnityEngine.UI.RawImage>();
        layout.RefreshNow();string state=driver.SaveState();int operation=app.Session.Operation;var phase=app.Session.Phase;var texture=image.texture;Rect crop=image.uvRect;
        check("Eyepiece uses black surround",layout.GetComponent<UnityEngine.UI.Image>().color==Color.black);
        check("Eyepiece source image has circular shader clipping and pointer bounds",selection.Circular&&image.material.GetFloat("_Circular")==1&&InstrumentInspection.InsideEyepiece(Vector2.one*.5f)&&!InstrumentInspection.InsideEyepiece(new Vector2(.01f,.01f)));
        check("Eyepiece remains geometrically circular",Mathf.Abs(image.rectTransform.rect.width-image.rectTransform.rect.height)<.01f);
        check("Circular field preserves source specimen proportions",Mathf.Abs(image.uvRect.width*image.texture.width-image.uvRect.height*image.texture.height)<.01f);
        var canvas=image.GetComponentInParent<Canvas>();Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector3 corner=image.rectTransform.TransformPoint(new Vector3(image.rectTransform.rect.xMin+2,image.rectTransform.rect.yMin+2));
        selection.OnPointerClick(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,corner)});
        check("Clicking the black corner cannot select or classify a source cell",driver.SaveState()==state);
        foreach(string id in new[]{"obj4","obj10","obj40","focus-","focus+","focusFine-","focusFine+"})
            check("Eyepiece keeps actual microscope control visible: "+id,layout.GetComponentsInChildren<UnityEngine.UI.Button>().Any(button=>button.name=="Instrument control "+id));
        layout.SetControlsVisible(false);
        check("Hide controls leaves specimen and show control available",image.isActiveAndEnabled&&!layout.GetComponentsInChildren<UnityEngine.UI.Button>().Any(button=>button.name.StartsWith("Instrument control ",StringComparison.Ordinal))&&layout.GetComponentsInChildren<UnityEngine.UI.Button>().Any(button=>button.name=="Microscope toggle controls"));
        check("Hide controls retains original source texture and UV",image.texture==texture&&image.uvRect==crop);
        layout.SetControlsVisible(true);
        check("Show controls restores actual microscope controls",layout.GetComponentsInChildren<UnityEngine.UI.Button>().Any(button=>button.name=="Instrument control obj10"));
        check("Rendering and hide/show preserve scientific state, preparation and pause",driver.SaveState()==state&&app.Session.Operation==operation&&app.Session.Phase==phase);
    }
}
#endif
