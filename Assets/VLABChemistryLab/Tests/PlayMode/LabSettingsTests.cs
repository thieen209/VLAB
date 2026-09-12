using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public class LabSettingsTests
    {
        [UnityTest]
        public IEnumerator PreferencesAndRepeatedClicks_PreserveLessonAndBoundAudioPool()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var lesson = Object.FindAnyObjectByType<TitrationLessonController>();
            var step = lesson.Experiment.CurrentStep;
            var previous = LabPreferences.Decode(JsonUtility.ToJson(LabPreferences.Current));
            try
            {
                LabPreferences.Apply(new LabUserSettings { quality = 0, volume = .5f, pourGuide = false }, false);
                for (int i = 0; i < 20; i++) LabUiAudio.PlayClick();
                yield return null;
                var pool = Object.FindAnyObjectByType<LabUiAudio>();
                Assert.That(pool, Is.Not.Null);
                Assert.That(pool.GetComponents<AudioSource>().Length, Is.EqualTo(2));
                Assert.That(lesson.Experiment.CurrentStep, Is.EqualTo(step));
                Assert.That(LabPreferences.Current.pourGuide, Is.False);
                Assert.That(AudioListener.volume, Is.EqualTo(.5f));
            }
            finally { LabPreferences.Apply(previous, false); }
        }
    }
}
