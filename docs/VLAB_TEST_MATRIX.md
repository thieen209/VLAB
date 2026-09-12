# VLAB test matrix

Generated from saved Unity XML on 2026-09-12T10:27:12.236654+00:00. Only the recorded executions below are claimed. The production subset repeats cases from PlayMode after final visual adjustments; suite totals are not additive unique-test counts.

| Suite | Cases | Passed | Failed | Skipped / inconclusive | Recorded UTC |
|---|---:|---:|---:|---:|---|
| edit | 108 | 108 | 0 | 0 | 2026-09-12T10:10:53.336900+00:00 |
| play | 88 | 88 | 0 | 0 | 2026-09-12T10:07:14.446912+00:00 |
| production | 46 | 46 | 0 | 0 | 2026-09-12T10:09:46.092217+00:00 |

Android: **Succeeded**. See `TestResults/Unified/android-build.txt` and the APK under `Builds/Android`.

The source inventory is `docs/Production/source-audit.json`. The build validates enabled scene dependencies and missing scripts. Real camera captures are under `TestResults/Production/Visuals`; these are not Android screenshots or a claim of measured headset performance.

One exact optional Unity AI editor account timeout is recorded separately in `TestResults/Production/editor-environment.txt`. It is permitted only when its editor-package stack matches. Application logs and assertions remain active.

## Physical device matrix

| Test | Environment | Expected | Actual | Status | Notes |
|---|---|---|---|---|---|
| Cardboard stereo, optics and device QR calibration | Android phone + viewer | Both eyes, aligned lenses, correct pose | Device unavailable | NOT RUN | Runtime uses installed Cardboard 1.35.0 |
| Viewer mode on/off and touch input | Physical Android | Correct subsystem lifecycle and scene reload | Device unavailable | NOT RUN | Deterministic policy/pose tests and APK compilation do not validate optics |
| BLE pairing, firmware buttons and packet transport | Supplied physical controllers | Match actual firmware/GATT protocol | Protocol and hardware unavailable | NOT RUN | Decoded replay contract is tested; no wire protocol invented |
| Sustained frame time, temperature and battery | Target Android device | Meet its refresh/thermal budget | Unmeasured | NOT RUN | No editor frame time represented as mobile performance |

## Individual executable checks

