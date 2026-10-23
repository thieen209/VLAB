using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Vlab.Product
{
    [Serializable] public sealed class BleAxis
    {
        public float min=-32767,centre,max=32767;
        public void Validate(){if(!float.IsFinite(min)||!float.IsFinite(centre)||!float.IsFinite(max)||min<-32768||max>32767||centre-min<16||max-centre<16)throw new InvalidDataException("Invalid joystick calibration.");}
        public float Normalize(float raw)=>Mathf.Clamp(raw>=centre?(raw-centre)/(max-centre):(raw-centre)/(centre-min),-1,1);
    }
    [Serializable] public sealed class BleControllerProfile
    {
        public string name="VLAB",service="",characteristic="",address="",prefixHex="";
        public bool littleEndian=true,crc16,useOrientation,autoReconnect=true;
        public int packetLength,sequenceOffset=-1,sequenceBytes=4,buttonsOffset=-1,buttonsBytes=2,handOffset=-1,handValue;
        public int stickXOffset=-1,stickYOffset=-1,triggerOffset=-1,gripOffset=-1;
        public int qXOffset=-1,qYOffset=-1,qZOffset=-1,qWOffset=-1,quaternionEncoding,axisX=1,axisY=2,axisZ=3;
        public float quaternionScale=1,deadzone=.15f,timeout=.35f,triggerMax=32767,gripMax=32767;
        public int[] buttonBits={0,1,2,3,4,5,6,7,8,9,10,11};
        public BleAxis x=new BleAxis(),y=new BleAxis();
        public bool Configured=>packetLength>0;
        public BleControllerProfile Copy()=>JsonUtility.FromJson<BleControllerProfile>(JsonUtility.ToJson(this));
        public static BleControllerProfile Proposal()
        {return new BleControllerProfile{name="VLAB input v1 (proposal)",packetLength=36,prefixHex="564C01",crc16=true,handOffset=3,handValue=1,sequenceOffset=4,buttonsOffset=8,stickXOffset=10,stickYOffset=12,triggerOffset=14,gripOffset=16,qXOffset=18,qYOffset=22,qZOffset=26,qWOffset=30};}
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(name)||name.Length>48||service==null||characteristic==null||address==null||prefixHex==null||prefixHex.Length>32||prefixHex.Length%2!=0)throw new InvalidDataException("Invalid BLE profile identity.");
            if(service!=""&&!Guid.TryParseExact(service,"D",out _)||characteristic!=""&&!Guid.TryParseExact(characteristic,"D",out _))throw new InvalidDataException("Use full 128-bit service/characteristic UUIDs.");
            if(address!=""&&!System.Text.RegularExpressions.Regex.IsMatch(address,"^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$"))throw new InvalidDataException("Invalid BLE address.");
            if(!float.IsFinite(timeout)||timeout<.15f||timeout>2||!float.IsFinite(deadzone)||deadzone<.05f||deadzone>.5f||x==null||y==null)throw new InvalidDataException("Invalid BLE timing/deadzone.");x.Validate();y.Validate();
            if(buttonBits==null||buttonBits.Length!=12)throw new InvalidDataException("Expected12 normalized button slots.");var used=new HashSet<int>();foreach(int bit in buttonBits)if(bit<-1||bit>31||bit>=0&&!used.Add(bit))throw new InvalidDataException("Invalid or duplicate button bit.");
            if(!float.IsFinite(triggerMax)||triggerMax<32||triggerMax>32767||!float.IsFinite(gripMax)||gripMax<32||gripMax>32767||!float.IsFinite(quaternionScale)||quaternionScale<=0||quaternionScale>32768)throw new InvalidDataException("Invalid analog scale.");
            if(axisX < -3||axisX > 3||axisY < -3||axisY > 3||axisZ < -3||axisZ > 3)throw new InvalidDataException("Invalid coordinate axis.");
            if(Math.Abs(axisX)<1||Math.Abs(axisX)>3||Math.Abs(axisY)<1||Math.Abs(axisY)>3||Math.Abs(axisZ)<1||Math.Abs(axisZ)>3||Math.Abs(axisX)==Math.Abs(axisY)||Math.Abs(axisX)==Math.Abs(axisZ)||Math.Abs(axisY)==Math.Abs(axisZ))throw new InvalidDataException("Coordinate axes must be a signed permutation of1,2,3.");
            if(packetLength==0){if(useOrientation)throw new InvalidDataException("Configure the packet before using orientation.");return;}
            if(packetLength<4||packetLength>128||sequenceOffset<0||(sequenceBytes!=2&&sequenceBytes!=4)||(buttonsBytes!=2&&buttonsBytes!=4)||handValue<0||handValue>255||quaternionEncoding<0||quaternionEncoding>1)throw new InvalidDataException("Invalid packet layout.");
            var occupied=new bool[packetLength];Action<int,int> field=(offset,bytes)=>{if(offset<-1)throw new InvalidDataException("Use -1 for an absent field.");if(offset<0)return;if(offset>packetLength-bytes)throw new InvalidDataException("Packet field exceeds length.");for(int i=offset;i<offset+bytes;i++){if(occupied[i])throw new InvalidDataException("Packet fields overlap.");occupied[i]=true;}};
            field(0,prefixHex.Length/2);for(int i=0;i<prefixHex.Length;i+=2)Convert.ToByte(prefixHex.Substring(i,2),16);
            if(crc16)field(packetLength-2,2);field(sequenceOffset,sequenceBytes);field(buttonsOffset,buttonsBytes);field(handOffset,1);
            if((stickXOffset<0)!=(stickYOffset<0))throw new InvalidDataException("Configure both joystick axes.");
            foreach(int offset in new[]{stickXOffset,stickYOffset,triggerOffset,gripOffset})field(offset,2);
            int count=0;foreach(int offset in new[]{qXOffset,qYOffset,qZOffset,qWOffset}){if(offset>=0)count++;field(offset,quaternionEncoding==0?4:2);}
            if(count!=0&&count!=4||useOrientation&&count!=4)throw new InvalidDataException("Configure all four quaternion components.");
        }
        static uint Word(byte[] data,int offset,int size,bool little)
        {uint result=0;for(int i=0;i<size;i++)result|=(uint)data[offset+i]<<((little?i:size-1-i)*8);return result;}
        public static ushort Crc(byte[] data,int count)
        {ushort crc=0xffff;for(int i=0;i<count;i++){crc^=(ushort)(data[i]<<8);for(int b=0;b<8;b++)crc=(ushort)((crc&0x8000)!=0?(crc<<1)^0x1021:crc<<1);}return crc;}
        float Component(byte[] data,int offset)=>quaternionEncoding==0?BitConverter.Int32BitsToSingle(unchecked((int)Word(data,offset,4,littleEndian)))*quaternionScale:unchecked((short)Word(data,offset,2,littleEndian))*quaternionScale;
        static float Axis(Vector3 v,int axis)=>v[Math.Abs(axis)-1]*Math.Sign(axis);
        public bool TryDecode(byte[] data,out BlePacket packet)
        {
            packet=default;if(!Configured||data==null||data.Length!=packetLength)return false;
            for(int i=0;i<prefixHex.Length/2;i++)if(data[i]!=Convert.ToByte(prefixHex.Substring(i*2,2),16))return false;
            if(crc16&&Word(data,data.Length-2,2,littleEndian)!=Crc(data,data.Length-2)||handOffset>=0&&data[handOffset]!=handValue)return false;
            packet.sequence=Word(data,sequenceOffset,sequenceBytes,littleEndian);uint raw=buttonsOffset<0?0:Word(data,buttonsOffset,buttonsBytes,littleEndian);
            for(int i=0;i<12;i++)if(buttonBits[i]>=0&&(raw&(1u<<buttonBits[i]))!=0)packet.buttons|=1<<i;
            if(stickXOffset>=0){packet.rawX=unchecked((short)Word(data,stickXOffset,2,littleEndian));packet.rawY=unchecked((short)Word(data,stickYOffset,2,littleEndian));}
            if(triggerOffset>=0&&Word(data,triggerOffset,2,littleEndian)>triggerMax||gripOffset>=0&&Word(data,gripOffset,2,littleEndian)>gripMax)return false;
            packet.trigger=triggerOffset<0?0:Word(data,triggerOffset,2,littleEndian)/triggerMax;packet.grip=gripOffset<0?0:Word(data,gripOffset,2,littleEndian)/gripMax;
            if(qXOffset>=0)
            {
                var q=new Quaternion(Component(data,qXOffset),Component(data,qYOffset),Component(data,qZOffset),Component(data,qWOffset));float norm=Quaternion.Dot(q,q);if(!float.IsFinite(norm)||norm<.25f||norm>4)return false;
                var a=new Vector3(Axis(Vector3.right,axisX),Axis(Vector3.right,axisY),Axis(Vector3.right,axisZ));var b=new Vector3(Axis(Vector3.up,axisX),Axis(Vector3.up,axisY),Axis(Vector3.up,axisZ));var c=new Vector3(Axis(Vector3.forward,axisX),Axis(Vector3.forward,axisY),Axis(Vector3.forward,axisZ));
                float determinant=Mathf.Sign(Vector3.Dot(a,Vector3.Cross(b,c)));var v=new Vector3(q.x,q.y,q.z);packet.orientation=Quaternion.Normalize(new Quaternion(determinant*Axis(v,axisX),determinant*Axis(v,axisY),determinant*Axis(v,axisZ),q.w));packet.hasOrientation=true;
            }
            return true;
        }
    }
    public struct BlePacket{public uint sequence;public int buttons,rawX,rawY;public float trigger,grip;public Quaternion orientation;public bool hasOrientation;}
    [Serializable] public sealed class BleProfileCollection
    {
        public int version=1,active;public List<BleControllerProfile> profiles=new List<BleControllerProfile>{new BleControllerProfile()};
        public BleControllerProfile Selected=>profiles[active];
        public void Validate(){if(version!=1||profiles==null||profiles.Count<1||profiles.Count>8||active<0||active>=profiles.Count)throw new InvalidDataException("Invalid BLE profile collection.");foreach(var p in profiles){if(p==null)throw new InvalidDataException("Missing BLE profile.");p.Validate();}}
    }
    public sealed class BleProfileStore
    {
        [Serializable] sealed class Envelope{public string payload,sha256;}
        readonly string path;public bool Protected{get;private set;}public string Error{get;private set;}="";
        public BleProfileStore(string folder){path=Path.Combine(folder,"ble-profiles.json");}
        BleProfileCollection Decode(string file){var e=JsonUtility.FromJson<Envelope>(File.ReadAllText(file));if(e==null||e.payload==null||ProfileStore.Hash(e.payload)!=e.sha256)throw new InvalidDataException("BLE profile checksum mismatch.");var value=JsonUtility.FromJson<BleProfileCollection>(e.payload);if(value==null)throw new InvalidDataException("Missing BLE profiles.");value.Validate();return value;}
        public BleProfileCollection Load()
        {
            foreach(string file in new[]{path,path+".backup"})
            {if(!File.Exists(file))continue;try{return Decode(file);}catch(Exception e)when(e is InvalidDataException||e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is FormatException){Error=e.Message;}}
            Protected=File.Exists(path)||File.Exists(path+".backup");return new BleProfileCollection();
        }
        public bool Save(BleProfileCollection value,out string error)
        {
            error="";try
            {
                if(Protected)throw new IOException("Existing BLE configuration is protected; export or explicitly reset it first.");value.Validate();Directory.CreateDirectory(Path.GetDirectoryName(path));var payload=JsonUtility.ToJson(value);var data=JsonUtility.ToJson(new Envelope{payload=payload,sha256=ProfileStore.Hash(payload)},true);
                foreach(string destination in new[]{path,path+".backup"})
                {string temp=destination+".tmp";using(var fs=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var bytes=System.Text.Encoding.UTF8.GetBytes(data);fs.Write(bytes,0,bytes.Length);fs.Flush(true);}if(File.Exists(destination))File.Replace(temp,destination,null);else File.Move(temp,destination);Decode(destination);}Error="";return true;
            }
            catch(Exception e)when(e is InvalidDataException||e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is FormatException){error=Error=e.Message;return false;}
        }
        public bool Reset(out string error)
        {
            error="";try{string suffix=".preserved-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N");foreach(string source in new[]{path,path+".backup"})if(File.Exists(source))File.Move(source,source+suffix);Protected=false;return Save(new BleProfileCollection(),out error);}
            catch(Exception e)when(e is InvalidDataException||e is IOException||e is UnauthorizedAccessException){Protected=true;error=Error=e.Message;return false;}
        }
    }
}
