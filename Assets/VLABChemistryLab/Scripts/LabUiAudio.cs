using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Bounded, lazy two-voice feedback pool. No allocations per click after initialization.</summary>
    public sealed class LabUiAudio : MonoBehaviour
    {
        private static LabUiAudio instance;
        private readonly AudioSource[] voices = new AudioSource[2];
        private AudioClip click;
        private int nextVoice;

        public static void PlayClick()
        {
            if (LabPreferences.Current.volume <= 0) return;
            if (instance == null)
            {
                var host = new GameObject("VLAB UI Audio Pool");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<LabUiAudio>();
            }
            var voice = instance.voices[instance.nextVoice];
            instance.nextVoice = (instance.nextVoice + 1) % instance.voices.Length;
            voice.Play();
        }

        private void Awake()
        {
            const int rate = 24000, count = 1200;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 660 / rate) * Mathf.Sin(t * Mathf.PI) * (1 - t) * .15f;
            }
            click = AudioClip.Create("VLAB soft click", count, 1, rate, false);
            click.SetData(samples, 0);
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0;
                voices[i].clip = click;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (click != null) Destroy(click);
        }
    }
}
