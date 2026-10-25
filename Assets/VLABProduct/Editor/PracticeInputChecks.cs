#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator PracticeRecovery()
    {
        yield return Tap("Station tools");yield return Tap("Dụng cụ");
        while(!Find("Equipment item pendulum"))yield return Tap(Find("Equipment previous")?"Equipment previous":"Equipment next");
        yield return Tap("Equipment item pendulum");yield return Tap("Equipment remove");
        Check("Removing a fitted tool blocks the incomplete setup",!App.Apparatus.Ready&&App.Session.Operation==0);
        yield return Tap("Equipment take selected");yield return Tap("Close equipment controls");
        Check("Close returns held tool and releases owner",!App.Apparatus.IsCarrying);
        yield return Tap("Compact current action");yield return Tap("Equipment place selected");
        Check("Same trial can refit without losing setup",App.Apparatus.Ready&&App.Session.Operation==0);
        yield return Tap("Close equipment controls");
    }
    IEnumerator PracticeTitrate()
    {
        yield return Tap("Compact current action");
        // Test learner uses the visible transferred volume, never writes instrument state.
        while(App.Session.parameters["flow"]<20)yield return Tap("Parameter increase flow");
        yield return Tap("Variables close");yield return Tap("Compact current action");
        float until=Time.realtimeSinceStartup+150;
        while(App.Session.Observe().Get("delivered")<9.96&&Time.realtimeSinceStartup<until&&App.Session.Phase==ExperimentPhase.Running)yield return null;
        if(App.Session.Phase==ExperimentPhase.Paused){rows.Add("Titration paused; resume through UI");yield break;}
        Check("Titration actually delivers fluid",Time.realtimeSinceStartup<until);
        yield return Tap("Instrument control close");yield return Tap("Close equipment controls");
    }
    IEnumerator PracticeImages(string id)
    {
        yield return Tap("Compact current action");
        var biology=(BiologyDriver)App.Apparatus.Driver;var points=new Dictionary<int,Vector2>();var answers=new Dictionary<int,string>();
        var specimen=App.Apparatus.items.Single(x=>x.assetId=="specimen");
        var plasmolysis=specimen.GetComponentInChildren<VlabPlasmolysis>();var atlas=specimen.GetComponentInChildren<VlabMitosisAtlas>();
        // Expected labels are a test fixture; all selections and answers enter through touch UI.
        for(int y=0;y<160;y++)for(int x=0;x<160;x++)
        {
            var uv=new Vector2((x+.5f)/160,(y+.5f)/160);int key=0;string answer="";
            if(id=="B01"&&biology.TryB01SourceIdentity(uv,out _,out int structure)&&structure<=3){key=structure;answer="structure:"+(structure-1);}
            if(id=="B02"&&plasmolysis.TryGetCell(uv,out int cell)){key=cell;answer="cellNormal";}
            if(id=="B08"&&atlas.TryResolve(uv,out int cellId,out string stage,out bool scorable)){key=cellId;answer="stage:"+(scorable?Array.IndexOf(new[]{"interphase","prophase","metaphase","anaphase","telophase","unclear"},stage):5);}
            if(key>0&&!points.ContainsKey(key)){points.Add(key,uv);answers.Add(key,answer);}
        }
        int count=id=="B01"?3:id=="B02"?20:100;Check(id+" source image fixtures found",points.Count>=count);
        foreach(var pair in points.OrderBy(x=>x.Key).Take(count))
        {
            var image=Read<RawImage>("inspectionImage");Rect crop=image.uvRect;Check("Source point inside visible crop",crop.Contains(pair.Value));
            var rect=image.rectTransform;var local=new Vector3(rect.rect.xMin+(pair.Value.x-crop.x)/crop.width*rect.rect.width,rect.rect.yMin+(pair.Value.y-crop.y)/crop.height*rect.rect.height);
            var canvas=image.GetComponentInParent<Canvas>();var screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(local));
            int finger=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=screen});yield return null;yield return new WaitForSecondsRealtime(.06f);
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=screen});yield return new WaitForSecondsRealtime(.15f);
            var selected=JsonUtility.FromJson<BiologyLessonState>(biology.SaveState());Check(id+" actual image picked expected cell/region",id=="B01"?selected.selectedStructure==pair.Key:selected.selectedCell==pair.Key);
            yield return Tap("Instrument control "+answers[pair.Key]);
        }
        yield return Tap("Trở lại trạm");
    }
}
#endif
