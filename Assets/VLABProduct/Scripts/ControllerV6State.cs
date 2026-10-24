using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vlab.Product
{
    public struct ControllerV6Packet
    {
        public int hand,buttons,flags,calibration,rawX,rawY,rawHall,batteryMillivolts;
        public ushort sequence;public uint sampledMilliseconds;
        public Vector2 stick;public float trigger;
        public Quaternion sensorRotation;public Vector3 linearAcceleration,angularVelocity,gravity;
        public bool ImuValid=>(flags&1)!=0;
        public bool ControlsCalibrated=>(flags&6)==6;
    }
    public static class ControllerV6Protocol
    {
        public const string Service="f1ab6001-6b4c-4a1e-9c32-564c41420001",Notify="f1ab6002-6b4c-4a1e-9c32-564c41420001",Command="f1ab6003-6b4c-4a1e-9c32-564c41420001";
        public const int Size=56;
        static int U16(byte[] p,int i)=>p[i]|p[i+1]<<8;
        static short I16(byte[] p,int i)=>unchecked((short)U16(p,i));
        static Vector3 Vector(byte[] p,int i,float scale)=>new Vector3(I16(p,i)*scale,I16(p,i+2)*scale,I16(p,i+4)*scale);
        public static bool TryDecode(byte[] p,int hand,out ControllerV6Packet value)
        {
            value=default;
            if(p==null||p.Length!=Size||hand<1||hand>2||p[0]!=86||p[1]!=54||p[2]!=1||p[3]!=hand||(p[6]&~31)!=0||U16(p,54)!=BleControllerProfile.Crc(p,54))return false;
            value.hand=hand;value.flags=p[6];value.calibration=p[7];value.sequence=(ushort)U16(p,4);
            value.sampledMilliseconds=(uint)(p[8]|p[9]<<8|p[10]<<16|p[11]<<24);
            value.buttons=U16(p,12);value.rawX=U16(p,46);value.rawY=U16(p,48);value.rawHall=U16(p,50);value.batteryMillivolts=U16(p,52);
            if((value.buttons&~(256|512|2|32|1024))!=0||value.rawX>4095||value.rawY>4095||value.rawHall>4095||I16(p,14)==short.MinValue||I16(p,16)==short.MinValue)return false;
            value.stick=new Vector2(I16(p,14)/32767f,I16(p,16)/32767f);if(value.stick.sqrMagnitude>1.01f)return false;
            value.trigger=U16(p,18)/65535f;
            value.sensorRotation=new Quaternion(I16(p,20)/16384f,I16(p,22)/16384f,I16(p,24)/16384f,I16(p,26)/16384f);
            float norm=Quaternion.Dot(value.sensorRotation,value.sensorRotation);
            if(value.ImuValid&&(norm<.81f||norm>1.21f))return false;
            value.sensorRotation=norm>=.81f&&norm<=1.21f?Quaternion.Normalize(value.sensorRotation):Quaternion.identity;
            value.linearAcceleration=Vector(p,28,.01f);value.angularVelocity=Vector(p,34,1f/16);value.gravity=Vector(p,40,.01f);
            if(value.ImuValid&&(value.linearAcceleration.magnitude>160||value.gravity.magnitude<7||value.gravity.magnitude>12))return false;
            return true;
        }
        public static byte[] MakeCommand(byte opcode,ushort argument=0)
        {
            if(!((opcode==1||opcode==2)&&argument==0||opcode==3&&argument<=2||opcode==4&&argument>=20&&argument<=100))throw new ArgumentOutOfRangeException(nameof(opcode));
            var p=new byte[]{86,67,1,opcode,(byte)argument,(byte)(argument>>8),0,0};ushort crc=BleControllerProfile.Crc(p,6);p[6]=(byte)crc;p[7]=(byte)(crc>>8);return p;
        }
        public static Quaternion ToUnity(Quaternion q)=>new Quaternion(-q.x,-q.y,q.z,q.w);
        public static Vector3 ToUnity(Vector3 v)=>new Vector3(v.x,v.y,-v.z);
    }
    [Serializable] public sealed class ControllerV6MotionSettings
    {
        public Vector3 anchor=new Vector3(.22f,-.25f,.5f),mountEuler;
        public float moveSpeed=.45f,rotationResponse=35,gestureLimit=.09f;
        public bool gestures=true,invertX,invertY;
        public bool Valid=>float.IsFinite(moveSpeed)&&moveSpeed>=.1f&&moveSpeed<=1&&float.IsFinite(rotationResponse)&&rotationResponse>=10&&rotationResponse<=80&&float.IsFinite(gestureLimit)&&gestureLimit>=0&&gestureLimit<=.15f&&Finite(anchor)&&anchor.magnitude<=1&&anchor.z>=.15f&&Finite(mountEuler);
        static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    }
    public sealed class ControllerV6State
    {
        public int Hand{get;}public string Address{get;internal set;}="";
        public ControllerV6Packet Raw{get;private set;}
        public ControllerV6MotionSettings Settings{get;set;}
        public bool Connected{get;internal set;}public bool Armed{get;private set;}
        public bool OrientationValid{get;private set;}public bool MovementMode{get;private set;}
        public double ReceivedAt{get;private set;}=-1;
        public long Accepted{get;private set;}public long Rejected{get;private set;}public long Missing{get;private set;}
        public float IntervalMilliseconds{get;private set;}
        public Pose Pose{get;private set;}public ActionFrame Frame{get;private set;}
        public float Trigger=>Armed?Raw.trigger:0;
        public Vector2 Stick=>Armed?new Vector2(Raw.stick.x*(Settings.invertX?-1:1),Raw.stick.y*(Settings.invertY?-1:1)):Vector2.zero;
        public bool Fresh(double now)=>Connected&&ReceivedAt>=0&&now>=ReceivedAt&&now-ReceivedAt<=.3;
        bool sequenceSet,triggerDown,wasFresh,select,grip,menu,recenter,back,click,centered,poseInitialized;
        struct ControlTransition {public int hand,held,epoch;public ushort sequence;public double receivedAt;public float trigger;}
        readonly Queue<ControlTransition> controlsQueue=new Queue<ControlTransition>(32);
        int held,controlHeld,inputEpoch;ushort sequence;bool pendingLoss;double sensorReceivedAt=-1,lastTick=-1,stallHandledTick=-2,rearmAfter=-1;
        public bool SelectHeld=>select;
        public float ActionTrigger{get;private set;}
        Quaternion offset=Quaternion.identity;Vector3 assisted,gesture,velocity;
        bool motionSampleSet,motionGameplay;uint motionStamp;double motionArrival=-1;
        public ControllerV6State(int hand)
        {
            if(hand<1||hand>2)throw new ArgumentOutOfRangeException(nameof(hand));Hand=hand;
            Settings=new ControllerV6MotionSettings{anchor=new Vector3(hand==1?-.22f:.22f,-.25f,.5f)};
        }
        public bool Submit(byte[] data,double now,double queuedMilliseconds)
        {
            if(!double.IsFinite(now)||now<0||!double.IsFinite(queuedMilliseconds)||queuedMilliseconds<0||queuedMilliseconds>300||!ControllerV6Protocol.TryDecode(data,Hand,out var packet)){Rejected++;return false;}
            double arrival=now-queuedMilliseconds/1000;
            if(arrival<0||arrival<ReceivedAt){Rejected++;return false;}
            ushort delta=unchecked((ushort)(packet.sequence-sequence));
            if(sequenceSet&&(delta==0||delta>=32768)){Rejected++;return false;}
            bool stalled=lastTick>=0&&now-lastTick>.3&&stallHandledTick!=lastTick;
            if(ReceivedAt>=0&&arrival-ReceivedAt>.3||stalled)LoseContinuity(now);
            if(sequenceSet){Missing+=delta-1;IntervalMilliseconds=(float)((arrival-ReceivedAt)*1000);}
            if(packet.ImuValid&&(!sequenceSet||packet.sampledMilliseconds!=Raw.sampledMilliseconds))sensorReceivedAt=arrival;
            Raw=packet;sequence=packet.sequence;sequenceSet=true;ReceivedAt=arrival;Accepted++;
            if(!packet.ControlsCalibrated){CancelUntilReleased(arrival);return true;}
            triggerDown=packet.trigger>(triggerDown?.45f:.55f);
            if(!Armed&&arrival>rearmAfter&&packet.buttons==0&&packet.trigger<.1f&&packet.stick.sqrMagnitude<.01f)Armed=true;
            int next=Armed?packet.buttons|(triggerDown?128:0):0;
            if(next!=held){
                if(controlsQueue.Count>=32)LoseContinuity(now);
                else controlsQueue.Enqueue(new ControlTransition{hand=Hand,held=next,epoch=inputEpoch,sequence=packet.sequence,receivedAt=arrival,trigger=packet.trigger});
            }
            held=Armed?next:0;ProcessMotionSample(packet,arrival);return true;
        }
        public void CancelUntilReleased(double now=-1)
        {Armed=false;held=controlHeld=0;triggerDown=false;ActionTrigger=0;controlsQueue.Clear();inputEpoch++;rearmAfter=now>=0?now:ReceivedAt;ResetMotion();}
        void LoseContinuity(double now){CancelUntilReleased(now);pendingLoss=true;stallHandledTick=lastTick;}
        void ResetMotion(){velocity=gesture=Vector3.zero;motionSampleSet=false;motionArrival=-1;}
        public void Disconnect()
        {
            pendingLoss|=wasFresh||select||grip;Connected=false;sequenceSet=centered=poseInitialized=false;ReceivedAt=sensorReceivedAt=-1;OrientationValid=false;
            CancelUntilReleased();assisted=Vector3.zero;MovementMode=false;
            lastTick=-1;stallHandledTick=-2;
        }
        Quaternion MeasuredRotation=>ControllerV6Protocol.ToUnity(Raw.sensorRotation)*Quaternion.Euler(Settings.mountEuler);
        public void Recenter(Quaternion view)
        {
            if(!Settings.Valid)return;
            if(Raw.ImuValid){offset=Quaternion.Euler(0,view.eulerAngles.y,0)*Quaternion.Inverse(MeasuredRotation);centered=true;}
            assisted=Vector3.zero;ResetMotion();poseInitialized=false;
            if(motionGameplay&&Raw.ImuValid){motionStamp=Raw.sampledMilliseconds;motionArrival=ReceivedAt;motionSampleSet=true;}
        }
        void ProcessMotionSample(ControllerV6Packet packet,double arrival)
        {
            int cal=packet.calibration;
            if(!motionGameplay||Settings==null||!Settings.Valid||!Settings.gestures||!Armed||!centered||!packet.ImuValid||((cal>>6)&3)<1||((cal>>4)&3)<2||((cal>>2)&3)<2||(cal&3)<1){ResetMotion();return;}
            if(!motionSampleSet){motionStamp=packet.sampledMilliseconds;motionArrival=arrival;motionSampleSet=true;return;}
            uint elapsed=unchecked(packet.sampledMilliseconds-motionStamp);
            if(elapsed==0)return;
            if(elapsed>=0x80000000u||elapsed>100){
                gesture*=Mathf.Exp(-5*(float)Math.Max(0,arrival-motionArrival));velocity=Vector3.zero;motionArrival=arrival;
                if(elapsed<0x80000000u)motionStamp=packet.sampledMilliseconds;return;
            }
            float dt=elapsed/1000f;motionStamp=packet.sampledMilliseconds;motionArrival=arrival;
            Vector3 acceleration=packet.linearAcceleration.magnitude>.8f&&packet.linearAcceleration.magnitude<12?offset*ControllerV6Protocol.ToUnity(packet.sensorRotation)*ControllerV6Protocol.ToUnity(packet.linearAcceleration):Vector3.zero;
            // Integrate the damped gesture per sensor sample; render FPS never advances this state.
            float ev=Mathf.Exp(-12*dt),eg=Mathf.Exp(-5*dt);Vector3 terminal=acceleration/12;
            gesture=Vector3.ClampMagnitude(gesture*eg+terminal*((1-eg)/5)+(velocity-terminal)*((eg-ev)/7),Settings.gestureLimit);
            velocity=Vector3.ClampMagnitude(velocity*ev+terminal*(1-ev),.6f);
        }
        Vector3 DisplayGesture(double now)
        {
            if(motionArrival<0||!Settings.gestures)return Vector3.zero;
            float dt=(float)Math.Clamp(now-motionArrival,0,.3),ev=Mathf.Exp(-12*dt),eg=Mathf.Exp(-5*dt);
            return Vector3.ClampMagnitude(gesture*eg+velocity*((eg-ev)/7),Settings.gestureLimit);
        }
        public void Tick(Camera camera,double now,float deltaTime,GameSettings bindings,bool gameplay)
        {
            if(Settings==null||!Settings.Valid)Settings=new ControllerV6MotionSettings{anchor=new Vector3(Hand==1?-.22f:.22f,-.25f,.5f)};
            if(lastTick>=0&&now-lastTick>.3&&stallHandledTick!=lastTick)LoseContinuity(now);
            lastTick=now;motionGameplay=gameplay;
            bool fresh=Fresh(now);if(!fresh){if(wasFresh)LoseContinuity(now);else CancelUntilReleased(now);OrientationValid=false;centered=false;}
            if(controlsQueue.Count>0&&now-controlsQueue.Peek().receivedAt>.3)LoseContinuity(now);
            if(fresh&&Armed&&!pendingLoss){
                // One ordered transition per Tick, at most300ms old; context changes clear the queue.
                if(controlsQueue.Count>0){var edge=controlsQueue.Dequeue();if(edge.epoch==inputEpoch){controlHeld=edge.held;ActionTrigger=edge.trigger;}}
                else{controlHeld=held;ActionTrigger=Raw.trigger;}
            }else{controlHeld=0;ActionTrigger=0;}
            int controls=fresh?controlHeld:0;
            bool s=(controls&ControllerBindings.Mask(bindings.controllerSelect))!=0,g=(controls&ControllerBindings.Mask(bindings.controllerGrip))!=0,m=(controls&ControllerBindings.Mask(bindings.controllerMenu))!=0,r=(controls&ControllerBindings.Mask(bindings.controllerRecenter))!=0,b=(controls&ControllerBindings.Mask(bindings.controllerBack))!=0;
            bool c=(controls&1024)!=0;if(c&&!click)MovementMode=!MovementMode;
            if(r&&!recenter)Recenter(camera.transform.rotation);
            var yaw=Quaternion.Euler(0,camera.transform.eulerAngles.y,0);float dt=Mathf.Clamp(deltaTime,0,.05f);
            int cal=Raw.calibration;
            OrientationValid=fresh&&Raw.ImuValid&&now-sensorReceivedAt<=.15&&((cal>>6)&3)>=1&&((cal>>4)&3)>=2&&((cal>>2)&3)>=2&&(cal&3)>=1;
            if(OrientationValid&&!centered)Recenter(camera.transform.rotation);
            Quaternion target=OrientationValid?offset*MeasuredRotation:poseInitialized?Pose.rotation:yaw;
            var stick=Stick;
            if(gameplay&&MovementMode) {
                bool vertical=(controls&2)!=0;
                assisted+=new Vector3(stick.x,vertical?stick.y:0,vertical?0:stick.y)*Settings.moveSpeed*dt;
                assisted=new Vector3(Mathf.Clamp(assisted.x,-.55f,.55f),Mathf.Clamp(assisted.y,-.4f,.4f),Mathf.Clamp(assisted.z,-.3f,.8f));
            }
            if(!gameplay||!OrientationValid||!Settings.gestures)ResetMotion();
            // shortcut: bounded acceleration gestures; external tracking is needed for accurate physical XYZ.
            Vector3 local=Settings.anchor+assisted+Quaternion.Inverse(yaw)*DisplayGesture(now);local.z=Mathf.Clamp(local.z,.18f,1.25f);local.y=Mathf.Clamp(local.y,-.7f,.35f);local.x=Mathf.Clamp(local.x,-.85f,.85f);
            Vector3 position=camera.transform.position+yaw*local;
            Quaternion rotation=poseInitialized?Quaternion.Slerp(Pose.rotation,target,1-Mathf.Exp(-Settings.rotationResponse*dt)):target;
            Pose=new Pose(position,rotation);poseInitialized=true;
            Ray ray=OrientationValid?new Ray(position,rotation*Vector3.forward):new Ray(camera.transform.position,camera.transform.forward);
            Frame=new ActionFrame{pointer=ray,pressed=s&&!select,grabPressed=g&&!grip,held=s||g,released=pendingLoss||(!s&&!g)&&(select||grip),menu=m&&!menu,recenter=r&&!recenter,back=b&&!back&&!MovementMode,lost=pendingLoss||wasFresh&&!fresh,move=gameplay&&!MovementMode?stick:Vector2.zero};pendingLoss=false;
            select=s;grip=g;menu=m;recenter=r;back=b;click=c;wasFresh=fresh;
        }
        public bool GripHeld(GameSettings bindings)=>Armed&&Fresh(Time.realtimeSinceStartupAsDouble)&&(controlHeld&ControllerBindings.Mask(bindings.controllerGrip))!=0;
    }
}
