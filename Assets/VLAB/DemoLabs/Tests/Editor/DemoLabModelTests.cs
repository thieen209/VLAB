using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VLAB.DemoLabs.Editor;

namespace VLAB.DemoLabs.Tests
{
    public sealed class DemoLabModelTests
    {
        private static List<CircuitLink> Series(bool backward = false) => new List<CircuitLink>
        {
            new CircuitLink(Terminal.Positive, Terminal.ResistorA),
            new CircuitLink(Terminal.ResistorB, backward ? Terminal.Cathode : Terminal.Anode),
            new CircuitLink(backward ? Terminal.Anode : Terminal.Cathode, Terminal.Ground)
        };
        [TestCase(100, CircuitState.Overcurrent, .030f)]
        [TestCase(220, CircuitState.Safe, .01363636f)]
        [TestCase(1000, CircuitState.Dim, .003f)]
        public void ResistorControlsCurrent(int resistance, CircuitState state, float amps)
        {
            var result = CircuitModel.Evaluate(resistance, true, false, Series());
            Assert.That(result.State, Is.EqualTo(state)); Assert.That(result.Current, Is.EqualTo(amps).Within(.000001));
        }
        [TestCase(false, true, CircuitState.Reversed)]
        [TestCase(true, false, CircuitState.Reversed)]
        [TestCase(true, true, CircuitState.Safe)]
        public void PhysicalPolarityAndSocketPolarityBothMatter(bool wireReversed, bool ledReversed, CircuitState expected)
            => Assert.That(CircuitModel.Evaluate(220, true, ledReversed, Series(wireReversed)).State, Is.EqualTo(expected));
        [Test]
        public void ShortsBypassesDuplicatesAndMissingPartsCannotPower()
        {
            var variants = new[]
            {
                new List<CircuitLink>(),
                new List<CircuitLink> { new CircuitLink(Terminal.Positive, Terminal.Ground), new CircuitLink(Terminal.ResistorA, Terminal.ResistorB), new CircuitLink(Terminal.Anode, Terminal.Cathode) },
                new List<CircuitLink> { new CircuitLink(Terminal.Positive, Terminal.Anode), new CircuitLink(Terminal.Cathode, Terminal.Ground), new CircuitLink(Terminal.ResistorA, Terminal.ResistorB) },
                new List<CircuitLink> { new CircuitLink(Terminal.Positive, Terminal.ResistorA), new CircuitLink(Terminal.Positive, Terminal.ResistorA), new CircuitLink(Terminal.Cathode, Terminal.Ground) },
                Series().Concat(new[] { new CircuitLink(Terminal.Positive, Terminal.Ground) }).ToList()
            };
            foreach (var variant in variants) Assert.That(CircuitModel.Evaluate(220, true, false, variant).CanPower, Is.False);
            Assert.That(CircuitModel.Evaluate(0, true, false, Series()).CanPower, Is.False);
            Assert.That(CircuitModel.Evaluate(220, false, false, Series()).CanPower, Is.False);
        }
        [Test]
        public void ResistorIsNonPolarized()
        {
            var wires = new List<CircuitLink> { new CircuitLink(Terminal.Positive, Terminal.ResistorB), new CircuitLink(Terminal.ResistorA, Terminal.Anode), new CircuitLink(Terminal.Cathode, Terminal.Ground) };
            Assert.That(CircuitModel.Evaluate(220, true, false, wires).State, Is.EqualTo(CircuitState.Safe));
        }
        [Test]
        public void MicroscopeRequiresPreparationLightBothFocusControlsAndRefocus()
        {
            var m = new MicroscopeModel { Objective = 10, Coarse = .60f, Fine = .60f };
            Assert.That(m.Sharp, Is.True); m.Observe(); Assert.That(m.Observed10, Is.False);
            m.Mounted = m.Clips = m.CoarseUsed = m.FineUsed10 = true;
            m.Observe(); Assert.That(m.Observed10, Is.True);
            m.Objective = 40; Assert.That(m.Sharp, Is.False);
            m.Fine = .75f; m.Observe(); Assert.That(m.Observed40, Is.False);
            m.FineUsed40 = true; m.Observe(); Assert.That(m.Observed40, Is.True);
            m.Light = 0; Assert.That(m.Sharp, Is.False);
            Assert.That(m.Identify(CellStructure.Nucleus), Is.False);
            m.Light = .7f; Assert.That(m.Identify(CellStructure.Wall), Is.False);
            Assert.That(m.Identify(CellStructure.Nucleus), Is.True);
            Assert.That(m.Identify(CellStructure.Wall), Is.True);
            Assert.That(m.Identify(CellStructure.Cytoplasm), Is.True);
            Assert.That(m.Identified, Is.EqualTo(3));
        }
        [Test]
        public void OvershootingFocusBlursAgain()
        {
            var m = new MicroscopeModel { Objective = 10, Coarse = .613f };
            var sharp = m.Clarity; m.Coarse = .45f; Assert.That(m.Clarity, Is.LessThan(sharp));
            m.Coarse = .8f; Assert.That(m.Clarity, Is.LessThan(sharp));
        }
        [TestCase(DemoLabSceneBuilder.EngineeringPath, typeof(EngineeringExperiment))]
        [TestCase(DemoLabSceneBuilder.BiologyPath, typeof(BiologyExperiment))]
        public void SavedScenesHaveWiredReferencesAndNoMissingScripts(string path, System.Type controllerType)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            Assert.That(components.All(c => c != null), Is.True, "Missing MonoBehaviour");
            var experiment = components.OfType<VLabExperimentController>().Single(); Assert.That(experiment.GetType(), Is.EqualTo(controllerType));
            Assert.That(experiment.Driver, Is.Not.Null); Assert.That(experiment.Hud, Is.Not.Null);
            Assert.That(experiment.Driver.Items.Length, Is.GreaterThan(3));
            if (experiment is BiologyExperiment biology)
            {
                Assert.That(biology.View.SpecimenPlane.sharedMaterial.mainTexture, Is.Not.Null, "Persisted specimen texture");
                Assert.That(biology.View.SpecimenPlane.sharedMaterial.mainTexture.width, Is.EqualTo(2048));
                Assert.That(biology.LeftClipPart.IsChildOf(biology.StageMovingPart), Is.True);
                Assert.That(biology.RightClipPart.IsChildOf(biology.StageMovingPart), Is.True);
                Assert.That(biology.View.ScopeImage.GetComponentInParent<UnityEngine.UI.Mask>().GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null, "Circular field must have a real sprite mask");
                Assert.That(Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().scrollDeltaPerTick, Is.EqualTo(1));
            }
            foreach (var component in components.Where(c => c.GetType().Namespace == "VLAB.DemoLabs"))
            {
                foreach (var field in component.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (!typeof(Object).IsAssignableFrom(field.FieldType) || field.Name == "Anchor" || field.Name == "Indicator") continue;
                    Assert.That(field.GetValue(component) as Object, Is.Not.Null, component.name + "." + field.Name);
                }
            }
        }
    }
}
