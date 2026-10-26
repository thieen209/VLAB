using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Vlab.Product
{
    public enum ControllerAction { Select,Grip,Menu,Recenter,Back }
    // Stable saved options describe standard Input System HID controls only.
    // Composite defaults overlap their individual buttons and are validated as such.
    public static class ControllerBindings
    {
        public const int OptionCount=14;
        static readonly int[] masks={129,48,256,512,1,2,4,8,16,32,64,128,1024,2048};
        public static int Mask(int option)=>option>=0&&option<OptionCount?masks[option]:0;
        public static string Label(int option,bool vi)
        {
            string[] labels=vi?new[]{"A / cò phải","LB / RB","Start / Menu","Select / View","A · nút dưới","B · nút phải","X · nút trái","Y · nút trên","LB · vai trái","RB · vai phải","LT · cò trái","RT · cò phải","Nhấn cần trái","Nhấn cần phải"}:new[]{"A / right trigger","LB / RB","Start / Menu","Select / View","A · south button","B · east button","X · west button","Y · north button","LB · left shoulder","RB · right shoulder","LT · left trigger","RT · right trigger","Left stick press","Right stick press"};
            return option>=0&&option<labels.Length?labels[option]:(vi?"Không hợp lệ":"Invalid");
        }
        public static string ActionLabel(ControllerAction action,bool vi)
        {string[] labels=vi?new[]{"Chọn / xác nhận","Giữ / thả dụng cụ","Mở menu","Căn lại phía trước","Quay lại / hủy"}:new[]{"Select / confirm","Hold / release equipment","Open menu","Recenter ahead","Back / cancel"};return labels[(int)action];}
        public static int Get(GameSettings settings,ControllerAction action)
        {switch(action){case ControllerAction.Select:return settings.controllerSelect;case ControllerAction.Grip:return settings.controllerGrip;case ControllerAction.Menu:return settings.controllerMenu;case ControllerAction.Recenter:return settings.controllerRecenter;case ControllerAction.Back:return settings.controllerBack;default:throw new ArgumentOutOfRangeException(nameof(action));}}
        static void Set(GameSettings settings,ControllerAction action,int option)
        {switch(action){case ControllerAction.Select:settings.controllerSelect=option;break;case ControllerAction.Grip:settings.controllerGrip=option;break;case ControllerAction.Menu:settings.controllerMenu=option;break;case ControllerAction.Recenter:settings.controllerRecenter=option;break;case ControllerAction.Back:settings.controllerBack=option;break;default:throw new ArgumentOutOfRangeException(nameof(action));}}
        public static bool CanAssign(GameSettings settings,ControllerAction action,int option)
        {int mask=Mask(option);if(mask==0)return false;for(int i=0;i<5;i++)if((ControllerAction)i!=action&&(mask&Mask(Get(settings,(ControllerAction)i)))!=0)return false;return true;}
        public static bool TryAssign(GameSettings settings,ControllerAction action,int option)
        {if(!CanAssign(settings,action,option))return false;Set(settings,action,option);return true;}
        // Physical-first editor: a swap is committed only after the complete map validates.
        // Composite legacy bindings can occupy two controls, so intersect masks, not IDs.
        public static bool TryAssignPhysical(GameSettings settings,int option,ControllerAction action)
        {
            if(settings==null||Mask(option)==0||(int)action<0||(int)action>=5)return false;
            var candidate=settings.Copy();int previous=Get(candidate,action),owner=-1;
            try{Validate(candidate);}catch(InvalidDataException){return false;}
            for(int i=0;i<5;i++)if(i!=(int)action&&(Mask(option)&Mask(Get(candidate,(ControllerAction)i)))!=0)
            {if(owner>=0)return false;owner=i;}
            Set(candidate,action,option);if(owner>=0)Set(candidate,(ControllerAction)owner,previous);
            try{Validate(candidate);}catch(InvalidDataException){return false;}
            for(int i=0;i<5;i++)Set(settings,(ControllerAction)i,Get(candidate,(ControllerAction)i));return true;
        }
        public static string PhysicalActionLabel(GameSettings settings,int option,bool vi)
        {
            var labels=new List<string>();int mask=Mask(option);
            if(settings!=null&&mask!=0)for(int i=0;i<5;i++)if((mask&Mask(Get(settings,(ControllerAction)i)))!=0)labels.Add(ActionLabel((ControllerAction)i,vi));
            return labels.Count==0?(vi?"Chưa gán":"Unassigned"):string.Join(" · ",labels);
        }
        // -1 means unassigned; -2 means a composite option intersects multiple actions.
        public static int OwnerOfPhysical(GameSettings settings,int option)
        {int owner=-1,mask=Mask(option);if(settings==null||mask==0)return -1;for(int i=0;i<5;i++)if((mask&Mask(Get(settings,(ControllerAction)i)))!=0){if(owner>=0)return -2;owner=i;}return owner;}
        public static int Cycle(GameSettings settings,ControllerAction action,int direction=1)
        {int current=Get(settings,action),step=direction<0?-1:1;for(int i=1;i<=OptionCount;i++){int next=((current+step*i)%OptionCount+OptionCount)%OptionCount;if(TryAssign(settings,action,next))return next;}return current;}
        public static bool Equal(GameSettings a,GameSettings b)
        {for(int i=0;i<5;i++)if(Get(a,(ControllerAction)i)!=Get(b,(ControllerAction)i))return false;return true;}
        public static void Validate(GameSettings settings)
        {int occupied=0;for(int i=0;i<5;i++){int mask=Mask(Get(settings,(ControllerAction)i));if(mask==0||(mask&occupied)!=0)throw new InvalidDataException("Invalid or overlapping controller bindings.");occupied|=mask;}}
    }
    [Serializable] public sealed class GameSettings
    {
        public float volume=.75f; public bool muted,reducedMotion,largeText;
        public int hintLevel;public bool hints=true; public int graphics=1;
        // Keep legacy graphics in 0..2 so an older app can still read the whole profile.
        public bool ultraGraphics,showPerformance;
        public int GraphicsTier=>ultraGraphics?3:graphics;
        public void SetGraphicsTier(int tier){if(tier<0||tier>3)throw new ArgumentOutOfRangeException(nameof(tier));graphics=Math.Min(tier,2);ultraGraphics=tier==3;}
        public int uiTheme;public float uiOpacity=.8f;
        public int vrResolution;
        public bool gazeAutoSelect=true;public float gazeDwellSeconds=1.2f,mouseLookSensitivity=1;
        public int controllerSelect=0,controllerGrip=1,controllerMenu=2,controllerRecenter=3,controllerBack=5;
        public bool invertLookX,invertLookY,phoneGyro;public bool showPhoneJoystick=true,focusAssist=true;public float moveSpeed=1,cameraFov=70,joystickScale=1;
        public GameSettings Copy()=>JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(this));
        public void Validate(){if(hintLevel<0||hintLevel>1)throw new InvalidDataException("Invalid hint level.");if(vrResolution<0||vrResolution>2)throw new InvalidDataException("Invalid VR resolution.");if(!float.IsFinite(joystickScale)||joystickScale<.75f||joystickScale>1.25f)throw new InvalidDataException("Invalid joystick size.");if(!float.IsFinite(moveSpeed)||moveSpeed<.3f||moveSpeed>2||!float.IsFinite(cameraFov)||cameraFov<70||cameraFov>120)throw new InvalidDataException("Invalid phone settings.");if(!float.IsFinite(volume)||volume<0||volume>1||graphics<0||graphics>2||(ultraGraphics&&graphics!=2)||uiTheme<0||uiTheme>4||!float.IsFinite(uiOpacity)||uiOpacity<.55f||uiOpacity>1||!float.IsFinite(gazeDwellSeconds)||gazeDwellSeconds<.6f||gazeDwellSeconds>2||!float.IsFinite(mouseLookSensitivity)||mouseLookSensitivity<.5f||mouseLookSensitivity>2)throw new InvalidDataException("Invalid settings.");ControllerBindings.Validate(this);}
    }
    [Serializable] public sealed class LegalAcceptance
    {public string documentId,contentHash,languageShown,acceptedAtUtc;}
    [Serializable] public sealed class AttemptRecord
    {
        public string experimentId,attemptId,savedAtUtc,model,observation,detailJson,assistance;
        public string modelNotesVi,modelNotesEn;
        public bool partial;
        public int seed; public List<Reading> readings=new List<Reading>();
        public double[] parameterValues;
    }
    [Serializable] public sealed class SessionCheckpoint
    {public string experimentId,detailJson,assistance;public int operation,seed;public double[] values;public bool apparatusReady;}
    [Serializable] public sealed class NotebookStroke
    {
        public List<Vector2> points=new List<Vector2>();
    }
    [Serializable] public sealed class NotebookPage
    {
        public const int InkStrokeLimit=48,InkPointLimit=128;
        public string id,experimentId="",title="",body="",createdAtUtc,updatedAtUtc;
        public List<NotebookStroke> inkStrokes=new List<NotebookStroke>();
        public void ValidateInk()
        {
            if(inkStrokes==null||inkStrokes.Count>InkStrokeLimit)throw new InvalidDataException("Invalid notebook drawing.");
            foreach(var stroke in inkStrokes)
            {
                if(stroke==null||stroke.points==null||stroke.points.Count==0||stroke.points.Count>InkPointLimit)throw new InvalidDataException("Invalid notebook stroke.");
                foreach(var point in stroke.points)if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||point.x<0||point.x>1||point.y<0||point.y>1)throw new InvalidDataException("Invalid notebook ink coordinate.");
            }
        }
    }
    [Serializable] public sealed class LocalProfile
    {
        public int schemaVersion=2;public long generation;
        public string locale="";public bool safetyAcknowledged;public bool quickStartSeen;
        public GameSettings settings=new GameSettings();
        public List<LegalAcceptance> legal=new List<LegalAcceptance>();
        public List<AttemptRecord> attempts=new List<AttemptRecord>();
        public List<NotebookPage> notebookPages=new List<NotebookPage>();
        public SessionCheckpoint checkpoint;
        public bool HasAccepted(string id,string hash)=>legal.Exists(x=>x.documentId==id&&x.contentHash==hash);
        public string NextSetup(string privacyHash,string termsHash)
        {
            if(string.IsNullOrEmpty(locale))return "language";
            if(!HasAccepted("privacy",privacyHash))return "privacy";
            if(!HasAccepted("terms",termsHash))return "terms";
            return safetyAcknowledged?"home":"safety";
        }
        public void Accept(string id,string hash,bool explicitConsent)
        {
            if(!explicitConsent||string.IsNullOrEmpty(hash)||(id!="privacy"&&id!="terms"))throw new InvalidOperationException("Explicit legal acceptance required.");
            legal.RemoveAll(x=>x.documentId==id);
            legal.Add(new LegalAcceptance{documentId=id,contentHash=hash,languageShown=locale,acceptedAtUtc=DateTime.UtcNow.ToString("O")});
        }
        public void Validate()
        {
            if(schemaVersion!=2||generation<0||(locale!=""&&locale!="vi"&&locale!="en")||settings==null||legal==null||attempts==null||notebookPages==null)throw new InvalidDataException("Invalid profile.");
            settings.Validate();
            foreach(var a in legal)if(a==null||string.IsNullOrEmpty(a.documentId)||string.IsNullOrEmpty(a.contentHash)||!DateTime.TryParse(a.acceptedAtUtc,out _))throw new InvalidDataException("Invalid legal acceptance.");
            foreach(var a in attempts){if(a==null||!LessonSpec.All.ContainsKey(a.experimentId)||a.readings==null)throw new InvalidDataException("Invalid attempt.");foreach(var r in a.readings)if(r==null||double.IsNaN(r.value)||double.IsInfinity(r.value))throw new InvalidDataException("Invalid reading.");}
            var pageIds=new HashSet<string>();
            foreach(var note in notebookPages)
            {
                if(note==null||string.IsNullOrWhiteSpace(note.id)||note.id.Length>64||!pageIds.Add(note.id)||note.title==null||note.title.Length>120||note.body==null||note.body.Length>12000||(!string.IsNullOrEmpty(note.experimentId)&&!LessonSpec.All.ContainsKey(note.experimentId))||!DateTime.TryParse(note.createdAtUtc,out _)||!DateTime.TryParse(note.updatedAtUtc,out _))throw new InvalidDataException("Invalid notebook page.");
                note.ValidateInk();
            }
            // Unity inline serialization materializes a null nested DTO as an empty object on load.
            if(checkpoint!=null&&string.IsNullOrEmpty(checkpoint.experimentId)&&checkpoint.operation==0&&(checkpoint.values==null||checkpoint.values.Length==0))checkpoint=null;
            if(checkpoint!=null)
            {
                if(!LessonSpec.All.TryGetValue(checkpoint.experimentId,out var spec)||checkpoint.values==null||checkpoint.values.Length!=spec.parameters.Length||checkpoint.operation<0||checkpoint.operation>spec.operationsEn.Length)throw new InvalidDataException("Invalid checkpoint.");
                for(int i=0;i<checkpoint.values.Length;i++){var v=checkpoint.values[i];var p=spec.parameters[i];if(double.IsNaN(v)||double.IsInfinity(v)||v<p.min||v>p.max)throw new InvalidDataException("Invalid checkpoint parameter.");}
            }
        }
    }
    public sealed class ProfileLoad
    {public LocalProfile profile;public string message="";public bool recovered,writeProtected;}
    public sealed class ProfileStore
    {
        [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
        readonly string folder,primary,backup,marker;
        bool writeProtected;
        public string Folder=>folder;
        public ProfileStore(string folder){this.folder=folder;primary=Path.Combine(folder,"profile.json");backup=Path.Combine(folder,"profile.backup.json");marker=Path.Combine(folder,"reset-generation.txt");}
        public static string Hash(string value){using(var sha=SHA256.Create()){var bytes=sha.ComputeHash(Encoding.UTF8.GetBytes(value));return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();}}
        LocalProfile Decode(string path)
        {
            var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(path,Encoding.UTF8));
            if(envelope==null||envelope.format!=1||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)throw new InvalidDataException("Profile checksum mismatch.");
            var p=JsonUtility.FromJson<LocalProfile>(envelope.payload);
            if(p==null)throw new InvalidDataException("Empty profile.");
            if(p.schemaVersion>2){writeProtected=true;throw new NotSupportedException("A newer application version wrote this profile. Its data has been preserved.");}
            // Legacy profiles predate appearance. Only an absent field receives the default;
            // an explicit zero, non-finite value or out-of-range new setting remains invalid.
            if(p.settings!=null&&!Regex.IsMatch(envelope.payload,"\"uiOpacity\"\\s*:"))p.settings.uiOpacity=1;
            if(p.settings!=null)
            {
                if(!Regex.IsMatch(envelope.payload,"\"gazeAutoSelect\"\\s*:"))p.settings.gazeAutoSelect=true;
                if(!Regex.IsMatch(envelope.payload,"\"gazeDwellSeconds\"\\s*:"))p.settings.gazeDwellSeconds=1.2f;
                if(!Regex.IsMatch(envelope.payload,"\"joystickScale\"\\s*:"))p.settings.joystickScale=1;
                if(!Regex.IsMatch(envelope.payload,"\"moveSpeed\"\\s*:"))p.settings.moveSpeed=1;
                if(!Regex.IsMatch(envelope.payload,"\"cameraFov\"\\s*:"))p.settings.cameraFov=70;
                // Only values valid in the previous phone range may migrate upward.
                if(float.IsFinite(p.settings.cameraFov)&&p.settings.cameraFov>=45&&p.settings.cameraFov<70)p.settings.cameraFov=70;
                if(!Regex.IsMatch(envelope.payload,"\"showPhoneJoystick\"\\s*:"))p.settings.showPhoneJoystick=true;
                if(!Regex.IsMatch(envelope.payload,"\"focusAssist\"\\s*:"))p.settings.focusAssist=true;
                if(!Regex.IsMatch(envelope.payload,"\"mouseLookSensitivity\"\\s*:"))p.settings.mouseLookSensitivity=1;
                if(!Regex.IsMatch(envelope.payload,"\"controllerSelect\"\\s*:"))p.settings.controllerSelect=0;
                if(!Regex.IsMatch(envelope.payload,"\"controllerGrip\"\\s*:"))p.settings.controllerGrip=1;
                if(!Regex.IsMatch(envelope.payload,"\"controllerMenu\"\\s*:"))p.settings.controllerMenu=2;
                if(!Regex.IsMatch(envelope.payload,"\"controllerRecenter\"\\s*:"))p.settings.controllerRecenter=3;
                if(!Regex.IsMatch(envelope.payload,"\"controllerBack\"\\s*:"))p.settings.controllerBack=5;
            }
            bool hasNotes=Regex.IsMatch(envelope.payload,"\"notebookPages\"\\s*:");
            if(Regex.IsMatch(envelope.payload,"\"notebookPages\"\\s*:\\s*null"))throw new InvalidDataException("Invalid notebook page collection.");
            if(p.schemaVersion==1){if(!hasNotes)p.notebookPages=new List<NotebookPage>();p.schemaVersion=2;}
            else if(!hasNotes)throw new InvalidDataException("Missing notebook pages in current profile.");
            // Older pages have no ink field; explicit null ink remains corrupt rather than erased.
            if(Regex.IsMatch(envelope.payload,"\"inkStrokes\"\\s*:\\s*null"))throw new InvalidDataException("Invalid notebook drawing collection.");
            if(p.notebookPages!=null)foreach(var note in p.notebookPages)if(note!=null&&note.inkStrokes==null)note.inkStrokes=new List<NotebookStroke>();
            p.Validate();return p;
        }
        long MinimumGeneration()
        {
            if(!File.Exists(marker))return 0;
            if(!long.TryParse(File.ReadAllText(marker),out var g)||g<0)throw new InvalidDataException("Reset marker is unreadable; preserved saved files.");return g;
        }
        public ProfileLoad Load()
        {
            var result=new ProfileLoad{profile=new LocalProfile()};
            try
            {
                var generation=MinimumGeneration();
                foreach(var file in new[]{primary,backup})
                {
                    if(!File.Exists(file))continue;
                    try
                    {
                        var p=Decode(file);if(p.generation<generation)continue;
                        result.profile=p;result.recovered=file==backup;
                        if(result.recovered)result.message="Recovered the valid local backup. The damaged primary file is preserved until the next successful save.";
                        return result;
                    }
                    catch(NotSupportedException ex){result.writeProtected=true;result.message=ex.Message;return result;}
                    catch(Exception ex) when(ex is IOException||ex is ArgumentException||ex is InvalidDataException){result.message="Saved profile is damaged. "+ex.Message;}
                }
                result.profile.generation=generation;
                if(File.Exists(primary)||File.Exists(backup))result.message="No valid saved profile could be loaded. Saved files were preserved; new progress will require an explicit recovery/reset choice.";
                result.writeProtected=File.Exists(primary)||File.Exists(backup);writeProtected=result.writeProtected;
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){result.message=ex.Message;result.writeProtected=writeProtected=true;}
            return result;
        }
        static void DurableWrite(string path,string content)
        {using(var fs=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(content);fs.Write(bytes,0,bytes.Length);fs.Flush(true);}}
        void AtomicReplace(string path,string content)
        {
            var temp=path+".tmp";DurableWrite(temp,content);
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        public bool Save(LocalProfile profile,out string error)
        {
            error="";
            try
            {
                if(writeProtected)throw new InvalidOperationException("Existing data is protected. Review the recovery message or explicitly reset the profile.");
                profile.Validate();Directory.CreateDirectory(folder);
                if(profile.generation<MinimumGeneration())throw new InvalidDataException("Refusing to restore data predating an explicit reset.");
                string payload=JsonUtility.ToJson(profile),data=JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)},true);
                AtomicReplace(primary,data);Decode(primary);
                // The verified primary is committed; a backup failure must not roll back callers.
                try{AtomicReplace(backup,data);Decode(backup);}
                catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException||ex is InvalidOperationException||ex is ArgumentException){Debug.LogWarning("VLAB profile saved; backup update failed: "+ex.Message);}
                return true;
            }
            catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException||ex is InvalidOperationException||ex is ArgumentException){error="Could not save locally: "+ex.Message;return false;}
        }
        public bool Reset(LocalProfile current,bool full,out LocalProfile replacement,out string error)
        {
            replacement=null;error="";
            try
            {
                Directory.CreateDirectory(folder);
                long generation=Math.Max(current.generation,MinimumGeneration())+1;
                var next=full?new LocalProfile():JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(current));
                next.generation=generation;next.attempts.Clear();next.checkpoint=null;next.Validate();
                // Commit reset intent first. A crash cannot reload a pre-reset backup.
                AtomicReplace(marker,generation.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writeProtected=false;
                if(!Save(next,out error))return false;
                replacement=next;return true;
            }
            catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException){error="Could not reset local profile: "+ex.Message;return false;}
        }
    }
}
