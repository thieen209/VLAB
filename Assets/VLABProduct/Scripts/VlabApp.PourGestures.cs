using System;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        VlabLiquidStream pourStream;
        float nextPourSound;
        Transform PourSpout(EquipmentHandle item)=>item.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith("_PourSpout_SOCKET",StringComparison.Ordinal));
        bool BeginPourGesture(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if(Session?.spec.id!="C08"||!frame.held||!(Apparatus.Driver is ChemistryDriver chemistry)||!chemistry.CanManualPour(item.assetId))return false;
            if(!Apparatus.BeginFlagshipGrab(item))return false;
            if(!chemistry.BeginManualPour(item.assetId,out var why)){Apparatus.EndFlagshipGrab(item);ActionFailure(why);return true;}
            CloseWorkbench();flagshipDrag=item;flagshipRest=item.transform.position;flagshipRotation=item.transform.rotation;
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
            if(Session?.spec.id!="C08"||!flagshipDrag||(flagshipDrag.assetId!="acidCup"&&flagshipDrag.assetId!="baseCup"))return false;
            var chemistry=Apparatus.Driver as ChemistryDriver;
            if(chemistry==null||frame.released||!frame.held||frame.lost||frame.back||frame.menu||Route!="activity"||modal||!chemistry.CanManualPour(flagshipDrag.assetId))
            {CancelPourGesture();Save();RefreshActivity();return true;}
            if(!flagshipPlane.Raycast(frame.pointer,out float distance)||distance>3.5f){HidePourStream();return true;}
            var spout=PourSpout(flagshipDrag);flagshipDrag.transform.position+=frame.pointer.GetPoint(distance)-spout.position;
            double moved=chemistry.TransferManualPour(5*Mathf.Min(Time.unscaledDeltaTime,.05f),out var why);
            ShowAcceptedPour(flagshipDrag,moved);
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
            var spout=PourSpout(source);var target=Apparatus.items.Single(i=>i.assetId=="cal").GetComponentInChildren<VlabCalorimeter>().Vessel;
            var end=new Vector3(spout.position.x,target.transform.position.y+(float)target.SurfaceHeightMeters,spout.position.z);
            if(pourStream&&!pourStream.TrySetEndpoints(spout.position,end,.0015f))pourStream.Hide();
            if(Time.unscaledTime>=nextPourSound){VlabAudio.At(VlabSound.Liquid,spout.position);nextPourSound=Time.unscaledTime+.4f;}
        }
        void HidePourStream(){if(pourStream)pourStream.Hide();}
        void CancelPourGesture()
        {
            HidePourStream();
            if(Session?.spec.id!="C08")return;
            if(Apparatus&&Apparatus.Driver is ChemistryDriver chemistry)chemistry.StopManualPour();
            if(!flagshipDrag||(flagshipDrag.assetId!="acidCup"&&flagshipDrag.assetId!="baseCup"))return;
            if(Apparatus&&Apparatus.EndFlagshipGrab(flagshipDrag))
            {
                flagshipDrag.transform.rotation=flagshipRotation;
                if(!Apparatus.TryRestInstalledTool(flagshipDrag,out _))flagshipDrag.transform.SetPositionAndRotation(flagshipRest,flagshipRotation);
            }
            flagshipDrag=null;
        }
    }
}
