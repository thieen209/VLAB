#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.PhysicsLab.Tests.PlayMode
{
    public sealed class PhysicsLabRepairSmokeTests
    {
        [UnityTest]
        public IEnumerator ExistingMainScenes_LoadWithCameraAndNoMissingScripts()
        {
            foreach (var path in new[] { "Assets/Home.unity", "Assets/HubWorld.unity", "Assets/PhysicLab.unity" })
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                for (var frame = 0; frame < 30; frame++) yield return null;
                var scene = SceneManager.GetActiveScene();
                Assert.That(scene.path, Is.EqualTo(path));
                Assert.That(Object.FindAnyObjectByType<Camera>(), Is.Not.Null, path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var item in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject), Is.Zero, path + "/" + item.name);
            }
        }

        [UnityTest]
        public IEnumerator NativeXriSelection_ReachesBothBridgesOnceAfterReenable()
        {
            var root = new GameObject("Repair interaction test");
            var manager = root.AddComponent<XRInteractionManager>();
            var hand = new GameObject("Test interactor");
            hand.transform.SetParent(root.transform);
            hand.transform.position = Vector3.one * 1000f;
            hand.AddComponent<SphereCollider>().isTrigger = true;
            var interactor = hand.AddComponent<XRDirectInteractor>();
            interactor.interactionManager = manager;
            var grabObject = new GameObject("Test grab", typeof(BoxCollider), typeof(LabInteractable), typeof(LabGrabbable), typeof(XRGrabInteractable), typeof(XrGrabEventBridge));
            grabObject.transform.SetParent(root.transform);
            var grab = grabObject.GetComponent<XRGrabInteractable>();
            grab.interactionManager = manager;
            var labGrab = grabObject.GetComponent<LabGrabbable>();
            labGrab.Configure(true, false, LabReleaseMode.Dynamic);
            int grabbed = 0, released = 0;
            labGrab.Grabbed += _ => grabbed++;
            labGrab.Released += _ => released++;
            var simpleObject = new GameObject("Test select", typeof(BoxCollider), typeof(LabInteractable), typeof(XRSimpleInteractable), typeof(XrSimpleInteractableBridge));
            simpleObject.transform.SetParent(root.transform);
            var simple = simpleObject.GetComponent<XRSimpleInteractable>();
            simple.interactionManager = manager;
            var labSimple = simpleObject.GetComponent<LabInteractable>();
            int activated = 0;
            labSimple.Activated += () => activated++;
            yield return null;
            try
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    manager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
                    Assert.That(labGrab.IsHeld, Is.True);
                    manager.SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
                    Assert.That(labGrab.IsHeld, Is.False);
                    manager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)simple);
                    Assert.That(labSimple.IsInteracting, Is.True);
                    manager.SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)simple);
                    Assert.That(labSimple.IsInteracting, Is.False);
                    grabObject.SetActive(false);
                    simpleObject.SetActive(false);
                    grabObject.SetActive(true);
                    simpleObject.SetActive(true);
                }
                Assert.That(grabbed, Is.EqualTo(2));
                Assert.That(released, Is.EqualTo(2));
                Assert.That(activated, Is.EqualTo(2));
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}
#endif
