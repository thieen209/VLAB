using System;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        VlabLiquidStream pourStream;
        float nextPourSound;
        Vector3 PourPosition(EquipmentHandle item)=>Session?.spec.id=="C07"?item.GetComponentInChildren<VlabReagentBottle>().Liquid.PourOutlet:PourSpout(item).position;
        Transform PourSpout(EquipmentHandle item)=>item.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith("_PourSpout_SOCKET",StringComparison.Ordinal));
        bool BeginPourGesture(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if((Session?.spec.id!="C08"&&Session?.spec.id!="C07")||!frame.held||!(Apparatus.Driver is ChemistryDriver chemistry)||!chemistry.CanManualPour(item.assetId))return false;
            if(!Apparatus.BeginFlagshipGrab(item))return false;
            if(!chemistry.BeginManualPour(item.assetId,out var why)){Apparatus.EndFlagshipGrab(item);ActionFailure(why);return true;}
            CloseWorkbench();flagshipDrag=item;flagshipRest=item.transform.position;flagshipRotation=item.transform.rotation;
            if(Session.spec.id=="C07")
            {
                item.transform.rotation=Quaternion.AngleAxis(100,Vector3.forward)*item.transform.rotation;
                var flask=Apparatus.items.Single(i=>i.assetId=="flask").GetComponentInChildren<VlabFlask>();float gasHeight=flask.Mouth.y+.10f;
                item.transform.position+=Vector3.up*(gasHeight-PourPosition(item).y);flagshipPlane=new Plane(Vector3.up,new Vector3(0,gasHeight,0));return true;
            }
            var spout=PourSpout(item);var liquid=item.GetComponentInChildren<VlabBeaker>();
            var radial=Vector3.ProjectOnPlane(spout.position-liquid.transform.position,Vector3.up).normalized;
            if(radial.sqrMagnitude<.9f){CancelPourGesture();return true;}
            // shortcut: phone height/tilt assistance; use a tracked hand for independent six-axis pouring.
            item.transform.rotation=Quaternion.AngleAxis(80,Vector3.Cross(radial,Vector3.down))*item.transform.rotation;
            var receiver=Apparatus.items.Single(i=>i.assetId=="cal").GetComponentInChildren<VlabCalorimeter>();
            float height=receiver.Vessel.transform.position.y+.22f;
            item.transform.position+=Vector3.up*(height-spout.position.y);
            flagshipPlane=new Plane(Vector3.up,new Vector3(0,height,0));
            return true;
        }
        bool UpdatePourGesture(ActionFrame frame)
        {
            if((Session?.spec.id!="C08"&&Session?.spec.id!="C07")||!flagshipDrag||(Session.spec.id=="C07"?flagshipDrag.assetId!="acid":flagshipDrag.assetId!="acidCup"&&flagshipDrag.assetId!="baseCup"))return false;
            var chemistry=Apparatus.Driver as ChemistryDriver;
            if(chemistry==null||frame.released||!frame.held||frame.lost||frame.back||frame.menu||Route!="activity"||modal||!chemistry.CanManualPour(flagshipDrag.assetId))
            {CancelPourGesture();Save();RefreshActivity();return true;}
            if(!flagshipPlane.Raycast(frame.pointer,out float distance)||distance>3.5f){HidePourStream();return true;}
            flagshipDrag.transform.position+=frame.pointer.GetPoint(distance)-PourPosition(flagshipDrag);
            double moved=chemistry.TransferManualPour(5*Mathf.Min(Time.unscaledDeltaTime,.05f),out var why);
            ShowAcceptedPour(flagshipDrag,moved);
            if(Session.spec.id=="C07"){if(liveHint)liveHint.text=L("Giữ miệng chai trên bình mở để rót; thả để dừng. Độ nghiêng và tốc độ có hỗ trợ.","Hold over the open flask to pour; release to stop. Tilt and flow are assisted.");return true;}
            if(liveHint)liveHint.text=L("Hỗ trợ độ cao và độ nghiêng: kéo miệng rót qua cốc cách nhiệt đang mở. Thả để dừng. Tốc độ dòng chỉ minh họa.","Height and tilt assistance: drag the spout over the open calorimeter. Release to stop. Flow rate is illustrative.");
            return true;
        }
        void ShowAcceptedPour(EquipmentHandle source,double moved)
        {
            if(moved<=0){HidePourStream();return;}
            if(!pourStream)
            {
                var prefab=Resources.Load<GameObject>("Equipment/VLAB_liquid_stream");
                if(prefab)pourStream=Instantiate(prefab,Apparatus.transform).GetComponent<VlabLiquidStream>();
            }
            var spout=PourPosition(source);float surface;
            if(Session.spec.id=="C07"){var flask=Apparatus.items.Single(i=>i.assetId=="flask").GetComponentInChildren<VlabFlask>();surface=flask.CavityCentreAt(flask.SurfaceHeightMeters).y;}
            else{var target=Apparatus.items.Single(i=>i.assetId=="cal").GetComponentInChildren<VlabCalorimeter>().Vessel;surface=target.transform.position.y+(float)target.SurfaceHeightMeters;}
            var end=new Vector3(spout.x,surface,spout.z);
            if(pourStream&&!pourStream.TrySetEndpoints(spout,end,.0015f))pourStream.Hide();
            if(Time.unscaledTime>=nextPourSound){VlabAudio.At(VlabSound.Liquid,spout);nextPourSound=Time.unscaledTime+.4f;}
        }
        void HidePourStream(){if(pourStream)pourStream.Hide();}
        void CancelPourGesture()
        {
            HidePourStream();
            if(Session?.spec.id!="C08"&&Session?.spec.id!="C07")return;
            if(Apparatus&&Apparatus.Driver is ChemistryDriver chemistry)chemistry.StopManualPour();
            if(!flagshipDrag||(Session.spec.id=="C07"?flagshipDrag.assetId!="acid":flagshipDrag.assetId!="acidCup"&&flagshipDrag.assetId!="baseCup"))return;
            if(Apparatus&&Apparatus.EndFlagshipGrab(flagshipDrag))
            {
                flagshipDrag.transform.rotation=flagshipRotation;
                if(!Apparatus.TryRestInstalledTool(flagshipDrag,out _))flagshipDrag.transform.SetPositionAndRotation(flagshipRest,flagshipRotation);
            }
            flagshipDrag=null;
        }
    }
}
