using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vlab.Product
{
    public enum VlabSound { Hover, Click, Back, Success, Warning, Experiment, Capture, Open, Liquid, Glass, Mechanism, Placement }

    // Original synthesized earcons keep the offline package small and require no licensed downloads.
    public sealed class VlabAudio : MonoBehaviour
    {
        static VlabAudio instance;
        readonly AudioClip[] cues=new AudioClip[System.Enum.GetValues(typeof(VlabSound)).Length];
        readonly AudioClip[] rooms=new AudioClip[4];
        readonly float[] lastPlayed=new float[System.Enum.GetValues(typeof(VlabSound)).Length];
        AudioSource effects,ambience,apparatus;
        int room=-1;
        const int Rate=22050;

        void Awake()
        {
            if(instance&&instance!=this){Destroy(this);return;}
            instance=this;
            effects=Source("Interface and experiment sounds",false);effects.volume=.5f;
            ambience=Source("Quiet laboratory atmosphere",true);ambience.volume=0;
            apparatus=Source("Apparatus contact and liquid",false);apparatus.spatialBlend=.8f;apparatus.minDistance=.5f;apparatus.maxDistance=6;apparatus.rolloffMode=AudioRolloffMode.Linear;apparatus.volume=.35f;
            for(int i=0;i<cues.Length;i++){cues[i]=Clip(((VlabSound)i).ToString(),CueSamples((VlabSound)i));lastPlayed[i]=-10;}
            for(int i=0;i<rooms.Length;i++)rooms[i]=Clip("Laboratory atmosphere "+i,RoomSamples(i));
        }
        AudioSource Source(string label,bool loop)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;
            source.spatialBlend=0;source.dopplerLevel=0;source.priority=loop?200:64;
            return source;
        }
        static AudioClip Clip(string label,float[] samples)
        {
            var clip=AudioClip.Create("VLAB "+label,samples.Length,1,Rate,false);clip.SetData(samples,0);return clip;
        }
        public static void Play(VlabSound sound)
        {
            int index=(int)sound;
            if(!instance||index<0||index>=instance.cues.Length||AudioListener.volume<=0)return;
            float now=Time.unscaledTime;
            if(now-instance.lastPlayed[index]<(sound==VlabSound.Hover?.15f:.07f))return;
            instance.lastPlayed[index]=now;
            instance.effects.PlayOneShot(instance.cues[index],sound==VlabSound.Hover?.28f:1);
        }
        public static VlabSound? SoundForAction(string action)
        {
            if(string.IsNullOrEmpty(action))return null;
            if(action.StartsWith("dose:")||action.StartsWith("pour")||action.StartsWith("transfer")||action=="rinseProbe"||action=="addWater")return VlabSound.Liquid;
            if(action.StartsWith("probe")||action=="buffer4"||action=="buffer7")return VlabSound.Glass;
            if(action.StartsWith("focus")||action.StartsWith("pull")||action=="steady"||action.StartsWith("crank")||action.StartsWith("angle")||action.StartsWith("speed"))return VlabSound.Mechanism;
            return null;
        }
        public static void At(VlabSound sound,Vector3 position)
        {
            if(!instance||AudioListener.volume<=0)return;
            int index=(int)sound;float now=Time.unscaledTime;if(now-instance.lastPlayed[index]<.12f)return;
            instance.lastPlayed[index]=now;instance.apparatus.transform.position=position;instance.apparatus.PlayOneShot(instance.cues[index]);
        }
        public static void Action(string action,Vector3 position){var sound=SoundForAction(action);if(sound.HasValue)At(sound.Value,position);}
        void Update()
        {
            var app=VlabApp.Instance;
            bool active=app&&(app.Route=="activity"||app.Route=="explore");
            string name=SceneManager.GetActiveScene().name;
            int next=name=="Physics"?0:name=="Chemistry"?1:name=="Biology"?2:name=="Engineering"?3:-1;
            float target=active&&next>=0?.055f:0;
            if(next!=room&&ambience.volume<=.001f)
            {
                ambience.Stop();room=next;
                if(room>=0){ambience.clip=rooms[room];ambience.Play();}
            }
            if(next!=room)target=0;
            ambience.volume=Mathf.MoveTowards(ambience.volume,target,Time.unscaledDeltaTime*.045f);
            if(target==0&&ambience.volume==0&&ambience.isPlaying)ambience.Pause();
            else if(target>0&&!ambience.isPlaying)ambience.UnPause();
        }
        public static float[] CueSamples(VlabSound sound)
        {
            if(sound>=VlabSound.Liquid)return ContactSamples(sound);
            float[] notes;float duration;
            switch(sound)
            {
                case VlabSound.Hover:notes=new[]{880f};duration=.045f;break;
                case VlabSound.Click:notes=new[]{660f,990f};duration=.105f;break;
                case VlabSound.Back:notes=new[]{660f,440f};duration=.17f;break;
                case VlabSound.Success:notes=new[]{523.25f,659.25f,783.99f,1046.5f};duration=.66f;break;
                case VlabSound.Warning:notes=new[]{311.13f,293.66f};duration=.34f;break;
                case VlabSound.Experiment:notes=new[]{261.63f,392f,523.25f};duration=.48f;break;
                case VlabSound.Capture:notes=new[]{1046.5f,1318.51f};duration=.24f;break;
                default:notes=new[]{392f,587.33f};duration=.23f;break;
            }
            var samples=new float[Mathf.CeilToInt(duration*Rate)];
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/(samples.Length-1),step=t*notes.Length;
                int n=Mathf.Min(notes.Length-1,(int)step);float local=step-n;
                float envelope=Mathf.Sin(Mathf.PI*local);envelope*=envelope;
                float phase=2*Mathf.PI*notes[n]*i/Rate;
                samples[i]=.24f*envelope*(Mathf.Sin(phase)+.16f*Mathf.Sin(phase*2));
            }
            samples[0]=samples[samples.Length-1]=0;return samples;
        }
        static float[] ContactSamples(VlabSound sound)
        {
            float duration=sound==VlabSound.Liquid?.48f:sound==VlabSound.Glass?.22f:.14f;
            var samples=new float[Mathf.CeilToInt(duration*Rate)];var random=new System.Random(47+(int)sound);float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/Rate,u=t/duration;filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.2f);
                float envelope=Mathf.Min(1,t*200)*Mathf.Pow(1-u,3),value;
                if(sound==VlabSound.Liquid){float drops=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*61)),8);value=.14f*filtered+.17f*drops*Mathf.Sin(2*Mathf.PI*(1100*t-500*t*t));}
                else if(sound==VlabSound.Glass)value=.18f*Mathf.Sin(2*Mathf.PI*1730*t)+.1f*Mathf.Sin(2*Mathf.PI*2670*t)+.04f*filtered;
                else if(sound==VlabSound.Mechanism)value=.2f*filtered*Mathf.Pow(.5f+.5f*Mathf.Cos(t*160),4)+.1f*Mathf.Sin(2*Mathf.PI*180*t);
                else value=.18f*filtered+.13f*Mathf.Sin(2*Mathf.PI*240*t);
                samples[i]=value*envelope;
            }
            samples[0]=samples[samples.Length-1]=0;return samples;
        }
        public static float[] RoomSamples(int index)
        {
            var samples=new float[Rate*4];float frequency=index==0?110:index==1?90:index==2?75:60;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/Rate;
                float breath=.72f+.14f*Mathf.Sin(2*Mathf.PI*.25f*t);
                samples[i]=breath*(.24f*Mathf.Sin(2*Mathf.PI*frequency*t)+.10f*Mathf.Sin(2*Mathf.PI*(frequency*2+3)*t)+.06f*Mathf.Sin(2*Mathf.PI*(frequency*4+7)*t));
            }
            return samples;
        }
        void OnDestroy()
        {
            if(instance!=this)return;instance=null;
            foreach(var clip in cues)if(clip)Destroy(clip);
            foreach(var clip in rooms)if(clip)Destroy(clip);
            if(apparatus)Destroy(apparatus.gameObject);if(effects)Destroy(effects.gameObject);if(ambience)Destroy(ambience.gameObject);
        }
    }
}
