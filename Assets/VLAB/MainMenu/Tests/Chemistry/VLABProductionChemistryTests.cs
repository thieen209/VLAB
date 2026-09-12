using NUnit.Framework;
using UnityEngine;

namespace VLAB.MainMenu.Tests
{
    public class VLABProductionChemistryTests
    {
        private GameObject root;
        [TearDown] public void Cleanup() { Time.timeScale=1; if(root != null) Object.DestroyImmediate(root); }
        private T Create<T>() where T : VLabActivity
        {
            root = new GameObject("ChemistryActivityTest");
            if(Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>()==null)root.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            var activity = root.AddComponent<T>();
            activity.Build();
            return activity;
        }

        [Test] public void WaterRejectsWrongElementAndMissingAtoms()
        {
            var water = Create<VLabMoleculeActivity>();
            Assert.That(water.ValidateStructure(), Is.False);
            water.SelectAtom(3);
            Assert.That(water.SelectSocket(0), Is.False);
            Assert.That(water.PlacedCount, Is.Zero);
            StringAssert.Contains("O", water.Result);
        }

        [Test] public void SharedInteractiveObjectsSelectAndTransferAndRespectPause()
        {
            var reaction = Create<VLabQualitativeActivity>();
            var reagent = root.transform.Find("Vessel_0").GetComponent<VLAB.PhysicsLab.Common.LabInteractable>();
            var sample = root.transform.Find("Vessel_3").GetComponent<VLAB.PhysicsLab.Common.LabInteractable>();
            Time.timeScale=0;
            reagent.Activate();
            Assert.That(reaction.SelectedReagent, Is.EqualTo(-1));
            Time.timeScale=1;
            reagent.Activate(); sample.Activate();
            Assert.That(reaction.HasPrecipitate, Is.True);
            Assert.That(reaction.ReagentVolume(0), Is.EqualTo(4));
        }

        [Test] public void WaterRequiresBentGeometryAndSupportsDisassemblyAndReset()
        {
            var water = Create<VLabMoleculeActivity>();
            water.SelectAtom(2); Assert.That(water.SelectSocket(0), Is.True);
            water.SelectAtom(0); Assert.That(water.SelectSocket(1), Is.True);
            water.SelectAtom(1); Assert.That(water.SelectSocket(2), Is.True);
            water.SetBentGeometry(false);
            Assert.That(water.ValidateStructure(), Is.False);
            Assert.That(water.BondAngle, Is.EqualTo(180).Within(.01));
            water.SetBentGeometry(true);
            Assert.That(water.BondAngle, Is.EqualTo(104.5).Within(.01));
            Assert.That(water.ValidateStructure(), Is.True);
            water.SelectSocket(1);
            Assert.That(water.Completed, Is.False);
            Assert.That(water.PlacedCount, Is.EqualTo(2));
            water.ResetActivity();
            Assert.That(water.PlacedCount, Is.Zero);
            Assert.That(water.SelectedAtom, Is.EqualTo(-1));
            Assert.That(water.Completed, Is.False);
        }

        [Test] public void QualitativeTransferNeedsSelectionAndConservesFiniteMaterial()
        {
            var reaction = Create<VLabQualitativeActivity>();
            Assert.That(reaction.TransferTo(0), Is.False);
            reaction.SelectReagent(1);
            Assert.That(reaction.TransferTo(0), Is.True);
            Assert.That(reaction.ReagentVolume(1), Is.EqualTo(4));
            Assert.That(reaction.SampleVolume(0), Is.EqualTo(6));
            Assert.That(reaction.HasPrecipitate, Is.False);
            Assert.That(reaction.Completed, Is.False);
            reaction.SelectReagent(1); reaction.TransferTo(0);
            reaction.SelectReagent(1); reaction.TransferTo(0);
            reaction.SelectReagent(0);
            Assert.That(reaction.TransferTo(0), Is.False, "Recipient capacity must be enforced.");
            Assert.That(reaction.ReagentVolume(0), Is.EqualTo(6));
        }

        [Test] public void QualitativeRequiresPositiveAndNegativeControlThenResetRestoresBoth()
        {
            var reaction = Create<VLabQualitativeActivity>();
            reaction.SelectReagent(0); Assert.That(reaction.TransferTo(0), Is.True);
            Assert.That(reaction.HasPrecipitate, Is.True);
            Assert.That(reaction.Completed, Is.False);
            reaction.SelectReagent(0); Assert.That(reaction.TransferTo(1), Is.True);
            Assert.That(reaction.Completed, Is.True);
            Assert.That(reaction.NegativeControlObserved, Is.True);
            reaction.ResetActivity();
            Assert.That(reaction.HasPrecipitate, Is.False);
            Assert.That(reaction.NegativeControlObserved, Is.False);
            Assert.That(reaction.ReagentVolume(0), Is.EqualTo(6));
            Assert.That(reaction.SampleVolume(0), Is.EqualTo(4));
            Assert.That(reaction.SelectedReagent, Is.EqualTo(-1));
            Assert.That(reaction.Completed, Is.False);
        }
    }
}
