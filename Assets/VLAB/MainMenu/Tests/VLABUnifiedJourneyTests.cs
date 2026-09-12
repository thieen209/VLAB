using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VLAB.Core.Input;

namespace VLAB.MainMenu.Tests
{
    public class VLABUnifiedJourneyTests
    {
        private static VLABApplicationUI App => Object.FindAnyObjectByType<VLABApplicationUI>();
        private string[] saved;
        private static readonly string[] keys={VLABOnboarding.LanguageKey,VLABOnboarding.TermsKey,VLABOnboarding.PrivacyKey};
        [SetUp] public void Setup()
        {
            saved=keys.Select(k=>PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):null).ToArray();
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            VLABOnboarding.SetLanguage("vi");VLABOnboarding.Accept(false,assets);VLABOnboarding.Accept(true,assets);
        }
        [TearDown] public void Cleanup()
        {
            Time.timeScale=1;
            for(int i=0;i<keys.Length;i++)if(saved[i]==null)PlayerPrefs.DeleteKey(keys[i]);else PlayerPrefs.SetString(keys[i],saved[i]);
            PlayerPrefs.Save();
        }
        private static IEnumerator Ready()
        {
            for(int i=0;i<8;i++)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
        }
        private static void Click(string name)
        {
            var button=GameObject.Find(name)?.GetComponent<Button>();
            Assert.That(button,Is.Not.Null,name);Assert.That(button.IsInteractable(),Is.True,name);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private static IEnumerator WaitScene(string scene)
        {
            var deadline=Time.realtimeSinceStartup+60;
            while(!SceneManager.GetSceneByName(scene).isLoaded && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(SceneManager.GetSceneByName(scene).isLoaded,Is.True,scene);
            yield return Ready();
        }
        [UnityTest] public IEnumerator EveryLab_ReturnsAndReentersWithoutDuplicateOwners()
        {
            yield return SceneManager.LoadSceneAsync("Menu");yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("home"));
#if UNITY_EDITOR && ENABLE_VR
            var simulator=UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator.instance;
            Assert.That(simulator==null || !simulator.gameObject.activeSelf,Is.True,"Menu head simulation must not compete with the package controller simulator.");
            Assert.That(EventSystem.current.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>().enableMouseInput,Is.True,"The Editor menu must accept real mouse clicks.");
#endif
            var head=Camera.main.GetComponent<VLabHeadPose>();Assert.That(head,Is.Not.Null);
            var position=Camera.main.transform.position;var rotation=Camera.main.transform.rotation;
            head.Simulate(25,-12);Assert.That(Quaternion.Angle(rotation,Camera.main.transform.rotation),Is.GreaterThan(20));
            Assert.That(Camera.main.transform.position,Is.EqualTo(position));
            head.Recenter();Assert.That(Quaternion.Angle(rotation,Camera.main.transform.rotation),Is.LessThan(.01f));
            Assert.That(Object.FindObjectsByType<VLAB.Core.Input.InputManager>().All(i=>i.TranslationLocked),Is.True);
            Assert.That(Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>().All(i=>!i.enabled),Is.True);
            Capture("home");
            var scenes=new[]{"PhysicsLab_Base","ChemistryLab","BiologyLab","EngineeringLab","ChemistryLab"};
            var cards=new[]{"01","02","03","04","02"};
            for(int i=0;i<scenes.Length;i++)
            {
                App.Show("labs");yield return Ready();Capture("labs");Click("Lab_"+cards[i]+"/Enter");
                yield return WaitScene(scenes[i]);
                if(i==0)
                {
                    yield return WaitScene("PhysicsLab_Hub");
                    while(VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance.IsTransitioning)yield return null;
                }
                Assert.That(Object.FindObjectsByType<VLABApplicationUI>().Length,Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<AudioListener>().Count(a=>a.isActiveAndEnabled),Is.EqualTo(1));
                Capture(scenes[i]);
                if(scenes[i]=="ChemistryLab")
                {
                    App.Show("chemistry");yield return Ready();Click("ChemAction_0");yield return Ready();
                    var hub=Object.FindAnyObjectByType<VLAB.ChemistryLab.ChemistryLabLessonHub>();
                    Assert.That(hub.Titration.LastActionAccepted,Is.True,"Spatial PPE button must reach the real lesson controller.");
                    Click("ChemAction_8");yield return Ready();Capture("chemistry-spatial-titration");
                    foreach(var lessonIndex in new[]{1,2})
                    {
                        Click("ChemLesson_"+lessonIndex);yield return Ready();var lesson=hub.ActiveConfigurable;
                        Click("ChemAdvance");yield return Ready();Assert.That(lesson.CompletedSteps,Is.EqualTo(1));
                        Click("ChemReset");yield return Ready();Assert.That(lesson.CompletedSteps,Is.Zero);
                        Capture("chemistry-spatial-lesson-"+lessonIndex);
                    }
                    Click("ChemLesson_0");yield return Ready();Click("ChemAction_11");yield return Ready();
                }
                App.TogglePause();yield return Ready();Assert.That(App.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
                Assert.That(Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>().All(p=>!p.enabled),Is.True,"Pause must stop snap-turn and locomotion independently of head tracking.");
                var bridge=Object.FindAnyObjectByType<VLAB.ChemistryLab.ChemistryControllerBridge>();
                if(bridge!=null)
                {
                    var lesson=Object.FindAnyObjectByType<VLAB.ChemistryLab.TitrationLessonController>();
                    var step=lesson.Experiment.CurrentStep;bridge.ReceiveCommand("safety");
                    Assert.That(lesson.Experiment.CurrentStep,Is.EqualTo(step),"Paused hardware commands must not advance the lesson.");
                }
                App.Show("settings");yield return Ready();Capture(scenes[i]+"-settings");
                if(i==0)
                {
                    foreach(var tab in new[]{"SoundTab","ViewerTab","GraphicsTab"})
                    { Click(tab);yield return Ready();Capture("settings-"+tab); }
                    Click("LearningTab");
                }
                App.Resume();yield return Ready();Assert.That(Time.timeScale,Is.EqualTo(1));
                App.TogglePause();yield return Ready();Click("ReturnHome");yield return Ready();Click("ConfirmExit");
                yield return WaitScene("Menu");Assert.That(App.CurrentScreen,Is.EqualTo("home"));
            }
        }
        private static void Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            Directory.CreateDirectory("TestResults/Unified");
            var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1440,900,24);var texture=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,1440,900),0,0);texture.Apply();File.WriteAllBytes("TestResults/Unified/"+name+".png",texture.EncodeToPNG());
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(texture);}
        }
    }
}
