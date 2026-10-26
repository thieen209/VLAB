#if UNITY_EDITOR
using System;
using UnityEngine;
using Vlab.Product;

public static class GuidanceTextChecks
{
    public static void Run()
    {
        Check(LearningGuide.All.Count==32,"All 32 authored study guides are available");
        foreach(var pair in LessonSpec.All)
        {
            var session=new ExperimentSession(pair.Key);var guide=new LessonGuide(session);
            var task=new GuideTask("prepare","","","current","success");
            string before=JsonUtility.ToJson(session.Checkpoint());
            Check(guide.Explanation(task,true)==LearningGuide.Load(pair.Key).practiceVi,"Preserve exact authored practice rationale: "+pair.Key);
            for(int step=0;step<pair.Value.operationsVi.Length;step++)
            {
                string next=guide.Next(null,task,true,step);
                Check(!string.IsNullOrWhiteSpace(next),"Every preparation reference has a conditional next step");
                if(step+1<pair.Value.operationsVi.Length)Check(next.Contains(pair.Value.operationsVi[step+1]),"Next reference is the next authored operation: "+pair.Key);
                else Check(next.Contains("tự bắt đầu"),"Last preparation does not automatically start a trial");
            }
            Check(guide.Next(null,new GuideTask("resume","","","resume",""),true).Contains("Tiếp tục"),"Paused guidance requires deliberate Resume");
            Check(guide.Next(null,new GuideTask("record","","","record","saved outcome"),true).Contains("saved outcome"),"Recording guidance preserves the current success condition");
            Check(guide.Next(null,null,true)==""&&guide.Explanation(null,true)=="","Missing current task does not invent guidance");
            Check(before==JsonUtility.ToJson(session.Checkpoint())&&session.Phase==ExperimentPhase.Preparing&&!session.Complete,"Reading guidance never advances or records a session: "+pair.Key);
            session.SetApparatusReady(true);
            while(session.Operation<pair.Value.operationsVi.Length)
            {
                int operation=session.Operation;string upcoming=guide.Next(null,task,true);
                if(operation+1<pair.Value.operationsVi.Length)Check(upcoming.Contains(pair.Value.operationsVi[operation+1]),"Live preparation follows current Operation: "+pair.Key);
                Check(session.Operation==operation,"Next-step lookup is read-only");
                Check(session.Prepare(operation,out _),"Test fixture prepares explicitly");
            }
            Check(guide.Next(null,new GuideTask("start","","","start",""),true).StartsWith("Sau khi bắt đầu:",StringComparison.Ordinal),"Ready phase describes observation after learner starts");
        }
        Debug.Log("VLAB_GUIDANCE_TEXT_CHECKS_PASS");
    }
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
#endif
