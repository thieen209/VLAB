using UnityEngine;

namespace VLAB.DemoLabs
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class VLabFeedback : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip pick, snap, warning;
        private float lastSound = -1;
        private void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.volume = .16f; source.spatialBlend = 0;
            pick = Tone("Tool pickup", 720, .045f); snap = Tone("Connector seated", 1120, .08f); warning = Tone("Try again", 270, .13f);
        }
        private static AudioClip Tone(string name, float frequency, float duration)
        {
            const int rate = 24000;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)rate;
                samples[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2) + Mathf.Sin(t * frequency * 3.14f) * .20f) * Mathf.Exp(-t * 48) * Mathf.Clamp01(t * 900) * .5f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void Play(AudioClip clip)
        {
            if (source == null || Time.unscaledTime - lastSound < .06f) return;
            lastSound = Time.unscaledTime; source.PlayOneShot(clip);
        }
        public void Pick() => Play(pick);
        public void Snap() => Play(snap);
        public void Warn() => Play(warning);
        private void OnDestroy() { Destroy(pick); Destroy(snap); Destroy(warning); }
    }
}
