using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VLAB.Core.Input;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace VLAB.MainMenu.Tests
{
    public class VLABProductionComfortLocomotionTests
    {
        [Test]
        public void SnapTurnNeedsNeutralBeforeAnotherStep()
        {
            var settings = new VLabComfortSettings { smoothTurn = false, snapAngle = 45 };
            var turn = new VLabComfortTurn();
            Assert.That(turn.Step(.1f, .02f, settings), Is.Zero);
            Assert.That(turn.Step(.8f, .02f, settings), Is.EqualTo(45));
            Assert.That(turn.Step(.8f, .02f, settings), Is.Zero);
            Assert.That(turn.Step(-.8f, .02f, settings), Is.Zero);
            Assert.That(turn.Step(0, .02f, settings), Is.Zero);
            Assert.That(turn.Step(-.8f, .02f, settings), Is.EqualTo(-45));
        }

        [Test]
        public void SmoothTurnIsFrameRateIndependentAndRejectsInvalidInput()
        {
            var settings = new VLabComfortSettings { smoothTurn = true, turnSpeed = 60 };
            var turn = new VLabComfortTurn();
            var angle = 0f;
            for (var i = 0; i < 50; i++) angle += turn.Step(1, .02f, settings);
            Assert.That(angle, Is.EqualTo(turn.Step(1, 1, settings)).Within(.001f));
            Assert.That(angle, Is.EqualTo(60).Within(.001f));
            Assert.That(turn.Step(1, 0, settings), Is.Zero);
            Assert.That(turn.Step(float.NaN, .02f, settings), Is.Zero);
            Assert.That(turn.Step(1, float.PositiveInfinity, settings), Is.Zero);
        }

        [Test]
        public void MovementUsesSavedSpeedWithoutDiagonalOrPitchAcceleration()
        {
            var settings = new VLabComfortSettings { movementSpeed = 2.4f };
            var velocity = VLabComfortLocomotion.PlanarVelocity(new Vector3(0, -.7f, 1), Vector2.one, false, settings);
            Assert.That(velocity.y, Is.Zero);
            Assert.That(velocity.magnitude, Is.EqualTo(2.4f).Within(.001f));
            var slow = VLabComfortLocomotion.PlanarVelocity(Vector3.forward, Vector2.up, false,
                new VLabComfortSettings { movementSpeed = .6f });
            Assert.That(slow.magnitude, Is.EqualTo(.6f).Within(.001f));
            Assert.That(VLabComfortLocomotion.PlanarVelocity(Vector3.forward,
                new Vector2(float.NaN, 0), false, settings), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void LocomotionRootTurnAndMovePreserveLocalHeadPose()
        {
            var cameraObject = new GameObject("ComfortTestCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(2, 1.7f, -3), Quaternion.Euler(15, 20, 0));
            var root = VLabComfortLocomotion.CreateViewRoot(camera, "ComfortTestRoot");
            try
            {
                camera.transform.localPosition = new Vector3(.1f, 0, 0);
                var localPosition = camera.transform.localPosition;
                var localRotation = camera.transform.localRotation;
                var eyePosition = camera.transform.position;
                VLabComfortLocomotion.TurnRoot(root, camera.transform, 30);
                Assert.That(Vector3.Distance(camera.transform.position, eyePosition), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(camera.transform.localRotation, localRotation), Is.LessThan(.0001f));
                Assert.That(camera.transform.localPosition, Is.EqualTo(localPosition));
                var target = eyePosition + Vector3.right;
                VLabComfortLocomotion.MoveRootToViewPosition(root, camera.transform, target);
                Assert.That(Vector3.Distance(camera.transform.position, target), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(camera.transform.localRotation, localRotation), Is.LessThan(.0001f));
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [UnityTest]
        public IEnumerator NativeSettingsUpdateValuesButNeverEnableTurnWhilePausedOrLocked()
        {
            var settings = VLabComfortSettings.Current;
            var saved = JsonUtility.ToJson(settings);
            var previousTimeScale = Time.timeScale;
            var scene = SceneManager.CreateScene("NativeComfortIsolatedTest");
            var root = new GameObject("NativeComfortTest");
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var camera = root.AddComponent<Camera>();
                var input = root.AddComponent<InputManager>();
                var move = root.AddComponent<VLABProductionNativeMove>();
                var snap = root.AddComponent<VLABProductionNativeSnap>();
                var continuous = root.AddComponent<VLABProductionNativeTurn>();
                continuous.enabled = false;
                var adapter = root.AddComponent<VLabNativeComfort>();
                Time.timeScale = 0;
                settings.movementSpeed = 2.3f;
                settings.snapAngle = 45;
                settings.turnSpeed = 70;
                settings.smoothTurn = true;
                adapter.Configure(camera, input);
                Assert.That(move.moveSpeed, Is.EqualTo(2.3f));
                Assert.That(snap.turnAmount, Is.EqualTo(45));
                Assert.That(continuous.turnSpeed, Is.EqualTo(70));
                Assert.That(snap.enabled, Is.True);
                Assert.That(continuous.enabled, Is.False);
                Time.timeScale = 1;
                input.TranslationLocked = true;
                adapter.ApplySettings();
                Assert.That(continuous.enabled, Is.False);
                input.TranslationLocked = false;
                adapter.ApplySettings();
                Assert.That(snap.enabled, Is.False);
                Assert.That(continuous.enabled, Is.True);
                continuous.enabled = false;
                settings.smoothTurn = false;
                adapter.ApplySettings();
                Assert.That(snap.enabled, Is.False, "Externally disabled locomotion stays disabled.");
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(saved, settings);
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(root);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    // The test exercises public provider settings without starting an XR subsystem or mediator.
    public sealed class VLABProductionNativeMove : ContinuousMoveProvider
    {
        protected override void Awake() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
    }
    public sealed class VLABProductionNativeSnap : SnapTurnProvider
    {
        protected override void Awake() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
    }
    public sealed class VLABProductionNativeTurn : ContinuousTurnProvider
    {
        protected override void Awake() { }
        protected override void OnEnable() { }
        protected override void OnDisable() { }
    }
}
