#if UNITY_EDITOR
using System;
using UnityEngine;
using Vlab.Product;
public static class WhiteboardChecks
{
    static void Check(bool result,string message){if(!result)throw new Exception(message);}
    public static string Run()
    {
        var drawing=new VlabWhiteboard.Drawing();
        Check(!drawing.Add(new Vector2(float.NaN,0),true),"Reject invalid coordinates");
        Check(drawing.Add(new Vector2(-1,2),true),"Begin stroke");
        Check(drawing.current[0]==new Vector2(0,1),"Clamp coordinates");
        for(int i=1;i<VlabWhiteboard.PointLimit;i++)drawing.Add(new Vector2(i%2,i%3*.5f),false);
        Check(drawing.current.Count==VlabWhiteboard.PointLimit,"Point bound");
        Check(!drawing.Add(Vector2.one,false),"Refuse point overflow");
        for(int i=1;i<VlabWhiteboard.StrokeLimit;i++)drawing.Add(Vector2.zero,true);
        Check(!drawing.Add(Vector2.one,true)&&drawing.strokes.Count==VlabWhiteboard.StrokeLimit,"Stroke bound");
        drawing.Undo();Check(drawing.strokes.Count==VlabWhiteboard.StrokeLimit-1&&drawing.current==null,"Undo last stroke");
        drawing.Clear();drawing.Undo();Check(drawing.strokes.Count==0,"Clear and empty undo");
        return "PASS whiteboard bounded coordinate, point, stroke, undo and clear checks; pointer and visual acceptance require runtime.";
    }
}
#endif
