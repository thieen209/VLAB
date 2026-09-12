using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLAB.PhysicsLab.Common;

namespace VLAB.MainMenu.Tests
{
    public class VLABProductionCellActivityTests
    {
        private GameObject root;
        private VLabCellActivity activity;
        private float savedTimeScale;

        [SetUp]
        public void SetUp()
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 1;
            root = new GameObject("CellActivityTest");
            if(Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>()==null)root.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            activity = root.AddComponent<VLabCellActivity>();
            activity.Build();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            Time.timeScale = savedTimeScale;
            yield return null;
        }

        [Test]
        public void WrongStructureDoesNotAdvance_OrderedFunctionsCompleteAndReset()
        {
            Activate("Nucleus");
            Assert.That(activity.IdentifiedCount, Is.Zero);
            Assert.That(activity.Completed, Is.False);
            Assert.That(activity.Result, Does.Contain("Chưa đúng"));

            var order = new[]
            {
                "CellWall_Left", "Membrane_Top", "Cytoplasm", "Vacuole",
                "Chloroplast_1", "Nucleus", "Mitochondrion"
            };
            for (var i = 0; i < order.Length; i++)
            {
                Activate(order[i]);
                Assert.That(activity.IdentifiedCount, Is.EqualTo(i + 1), order[i]);
            }

            Assert.That(activity.IdentifiedCount, Is.EqualTo(7));
            Assert.That(activity.Completed, Is.True);
            Assert.That(activity.Result, Does.Contain("7/7"));
            activity.RotateInspection(30);
            activity.ResetActivity();
            Assert.That(activity.IdentifiedCount, Is.Zero);
            Assert.That(activity.Completed, Is.False);
            Assert.That(activity.InspectionAngle, Is.Zero);
            Assert.That(activity.Result, Is.Empty);
            Assert.That(activity.SelectStructure(VLabCellActivity.Structure.Membrane), Is.False);
            Assert.That(activity.SelectStructure(VLabCellActivity.Structure.CellWall), Is.True);
            Assert.That(activity.SelectStructure(VLabCellActivity.Structure.CellWall), Is.False,
                "Repeating the same correct answer must not answer the next question.");
            Assert.That(activity.IdentifiedCount, Is.EqualTo(1));
        }

        private void Activate(string partName)
        {
            var part = root.transform.Find("CellModel/" + partName);
            Assert.That(part, Is.Not.Null, partName);
            var interactable = part.GetComponent<LabInteractable>();
            Assert.That(interactable, Is.Not.Null, partName);
            Assert.That(part.GetComponent<Collider>().enabled, Is.True, partName);
            interactable.Activate();
        }

        [Test]
        public void PointerCallbacksRespectPause_AndRepeatedBuildKeepsOneModel()
        {
            var wall = root.transform.Find("CellModel/CellWall_Left").GetComponent<LabInteractable>();
            Assert.That(wall, Is.Not.Null);
            Time.timeScale = 0;
            wall.Activate();
            Assert.That(activity.IdentifiedCount, Is.Zero);
            Time.timeScale = 1;
            wall.Activate();
            Assert.That(activity.IdentifiedCount, Is.EqualTo(1));
            var childCount = root.transform.childCount;
            activity.Build();
            Assert.That(root.transform.childCount, Is.EqualTo(childCount));
            Assert.That(activity.IdentifiedCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidStructureAndNonFiniteRotationCannotCorruptLesson()
        {
            Assert.That(activity.SelectStructure((VLabCellActivity.Structure)99), Is.False);
            activity.RotateInspection(float.NaN);
            activity.RotateInspection(float.PositiveInfinity);
            Assert.That(activity.InspectionAngle, Is.Zero);
            activity.RotateInspection(1000);
            Assert.That(activity.InspectionAngle, Is.EqualTo(30));
            activity.RotateInspection(-1000);
            Assert.That(activity.InspectionAngle, Is.EqualTo(-30));
            Assert.That(activity.IdentifiedCount, Is.Zero);
            activity.enabled = false;
            Assert.That(activity.SelectStructure(VLabCellActivity.Structure.CellWall), Is.False);
        }
    }
}
