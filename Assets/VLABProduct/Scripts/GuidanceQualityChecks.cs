#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Vlab.Product;

// Native stock-variant regression. Invoke from the root's serialized play-mode runner.
public static class GuidanceQualityChecks
{
    public static IEnumerator Run(Action<string,bool> check)
    {
        foreach(int length in new[]{80,100,120})
        {
            var root=new GameObject("Guidance stock rod "+length);ApparatusRig rig=null;ExperimentSession session=null;string failure="";
            try{session=new ExperimentSession("M05");session.parameters["rod"]=length;rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(5,.8f,0));Install(rig);}
            catch(Exception error){failure=error.ToString();}
            yield return null;
            if(failure=="")try
            {
                rig.RefreshConfiguration();var task=new LessonGuide(session).Preparation(rig,true,1);var target=rig.items.Single(item=>item.assetId==task.key);
                check("M05 "+length+" mm guide selects the active fitted rod",task.key=="rod"+length&&target.gameObject.activeInHierarchy);
                check("M05 "+length+" mm other rods remain inactive",rig.items.Where(item=>item.assetId.StartsWith("rod",StringComparison.Ordinal)&&item.assetId!=task.key).All(item=>!item.gameObject.activeSelf));
                while(session.Operation<session.spec.operationsEn.Length)if(!rig.PrepareNext(out var reason))throw new InvalidOperationException(reason);
                var guide=new LessonGuide(session);
                check("M05 ready guidance uses the SDK Start trial label in both languages",guide.Current(rig,true).action.Contains("Bắt đầu lượt đo")&&guide.Current(rig,false).action.Contains("Start trial")&&!guide.Current(rig,false).action.Contains("Release"));
                ReadOnly(guide,rig,session,check,"Ready "+length);
                if(!session.Start(out var startFailure))throw new InvalidOperationException(startFailure);
                ReadOnly(guide,rig,session,check,"Running "+length);
            }
            catch(Exception error){failure=error.ToString();}
            check("M05 "+length+" mm native guidance fixture completes: "+failure,failure=="");UnityEngine.Object.Destroy(root);yield return null;
        }
        {
            var root=new GameObject("Guidance stock follower");ApparatusRig rig=null;ExperimentSession session=null;string failure="";
            try{session=new ExperimentSession("M06");rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(5,.8f,0));Install(rig);}
            catch(Exception error){failure=error.ToString();}
            yield return null;
            if(failure=="")try
            {
                rig.RefreshConfiguration();var guide=new LessonGuide(session);var knife=guide.Preparation(rig,true,1);
                check("M06 knife guide selects the active fitted follower",knife.key=="follower"&&rig.items.Single(item=>item.assetId==knife.key).gameObject.activeInHierarchy);
                if(!rig.Driver.Operate("follower-type",out var reason))throw new InvalidOperationException(reason);
                var roller=guide.Preparation(rig,true,1);
                check("M06 roller guide selects the active fitted follower",roller.key=="roller"&&rig.items.Single(item=>item.assetId==roller.key).gameObject.activeInHierarchy&&!rig.items.Single(item=>item.assetId=="follower").gameObject.activeSelf);
                if(!rig.Driver.Operate("follower-type",out reason))throw new InvalidOperationException(reason);
                check("M06 returning to knife updates the guide",guide.Preparation(rig,false,1).key=="follower");
            }
            catch(Exception error){failure=error.ToString();}
            check("M06 native guidance fixture completes: "+failure,failure=="");UnityEngine.Object.Destroy(root);yield return null;
        }
        yield return Variant("P06","elastic",1,2,"bumperA",check);
        yield return Variant("P06","elastic",0,2,"latch",check);
        yield return Variant("C05","final",50,1,"vol50",check);
        yield return Variant("C05","final",100,1,"vol100",check);
        yield return Variant("C05","final",100,2,"vol100",check);
        yield return Variant("M07","micrometer",0,0,"caliper",check);
        yield return Variant("M07","micrometer",0,2,"caliper",check);
        yield return Variant("M07","micrometer",1,0,"micrometer",check);
        yield return Variant("P08","height",.2,1,"ruler",check);
        foreach(string id in LessonSpec.All.Keys)
        {
            string vi=ReadinessView.Goal(id,true),en=ReadinessView.Goal(id,false);
            check(id+" readiness has concise bilingual measurable objectives",!string.IsNullOrWhiteSpace(vi)&&!string.IsNullOrWhiteSpace(en)&&vi.Length<=80&&en.Length<=80);
        }
        check("All 32 preparation sequences have matching bilingual observable success criteria",LessonGuide.ValidateMetadata().Length==0);
        string room=LessonGuide.ExplainFailure(PhysicsMechanicsDriver.ReceivingFailure,true);
        string capacity=LessonGuide.ExplainFailure("Retain enough empty receiver capacity before filtering; no extract has been transferred.",true);
        check("Rejected filter capacity coaches a fresh empty receiver and retains saved measurements",capacity.Contains("bình hứng trống")&&capacity.Contains("chưa chuyển dung dịch")&&capacity.Contains("vẫn được giữ"));
        string outlet=LessonGuide.ExplainFailure("The retained extract would submerge the funnel outlet.",true);
        check("Funnel outlet help explains direction and clearance in Vietnamese",outlet.Contains("hướng xuống")&&outlet.Contains("cao hơn mặt dung dịch")&&outlet.Contains("Lắp lại"));
        string holder=LessonGuide.ExplainFailure("Insulating jaws no longer touch and span the actual electrode faces.",true);
        check("Detached holder help points back to physical installation without revealing measurements",holder.Contains("vị trí lắp")&&holder.Contains("điểm tựa")&&holder.Contains("vẫn được giữ"));
        string beam=LessonGuide.ExplainFailure("Use the delivered configurable beam: span <=0.300 m, h/L<=0.1 and delta/L<=0.01.",true);
        check("M08 physical model limit has actionable Vietnamese help without discarding readings",beam.Contains("giảm tải")&&beam.Contains("giảm nhịp")&&beam.Contains("1%")&&beam.Contains("vẫn được giữ"));
        check("P02 room limit explains an actionable adjustment in Vietnamese without changing input",room.Contains("Đổi vận tốc")&&room.Contains("góc phóng")&&room.Contains("vẫn được giữ"));
        string focus=LessonGuide.ExplainFailure("Focus at actual low objective first",true);
        check("Microscope focus help coaches observation without revealing a magic focus coordinate",focus.Contains("4×")&&focus.Contains("cấu trúc đủ rõ")&&!focus.Contains("6,5")&&!focus.Contains("6.5"));
        check("P03 preparation names declared coefficients instead of an unavailable material selector",LessonSpec.All["P03"].operationsVi[1].Contains("hệ số")&&LessonSpec.All["P03"].operationsEn[1].Contains("coefficients"));
        check("P06 preparation names the supplied track and does not promise photogates",LessonSpec.All["P06"].operationsVi[0].Contains("ray")&&!LessonSpec.All["P06"].operationsVi[0].Contains("cổng")&&!LessonSpec.All["P06"].operationsEn[0].Contains("gates"));
        foreach(string reason in new[]{"Endpoint overshot; retain this rejected titre.","Endpoint overshoot"})
        {
            string text=LessonGuide.ExplainFailure(reason,true);
            check("C01 overshot guidance retains excluded evidence before fresh aliquot",text.Contains("Giữ lượt chuẩn độ")&&text.Contains("Mẫu mới")&&text.Contains("không tính vào ba lượt hợp lệ")&&!text.Contains("Không giữ lượt này"));
        }
    }
    static void ReadOnly(LessonGuide guide,ApparatusRig rig,ExperimentSession session,Action<string,bool> check,string label)
    {
        var phase=session.Phase;var operation=session.Operation;double elapsed=session.Elapsed,dose=session.Dose;var recorded=session.Recorded;
        var parameters=session.parameters.ToDictionary(pair=>pair.Key,pair=>pair.Value);string state=rig.Driver.SaveState();
        var transforms=rig.GetComponentsInChildren<Transform>(true);var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
        var vi=guide.Current(rig,true);var en=guide.Current(rig,false);guide.Preparation(rig,true,0);
        bool unchanged=session.Phase==phase&&session.Operation==operation&&session.Elapsed==elapsed&&session.Dose==dose&&ReferenceEquals(session.Recorded,recorded)&&rig.Driver.SaveState()==state&&session.parameters.Count==parameters.Count&&parameters.All(pair=>session.parameters.TryGetValue(pair.Key,out var value)&&value==pair.Value);
        for(int i=0;i<transforms.Length;i++)unchanged&=transforms[i].localPosition==positions[i]&&transforms[i].localRotation==rotations[i]&&transforms[i].localScale==scales[i];
        check("Requested guidance is read-only: "+label,unchanged&&vi!=null&&en!=null);
    }
    static IEnumerator Variant(string id,string parameter,double value,int operation,string expected,Action<string,bool> check)
    {
        var root=new GameObject("Guidance variant "+id+" "+value);ApparatusRig rig=null;ExperimentSession session=null;string failure="";
        try{session=new ExperimentSession(id);session.parameters[parameter]=value;rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(5,.8f,0));Install(rig);}
        catch(Exception error){failure=error.ToString();}
        yield return null;
        if(failure=="")try
        {
            rig.RefreshConfiguration();var task=new LessonGuide(session).Preparation(rig,true,operation);var target=rig.items.Single(item=>item.assetId==task.key);
            check(id+" "+parameter+"="+value+" step "+operation+" targets active "+expected,task.key==expected&&target.gameObject.activeInHierarchy);
        }
        catch(Exception error){failure=error.ToString();}
        check(id+" "+parameter+"="+value+" native variant fixture completes: "+failure,failure=="");UnityEngine.Object.Destroy(root);yield return null;
    }
    static void Install(ApparatusRig rig)
    {
        foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new InvalidOperationException("Could not install "+item.assetId);}
    }
}
#endif
