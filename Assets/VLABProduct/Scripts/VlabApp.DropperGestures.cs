using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        float dropperNextPulse;
        bool BeginDropperGesture(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if(!(Apparatus.Driver is ChemistryDriver chemistry)||(Session?.spec.id!="C04"&&Session?.spec.id!="C01")||item.assetId!=chemistry.ManualDropperKey||!frame.held)return false;
            if(!chemistry.BeginManualDropper(out var why)){ActionFailure(LessonGuide.ExplainFailure(why,Vi));return true;}
            if(!Apparatus.BeginFlagshipGrab(item)){chemistry.StopManualDropper();return true;}
            CloseWorkbench();flagshipDrag=item;flagshipRest=item.transform.position;flagshipRotation=item.transform.rotation;
            var drop=item.GetComponentInChildren<VlabDropper>();var pivot=drop.Tip.position;
            item.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.down)*item.transform.rotation;item.transform.position+=pivot-drop.Tip.position;
            float height=Apparatus.Worktop.y+.16f;
            if(Session.spec.id=="C01")foreach(var receiver in Apparatus.items)if(receiver.assetId=="flask")foreach(var socket in receiver.GetComponentsInChildren<Transform>())if(socket.name.EndsWith("_NeckSocket_SOCKET",System.StringComparison.Ordinal))height=socket.position.y+.04f;
            flagshipPlane=new Plane(Vector3.up,new Vector3(0,height,0));dropperNextPulse=Time.unscaledTime+.35f;return true;
        }
        void AdvanceDropperStages(ChemistryDriver chemistry)
        {
            if(Session.spec.id=="C01")
            {
                if(chemistry.IndicatorDrops==2)
                {
                    bool accepted=Session.Phase==ExperimentPhase.Preparing?Apparatus.PrepareNext(out var error):chemistry.Operate("indicator",out error);
                    if(!accepted)ActionFailure(error);else Guide().Applied("indicator");
                    if(Session.Phase==ExperimentPhase.Ready&&!Session.Start(out error))ActionFailure(error);
                }
                Save();RefreshActivity();return;
            }
            string cup=chemistry.LastDropperCup;
            if(cup!=""&&chemistry.QualitativeDoseUnits(cup)==2)Guide().Applied("dose:"+cup);
            while(Session.Phase==ExperimentPhase.Preparing&&Session.Operation<chemistry.CompletedDropperOperations)
                if(!Apparatus.PrepareNext(out var why)){ActionFailure(why);break;}
            if(Session.Phase==ExperimentPhase.Ready&&!Session.Start(out var failure))ActionFailure(failure);
            Save();RefreshActivity();
        }
        bool UpdateDropperGesture(ActionFrame frame)
        {
            if((Session?.spec.id!="C04"&&Session?.spec.id!="C01")||!flagshipDrag||flagshipDrag.assetId!=(Session.spec.id=="C01"?"indicator":"dropper"))return false;
            var chemistry=Apparatus.Driver as ChemistryDriver;
            if(chemistry==null||frame.released||!frame.held||frame.lost||frame.back||frame.menu||Route!="activity"||modal||!chemistry.CanManualDropper)
            {CancelDropperGesture();Save();RefreshActivity();return true;}
            if(!flagshipPlane.Raycast(frame.pointer,out float distance)||distance>3.5f)return true;
            var above=frame.pointer.GetPoint(distance);var drop=flagshipDrag.GetComponentInChildren<VlabDropper>();var target=chemistry.AssistedDropperTipTarget(above);var lateral=above-drop.Tip.position;lateral.y=0;
            if(lateral.magnitude>.002f&&drop.Tip.position.y<above.y-.001f)target=new Vector3(drop.Tip.position.x,above.y,drop.Tip.position.z);
            else if(lateral.magnitude>.002f)target=above;
            flagshipDrag.transform.position+=Vector3.MoveTowards(drop.Tip.position,target,.4f*Mathf.Min(Time.unscaledDeltaTime,.05f))-drop.Tip.position;
            if(Time.unscaledTime>=dropperNextPulse)
            {
                // This cadence is interaction assistance, never a calibrated drop rate.
                dropperNextPulse=Time.unscaledTime+.35f;
                if(chemistry.TransferManualDropper(out var why)>0)AdvanceDropperStages(chemistry);
                if(liveHint)liveHint.text=Session.spec.id=="C01"?(Vi?"Chỉ thị: "+chemistry.IndicatorDrops+"/2 giọt mô phỏng. Giữ đầu ống trên miệng bình; thả để dừng. Chưa hiệu chuẩn thể tích.":why):(Vi?"Giọt mô phỏng, chưa hiệu chuẩn thể tích. Giữ đầu ống trong nguồn để nạp; đưa đầu ống trên miệng cốc để nhỏ. Thả để dừng.":why);
            }
            return true;
        }
        void CancelDropperGesture()
        {
            if(Session?.spec.id!="C04"&&Session?.spec.id!="C01")return;
            if(Apparatus&&Apparatus.Driver is ChemistryDriver chemistry)chemistry.StopManualDropper();
            if(!flagshipDrag||flagshipDrag.assetId!=(Session.spec.id=="C01"?"indicator":"dropper"))return;
            if(Apparatus&&Apparatus.EndFlagshipGrab(flagshipDrag))
            {
                flagshipDrag.transform.rotation=flagshipRotation;
                if(!Apparatus.TryRestInstalledTool(flagshipDrag,out _))flagshipDrag.transform.SetPositionAndRotation(flagshipRest,flagshipRotation);
            }
            flagshipDrag=null;
        }
    }
}
