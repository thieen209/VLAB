using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLAB.Core.Input;
using VLAB.DemoLabs;

namespace VLAB.MainMenu.Tests
{
    public sealed class VLABProductionMicroscopeHandoffTests
    {
        private GameObject root;
        private Camera view;
        private Canvas canvas;
        private MicroscopeView scope;
        private VLabInteractionDriver driver;

        [SetUp] public void SetUp()
        {
            root = new GameObject("Microscope handoff test");
            view = new GameObject("View").AddComponent<Camera>();
            view.transform.SetParent(root.transform, false); view.enabled = false;
            view.transform.SetPositionAndRotation(new Vector3(0, 2.65f, -3.3f), Quaternion.Euler(23, 12, 0));
            view.aspect = 1.6f;
            driver = root.AddComponent<VLabInteractionDriver>(); driver.enabled = false; driver.ViewCamera = view;
            var experiment = root.AddComponent<BiologyExperiment>(); experiment.enabled = false;
            experiment.Driver = driver; driver.Experiment = experiment;
            canvas = new GameObject("Original scope canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(root.transform, false); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = view;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1440, 900);
            canvas.transform.SetPositionAndRotation(new Vector3(3, 1, 2), Quaternion.Euler(0, 35, 0));
            canvas.transform.localScale = Vector3.one * .0018f;
            scope = root.AddComponent<MicroscopeView>(); scope.enabled = false; scope.Experiment = experiment; experiment.View = scope;
            scope.ScopePanel = new GameObject("Scope panel", typeof(RectTransform), typeof(CanvasGroup));
            scope.ScopePanel.transform.SetParent(canvas.transform, false);
            ((RectTransform)scope.ScopePanel.transform).sizeDelta = new Vector2(1440, 900);
            scope.ScopeFade = scope.ScopePanel.GetComponent<CanvasGroup>();
            scope.OpticalRim = new GameObject("Optical field", typeof(RectTransform)).GetComponent<RectTransform>();
            scope.OpticalRim.SetParent(scope.ScopePanel.transform, false);
            scope.EyepieceView = new GameObject("Legacy eyepiece camera target").transform;
            scope.EyepieceView.SetParent(root.transform, false);
            scope.EyepieceView.position = new Vector3(.65f, 2.219f, -.158f);
        }

        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }

        [Test] public void WorldScopeFitsViewWithoutMovingHeadAndRestoresOriginalBoardOnExit()
        {
            var headPosition = view.transform.position; var headRotation = view.transform.rotation;
            var boardPosition = canvas.transform.position; var boardRotation = canvas.transform.rotation; var boardScale = canvas.transform.localScale;
            scope.Enter();
            Assert.That(scope.Inspecting, Is.True); Assert.That(scope.ScopeFade.alpha, Is.EqualTo(1));
            Assert.That(view.transform.position, Is.EqualTo(headPosition));
            Assert.That(Quaternion.Angle(view.transform.rotation, headRotation), Is.LessThan(.001));
            var corners = new Vector3[4]; ((RectTransform)canvas.transform).GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = view.WorldToViewportPoint(corner);
                Assert.That(point.z, Is.GreaterThan(0));
                Assert.That(point.x, Is.InRange(.01f, .99f)); Assert.That(point.y, Is.InRange(.01f, .99f));
            }
            var placedBoard = canvas.transform.position;
            view.transform.rotation = Quaternion.Euler(5, 55, 0);
            scope.Enter(); // Opening an already open scope must not chase the new gaze.
            Assert.That(canvas.transform.position, Is.EqualTo(placedBoard));
            var naturalHeadRotation = view.transform.rotation;
            scope.Exit();
            Assert.That(view.transform.position, Is.EqualTo(headPosition));
            Assert.That(Quaternion.Angle(view.transform.rotation, naturalHeadRotation), Is.LessThan(.001));
            Assert.That(canvas.transform.position, Is.EqualTo(boardPosition));
            Assert.That(Quaternion.Angle(canvas.transform.rotation, boardRotation), Is.LessThan(.001));
            Assert.That(canvas.transform.localScale, Is.EqualTo(boardScale));
            Assert.That(driver.ViewLocked, Is.False);
        }

        [Test] public void DisabledDriverInputDoesNotExitInspectionOrDropOriginalItemButExplicitResetStillWorks()
        {
            scope.Enter();
            var item = new GameObject("Original held slide").AddComponent<VLabGrabInteractable>();
            item.transform.SetParent(root.transform, false);
            var heldPosition = new Vector3(.4f, 1.2f, .5f); item.transform.position = heldPosition;
            typeof(VLabInteractionDriver).GetProperty("Held").SetValue(driver, item);
            typeof(VLabInteractionDriver).GetMethod("HandleDropInput", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(driver, null);
            typeof(VLabInteractionDriver).GetMethod("Escape", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(driver, null);
            Assert.That(driver.Held, Is.SameAs(item)); Assert.That(item.transform.position, Is.EqualTo(heldPosition));
            Assert.That(scope.Inspecting, Is.True, "A suspended original lesson must ignore the supplementary menu input.");
            driver.ReturnHeld();
            Assert.That(driver.Held, Is.Null, "Explicit resets must retain their public ReturnHeld operation even while suspended.");
        }
    }
}
