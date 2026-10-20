#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Vlab.Product;

public static class PhoneUiChecks
{
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var ui=(VlabUi)typeof(VlabApp).GetField("ui",flags).GetValue(app);var fit=ui.PhoneViewport;
        string profile=JsonUtility.ToJson(app.Profile);bool vr=app.Input.VrMode,sim=app.Input.SimulatorMode;var log=new List<string>();
        Action<string,bool> verify=(name,pass)=>{log.Add((pass?"PASS ":"FAIL ")+name);check(name,pass);if(!pass)throw new InvalidOperationException(name);};
        verify("Phone viewport component exists on the actual shared Canvas",fit&&fit.GetComponent<Canvas>()==ui.canvas);
        if(vr)app.Input.SetVr(false);if(sim)app.Input.SetSimulator(false);app.ShowHome();yield return new WaitForSecondsRealtime(.32f);
        try
        {
            verify("Noto Sans primary body font loaded",ui.font&&ui.font.faceInfo.familyName.Contains("Noto Sans"));
            verify("Vietnamese and equation glyphs have native TMP coverage",ui.font.HasCharacters("Tiếng Việt ăâđêôơư ĐỘNG LỰC Δπ √−≈≤≥ ²³₀₁₂",out uint[] missing,true,false));
            var cases=new[]{new Vector2Int(1920,1080),new Vector2Int(2340,1080),new Vector2Int(1280,720),new Vector2Int(1080,2340)};
            foreach(var size in cases)
            {
                Rect safe=size.x>size.y?new Rect(72,24,size.x-120,size.y-48):new Rect(24,96,size.x-48,size.y-176);
                yield return View(app,ui,size,safe,verify,log);
            }
            fit.ClearViewportOverride();ui.SetWorld(true,app.View);var world=ui.canvas.transform.position;var scale=ui.surface.localScale;
            fit.Refresh();verify("World VR retains full authored logical surface and scale",ui.surface.rect.size==PhoneUiViewport.LogicalSize&&scale==Vector3.one&&ui.surface.localScale==Vector3.one);
            var rotation=app.View.transform.rotation;app.View.transform.rotation=rotation*Quaternion.Euler(0,20,0);fit.Refresh();verify("Phone safe-area refresh does not head-lock the VR menu",Vector3.Distance(world,ui.canvas.transform.position)<.0001f);app.View.transform.rotation=rotation;
            verify("Authored formula formatting supports powers, indices, fractions and roots",UiFormula.RichText(@"T^2=\frac{4\pi^2}{g}L; v_0=\sqrt{2gh}").Contains("<sup>2</sup>")&&UiFormula.RichText("v_0").Contains("<sub>0</sub>")&&UiFormula.RichText(@"\sqrt{2gh}")=="√(2gh)");
        }
        finally
        {
            fit.ClearViewportOverride();ui.SetWorld(false,app.View);if(vr)app.Input.SetVr(true);if(sim)app.Input.SetSimulator(true);app.ShowHome();
            verify("Phone presentation checks preserve every stored learner field",JsonUtility.ToJson(app.Profile)==profile);
            Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/phone-ui.txt",new[]{"Phone safe-area bounds/native raycasts and monoscopic aspect-ratio captures; no44dp/device/headset certification."}.Concat(log));
        }
    }
    static IEnumerator View(VlabApp app,VlabUi ui,Vector2Int size,Rect safe,Action<string,bool> verify,List<string> log)
    {
        var canvas=ui.canvas;var camera=app.View;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        var rt=new RenderTexture(size.x,size.y,24);Texture2D pixels=null;
        try
        {
            rt.Create();camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;ui.PhoneViewport.SetViewportOverride(size,safe);ui.RefreshPhoneLayout();Canvas.ForceUpdateCanvases();yield return null;yield return null;yield return null;
            var corners=new Vector3[4];ui.surface.GetWorldCorners(corners);var projected=corners.Select(point=>(Vector2)camera.WorldToScreenPoint(point)).ToArray();
            verify("Actual1600×1000 surface fits notch-safe pixels "+size,projected.All(p=>p.x>=safe.xMin-1&&p.x<=safe.xMax+1&&p.y>=safe.yMin-1&&p.y<=safe.yMax+1)&&Mathf.Abs(ui.surface.rect.width-1600)<.1f&&Mathf.Abs(ui.surface.rect.height-1000)<.1f);
            foreach(var button in app.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.interactable&&b.name.StartsWith("Subject ")).ToArray())
            {
                var rect=button.GetComponent<RectTransform>();rect.GetWorldCorners(corners);var points=corners.Select(p=>camera.WorldToScreenPoint(p)).ToArray();
                verify("Home target lies inside safe screen "+button.name+" "+size,points.All(p=>p.z>0&&p.x>=safe.xMin-1&&p.x<=safe.xMax+1&&p.y>=safe.yMin-1&&p.y<=safe.yMax+1));
                var centre=camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=centre},hits);
                verify("Actual UGUI ray reaches Home button centre "+button.name+" "+size,hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button);
                foreach(var text in button.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate(true,true);verify("Home Noto title fits "+button.name+" "+size,!text.isTextOverflowing);}
            }
            RenderTexture.active=rt;pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();string name="90-phone-home-"+size.x+"x"+size.y+"-safe.png";Directory.CreateDirectory("Evidence/Runtime");File.WriteAllBytes("Evidence/Runtime/"+name,pixels.EncodeToPNG());log.Add("CAPTURE "+name+"; authored fit only, portrait touch/readability not certified");
        }
        finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;ui.PhoneViewport.ClearViewportOverride();ui.RefreshPhoneLayout();if(pixels)UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
#endif
