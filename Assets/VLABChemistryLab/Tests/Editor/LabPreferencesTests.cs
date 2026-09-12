using NUnit.Framework;
using UnityEngine;

namespace VLAB.ChemistryLab.Tests
{
    public class LabPreferencesTests
    {
        private string previous;
        private bool hadKey;
        private LabUserSettings settings;
        [SetUp] public void Setup()
        {
            hadKey = PlayerPrefs.HasKey(LabPreferences.StorageKey);
            previous = PlayerPrefs.GetString(LabPreferences.StorageKey);
            settings = LabPreferences.Decode(JsonUtility.ToJson(LabPreferences.Current));
        }
        [TearDown] public void Cleanup()
        {
            LabPreferences.Apply(settings, false);
            if (hadKey) PlayerPrefs.SetString(LabPreferences.StorageKey, previous);
            else PlayerPrefs.DeleteKey(LabPreferences.StorageKey);
            PlayerPrefs.Save();
        }
        [TestCase(0, 0, ShadowQuality.Disable, 0)]
        [TestCase(1, 4, ShadowQuality.HardOnly, 20)]
        [TestCase(2, 4, ShadowQuality.All, 35)]
        [TestCase(3, 8, ShadowQuality.All, 60)]
        public void Preset_AppliesRealRenderingSettings(int quality, int aa, ShadowQuality shadows, int distance)
        {
            LabPreferences.Apply(new LabUserSettings { quality = quality, volume = .25f, lookSensitivity = 4, pourGuide = false });
            Assert.That(QualitySettings.antiAliasing, Is.EqualTo(aa));
            Assert.That(QualitySettings.shadows, Is.EqualTo(shadows));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(distance));
            Assert.That(AudioListener.volume, Is.EqualTo(.25f));
            var restored = LabPreferences.Decode(PlayerPrefs.GetString(LabPreferences.StorageKey));
            Assert.That(restored.quality, Is.EqualTo(quality));
            Assert.That(restored.pourGuide, Is.False);
            Assert.That(restored.lookSensitivity, Is.EqualTo(4));
        }
        [Test]
        public void InvalidSettings_UseSafeDefaultsAndClampValues()
        {
            Assert.That(LabPreferences.Decode("broken json").quality, Is.EqualTo(1));
            var invalid = new LabUserSettings { quality = 100, volume = -1, lookSensitivity = float.NaN };
            invalid.Validate();
            Assert.That(invalid.quality, Is.EqualTo(3));
            Assert.That(invalid.volume, Is.Zero);
            Assert.That(invalid.lookSensitivity, Is.EqualTo(2.2f));
        }
    }
}
