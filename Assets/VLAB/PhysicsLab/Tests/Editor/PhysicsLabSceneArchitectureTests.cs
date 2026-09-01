using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VLAB.PhysicsLab.Education;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.Mechanics;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Tests
{
    public sealed class PhysicsLabSceneArchitectureTests
    {
        private const string SceneRoot = "Assets/VLAB/PhysicsLab/Scenes";
        private static readonly string[] ContentScenes =
        {
            "PhysicsLab_Hub",
            "Physics_Pendulum",
            "Physics_Projectile",
            "Physics_Friction",
            "Physics_PhotogateMotion",
            "Physics_Spring",
            "Physics_AirTrackMomentum",
        };

        [Test]
        public void BaseAndAllContentScenes_ExistAndUseAdditiveArchitecture()
        {
            var basePath = $"{SceneRoot}/PhysicsLab_Base.unity";
            Assert.That(File.Exists(basePath), Is.True, basePath);
            foreach (var sceneName in ContentScenes)
            {
                Assert.That(File.Exists($"{SceneRoot}/{sceneName}.unity"), Is.True, sceneName);
            }

            var baseScene = EditorSceneManager.OpenScene(basePath, OpenSceneMode.Single);
            Assert.That(FindRoot(baseScene, "_SYSTEMS"), Is.Not.Null);
            Assert.That(FindRoot(baseScene, "_PLAYER"), Is.Not.Null);
            Assert.That(FindRoot(baseScene, "_ENVIRONMENT"), Is.Not.Null);
            Assert.That(FindRoot(baseScene, "_UI"), Is.Not.Null);
            Assert.That(CountComponents<Camera>(baseScene), Is.EqualTo(1));
            Assert.That(CountComponents<AudioListener>(baseScene), Is.EqualTo(1));
            Assert.That(CountComponents<EventSystem>(baseScene), Is.EqualTo(1));

            foreach (var sceneName in ContentScenes)
            {
                var content = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Additive);
                Assert.That(CountComponents<Camera>(content), Is.Zero, sceneName);
                Assert.That(CountComponents<AudioListener>(content), Is.Zero, sceneName);
                Assert.That(CountComponents<EventSystem>(content), Is.Zero, sceneName);
                Assert.That(FindRoot(content, sceneName == "PhysicsLab_Hub" ? "_HUB" : "_STATION"), Is.Not.Null, sceneName);
                EditorSceneManager.CloseScene(content, true);
            }
        }

        [Test]
        public void BuildSettings_ContainsHomeBaseHubAndSixExperiments()
        {
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Assert.That(enabled, Does.Contain("Assets/Home.unity"));
            Assert.That(enabled, Does.Contain($"{SceneRoot}/PhysicsLab_Base.unity"));
            foreach (var sceneName in ContentScenes)
            {
                Assert.That(enabled, Does.Contain($"{SceneRoot}/{sceneName}.unity"), sceneName);
            }
        }

        [Test]
        public void PhysicsLabScenes_HaveNoMissingScriptsOrMaterials()
        {
            var sceneNames = new[] { "PhysicsLab_Base" }.Concat(ContentScenes);
            foreach (var sceneName in sceneNames)
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                {
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.Zero, $"Missing script under {sceneName}/{root.name}");
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        Assert.That(renderer.sharedMaterials, Has.None.Null, $"Missing material on {sceneName}/{renderer.name}");
                    }
                }
            }
        }

        [Test]
        public void XRDeviceSimulator_AutoStartsForTemporaryVrTesting()
        {
            const string settingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
            Assert.That(File.Exists(settingsPath), Is.True);
            Assert.That(File.ReadAllText(settingsPath), Does.Contain("m_AutomaticallyInstantiateSimulatorPrefab: 1"));
        }

        [Test]
        public void Base_UsesNativeXriInteractionWithoutCustomCrosshairPath()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Assert.That(transforms.Any(item => item.name == "XR Origin (XR Rig)"), Is.True);
            Assert.That(transforms.Any(item => item.name == "XR Interaction Manager"), Is.True);
            Assert.That(transforms.Any(item => item.name == "DesktopRig"), Is.False);
            Assert.That(transforms.Count(item => item.name == "Near-Far Interactor"), Is.GreaterThanOrEqualTo(2));
            Assert.That(transforms.Any(item => item.name == "Crosshair"), Is.False);
            Assert.That(CountComponents<DesktopPlayerRig>(scene), Is.Zero);
            Assert.That(CountComponents<InteractionRaycaster>(scene), Is.Zero);
            Assert.That(CountComponents<GrabController>(scene), Is.Zero);
            Assert.That(CountComponents<XRUIInputModule>(scene), Is.EqualTo(1));
        }

        [Test]
        public void Base_XriLocomotionUsesTheHmdCameraAsForwardSource()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true));
            var moveProvider = components.FirstOrDefault(item => item != null && item.GetType().Name == "DynamicMoveProvider");
            Assert.That(moveProvider, Is.Not.Null);

            var serialized = new SerializedObject(moveProvider);
            var forwardSource = serialized.FindProperty("m_ForwardSource");
            var enableStrafe = serialized.FindProperty("m_EnableStrafe");
            Assert.That(forwardSource, Is.Not.Null);
            Assert.That(forwardSource.objectReferenceValue, Is.EqualTo(CountComponents<Camera>(scene) == 1
                ? scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single().transform
                : null));
            Assert.That(enableStrafe, Is.Not.Null);
            Assert.That(enableStrafe.boolValue, Is.True);
        }

        [Test]
        public void HubSelector_IsWorldSpaceAndControllerRayCompatible()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Hub.unity", OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Single(item => item.name == "ExperimentSelectorCanvas");
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.GetComponent<TrackedDeviceGraphicRaycaster>(), Is.Not.Null);
            Assert.That(canvas.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(7));
            Assert.That(canvas.GetComponentsInChildren<Button>(true).All(button => button.targetGraphic != null && button.targetGraphic.raycastTarget), Is.True);
            var consoleBase = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(item => item.name == "Base" && item.parent != null && item.parent.name == "VLAB_ExperimentConsole");
            Assert.That(canvas.transform.position.z, Is.LessThan(consoleBase.GetComponent<Renderer>().bounds.min.z - 0.01f),
                "The world-space selector must sit in front of the physical console so no button row is occluded.");
        }

        [Test]
        public void EveryWorldInteractable_HasExactlyOneNativeXriBridge()
        {
            foreach (var sceneName in ContentScenes.Skip(1))
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                var interactables = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LabInteractable>(true)).ToArray();
                Assert.That(interactables, Is.Not.Empty, sceneName);
                foreach (var interactable in interactables)
                {
                    var grabbable = interactable.GetComponent<LabGrabbable>();
                    if (grabbable != null)
                    {
                        Assert.That(interactable.GetComponents<XRGrabInteractable>(), Has.Length.EqualTo(1), $"{sceneName}/{interactable.name}");
                        Assert.That(interactable.GetComponents<XrGrabEventBridge>(), Has.Length.EqualTo(1), $"{sceneName}/{interactable.name}");
                    }
                    else
                    {
                        Assert.That(interactable.GetComponents<XRSimpleInteractable>(), Has.Length.EqualTo(1), $"{sceneName}/{interactable.name}");
                        Assert.That(interactable.GetComponents<XrSimpleInteractableBridge>(), Has.Length.EqualTo(1), $"{sceneName}/{interactable.name}");
                    }
                }
            }
        }

        [Test]
        public void PhysicsLabUi_IsResponsiveAndLongResultsAreScrollable()
        {
            var hub = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Hub.unity", OpenSceneMode.Single);
            var hubLabels = hub.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .Select(button => button.GetComponentInChildren<TextMeshProUGUI>(true));
            Assert.That(hubLabels.All(label => label != null && label.enableAutoSizing && label.fontSizeMin >= 12f), Is.True);

            foreach (var sceneName in ContentScenes.Skip(1))
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                var resultsPanel = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Single(item => item.name == "ResultsPanel");
                var scrollRect = resultsPanel.GetComponentInChildren<ScrollRect>(true);
                Assert.That(scrollRect, Is.Not.Null, sceneName);
                Assert.That(scrollRect.vertical, Is.True, sceneName);
                Assert.That(scrollRect.viewport.GetComponent<RectMask2D>(), Is.Not.Null, sceneName);
                Assert.That(scrollRect.verticalScrollbar, Is.Not.Null, sceneName);
                Assert.That(scrollRect.verticalScrollbarVisibility, Is.EqualTo(ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport), sceneName);

                var summary = roots.SelectMany(root => root.GetComponentsInChildren<TextMeshProUGUI>(true)).Single(item => item.name == "ResultsSummary");
                Assert.That(summary.transform.IsChildOf(scrollRect.content), Is.True, sceneName);
                Assert.That(summary.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal), sceneName);
                summary.text = string.Concat(Enumerable.Repeat("Số đo thực nghiệm dài có dấu tiếng Việt và phần giải thích khoa học cần cuộn. ", 40));
                var preferred = summary.GetPreferredValues(summary.text, scrollRect.viewport.rect.width - 24f, 0f);
                Assert.That(preferred.y, Is.GreaterThan(scrollRect.viewport.rect.height), sceneName);
            }
        }

        [Test]
        public void Base_UsesOrganizedSharedEnvironmentAndRuntimeSizedHdriSky()
        {
            const string hdriPath = "Assets/VLAB/PhysicsLab/Environment/Exterior/qwantani_moon_noon_puresky_4k.exr";
            const string skyboxPath = "Assets/VLAB/PhysicsLab/Environment/Materials/VLAB_Qwantani_Skybox.mat";
            Assert.That(File.Exists(hdriPath), Is.True, "The source HDRI must be copied into the organized shared environment folder.");
            Assert.That(File.Exists(skyboxPath), Is.True);

            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var environment = FindRoot(scene, "_ENVIRONMENT");
            var shared = environment.transform.Find("ENV_PhysicsLab");
            Assert.That(shared, Is.Not.Null);
            var requiredGroups = new[]
            {
                "Architecture", "Windows", "Furniture", "Cabinets",
                "Lighting", "ReflectionProbes", "LightProbes", "Exterior", "Branding",
            };
            foreach (var group in requiredGroups)
            {
                Assert.That(shared.Find(group), Is.Not.Null, group);
            }

            Assert.That(RenderSettings.skybox, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(RenderSettings.skybox), Is.EqualTo(skyboxPath));
            Assert.That(RenderSettings.skybox.shader.name, Is.EqualTo("Skybox/Panoramic"));
            Assert.That(RenderSettings.skybox.GetFloat("_Mapping"), Is.EqualTo(1f));
            var skyTexture = RenderSettings.skybox.GetTexture("_MainTex");
            Assert.That(skyTexture, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(skyTexture), Is.EqualTo(hdriPath));
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(UnityEngine.Rendering.AmbientMode.Skybox));
            Assert.That(RenderSettings.ambientIntensity, Is.InRange(0.45f, 0.85f));

            var importer = AssetImporter.GetAtPath(hdriPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), "Preserve the source file while limiting runtime texture residency for mobile VR.");
            Assert.That(importer.textureShape, Is.EqualTo(TextureImporterShape.Texture2D), "A latitude-longitude panoramic sky must remain a 2D equirectangular texture.");

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(hdriPath);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(texture.height * 2), "The runtime sky must keep a 2:1 equirectangular aspect ratio.");

            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
            Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.Skybox));
            Assert.That(shared.Find("Exterior/ExteriorHorizonDeck"), Is.Null, "A flat exterior deck must not hide the lower half of the 360-degree panorama.");

            foreach (var contentScene in ContentScenes)
            {
                EditorSceneManager.OpenScene($"{SceneRoot}/{contentScene}.unity", OpenSceneMode.Single);
                Assert.That(AssetDatabase.GetAssetPath(RenderSettings.skybox), Is.EqualTo(skyboxPath), $"{contentScene} must retain the HDRI while it is the active additive scene.");
            }
        }

        [Test]
        public void Base_UsesProvidedWindowModelWithSingleLayerPerformantGlass()
        {
            const string windowModelPath = "Assets/VLAB/PhysicsLab/Environment/Models/VLAB_Architectural_Window.fbx";
            Assert.That(File.Exists(windowModelPath), Is.True, "The supplied GLB should be converted to an organized Unity-friendly model.");

            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var windows = transforms.Where(item => item.name.StartsWith("ArchitecturalWindow_", System.StringComparison.Ordinal)).ToArray();
            Assert.That(windows, Has.Length.EqualTo(3));
            Assert.That(windows.All(item => item.GetComponentsInChildren<Renderer>(true).Length > 0), Is.True);
            Assert.That(windows.All(item => item.GetComponentsInChildren<Collider>(true).Length <= 1), Is.True);
            Assert.That(windows.All(item => item.GetComponentsInChildren<Rigidbody>(true).Length == 0), Is.True);

            var glassRenderers = transforms.Where(item => item.name == "GlassPane").Select(item => item.GetComponent<Renderer>()).ToArray();
            Assert.That(glassRenderers, Has.Length.EqualTo(3));
            Assert.That(glassRenderers.All(renderer => renderer != null && renderer.sharedMaterial != null), Is.True);
            Assert.That(glassRenderers.Select(renderer => renderer.sharedMaterial).Distinct().Count(), Is.EqualTo(1), "All windows should share one glass material.");
        }

        [Test]
        public void Base_LightingKeepsMobileVrBudgetAndProvidesProbeCoverage()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var lights = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
            Assert.That(lights.Length, Is.InRange(3, 4));
            Assert.That(lights.Count(light => light.shadows != LightShadows.None), Is.EqualTo(1), "Only the key light may cast realtime shadows.");
            Assert.That(lights.Count(light => light.type == LightType.Directional), Is.EqualTo(1));
            Assert.That(lights.Where(light => light.type != LightType.Directional).All(light => light.shadows == LightShadows.None), Is.True);

            var probes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ReflectionProbe>(true)).ToArray();
            Assert.That(probes, Has.Length.EqualTo(2));
            Assert.That(probes.All(probe => probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Baked), Is.True);
            Assert.That(probes.All(probe => probe.resolution <= 128), Is.True);

            var lightProbeGroups = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LightProbeGroup>(true)).ToArray();
            Assert.That(lightProbeGroups, Has.Length.EqualTo(1));
            Assert.That(lightProbeGroups[0].probePositions.Length, Is.GreaterThanOrEqualTo(18));
        }

        [Test]
        public void Base_PreservesCabinetsWithoutDecorativeEquipmentDisplay()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Assert.That(transforms.Count(item => item.name == "WestPhysicsCabinet"), Is.EqualTo(1));
            Assert.That(transforms.Count(item => item.name == "EastPhysicsCabinet"), Is.EqualTo(1));
            Assert.That(transforms.Any(item => item.name == "EquipmentDisplay"), Is.False);
            Assert.That(transforms.Any(item => item.name.StartsWith("Display_", System.StringComparison.Ordinal)), Is.False);
            Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component => component != null && component.GetType().Name == "PhysicsLabEquipmentDisplayVisibility"), Is.False);
        }

        [Test]
        public void Base_XrControllerRaysAreConfiguredForWorldAndUi()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/PhysicsLab_Base.unity", OpenSceneMode.Single);
            var interactors = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<NearFarInteractor>(true)).ToArray();
            Assert.That(interactors, Has.Length.GreaterThanOrEqualTo(2));
            var interactableLayer = LayerMask.NameToLayer("Interactable");
            var uiLayer = LayerMask.NameToLayer("UI");
            Assert.That(interactableLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(uiLayer, Is.GreaterThanOrEqualTo(0));

            foreach (var interactor in interactors)
            {
                Assert.That(interactor.enableUIInteraction, Is.True, interactor.name);
                Assert.That(interactor.enableFarCasting, Is.True, interactor.name);
                var caster = interactor.farInteractionCaster as CurveInteractionCaster;
                Assert.That(caster, Is.Not.Null, interactor.name);
                Assert.That((caster.raycastMask.value & (1 << interactableLayer)) != 0, Is.True, interactor.name);
                Assert.That((caster.raycastMask.value & (1 << uiLayer)) != 0, Is.True, interactor.name);
                Assert.That((caster.raycastMask.value & 1) != 0, Is.True, $"{interactor.name} must retain Default-layer hits for existing apparatus and UI.");
            }

            var attachControllers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<InteractionAttachController>(true)).ToArray();
            Assert.That(attachControllers, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(attachControllers.All(controller => controller.useManipulationInput), Is.True);
            Assert.That(attachControllers.All(controller => controller.manipulationXAxisMode == InteractionAttachController.ManipulationXAxisMode.HorizontalRotation), Is.True);
            Assert.That(attachControllers.All(controller => controller.manipulationYAxisMode == InteractionAttachController.ManipulationYAxisMode.Translate), Is.True);
            Assert.That(attachControllers.All(controller => !controller.useMomentum), Is.True);
        }

        [Test]
        public void EveryWorldSpacePhysicsLabCanvas_IsOnUiLayerAndTrackedRaycastable()
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            foreach (var sceneName in ContentScenes)
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                var canvases = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                    .Where(canvas => canvas.renderMode == RenderMode.WorldSpace).ToArray();
                Assert.That(canvases, Is.Not.Empty, sceneName);
                foreach (var canvas in canvases)
                {
                    Assert.That(canvas.GetComponent<TrackedDeviceGraphicRaycaster>(), Is.Not.Null, $"{sceneName}/{canvas.name}");
                    Assert.That(canvas.GetComponentsInChildren<Transform>(true).All(item => item.gameObject.layer == uiLayer), Is.True, $"{sceneName}/{canvas.name}");
                    Assert.That(canvas.GetComponentsInChildren<Selectable>(true).All(selectable => selectable.targetGraphic != null && selectable.targetGraphic.raycastTarget), Is.True, $"{sceneName}/{canvas.name}");
                }
            }
        }

        [Test]
        public void EveryNativeGrab_PreservesRemoteOffsetAndRotationUntilDeliberateManipulation()
        {
            foreach (var sceneName in ContentScenes.Skip(1))
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                var grabs = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<XRGrabInteractable>(true)).ToArray();
                Assert.That(grabs, Is.Not.Empty, sceneName);
                foreach (var grab in grabs)
                {
                    Assert.That(grab.useDynamicAttach, Is.True, $"{sceneName}/{grab.name}");
                    Assert.That(grab.matchAttachPosition, Is.True, $"{sceneName}/{grab.name}");
                    Assert.That(grab.matchAttachRotation, Is.False, $"{sceneName}/{grab.name}");
                    Assert.That(grab.snapToColliderVolume, Is.False, $"{sceneName}/{grab.name}");
                    Assert.That(grab.trackPosition, Is.True, $"{sceneName}/{grab.name}");
                    Assert.That(grab.trackRotation, Is.False, $"{sceneName}/{grab.name}");
                    Assert.That(grab.movementType, Is.EqualTo(XRBaseInteractable.MovementType.Kinematic), $"{sceneName}/{grab.name}");
                    Assert.That(grab.attachEaseInTime, Is.GreaterThanOrEqualTo(0.12f), $"{sceneName}/{grab.name}");
                    Assert.That(grab.throwOnDetach, Is.False, $"{sceneName}/{grab.name}");
                    var bridge = grab.GetComponent<XrGrabEventBridge>();
                    Assert.That(bridge, Is.Not.Null, $"{sceneName}/{grab.name}");
                    var constrained = grab.name == "Pendulum_Bob" || grab.name.StartsWith("Air_Track_Glider", System.StringComparison.Ordinal);
                    Assert.That(bridge.AllowsDeliberateRotation, Is.EqualTo(!constrained), $"{sceneName}/{grab.name}");
                }
            }
        }

        [Test]
        public void ExperimentStations_UseEnlargedTablesAndReadableApparatus()
        {
            foreach (var sceneName in ContentScenes.Skip(1))
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var table = transforms.FirstOrDefault(item => item.name == "Experiment_Table");
                Assert.That(table, Is.Not.Null, sceneName);
                Assert.That(table.localScale.x, Is.EqualTo(1.48f).Within(0.001f), sceneName);
                Assert.That(table.localScale.y, Is.EqualTo(1.15f).Within(0.001f), sceneName);
                Assert.That(transforms.Any(item => item != table && Mathf.Approximately(item.localScale.x, 1.28f)), Is.True, sceneName);
            }
        }

        [Test]
        public void ExperimentStations_UsePhysicalControllerAndNoWizardControls()
        {
            foreach (var sceneName in ContentScenes.Skip(1))
            {
                var scene = EditorSceneManager.OpenScene($"{SceneRoot}/{sceneName}.unity", OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.Sum(root => root.GetComponentsInChildren<ExperimentPhysicalController>(true).Length), Is.EqualTo(1), sceneName);
                Assert.That(roots.Sum(root => root.GetComponentsInChildren<ExperimentSafetyBoundary>(true).Length), Is.EqualTo(1), sceneName);
                var names = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(item => item.name).ToArray();
                Assert.That(names, Does.Not.Contain("LearningWorkspace"), sceneName);
                Assert.That(names, Does.Not.Contain("ProgressRail"), sceneName);
                Assert.That(names, Does.Not.Contain("PrimaryAction"), sceneName);
                Assert.That(names, Does.Not.Contain("IncreaseParameter"), sceneName);
                Assert.That(names, Does.Not.Contain("DecreaseParameter"), sceneName);
                Assert.That(names, Does.Contain("ContextualLabPanel"), sceneName);
                Assert.That(names, Does.Contain("SettingsPanel"), sceneName);
                Assert.That(names, Does.Contain("HelpPanel"), sceneName);
                var stationCanvas = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).First(item => item.name == "StationCanvas");
                Assert.That(Quaternion.Angle(stationCanvas.transform.rotation, Quaternion.identity), Is.LessThan(0.1f), $"{sceneName} context panel must face the player, not render mirrored.");
            }
        }

        [Test]
        public void FrictionStation_UsesThreePhysicalSurfaceSamples()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneRoot}/Physics_Friction.unity", OpenSceneMode.Single);
            Assert.That(CountComponents<FrictionSurfaceSelector>(scene), Is.EqualTo(3));
        }

        private static int CountComponents<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<T>(true).Length);

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }
}
