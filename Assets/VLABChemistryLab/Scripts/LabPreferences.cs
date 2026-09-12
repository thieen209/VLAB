using System;
using UnityEngine;

namespace VLAB.ChemistryLab
{
    [Serializable]
    public sealed class LabUserSettings
    {
        public int quality = 1;
        public float lookSensitivity = 2.2f;
        public float volume = .7f;
        public bool pourGuide = true;

        public void Validate()
        {
            quality = Mathf.Clamp(quality, 0, 3);
            lookSensitivity = float.IsNaN(lookSensitivity) || float.IsInfinity(lookSensitivity) ? 2.2f : Mathf.Clamp(lookSensitivity, .2f, 5f);
            volume = float.IsNaN(volume) || float.IsInfinity(volume) ? .7f : Mathf.Clamp01(volume);
        }
    }

    /// <summary>Only user preferences are persisted; never experiment data or tracked head poses.</summary>
    public static class LabPreferences
    {
        public const string StorageKey = "VLAB.UserSettings.v1";
        public static LabUserSettings Current { get; private set; } = new LabUserSettings();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => Apply(Decode(PlayerPrefs.GetString(StorageKey, "")), false);

        public static LabUserSettings Decode(string json)
        {
            LabUserSettings settings;
            try { settings = string.IsNullOrWhiteSpace(json) ? new LabUserSettings() : JsonUtility.FromJson<LabUserSettings>(json); }
            catch (ArgumentException) { settings = new LabUserSettings(); }
            settings ??= new LabUserSettings();
            settings.Validate();
            return settings;
        }

        public static void Apply(LabUserSettings settings, bool save = true)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            Current = Decode(JsonUtility.ToJson(settings));
            AudioListener.volume = Current.volume;
            QualitySettings.antiAliasing = Current.quality == 0 ? 0 : Current.quality == 3 ? 8 : 4;
            QualitySettings.shadows = Current.quality == 0 ? ShadowQuality.Disable : Current.quality == 1 ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = Current.quality < 2 ? ShadowResolution.Medium : ShadowResolution.High;
            QualitySettings.shadowDistance = Current.quality == 0 ? 0 : Current.quality == 1 ? 20 : Current.quality == 2 ? 35 : 60;
            QualitySettings.pixelLightCount = Current.quality < 2 ? 2 : 4;
            if (!save) return;
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }
    }
}
