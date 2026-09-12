using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VLAB.MainMenu.Tests
{
    public sealed class VLABProductionMechanicsTests
    {
        [Test] public void GearAssemblyRequiresBothDistinctGearsAndReductionLayout()
        {
            var model = new GearTrainModel();
            Assert.That(model.Check(), Is.EqualTo(GearAssemblyState.Incomplete));
            Assert.That(model.Place(12, 0), Is.True);
            Assert.That(model.Check(), Is.EqualTo(GearAssemblyState.Incomplete));
            Assert.That(model.Place(12, 1), Is.True);
            Assert.That(model.DriverTeeth, Is.Zero, "One physical gear cannot occupy two shafts.");
            model.Place(24, 0);
            Assert.That(model.Check(), Is.EqualTo(GearAssemblyState.SpeedIncrease));
            Assert.That(model.SpeedRatio, Is.EqualTo(-2).Within(.001));
            model.Place(12, 0); model.Place(24, 1);
            Assert.That(model.Check(), Is.EqualTo(GearAssemblyState.Reduction));
            Assert.That(model.SpeedRatio, Is.EqualTo(-.5f).Within(.001));
            Assert.That(model.Place(18, 0), Is.False);
            Assert.That(model.Place(12, 2), Is.False);
        }

        [Test] public void GearResetRemovesBothGears()
        {
            var model = new GearTrainModel(); model.Place(12, 0); model.Place(24, 1); model.Reset();
            Assert.That(model.DriverTeeth, Is.Zero); Assert.That(model.OutputTeeth, Is.Zero);
            Assert.That(model.SpeedRatio, Is.Zero); Assert.That(model.Check(), Is.EqualTo(GearAssemblyState.Incomplete));
        }

        [Test] public void LeverUsesSignedMomentAndRequiresOppositeSides()
        {
            var model = new LeverBalanceModel();
            Assert.That(model.Balanced, Is.False);
            Assert.That(model.SetPositions(-2, 0, 4), Is.True);
            Assert.That(model.LoadTorque, Is.EqualTo(2f).Within(.001));
            Assert.That(model.EffortTorque, Is.EqualTo(2f).Within(.001));
            Assert.That(model.Balanced, Is.True);
            Assert.That(model.SetPositions(-1, 1, 4), Is.True);
            Assert.That(model.Balanced, Is.False);
            Assert.That(model.NetTorque, Is.EqualTo(.5f).Within(.001));
            Assert.That(model.SetPositions(0, 0, 4), Is.False, "Force on pivot cannot satisfy the lesson.");
            Assert.That(model.SetPositions(2, 0, 4), Is.False, "Both downward forces on one side cannot balance.");
            Assert.That(model.SetPositions(-5, 0, 4), Is.False);
        }

        [Test] public void LeverResetRestoresKnownUnbalancedConfiguration()
        {
            var model = new LeverBalanceModel(); model.SetPositions(-2, 0, 4); model.Reset();
            Assert.That(model.LoadPosition, Is.EqualTo(-2)); Assert.That(model.PivotPosition, Is.Zero);
            Assert.That(model.EffortPosition, Is.EqualTo(2)); Assert.That(model.NetTorque, Is.EqualTo(1f).Within(.001));
            Assert.That(model.Balanced, Is.False);
        }

        [UnityTest] public IEnumerator ActivitiesInvalidateCompletionAndResetTheirPhysicalState()
        {
            var root = new GameObject("Mechanics test");
            if(Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>()==null)root.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            var gears = root.AddComponent<VLabGearActivity>(); gears.Build();
            gears.SelectGear(12); gears.PlaceSelected(0); gears.SelectGear(24); gears.PlaceSelected(1); gears.Run();
            Assert.That(gears.Completed, Is.True); Assert.That(gears.Running, Is.True);
            gears.SelectGear(24); gears.PlaceSelected(0);
            Assert.That(gears.Completed, Is.False); Assert.That(gears.Running, Is.False);
            gears.ResetActivity(); Assert.That(gears.Model.Check(), Is.EqualTo(GearAssemblyState.Incomplete));
            Object.Destroy(root); yield return null;

            root = new GameObject("Lever test");
            if(Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>()==null)root.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            var lever = root.AddComponent<VLabLeverActivity>(); lever.Build(); lever.MoveEffort(4); lever.CheckBalance();
            Assert.That(lever.Completed, Is.True);
            lever.MoveFulcrum(1); Assert.That(lever.Completed, Is.False);
            lever.ResetActivity(); Assert.That(lever.Model.Balanced, Is.False);
            Assert.That(lever.Model.PivotPosition, Is.Zero);
            Object.Destroy(root); yield return null;
        }
    }
}
