#if UNITY_EDITOR
using System;
using UnityEngine;
using Vlab.Product;

public static class VlabAudioChecks
{
    public static void Run()
    {
        foreach(VlabSound sound in Enum.GetValues(typeof(VlabSound)))
        {
            float[] samples=VlabAudio.CueSamples(sound);
            Check(samples.Length>100&&samples.Length<22050,"Short mono cue duration");
            Check(samples[0]==0&&samples[samples.Length-1]==0,"Silent cue endpoints");
            CheckSamples(samples);
        }
        for(int room=0;room<4;room++)
        {
            float[] samples=VlabAudio.RoomSamples(room);CheckSamples(samples);
            Check(Mathf.Abs(samples[0]-samples[samples.Length-1])<.03f,"Continuous room loop");
        }
        var motor=VlabAudio.MotorSamples();CheckSamples(motor);Check(Mathf.Abs(motor[0]-motor[motor.Length-1])<.02f,"Motor loop boundary");
        Debug.Log("VLAB_AUDIO_SAMPLE_CHECKS_PASS");
    }
    static void CheckSamples(float[] samples)
    {
        float peak=0;
        foreach(float sample in samples){Check(float.IsFinite(sample)&&Mathf.Abs(sample)<.5f,"Finite unclipped samples");peak=Mathf.Max(peak,Mathf.Abs(sample));}
        Check(peak>.01f,"Audible source samples");
    }
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
#endif
