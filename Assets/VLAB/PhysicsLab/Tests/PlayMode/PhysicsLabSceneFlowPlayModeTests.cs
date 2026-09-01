using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VLAB.Core.Input;
using VLAB.PhysicsLab.Education;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.SceneFlow;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Mechanics;
using VLAB.PhysicsLab.AirTrack;

namespace VLAB.PhysicsLab.Tests.PlayMode
{
    public sealed class PhysicsLabSceneFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator EveryContentScene_DirectLaunchBootstrapsSharedBaseAndRuntimeServices()
        {
            foreach (var contentScene in PhysicsLabSceneNames.ContentScenes)
            {
                SceneManager.LoadScene(contentScene, LoadSceneMode.Single);
                yield return WaitForLoadedScene(PhysicsLabSceneNames.Base);
                yield return null;

                Assert.That(SceneManager.GetSceneByName(contentScene).isLoaded, Is.True, contentScene);
                Assert.That(Object.FindAnyObjectByType<PhysicsLabSceneFlow>(), Is.Not.Null, contentScene);
                Assert.That(Object.FindAnyObjectByType<InputManager>(), Is.Not.Null, contentScene);
                Assert.That(GameObject.Find("XR Origin (XR Rig)"), Is.Not.Null, contentScene);
                AssertRuntimeSingletons();
            }
        }

        [UnityTest]
        public IEnumerator BaseScene_LoadsHub_AndCyclesThroughAllExperimentsWithoutDuplicatingRig()
        {
            SceneManager.LoadScene(PhysicsLabSceneNames.Base, LoadSceneMode.Single);
            yield return WaitForLoadedScene(PhysicsLabSceneNames.Hub);

            var flow = Object.FindAnyObjectByType<PhysicsLabSceneFlow>();
            Assert.That(flow, Is.Not.Null);
            yield return WaitForTransition(flow);
            Assert.That(flow.CurrentContentScene, Is.EqualTo(PhysicsLabSceneNames.Hub));
            Assert.That(Object.FindAnyObjectByType<InputManager>(), Is.Not.Null);
            Assert.That(GameObject.Find("XR Origin (XR Rig)"), Is.Not.Null);
            AssertRuntimeSingletons();

            var experimentScenes = new[]
            {
                PhysicsLabSceneNames.Pendulum,
                PhysicsLabSceneNames.Projectile,
                PhysicsLabSceneNames.Friction,
                PhysicsLabSceneNames.PhotogateMotion,
                PhysicsLabSceneNames.Spring,
                PhysicsLabSceneNames.AirTrackMomentum,
            };

            foreach (var experimentScene in experimentScenes)
            {
                flow.LoadContent(experimentScene);
                yield return WaitForLoadedScene(experimentScene);
                yield return WaitForUnloadedScene(PhysicsLabSceneNames.Hub);
                yield return WaitForTransition(flow);

                Assert.That(SceneManager.GetSceneByName(PhysicsLabSceneNames.Base).isLoaded, Is.True);
                Assert.That(Object.FindAnyObjectByType<PhysicsLabStationController>(), Is.Not.Null, experimentScene);
                Assert.That(flow.CurrentContentScene, Is.EqualTo(experimentScene));
                AssertRuntimeSingletons();

                flow.LoadHub();
                yield return WaitForLoadedScene(PhysicsLabSceneNames.Hub);
                yield return WaitForUnloadedScene(experimentScene);
                yield return WaitForTransition(flow);

                Assert.That(flow.CurrentContentScene, Is.EqualTo(PhysicsLabSceneNames.Hub));
                AssertRuntimeSingletons();
            }
        }

        [UnityTest]
        public IEnumerator HubUiPointerClick_LoadsSelectedExperimentAndCompletesFade()
        {
            SceneManager.LoadScene(PhysicsLabSceneNames.Hub, LoadSceneMode.Single);
            yield return WaitForLoadedScene(PhysicsLabSceneNames.Base);
            yield return null;

            var flow = Object.FindAnyObjectByType<PhysicsLabSceneFlow>();
            Assert.That(flow, Is.Not.Null);
            yield return WaitForTransition(flow);

            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var experimentButton = GameObject.Find("Experiment_01")?.GetComponent<Button>();
            Assert.That(experimentButton, Is.Not.Null, "The first experiment button is missing or has no Button component.");
            Assert.That(experimentButton.interactable, Is.True);

            var pointerEvent = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
            };
            Assert.That(
                ExecuteEvents.Execute(experimentButton.gameObject, pointerEvent, ExecuteEvents.pointerClickHandler),
                Is.True,
                "The hub button did not accept a real pointer-click event.");

            yield return WaitForLoadedScene(PhysicsLabSceneNames.Pendulum);
            yield return WaitForUnloadedScene(PhysicsLabSceneNames.Hub);
            yield return WaitForTransition(flow);

