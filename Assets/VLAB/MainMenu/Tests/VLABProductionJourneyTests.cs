using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VLAB.Core.Input;

namespace VLAB.MainMenu.Tests
{
    public class VLABProductionJourneyTests
    {
        private uint sequence;
        private static IEnumerator Ready()
        {
            for(int i=0;i<12;i++)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
        }
        [UnityTest]
        public IEnumerator ControllerRay_SelectsUiInEveryLabIncludingPause()
        {
            foreach(var scene in new[]{"Menu","PhysicsLab_Base","ChemistryLab","BiologyLab","EngineeringLab"})
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return Ready();
                var flow=VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance;
                while(flow!=null && flow.IsTransitioning)yield return null;
                var app=Object.FindAnyObjectByType<VLABApplicationUI>();
                var pointer=Object.FindAnyObjectByType<VLabSharedPointer>();
                Assert.That(pointer,Is.Not.Null,scene);
                Assert.That(GameObject.Find("VLAB Controller Visual(Clone)"),Is.Not.Null,"Shared imported visual: "+scene);
                var input=pointer.Input;Assert.That(input,Is.Not.Null);
                var replay=pointer.gameObject.AddComponent<VLabControllerReplayProvider>();
                input.SetProvider(replay);
                var demo=Object.FindAnyObjectByType<VLAB.DemoLabs.VLabInteractionDriver>();
                if(demo!=null && demo.Hud.ModalOpen)
                {
                    yield return ClickWithRay(replay,input,demo.Hud.ModalButton.transform);
                    Assert.That(demo.Hud.ModalOpen,Is.False,"Controller starts the original lesson: "+scene);
                }
                if(scene!="Menu")app.TogglePause();
                app.Show("settings");yield return Ready();
                var button=GameObject.Find("SettingsDone").GetComponent<Button>();
                int clicks=0;button.onClick.AddListener(()=>clicks++);
                yield return ClickWithRay(replay,input,button.transform);
                Assert.That(clicks,Is.EqualTo(1),"One held trigger must produce one click: "+scene);
                Assert.That(app.CurrentScreen,Is.EqualTo(scene=="Menu"?"home":"pause"),scene);
                Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
                Assert.That(EventSystem.current.GetComponents<BaseInputModule>().Count(m=>m.isActiveAndEnabled),Is.EqualTo(1));
                yield return Ready();
                Capture(scene+"-controller");
                if(scene!="Menu")
                {
                    Assert.That(Time.timeScale,Is.Zero);
                    yield return ClickWithRay(replay,input,GameObject.Find("Resume").transform);
                    Assert.That(app.IsPaused,Is.False);
                }
            }
        }
        private IEnumerator ClickWithRay(VLabControllerReplayProvider replay,VLAB.Core.Input.InputManager input,Transform target)
        {
            var camera=Camera.main;
            var heading=Quaternion.Euler(0,camera.transform.eulerAngles.y,0);
            var origin=camera.transform.TransformPoint(new Vector3(VLabComfortSettings.Current.leftHanded?-.20f:.20f,-.17f,.60f));
            var rect=(RectTransform)target;
            var center=rect.TransformPoint(rect.rect.center);
            var rotation=Quaternion.Inverse(heading)*Quaternion.LookRotation(center-origin);
            for(int i=0;i<6;i++){if(input==null || replay==null)yield break;replay.Submit(default,rotation,++sequence);input.RefreshInput();yield return null;}
            Assert.That(replay.Connected,Is.True,"Controller heartbeat before press");
            var events=EventSystem.current;
            var hit=VLabPointerUi.Raycast(input.PointerRay(camera,Vector2.zero),new PointerEventData(events),new System.Collections.Generic.List<RaycastResult>());
            Assert.That(hit.gameObject,Is.EqualTo(target.gameObject),"Controller aims at requested UI control");
            for(int i=0;i<5;i++){replay.Submit(new VLABInputState{PrimaryPressed=true},rotation,++sequence);input.RefreshInput();yield return null;}
            for(int i=0;i<6;i++){if(input==null || replay==null)yield break;replay.Submit(default,rotation,++sequence);input.RefreshInput();yield return null;}
        }
        [UnityTest]
        public IEnumerator SupplementaryLessons_OpenResetRestoreOriginalAndReturnHome()
        {
            var scenes=new[]{"BiologyLab","EngineeringLab","EngineeringLab","ChemistryLab","ChemistryLab"};
            var activities=new[]{"VLabCellActivity","VLabGearActivity","VLabLeverActivity","VLabMoleculeActivity","VLabQualitativeActivity"};
            var parts=new[]{"Nucleus","Gear_12","LeverCheckBalance","Atom_2_O","Vessel_0"};
            for(int i=0;i<scenes.Length;i++)
            {
                yield return SceneManager.LoadSceneAsync(scenes[i]);yield return Ready();
                var app=Object.FindAnyObjectByType<VLABApplicationUI>();
                var originalCamera=Camera.main;var position=originalCamera.transform.position;
                var originalRenderers=Object.FindObjectsByType<Renderer>().Where(r=>r.enabled && !r.transform.IsChildOf(app.transform)).ToArray();
                app.StartActivity(activities[i]);yield return Ready();
                var bench=app.GetComponent<VLabActivityWorkbench>();
                Assert.That(bench.Active,Is.Not.Null);Assert.That(bench.Active.Title,Is.Not.Empty);
                Assert.That(bench.Active.Instruction,Is.Not.Empty);Assert.That(bench.Active.Completed,Is.False);
                Assert.That(Camera.main,Is.SameAs(originalCamera));Assert.That(Camera.main.transform.position,Is.EqualTo(position));
                Assert.That(Object.FindObjectsByType<AudioListener>().Count(a=>a.isActiveAndEnabled),Is.EqualTo(1));
                Assert.That(bench.Active.GetComponentsInChildren<VLAB.PhysicsLab.Common.LabInteractable>().Length,Is.GreaterThan(2));
                var part=bench.Active.GetComponentsInChildren<VLAB.PhysicsLab.Common.LabInteractable>().Single(p=>p.name==parts[i]);
                int activations=0;part.Activated+=()=>activations++;
                var shared=app.GetComponent<VLabSharedPointer>();
                var replay=shared.gameObject.AddComponent<VLabControllerReplayProvider>();shared.Input.SetProvider(replay);
                if(scenes[i]=="ChemistryLab")
                {
                    // Walk from the real room entrance to the fixed bench through normal input.
                    float end=Time.realtimeSinceStartup+2.3f;
                    while(Time.realtimeSinceStartup<end)
                    {replay.Submit(new VLABInputState{Move=Vector2.up},Quaternion.identity,++sequence);shared.Input.RefreshInput();yield return null;}
                    replay.Submit(default,Quaternion.identity,++sequence);shared.Input.RefreshInput();
                    Assert.That(Camera.main.transform.position.z-position.z,Is.GreaterThan(2),"Movement remains usable while an activity is active.");
                }
                yield return ClickWorldPart(replay,shared.Input,part);
                Assert.That(activations,Is.EqualTo(1),"Held controller trigger activates one real lesson object: "+activities[i]);
                Capture(activities[i]);
                app.TogglePause();yield return Ready();Capture(activities[i]+"-pause");
                yield return ClickWorldPart(replay,shared.Input,part,false);
                Assert.That(activations,Is.EqualTo(1),"Pause blocks apparatus input: "+activities[i]);
                app.ResetCurrentExperiment();yield return Ready();
                Assert.That(bench.Active.Completed,Is.False);Assert.That(app.IsPaused,Is.False);
                app.StartActivity("");yield return Ready();Assert.That(bench.Active,Is.Null);
                Assert.That(originalRenderers.Where(r=>r!=null).All(r=>r.enabled),Is.True,"Original lesson visuals restored: "+activities[i]);
                app.TogglePause();app.Show("exit");yield return Ready();
                yield return ClickWithRay(replay,shared.Input,GameObject.Find("ConfirmExit").transform);
                while(SceneManager.GetActiveScene().name!="Menu")yield return null;
                yield return Ready();Assert.That(Object.FindObjectsByType<VLabActivity>().Length,Is.Zero);
            }
        }
        private IEnumerator ClickWorldPart(VLabControllerReplayProvider replay,VLAB.Core.Input.InputManager input,VLAB.PhysicsLab.Common.LabInteractable part,bool verifyHit=true)
        {
            var camera=Camera.main;
            var origin=camera.transform.TransformPoint(new Vector3(VLabComfortSettings.Current.leftHanded?-.20f:.20f,-.17f,.60f));
            var collider=part.GetComponents<Collider>().First(c=>c.enabled);
            var rotation=Quaternion.Inverse(Quaternion.Euler(0,camera.transform.eulerAngles.y,0))*Quaternion.LookRotation(collider.bounds.center-origin);
            for(int i=0;i<6;i++){replay.Submit(default,rotation,++sequence);input.RefreshInput();yield return null;}
            if(verifyHit)
            {
                Assert.That(Physics.Raycast(input.PointerRay(camera,Vector2.zero),out var hit,8),Is.True);
                Assert.That(hit.collider.GetComponentInParent<VLAB.PhysicsLab.Common.LabInteractable>(),Is.EqualTo(part),"Reachable apparatus collider: "+part.name);
            }
            for(int i=0;i<5;i++){replay.Submit(new VLABInputState{PrimaryPressed=true},rotation,++sequence);input.RefreshInput();yield return null;}
            for(int i=0;i<6;i++){replay.Submit(default,rotation,++sequence);input.RefreshInput();yield return null;}
        }
        private static void Capture(string name)
        {
            var camera=Camera.main;
            if(camera==null || SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            Directory.CreateDirectory("TestResults/Production/Visuals");
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1600,1000,24);var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();File.WriteAllBytes("TestResults/Production/Visuals/"+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(texture);}
        }
        [TearDown] public void Cleanup(){Time.timeScale=1;}
    }
}
