using NUnit.Framework;
using UnityEngine;
using VLAB.Core.Input;

namespace VLAB.MainMenu.Tests
{
    public sealed class VLABProductionMobileVrTests
    {
        [TestCase(RuntimePlatform.Android, "Menu", true)]
        [TestCase(RuntimePlatform.Android, "PhysicsLab_Base", false)]
        [TestCase(RuntimePlatform.Android, "ChemistryLab", false)]
        [TestCase(RuntimePlatform.Android, "BiologyLab", false)]
        [TestCase(RuntimePlatform.Android, "EngineeringLab", false)]
        [TestCase(RuntimePlatform.WindowsEditor, "Menu", false)]
        [TestCase(RuntimePlatform.WindowsPlayer, "Menu", false)]
        [TestCase(RuntimePlatform.Android, "menu", false)]
        [TestCase(RuntimePlatform.Android, null, false)]
        public void VrModeChangesAreRestrictedToAndroidMainMenu(RuntimePlatform platform, string scene, bool allowed)
        {
            Assert.That(VLabMobileVrMode.CanSwitch(platform, scene), Is.EqualTo(allowed));
        }

        [Test] public void CachedRequestedModeDisablesPhoneViewerWithoutChangingSavedPreference()
        {
            var previous = VLabHeadPose.ViewerRequested;
            var preference = PlayerPrefs.GetInt(VLabHeadPose.ViewerPreferenceKey, -1);
            try
            {
                VLabHeadPose.SetViewerRequested(false);
                Assert.That(VLabHeadPose.PhoneViewer, Is.False);
                Assert.That(VLabMobileVrMode.Enabled, Is.False);
                VLabHeadPose.SetViewerRequested(true);
                Assert.That(VLabHeadPose.PhoneViewer, Is.EqualTo(Application.platform == RuntimePlatform.Android));
                Assert.That(VLabMobileVrMode.Enabled, Is.True);
                Assert.That(PlayerPrefs.GetInt(VLabHeadPose.ViewerPreferenceKey, -1), Is.EqualTo(preference));
            }
            finally { VLabHeadPose.SetViewerRequested(previous); }
        }

        [Test] public void DevicePoseRejectsMissingOrNonFiniteValuesAndNormalizesValidRotation()
        {
            Assert.That(VLabHeadPose.TryNormalizePose(new Quaternion(0, 0, 0, 0), out _), Is.False);
            Assert.That(VLabHeadPose.TryNormalizePose(new Quaternion(float.NaN, 0, 0, 1), out _), Is.False);
            Assert.That(VLabHeadPose.TryNormalizePose(new Quaternion(0, float.PositiveInfinity, 0, 1), out _), Is.False);
            Assert.That(VLabHeadPose.TryNormalizePose(new Quaternion(0, 0, 0, 2), out var normalized), Is.True);
            Assert.That(Quaternion.Angle(normalized, Quaternion.identity), Is.LessThan(.001f));
            var expected = Quaternion.Euler(12, 34, 5);
            Assert.That(VLabHeadPose.TryNormalizePose(new Quaternion(expected.x * 2, expected.y * 2, expected.z * 2, expected.w * 2), out normalized), Is.True);
            Assert.That(Quaternion.Angle(normalized, expected), Is.LessThan(.01f));
        }
    }
}
