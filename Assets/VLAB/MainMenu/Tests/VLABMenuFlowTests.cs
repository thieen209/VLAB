using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VLAB.PhysicsLab.SceneFlow;

namespace VLAB.MainMenu.Tests
{
    public sealed class VLABMenuFlowTests
    {
        private readonly Dictionary<string,string> saved=new Dictionary<string,string>();
        private static readonly string[] Keys={VLABOnboarding.LanguageKey,VLABOnboarding.TermsKey,VLABOnboarding.PrivacyKey};
        [SetUp] public void Backup()
        {
            foreach(var key in Keys){saved[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetString(key):null;PlayerPrefs.DeleteKey(key);}
        }
        [TearDown] public void Restore()
        {
            Time.timeScale=1;
            foreach(var key in Keys)if(saved[key]==null)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetString(key,saved[key]);
            PlayerPrefs.Save();
        }
        [Test] public void OnboardingRequiresBothExplicitVersionAcceptances()
        {
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");Assert.That(assets,Is.Not.Null);
            Assert.That(VLABOnboarding.Next(assets),Is.EqualTo("language"));
            VLABOnboarding.SetLanguage("vi");Assert.That(VLABOnboarding.Next(assets),Is.EqualTo("terms"));
            VLABOnboarding.Accept(false,assets);Assert.That(VLABOnboarding.Next(assets),Is.EqualTo("privacy"));
            VLABOnboarding.Accept(true,assets);Assert.That(VLABOnboarding.Next(assets),Is.EqualTo("home"));
            PlayerPrefs.SetString(VLABOnboarding.TermsKey,"older-version");Assert.That(VLABOnboarding.Next(assets),Is.EqualTo("terms"));
        }
        [UnityTest] public IEnumerator CompleteJourney_ReturnResumeAndReentry()
        {
            SceneManager.LoadScene("Menu");yield return Frames();
            Assert.That(App.CurrentScreen,Is.EqualTo("language"));
            Click("Continue");yield return Frames();Assert.That(App.CurrentScreen,Is.EqualTo("terms"));
            Assert.That(VLABOnboarding.Accepted(false,Resources.Load<VLABMenuAssets>("VLABMenuAssets")),Is.False);
            yield return Capture("01_Terms",1440,900);
            Click("NextPage");Click("ZoomIn");yield return Frames();
            Assert.That(GameObject.Find("DocumentPage").GetComponent<RawImage>().texture,Is.Not.Null);
            Click("AcceptDocument");yield return Frames();Assert.That(App.CurrentScreen,Is.EqualTo("privacy"));
            Click("AcceptDocument");yield return Frames();Assert.That(App.CurrentScreen,Is.EqualTo("home"));
            yield return Capture("02_Home_16x10",1440,900);
            yield return Capture("03_Home_4x3",1200,900);
            yield return Capture("04_Home_Phone",1280,720);
            Click("EnterLabs");yield return Frames();yield return Capture("05_Labs",1440,900);
            Assert.That(GameObject.Find("Lab_02/Enter").GetComponent<Button>().interactable,Is.True);
            Click("Lab_01/Enter");Click("Lab_01/Enter");yield return WaitLab();
            Assert.That(Find<Camera>().Length,Is.EqualTo(1));Assert.That(Find<EventSystem>().Length,Is.EqualTo(1));
            App.TogglePause();yield return Frames();Assert.That(Time.timeScale,Is.Zero);Assert.That(App.IsPaused,Is.True);
            yield return Capture("06_Pause",1440,900);
            Click("LabSettings");yield return Frames();yield return Capture("07_Settings",1440,900);
            App.Resume();yield return Frames();Assert.That(Time.timeScale,Is.EqualTo(1));
            App.TogglePause();yield return Frames();Click("ReturnHome");yield return Frames();Click("ConfirmExit");
            yield return WaitScene("Menu");yield return Frames();Assert.That(App.CurrentScreen,Is.EqualTo("home"));
            Click("EnterLabs");yield return Frames();Click("Lab_01/Enter");yield return WaitLab();
            Assert.That(Find<EventSystem>().Length,Is.EqualTo(1));Assert.That(Find<VLABApplicationUI>().Length,Is.EqualTo(1));
            PhysicsLabSceneFlow.Instance.LoadContent(PhysicsLabSceneNames.Pendulum);
            yield return WaitScene(PhysicsLabSceneNames.Pendulum);yield return Frames();
            Assert.That(Find<VLAB.PhysicsLab.Education.ExperimentPhysicalController>().Length,Is.EqualTo(1));
            yield return Capture("08_Experiment",1440,900);
        }
        private static T[] Find<T>() where T:Object => Object.FindObjectsByType<T>();
        private static VLABApplicationUI App=>Object.FindAnyObjectByType<VLABApplicationUI>();
        private static IEnumerator Frames()
        {
            for(int i=0;i<60;i++)yield return null;
            yield return new WaitForSecondsRealtime(.20f);
        }
        private static void Click(string name)
        {
            var obj=GameObject.Find(name);Assert.That(obj,Is.Not.Null,name);
            ExecuteEvents.Execute(obj,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private static IEnumerator WaitScene(string scene)
        {
            float end=Time.realtimeSinceStartup+45;
            while(!SceneManager.GetSceneByName(scene).isLoaded && Time.realtimeSinceStartup<end)yield return null;
            Assert.That(SceneManager.GetSceneByName(scene).isLoaded,Is.True,scene);
        }
        private static IEnumerator WaitLab()
        {
            yield return WaitScene(PhysicsLabSceneNames.Hub);
            float end=Time.realtimeSinceStartup+30;
            while(PhysicsLabSceneFlow.Instance.IsTransitioning && Time.realtimeSinceStartup<end)yield return null;
            yield return Frames();
        }
        private static IEnumerator Capture(string name,int width,int height)
        {
            Directory.CreateDirectory("TestResults/MenuUI");
            var camera=Camera.main;Assert.That(camera,Is.Not.Null);
            var canvas=App.GetComponentInChildren<Canvas>();
            Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.WorldSpace));
            var rt=RenderTexture.GetTemporary(width,height,24);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            camera.targetTexture=rt;
            Canvas.ForceUpdateCanvases();yield return null;yield return null;
            Canvas.ForceUpdateCanvases();
            var report=new System.Text.StringBuilder();
            foreach(var graphic in App.GetComponentsInChildren<Graphic>())
                if(graphic.GetComponent<CanvasRenderer>()!=null)
                    report.AppendLine(graphic.name+" rect="+graphic.rectTransform.rect+" pos="+graphic.rectTransform.anchoredPosition+" culled="+graphic.canvasRenderer.cull+" color="+graphic.color+" texture="+(graphic.mainTexture!=null?graphic.mainTexture.name:"none"));
            File.WriteAllText("TestResults/MenuUI/"+name+"-layout.txt",report.ToString());
            camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
            File.WriteAllBytes("TestResults/MenuUI/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);
            camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