| Test name | Environment | Expected | Actual | Duration | Notes |
|---|---|---|---|---|---|
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.AreConcordant_AcceptsThreeTrialsWithinPointOneMillilitreSpread | Unity Editor edit | Pass | Passed | 0.000410 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.AreConcordant_RejectsTrialsOutsidePointOneMillilitreSpread | Unity Editor edit | Pass | Passed | 0.000126 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.CalculateMassVolumePercent_ConvertsMolarityUsingAceticAcidMolarMass | Unity Editor edit | Pass | Passed | 0.000366 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.CalculateMeanTitre_RejectsDiscordantTrials | Unity Editor edit | Pass | Passed | 0.001436 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.CalculateMeanTitre_ReturnsMeanForConcordantTrials | Unity Editor edit | Pass | Passed | 0.000129 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.CalculateOriginalAcidConcentration_AppliesTenFoldVinegarDilution | Unity Editor edit | Pass | Passed | 0.000163 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.CalculateOriginalAcidConcentration_RejectsZeroAliquotVolume | Unity Editor edit | Pass | Passed | 0.000250 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.ClassifyEndpoint_UsesEndpointWindowAndDetectsOvershoot(8.24d,BeforeEndpoint) | Unity Editor edit | Pass | Passed | 0.000981 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.ClassifyEndpoint_UsesEndpointWindowAndDetectsOvershoot(8.28d,Endpoint) | Unity Editor edit | Pass | Passed | 0.000137 s |  |
| VLAB.ChemistryLab.Tests.ChemistryCalculationTests.ClassifyEndpoint_UsesEndpointWindowAndDetectsOvershoot(8.38d,Overshot) | Unity Editor edit | Pass | Passed | 0.000103 s |  |
| VLAB.ChemistryLab.Tests.InteractionSafetyTests.OnePressCannotDispatchTwiceOrWhileDisabled | Unity Editor edit | Pass | Passed | 0.000362 s |  |
| VLAB.ChemistryLab.Tests.InteractionSafetyTests.RecoveryNeverMovesHeldObjectsAndDetectsFloorAndRoomLoss | Unity Editor edit | Pass | Passed | 0.000300 s |  |
| VLAB.ChemistryLab.Tests.InteractionSafetyTests.UnsafeThrowsAreLimitedWithoutChangingDirection | Unity Editor edit | Pass | Passed | 0.000546 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.InvalidStorage_IsRejected | Unity Editor edit | Pass | Passed | 0.001653 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.InvalidTransfer_DoesNotMutate(double.NaN) | Unity Editor edit | Pass | Passed | 0.000666 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.InvalidTransfer_DoesNotMutate(double.PositiveInfinity) | Unity Editor edit | Pass | Passed | 0.000040 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.InvalidTransfer_DoesNotMutate(-1) | Unity Editor edit | Pass | Passed | 0.000038 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.InvalidTransfer_DoesNotMutate(0) | Unity Editor edit | Pass | Passed | 0.000030 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.MixedLiquid_IsTrackedAndEmptyVesselLosesIdentity | Unity Editor edit | Pass | Passed | 0.000499 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.RepeatedFractionalTransfers_ConserveVolume | Unity Editor edit | Pass | Passed | 0.000279 s |  |
| VLAB.ChemistryLab.Tests.LabLiquidStateTests.Transfer_ConservesVolumeAndStopsAtCapacity | Unity Editor edit | Pass | Passed | 0.000227 s |  |
| VLAB.ChemistryLab.Tests.LabPreferencesTests.InvalidSettings_UseSafeDefaultsAndClampValues | Unity Editor edit | Pass | Passed | 0.001067 s |  |
| VLAB.ChemistryLab.Tests.LabPreferencesTests.Preset_AppliesRealRenderingSettings(0,0,Disable,0) | Unity Editor edit | Pass | Passed | 0.001052 s |  |
| VLAB.ChemistryLab.Tests.LabPreferencesTests.Preset_AppliesRealRenderingSettings(1,4,HardOnly,20) | Unity Editor edit | Pass | Passed | 0.000254 s |  |
| VLAB.ChemistryLab.Tests.LabPreferencesTests.Preset_AppliesRealRenderingSettings(2,4,All,35) | Unity Editor edit | Pass | Passed | 0.000240 s |  |
| VLAB.ChemistryLab.Tests.LabPreferencesTests.Preset_AppliesRealRenderingSettings(3,8,All,60) | Unity Editor edit | Pass | Passed | 0.000261 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Desktop_DoesNotInitializeXr | Unity Editor edit | Pass | Passed | 0.005940 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Hardware_ActivatesAfterLoaderSuccess | Unity Editor edit | Pass | Passed | 0.002240 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Hardware_IsBlockedByMandatoryValidationError | Unity Editor edit | Pass | Passed | 0.001793 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Hardware_ReturnsReasonWhenLoaderFails | Unity Editor edit | Pass | Passed | 0.001268 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Hardware_TimesOutAndCancelsLoader | Unity Editor edit | Pass | Passed | 0.002156 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Hardware_UsesDesktopFallbackWhenRuntimeIsMissing | Unity Editor edit | Pass | Passed | 0.001945 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.ModeTransition_PreservesSharedExperimentStateReference | Unity Editor edit | Pass | Passed | 0.001130 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Resolver_CommandLineOverridesPersistedAndLaunchChoices | Unity Editor edit | Pass | Passed | 0.000596 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Resolver_InvalidCommandLineFallsBackToDesktopWithReason | Unity Editor edit | Pass | Passed | 0.000242 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Resolver_PersistedExplicitChoicePrecedesLaunchMenu | Unity Editor edit | Pass | Passed | 0.000335 s |  |
| VLAB.ChemistryLab.Tests.PresentationModeCoordinatorTests.Simulator_IsRejectedOutsideEditorOrDevelopmentContext | Unity Editor edit | Pass | Passed | 0.002030 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.AddTitrant_AccumulatesDoseAndUpdatesEndpointState | Unity Editor edit | Pass | Passed | 0.000615 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.ClearResults_ReturnsToSafetyAndRemovesAllObservations | Unity Editor edit | Pass | Passed | 0.001614 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.CompletedExperiment_ExposesCalculatedVinegarResults | Unity Editor edit | Pass | Passed | 0.000583 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.DesktopDosePlan_ReachesTheTargetWithCoarseMediumAndFineControls | Unity Editor edit | Pass | Passed | 0.000279 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.DiscordantAcceptedResults_KeepLessonOpenForAnotherTrial | Unity Editor edit | Pass | Passed | 0.000212 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.NewExperiment_StartsByRequiringSafetyEquipment | Unity Editor edit | Pass | Passed | 0.000135 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.RecordCurrentTitre_AcceptsEndpointAndStartsNextTrial | Unity Editor edit | Pass | Passed | 0.000225 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.RecordTitre_AcceptsEndpointResult | Unity Editor edit | Pass | Passed | 0.000168 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.RecordTitre_RejectsOvershootAndKeepsExperimentAtTitrationStep | Unity Editor edit | Pass | Passed | 0.000154 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.RequiredProcedure_CompletesOnlyInApprovedOrder | Unity Editor edit | Pass | Passed | 0.000200 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.ResetTrial_PreservesObservationsButReturnsToSetup | Unity Editor edit | Pass | Passed | 0.000315 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.ThreeDiscordantTitres_DoNotCompleteUntilLatestThreeAreConcordant | Unity Editor edit | Pass | Passed | 0.000167 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.TryCompleteStep_AdvancesWhenRequiredStepIsPerformed | Unity Editor edit | Pass | Passed | 0.000133 s |  |
| VLAB.ChemistryLab.Tests.TitrationExperimentTests.TryCompleteStep_RejectsOutOfOrderActionWithoutAdvancing | Unity Editor edit | Pass | Passed | 0.000874 s |  |
| VLAB.Tests.InputLifecycleTests.ProviderLossReleasesHeldActionsAndPausedUiRetainsRawInput | Unity Editor edit | Pass | Passed | 0.001714 s |  |
| VLAB.Tests.Integration.PhysicsLabRebuildSceneTests.BaseScene_HasOneRigOneUiSystemAndNoMissingScripts | Unity Editor edit | Pass | Passed | 0.605379 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.MicroscopeRequiresPreparationLightBothFocusControlsAndRefocus | Unity Editor edit | Pass | Passed | 0.001516 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.OvershootingFocusBlursAgain | Unity Editor edit | Pass | Passed | 0.000486 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.PhysicalPolarityAndSocketPolarityBothMatter(False,True,Reversed) | Unity Editor edit | Pass | Passed | 0.003191 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.PhysicalPolarityAndSocketPolarityBothMatter(True,False,Reversed) | Unity Editor edit | Pass | Passed | 0.000132 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.PhysicalPolarityAndSocketPolarityBothMatter(True,True,Safe) | Unity Editor edit | Pass | Passed | 0.000098 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.ResistorControlsCurrent(100,Overcurrent,0.03f) | Unity Editor edit | Pass | Passed | 0.000602 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.ResistorControlsCurrent(220,Safe,0.01363636f) | Unity Editor edit | Pass | Passed | 0.000124 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.ResistorControlsCurrent(1000,Dim,0.003f) | Unity Editor edit | Pass | Passed | 0.000089 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.ResistorIsNonPolarized | Unity Editor edit | Pass | Passed | 0.000688 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.SavedScenesHaveWiredReferencesAndNoMissingScripts("Assets/VLAB/DemoLabs/Scenes/EngineeringLab.unity",VLAB.DemoLabs.EngineeringExperiment) | Unity Editor edit | Pass | Passed | 0.631493 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.SavedScenesHaveWiredReferencesAndNoMissingScripts("Assets/VLAB/DemoLabs/Scenes/BiologyLab.unity",VLAB.DemoLabs.BiologyExperiment) | Unity Editor edit | Pass | Passed | 0.643858 s |  |
| VLAB.DemoLabs.Tests.DemoLabModelTests.ShortsBypassesDuplicatesAndMissingPartsCannotPower | Unity Editor edit | Pass | Passed | 0.002721 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.Grabbable_CanReleaseAsDynamicForPhysicalExperimentMotion | Unity Editor edit | Pass | Passed | 0.001977 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.Grabbable_ReportsHeldStateSoSafetyRecoveryCannotInterruptAHand | Unity Editor edit | Pass | Passed | 0.000953 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.LearningController_HasNoUiMethodThatPerformsPhysicalTrial | Unity Editor edit | Pass | Passed | 0.000443 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.PhysicalProgress_AdvancesOnlyFromSourcedApparatusEvents | Unity Editor edit | Pass | Passed | 0.002527 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.ResetManager_DetachesSnappedObjectAndRestoresItsOriginalParent | Unity Editor edit | Pass | Passed | 0.002864 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.ResultSheet_RemainsLockedUntilPhysicalSequenceAndThreeLiveMeasurements | Unity Editor edit | Pass | Passed | 0.006746 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.SnapController_AttachesOnlyCompatibleReleasedApparatus | Unity Editor edit | Pass | Passed | 0.002418 s |  |
| VLAB.PhysicsLab.Tests.PhysicalLabArchitectureTests.TrialRecorder_RejectsPresetDataAndAcceptsLiveComponentMeasurements | Unity Editor edit | Pass | Passed | 0.000849 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabPrefabGenerationTests.GenerateAll_CreatesAndValidatesTwentyProductionPrefabs | Unity Editor edit | Pass | Passed | 1.455035 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabPrefabGenerationTests.GeneratedPrefabs_ExposeOnlyPurposefulPhysicalInteractionPoints | Unity Editor edit | Pass | Passed | 1.313110 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.AttachmentPoint_AttachesAndDetachesCompatibleObject | Unity Editor edit | Pass | Passed | 0.001344 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.CoilSpring_MeasuresPeriodBetweenSuccessiveExtensionPeaks | Unity Editor edit | Pass | Passed | 0.000766 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.FrictionMath_UsesStaticThenKineticLimit | Unity Editor edit | Pass | Passed | 0.000398 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.MeterRuler_ProjectsDistanceOnMeasurementAxis | Unity Editor edit | Pass | Passed | 0.001005 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.MomentumMath_SumsSignedMomentumAlongTrack | Unity Editor edit | Pass | Passed | 0.000294 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.PendulumMath_ReturnsExpectedSmallAnglePeriod | Unity Editor edit | Pass | Passed | 0.000267 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.Photogate_RejectsItsOwnColliders | Unity Editor edit | Pass | Passed | 0.000937 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.PhotogateVelocity_UsesFlagWidthAndBeamBlockDuration | Unity Editor edit | Pass | Passed | 0.000831 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.PhysicsParameter_ClampsAndPublishesSIValue | Unity Editor edit | Pass | Passed | 0.000794 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.ProjectileMath_ProducesRequestedSpeedAndAngle | Unity Editor edit | Pass | Passed | 0.001254 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.ResetManager_RestoresRegisteredObjectTransform | Unity Editor edit | Pass | Passed | 0.001006 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.SpringMath_ReturnsHookeAndDampingForce | Unity Editor edit | Pass | Passed | 0.000285 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.Timer_ModeAPlusB_MeasuresBetweenTwoPorts | Unity Editor edit | Pass | Passed | 0.001550 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabRuntimeTests.Timer_ModeT_MeasuresPeriodBetweenSuccessiveBlocks | Unity Editor edit | Pass | Passed | 0.000498 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_LightingKeepsMobileVrBudgetAndProvidesProbeCoverage | Unity Editor edit | Pass | Passed | 0.580882 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_PreservesCabinetsWithoutDecorativeEquipmentDisplay | Unity Editor edit | Pass | Passed | 0.501620 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_UsesNativeXriInteractionWithoutCustomCrosshairPath | Unity Editor edit | Pass | Passed | 0.541803 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_UsesOrganizedSharedEnvironmentAndRuntimeSizedHdriSky | Unity Editor edit | Pass | Passed | 4.491592 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_UsesProvidedWindowModelWithSingleLayerPerformantGlass | Unity Editor edit | Pass | Passed | 0.610643 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_XrControllerRaysAreConfiguredForWorldAndUi | Unity Editor edit | Pass | Passed | 0.507654 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.Base_XriLocomotionUsesTheHmdCameraAsForwardSource | Unity Editor edit | Pass | Passed | 0.516503 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.BaseAndAllContentScenes_ExistAndUseAdditiveArchitecture | Unity Editor edit | Pass | Passed | 0.697432 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.BuildSettings_ContainsMenuBaseHubAndSixExperiments | Unity Editor edit | Pass | Passed | 0.002076 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.EveryNativeGrab_PreservesRemoteOffsetAndRotationUntilDeliberateManipulation | Unity Editor edit | Pass | Passed | 3.256452 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.EveryWorldInteractable_HasExactlyOneNativeXriBridge | Unity Editor edit | Pass | Passed | 3.257141 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.EveryWorldSpacePhysicsLabCanvas_IsOnUiLayerAndTrackedRaycastable | Unity Editor edit | Pass | Passed | 4.113487 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.ExperimentStations_UseEnlargedTablesAndReadableApparatus | Unity Editor edit | Pass | Passed | 3.333327 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.ExperimentStations_UsePhysicalControllerAndNoWizardControls | Unity Editor edit | Pass | Passed | 3.415277 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.FrictionStation_UsesThreePhysicalSurfaceSamples | Unity Editor edit | Pass | Passed | 0.544909 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.HubSelector_IsWorldSpaceAndControllerRayCompatible | Unity Editor edit | Pass | Passed | 0.507761 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.PhysicsLabScenes_HaveNoMissingScriptsOrMaterials | Unity Editor edit | Pass | Passed | 4.427164 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.PhysicsLabUi_IsResponsiveAndLongResultsAreScrollable | Unity Editor edit | Pass | Passed | 3.809179 s |  |
| VLAB.PhysicsLab.Tests.PhysicsLabSceneArchitectureTests.XRDeviceSimulator_AutoStartsForTemporaryVrTesting | Unity Editor edit | Pass | Passed | 0.040988 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopGrabTests.DisablingDesktop_ReleasesAndRestoresXrManager | Unity Editor play | Pass | Passed | 1.688926 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopGrabTests.HeldBottle_PushedBelowBenchAndReleased_StaysAboveSurface | Unity Editor play | Pass | Passed | 2.179920 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopGrabTests.MouseClick_GrabsMovesUsesAndReleasesBottle | Unity Editor play | Pass | Passed | 0.932951 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopGrabTests.MouseTap_HoldDrainsAndReleaseOrFocusLossStops | Unity Editor play | Pass | Passed | 1.380461 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopGrabTests.ReagentShelfBottlesAndPipette_AreGrabbable | Unity Editor play | Pass | Passed | 0.939139 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopInputBehaviorTests.InputSource_ReleaseDisablesCommandsAndDestroysOwnedAsset | Unity Editor play | Pass | Passed | 0.692195 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopInputBehaviorTests.MouseLook_PreservesLegacySensitivity | Unity Editor play | Pass | Passed | 0.743084 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.DesktopInputBehaviorTests.ScrollCrouchAndEscape_RespectDesktopPresentation | Unity Editor play | Pass | Passed | 1.458874 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.GrabRecoveryTests.TwoHandsHoldIndependentlyAndRecoveryRejectsHeldObjects | Unity Editor play | Pass | Passed | 2.140762 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.ActualPipetteUseAndMeasuredDoses_CompleteThreeConcordantTrials | Unity Editor play | Pass | Passed | 1.013828 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.ClosedLidWrongReagentAndBlockedPour_DoNotAdvanceLesson | Unity Editor play | Pass | Passed | 0.786614 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.ControllerSignals_DriveGripTriggerAndTrackingLoss | Unity Editor play | Pass | Passed | 1.270636 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.PourPreview_MatchesMouthAndRejectsObstructionWithoutConsumingLiquid | Unity Editor play | Pass | Passed | 0.774973 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.ResetChemicals_RestoresStockAndStopsTapWithoutMovingHeldBottle | Unity Editor play | Pass | Passed | 1.178534 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.SpillInvalidatesRecordingAndResetRestoresApparatus | Unity Editor play | Pass | Passed | 0.768166 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.HandsOnTitrationTests.TiltedOpenBottle_PoursThroughMouthAndUprightStopsFlow | Unity Editor play | Pass | Passed | 1.119347 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.LabSettingsTests.PreferencesAndRepeatedClicks_PreserveLessonAndBoundAudioPool | Unity Editor play | Pass | Passed | 0.724237 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.LabTeleportTests.FloorOnlyTeleport_RejectsFurnitureAndMovesRigWithoutChangingLesson | Unity Editor play | Pass | Passed | 1.169808 s |  |
| VLAB.ChemistryLab.Tests.PlayMode.LabTeleportTests.TeleportRay_SelectAndReleaseOnFloor_TeleportsThroughInteractable | Unity Editor play | Pass | Passed | 1.159562 s |  |
| VLAB.DemoLabs.Tests.DemoLabInputTests.FastDragRetainsPickupPositionBeforeBatchedMotionAndRelease | Unity Editor play | Pass | Passed | 0.083803 s |  |
| VLAB.DemoLabs.Tests.DemoLabInputTests.ShortDeviceTapsSurviveOneInputUpdate | Unity Editor play | Pass | Passed | 0.018574 s |  |
| VLAB.DemoLabs.Tests.DemoLabInputTests.SimulatorTouchKeepsItsPositionAfterFingerRelease | Unity Editor play | Pass | Passed | 0.162883 s |  |
| VLAB.DemoLabs.Tests.DemoLabPlaythroughTests.BiologyPartialResetLostSlideFocusOvershootAndViewExit | Unity Editor play | Pass | Passed | 3.589524 s |  |
| VLAB.DemoLabs.Tests.DemoLabPlaythroughTests.BiologyPreparationFocusIdentificationAndReset | Unity Editor play | Pass | Passed | 2.271754 s |  |
| VLAB.DemoLabs.Tests.DemoLabPlaythroughTests.EngineeringAllResistorsShortCircuitPartialResetAndRepeat | Unity Editor play | Pass | Passed | 0.965166 s |  |
| VLAB.DemoLabs.Tests.DemoLabPlaythroughTests.EngineeringCorrectIncorrectResetAndRecovery | Unity Editor play | Pass | Passed | 1.576121 s |  |
| VLAB.DemoLabs.Tests.DemoLabPlaythroughTests.MenuRoutesAllLabsAndKeepsPhysicsAvailable | Unity Editor play | Pass | Passed | 7.281992 s |  |
| VLAB.MainMenu.Tests.VLABMenuFlowTests.CompleteJourney_ReturnResumeAndReentry | Unity Editor play | Pass | Passed | 15.429442 s |  |
| VLAB.MainMenu.Tests.VLABMenuFlowTests.OnboardingRequiresBothExplicitVersionAcceptances | Unity Editor play | Pass | Passed | 0.001254 s |  |
| VLAB.MainMenu.Tests.VLABProductionCellActivityTests.InvalidStructureAndNonFiniteRotationCannotCorruptLesson | Unity Editor play | Pass | Passed | 0.029263 s |  |
| VLAB.MainMenu.Tests.VLABProductionCellActivityTests.PointerCallbacksRespectPause_AndRepeatedBuildKeepsOneModel | Unity Editor play | Pass | Passed | 0.014463 s |  |
| VLAB.MainMenu.Tests.VLABProductionCellActivityTests.WrongStructureDoesNotAdvance_OrderedFunctionsCompleteAndReset | Unity Editor play | Pass | Passed | 0.017929 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.QualitativeRequiresPositiveAndNegativeControlThenResetRestoresBoth | Unity Editor play | Pass | Passed | 0.018972 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.QualitativeTransferNeedsSelectionAndConservesFiniteMaterial | Unity Editor play | Pass | Passed | 0.008625 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.SharedInteractiveObjectsSelectAndTransferAndRespectPause | Unity Editor play | Pass | Passed | 0.010687 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.WaterRejectsWrongElementAndMissingAtoms | Unity Editor play | Pass | Passed | 0.012855 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.WaterRequiresBentGeometryAndSupportsDisassemblyAndReset | Unity Editor play | Pass | Passed | 0.008089 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.LocomotionRootTurnAndMovePreserveLocalHeadPose | Unity Editor play | Pass | Passed | 0.002287 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.MovementUsesSavedSpeedWithoutDiagonalOrPitchAcceleration | Unity Editor play | Pass | Passed | 0.000599 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.NativeSettingsUpdateValuesButNeverEnableTurnWhilePausedOrLocked | Unity Editor play | Pass | Passed | 0.020571 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.SmoothTurnIsFrameRateIndependentAndRejectsInvalidInput | Unity Editor play | Pass | Passed | 0.000957 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.SnapTurnNeedsNeutralBeforeAnotherStep | Unity Editor play | Pass | Passed | 0.000510 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.ChemistryUseButtonFiresOncePerPressAndResetsForNewProvider | Unity Editor play | Pass | Passed | 0.010424 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.ControllerDragChangesActualSlider_AndDeactivationCancelsClick | Unity Editor play | Pass | Passed | 0.030267 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.DisconnectAndProviderSwitchCancelPendingClick | Unity Editor play | Pass | Passed | 0.008506 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.HeadYawDoesNotRotateControllerRay_ExplicitCalibrationRebases | Unity Editor play | Pass | Passed | 0.009177 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.NewlyActivatedModuleRequiresReleaseBeforeFirstClick | Unity Editor play | Pass | Passed | 0.016441 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.PausedUiReceivesRawClickScrollAndDrag_WithoutClickAfterDrag | Unity Editor play | Pass | Passed | 0.016007 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.TimeoutAcceptsRestartedSequence_InvalidPacketsDoNotReviveConnection | Unity Editor play | Pass | Passed | 0.016806 s |  |
| VLAB.MainMenu.Tests.VLABProductionJourneyTests.ControllerRay_SelectsUiInEveryLabIncludingPause | Unity Editor play | Pass | Passed | 12.928605 s |  |
| VLAB.MainMenu.Tests.VLABProductionJourneyTests.SupplementaryLessons_OpenResetRestoreOriginalAndReturnHome | Unity Editor play | Pass | Passed | 25.480305 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.ActivitiesInvalidateCompletionAndResetTheirPhysicalState | Unity Editor play | Pass | Passed | 0.037174 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.GearAssemblyRequiresBothDistinctGearsAndReductionLayout | Unity Editor play | Pass | Passed | 0.000738 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.GearResetRemovesBothGears | Unity Editor play | Pass | Passed | 0.000304 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.LeverResetRestoresKnownUnbalancedConfiguration | Unity Editor play | Pass | Passed | 0.000636 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.LeverUsesSignedMomentAndRequiresOppositeSides | Unity Editor play | Pass | Passed | 0.000499 s |  |
| VLAB.MainMenu.Tests.VLABProductionMicroscopeHandoffTests.DisabledDriverInputDoesNotExitInspectionOrDropOriginalItemButExplicitResetStillWorks | Unity Editor play | Pass | Passed | 0.002953 s |  |
| VLAB.MainMenu.Tests.VLABProductionMicroscopeHandoffTests.WorldScopeFitsViewWithoutMovingHeadAndRestoresOriginalBoardOnExit | Unity Editor play | Pass | Passed | 0.001980 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.CachedRequestedModeDisablesPhoneViewerWithoutChangingSavedPreference | Unity Editor play | Pass | Passed | 0.000718 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.DevicePoseRejectsMissingOrNonFiniteValuesAndNormalizesValidRotation | Unity Editor play | Pass | Passed | 0.000965 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"Menu",True) | Unity Editor play | Pass | Passed | 0.000675 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"PhysicsLab_Base",False) | Unity Editor play | Pass | Passed | 0.000083 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"ChemistryLab",False) | Unity Editor play | Pass | Passed | 0.000069 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"BiologyLab",False) | Unity Editor play | Pass | Passed | 0.000079 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"EngineeringLab",False) | Unity Editor play | Pass | Passed | 0.000065 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(WindowsEditor,"Menu",False) | Unity Editor play | Pass | Passed | 0.000063 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(WindowsPlayer,"Menu",False) | Unity Editor play | Pass | Passed | 0.000061 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"menu",False) | Unity Editor play | Pass | Passed | 0.000060 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,null,False) | Unity Editor play | Pass | Passed | 0.000065 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.CellBoardShowsShortSynopsisAndOffersFullTheory | Unity Editor play | Pass | Passed | 0.018196 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.OpeningFreezesOriginalPhysicsAndCloseRestoresMotionAndRecovery | Unity Editor play | Pass | Passed | 0.111463 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.PitchedEntryFramesBoardAndApparatusWithoutMovingCamera | Unity Editor play | Pass | Passed | 0.022142 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.RepeatedOpenClosePreservesControllerVisualsAndOriginallyDisabledObjects | Unity Editor play | Pass | Passed | 0.047282 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.SharedResetInputTargetsActiveActivityOnceAndStopsAfterClose | Unity Editor play | Pass | Passed | 0.018521 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.VesselHitVolumesAreRegisteredWithTheNativeXrManager | Unity Editor play | Pass | Passed | 0.022359 s |  |
| VLAB.MainMenu.Tests.VLABSpatialMenuTests.MissingDocuments_CanRetryAndBack_CannotAcceptOrZoom | Unity Editor play | Pass | Passed | 5.128598 s |  |
| VLAB.MainMenu.Tests.VLABSpatialMenuTests.MissingScene_ShowsRecoverableError_AndPreservesPause | Unity Editor play | Pass | Passed | 3.909778 s |  |
| VLAB.MainMenu.Tests.VLABSpatialMenuTests.PauseInput_IsolatesHiddenRaycasters_AndRestoresCursor | Unity Editor play | Pass | Passed | 3.013105 s |  |
| VLAB.MainMenu.Tests.VLABSpatialMenuTests.ProductionMenu_PreservesAvailableLabs_AndNavigationReturns | Unity Editor play | Pass | Passed | 7.248850 s |  |
| VLAB.MainMenu.Tests.VLABSpatialMenuTests.TrackedRay_NavigationSettingsDocumentsAndPause | Unity Editor play | Pass | Passed | 23.293014 s |  |
| VLAB.MainMenu.Tests.VLABUnifiedJourneyTests.EveryLab_ReturnsAndReentersWithoutDuplicateOwners | Unity Editor play | Pass | Passed | 32.848904 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabRepairSmokeTests.ExistingMainScenes_LoadWithCameraAndNoMissingScripts | Unity Editor play | Pass | Passed | 5.446638 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabRepairSmokeTests.NativeXriSelection_ReachesBothBridgesOnceAfterReenable | Unity Editor play | Pass | Passed | 0.072014 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabSceneFlowPlayModeTests.BaseScene_LoadsHub_AndCyclesThroughAllExperimentsWithoutDuplicatingRig | Unity Editor play | Pass | Passed | 7.142463 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabSceneFlowPlayModeTests.EveryContentScene_DirectLaunchBootstrapsSharedBaseAndRuntimeServices | Unity Editor play | Pass | Passed | 5.597462 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabSceneFlowPlayModeTests.EveryExperiment_UsesPhysicalControllerWithoutWizardUi | Unity Editor play | Pass | Passed | 5.024488 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabSceneFlowPlayModeTests.HubUiPointerClick_LoadsSelectedExperimentAndCompletesFade | Unity Editor play | Pass | Passed | 1.342641 s |  |
| VLAB.PhysicsLab.Tests.PlayMode.PhysicsLabSceneFlowPlayModeTests.NativeXriSimpleSelection_ActivatesPhysicalApparatusExactlyOnce | Unity Editor play | Pass | Passed | 1.022791 s |  |
| VLAB.MainMenu.Tests.VLABProductionCellActivityTests.InvalidStructureAndNonFiniteRotationCannotCorruptLesson | Unity Editor production | Pass | Passed | 0.200239 s |  |
| VLAB.MainMenu.Tests.VLABProductionCellActivityTests.PointerCallbacksRespectPause_AndRepeatedBuildKeepsOneModel | Unity Editor production | Pass | Passed | 0.049025 s |  |
| VLAB.MainMenu.Tests.VLABProductionCellActivityTests.WrongStructureDoesNotAdvance_OrderedFunctionsCompleteAndReset | Unity Editor production | Pass | Passed | 0.022892 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.QualitativeRequiresPositiveAndNegativeControlThenResetRestoresBoth | Unity Editor production | Pass | Passed | 0.025102 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.QualitativeTransferNeedsSelectionAndConservesFiniteMaterial | Unity Editor production | Pass | Passed | 0.013187 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.SharedInteractiveObjectsSelectAndTransferAndRespectPause | Unity Editor production | Pass | Passed | 0.012395 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.WaterRejectsWrongElementAndMissingAtoms | Unity Editor production | Pass | Passed | 0.016662 s |  |
| VLAB.MainMenu.Tests.VLABProductionChemistryTests.WaterRequiresBentGeometryAndSupportsDisassemblyAndReset | Unity Editor production | Pass | Passed | 0.012114 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.LocomotionRootTurnAndMovePreserveLocalHeadPose | Unity Editor production | Pass | Passed | 0.014770 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.MovementUsesSavedSpeedWithoutDiagonalOrPitchAcceleration | Unity Editor production | Pass | Passed | 0.001593 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.NativeSettingsUpdateValuesButNeverEnableTurnWhilePausedOrLocked | Unity Editor production | Pass | Passed | 0.037228 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.SmoothTurnIsFrameRateIndependentAndRejectsInvalidInput | Unity Editor production | Pass | Passed | 0.001034 s |  |
| VLAB.MainMenu.Tests.VLABProductionComfortLocomotionTests.SnapTurnNeedsNeutralBeforeAnotherStep | Unity Editor production | Pass | Passed | 0.000524 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.ChemistryUseButtonFiresOncePerPressAndResetsForNewProvider | Unity Editor production | Pass | Passed | 0.051677 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.ControllerDragChangesActualSlider_AndDeactivationCancelsClick | Unity Editor production | Pass | Passed | 0.132186 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.DisconnectAndProviderSwitchCancelPendingClick | Unity Editor production | Pass | Passed | 0.010684 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.HeadYawDoesNotRotateControllerRay_ExplicitCalibrationRebases | Unity Editor production | Pass | Passed | 0.010139 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.NewlyActivatedModuleRequiresReleaseBeforeFirstClick | Unity Editor production | Pass | Passed | 0.013177 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.PausedUiReceivesRawClickScrollAndDrag_WithoutClickAfterDrag | Unity Editor production | Pass | Passed | 0.016725 s |  |
| VLAB.MainMenu.Tests.VLABProductionControllerRoutingTests.TimeoutAcceptsRestartedSequence_InvalidPacketsDoNotReviveConnection | Unity Editor production | Pass | Passed | 0.016834 s |  |
| VLAB.MainMenu.Tests.VLABProductionJourneyTests.ControllerRay_SelectsUiInEveryLabIncludingPause | Unity Editor production | Pass | Passed | 13.420855 s |  |
| VLAB.MainMenu.Tests.VLABProductionJourneyTests.SupplementaryLessons_OpenResetRestoreOriginalAndReturnHome | Unity Editor production | Pass | Passed | 25.397180 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.ActivitiesInvalidateCompletionAndResetTheirPhysicalState | Unity Editor production | Pass | Passed | 0.035577 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.GearAssemblyRequiresBothDistinctGearsAndReductionLayout | Unity Editor production | Pass | Passed | 0.000623 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.GearResetRemovesBothGears | Unity Editor production | Pass | Passed | 0.000230 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.LeverResetRestoresKnownUnbalancedConfiguration | Unity Editor production | Pass | Passed | 0.000240 s |  |
| VLAB.MainMenu.Tests.VLABProductionMechanicsTests.LeverUsesSignedMomentAndRequiresOppositeSides | Unity Editor production | Pass | Passed | 0.000405 s |  |
| VLAB.MainMenu.Tests.VLABProductionMicroscopeHandoffTests.DisabledDriverInputDoesNotExitInspectionOrDropOriginalItemButExplicitResetStillWorks | Unity Editor production | Pass | Passed | 0.003911 s |  |
| VLAB.MainMenu.Tests.VLABProductionMicroscopeHandoffTests.WorldScopeFitsViewWithoutMovingHeadAndRestoresOriginalBoardOnExit | Unity Editor production | Pass | Passed | 0.003762 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.CachedRequestedModeDisablesPhoneViewerWithoutChangingSavedPreference | Unity Editor production | Pass | Passed | 0.000669 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.DevicePoseRejectsMissingOrNonFiniteValuesAndNormalizesValidRotation | Unity Editor production | Pass | Passed | 0.000867 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"Menu",True) | Unity Editor production | Pass | Passed | 0.000444 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"PhysicsLab_Base",False) | Unity Editor production | Pass | Passed | 0.000085 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"ChemistryLab",False) | Unity Editor production | Pass | Passed | 0.000060 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"BiologyLab",False) | Unity Editor production | Pass | Passed | 0.000056 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"EngineeringLab",False) | Unity Editor production | Pass | Passed | 0.000054 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(WindowsEditor,"Menu",False) | Unity Editor production | Pass | Passed | 0.000054 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(WindowsPlayer,"Menu",False) | Unity Editor production | Pass | Passed | 0.000051 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,"menu",False) | Unity Editor production | Pass | Passed | 0.000050 s |  |
| VLAB.MainMenu.Tests.VLABProductionMobileVrTests.VrModeChangesAreRestrictedToAndroidMainMenu(Android,null,False) | Unity Editor production | Pass | Passed | 0.000053 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.CellBoardShowsShortSynopsisAndOffersFullTheory | Unity Editor production | Pass | Passed | 0.018395 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.OpeningFreezesOriginalPhysicsAndCloseRestoresMotionAndRecovery | Unity Editor production | Pass | Passed | 0.082640 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.PitchedEntryFramesBoardAndApparatusWithoutMovingCamera | Unity Editor production | Pass | Passed | 0.016629 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.RepeatedOpenClosePreservesControllerVisualsAndOriginallyDisabledObjects | Unity Editor production | Pass | Passed | 0.041352 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.SharedResetInputTargetsActiveActivityOnceAndStopsAfterClose | Unity Editor production | Pass | Passed | 0.025461 s |  |
| VLAB.MainMenu.Tests.VLABProductionWorkbenchTests.VesselHitVolumesAreRegisteredWithTheNativeXrManager | Unity Editor production | Pass | Passed | 0.019004 s |  |
