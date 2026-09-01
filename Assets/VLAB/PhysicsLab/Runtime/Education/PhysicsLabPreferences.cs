using UnityEngine;

namespace VLAB.PhysicsLab.Education
{
    public static class PhysicsLabPreferences
    {
        private const string GuidanceKey = "VLAB.PhysicsLab.Guidance";
        private const string LeftHandedKey = "VLAB.PhysicsLab.LeftHanded";
        private const string UiScaleKey = "VLAB.PhysicsLab.UiScale";
        private const string SoundKey = "VLAB.PhysicsLab.Sound";
        private const string GuidanceLevelKey = "VLAB.PhysicsLab.GuidanceLevel";
        private const string OutlineKey = "VLAB.PhysicsLab.InteractionOutlines";
        private const string AutoReturnKey = "VLAB.PhysicsLab.AutoReturn";
        private const string MouseSensitivityKey = "VLAB.PhysicsLab.MouseSensitivity";

        public static bool GuidanceEnabled
        {
            get => PlayerPrefs.GetInt(GuidanceKey, 1) == 1;
            set { PlayerPrefs.SetInt(GuidanceKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool LeftHanded
        {
            get => PlayerPrefs.GetInt(LeftHandedKey, 0) == 1;
            set { PlayerPrefs.SetInt(LeftHandedKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float UiScale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(UiScaleKey, 1f), 0.85f, 1.25f);
            set { PlayerPrefs.SetFloat(UiScaleKey, Mathf.Clamp(value, 0.85f, 1.25f)); PlayerPrefs.Save(); }
        }

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                AudioListener.volume = value ? 1f : 0f;
                PlayerPrefs.Save();
            }
        }

        public static int GuidanceLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(GuidanceLevelKey, 1), 0, 2);
            set { PlayerPrefs.SetInt(GuidanceLevelKey, Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
        }

        public static bool InteractionOutlines
        {
            get => PlayerPrefs.GetInt(OutlineKey, 1) == 1;
            set { PlayerPrefs.SetInt(OutlineKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool AutoReturnTools
        {
            get => PlayerPrefs.GetInt(AutoReturnKey, 1) == 1;
            set { PlayerPrefs.SetInt(AutoReturnKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, 0.08f), 0.02f, 0.20f);
            set { PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp(value, 0.02f, 0.20f)); PlayerPrefs.Save(); }
        }
    }
}
