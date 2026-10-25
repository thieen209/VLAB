using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        Reading latestPracticeReading;
        RectTransform practiceSummary;TextMeshProUGUI practicePhase,practiceHint,practiceReading;
        float nextPracticeSave;string practiceSignature="",practiceMilestone="";ExperimentSession practiceSession;

        void CapturePracticeProgress()
        {
            if(Session==null||Session.Complete||HasPendingMeasurement||!Apparatus||!Apparatus.Ready)return;
            if(practiceSession!=Session){practiceSession=Session;practiceSignature=practiceMilestone="";latestPracticeReading=null;}
            bool running=Session.Phase==ExperimentPhase.Running||Session.Phase==ExperimentPhase.Paused;
            bool complete=running&&(Apparatus.Driver!=null?Apparatus.Driver.CanRecord(out _):Session.Pendulum!=null&&Session.Pendulum.Cycles>=Session.parameters["cycles"]);
            var progress=Apparatus.Driver?.ProgressObservation();
            string milestone=progress==null?"":string.Join("|",progress.readings.Select(x=>JsonUtility.ToJson(x)))+progress.detailJson;
            // Continuous motion is not a new measurement; explicit readings remain editable until finishing.
            bool completedBefore=Profile.attempts.Any(x=>x.attemptId==Session.TrialId&&!x.partial);
            if(completedBefore&&milestone==practiceMilestone)return;
            ModelResult result=complete?Apparatus.Driver?.RecordableObservation():progress;
            if(complete&&Apparatus.Driver==null)
            {
                var p=Session.Pendulum;double period=p.MeasuredPeriod;
                result=new ModelResult().Add("period",period,"s").Add("timedCycles",p.MeasuredCycles,"").Add("measuredTime",p.MeasuredStop-p.FirstCrossing,"s").Add("gravityEstimate",4*Math.PI*Math.PI*Session.parameters["length"]/(period*period),"m/s²");
            }
            if(result==null||result.readings.Count==0)return;
            string signature=(complete?"complete:":"partial:")+string.Join("|",result.readings.Select(x=>JsonUtility.ToJson(x)))+result.detailJson;
            if(signature==practiceSignature)return;
            var record=Session.Snapshot(result,!complete);
            bool first=!Profile.attempts.Any(x=>x.attemptId==Session.TrialId);
            if(CommitPracticeRecord(record)){practiceSignature=signature;practiceMilestone=milestone;if(first||complete&&!completedBefore)VlabAudio.Play(VlabSound.Capture);}
            else{pendingMeasurement=record;pendingMeasurementSession=Session;pendingMeasurementCheckpoint=Profile.checkpoint;}
        }
        bool CommitPracticeRecord(AttemptRecord record)
        {
            int index=Profile.attempts.FindIndex(x=>x.attemptId==record.attemptId);var old=index>=0?Profile.attempts[index]:null;
            if(index<0)Profile.attempts.Add(record);else Profile.attempts[index]=record;
            if(store.Save(Profile,out var error)){writeProtected=false;var changed=record.readings.Where(r=>old==null||!old.readings.Any(x=>x.key==r.key&&x.value==r.value&&x.unit==r.unit));latestPracticeReading=changed.LastOrDefault(r=>!r.key.StartsWith("time",StringComparison.Ordinal)&&!r.key.StartsWith("temperature",StringComparison.Ordinal))??changed.LastOrDefault()??latestPracticeReading;return true;}
            if(index<0)Profile.attempts.Remove(record);else Profile.attempts[index]=old;
            StorageFailure(error);return false;
        }
        void CreatePracticeSummary(RectTransform parent)
        {
            practiceSummary=ui.Panel(parent,"Practice summary",36,32,440,268);
            var opacity=practiceSummary.GetComponent<VlabPanelOpacity>();opacity.Solid=true;opacity.Apply(false);
            practicePhase=ui.Text(practiceSummary,"",20,18,400,40,22,VlabUi.Secondary);
            practiceHint=ui.Text(practiceSummary,"",20,66,400,122,26);
            practiceReading=ui.Text(practiceSummary,"",20,190,400,70,22);
        }
        void RefreshPracticeSummary()
        {
            if(!practiceSummary||Session==null)return;
            practiceSummary.gameObject.SetActive(compactActivity&&!hudHidden&&!toolCard);
            practicePhase.text=liveStage.text;
            practiceHint.text=Profile.settings.hints||HasPendingMeasurement?ShortInstruction(liveHint.text):ReadinessView.Goal(Session.spec.id,Vi);
            practiceHint.overflowMode=TextOverflowModes.Ellipsis;practiceHint.maxVisibleLines=4;
            var saved=Profile.attempts.FirstOrDefault(x=>x.attemptId==Session.TrialId);
            if(HasPendingMeasurement)practiceReading.text=L("Chưa lưu · thử lưu lại","Not saved · retry saving");
            else if(saved!=null)
            {
                if(!saved.partial){practicePhase.text=L("ĐÃ LƯU SỐ ĐO","MEASUREMENT SAVED");practiceHint.text=L("Số đo của lượt này đã có trong sổ. Chọn Xem kết quả để đọc và giải thích.","This trial is saved in your notebook. Select View results to review and interpret it.");}
                var reading=saved.partial&&latestPracticeReading!=null?latestPracticeReading:ObservationCards.PresentationReadings(saved.readings,saved.experimentId).FirstOrDefault();
                practiceReading.text=reading==null?L("Số đo đã lưu","Reading saved"):ObservationCards.FormatValue(reading,Vi)+" "+ObservationCards.DisplayUnit(reading)+"\n"+ReadingLabel(reading);
            }
            else practiceReading.text=!Apparatus.Ready?Apparatus.items.Count(x=>x.installed)+" / "+Apparatus.items.Count+L(" dụng cụ đã lắp"," instruments fitted"):liveTime.text;
            practiceReading.overflowMode=TextOverflowModes.Ellipsis;practiceReading.maxVisibleLines=2;
        }
        void NextPracticeTool()
        {
            CloseWorkbench();var next=Apparatus.NextInstallableItem();
            if(next)ChooseWorkbenchItem(next,true);else{RefreshActivity();PerformContextAction();}
        }
    }
}
