using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VLAB.ChemistryLab.Input;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public sealed class DesktopInputBehaviorTests
    {
        private Mouse mouse;
        private Keyboard keyboard;

        [SetUp]
        public void Setup()
        {
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void Cleanup()
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
        }

        [UnityTest]
        public IEnumerator MouseLook_PreservesLegacySensitivity()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab");
            yield return null;
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            float before = navigator.transform.eulerAngles.y;
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(10, 0) }.WithButton(MouseButton.Right));
            InputSystem.Update();
            navigator.SendMessage("Update");
            Assert.That(Mathf.DeltaAngle(before, navigator.transform.eulerAngles.y), Is.EqualTo(2.2f).Within(.05f),
                "10 mouse pixels must retain the previous 0.1 axis scale times 2.2 sensitivity.");
        }

        [UnityTest]
        public IEnumerator InputSource_ReleaseDisablesCommandsAndDestroysOwnedAsset()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab");
            yield return null;
            var source = Object.FindAnyObjectByType<DesktopInputActionSource>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            InputSystem.Update();
            Assert.That(source.ReadFrame().Move.y, Is.GreaterThan(.9f));
            Assert.That(source.ReadFrame().RunHeld, Is.True);
            source.enabled = false;
            Assert.That(source.ReadFrame().Move, Is.EqualTo(Vector2.zero));
            source.enabled = true;
            yield return null;
            int before = OwnedAssets();
            Assert.That(before, Is.GreaterThan(0));
            Object.Destroy(source);
            yield return null;
            Assert.That(OwnedAssets(), Is.EqualTo(before - 1), "Reloading the scene must not leak cloned InputActionAssets.");
        }

        private static int OwnedAssets()
        {
            int count = 0;
            foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
                if (asset.name == "VLAB Desktop Input Actions(Clone)") count++;
            return count;
        }

        [UnityTest]
        public IEnumerator ScrollCrouchAndEscape_RespectDesktopPresentation()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab");
            yield return null;
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            var camera = navigator.GetComponent<Camera>();
            var panel = Object.FindAnyObjectByType<DesktopTitrationInterface>();
            float before = camera.fieldOfView;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(50, Screen.height - 80), scroll = new Vector2(0, 1) });
            InputSystem.Update();
            Assert.That(panel.IsPointerOverScrollableUi, Is.True);
            navigator.SendMessage("Update");
            Assert.That(camera.fieldOfView, Is.EqualTo(before), "Panel scrolling must not zoom.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width - 5, 5), scroll = new Vector2(0, 100) });
            InputSystem.Update();
            navigator.SendMessage("Update");
            Assert.That(camera.fieldOfView, Is.EqualTo(32));
            // Capture the grounded baseline after the capsule resolves its initial skin clearance.
            yield return new WaitForSeconds(.2f);
            float standingHeight = navigator.transform.position.y;
            var capsule = GameObject.Find("VLAB Desktop Upright Body").GetComponent<CharacterController>();
            float standingCapsuleHeight = capsule.height;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
            InputSystem.Update();
            yield return new WaitForSeconds(.3f);
            Assert.That(capsule.height, Is.EqualTo(standingCapsuleHeight - .62f).Within(.01f), "Crouch must reduce the capsule by exactly 0.62m.");
            Assert.That(navigator.transform.position.y, Is.InRange(standingHeight - .63f, standingHeight - .62f + capsule.skinWidth + .01f),
                "Crouched eye height must respect the controller's configured floor-contact skin, without sinking below the floor.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return new WaitForSeconds(.3f);
            Assert.That(navigator.transform.position.y, Is.EqualTo(standingHeight).Within(.04f),
                "Standing must recover to the original eye height within floor-contact tolerance.");
            // Public visibility reflects the actual panel drawn by OnGUI.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            InputSystem.Update();
            navigator.SendMessage("Update");
            Assert.That(panel.IsPanelVisible, Is.False, "Escape must toggle the lab menu.");
        }
    }
}
