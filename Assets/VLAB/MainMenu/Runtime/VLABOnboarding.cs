using UnityEngine;

namespace VLAB.MainMenu
{
    public static class VLABOnboarding
    {
        public const string LanguageKey = "VLAB.UI.Language";
        public const string TermsKey = "VLAB.UI.TermsVersion";
        public const string PrivacyKey = "VLAB.UI.PrivacyVersion";
        public static string Language => PlayerPrefs.GetString(LanguageKey, "vi");
        public static bool HasLanguage => PlayerPrefs.HasKey(LanguageKey);
        public static void SetLanguage(string value)
        {
            PlayerPrefs.SetString(LanguageKey, value == "en" ? "en" : "vi");
            PlayerPrefs.Save();
        }
        public static bool Accepted(bool privacy, VLABMenuAssets assets) => assets != null &&
            PlayerPrefs.GetString(privacy ? PrivacyKey : TermsKey, "") ==
            (privacy ? assets.privacyVersion : assets.termsVersion);
        public static void Accept(bool privacy, VLABMenuAssets assets)
        {
            PlayerPrefs.SetString(privacy ? PrivacyKey : TermsKey, privacy ? assets.privacyVersion : assets.termsVersion);
            PlayerPrefs.Save();
        }
        public static string Next(VLABMenuAssets assets)
        {
            if (!HasLanguage) return "language";
            if (!Accepted(false, assets)) return "terms";
            if (!Accepted(true, assets)) return "privacy";
            return "home";
        }
    }
}
