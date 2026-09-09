using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VLAB.DemoLabs.Tests
{
    public sealed class DemoLabPlaythroughTests
    {
        private static bool Click(VLabInteractionDriver driver, Component target)
        {
            Physics.SyncTransforms();
            var collider = target.GetComponent<Collider>();
            var point = collider != null ? collider.bounds.center : target.transform.position;
            return driver.SelectRay(new Ray(driver.ViewCamera.transform.position, point - driver.ViewCamera.transform.position));
        }
        private static string DescribeTarget(VLabInteractionDriver driver, Component target)
        {
            var ray = new Ray(driver.ViewCamera.transform.position, target.transform.position - driver.ViewCamera.transform.position);
            var collider = target.GetComponent<Collider>();
            return $"target={target.name} position={target.transform.position} camera={ray.origin} distance={Vector3.Distance(ray.origin, target.transform.position)} scale={target.transform.lossyScale} collider={collider?.bounds} active={target.gameObject.activeInHierarchy} held={driver.Held?.name} started={driver.Experiment.Started} locked={driver.ViewLocked} modal={driver.Hud.ModalOpen}\nHits: " + string.Join("; ", Physics.RaycastAll(ray, 20).OrderBy(h => h.distance).Select(h => h.collider.name + " " + h.distance));
        }
        private static void Place(VLabInteractionDriver driver, VLabGrabInteractable item, VLabSnapZone zone)
        {
            Assert.That(Click(driver, item), Is.True, "Cannot pick " + item.name + "; hovered " + driver.Hovered?.name);
            Assert.That(driver.Held, Is.EqualTo(item), "Wrong item grabbed " + item.name);
            Assert.That(Click(driver, zone), Is.True, "Cannot place at " + zone.name + "; hovered " + driver.Hovered?.name);
            Assert.That(driver.Held, Is.Null);
        }
        private static void Capture(Camera camera, string name)
        {
            var dir = Path.GetFullPath("TestResults/DemoLabs"); Directory.CreateDirectory(dir);
            var target = new RenderTexture(1600, 1000, 24); target.Create();
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            target.Release(); Object.Destroy(target); Object.Destroy(texture);
        }
        private static float CaptureSpecimen(MicroscopeView view, string name)
        {
            var previous = RenderTexture.active; RenderTexture.active = view.Texture;
            var texture = new Texture2D(view.Texture.width, view.Texture.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); texture.Apply();
            var pixels = texture.GetPixels32();
            var edgeEnergy = 0f;
            for (var y = 32; y < texture.height - 32; y += 2) for (var x = 32; x < texture.width - 33; x += 2)
            {
                var a = pixels[y * texture.width + x]; var b = pixels[y * texture.width + x + 1];
                edgeEnergy += Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
            }
            Directory.CreateDirectory("TestResults/DemoLabs");
            File.WriteAllBytes("TestResults/DemoLabs/" + name + ".png", texture.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(texture); return edgeEnergy;
        }
        [UnityTest]
        public IEnumerator EngineeringCorrectIncorrectResetAndRecovery()
        {
            yield return SceneManager.LoadSceneAsync("EngineeringLab"); yield return null; yield return null;
            var e = Object.FindAnyObjectByType<EngineeringExperiment>(); Assert.That(e, Is.Not.Null);
            Assert.That(e.Hud.ModalOpen, Is.True); e.Hud.ModalButton.onClick.Invoke();
            Assert.That(e.Started, Is.True); Capture(e.Driver.ViewCamera, "Engineering-room");
            e.TogglePower(); Assert.That(e.Powered, Is.False);
            var resistor = e.Driver.Items.Single(i => i.Kind == "resistor" && i.Value == 220);
            var led = e.Driver.Items.Single(i => i.Kind == "led");
            Place(e.Driver, resistor, e.ResistorZone); Place(e.Driver, led, e.LedZone);
            e.Check(); Assert.That(e.Validated, Is.False);
            var sockets = e.Driver.Zones.Where(z => z.GetComponent<VLabConnectionSocket>() != null).ToDictionary(z => z.GetComponent<VLabConnectionSocket>().Terminal);
            Place(e.Driver, e.Wires[0].EndA, sockets[Terminal.Positive]); Place(e.Driver, e.Wires[0].EndB, sockets[Terminal.ResistorA]);
            Place(e.Driver, e.Wires[1].EndA, sockets[Terminal.ResistorB]); Place(e.Driver, e.Wires[1].EndB, sockets[Terminal.Anode]);
            Place(e.Driver, e.Wires[2].EndA, sockets[Terminal.Cathode]); Place(e.Driver, e.Wires[2].EndB, sockets[Terminal.Ground]);
            Assert.That(Click(e.Driver, e.ValidateButton), Is.True); Assert.That(e.Validated, Is.True);
            Assert.That(e.ReadingDisplay.text, Does.Contain("MẠCH HỢP LỆ"));
            Assert.That(Click(e.Driver, e.PowerButton), Is.True); Assert.That(e.Powered, Is.True);
            Assert.That(e.Result.State, Is.EqualTo(CircuitState.Safe));
            yield return null; Capture(e.Driver.ViewCamera, "Engineering-working");
            Assert.That(Click(e.Driver, e.ResultButton), Is.True); Assert.That(e.Completed, Is.True);
            e.Hud.ModalButton.onClick.Invoke();
            // Altering a validated powered circuit must cut power and require a fresh check.
            Assert.That(Click(e.Driver, led), Is.True); Assert.That(e.Powered, Is.False); led.Rotate(1);
            led.SetFocus(false);
            var ledProperties = new MaterialPropertyBlock(); e.LedLens.GetPropertyBlock(ledProperties);
            Assert.That(ledProperties.GetColor("_EmissionColor").maxColorComponent, Is.EqualTo(0), "Hover must not restore powered LED emission after removal");
            Assert.That(Click(e.Driver, e.LedZone), Is.True); e.Check(); Assert.That(e.Result.State, Is.EqualTo(CircuitState.Reversed));
            Assert.That(e.Validated, Is.False); e.Driver.ReturnHeld();
            e.ResetExperiment(); Assert.That(e.Started, Is.False); Assert.That(e.Powered, Is.False); Assert.That(e.LedZone.Occupant, Is.Null);
            Assert.That(e.Wires.All(w => !w.TryGetLink(out _)), Is.True); Assert.That(led.Flipped, Is.False);
            e.BeginExperiment(); resistor.transform.position = new Vector3(0, -5, 0);
            yield return new WaitForSeconds(.7f); Assert.That(resistor.transform.position.y, Is.GreaterThan(1));
            Assert.That(e.Driver.Held, Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator BiologyPreparationFocusIdentificationAndReset()
        {
            yield return SceneManager.LoadSceneAsync("BiologyLab"); yield return null; yield return null;
            var e = Object.FindAnyObjectByType<BiologyExperiment>(); Assert.That(e, Is.Not.Null);
            for (var repeat = 0; repeat < 2; repeat++)
            {
            e.Hud.ModalButton.onClick.Invoke(); Capture(e.Driver.ViewCamera, "Biology-room");
            e.EnterView(); Assert.That(e.View.Inspecting, Is.False);
            Assert.That(Click(e.Driver, e.Slide), Is.True);
            Assert.That(Click(e.Driver, e.StageZone), Is.False); Assert.That(e.Model.Mounted, Is.False); e.Driver.ReturnHeld();
            Place(e.Driver, e.Slide, e.PreparationZone);
            var water = e.Driver.Items.Single(i => i.Kind == "liquid");
            var sample = e.Driver.Items.Single(i => i.Kind == "sample");
            var cover = e.Driver.Items.Single(i => i.Kind == "cover");
            Assert.That(Click(e.Driver, cover), Is.True); Assert.That(Click(e.Driver, e.CoverZone), Is.False); e.Driver.ReturnHeld();
            Place(e.Driver, water, e.WaterZone); Place(e.Driver, sample, e.SampleZone); Place(e.Driver, cover, e.CoverZone);
            Assert.That(e.Model.Prepared, Is.True); Place(e.Driver, e.Slide, e.StageZone);
            Capture(e.Driver.ViewCamera, "Biology-mounted");
            Assert.That(Click(e.Driver, e.ClipLeft), Is.True, "Left clip click; " + DescribeTarget(e.Driver, e.ClipLeft));
            Assert.That(e.Driver.Held, Is.Null, "Clip click accidentally picked slide");
            Assert.That(Click(e.Driver, e.ClipRight), Is.True, "Right clip click; hover=" + e.Driver.Hovered?.name);
            Assert.That(e.Model.Clips, Is.True, "Both clips must close");
            Assert.That(Click(e.Driver, e.Nosepiece), Is.True); Assert.That(e.Model.Objective, Is.EqualTo(10));
            Assert.That(Click(e.Driver, e.Eyepiece), Is.True); Assert.That(e.View.Inspecting, Is.True);
            e.CoarseKnob.Rotate(15); e.FineKnob.Rotate(1);
            yield return new WaitForSeconds(.35f); Assert.That(e.Model.Observed10, Is.True);
            Assert.That(CaptureSpecimen(e.View, "Biology-10x-focused"), Is.GreaterThan(500), "10x image must contain visible structures");
            e.Nosepiece.Activate(); Assert.That(e.Model.Objective, Is.EqualTo(40)); Assert.That(e.Model.Sharp, Is.False);
            yield return new WaitForSeconds(.15f);
            var blurredEdges = CaptureSpecimen(e.View, "Biology-40x-defocused");
            e.FineKnob.Rotate(1.5f); yield return new WaitForSeconds(.25f);
            Assert.That(e.Model.Observed40, Is.True);
            Assert.That(CaptureSpecimen(e.View, "Biology-40x-focused"), Is.GreaterThan(blurredEdges * 1.1f), "Refocusing must visibly restore image detail");
            Assert.That(e.IdentifyAt(new Vector2(.5f, .5f)), Is.False);
            foreach (var structure in new[] { CellStructure.Nucleus, CellStructure.Wall, CellStructure.Cytoplasm })
            {
                var found = false;
                for (var y = 0; y < 100 && !found; y++) for (var x = 0; x < 100 && !found; x++)
                {
                    var viewUv = new Vector2(x / 100f, y / 100f);
                    if (Vector2.Distance(viewUv, Vector2.one * .5f) > .45f) continue;
                    var uv = e.View.ViewToSpecimen(viewUv);
                    if (OnionSpecimen.Classify(uv) != structure) continue;
                    Assert.That(e.IdentifyAt(uv), Is.True, "Cannot identify " + structure); found = true;
                }
                Assert.That(found, Is.True, "Structure not in 40x field: " + structure);
            }
            Assert.That(e.Model.Identified, Is.EqualTo(3)); e.Check(); Assert.That(e.Completed, Is.True);
            e.ResetExperiment(); Assert.That(e.View.Inspecting, Is.False); Assert.That(e.Driver.ViewLocked, Is.False);
            Assert.That(e.Model.Prepared, Is.False); Assert.That(e.Model.Mounted, Is.False); Assert.That(e.Model.Identified, Is.Zero);
            Assert.That(e.CoarseKnob.Value, Is.Zero); Assert.That(e.FineKnob.Value, Is.EqualTo(.5f));
            Assert.That(sample.gameObject.activeSelf, Is.True); Assert.That(cover.gameObject.activeSelf, Is.True);
            Assert.That(e.Driver.Items.All(i => i.Zone == null && !i.IsHeld), Is.True);
            LogAssert.NoUnexpectedReceived();
            }
        }
        [UnityTest]
        public IEnumerator BiologyPartialResetLostSlideFocusOvershootAndViewExit()
        {
            yield return SceneManager.LoadSceneAsync("BiologyLab"); yield return null; yield return null;
            var e = Object.FindAnyObjectByType<BiologyExperiment>(); e.BeginExperiment();
            Place(e.Driver, e.Slide, e.PreparationZone);
            Place(e.Driver, e.Driver.Items.Single(i => i.Kind == "liquid"), e.WaterZone);
            e.ResetExperiment(); e.BeginExperiment();
            Assert.That(e.Model.Water, Is.False); Assert.That(e.WaterDrop.activeSelf, Is.False);
            e.Slide.transform.position = new Vector3(0, -4, 0);
            yield return new WaitForSeconds(.7f);
            Assert.That(e.Slide.transform.position.y, Is.GreaterThan(1));
            Place(e.Driver, e.Slide, e.PreparationZone);
            Place(e.Driver, e.Driver.Items.Single(i => i.Kind == "liquid"), e.WaterZone);
            Place(e.Driver, e.Driver.Items.Single(i => i.Kind == "sample"), e.SampleZone);
            Place(e.Driver, e.Driver.Items.Single(i => i.Kind == "cover"), e.CoverZone);
            Place(e.Driver, e.Slide, e.StageZone);
            Click(e.Driver, e.ClipLeft); Click(e.Driver, e.ClipRight); Click(e.Driver, e.Nosepiece); Click(e.Driver, e.Eyepiece);
            yield return new WaitForSeconds(.6f);
            Canvas.ForceUpdateCanvases();
            var rimCorners = new Vector3[4]; e.View.OpticalRim.GetWorldCorners(rimCorners);
            var panel = (RectTransform)e.View.ScopePanel.transform;
            foreach (var corner in rimCorners) Assert.That(panel.rect.Contains(panel.InverseTransformPoint(corner)), Is.True, "Circular field must fit between the HUD bars");
            e.CoarseKnob.Rotate(20); e.FineKnob.Rotate(1);
            yield return new WaitForSeconds(.3f); Assert.That(e.Model.Sharp, Is.False); Assert.That(e.Model.Observed10, Is.False);
            e.CoarseKnob.Rotate(-5); yield return new WaitForSeconds(.3f); Assert.That(e.Model.Observed10, Is.True);
            e.View.Exit(); Assert.That(e.Driver.ViewLocked, Is.False);
            Click(e.Driver, e.Eyepiece); yield return new WaitForSeconds(.6f); Assert.That(e.View.Inspecting, Is.True);
            e.Nosepiece.Activate(); var coarse = e.CoarseKnob.Value;
            e.CoarseKnob.Rotate(2); Assert.That(e.CoarseKnob.Value, Is.EqualTo(coarse), "Coarse focus must stay locked at 40x");
            e.ResetExperiment(); Assert.That(e.View.Inspecting, Is.False); Assert.That(e.Driver.ViewLocked, Is.False);
            Assert.That(e.Driver.Items.All(i => i.Zone == null && !i.IsHeld && i.gameObject.activeSelf), Is.True);
            Assert.That(e.View.IdentificationMarkers.All(marker => !marker.gameObject.activeSelf), Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator EngineeringAllResistorsShortCircuitPartialResetAndRepeat()
        {
            yield return SceneManager.LoadSceneAsync("EngineeringLab"); yield return null; yield return null;
            var e = Object.FindAnyObjectByType<EngineeringExperiment>();
            var sockets = e.Driver.Zones.Where(z => z.GetComponent<VLabConnectionSocket>() != null).ToDictionary(z => z.GetComponent<VLabConnectionSocket>().Terminal);
            var led = e.Driver.Items.Single(i => i.Kind == "led");
            var brightness = new float[3]; var values = new[] { 100, 220, 1000 };
            for (var i = 0; i < values.Length; i++)
            {
                e.ResetExperiment(); e.BeginExperiment();
                Place(e.Driver, e.Driver.Items.Single(item => item.Kind == "resistor" && item.Value == values[i]), e.ResistorZone);
                Place(e.Driver, led, e.LedZone);
                Place(e.Driver, e.Wires[0].EndA, sockets[Terminal.Positive]); Place(e.Driver, e.Wires[0].EndB, sockets[Terminal.ResistorA]);
                Place(e.Driver, e.Wires[1].EndA, sockets[Terminal.ResistorB]); Place(e.Driver, e.Wires[1].EndB, sockets[Terminal.Anode]);
                Place(e.Driver, e.Wires[2].EndA, sockets[Terminal.Cathode]); Place(e.Driver, e.Wires[2].EndB, sockets[Terminal.Ground]);
                e.Check(); e.TogglePower(); Assert.That(e.Powered, Is.True);
                Assert.That(e.Result.Current, Is.EqualTo(3f / values[i]).Within(.00001f)); brightness[i] = e.LedLight.intensity;
                Assert.That(e.Result.State, Is.EqualTo(i == 0 ? CircuitState.Overcurrent : i == 1 ? CircuitState.Safe : CircuitState.Dim));
                yield return null; Capture(e.Driver.ViewCamera, "Engineering-" + values[i] + "ohm");
                if (values[i] == 220) { e.ObserveResult(); Assert.That(e.Completed, Is.True); e.Hud.ModalButton.onClick.Invoke(); }
            }
            Assert.That(brightness[0], Is.GreaterThan(brightness[1])); Assert.That(brightness[1], Is.GreaterThan(brightness[2]));
            Assert.That(Click(e.Driver, e.Wires[0].EndB), Is.True); e.Driver.ReturnHeld();
            Assert.That(e.Powered, Is.False); Assert.That(e.Validated, Is.False);
            Assert.That(Click(e.Driver, e.Wires[2].EndB), Is.True); e.Driver.ReturnHeld();
            Place(e.Driver, e.Wires[0].EndB, sockets[Terminal.Ground]); Place(e.Driver, e.Wires[2].EndB, sockets[Terminal.ResistorA]);
            e.Check(); Assert.That(e.Result.State, Is.EqualTo(CircuitState.Miswired)); e.TogglePower(); Assert.That(e.Powered, Is.False);
            e.ResetExperiment(); e.BeginExperiment();
            Place(e.Driver, led, e.LedZone); e.ResetExperiment();
            Assert.That(e.LedZone.Occupant, Is.Null); Assert.That(e.Driver.Items.All(item => item.Zone == null && !item.IsHeld), Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator HomeRoutesBothNewLabsAndKeepsPhysicsAvailable()
        {
            foreach (var choice in new[] { "BiologyLabButton", "MechanicalLabButton", "PhysicsLabButton", "ChemistryLabButton" })
            {
                yield return SceneManager.LoadSceneAsync("Home"); yield return null;
                var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include);
                buttons.Single(b => b.name == "LabListButton").onClick.Invoke();
                Assert.That(buttons.Single(b => b.name == choice).GetComponentInChildren<TMPro.TMP_Text>().text, Is.Not.EqualTo("Button"));
                buttons.Single(b => b.name == choice).onClick.Invoke();
                buttons.Single(b => b.name == "JoinNowButton").onClick.Invoke();
                var expected = choice == "BiologyLabButton" ? "BiologyLab" : choice == "MechanicalLabButton" ? "EngineeringLab" : "PhysicsLab_Base";
                var timeout = Time.realtimeSinceStartup + 20;
                while (!SceneManager.GetSceneByName(expected).isLoaded && Time.realtimeSinceStartup < timeout) yield return null;
                Assert.That(SceneManager.GetSceneByName(expected).isLoaded, Is.True, choice);
                yield return null;
                if (expected == "PhysicsLab_Base")
                {
                    // The real menu waits for Physics' additive hub transition before returning.
                    var flow = VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance;
                    while ((!SceneManager.GetSceneByName("PhysicsLab_Hub").isLoaded || flow == null || flow.IsTransitioning) && Time.realtimeSinceStartup < timeout)
                    { yield return null; flow = VLAB.PhysicsLab.SceneFlow.PhysicsLabSceneFlow.Instance; }
                    Assert.That(SceneManager.GetSceneByName("PhysicsLab_Hub").isLoaded, Is.True);
                    Assert.That(flow.IsTransitioning, Is.False);
                    Assert.That(Object.FindObjectsByType<AudioListener>().Count(listener => listener.isActiveAndEnabled), Is.EqualTo(1));
                }
                if (choice == "BiologyLabButton" || choice == "MechanicalLabButton") Assert.That(Object.FindAnyObjectByType<VLabExperimentController>(), Is.Not.Null);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
