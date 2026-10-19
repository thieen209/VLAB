#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public static class NotebookVrChecks
    {
        static Button Find(VlabApp app,string name)=>app.GetComponentsInChildren<Button>().Single(button=>button.isActiveAndEnabled&&button.name==name);
        static IEnumerator Press(VlabApp app,string name)
        {yield return new WaitForSecondsRealtime(.3f);Find(app,name).onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);}
        public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
        {
            check("VR writing review begins with a saved personal page",app.Route=="home"&&app.Profile.notebookPages.Count>0);
            app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.3f);yield return Press(app,"Open journal");yield return Press(app,"Journal mode notes");
            var body=app.GetComponentsInChildren<TMP_InputField>().Single(field=>field.name=="Journal note body");string before=body.text;
            check("Existing note does not select all writing on focus",!body.onFocusSelectAll);
            body.caretPosition=body.text.Length;body.selectionStringAnchorPosition=body.selectionStringFocusPosition=body.text.Length;
            Canvas.ForceUpdateCanvases();var fieldRect=body.GetComponent<RectTransform>();Vector3 fieldCenter=fieldRect.TransformPoint(fieldRect.rect.center);var fieldOrientation=Quaternion.LookRotation(fieldCenter-app.View.transform.position);app.Input.UseMockController=true;
            check("VR field pointer accepts released pointing sample",app.Input.Controller.Submit(new ControllerSignal{sequence=19990,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=fieldOrientation}));yield return null;yield return null;
            check("VR field pointer sends a fresh selection edge",app.Input.Controller.Submit(new ControllerSignal{sequence=19991,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=fieldOrientation,select=true}));yield return null;yield return null;
            app.Input.Controller.Submit(new ControllerSignal{sequence=19992,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.LookRotation(Vector3.up,Vector3.forward)});yield return null;yield return null;yield return new WaitForSecondsRealtime(.35f);
            var keyboard=app.GetComponentInChildren<NotebookKeyboardState>();check("Selecting a VR notebook field opens its real keyboard",keyboard&&keyboard.Target==body);
            check("Keyboard backing prevents underlying notebook writing showing through",keyboard.GetComponent<VlabPanelOpacity>().Solid&&keyboard.GetComponent<Image>().color.a>.999f);
            var key=Find(app,"Notebook key Space");var rect=key.GetComponent<RectTransform>();Vector3 center=rect.TransformPoint(rect.rect.center);
            var orientation=Quaternion.LookRotation(center-app.View.transform.position);app.Input.UseMockController=true;
            check("Keyboard test controller accepts fresh pointing sample",app.Input.Controller.Submit(new ControllerSignal{sequence=20000,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation}));yield return null;yield return null;
            app.Input.Controller.Submit(new ControllerSignal{sequence=20001,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=true});yield return null;yield return null;
            Debug.Log("VLAB notebook VR append diagnostics: beforeChars="+before.Length+" afterChars="+body.text.Length+" anchor="+body.selectionStringAnchorPosition+" focus="+body.selectionStringFocusPosition+" selectAllOnFocus="+body.onFocusSelectAll);
            check("Actual VR pointer ray types exactly one character into the authored page",body.text==before+" ");
            app.Input.Controller.Submit(new ControllerSignal{sequence=20002,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.LookRotation(Vector3.up,Vector3.forward)});yield return null;yield return null;
            capture("45-ban-phim-ghi-chep-VI");yield return Press(app,"Notebook key Done");yield return Press(app,"Journal note save");
            var back=app.GetComponentsInChildren<Button>().Single(button=>button.isActiveAndEnabled&&button.GetComponentInChildren<TextMeshProUGUI>()?.text=="‹ Quay lại");yield return new WaitForSecondsRealtime(.3f);back.onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            check("VR keyboard returns safely to Home after saved writing",app.Route=="home");
        }
    }
}
#endif
