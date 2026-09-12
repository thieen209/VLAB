using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.InputSystem;
using VLAB.ChemistryLab;

namespace VLAB.MainMenu.Tests
{
    public class VLABStabilizationTests
    {
        private readonly System.Collections.Generic.List<InputDevice> devices=new System.Collections.Generic.List<InputDevice>();
        private static IEnumerator Ready()
        {
            for (int i = 0; i < 15; i++) yield return null;
            yield return new WaitForSecondsRealtime(.25f);
        }

        [UnityTest]
        public IEnumerator BothOriginalPhysicsHandsClickSharedUiInEveryScene()
        {
            foreach(var scene in new[]{"Menu","PhysicsLab_Base","ChemistryLab","BiologyLab","EngineeringLab"})
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return Ready();
                var flow=VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance;
                while(flow!=null && flow.IsTransitioning)yield return null;
                Assert.That(Object.FindObjectsByType<XROrigin>().Length,Is.EqualTo(1),scene+" origin count");
                Assert.That(Object.FindObjectsByType<XRInteractionManager>().Length,Is.EqualTo(1),scene+" interaction owner");
                Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1),scene+" event owner");
                var hands=Camera.main.GetComponentInParent<XROrigin>().GetComponentsInChildren<NearFarInteractor>(true);
                Assert.That(hands.Length,Is.EqualTo(2),"Original two NearFar hands: "+scene);
                foreach(var usage in new[]{UnityEngine.InputSystem.CommonUsages.LeftHand,UnityEngine.InputSystem.CommonUsages.RightHand})
                {
                    var device=InputSystem.AddDevice<XRSimulatedController>();devices.Add(device);InputSystem.SetDeviceUsage(device,usage);
                    InputSystem.QueueStateEvent(device,new XRSimulatedControllerState{isTracked=true,trackingState=3,devicePosition=new Vector3(usage==UnityEngine.InputSystem.CommonUsages.LeftHand?-.2f:.2f,1.3f,0),deviceRotation=Quaternion.identity});
                }
                yield return Ready();
                var app=Object.FindAnyObjectByType<VLABApplicationUI>();
                if(scene!="Menu")app.TogglePause();
                foreach(var hand in hands)
                {
                    app.Show("settings");yield return Ready();
                    var button=GameObject.Find("SettingsDone").GetComponent<Button>();int clicks=0;
                    button.onClick.AddListener(()=>clicks++);
                    Assert.That(hand.isActiveAndEnabled,Is.True,hand.name);
                    hand.uiPressInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
                    hand.uiPressInput.manualPerformed=false;
                    var caster=(CurveInteractionCaster)hand.farInteractionCaster;
                    var origin=caster.castOrigin;
                    origin.SetPositionAndRotation(Camera.main.transform.position+Camera.main.transform.right*.12f,Quaternion.LookRotation(button.transform.position-(Camera.main.transform.position+Camera.main.transform.right*.12f)));
                    yield return Ready();
                    Assert.That(hand.TryGetCurrentUIRaycastResult(out var hit),Is.True,scene+" native ray hits UI");
                    Assert.That(hit.gameObject,Is.EqualTo(button.gameObject),scene+" native UI target");
                    hand.uiPressInput.manualPerformed=true;hand.uiPressInput.manualValue=1;
                    yield return Ready();
                    hand.uiPressInput.manualPerformed=false;hand.uiPressInput.manualValue=0;
                    yield return Ready();
                    Assert.That(clicks,Is.EqualTo(1),scene+" "+hand.name+" one native trigger click");
                }
                if(app.IsPaused)app.TogglePause();
                if(scene!="Menu")
                {
                    var device=(XRSimulatedController)devices[0];
                    var state=new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity};
                    state.buttons=(ushort)(1<<(int)ControllerButton.MenuButton);
                    InputSystem.QueueStateEvent(device,state);yield return Ready();Assert.That(app.IsPaused,Is.True,scene+" native menu opens pause");
                    state.buttons=0;InputSystem.QueueStateEvent(device,state);yield return Ready();
                    state.buttons=(ushort)(1<<(int)ControllerButton.MenuButton);InputSystem.QueueStateEvent(device,state);yield return Ready();Assert.That(app.IsPaused,Is.False,scene+" native menu resumes");
                    state.buttons=0;InputSystem.QueueStateEvent(device,state);yield return Ready();
                }
                foreach(var device in devices)if(device.added)InputSystem.RemoveDevice(device);devices.Clear();
            }
        }

        [UnityTest]
        public IEnumerator OriginalNativeHandsPickAndPlaceGuidedLessonItems()
        {
            foreach(var scene in new[]{"BiologyLab","EngineeringLab"})
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return Ready();
                var driver=Object.FindAnyObjectByType<VLAB.DemoLabs.VLabInteractionDriver>();
                driver.Hud.ModalButton.onClick.Invoke();
                var hand=Camera.main.GetComponentInParent<XROrigin>().GetComponentsInChildren<NearFarInteractor>(true)[0];
                var device=InputSystem.AddDevice<XRSimulatedController>();devices.Add(device);InputSystem.SetDeviceUsage(device,UnityEngine.InputSystem.CommonUsages.LeftHand);
                InputSystem.QueueStateEvent(device,new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity});yield return Ready();
                // Follow each lesson's valid first placement; Biology rejects water before a slide.
                var zone=scene=="BiologyLab"?Object.FindAnyObjectByType<VLAB.DemoLabs.BiologyExperiment>().PreparationZone:Object.FindAnyObjectByType<VLAB.DemoLabs.EngineeringExperiment>().ResistorZone;
                var item=driver.Items.First(i=>i.Kind==zone.AcceptedKind);
                var target=item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                Assert.That(target,Is.Not.Null,"Guided items use the shared native XRI route.");
                hand.interactionManager.SelectEnter((IXRSelectInteractor)hand,target);
                Assert.That(driver.Held,Is.SameAs(item),scene+" native selection picks real item");
                hand.interactionManager.SelectExit((IXRSelectInteractor)hand,target);
                var socket=zone.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                hand.interactionManager.SelectEnter((IXRSelectInteractor)hand,socket);
                Assert.That(zone.Occupant,Is.SameAs(item),scene+" native selection fits the actual matching socket");
                hand.interactionManager.SelectExit((IXRSelectInteractor)hand,socket);
                Assert.That(driver.Held,Is.Null);
                Object.FindAnyObjectByType<VLABApplicationUI>().StartActivity(scene=="BiologyLab"?nameof(VLabCellActivity):nameof(VLabGearActivity));
                Assert.That(driver.SelectTarget(item),Is.False,"Old guided apparatus cannot respond during another lesson.");
                InputSystem.RemoveDevice(device);devices.Clear();
            }
        }

        [UnityTest]
        public IEnumerator PauseRemainsInFrontOfNearbyGeometryAndItsButtonsStayReachable()
        {
            foreach(var scene in new[]{"PhysicsLab_Base","ChemistryLab","BiologyLab","EngineeringLab"})
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return Ready();
                var flow=VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance;
                while(flow!=null && flow.IsTransitioning)yield return null;
                var app=Object.FindAnyObjectByType<VLABApplicationUI>();var view=Camera.main;
                foreach(float distance in new[]{1.2f,.65f,.35f})
                {
                    var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="Pause regression wall at "+distance;
                    barrier.transform.SetPositionAndRotation(view.transform.position+view.transform.forward*distance,view.transform.rotation);
                    barrier.transform.localScale=new Vector3(4,4,.10f);
                    var pose=view.transform.position;var rotation=view.transform.rotation;
                    Physics.SyncTransforms();app.TogglePause();yield return Ready();
                    var canvas=(RectTransform)app.transform.Find("VLAB Canvas");
                    Assert.That(Vector3.Distance(view.transform.position,canvas.position),Is.LessThan(distance-.05f),scene+" panel must stay in front of wall");
                    Assert.That(view.transform.position,Is.EqualTo(pose));Assert.That(Quaternion.Angle(rotation,view.transform.rotation),Is.LessThan(.1f));
                    var target=GameObject.Find("Resume").transform;
                    var ray=new Ray(view.transform.position,(target.position-view.transform.position).normalized);
                    var hit=VLAB.Core.Input.VLabPointerUi.Raycast(ray,new PointerEventData(EventSystem.current),new System.Collections.Generic.List<RaycastResult>());
                    Assert.That(hit.gameObject,Is.EqualTo(target.gameObject),scene+" visible Resume remains clickable beside geometry");
                    var corners=new Vector3[4];canvas.GetWorldCorners(corners);
                    foreach(var corner in corners)
                    {
                        var direction=corner-view.transform.position;
                        Assert.That(barrier.GetComponent<Collider>().Raycast(new Ray(view.transform.position,direction),out _,direction.magnitude),Is.False,"Wall must not cover menu corners");
                    }
                    app.TogglePause();Object.Destroy(barrier);yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator PhysicsSharedSelectionCyclesSixStationsWithoutMovingPlayer()
        {
            yield return SceneManager.LoadSceneAsync("PhysicsLab_Base");yield return Ready();
            var flow=VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance;
            while(flow.IsTransitioning)yield return null;
            Assert.That(GameObject.Find("ExperimentSelectorCanvas"),Is.Null,"Legacy giant hub is inactive.");
            Assert.That(GameObject.Find("Physics entry prompt"),Is.Not.Null);
            Capture("Physics-entry");
            var app=Object.FindAnyObjectByType<VLABApplicationUI>();var view=Camera.main;
            var pose=view.transform.position;var rotation=view.transform.rotation;
            for(int cycle=0;cycle<2;cycle++)for(int i=1;i<=6;i++)
            {
                app.SelectExperiment();yield return Ready();
                var button=GameObject.Find("Experiment_"+i.ToString("00")).GetComponent<Button>();
                ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                yield return null;while(flow.IsTransitioning)yield return null;yield return Ready();
                Assert.That(Object.FindObjectsByType<VLAB.PhysicsLab.Education.ExperimentPhysicalController>().Length,Is.EqualTo(1));
                Assert.That(Vector3.Distance(view.transform.position,pose),Is.LessThan(.02f));
                Assert.That(Quaternion.Angle(view.transform.rotation,rotation),Is.LessThan(.1f));
                Assert.That(Object.FindObjectsByType<XROrigin>().Length,Is.EqualTo(1));
                if(cycle==0)Capture("Physics-"+i.ToString("00"));
            }
        }

        private static void Capture(string name)
        {
            var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1600,1000,24);var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
                System.IO.Directory.CreateDirectory("TestResults/Stabilization/Visuals");
                System.IO.File.WriteAllBytes("TestResults/Stabilization/Visuals/"+name+".png",image.EncodeToPNG());
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(image);}
        }

        [UnityTest]
        public IEnumerator ChemistryActivitiesKeepFloorRoomMovementAndStation()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab");
            yield return Ready();
            var app = Object.FindAnyObjectByType<VLABApplicationUI>();
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            var floor = GameObject.Find("Floor").GetComponent<Collider>();
            var room = floor.GetComponent<Renderer>();
            var camera = Camera.main;
            var pose = camera.transform.position;
            app.StartActivity(nameof(VLabMoleculeActivity));
            yield return Ready();
            Assert.That(floor.enabled, Is.True, "Opening a lesson must retain the real floor collider.");
            Assert.That(room.enabled, Is.True, "Opening a lesson must retain the room.");
            Assert.That(navigator.enabled, Is.True, "Opening a lesson must retain locomotion.");
            Assert.That(Vector3.Distance(camera.transform.position, pose), Is.LessThan(.02f));
            var station = app.transform.Find("VLAB Learning Workbench");
            var stationPosition = station.position;
            camera.transform.rotation = Quaternion.Euler(12, 80, 0);
            var rotation = camera.transform.rotation;
            app.StartActivity(nameof(VLabQualitativeActivity));
            yield return Ready();
            Assert.That(app.transform.Find("VLAB Learning Workbench").position, Is.EqualTo(stationPosition), "The station is a lab anchor, not the gaze.");
            Assert.That(Quaternion.Angle(camera.transform.rotation, rotation), Is.LessThan(.1f));
            Assert.That(floor.enabled && navigator.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PauseRecentersAfterHeadTurnWithoutMovingPlayer()
        {
            yield return SceneManager.LoadSceneAsync("BiologyLab");
            yield return Ready();
            var app = Object.FindAnyObjectByType<VLABApplicationUI>();
            var view = Camera.main.transform;
            app.TogglePause();
            var pose = view.position;
            view.rotation = Quaternion.Euler(0, 90, 0);
            yield return new WaitForSecondsRealtime(1.2f);
            var canvas = app.transform.Find("VLAB Canvas");
            Assert.That(Vector3.Angle(view.forward, canvas.position - view.position), Is.LessThan(10));
            Assert.That(view.position, Is.EqualTo(pose));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [TearDown] public void Cleanup() { Time.timeScale = 1;foreach(var device in devices)if(device.added)InputSystem.RemoveDevice(device);devices.Clear(); }
    }
}
