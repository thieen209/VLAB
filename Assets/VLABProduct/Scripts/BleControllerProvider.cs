using System;
using UnityEngine;

namespace Vlab.Product
{
    public sealed class BleControllerProvider:IVlabInputProvider
    {
        public ControllerCapabilities Capabilities=>(Profile.useOrientation&&LastPacket.hasOrientation?ControllerCapabilities.Orientation:ControllerCapabilities.None)|ControllerCapabilities.Select|ControllerCapabilities.Grip|ControllerCapabilities.Adjustment|ControllerCapabilities.Menu|ControllerCapabilities.Pointer|ControllerCapabilities.Back;
        public BleControllerProfile Profile{get;private set;}=new BleControllerProfile();
        public BlePacket LastPacket{get;private set;}public double LastReceived{get;private set;}=-1;
        public long Accepted{get;private set;}public long Rejected{get;private set;}
        bool have,sequenceSet,suppressed=true,select,grip,menu,recenter,back,pendingRelease,pendingLoss,activity,triggerDown,gripDown;uint sequence;
        GameSettings bindings=new GameSettings();Quaternion calibration=Quaternion.identity;Vector2 cursor=new Vector2(.5f,.5f);double lastPoll,nextAdjustment;int lastDirection;
        public bool Fresh(double now)=>have&&now>=LastReceived&&now-LastReceived<=Profile.timeout;
        public void Configure(BleControllerProfile profile){profile.Validate();Disconnect();Profile=profile.Copy();calibration=Quaternion.identity;}
        public void ConfigureBindings(GameSettings value){ControllerBindings.Validate(value);if(!ControllerBindings.Equal(bindings,value)){bindings=value.Copy();CancelUntilReleased();}}
        public void CancelUntilReleased(){pendingRelease|=select||grip;select=grip=menu=recenter=back=false;suppressed=true;lastDirection=0;}
        public void Disconnect(){pendingLoss|=have;CancelUntilReleased();have=sequenceSet=activity=triggerDown=gripDown=false;LastReceived=-1;lastPoll=0;}
        public bool ConsumeActivity(){bool value=activity;activity=false;return value;}
        public bool Submit(byte[] data,double now,double queuedMilliseconds)
        {
            if(!double.IsFinite(now)||now<0||!double.IsFinite(queuedMilliseconds)||queuedMilliseconds<0||queuedMilliseconds>Profile.timeout*1000||!Profile.TryDecode(data,out var packet)){Rejected++;return false;}
            if(sequenceSet){uint mask=Profile.sequenceBytes==2?0xffffu:uint.MaxValue;uint delta=unchecked(packet.sequence-sequence)&mask;if(delta==0||delta>mask/2){Rejected++;return false;}}
            bool moved=have&&(Math.Abs(packet.rawX-LastPacket.rawX)>500||Math.Abs(packet.rawY-LastPacket.rawY)>500||packet.hasOrientation&&LastPacket.hasOrientation&&Quaternion.Angle(packet.orientation,LastPacket.orientation)>2);
            bool edge=(packet.buttons&~LastPacket.buttons)!=0||packet.trigger>.55f&&!triggerDown||packet.grip>.55f&&!gripDown;
            activity|=moved||edge;triggerDown=packet.trigger>(triggerDown?.45f:.55f);gripDown=packet.grip>(gripDown?.45f:.55f);
            sequence=packet.sequence;sequenceSet=have=true;LastPacket=packet;LastReceived=now-queuedMilliseconds/1000;Accepted++;
            if(packet.buttons==0&&!triggerDown&&!gripDown)suppressed=false;return true;
        }
        public void Calibrate(Quaternion forward){if(have&&LastPacket.hasOrientation)calibration=forward*Quaternion.Inverse(LastPacket.orientation);}
        public Vector2 Stick
        {
            get{if(Profile.stickXOffset<0)return Vector2.zero;var v=new Vector2(Profile.x.Normalize(LastPacket.rawX),Profile.y.Normalize(LastPacket.rawY));float magnitude=Mathf.Min(1,v.magnitude);return magnitude<=Profile.deadzone?Vector2.zero:v.normalized*((magnitude-Profile.deadzone)/(1-Profile.deadzone));}
        }
        public ActionFrame Poll(Camera camera,double now)=>Poll(camera,now,false);
        public ActionFrame Poll(Camera camera,double now,bool headset)
        {
            bool fresh=Fresh(now);if(!fresh&&have){pendingLoss=true;have=false;CancelUntilReleased();}
            int held=fresh&&!suppressed?LastPacket.buttons|(triggerDown?128:0)|(gripDown?32:0):0;
            bool s=(held&ControllerBindings.Mask(bindings.controllerSelect))!=0,g=(held&ControllerBindings.Mask(bindings.controllerGrip))!=0,m=(held&ControllerBindings.Mask(bindings.controllerMenu))!=0,r=(held&ControllerBindings.Mask(bindings.controllerRecenter))!=0,b=(held&ControllerBindings.Mask(bindings.controllerBack))!=0;
            var frame=new ActionFrame{pointer=new Ray(camera.transform.position,camera.transform.forward),held=s||g,pressed=s&&!select,grabPressed=g&&!grip,menu=m&&!menu,recenter=r&&!recenter,back=b&&!back,released=pendingRelease||!s&&!g&&(select||grip),lost=pendingLoss};
            pendingRelease=pendingLoss=false;
            if(fresh)
            {
                if(Profile.useOrientation&&LastPacket.hasOrientation)frame.pointer=new Ray(camera.transform.position,calibration*LastPacket.orientation*Vector3.forward);
                else if(!headset){double dt=lastPoll<=0?0:Math.Clamp(now-lastPoll,0,.05);cursor+=Stick*(float)(dt*.65);cursor=new Vector2(Mathf.Clamp(cursor.x,.02f,.98f),Mathf.Clamp(cursor.y,.02f,.98f));var area=Screen.safeArea;if(area.width<1)area=new Rect(0,0,Screen.width,Screen.height);frame.pointer=camera.ScreenPointToRay(area.min+Vector2.Scale(area.size,cursor));}
                int direction=headset?(Stick.y>.6f?1:Stick.y<-.6f?-1:0):0;if(direction!=0&&(direction!=lastDirection||now>=nextAdjustment)){frame.adjustment=direction;nextAdjustment=now+(direction!=lastDirection?.35:.15);}lastDirection=direction;
            }
            select=s;grip=g;menu=m;recenter=r;back=b;lastPoll=now;return frame;
        }
    }
}
