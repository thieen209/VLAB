using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VLAB.ChemistryLab.Interaction;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public sealed class GrabRecoveryTests
    {
        [UnityTest]
        public IEnumerator TwoHandsHoldIndependentlyAndRecoveryRejectsHeldObjects()
        {
            var root = new GameObject("Recovery test");
            var manager = root.AddComponent<XRInteractionManager>();
            var left = Hand(root, manager, "Left");
            var right = Hand(root, manager, "Right");
            var a = Item(root, manager, new Vector3(0, 2, 0));
            var b = Item(root, manager, new Vector3(1, 2, 0));
            yield return null;
            try
            {
                manager.SelectEnter((IXRSelectInteractor)left, a.GetComponent<XRGrabInteractable>());
                manager.SelectEnter((IXRSelectInteractor)right, b.GetComponent<XRGrabInteractable>());
                Assert.That(a.IsHeld && b.IsHeld, Is.True);
                Assert.That(a.RecoverToSpawn(), Is.False);
                manager.SelectExit((IXRSelectInteractor)left, a.GetComponent<XRGrabInteractable>());
                Assert.That(b.IsHeld, Is.True, "Releasing one hand must not release the other item.");
                manager.SelectExit((IXRSelectInteractor)right, b.GetComponent<XRGrabInteractable>());
                manager.SelectEnter((IXRSelectInteractor)left, b.GetComponent<XRGrabInteractable>());
                Assert.That(b.GetComponent<XRGrabInteractable>().firstInteractorSelecting, Is.SameAs(left));
                manager.SelectExit((IXRSelectInteractor)left, b.GetComponent<XRGrabInteractable>());
                for (int i = 0; i < 50; i++)
                {
                    var body = a.GetComponent<Rigidbody>();
                    body.position = new Vector3(9, -1, 0);
                    body.linearVelocity = Vector3.one * 50;
                    Assert.That(a.RecoverToSpawn(), Is.True);
                    Assert.That(body.position, Is.EqualTo(new Vector3(0, 2, 0)));
                    Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
                    yield return new WaitForFixedUpdate();
                }
                a.GetComponent<Rigidbody>().position = new Vector3(9, -1, 0);
                yield return new WaitForSeconds(1.1f);
                Assert.That(a.GetComponent<Rigidbody>().position, Is.EqualTo(new Vector3(0, 2, 0)),
                    "A dropped, unheld object outside the room must recover automatically.");
            }
            finally { Object.Destroy(root); }
        }

        private static XRDirectInteractor Hand(GameObject root, XRInteractionManager manager, string name)
        {
            var hand = new GameObject(name);
            hand.transform.SetParent(root.transform);
            hand.AddComponent<SphereCollider>().isTrigger = true;
            var interactor = hand.AddComponent<XRDirectInteractor>();
            interactor.interactionManager = manager;
            return interactor;
        }

        private static LabGrabRecovery Item(GameObject root, XRInteractionManager manager, Vector3 position)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.transform.SetParent(root.transform);
            item.transform.position = position;
            item.transform.localScale = Vector3.one * .1f;
            item.AddComponent<Rigidbody>().useGravity = false;
            item.AddComponent<XRGrabInteractable>().interactionManager = manager;
            return item.AddComponent<LabGrabRecovery>();
        }
    }
}
