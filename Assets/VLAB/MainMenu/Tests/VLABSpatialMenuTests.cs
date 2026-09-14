using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VLAB.PhysicsLab.Education;
using VLAB.PhysicsLab.SceneFlow;
using VLAB.PhysicsLab.Interaction;
using VLAB.Core.Input;

namespace VLAB.MainMenu.Tests
{
    public sealed class VLABSpatialMenuTests
    {
        private readonly Dictionary<string,string> strings=new Dictionary<string,string>();
        private bool sound,guidance;
        private int hints;
        private float sensitivity;
        private static VLABApplicationUI App=>Object.FindAnyObjectByType<VLABApplicationUI>();

        [UnityTest] public IEnumerator ControllerVisual_PreservesAuthoredBasisAndFullCalibratedPose()
        {
            SceneManager.LoadScene("Menu");yield return Ready();
            var pointer=App.GetComponent<VLabSharedPointer>();
            pointer.ToggleReplay();
            var provider=App.GetComponent<VLabControllerReplayProvider>();
            var visual=(GameObject)typeof(VLabSharedPointer).GetField("model",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pointer);
            var basis=Resources.Load<VLABMenuAssets>("VLABMenuAssets").controllerVisual.transform.localRotation;
            // Invoke rendering directly so keyboard replay cannot replace the test packets.
            var render=typeof(VLabSharedPointer).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            uint packet=100;
            foreach(var angles in new[]{new Vector3(0,30,0),new Vector3(0,-30,0),new Vector3(30,0,0),new Vector3(-30,0,0),new Vector3(0,0,45),new Vector3(0,0,-45),new Vector3(25,40,35),new Vector3(0,179,0),new Vector3(0,181,0)})
            {
                var rotation=Quaternion.Euler(angles);
                Assert.That(provider.Submit(default,rotation,++packet),Is.True);
                Assert.That(provider.TryGetRay(Camera.main,out var ray),Is.True);
                render.Invoke(pointer,null);
                Assert.That(Quaternion.Angle(visual.transform.rotation,provider.WorldRotation*basis),Is.LessThan(.01f));
                Assert.That(Vector3.Angle(ray.direction,provider.WorldRotation*Vector3.forward),Is.LessThan(.01f));
            }
            provider.Calibrate();provider.TryGetRay(Camera.main,out var centered);
            Assert.That(Vector3.Angle(centered.direction,Quaternion.Euler(0,Camera.main.transform.eulerAngles.y,0)*Vector3.forward),Is.LessThan(.01f));
            pointer.ToggleReplay();
        }

        [SetUp] public void Setup()
        {
            foreach(var key in new[]{VLABOnboarding.LanguageKey,VLABOnboarding.TermsKey,VLABOnboarding.PrivacyKey})
                strings[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetString(key):null;
            sound=PhysicsLabPreferences.SoundEnabled;guidance=PhysicsLabPreferences.GuidanceEnabled;
            hints=PhysicsLabPreferences.GuidanceLevel;sensitivity=PhysicsLabPreferences.MouseSensitivity;
            VLABOnboarding.SetLanguage("en");
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            VLABOnboarding.Accept(false,assets);VLABOnboarding.Accept(true,assets);
            Directory.CreateDirectory("TestResults/SpatialUI");
        }
        [TearDown] public void Restore()
        {
            Time.timeScale=1;
            foreach(var pair in strings)if(pair.Value==null)PlayerPrefs.DeleteKey(pair.Key);else PlayerPrefs.SetString(pair.Key,pair.Value);
            PhysicsLabPreferences.SoundEnabled=sound;PhysicsLabPreferences.GuidanceEnabled=guidance;
            PhysicsLabPreferences.GuidanceLevel=hints;PhysicsLabPreferences.MouseSensitivity=sensitivity;
            PlayerPrefs.Save();
        }

        [UnityTest] public IEnumerator TrackedRay_NavigationSettingsDocumentsAndPause()
        {
            SceneManager.LoadScene("Menu");yield return Ready();
            var canvas=App.GetComponentInChildren<Canvas>();
            Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.GetComponent<TrackedDeviceGraphicRaycaster>(),Is.Not.Null);
            Assert.That(Object.FindObjectsByType<XRUIInputModule>().Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include).Length,Is.GreaterThanOrEqualTo(2));
            var pose=canvas.transform.position;
            Camera.main.transform.position+=Vector3.right*.1f;yield return null;
            Assert.That(canvas.transform.position,Is.EqualTo(pose),"Menu must remain anchored when the head moves.");
            Camera.main.transform.position-=Vector3.right*.1f;

