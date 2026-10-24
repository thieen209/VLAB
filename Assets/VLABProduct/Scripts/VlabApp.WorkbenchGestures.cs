using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        bool BeginWorkbenchDrag(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if(!Apparatus.Ready||!frame.held||Session.Phase==ExperimentPhase.Paused)return false;
            string id=Session.spec.id,key=item.assetId;
            bool available=id=="C06"&&key=="probe"||id=="P03"&&(key=="meter"||key=="block")||id=="M03"&&(key=="meter"||key=="cord");
            if(!available||!Apparatus.Driver.Controls(key).Any(c=>c.enabled&&(id=="C06"||c.id==(id=="P03"?"pull+":"pull-rope")))||!Apparatus.BeginFlagshipGrab(item))return false;
            CloseWorkbench();flagshipDrag=item;flagshipRest=item.transform.position;flagshipRotation=item.transform.rotation;
            flagshipPlane=new Plane(View.transform.forward,hit.point);flagshipLast=hit.point;flagshipDistance=Mathf.Clamp(hit.distance,.3f,3);
            return true;
        }
        string DirectGestureHint(EquipmentHandle item)
        {
            if(Session.spec.id=="C06"&&item.assetId=="probe")return L("Giữ đầu dò, kéo tới cốc cần đo rồi thả. Nhớ rửa giữa hai dung dịch.","Hold the probe, drag to a cup, then release. Rinse between solutions.");
            if(Session.spec.id=="P03"&&(item.assetId=="meter"||item.assetId=="block"))return L("Giữ và kéo phải để tăng lực. Khi khối trượt, kéo ngược nhẹ để kéo chậm và đều.","Hold and drag right to raise the pull. Once the block slides, pull back slightly for a slow steady pull.");
            if(Session.spec.id=="M03"&&(item.assetId=="meter"||item.assetId=="cord"))return L("Khi lượt đo bắt đầu, giữ đầu dây và kéo xuống để nâng tải.","After starting, hold the free cord and pull down to lift the load.");
            return "";
        }
        bool UpdateWorkbenchDrag(ActionFrame frame)
        {
            string id=Session.spec.id;if(id!="C06"&&id!="P03"&&id!="M03")return false;
            if(id=="C06")
            {
                flagshipDrag.transform.position=frame.pointer.GetPoint(flagshipDistance)+Vector3.up*.04f;
                if(frame.released)
                {
                    var held=flagshipDrag;
                    var target=Physics.RaycastAll(frame.pointer,3.5f).OrderBy(h=>h.distance).Select(h=>h.collider.GetComponentInParent<EquipmentHandle>()).FirstOrDefault(i=>i&&i!=held);
                    string control=target?target.assetId switch{"buffer4"=>"buffer4","buffer7"=>"buffer7","rinse"=>"rinseProbe","test"=>"probeSample","water"=>"probeWater",_=>""}:"";
                    CancelFlagshipDrag();
                    if(control=="")ActionFailure(L("Thả đầu dò trên cốc buffer, nước rửa hoặc mẫu.","Release over a buffer, rinse or sample cup."));
                    else FlagshipOperate(control);
                }
            }
            else
            {
                if(flagshipPlane.Raycast(frame.pointer,out float distance))
                {
                    var point=frame.pointer.GetPoint(distance);float travel=Vector3.Dot(point-flagshipLast,id=="P03"?View.transform.right:-View.transform.up);
                    if(travel>=.01f||id=="P03"&&travel<=-.01f)
                    {
                        string control=id=="P03"?(travel<0?"steady":"pull+"):"pull-rope";
                        if(Apparatus.Driver.Controls(flagshipDrag.assetId).Any(c=>c.id==control&&c.enabled))FlagshipOperate(control);
                        flagshipLast=point;
                    }
                }
                if(frame.released)CancelFlagshipDrag();
            }
            return true;
        }
    }
}
