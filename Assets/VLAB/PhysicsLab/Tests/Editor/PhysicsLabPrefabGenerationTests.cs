using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Editor;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.Measurement;
using VLAB.PhysicsLab.Projectile;

namespace VLAB.PhysicsLab.Tests
{
    public sealed class PhysicsLabPrefabGenerationTests
    {
        [Test]
        public void GenerateAll_CreatesAndValidatesTwentyProductionPrefabs()
        {
            PhysicsLabAssetPipeline.GenerateAll();

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/VLAB/PhysicsLab/Prefabs" });
            Assert.That(guids, Has.Length.EqualTo(20));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(prefab.transform.Find("Visual"), Is.Not.Null, path);
                Assert.That(prefab.transform.Find("Physics"), Is.Not.Null, path);
                Assert.That(prefab.transform.Find("Interaction"), Is.Not.Null, path);
                Assert.That(prefab.transform.Find("AttachmentPoints"), Is.Not.Null, path);
                Assert.That(prefab.transform.Find("Sensors"), Is.Not.Null, path);
                Assert.That(prefab.transform.Find("RuntimeVisuals"), Is.Not.Null, path);
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), Is.Zero, path);
            }

            Assert.That(PhysicsLabExperimentCatalog.Experiments.Count, Is.EqualTo(6));
            foreach (var experiment in PhysicsLabExperimentCatalog.Experiments)
            {
                Assert.That(experiment.RequiredAssets.Count, Is.GreaterThan(0), experiment.Id);
                foreach (var assetName in experiment.RequiredAssets)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/VLAB/PhysicsLab/Prefabs/{assetName}.prefab");
                    Assert.That(prefab, Is.Not.Null, $"{experiment.Id} missing {assetName}");
                }
            }
        }

        [Test]
        public void GeneratedPrefabs_ExposeOnlyPurposefulPhysicalInteractionPoints()
        {
            PhysicsLabAssetPipeline.GenerateAll();

            var table = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Prefabs/Experiment_Table.prefab");
            Assert.That(table.GetComponent<LabInteractable>(), Is.Null, "Furniture must not steal hover or selection.");
            Assert.That(table.GetComponent<LabGrabbable>(), Is.Null);

            var frictionBlock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Prefabs/Physics_Friction_Block.prefab");
            Assert.That(frictionBlock.GetComponentsInChildren<LabAttachmentPoint>(true).Any(point => point.AcceptedType == "mass"), Is.True);

            var glider = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Prefabs/Air_Track_Glider_A.prefab");
            Assert.That(glider.GetComponentsInChildren<LabAttachmentPoint>(true).Any(point => point.AcceptedType == "mass"), Is.True);
            Assert.That(glider.GetComponentsInChildren<LabAttachmentPoint>(true).Any(point => point.AcceptedType == "glider-collision"), Is.True);

            var timer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Prefabs/Digital_Timer_MC964.prefab");
            Assert.That(timer.GetComponentsInChildren<InstrumentPushButton>(true), Has.Length.EqualTo(3));
            Assert.That(timer.GetComponent<DigitalTimerMC964>(), Is.Not.Null);

            var launcher = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Prefabs/Physics_Projectile_Launcher.prefab");
            Assert.That(launcher.GetComponent<ProjectileLauncher>(), Is.Not.Null);
            Assert.That(launcher.GetComponentInChildren<LauncherAngleManipulator>(true), Is.Not.Null);
            Assert.That(launcher.GetComponentInChildren<InstrumentPushButton>(true), Is.Not.Null);
        }
    }
}
