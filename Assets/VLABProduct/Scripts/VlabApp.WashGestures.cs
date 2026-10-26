using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        bool BeginWashGesture(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if(Session?.spec.id!="C05"||item.assetId!="wash"||!frame.held||!(Apparatus.Driver is ChemistryDriver chemistry)||!chemistry.CanManualWash)return false;
            if(!Apparatus.BeginFlagshipGrab(item))return false;
            if(!chemistry.BeginManualWash(out _)){Apparatus.EndFlagshipGrab(item);return false;}
            CloseWorkbench();flagshipDrag=item;flagshipRest=item.transform.position;flagshipRotation=item.transform.rotation;
            var wash=item.GetComponentInChildren<VlabWashBottle>();
            // Phone height assistance: hold the real nozzle downward above both
            // delivered necks. X/Z follow the pointer freely, never a receiver snap.
            item.transform.rotation=Quaternion.FromToRotation(wash.OutletDirection,Vector3.down)*item.transform.rotation;
            float outletHeight=Apparatus.Worktop.y+.28f;
            item.transform.position+=Vector3.up*(outletHeight-wash.Outlet.y);
            flagshipPlane=new Plane(Vector3.up,new Vector3(0,outletHeight,0));
            if(liveHint)liveHint.text=L("Hỗ trợ giữ vòi hướng xuống ở độ cao cố định. Giữ và kéo ngang tới cổ bình mở; thả để dừng. Tốc độ dòng chỉ minh họa.","Height assistance holds the nozzle downward. Hold and drag across the open flask neck; release to stop. Flow speed is illustrative.");
            return true;
        }
        bool UpdateWashGesture(ActionFrame frame)
        {
            if(!flagshipDrag||Session?.spec.id!="C05"||flagshipDrag.assetId!="wash")return false;
            if(frame.released||!frame.held||frame.lost||frame.back||frame.menu||Route!="activity"||modal){CancelWashGesture();Save();RefreshActivity();return true;}
            var chemistry=Apparatus.Driver as ChemistryDriver;
            if(chemistry==null||!chemistry.CanManualWash){CancelWashGesture();Save();RefreshActivity();return true;}
            var wash=flagshipDrag.GetComponentInChildren<VlabWashBottle>();
            if(!flagshipPlane.Raycast(frame.pointer,out float distance)||distance>3.5f){wash.StopFlow();return true;}
            flagshipDrag.transform.position+=frame.pointer.GetPoint(distance)-wash.Outlet;
            // Bounded interaction rate, with a slower approach near the authored
            // mark. It is not a calibrated pressure/nozzle flow-rate measurement.
            double moved=chemistry.DispenseManualWater(chemistry.ManualWashRateMlPerSecond*Mathf.Min(Time.unscaledDeltaTime,.05f),out var why);
            if(liveHint)liveHint.text=Vi?(moved>0?"Hỗ trợ độ cao: nước đang vào bình thực; thả để dừng. Gần vạch, dòng minh họa chậm lại.":"Hỗ trợ độ cao: kéo ngang vòi tới cổ bình mở, giữ đường nước thông thoáng; thả để dừng."):("Height assistance: "+why);
            return true;
        }
        void CancelWashGesture()
        {
            if(Session?.spec.id!="C05")return;
            if(Apparatus&&Apparatus.Driver is ChemistryDriver chemistry)chemistry.StopManualWash();
            if(!flagshipDrag||flagshipDrag.assetId!="wash")return;
            // Return this fixture to its known support pose; accepted water remains
            // in the flask. Never restore a pose after another owner acquired it.
            if(Apparatus&&Apparatus.EndFlagshipGrab(flagshipDrag))flagshipDrag.transform.SetPositionAndRotation(flagshipRest,flagshipRotation);
            flagshipDrag=null;
        }
    }
}
