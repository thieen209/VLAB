using UnityEngine;

namespace VLAB.MainMenu
{
    [CreateAssetMenu(menuName = "VLAB/Menu assets")]
    public sealed class VLABMenuAssets : ScriptableObject
    {
        public Font font;
        public TMPro.TMP_FontAsset activityFont;
        public GameObject controllerVisual;
        public GameObject playerRig;
        public string[] termsVi;
        public string[] termsEn;
        public string[] privacyVi;
        public string[] privacyEn;
        public string termsVersion = "2026-08-27";
        public string privacyVersion = "2026-08-27";
    }
}