            Assert.That(flow.CurrentContentScene, Is.EqualTo(PhysicsLabSceneNames.Pendulum));
            Assert.That(SceneManager.GetSceneByName(PhysicsLabSceneNames.Base).isLoaded, Is.True);
            var fade = GameObject.Find("FadeCanvas")?.GetComponent<CanvasGroup>();
            Assert.That(fade, Is.Not.Null);
            Assert.That(fade.alpha, Is.LessThanOrEqualTo(0.01f));
            Assert.That(fade.blocksRaycasts, Is.False, "The completed fade must not block later UI interaction.");
            AssertRuntimeSingletons();
        }

        [UnityTest]
        public IEnumerator EveryExperiment_UsesPhysicalControllerWithoutWizardUi()
        {
            var experimentScenes = new[]
            {
                PhysicsLabSceneNames.Pendulum,
                PhysicsLabSceneNames.Projectile,
                PhysicsLabSceneNames.Friction,
                PhysicsLabSceneNames.PhotogateMotion,
                PhysicsLabSceneNames.Spring,
                PhysicsLabSceneNames.AirTrackMomentum,
            };

            foreach (var experimentScene in experimentScenes)
            {
                SceneManager.LoadScene(experimentScene, LoadSceneMode.Single);
                yield return WaitForLoadedScene(PhysicsLabSceneNames.Base);
                yield return null;

                var controller = Object.FindAnyObjectByType<ExperimentPhysicalController>();
                Assert.That(controller, Is.Not.Null, experimentScene);
                Assert.That(Object.FindAnyObjectByType<LabSnapController>(), Is.Not.Null, experimentScene);
                Assert.That(GameObject.Find("PrimaryAction"), Is.Null, experimentScene);
                Assert.That(GameObject.Find("IncreaseParameter"), Is.Null, experimentScene);
                Assert.That(GameObject.Find("DecreaseParameter"), Is.Null, experimentScene);

                if (experimentScene == PhysicsLabSceneNames.Pendulum)
                {
                    var bob = Object.FindObjectsByType<LabGrabbable>().First(item => item.name == "Pendulum_Bob");
                    bob.OnGrabbed(bob.transform);
                    bob.OnReleased();
                    yield return new WaitForFixedUpdate();
                    Assert.That(controller.Progress.StepIndex, Is.GreaterThanOrEqualTo(2));
                }
                else if (experimentScene == PhysicsLabSceneNames.Friction)
                {
                    var block = Object.FindAnyObjectByType<FrictionBlock>();
                    var mass = Object.FindObjectsByType<LabGrabbable>().First(item => item.name == "Mass_Set");
                    var mount = block.GetComponentsInChildren<LabAttachmentPoint>(true).First(item => item.AcceptedType == "mass");
                    var initialMass = block.MassKilograms;
                    mass.transform.position = mount.transform.position;
                    Assert.That(Object.FindAnyObjectByType<LabSnapController>().TrySnap(mass), Is.True);
                    yield return new WaitForFixedUpdate();
                    Assert.That(block.MassKilograms, Is.GreaterThan(initialMass));
                }
                else if (experimentScene == PhysicsLabSceneNames.AirTrackMomentum)
                {
                    var glider = Object.FindObjectsByType<AirTrackGlider>().First();
                    var mass = Object.FindObjectsByType<LabGrabbable>().First(item => item.name == "Mass_Set");
                    var massMount = glider.GetComponentsInChildren<LabAttachmentPoint>(true).First(item => item.AcceptedType == "mass");
                    var initialMass = glider.MassKilograms;
                    mass.transform.position = massMount.transform.position;
                    Assert.That(Object.FindAnyObjectByType<LabSnapController>().TrySnap(mass), Is.True);
                    yield return new WaitForFixedUpdate();
                    Assert.That(glider.MassKilograms, Is.GreaterThan(initialMass));

                    var inelastic = Object.FindObjectsByType<LabGrabbable>().First(item => item.name == "Inelastic_Collision_Attachment");
                    var collisionMount = glider.GetComponentsInChildren<LabAttachmentPoint>(true).First(item => item.AcceptedType == "glider-collision");
                    inelastic.transform.position = collisionMount.transform.position;
                    Assert.That(Object.FindAnyObjectByType<LabSnapController>().TrySnap(inelastic), Is.True);
                    Assert.That(glider.GetComponent<GliderCollisionAttachment>().Mode, Is.EqualTo(GliderCollisionMode.Inelastic));
                }
            }
        }

        [UnityTest]
        public IEnumerator NativeXriSimpleSelection_ActivatesPhysicalApparatusExactlyOnce()
        {
            SceneManager.LoadScene(PhysicsLabSceneNames.Friction, LoadSceneMode.Single);
            yield return WaitForLoadedScene(PhysicsLabSceneNames.Base);
            yield return null;

            Assert.That(Object.FindObjectsByType<XRUIInputModule>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<InteractionRaycaster>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<GrabController>(), Is.Empty);

            var surface = Object.FindObjectsByType<LabInteractable>().First(item => item.name.StartsWith("Surface_"));
            var native = surface.GetComponent<XRSimpleInteractable>();
            var activationCount = 0;
            surface.Activated += () => activationCount++;

            native.selectEntered.Invoke(new SelectEnterEventArgs());
            native.selectExited.Invoke(new SelectExitEventArgs());

            Assert.That(activationCount, Is.EqualTo(1));
        }

        private static IEnumerator WaitForLoadedScene(string sceneName)
        {
            const int maxFrames = 600;
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    yield break;
                }
                yield return null;
            }
            Assert.Fail($"Scene '{sceneName}' did not load within {maxFrames} frames.");
        }

        private static IEnumerator WaitForUnloadedScene(string sceneName)
        {
            const int maxFrames = 600;
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    yield break;
                }
                yield return null;
            }
            Assert.Fail($"Scene '{sceneName}' did not unload within {maxFrames} frames.");
        }

        private static IEnumerator WaitForTransition(PhysicsLabSceneFlow flow)
        {
            const int maxFrames = 600;
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (!flow.IsTransitioning)
                {
                    yield break;
                }
                yield return null;
            }
            Assert.Fail($"Physics Lab transition did not finish within {maxFrames} frames.");
        }

        private static void AssertRuntimeSingletons()
        {
            Assert.That(Object.FindObjectsByType<Camera>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(), Has.Length.EqualTo(1));
        }
    }
}
