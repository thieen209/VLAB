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
        Check(!drawing.Add(Vector2.one,false)&&drawing.strokes[drawing.strokes.Count-1].points.Count==1,"A refused new stroke cannot extend the preceding stroke");
        drawing.Undo();Check(drawing.strokes.Count==VlabWhiteboard.StrokeLimit-1&&drawing.current==null,"Undo last stroke");
        drawing.Clear();drawing.Undo();Check(drawing.strokes.Count==0,"Clear and empty undo");
        var note=new NotebookPage();var first=new VlabWhiteboard.Drawing(note.inkStrokes);first.Add(new Vector2(.1f,.5f),true);first.Add(new Vector2(.9f,.5f),false);
        var second=new VlabWhiteboard.Drawing(new NotebookPage().inkStrokes);second.Add(Vector2.one,true);
        Check(note.inkStrokes.Count==1&&note.inkStrokes[0].points.Count==2&&second.strokes.Count==1,"Ink belongs to its own notebook page");
        Check(!first.Erase(new Vector2(float.NaN,.5f),new Vector2(1000,300))&&first.strokes.Count==1,"Invalid eraser coordinates cannot remove ink");
        Check(first.Erase(new Vector2(.5f,.5f),new Vector2(1000,300))&&note.inkStrokes.Count==0,"Eraser hits the middle of a segment");
        Check(first.Undo()&&note.inkStrokes.Count==1&&note.inkStrokes[0].points.Count==2,"Undo restores the last erased stroke");
        Check(first.Undo()&&note.inkStrokes.Count==0&&!first.Undo(),"Undo removes the last drawn stroke and safely stops when empty");
        return "PASS whiteboard bounded coordinate, point, stroke, page isolation, erase, undo and clear checks; pointer and visual acceptance require runtime.";
    }
}
#endif
