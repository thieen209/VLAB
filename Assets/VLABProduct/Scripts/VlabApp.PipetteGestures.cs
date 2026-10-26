using System;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        EquipmentHandle pipetteBulbGrab;
        bool BeginPipetteGesture(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if((Session?.spec.id!="C05"&&Session?.spec.id!="C01")||(item.assetId!="pipette"&&item.assetId!="bulb")||!frame.held||!(Apparatus.Driver is ChemistryDriver chemistry)||!chemistry.CanManualPipette)return false;
            var pipette=Apparatus.items.First(i=>i.assetId=="pipette");var bulb=Apparatus.items.First(i=>i.assetId=="bulb");
            if(!Apparatus.BeginFlagshipGrab(pipette))return false;
            if(!Apparatus.BeginFlagshipGrab(bulb)){Apparatus.EndFlagshipGrab(pipette);return false;}
            if(!chemistry.BeginManualPipette(out _)){Apparatus.EndFlagshipGrab(pipette);Apparatus.EndFlagshipGrab(bulb);return false;}
            CloseWorkbench();flagshipDrag=pipette;pipetteBulbGrab=bulb;flagshipRest=pipette.transform.position;flagshipRotation=pipette.transform.rotation;
            var tip=PipetteSocket("_TipOutlet_SOCKET");var top=PipetteSocket("_BulbConnection_SOCKET");var pivot=tip.position;
            pipette.transform.rotation=Quaternion.FromToRotation((top.position-tip.position).normalized,Vector3.up)*pipette.transform.rotation;
            pipette.transform.position+=pivot-tip.position;
            flagshipPlane=new Plane(Vector3.up,new Vector3(0,Apparatus.Worktop.y+.18f,0));
            return true;
        }
        Transform PipetteSocket(string suffix)=>flagshipDrag.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith(suffix,StringComparison.Ordinal));
        bool UpdatePipetteGesture(ActionFrame frame)
        {
            if((Session?.spec.id!="C05"&&Session?.spec.id!="C01")||!flagshipDrag||flagshipDrag.assetId!="pipette")return false;
            if(frame.released||!frame.held||frame.lost||frame.back||frame.menu||Route!="activity"||modal||!pipetteBulbGrab||pipetteBulbGrab.Owner!=EquipmentGrabOwner.Flagship)
            {CancelPipetteGesture();Save();RefreshActivity();return true;}
            var chemistry=Apparatus.Driver as ChemistryDriver;
            if(chemistry==null||!chemistry.CanManualPipette){CancelPipetteGesture();Save();RefreshActivity();return true;}
            if(!flagshipPlane.Raycast(frame.pointer,out float distance)||distance>3.5f)return true;
            var above=frame.pointer.GetPoint(distance);var tip=PipetteSocket("_TipOutlet_SOCKET");
            var target=chemistry.AssistedPipetteTipTarget(above);var lateral=above-tip.position;lateral.y=0;
            // Height assistance raises before crossing a rim, then lowers through
            // the real mouth. Horizontal aim stays entirely with the pointer.
            if(lateral.magnitude>.002f&&tip.position.y<above.y-.001f)target=new Vector3(tip.position.x,above.y,tip.position.z);
            else if(lateral.magnitude>.002f)target=above;
            flagshipDrag.transform.position+=Vector3.MoveTowards(tip.position,target,.4f*Mathf.Min(Time.unscaledDeltaTime,.05f))-tip.position;
            double moved=chemistry.TransferManualPipette(3*Mathf.Min(Time.unscaledDeltaTime,.05f),out var why);
            if(moved>0)
            {
                while(Session.Operation<chemistry.CompletedPipetteOperations)
                    if(!Apparatus.PrepareNext(out var failure)){ActionFailure(failure);break;}
            }
            if(liveHint)liveHint.text=Vi?(chemistry.PipetteNeedsRinse?"Hỗ trợ độ cao: kéo pipette tới chai gốc rồi cốc thải để tráng đủ 10 mL. Thả để đóng van và đặt lại vào kẹp.":(Session.spec.id=="C01"?"Kéo pipette tới chai mẫu rồi bình chuẩn độ để chuyển đủ 10 mL. Thả để đóng van và đặt vào kẹp riêng.":"Hỗ trợ độ cao: kéo pipette tới chai gốc rồi bình định mức để chuyển đủ 10 mL. Thả để đóng van và đặt lại vào kẹp.")):("Height assistance; release parks in the clamp. Illustrative flow: "+why);
            return true;
        }
        void CancelPipetteGesture()
        {
            if((Session?.spec.id!="C05"&&Session?.spec.id!="C01"))return;
            if(Apparatus&&Apparatus.Driver is ChemistryDriver chemistry)chemistry.StopManualPipette();
            if(pipetteBulbGrab&&Apparatus)Apparatus.EndFlagshipGrab(pipetteBulbGrab);pipetteBulbGrab=null;
            if(!flagshipDrag||flagshipDrag.assetId!="pipette")return;
            // The supplied clamp supports the upright loaded pipette. Parking
            // changes no accepted liquid and never moves another owner's tool.
            if(Apparatus&&Apparatus.EndFlagshipGrab(flagshipDrag)&&Apparatus.Driver is ChemistryDriver owner)
                if(!owner.ParkManualPipette(out var why))ActionFailure(why);
            flagshipDrag=null;
        }
    }
}
