using System.Collections;
using UnityEngine;
using TMPro;
namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        readonly Transform[] handVisuals=new Transform[2];readonly LineRenderer[] handRays=new LineRenderer[2];
        readonly Transform[][] handFingers={new Transform[4],new Transform[4]};Material handMaterial;
        bool dualGameplay;
        void UpdateDualHands(ref ActionFrame primary)
        {
            var dual=Input.Dual;if(!dual)return;
            bool gameplay=!modal&&!changing&&(Route=="activity"||Route=="explore")&&Time.unscaledTime>=inputAfter;
            if(dualGameplay&&!gameplay){Apparatus?.CancelHandGrabs();dual.LeftControllerState.CancelUntilReleased();dual.RightControllerState.CancelUntilReleased();primary.pressed=primary.held=primary.grabPressed=false;primary.released=true;}
            dualGameplay=gameplay;
            for(int hand=1;hand<=2;hand++) {
                var state=dual.Hand(hand);bool fresh=state.Fresh(Time.realtimeSinceStartupAsDouble);
                if(fresh&&!handVisuals[hand-1])CreateHandVisual(hand);
                var visual=handVisuals[hand-1];if(visual)visual.gameObject.SetActive(fresh);
                if(visual&&fresh) {
                    visual.SetPositionAndRotation(state.Pose.position,state.Pose.rotation);
                    for(int f=0;f<4;f++)handFingers[hand-1][f].localRotation=Quaternion.Euler(state.Trigger*65,0,0);
                    var ray=state.Frame.pointer;float distance=Physics.Raycast(ray,out var hit,3)?hit.distance:1.4f;
                    handRays[hand-1].SetPosition(0,ray.origin+ray.direction*.06f);handRays[hand-1].SetPosition(1,ray.GetPoint(distance));
                }
                if(!Apparatus)continue;
                bool holding=Apparatus.HandItem(hand);
                if(!fresh||!gameplay||!state.Armed||state.Frame.lost) {if(holding)Apparatus.EndHandGrab(hand,false);continue;}
                if(holding) {
                    var heldItem=Apparatus.HandItem(hand);
                    if(!state.GripHeld(Profile.settings)){Apparatus.EndHandGrab(hand,true);continue;}
                    Apparatus.UpdateHandGrab(hand,state.Pose,state.Trigger);
                    if(Session.spec.id=="C04"&&heldItem.assetId=="dropper"&&state.Frame.pressed&&state.ActionTrigger>.55f) {
                        var dropper=heldItem.GetComponentInChildren<VlabDropper>();
                        var ray=dropper?new Ray(dropper.Tip.position,dropper.EmissionDirection):state.Frame.pointer;
                        var cup=FindSampleCup(ray);
                        if(cup&&FlagshipOperate("dose:"+cup.assetId)) {
                            if(Session.Phase==ExperimentPhase.Preparing)Apparatus.PrepareNext(out _);if(Session.Phase==ExperimentPhase.Preparing)Apparatus.PrepareNext(out _);if(Session.Phase==ExperimentPhase.Ready)Session.Start(out _);Save();RefreshActivity();
                        }
                    }
                    if(hand==2||!dual.RightControllerState.Fresh(Time.realtimeSinceStartupAsDouble))primary.pressed=false;
                }else if(state.Frame.grabPressed&&!flagshipDrag) {
                    var ray=state.Frame.pointer;
                    if(!PointerHitsUi(ray)&&Physics.Raycast(ray,out var hit,3.5f)) {
                        var item=hit.collider.GetComponentInParent<EquipmentHandle>();
                        if(item&&Apparatus.BeginHandGrab(hand,item,state.Pose,hit.point))Apparatus.UpdateHandGrab(hand,state.Pose,state.Trigger);
                    }
                }
            }
        }
        void CreateHandVisual(int hand)
        {
            if(!handMaterial){handMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default"));handMaterial.color=new Color(.7f,.85f,1);}
            var root=new GameObject(hand==1?"V6 Left hand":"V6 Right hand").transform;root.SetParent(transform,false);handVisuals[hand-1]=root;
            void Part(string name,Vector3 position,Vector3 scale,Transform parent) {
                var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;
                var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=handMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Part("Palm",Vector3.zero,new Vector3(.065f,.023f,.075f),root);
            for(int f=0;f<4;f++) {
                var joint=new GameObject("Finger "+f).transform;joint.SetParent(root,false);joint.localPosition=new Vector3((f-1.5f)*.016f,0,.038f);handFingers[hand-1][f]=joint;
                Part("Finger",new Vector3(0,0,.022f),new Vector3(.012f,.014f,.044f),joint);
            }
            Part("Thumb",new Vector3(hand==1?.04f:-.04f,0,.004f),new Vector3(.035f,.016f,.018f),root);
            var ray=root.gameObject.AddComponent<LineRenderer>();ray.sharedMaterial=handMaterial;ray.positionCount=2;ray.useWorldSpace=true;ray.startWidth=.0025f;ray.endWidth=.001f;ray.startColor=ray.endColor=hand==1?new Color(.2f,.85f,1):new Color(1,.65f,.25f);ray.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;handRays[hand-1]=ray;
        }
        void ShowDualControllers(bool diagnostics=false,int start=0)
        {
            var dual=Input.Dual;var box=Modal(L("Hai tay cầm VLAB V6","VLAB V6 dual controllers"),1100,900);
            Button(box,L("Quét Bluetooth","Scan Bluetooth"),40,100,240,62,()=>{dual.Scan();ShowDualControllers(false);});
            Button(box,L("Cập nhật","Refresh devices"),300,100,240,62,()=>ShowDualControllers(diagnostics));
            Button(box,L("Nối lại đã lưu","Reconnect saved"),560,100,240,62,()=>{dual.ReconnectSaved();ShowDualControllers();});
            Button(box,L("Căn lại hai tay","Recenter hands"),820,100,235,62,()=>dual.Recenter(View.transform.rotation));
            var live=ui.Text(box,"",40,180,1020,diagnostics?540:150,diagnostics?20:23);StartCoroutine(DualControllerStatus(live,diagnostics));
            if(!diagnostics) {
                start=Mathf.Clamp(start,0,Mathf.Max(0,dual.Devices.Count-3));
                for(int i=start;i<Mathf.Min(dual.Devices.Count,start+3);i++) {
                    var device=dual.Devices[i];int hand=device.name=="VLAB-Left"?1:2;
                    Button(box,device.name+" · "+device.address+" · "+device.rssi+" dBm",40,345+(i-start)*72,1015,62,()=>{if(!dual.Connect(hand,device.address))Toast(L("Chưa thể kết nối; kiểm tra quyền và địa chỉ hai tay.","Connection unavailable; check permissions and distinct addresses."));ShowDualControllers();});
                }
                Button(box,L("Thiết bị trước","Previous devices"),40,566,490,54,()=>ShowDualControllers(false,start-3)).interactable=start>0;
                Button(box,L("Thiết bị sau","More devices"),565,566,490,54,()=>ShowDualControllers(false,start+3)).interactable=start+3<dual.Devices.Count;
                ui.Text(box,L("Bóp Grip để cầm. Bấm cần để đổi di chuyển tay; giữ MODE + cần để lên/xuống. Chế độ thường: cần trái đi lại. Nhả hết nút sau khi nối lại.","Hold Grip to grab. Click stick for hand movement; MODE + stick moves up/down. Normal mode: left stick walks. Release all controls after reconnect."),40,640,1015,84,22,VlabUi.Secondary);
            }
            Button(box,L("Hiệu chỉnh tay trái","Calibrate left"),40,740,315,60,()=>ShowDualCalibration(1));
            Button(box,L("Hiệu chỉnh tay phải","Calibrate right"),390,740,315,60,()=>ShowDualCalibration(2));
            Button(box,diagnostics?L("Thiết bị","Devices"):L("Chẩn đoán trực tiếp","Live diagnostics"),740,740,315,60,()=>ShowDualControllers(!diagnostics));
            Button(box,L("BLE cũ / hồ sơ khác","Legacy BLE / other profiles"),40,822,490,58,()=>ShowBleController());
            Button(box,L("Quay lại","Back"),565,822,490,58,()=>{CloseModal();ShowSettings();});
        }
        IEnumerator DualControllerStatus(TextMeshProUGUI label,bool diagnostics)
        {
            var dual=Input.Dual;
            while(label&&label.gameObject.activeInHierarchy) {
                string text=BleControllerRuntime.Plain(dual.ScanStatus)+"\n";
                for(int hand=1;hand<=2;hand++) {
                    var state=dual.Hand(hand);var p=state.Raw;double age=state.ReceivedAt<0?-1:(Time.realtimeSinceStartupAsDouble-state.ReceivedAt)*1000;
                    text+=(hand==1?L("TRÁI","LEFT"):L("PHẢI","RIGHT"))+" · "+BleControllerRuntime.Plain(dual.Status(hand))+" · "+(state.Armed?L("Sẵn sàng","Armed"):L("Thả nút / hiệu chỉnh","Release controls / calibrate"))+"\n";
                    if(diagnostics)text+=$"{state.Address} · seq {p.sequence} · age {age:F0}ms · interval {state.IntervalMilliseconds:F1}ms · accepted/reject/gaps {state.Accepted}/{state.Rejected}/{state.Missing}\n"+
                        $"stick {state.Stick:F2} · trigger {state.Trigger:F3} · buttons 0x{p.buttons:X4} · moveMode {state.MovementMode}\n"+
                        $"ADC X/Y/H {p.rawX}/{p.rawY}/{p.rawHall} · battery estimate {p.batteryMillivolts}mV · flags 0x{p.flags:X2} · cal 0x{p.calibration:X2}\n"+
                        $"raw qXYZW {p.sensorRotation:F3} · orientation {state.OrientationValid}\n"+
                        $"linear m/s² {p.linearAcceleration:F2} · gyro °/s {p.angularVelocity:F2} · gravity m/s² {p.gravity:F2}\n"+
                        $"hand XYZ {state.Pose.position:F2} · hand q {state.Pose.rotation:F2}\n\n";
                }
                label.text=text;yield return new WaitForSecondsRealtime(.15f);
            }
        }
        void ShowDualCalibration(int hand)
        {
            var dual=Input.Dual;var state=dual.Hand(hand);var settings=state.Settings;
            var box=Modal(hand==1?L("Hiệu chỉnh tay trái","Left calibration"):L("Hiệu chỉnh tay phải","Right calibration"),1100,900);
            ui.Text(box,L("Hiệu chỉnh 10 giây: thả cần và cò 2 giây, sau đó xoay cần hết biên và bóp/thả cò hết hành trình trong 8 giây. Xoay BNO055 đủ hướng để hiệu chỉnh cảm biến. Kiểm tra trục và góc lắp trên phần cứng thật.","10-second calibration: center stick and release trigger for 2 seconds, then sweep the full stick rim and squeeze/release the trigger for 8 seconds. Rotate BNO055 through all axes to calibrate fusion. Verify axes and mounting on real hardware."),40,108,1020,130,24);
            Button(box,L("Đo joystick + cò (10s)","Measure stick + trigger (10s)"),40,250,490,62,()=>{if(!dual.SendCommand(hand,2))Toast(L("Cần kết nối tay cầm.","Connect this controller first."));});
            Button(box,L("Căn hướng tay này","Recenter this hand"),565,250,490,62,()=>state.Recenter(View.transform.rotation));
            ui.Text(box,$"Anchor XYZ {settings.anchor:F2} m · Mount XYZ {settings.mountEuler:F0}°",40,332,1020,60,26);
            for(int axis=0;axis<3;axis++) {
                int a=axis;string title=new[]{"X","Y","Z"}[axis];
                Button(box,title+" −5cm",40+axis*346,410,160,62,()=>ChangeDualMotion(hand,s=>s.anchor[a]-=.05f));
                Button(box,title+" +5cm",210+axis*346,410,160,62,()=>ChangeDualMotion(hand,s=>s.anchor[a]+=.05f));
                Button(box,L("Góc lắp ","Mount ")+title+" +90°",40+axis*346,492,330,62,()=>ChangeDualMotion(hand,s=>s.mountEuler[a]=Mathf.Repeat(s.mountEuler[a]+90,360)));
            }
            Button(box,L("Cử chỉ IMU: ","IMU gestures: ")+(settings.gestures?"ON":"OFF"),40,576,490,62,()=>ChangeDualMotion(hand,s=>s.gestures=!s.gestures));
            Button(box,L("Đảo chiều cần X","Invert stick X"),565,576,240,62,()=>ChangeDualMotion(hand,s=>s.invertX=!s.invertX));
            Button(box,L("Đảo chiều cần Y","Invert stick Y"),820,576,235,62,()=>ChangeDualMotion(hand,s=>s.invertY=!s.invertY));
            Button(box,L("LED bật","LED on"),40,660,490,62,()=>dual.SendCommand(hand,3,1));
            Button(box,L("LED tự động","Automatic LED"),565,660,490,62,()=>dual.SendCommand(hand,3,2));
            Button(box,L("Ngắt và quên tay này","Disconnect and forget"),40,742,490,62,()=>{dual.Stop(hand,true);ShowDualControllers();});
            Button(box,L("Mặc định vị trí/góc lắp","Reset anchor/mount"),565,742,490,62,()=>{dual.SaveMotion(hand,new ControllerV6MotionSettings{anchor=new Vector3(hand==1?-.22f:.22f,-.25f,.5f)});ShowDualCalibration(hand);});
            Button(box,L("Quay lại hai tay","Back to controllers"),40,824,1015,56,()=>ShowDualControllers());
        }
        void ChangeDualMotion(int hand,System.Action<ControllerV6MotionSettings> change)
        {
            var settings=JsonUtility.FromJson<ControllerV6MotionSettings>(JsonUtility.ToJson(Input.Dual.Hand(hand).Settings));change(settings);
            if(!Input.Dual.SaveMotion(hand,settings))Toast(L("Giá trị vượt giới hạn an toàn.","Value exceeds the allowed range."));ShowDualCalibration(hand);
        }
    }
}