            yield return RayClick("Settings","settings");
            bool oldGuidance=PhysicsLabPreferences.GuidanceEnabled;
            yield return RayClick("Setting_Action guidance","settings");
            Assert.That(PhysicsLabPreferences.GuidanceEnabled,Is.Not.EqualTo(oldGuidance));
            int oldHints=PhysicsLabPreferences.GuidanceLevel;
            yield return RayClick("Setting_Contextual hints","settings");
            Assert.That(PhysicsLabPreferences.GuidanceLevel,Is.Not.EqualTo(oldHints));
            float oldSensitivity=PhysicsLabPreferences.MouseSensitivity;
            yield return RayClick("Setting_Mouse sensitivity","settings");
            Assert.That(PhysicsLabPreferences.MouseSensitivity,Is.Not.EqualTo(oldSensitivity));
            yield return RayClick("SoundTab","settings");
            bool oldSound=PhysicsLabPreferences.SoundEnabled;
            yield return RayClick("Setting_Application sound","settings");
            Assert.That(AudioListener.volume,Is.EqualTo(oldSound?0:1));
            yield return RayClick("LanguageTab","settings");
            yield return RayClick("Setting_Interface language","settings");
            Assert.That(VLABOnboarding.Language,Is.EqualTo("vi"));
            yield return RayClick("Setting_Ngôn ngữ giao diện","settings");
            Assert.That(VLABOnboarding.Language,Is.EqualTo("en"));
            yield return RayClick("LearningTab","settings");
            yield return RayClick("SettingsDone","home");
            yield return RayClick("Help","help");yield return RayClick("Back","home");
            yield return RayClick("About","about");yield return RayClick("ViewTerms","terms-read");
            var first=GameObject.Find("DocumentPage").GetComponent<RawImage>().texture;
            yield return RayClick("NextPage","terms-read");
            Assert.That(GameObject.Find("DocumentPage").GetComponent<RawImage>().texture,Is.Not.EqualTo(first));
            yield return RayClick("PreviousPage","terms-read");
            yield return ScrollDocument();
            yield return RayClick("ZoomIn","terms-read");
            Assert.That(GameObject.Find("DocumentViewer").GetComponent<ScrollRect>().horizontal,Is.True);
            yield return RayClick("ZoomOut","terms-read");
            Assert.That(GameObject.Find("ZoomOut").GetComponent<Button>().interactable,Is.False);
            yield return RayClick("Back","about");yield return RayClick("ViewPrivacy","privacy-read");
            yield return RayClick("Back","about");yield return RayClick("Back","home");
            yield return RayClick("Quit","quit");yield return RayClick("CancelQuit","home");
            yield return RayClick("EnterLabs","labs");
            foreach(var name in new[]{"Lab_02/Enter","Lab_03/Enter","Lab_04/Enter"})
                Assert.That(GameObject.Find(name).GetComponent<Button>().interactable,Is.True);
            yield return RayClick("Back","home");yield return RayClick("EnterLabs","labs");
            yield return RayClick("Lab_01/Enter");
            yield return WaitLab();
            yield return RayClick("OpenLabMenu","pause");
            yield return RayClick("LabSettings","settings");
            App.NavigateBack();yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("pause"));Assert.That(Time.timeScale,Is.Zero);
            yield return RayClick("LabHelp","help");yield return RayClick("Back","pause");
            yield return RayClick("ReturnHome","exit");yield return RayClick("Back","pause");
            yield return RayClick("ReturnHome","exit");yield return RayClick("KeepLearning","hud");
            Assert.That(Time.timeScale,Is.EqualTo(1));
            App.NavigateBack();yield return Ready();
            yield return RayClick("Resume","hud");
            App.NavigateBack();yield return Ready();
            yield return RayClick("ReturnHome","exit");yield return RayClick("ConfirmExit");
            yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("home"));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator MissingScene_ShowsRecoverableError_AndPreservesPause()
        {
            SceneManager.LoadScene(PhysicsLabSceneNames.Base);yield return WaitLab();
            App.TogglePause();yield return Ready();App.Show("exit");yield return Ready();
            var method=typeof(VLABApplicationUI).GetMethod("LoadScene",BindingFlags.NonPublic|BindingFlags.Instance);
            // Deliberately invalid test input, never a shipped scene binding.
            App.StartCoroutine((IEnumerator)method.Invoke(App,new object[]{"__MissingSceneForFailureTest__"}));
            yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("error"));
            Assert.That(App.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
            yield return RayClick("RetryLoad","error");yield return RayClick("Back","exit");
            yield return RayClick("KeepLearning","hud");Assert.That(Time.timeScale,Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator PauseInput_IsolatesHiddenRaycasters_AndRestoresCursor()
        {
            SceneManager.LoadScene(PhysicsLabSceneNames.Base);yield return WaitLab();
            var input=Object.FindAnyObjectByType<InputManager>();
            var provider=new PauseProvider();input.SetProvider(provider);
            var raycasters=new Dictionary<BaseRaycaster,bool>();
            foreach(var r in Object.FindObjectsByType<BaseRaycaster>())
                if(!r.transform.IsChildOf(App.transform))raycasters[r]=r.enabled;
            Assert.That(raycasters.Count,Is.GreaterThan(0));
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;yield return Ready();
            var expectedCursor=Cursor.lockState; // A batch Editor may release a lock when its Game view lacks focus.
            provider.pause=true;input.RefreshInput();yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("pause"));
            foreach(var r in raycasters.Keys)Assert.That(r.enabled,Is.False);
            App.Show("settings");yield return Ready();
            provider.pause=false;input.RefreshInput();provider.pause=true;input.RefreshInput();yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("pause"));Assert.That(App.IsPaused,Is.True);
            provider.pause=false;input.RefreshInput();provider.pause=true;input.RefreshInput();yield return Ready();
            Assert.That(App.CurrentScreen,Is.EqualTo("hud"));Assert.That(Cursor.lockState,Is.EqualTo(expectedCursor));
            foreach(var pair in raycasters)Assert.That(pair.Key.enabled,Is.EqualTo(pair.Value));
            input.SetProvider(Object.FindAnyObjectByType<SimulatorInputProvider>());Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }

        [UnityTest] public IEnumerator MissingDocuments_CanRetryAndBack_CannotAcceptOrZoom()
        {
            SceneManager.LoadScene("Menu");yield return Ready();
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            var original=assets.termsEn;
            try
            {
                assets.termsEn=System.Array.Empty<string>();App.Show("terms");yield return Ready();
                Assert.That(GameObject.Find("MissingDocument"),Is.Not.Null);
                yield return RayClick("LegalBack","language");
                yield return RayClick("Vietnamese","language");
                Assert.That(GameObject.Find("Vietnamese").GetComponentInChildren<Text>().text,Does.Contain("Đã chọn"));
                yield return RayClick("English","language");
                Assert.That(GameObject.Find("English").GetComponentInChildren<Text>().text,Does.Contain("Selected"));
                yield return RayClick("Continue","home");
                App.Show("terms");yield return Ready();
                assets.termsEn=original;yield return RayClick("Retry","terms");
                Assert.That(GameObject.Find("AcceptDocument").GetComponent<Button>().interactable,Is.True);
                assets.termsEn=new[]{"__MissingDocumentForFailureTest__"};App.Show("terms");yield return Ready();
                foreach(var name in new[]{"AcceptDocument","ZoomIn","ZoomOut"})
                    Assert.That(GameObject.Find(name).GetComponent<Button>().interactable,Is.False,name);
                yield return RayClick("LegalBack","language");
            }
            finally {assets.termsEn=original;}
        }

        private sealed class PauseProvider : IVLABInputProvider
        {
            public bool pause;
            public string ProviderName=>"Menu regression fixture";
            public bool InteractionPressed=>false;
            public bool ResetPressed=>false;
            public VLABInputState ReadState()=>new VLABInputState{PausePressed=pause};
        }

        [UnityTest] public IEnumerator ProductionMenu_PreservesAvailableLabs_AndNavigationReturns()
        {
            SceneManager.LoadScene("Menu");yield return Ready();
            Assert.That(App,Is.Not.Null);
            Assert.That(App.CurrentScreen,Is.EqualTo("home"));
            yield return RayClick("EnterLabs","labs");
            foreach(var index in new[]{"01","02","03","04"})
            {
                var button=GameObject.Find("Lab_"+index+"/Enter")?.GetComponent<Button>();
                Assert.That(button,Is.Not.Null,index);
                Assert.That(button.IsInteractable(),Is.True,index);
                var title=button.transform.parent.Find("Name").GetComponent<Text>();
                Assert.That(title.text,Is.Not.Empty);
                Assert.That(title.text,Is.Not.EqualTo("Button"));
            }
            yield return RayClick("Back","home");
            yield return RayClick("Settings","settings");
            yield return RayClick("SettingsDone","home");
            yield return RayClick("EnterLabs","labs");
            yield return RayClick("Lab_01/Enter");yield return WaitLab();
            Assert.That(App,Is.Not.Null);
            Assert.That(SceneManager.GetSceneByName(PhysicsLabSceneNames.Base).isLoaded,Is.True);
            Assert.That(SceneManager.GetSceneByName(PhysicsLabSceneNames.Hub).isLoaded,Is.True);
            yield return RayClick("OpenLabMenu","pause");
            yield return RayClick("ReturnHome","exit");
            yield return RayClick("ConfirmExit");yield return Ready();
            Assert.That(SceneManager.GetSceneByName("Menu").isLoaded,Is.True);
            Assert.That(App.CurrentScreen,Is.EqualTo("home"));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
        }

        private sealed class TestRay : IUIInteractor
        {
            public Vector3 origin,target;
            public bool pressed;
            public Vector2 scroll;
            public TrackedDeviceModel last;
            public void UpdateUIModel(ref TrackedDeviceModel model)
            {
                last=model;
                model.position=origin;model.orientation=Quaternion.LookRotation(target-origin);
                model.raycastPoints=new List<Vector3>{origin,target+(target-origin).normalized*.1f};
                model.raycastLayerMask=~0;model.interactionType=UIInteractionType.Ray;model.select=pressed;
                model.scrollDelta=scroll;
            }
            public bool TryGetUIModel(out TrackedDeviceModel model){model=last;return true;}
        }
        private static IEnumerator ScrollDocument()
        {
            var scroll=GameObject.Find("DocumentViewer").GetComponent<ScrollRect>();
            float before=scroll.verticalNormalizedPosition;
            var rect=(RectTransform)scroll.transform;
            var ray=new TestRay{origin=Camera.main.transform.position,target=rect.TransformPoint(rect.rect.center),scroll=new Vector2(0,-1)};
            var module=Object.FindAnyObjectByType<XRUIInputModule>();module.RegisterInteractor(ray);
            try{for(int i=0;i<5;i++)yield return null;}
            finally{module.UnregisterInteractor(ray);}
            Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(before),"Tracked controller scrolling must move the document.");
            File.AppendAllText("TestResults/SpatialUI/tracked-ray-actions.tsv","terms-read\tDocumentViewer scroll\tterms-read\tPASS\n");
        }
        private static IEnumerator RayClick(string name,string expected=null)
        {
            Canvas.ForceUpdateCanvases();
            var obj=GameObject.Find(name);Assert.That(obj,Is.Not.Null,name);
            var button=obj.GetComponent<Button>();Assert.That(button,Is.Not.Null,name);Assert.That(button.IsInteractable(),Is.True,name);
            var rect=(RectTransform)obj.transform;
            var ray=new TestRay{origin=Camera.main.transform.position,target=rect.TransformPoint(rect.rect.center)};
            var module=Object.FindAnyObjectByType<XRUIInputModule>();Assert.That(module,Is.Not.Null);
            int calls=0;button.onClick.AddListener(()=>calls++);
            string before=App.CurrentScreen;
            module.RegisterInteractor(ray);
            try
            {
                for(int i=0;i<4;i++)yield return null;
                Assert.That(ray.last.currentRaycast.gameObject,Is.EqualTo(obj),"Tracked ray must hit "+name);
                ray.pressed=true;for(int i=0;i<3;i++)yield return null;
                ray.pressed=false;for(int i=0;i<3;i++)yield return null;
                Assert.That(calls,Is.EqualTo(1),name+" must execute exactly once");
            }
            finally {if(module!=null)module.UnregisterInteractor(ray);}
            yield return Ready();
            if(expected!=null)Assert.That(App.CurrentScreen,Is.EqualTo(expected),name);
            File.AppendAllText("TestResults/SpatialUI/tracked-ray-actions.tsv",before+"\t"+name+"\t"+(App!=null?App.CurrentScreen:"loading")+"\tPASS\n");
        }
        [System.Serializable] private sealed class ControlRecord
        {
            public string scene,state,path,component,handlerSource,canvasMode,camera;
            public bool active,enabled,interactable,raycastTarget;
            public int persistentCalls;
        }
        private static void RecordScreen()
        {
            if(App==null)return;
            var canvas=App.GetComponentInChildren<Canvas>();
            foreach(var button in App.GetComponentsInChildren<Button>())
            {
                string path=button.name;
                for(var t=button.transform.parent;t!=null;t=t.parent)path=t.name+"/"+path;
                var row=new ControlRecord{scene=App.gameObject.scene.name,state=App.CurrentScreen,path=path,
                    component=button.GetType().FullName,handlerSource="VLABApplicationUI -> VLABUI.Button -> onClick runtime delegate",
                    canvasMode=canvas.renderMode.ToString(),camera=canvas.worldCamera!=null?canvas.worldCamera.name:"NULL",
                    active=button.gameObject.activeInHierarchy,enabled=button.enabled,interactable=button.IsInteractable(),
                    raycastTarget=button.targetGraphic!=null && button.targetGraphic.raycastTarget,
                    persistentCalls=button.onClick.GetPersistentEventCount()};
                File.AppendAllText("TestResults/SpatialUI/runtime-controls.jsonl",JsonUtility.ToJson(row)+"\n");
            }
        }
        private static IEnumerator Ready()
        {
            for(int i=0;i<20;i++)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            RecordScreen();
        }
        private static IEnumerator WaitLab()
        {
            float end=Time.realtimeSinceStartup+40;
            while((!SceneManager.GetSceneByName(PhysicsLabSceneNames.Hub).isLoaded || PhysicsLabSceneFlow.Instance==null || PhysicsLabSceneFlow.Instance.IsTransitioning) && Time.realtimeSinceStartup<end)yield return null;
            Assert.That(SceneManager.GetSceneByName(PhysicsLabSceneNames.Hub).isLoaded,Is.True);yield return Ready();
        }
    }
}
