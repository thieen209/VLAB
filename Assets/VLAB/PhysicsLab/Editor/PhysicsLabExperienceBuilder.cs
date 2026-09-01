using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VLAB.Core.Input;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Education;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.SceneFlow;
using Object = UnityEngine.Object;

namespace VLAB.PhysicsLab.Editor
{
    public static class PhysicsLabExperienceBuilder
    {
        private const string SceneRoot = "Assets/VLAB/PhysicsLab/Scenes";
        private const string MaterialRoot = "Assets/VLAB/PhysicsLab/Environment/Materials";
        private const string ExteriorRoot = "Assets/VLAB/PhysicsLab/Environment/Exterior";
        private const string EnvironmentModelRoot = "Assets/VLAB/PhysicsLab/Environment/Models";
        private const string PrefabRoot = "Assets/VLAB/PhysicsLab/Prefabs";
        private const string HdriPath = ExteriorRoot + "/qwantani_moon_noon_puresky_4k.exr";
        private const string WindowModelPath = EnvironmentModelRoot + "/VLAB_Architectural_Window.fbx";
        private const string SkyboxMaterialPath = MaterialRoot + "/VLAB_Qwantani_Skybox.mat";
        private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string LegacyRebuildScene = "Assets/VLAB/Physics/Scenes/PhysicsLab_Rebuild.unity";

        private static Material navy;
        private static Material dark;
        private static Material warmWhite;
        private static Material floor;
        private static Material cyan;
        private static Material green;
        private static Material orange;
        private static Material metal;
        private static Material graphite;
        private static Material softBlue;
        private static Material wood;
        private static Material glass;
        private static Material skybox;
        private static Material uiText;

        [MenuItem("Tools/VLAB/Physics Lab/Build Polished Experience")]
        public static void BuildAll()
        {
            EnsureFolders();
            PhysicsLabAssetPipeline.GenerateAll();
            ConfigureEnvironmentImports();
            CreateMaterials();
            PreserveRebuildSceneGuid();
            BuildBaseScene();
            BuildHubScene();
            BuildStationScenes();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLAB Physics Lab] Built one reusable base lab, hub and six additive apparatus scenes.");
        }

        public static void BuildAllFromCommandLine() => BuildAll();

        [MenuItem("Tools/VLAB/Physics Lab/Rebuild Base Environment Only")]
        public static void RebuildBaseEnvironmentOnly()
        {
            EnsureFolders();
            ConfigureEnvironmentImports();
            CreateMaterials();
            BuildBaseScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLAB Physics Lab] Rebuilt the shared Base environment only.");
        }

        public static void RebuildBaseFromCommandLine() => RebuildBaseEnvironmentOnly();

        [MenuItem("Tools/VLAB/Physics Lab/Rebuild Experiment Stations Only")]
        public static void RebuildExperimentStationsOnly()
        {
            EnsureFolders();
            ConfigureEnvironmentImports();
            CreateMaterials();
            BuildStationScenes();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLAB Physics Lab] Rebuilt six enlarged experiment stations only.");
        }

        public static void RebuildStationsFromCommandLine() => RebuildExperimentStationsOnly();

        [InitializeOnLoadMethod]
        private static void RunQueuedRebuildAfterScriptReload()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var requestPath = Path.Combine(projectRoot ?? string.Empty, "Temp", "VLAB_RebuildXrBaseAndStations.request");
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (File.Exists(requestPath))
                {
                    try
                    {
                        RebuildBaseEnvironmentOnly();
                        RebuildExperimentStationsOnly();
                    }
                    finally
                    {
                        File.Delete(requestPath);
                    }
                }
            };
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(SceneRoot);
            Directory.CreateDirectory(MaterialRoot);
            Directory.CreateDirectory(ExteriorRoot);
            Directory.CreateDirectory(EnvironmentModelRoot);
        }

        private static void ConfigureEnvironmentImports()
        {
            var hdriImporter = AssetImporter.GetAtPath(HdriPath) as TextureImporter;
            if (hdriImporter != null &&
                (hdriImporter.maxTextureSize != 2048 || hdriImporter.mipmapEnabled == false || hdriImporter.sRGBTexture ||
                 hdriImporter.textureShape != TextureImporterShape.Texture2D ||
                 hdriImporter.wrapModeU != TextureWrapMode.Repeat || hdriImporter.wrapModeV != TextureWrapMode.Clamp))
            {
                hdriImporter.maxTextureSize = 2048;
                hdriImporter.mipmapEnabled = true;
                hdriImporter.sRGBTexture = false;
                hdriImporter.textureShape = TextureImporterShape.Texture2D;
                hdriImporter.wrapModeU = TextureWrapMode.Repeat;
                hdriImporter.wrapModeV = TextureWrapMode.Clamp;
                hdriImporter.textureCompression = TextureImporterCompression.Compressed;
                hdriImporter.SaveAndReimport();
            }

            var modelImporter = AssetImporter.GetAtPath(WindowModelPath) as ModelImporter;
            if (modelImporter != null &&
                (modelImporter.importCameras || modelImporter.importLights || modelImporter.importAnimation || modelImporter.isReadable))
            {
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.importAnimation = false;
                modelImporter.isReadable = false;
                modelImporter.SaveAndReimport();
            }
        }

        private static void PreserveRebuildSceneGuid()
        {
            var basePath = ScenePath(PhysicsLabSceneNames.Base);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(basePath) == null && AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyRebuildScene) != null)
            {
                var error = AssetDatabase.MoveAsset(LegacyRebuildScene, basePath);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"Unable to upgrade PhysicsLab_Rebuild scene: {error}");
                }
            }
        }

        private static void CreateMaterials()
        {
            navy = CreateMaterial("VLAB_Navy", Hex("0A2463"), 0.15f, 0.62f);
            dark = CreateMaterial("VLAB_Dark", Hex("0D1117"), 0.05f, 0.52f);
            warmWhite = CreateMaterial("VLAB_WarmWhite", Hex("F8F9FA"), 0.0f, 0.36f);
            floor = CreateMaterial("VLAB_Floor", new Color(0.16f, 0.19f, 0.23f), 0.18f, 0.48f);
            cyan = CreateMaterial("VLAB_Cyan", Hex("00B4D8"), 0.05f, 0.58f, 1.25f);
            green = CreateMaterial("VLAB_Green", Hex("06D6A0"), 0.05f, 0.56f, 0.9f);
            orange = CreateMaterial("VLAB_Orange", Hex("FF6B35"), 0.05f, 0.46f, 0.35f);
            metal = CreateMaterial("VLAB_Metal", new Color(0.34f, 0.39f, 0.44f), 0.82f, 0.64f);
            graphite = CreateMaterial("VLAB_Graphite", new Color(0.085f, 0.105f, 0.13f), 0.30f, 0.58f);
            softBlue = CreateMaterial("VLAB_SoftBlue", new Color(0.60f, 0.72f, 0.82f), 0.05f, 0.42f);
            wood = CreateMaterial("VLAB_WarmWood", new Color(0.34f, 0.20f, 0.11f), 0.0f, 0.34f);
            glass = CreateGlassMaterial();
            skybox = CreateSkyboxMaterial();
            uiText = CreateTextMaterial();
        }

        private static Material CreateGlassMaterial()
        {
            var material = CreateMaterial("VLAB_WindowGlass", new Color(0.52f, 0.72f, 0.78f, 0.16f), 0.05f, 0.82f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSkyboxMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                throw new InvalidOperationException("Built-in panoramic skybox shader was not found.");
            }
            if (material == null)
            {
                material = new Material(shader) { name = "VLAB_Qwantani_Skybox" };
                AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(HdriPath);
            if (texture == null)
            {
                throw new FileNotFoundException($"Physics Lab HDRI was not imported: {HdriPath}");
            }
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Mapping", 1f);
            material.SetFloat("_ImageType", 0f);
            material.SetFloat("_Exposure", 0.72f);
            material.SetFloat("_Rotation", 132f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTextMaterial()
        {
            var path = $"{MaterialRoot}/VLAB_UI_Text.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var source = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset.material : null;
                if (source == null)
                {
                    throw new InvalidOperationException("TextMesh Pro default font material was not found.");
                }
                material = new Material(source) { name = "VLAB_UI_Text" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, float emission = 0f)
        {
            var path = $"{MaterialRoot}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Built-in Standard shader was not found.");
                }
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildBaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var systems = Root("_SYSTEMS");
            var player = Root("_PLAYER");
            var environment = Root("_ENVIRONMENT");
            var ui = Root("_UI");

            var inputObject = Child(systems.transform, "DesktopInput");
            var provider = inputObject.AddComponent<SimulatorInputProvider>();
            var input = inputObject.AddComponent<InputManager>();
            input.SetProvider(provider);

            var eventSystem = Child(systems.transform, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<XRUIInputModule>();

            var xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
            if (xrOriginPrefab == null)
            {
                throw new FileNotFoundException($"Unity XRI Starter Assets rig not found: {XrOriginPrefabPath}");
            }
            var xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab, player.transform);
            xrOrigin.name = "XR Origin (XR Rig)";
            xrOrigin.transform.localPosition = new Vector3(0f, 0.05f, -2.9f);
            xrOrigin.transform.localRotation = Quaternion.identity;
            xrOrigin.transform.localScale = Vector3.one;

            var xrCamera = xrOrigin.GetComponentInChildren<Camera>(true);
            if (xrCamera == null)
            {
                throw new InvalidOperationException("Unity XRI Starter Assets rig has no camera.");
            }
            xrCamera.clearFlags = CameraClearFlags.Skybox;
            ConfigureNativeXriRig(xrOrigin);

            var interactionManagerObject = Child(systems.transform, "XR Interaction Manager");
            interactionManagerObject.AddComponent<XRInteractionManager>();

            var sharedEnvironment = BuildEnvironment(environment.transform);
            BuildLighting(sharedEnvironment);

            var fade = BuildFadeCanvas(ui.transform);
            var flowObject = Child(systems.transform, "PhysicsLabSceneFlow");
            var flow = flowObject.AddComponent<PhysicsLabSceneFlow>();
            flow.Configure(fade, PhysicsLabSceneNames.Hub);
            BuildStudentHud(ui.transform);

            SaveScene(scene, ScenePath(PhysicsLabSceneNames.Base));
        }

        private static Transform BuildEnvironment(Transform parent)
        {
            var shared = Child(parent, "ENV_PhysicsLab").transform;
            var architecture = Child(shared, "Architecture").transform;
            Primitive("Floor", architecture, new Vector3(0f, -0.10f, 0f), new Vector3(12f, 0.20f, 9f), floor, true);
            Primitive("Ceiling", architecture, new Vector3(0f, 4.24f, 0f), new Vector3(12f, 0.16f, 9f), graphite, false);
            Primitive("NorthWindowSillWall", architecture, new Vector3(0f, 0.47f, 4.50f), new Vector3(12f, 0.94f, 0.18f), warmWhite, true);
            Primitive("NorthWindowHeader", architecture, new Vector3(0f, 3.72f, 4.50f), new Vector3(12f, 0.80f, 0.18f), warmWhite, true);
            Primitive("NorthPierLeft", architecture, new Vector3(-5.75f, 2.25f, 4.50f), new Vector3(0.50f, 2.55f, 0.18f), warmWhite, true);
            Primitive("NorthPierRight", architecture, new Vector3(5.75f, 2.25f, 4.50f), new Vector3(0.50f, 2.55f, 0.18f), warmWhite, true);
            Primitive("NorthPierInnerLeft", architecture, new Vector3(-1.88f, 2.25f, 4.50f), new Vector3(0.34f, 2.55f, 0.18f), warmWhite, true);
            Primitive("NorthPierInnerRight", architecture, new Vector3(1.88f, 2.25f, 4.50f), new Vector3(0.34f, 2.55f, 0.18f), warmWhite, true);
            Primitive("SouthWallLeft", architecture, new Vector3(-3.65f, 2.05f, -4.50f), new Vector3(4.70f, 4.10f, 0.18f), warmWhite, true);
            Primitive("SouthWallRight", architecture, new Vector3(3.65f, 2.05f, -4.50f), new Vector3(4.70f, 4.10f, 0.18f), warmWhite, true);
            Primitive("SouthDoorHeader", architecture, new Vector3(0f, 3.70f, -4.50f), new Vector3(2.60f, 0.80f, 0.18f), graphite, true);
            Primitive("WestWall", architecture, new Vector3(-6f, 2.05f, 0f), new Vector3(0.18f, 4.10f, 9f), warmWhite, true);
            Primitive("EastWall", architecture, new Vector3(6f, 2.05f, 0f), new Vector3(0.18f, 4.10f, 9f), warmWhite, true);

            Primitive("NorthWallBase", architecture, new Vector3(0f, 0.48f, 4.38f), new Vector3(11.7f, 0.96f, 0.10f), navy, false);
            Primitive("WestWallBase", architecture, new Vector3(-5.88f, 0.48f, 0f), new Vector3(0.10f, 0.96f, 8.7f), navy, false);
            Primitive("EastWallBase", architecture, new Vector3(5.88f, 0.48f, 0f), new Vector3(0.10f, 0.96f, 8.7f), navy, false);
            BuildCeilingRaft(architecture, "WestLightRaft", -3.15f);
            BuildCeilingRaft(architecture, "CenterLightRaft", 0f);
            BuildCeilingRaft(architecture, "EastLightRaft", 3.15f);

            var accents = Child(architecture, "ArchitecturalAccents").transform;
            Primitive("NorthCyanLine", accents, new Vector3(0f, 1.03f, 4.31f), new Vector3(10.7f, 0.025f, 0.025f), cyan, false);
            Primitive("CentralWorkZone", accents, new Vector3(0f, 0.012f, 0.48f), new Vector3(5.5f, 0.018f, 4.5f), graphite, false);
            Primitive("WorkZoneEntry", accents, new Vector3(0f, 0.027f, -1.78f), new Vector3(2.0f, 0.012f, 0.045f), green, false);
            Primitive("WorkZoneEdgeLeft", accents, new Vector3(-2.75f, 0.027f, 0.48f), new Vector3(0.045f, 0.012f, 4.5f), cyan, false);
            Primitive("WorkZoneEdgeRight", accents, new Vector3(2.75f, 0.027f, 0.48f), new Vector3(0.045f, 0.012f, 4.5f), cyan, false);

            var windows = Child(shared, "Windows").transform;
            BuildArchitecturalWindows(windows);

            var furniture = Child(shared, "Furniture").transform;
            BuildCounter(furniture, "NorthCounter", new Vector3(0f, 0f, 3.82f), new Vector3(8.6f, 0.84f, 0.70f), 6);
            BuildCounter(furniture, "WestStorage", new Vector3(-5.25f, 0f, 1.55f), new Vector3(0.72f, 0.84f, 2.55f), 3, true);
            BuildCounter(furniture, "EastStorage", new Vector3(5.25f, 0f, 1.55f), new Vector3(0.72f, 0.84f, 2.55f), 3, true);

            var cabinets = Child(shared, "Cabinets").transform;
            BuildShowcaseCabinet(cabinets, "WestPhysicsCabinet", new Vector3(-5.66f, 1.05f, 0.95f), Quaternion.Euler(0f, 90f, 0f), cyan);
            BuildShowcaseCabinet(cabinets, "EastPhysicsCabinet", new Vector3(5.66f, 1.05f, 0.95f), Quaternion.Euler(0f, -90f, 0f), green);

            var branding = Child(shared, "Branding").transform;
            Primitive("BrandBand", branding, new Vector3(0f, 3.69f, 4.31f), new Vector3(7.8f, 0.42f, 0.08f), graphite, false);
            WorldText("VLAB  /  PHÒNG THÍ NGHIỆM VẬT LÝ", branding, new Vector3(0f, 3.69f, 4.25f), Quaternion.Euler(0f, 180f, 0f), 0.24f, Color.white, TextAlignmentOptions.Center, new Vector2(7.4f, 0.38f));
            WorldText("CƠ HỌC  •  ĐO LƯỜNG  •  DAO ĐỘNG", branding, new Vector3(0f, 3.34f, 4.25f), Quaternion.Euler(0f, 180f, 0f), 0.12f, Hex("00B4D8"), TextAlignmentOptions.Center, new Vector2(6.5f, 0.24f));

            var navigation = Child(architecture, "NavigationSafety").transform;
            Primitive("DoorFrameLeft", navigation, new Vector3(-1.35f, 1.55f, -4.37f), new Vector3(0.16f, 3.1f, 0.26f), graphite, true);
            Primitive("DoorFrameRight", navigation, new Vector3(1.35f, 1.55f, -4.37f), new Vector3(0.16f, 3.1f, 0.26f), graphite, true);
            Primitive("DoorCanopy", navigation, new Vector3(0f, 3.18f, -4.25f), new Vector3(2.85f, 0.12f, 0.55f), navy, false);
            Primitive("ExitAccent", navigation, new Vector3(0f, 3.13f, -3.96f), new Vector3(1.05f, 0.025f, 0.025f), green, false);
            WorldText("LỐI VÀO  /  EXIT", navigation, new Vector3(0f, 3.42f, -4.35f), Quaternion.identity, 0.18f, Hex("06D6A0"), TextAlignmentOptions.Center, new Vector2(2.5f, 0.35f));

            Child(shared, "Lighting");
            Child(shared, "ReflectionProbes");
            Child(shared, "LightProbes");
            Child(shared, "Exterior");
            return shared;
        }

        private static void ConfigureNativeXriRig(GameObject xrOrigin)
        {
            var interactableLayer = LayerMask.NameToLayer("Interactable");
            var uiLayer = LayerMask.NameToLayer("UI");
            var worldAndUiMask = 1;
            if (interactableLayer >= 0) worldAndUiMask |= 1 << interactableLayer;
            if (uiLayer >= 0) worldAndUiMask |= 1 << uiLayer;
            foreach (var interactor in xrOrigin.GetComponentsInChildren<NearFarInteractor>(true))
            {
                interactor.enableUIInteraction = true;
                interactor.enableFarCasting = true;
                if (interactor.farInteractionCaster is CurveInteractionCaster caster)
                {
                    caster.raycastMask = worldAndUiMask;
                }
            }

            foreach (var controller in xrOrigin.GetComponentsInChildren<InteractionAttachController>(true))
            {
                controller.useManipulationInput = true;
                controller.manipulationXAxisMode = InteractionAttachController.ManipulationXAxisMode.HorizontalRotation;
                controller.manipulationYAxisMode = InteractionAttachController.ManipulationYAxisMode.Translate;
                controller.combineManipulationAxes = false;
                controller.smoothOffset = true;
                controller.smoothingSpeed = 14f;
                controller.useMomentum = false;
                controller.manipulationTranslateSpeed = 1.25f;
                controller.manipulationRotateSpeed = 90f;
            }
        }

        private static void BuildCeilingRaft(Transform parent, string name, float x)
        {
            var raft = Child(parent, name).transform;
            Primitive("Panel", raft, new Vector3(x, 4.05f, 0.15f), new Vector3(2.45f, 0.10f, 7.65f), warmWhite, false);
            Primitive("RailLeft", raft, new Vector3(x - 1.15f, 3.97f, 0.15f), new Vector3(0.055f, 0.08f, 7.25f), navy, false);
            Primitive("RailRight", raft, new Vector3(x + 1.15f, 3.97f, 0.15f), new Vector3(0.055f, 0.08f, 7.25f), navy, false);
            Primitive("LinearLight", raft, new Vector3(x, 3.965f, 0.15f), new Vector3(0.16f, 0.025f, 6.45f), warmWhite, false);
        }

        private static void BuildArchitecturalWindows(Transform parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(WindowModelPath);
            if (model == null)
            {
                throw new FileNotFoundException($"Clean architectural window model was not found: {WindowModelPath}");
            }

            var positions = new[] { -3.75f, 0f, 3.75f };
            for (var index = 0; index < positions.Length; index++)
            {
                var root = Child(parent, $"ArchitecturalWindow_{index + 1:00}").transform;
                root.position = new Vector3(positions[index], 1.02f, 4.40f);
                root.rotation = Quaternion.Euler(0f, 180f, 0f);
                var frame = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
                frame.name = "ProvidedWindowFrame";
                frame.transform.localPosition = Vector3.zero;
                frame.transform.localRotation = Quaternion.identity;
                frame.transform.localScale = Vector3.one;
                foreach (var renderer in frame.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        materials[materialIndex] = metal;
                    }
                    renderer.sharedMaterials = materials;
                }
                Primitive("GlassPane", root, new Vector3(0f, 0.89f, -0.018f), new Vector3(3.05f, 1.50f, 0.012f), glass, false);
            }
        }

        private static void BuildShowcaseCabinet(Transform parent, string name, Vector3 position, Quaternion rotation, Material accent)
        {
            var root = Child(parent, name).transform;
            root.position = position;
            root.rotation = rotation;
            Primitive("Back", root, new Vector3(0f, 1.18f, 0.15f), new Vector3(3.55f, 2.42f, 0.10f), navy, false);
            Primitive("Top", root, new Vector3(0f, 2.40f, -0.02f), new Vector3(3.65f, 0.10f, 0.48f), metal, false);
            Primitive("LeftFrame", root, new Vector3(-1.78f, 1.18f, -0.02f), new Vector3(0.10f, 2.42f, 0.48f), metal, false);
            Primitive("RightFrame", root, new Vector3(1.78f, 1.18f, -0.02f), new Vector3(0.10f, 2.42f, 0.48f), metal, false);
            for (var shelf = 0; shelf < 3; shelf++)
            {
                var shelfHeight = 0.56f + shelf * 0.64f;
                Primitive($"Shelf_{shelf + 1:00}", root, new Vector3(0f, shelfHeight, -0.04f), new Vector3(3.45f, 0.055f, 0.42f), metal, false);
                Primitive($"ShelfBacking_{shelf + 1:00}", root, new Vector3(0f, shelfHeight + 0.29f, 0.085f), new Vector3(3.30f, 0.50f, 0.018f), graphite, false);
            }
            Primitive("CabinetAccent", root, new Vector3(0f, 2.31f, -0.28f), new Vector3(3.32f, 0.035f, 0.025f), accent, false);
        }

        private static void BuildLighting(Transform parent)
        {
            ConfigureSkyEnvironment();

            var lighting = parent.Find("Lighting");
            var keyObject = Child(lighting, "ExteriorKey_Directional");
            keyObject.transform.rotation = Quaternion.Euler(46f, -28f, 0f);
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.94f, 0.84f);
            key.intensity = 0.72f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.68f;
            key.lightmapBakeType = LightmapBakeType.Mixed;
            RenderSettings.sun = key;

            CreateTaskLight(lighting, "TaskLight_West", new Vector3(-2.25f, 3.70f, 0.65f), new Color(1f, 0.89f, 0.76f));
            CreateTaskLight(lighting, "TaskLight_East", new Vector3(2.25f, 3.70f, 0.65f), new Color(1f, 0.92f, 0.82f));

            var accentObject = Child(lighting, "FeatureWall_CyanFill");
            accentObject.transform.position = new Vector3(0f, 2.65f, 3.65f);
            var accent = accentObject.AddComponent<Light>();
            accent.type = LightType.Point;
            accent.color = new Color(0.25f, 0.78f, 0.92f);
            accent.intensity = 0.24f;
            accent.range = 4.2f;
            accent.shadows = LightShadows.None;

            BuildReflectionProbes(parent.Find("ReflectionProbes"));
            BuildLightProbeGroup(parent.Find("LightProbes"));
        }

        private static void CreateTaskLight(Transform parent, string name, Vector3 position, Color color)
        {
            var lightObject = Child(parent, name);
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = 1.10f;
            light.range = 6.5f;
            light.spotAngle = 74f;
            light.innerSpotAngle = 54f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Mixed;
        }

        private static void BuildReflectionProbes(Transform parent)
        {
            var positions = new[] { new Vector3(-2.75f, 1.85f, 0.35f), new Vector3(2.75f, 1.85f, 0.35f) };
            for (var index = 0; index < positions.Length; index++)
            {
                var probeObject = Child(parent, $"LabReflectionProbe_{index + 1:00}");
                probeObject.transform.position = positions[index];
                var probe = probeObject.AddComponent<ReflectionProbe>();
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
                probe.resolution = 128;
                probe.boxProjection = true;
                probe.size = new Vector3(5.7f, 3.55f, 7.7f);
                probe.importance = 1;
                probe.intensity = 0.88f;
            }
        }

        private static void BuildLightProbeGroup(Transform parent)
        {
            var probeObject = Child(parent, "DynamicApparatusLightProbes");
            var group = probeObject.AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            var xPositions = new[] { -4f, 0f, 4f };
            var yPositions = new[] { 0.55f, 1.65f, 2.85f };
            var zPositions = new[] { -2.6f, 0.45f, 3.2f };
            foreach (var x in xPositions)
            {
                foreach (var y in yPositions)
                {
                    foreach (var z in zPositions)
                    {
                        positions.Add(new Vector3(x, y, z));
                    }
                }
            }
            group.probePositions = positions.ToArray();
        }

        private static void BuildCounter(Transform parent, string name, Vector3 position, Vector3 size, int doorCount, bool rotateDoors = false)
        {
            var root = Child(parent, name).transform;
            root.localPosition = position;
            Primitive("CabinetBody", root, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x, size.y, size.z), graphite, true);
            Primitive("Worktop", root, new Vector3(0f, size.y + 0.055f, 0f), new Vector3(size.x + 0.10f, 0.11f, size.z + 0.10f), warmWhite, true);
            Primitive("ToeKick", root, new Vector3(0f, 0.09f, -size.z * 0.51f), new Vector3(size.x * 0.94f, 0.18f, 0.04f), dark, false);
            for (var i = 0; i < doorCount; i++)
            {
                var t = (i + 0.5f) / doorCount - 0.5f;
                var doorPosition = rotateDoors
                    ? new Vector3(-size.x * 0.51f, size.y * 0.51f, t * size.z * 0.92f)
                    : new Vector3(t * size.x * 0.96f, size.y * 0.51f, -size.z * 0.51f);
                var doorScale = rotateDoors
                    ? new Vector3(0.035f, size.y * 0.78f, size.z * 0.82f / doorCount)
                    : new Vector3(size.x * 0.90f / doorCount, size.y * 0.78f, 0.035f);
                Primitive($"Door_{i + 1:00}", root, doorPosition, doorScale, navy, false);
                var handlePosition = doorPosition + (rotateDoors ? Vector3.left * 0.025f : Vector3.back * 0.025f) + Vector3.up * 0.12f;
                Primitive($"Handle_{i + 1:00}", root, handlePosition, new Vector3(0.025f, 0.16f, 0.025f), metal, false);
            }
        }

        private static void BuildDisplayNiche(Transform parent, string name, Vector3 position, Quaternion rotation, Material accent, string label)
        {
            var root = Child(parent, name).transform;
            root.localPosition = position;
            root.localRotation = rotation;
            Primitive("Frame", root, Vector3.zero, new Vector3(2.1f, 1.18f, 0.12f), graphite, false);
            Primitive("Inset", root, new Vector3(0f, 0f, -0.075f), new Vector3(1.84f, 0.92f, 0.035f), softBlue, false);
            Primitive("Shelf", root, new Vector3(0f, -0.32f, -0.15f), new Vector3(1.75f, 0.07f, 0.34f), wood, false);
            Primitive("Accent", root, new Vector3(0f, -0.55f, -0.08f), new Vector3(1.72f, 0.035f, 0.025f), accent, false);
            WorldText(label, root, new Vector3(0f, 0.28f, -0.105f), Quaternion.Euler(0f, 180f, 0f), 0.16f, Color.white, TextAlignmentOptions.Center, new Vector2(1.65f, 0.35f));
        }

        private static void BuildWallPanel(Transform parent, string name, Vector3 position, string label, Material accent)
        {
            var root = Child(parent, name).transform;
            root.position = position;
            Primitive("Back", root, Vector3.zero, new Vector3(2.8f, 0.9f, 0.08f), dark, false);
            Primitive("Accent", root, new Vector3(0f, -0.42f, -0.05f), new Vector3(2.8f, 0.045f, 0.025f), accent, false);
            WorldText(label, root, new Vector3(0f, 0f, -0.055f), Quaternion.Euler(0f, 180f, 0f), 0.20f, Color.white, TextAlignmentOptions.Center, new Vector2(2.5f, 0.5f));
        }

        private static void BuildHubScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureSkyEnvironment();
            var hub = Root("_HUB");
            var controller = hub.AddComponent<PhysicsLabHubController>();
            BuildSelectorConsole(hub.transform);
            BuildHubCanvas(hub.transform, controller);
            SaveScene(scene, ScenePath(PhysicsLabSceneNames.Hub));
        }

        private static void BuildSelectorConsole(Transform parent)
        {
            var console = Child(parent, "VLAB_ExperimentConsole").transform;
            console.position = new Vector3(0f, 0f, 1.6f);
            Primitive("Base", console, new Vector3(0f, 0.48f, 0f), new Vector3(2.7f, 0.96f, 0.85f), navy, true);
            var screen = Primitive("Screen", console, new Vector3(0f, 1.18f, -0.16f), new Vector3(2.45f, 1.05f, 0.08f), dark, true);
            screen.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Primitive("ScreenAccent", screen.transform, new Vector3(0f, -0.47f, -0.06f), new Vector3(2.25f, 0.035f, 0.02f), cyan, false);
        }

        private static void BuildHubCanvas(Transform parent, PhysicsLabHubController controller)
        {
            var canvasObject = UiObject(parent, "ExperimentSelectorCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 680f);
            canvasRect.localScale = Vector3.one * 0.0027f;
            canvasRect.position = new Vector3(0f, 1.32f, 1.10f);
            canvasRect.rotation = Quaternion.identity;
            var panel = UiPanel(canvas.transform, "SelectorPanel", new Vector2(860f, 650f), new Color(0.05f, 0.07f, 0.11f, 0.95f));
            SetAnchor(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 650f));
            UiText(panel.transform, "Title", "PHÒNG THÍ NGHIỆM VẬT LÝ", 38f, Color.white, TextAlignmentOptions.Center, new Vector2(760f, 60f), new Vector2(0f, 265f));
            UiText(panel.transform, "Subtitle", "Chọn một trạm để bắt đầu khám phá", 20f, Hex("88DFF2"), TextAlignmentOptions.Center, new Vector2(720f, 38f), new Vector2(0f, 220f));

            var buttons = new[]
            {
                ("01", "Con lắc đơn", new UnityEngine.Events.UnityAction(controller.OpenPendulum)),
                ("02", "Ném xiên", new UnityEngine.Events.UnityAction(controller.OpenProjectile)),
                ("03", "Ma sát", new UnityEngine.Events.UnityAction(controller.OpenFriction)),
                ("04", "Cổng quang", new UnityEngine.Events.UnityAction(controller.OpenPhotogateMotion)),
                ("05", "Dao động lò xo", new UnityEngine.Events.UnityAction(controller.OpenSpring)),
                ("06", "Bảo toàn động lượng", new UnityEngine.Events.UnityAction(controller.OpenAirTrackMomentum)),
            };

            for (var i = 0; i < buttons.Length; i++)
            {
                var column = i % 2;
                var row = i / 2;
                var position = new Vector2(column == 0 ? -205f : 205f, 120f - row * 125f);
                var button = UiButton(panel.transform, $"Experiment_{buttons[i].Item1}", $"{buttons[i].Item1}   {buttons[i].Item2}", new Vector2(370f, 92f), position, Hex("0A2463"));
                UnityEventTools.AddPersistentListener(button.onClick, buttons[i].Item3);
            }

            var back = UiButton(panel.transform, "BackToMainMenu", "←  TRỞ VỀ SẢNH CHÍNH", new Vector2(360f, 54f), new Vector2(0f, -268f), new Color(0.15f, 0.18f, 0.23f));
            UnityEventTools.AddPersistentListener(back.onClick, controller.BackToMainMenu);
            ConfigureXrCanvas(canvasObject);
        }

        private static void BuildStationScenes()
        {
            BuildStation(new StationDefinition("PHY_01", "CON LẮC ĐƠN", PhysicsLabSceneNames.Pendulum,
                new Apparatus("Experiment_Table", Vector3.zero, 0f, 0f),
                new Apparatus("Laboratory_Retort_Stand", new Vector3(-0.48f, 0f, 0.05f), 0f),
                new Apparatus("Adjustable_Clamp", new Vector3(-0.42f, 0f, 0.05f), 0.70f),
                new Apparatus("Pendulum_String_Anchor", new Vector3(-0.10f, 0f, 0.05f), 1.42f),
                new Apparatus("Pendulum_Bob", new Vector3(-0.10f, 0f, 0.05f), 0.92f),
                new Apparatus("Laboratory_Meter_Ruler", new Vector3(0.54f, 0f, 0.26f), 0f, 90f)));

            BuildStation(new StationDefinition("PHY_02", "NÉM XIÊN", PhysicsLabSceneNames.Projectile,
                new Apparatus("Experiment_Table", Vector3.zero, 0f, 0f),
                new Apparatus("Physics_Projectile_Launcher", new Vector3(-0.72f, 0f, 0.05f), 0f, 90f),
                new Apparatus("Projectile_Steel_Ball", new Vector3(-0.36f, 0f, 0.05f), 0.18f),
                new Apparatus("Laboratory_Meter_Ruler", new Vector3(0.62f, 0f, 0.28f), 0f, 0f)));

            BuildStation(new StationDefinition("PHY_03", "MA SÁT", PhysicsLabSceneNames.Friction,
                new Apparatus("Experiment_Table", Vector3.zero, 0f, 0f),
                new Apparatus("Physics_Friction_Block", new Vector3(0f, 0f, 0.02f), 0f),
                new Apparatus("Spring_Force_Meter", new Vector3(-0.72f, 0f, 0.02f), 0f, 90f),
                new Apparatus("Mass_Set", new Vector3(0.68f, 0f, 0.10f), 0f),
                new Apparatus("Laboratory_Meter_Ruler", new Vector3(0f, 0f, 0.34f), 0f, 0f)));

            BuildStation(new StationDefinition("PHY_04", "ĐO CHUYỂN ĐỘNG BẰNG CỔNG QUANG", PhysicsLabSceneNames.PhotogateMotion,
                new Apparatus("Experiment_Table", Vector3.zero, 0f, 0f),
                new Apparatus("Physics_Air_Track", new Vector3(0f, 0f, 0.05f), 0f),
                new Apparatus("Digital_Timer_MC964", new Vector3(0f, 0f, 0.36f), 0f),
                new Apparatus("Physics_Photogate", new Vector3(-0.52f, 0f, 0.05f), 0.23f, 90f, "Physics_Photogate_A"),
                new Apparatus("Physics_Photogate", new Vector3(0.52f, 0f, 0.05f), 0.23f, 90f, "Physics_Photogate_B"),
                new Apparatus("Air_Track_Glider_A", new Vector3(0f, 0f, 0.05f), 0.25f),
                new Apparatus("Laboratory_Meter_Ruler", new Vector3(0f, 0f, -0.34f), 0f)));

            BuildStation(new StationDefinition("PHY_05", "DAO ĐỘNG LÒ XO", PhysicsLabSceneNames.Spring,
                new Apparatus("Experiment_Table", Vector3.zero, 0f, 0f),
                new Apparatus("Laboratory_Retort_Stand", new Vector3(-0.46f, 0f, 0.05f), 0f),
                new Apparatus("Adjustable_Clamp", new Vector3(-0.40f, 0f, 0.05f), 0.72f),
                new Apparatus("Physics_Coil_Spring", new Vector3(-0.08f, 0f, 0.05f), 0.92f),
                new Apparatus("Mass_Set", new Vector3(0.60f, 0f, 0.10f), 0f),
                new Apparatus("Laboratory_Meter_Ruler", new Vector3(0.58f, 0f, -0.18f), 0f, 90f)));

            BuildStation(new StationDefinition("PHY_06", "BẢO TOÀN ĐỘNG LƯỢNG TRÊN ĐỆM KHÍ", PhysicsLabSceneNames.AirTrackMomentum,
                new Apparatus("Experiment_Table", Vector3.zero, 0f, 0f),
                new Apparatus("Physics_Air_Track", new Vector3(0f, 0f, 0.02f), 0f),
                new Apparatus("Air_Track_Glider_A", new Vector3(-0.42f, 0f, 0.02f), 0.25f),
                new Apparatus("Air_Track_Glider_B", new Vector3(0.42f, 0f, 0.02f), 0.25f),
                new Apparatus("Physics_Photogate", new Vector3(-0.76f, 0f, 0.02f), 0.23f, 90f, "Physics_Photogate_A"),
                new Apparatus("Physics_Photogate", new Vector3(0.76f, 0f, 0.02f), 0.23f, 90f, "Physics_Photogate_B"),
                new Apparatus("Digital_Timer_MC964", new Vector3(0f, 0f, 0.38f), 0f),
                new Apparatus("Mass_Set", new Vector3(0f, 0f, -0.32f), 0f),
                new Apparatus("Collision_Bumper", new Vector3(-0.84f, 0f, -0.30f), 0f),
                new Apparatus("Inelastic_Collision_Attachment", new Vector3(0.84f, 0f, -0.30f), 0f)));
        }

        private static void BuildStation(StationDefinition definition)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureSkyEnvironment();
            var station = Root("_STATION");
            station.name = "_STATION";
            var apparatusRoot = Child(station.transform, "Apparatus").transform;
            var resetManager = station.AddComponent<ExperimentResetManager>();
            var controller = station.AddComponent<PhysicsLabStationController>();
            controller.Configure(definition.Id, definition.Title, resetManager);
            var learningController = station.AddComponent<ExperimentPhysicalController>();
            learningController.Configure(definition.Id, controller, resetManager);
            var safetyBoundary = station.AddComponent<ExperimentSafetyBoundary>();
            safetyBoundary.Configure(resetManager, new Vector3(0f, 1.2f, 0f), new Vector3(2.8f, 2.2f, 2.2f));
            var snapController = station.AddComponent<LabSnapController>();
            snapController.Configure(0.28f);
            var physicalWiring = station.AddComponent<PhysicalStationWiring>();
            physicalWiring.Configure(definition.Id, learningController, snapController);

            var tableTop = 0.92f;
            foreach (var apparatus in definition.Apparatus)
            {
                var instance = InstantiatePrefab(apparatus.AssetName, apparatusRoot);
                instance.name = string.IsNullOrWhiteSpace(apparatus.InstanceName) ? apparatus.AssetName : apparatus.InstanceName;
                instance.transform.localRotation = Quaternion.Euler(0f, apparatus.YawDegrees, 0f);
                if (apparatus.AssetName == "Experiment_Table")
                {
                    // The source table was only about 1.5 m wide. Enlarge its footprint
                    // without making the worktop implausibly tall for a school lab.
                    instance.transform.localScale = new Vector3(1.48f, 1.15f, 1.48f);
                    instance.transform.localPosition = Vector3.zero;
                    PlaceBottomAt(instance, 0f);
                    tableTop = BoundsOf(instance).max.y;
                }
                else
                {
                    // A restrained 28% increase keeps apparatus readable in VR without
                    // turning it into toy-like oversized geometry.
                    instance.transform.localScale *= 1.28f;
                    instance.transform.localPosition = new Vector3(apparatus.Offset.x * 1.16f, 0f, apparatus.Offset.z * 1.16f);
                    if (apparatus.ExplicitHeight > 0f)
                    {
                        if (apparatus.AssetName == "Pendulum_Bob" || apparatus.AssetName == "Physics_Coil_Spring")
                        {
                            PlaceCenterAt(instance, tableTop + apparatus.ExplicitHeight);
                        }
                        else
                        {
                            var position = instance.transform.position;
                            position.y = tableTop + apparatus.ExplicitHeight * 1.18f;
                            instance.transform.position = position;
                        }
                    }
                    else
                    {
                        PlaceBottomAt(instance, tableTop + 0.015f);
                    }

                }
            }

            if (definition.SceneName == PhysicsLabSceneNames.Pendulum)
            {
                Primitive("Pendulum_String", apparatusRoot, new Vector3(-0.116f, tableTop + 1.18f, 0.058f), new Vector3(0.008f, 0.50f, 0.008f), metal, false);
            }
            else if (definition.SceneName == PhysicsLabSceneNames.Friction)
            {
                BuildFrictionSurfaceSamples(apparatusRoot, tableTop, apparatusRoot.GetComponentInChildren<VLAB.PhysicsLab.Mechanics.FrictionBlock>(true));
            }

            ConfigureNativeXriInteractables(apparatusRoot);

            var learningPanel = BuildStationCanvas(station.transform, learningController, definition.Title, definition.Id, tableTop);
            learningController.SetPanel(learningPanel);
            WorldText(definition.Id, apparatusRoot, new Vector3(-1.35f, tableTop + 0.03f, -0.48f), Quaternion.Euler(90f, 0f, 0f), 0.16f, Hex("00B4D8"), TextAlignmentOptions.Left, new Vector2(1.2f, 0.3f));
            SaveScene(scene, ScenePath(definition.SceneName));
        }

        private static void ConfigureNativeXriInteractables(Transform apparatusRoot)
        {
            var interactionLayer = LayerMask.NameToLayer("Interactable");
            foreach (var interactable in apparatusRoot.GetComponentsInChildren<LabInteractable>(true))
            {
                if (interactionLayer >= 0) SetLayerRecursively(interactable.transform, interactionLayer);
                if (interactable.GetComponent<LabGrabbable>() != null)
                {
                    var xrGrab = GetOrAddComponent<XRGrabInteractable>(interactable.gameObject);
                    var bridge = GetOrAddComponent<XrGrabEventBridge>(interactable.gameObject);
                    var constrainedRotation = interactable.name == "Pendulum_Bob" || interactable.name.StartsWith("Air_Track_Glider", StringComparison.Ordinal);
                    bridge.ConfigureNativeRemoteGrab(xrGrab, !constrainedRotation);
                }
                else
                {
                    GetOrAddComponent<XRSimpleInteractable>(interactable.gameObject);
                    GetOrAddComponent<XrSimpleInteractableBridge>(interactable.gameObject);
                }
            }
        }

        private static void BuildFrictionSurfaceSamples(Transform parent, float tableTop, VLAB.PhysicsLab.Mechanics.FrictionBlock block)
        {
            var samples = Child(parent, "Physical_Surface_Samples").transform;
            var definitions = new[]
            {
                ("NHỰA", -0.52f, 0.32f, 0.22f, softBlue),
                ("GỖ", 0f, 0.45f, 0.30f, wood),
                ("CAO SU", 0.52f, 0.75f, 0.60f, graphite),
            };
            var interactionLayer = LayerMask.NameToLayer("Interactable");
            foreach (var definition in definitions)
            {
                var sample = Primitive($"Surface_{definition.Item1}", samples, new Vector3(definition.Item2, tableTop + 0.035f, -0.34f), new Vector3(0.38f, 0.035f, 0.24f), definition.Item5, true);
                if (interactionLayer >= 0) sample.layer = interactionLayer;
                sample.AddComponent<LabInteractable>();
                sample.AddComponent<VLAB.PhysicsLab.Mechanics.FrictionSurfaceSelector>().Configure(
                    definition.Item1,
                    definition.Item3,
                    definition.Item4,
                    block,
                    sample.GetComponent<Renderer>());
                WorldText(definition.Item1, samples, new Vector3(definition.Item2, tableTop + 0.06f, -0.34f), Quaternion.Euler(90f, 0f, 0f), 0.11f, Color.white, TextAlignmentOptions.Center, new Vector2(0.34f, 0.12f));
            }
        }

        private static ExperimentContextPanel BuildStationCanvas(Transform parent, ExperimentPhysicalController controller, string title, string id, float tableTop)
        {
            var canvasObject = UiObject(parent, "StationCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(520f, 250f);
            canvasRect.localScale = Vector3.one * 0.0018f;
            canvasRect.position = new Vector3(1.55f, tableTop + 0.72f, -0.28f);
            canvasRect.rotation = Quaternion.identity;

            var panelRoot = UiPanel(canvas.transform, "ContextualLabPanel", new Vector2(520f, 250f), new Color(0.035f, 0.055f, 0.085f, 0.94f));
            var titleLabel = UiText(panelRoot.transform, "ExperimentTitle", $"{id}  •  {title}", 24f, Color.white, TextAlignmentOptions.Left, new Vector2(470f, 40f), new Vector2(0f, 92f));
            var cueLabel = UiText(panelRoot.transform, "CurrentActionCue", "Tương tác trực tiếp với thiết bị để bắt đầu.", 21f, Hex("06D6A0"), TextAlignmentOptions.TopLeft, new Vector2(470f, 76f), new Vector2(0f, 32f));
            ConfigureBodyText(cueLabel);
            var hintLabel = UiText(panelRoot.transform, "AdaptiveHint", "", 16f, Hex("00B4D8"), TextAlignmentOptions.TopLeft, new Vector2(470f, 45f), new Vector2(0f, -28f));
            ConfigureBodyText(hintLabel);
            var measurementLabel = UiText(panelRoot.transform, "LiveMeasurement", "Chưa có số đo thực.", 17f, new Color(0.86f, 0.93f, 0.98f), TextAlignmentOptions.Left, new Vector2(260f, 55f), new Vector2(-105f, -82f));
            ConfigureBodyText(measurementLabel);

            var back = UiButton(panelRoot.transform, "BackToHub", "← LAB", new Vector2(68f, 36f), new Vector2(-220f, -90f), new Color(0.25f, 0.29f, 0.34f));
            var resetTrial = UiButton(panelRoot.transform, "ResetTrial", "RESET LƯỢT", new Vector2(92f, 36f), new Vector2(-135f, -90f), new Color(0.42f, 0.22f, 0.10f));
            var results = UiButton(panelRoot.transform, "OpenResults", "KẾT QUẢ", new Vector2(84f, 36f), new Vector2(-42f, -90f), Hex("06A77D"));
            var hint = UiButton(panelRoot.transform, "Hint", "GỢI Ý", new Vector2(68f, 36f), new Vector2(58f, -90f), Hex("0A2463"));
            var settings = UiButton(panelRoot.transform, "OpenSettings", "CÀI ĐẶT", new Vector2(76f, 36f), new Vector2(134f, -90f), Hex("0A2463"));
            var help = UiButton(panelRoot.transform, "OpenHelp", "TRỢ GIÚP", new Vector2(76f, 36f), new Vector2(214f, -90f), Hex("0A2463"));

            var settingsPanel = UiPanel(canvas.transform, "SettingsPanel", new Vector2(460f, 360f), new Color(0.035f, 0.055f, 0.085f, 0.985f));
            settingsPanel.rectTransform.anchoredPosition = new Vector2(500f, 0f);
            UiText(settingsPanel.transform, "SettingsTitle", "CÀI ĐẶT PHYSICS LAB", 20f, Color.white, TextAlignmentOptions.Center, new Vector2(420f, 38f), new Vector2(0f, 150f));
            var settingsSummary = UiText(settingsPanel.transform, "SettingsSummary", "", 16f, new Color(0.86f, 0.93f, 0.98f), TextAlignmentOptions.TopLeft, new Vector2(200f, 180f), new Vector2(-105f, 35f));
            ConfigureBodyText(settingsSummary);
            var guidanceLevel = UiButton(settingsPanel.transform, "GuidanceLevel", "MỨC GỢI Ý", new Vector2(170f, 34f), new Vector2(115f, 75f), Hex("0A2463"));
            var actionGuidance = UiButton(settingsPanel.transform, "ActionGuidance", "CUE THAO TÁC", new Vector2(170f, 34f), new Vector2(115f, 35f), Hex("0A2463"));
            var outlines = UiButton(settingsPanel.transform, "InteractionOutlines", "VIỀN TƯƠNG TÁC", new Vector2(170f, 34f), new Vector2(115f, -5f), Hex("0A2463"));
            var autoReturn = UiButton(settingsPanel.transform, "AutoReturn", "TỰ TRẢ DỤNG CỤ", new Vector2(170f, 34f), new Vector2(115f, -45f), Hex("0A2463"));
            var sensitivityDown = UiButton(settingsPanel.transform, "SensitivityDown", "NHẠY −", new Vector2(82f, 34f), new Vector2(70f, -85f), Hex("0A2463"));
            var sensitivityUp = UiButton(settingsPanel.transform, "SensitivityUp", "NHẠY +", new Vector2(82f, 34f), new Vector2(160f, -85f), Hex("0A2463"));
            var closeSettings = UiButton(settingsPanel.transform, "CloseSettings", "ĐÓNG", new Vector2(90f, 36f), new Vector2(0f, -145f), Hex("06A77D"));

            var helpPanel = UiPanel(canvas.transform, "HelpPanel", new Vector2(420f, 220f), new Color(0.035f, 0.055f, 0.085f, 0.985f));
            helpPanel.rectTransform.anchoredPosition = new Vector2(-470f, 0f);
            var helpCopy = UiText(helpPanel.transform, "HelpCopy", "WASD: di chuyển  •  Chuột: nhìn\nChuột trái/E: tương tác vật  •  Q: thả\nEsc: mở chuột  •  Chuột phải: quay lại điều khiển\n\nCác bước hoàn thành tự động từ thao tác thiết bị thật.", 17f, new Color(0.86f, 0.93f, 0.98f), TextAlignmentOptions.TopLeft, new Vector2(380f, 150f), new Vector2(0f, 18f));
            ConfigureBodyText(helpCopy);
            var closeHelp = UiButton(helpPanel.transform, "CloseHelp", "ĐÓNG", new Vector2(90f, 36f), new Vector2(0f, -82f), Hex("06A77D"));

            var resultsPanel = UiPanel(canvas.transform, "ResultsPanel", new Vector2(520f, 430f), new Color(0.035f, 0.055f, 0.085f, 0.99f));
            resultsPanel.rectTransform.anchoredPosition = new Vector2(520f, 0f);
            UiText(resultsPanel.transform, "ResultsTitle", "PHIẾU KẾT QUẢ TỪ SỐ ĐO THỰC", 20f, Color.white, TextAlignmentOptions.Center, new Vector2(470f, 36f), new Vector2(0f, 188f));
            var resultsSummary = BuildScrollableResults(resultsPanel.transform);
            var retryResults = UiButton(resultsPanel.transform, "RetryExperiment", "THỬ LẠI", new Vector2(120f, 36f), new Vector2(-75f, -188f), Hex("0A2463"));
            var closeResults = UiButton(resultsPanel.transform, "CloseResults", "ĐÓNG", new Vector2(120f, 36f), new Vector2(75f, -188f), Hex("06A77D"));

            var panel = canvasObject.AddComponent<ExperimentContextPanel>();
            panel.Configure(titleLabel, cueLabel, hintLabel, measurementLabel, settingsSummary, settingsPanel.gameObject, helpPanel.gameObject, results.gameObject, resultsPanel.gameObject, resultsSummary);
            UnityEventTools.AddPersistentListener(hint.onClick, controller.RequestHint);
            UnityEventTools.AddPersistentListener(back.onClick, controller.BackToHub);
            UnityEventTools.AddPersistentListener(resetTrial.onClick, controller.ResetCurrentTrial);
            UnityEventTools.AddPersistentListener(settings.onClick, panel.ToggleSettings);
            UnityEventTools.AddPersistentListener(help.onClick, panel.ToggleHelp);
            UnityEventTools.AddPersistentListener(results.onClick, panel.ToggleResults);
            UnityEventTools.AddPersistentListener(guidanceLevel.onClick, panel.CycleGuidanceLevel);
            UnityEventTools.AddPersistentListener(actionGuidance.onClick, panel.ToggleActionGuidance);
            UnityEventTools.AddPersistentListener(outlines.onClick, panel.ToggleInteractionOutlines);
            UnityEventTools.AddPersistentListener(autoReturn.onClick, panel.ToggleAutoReturn);
            UnityEventTools.AddPersistentListener(sensitivityDown.onClick, panel.DecreaseMouseSensitivity);
            UnityEventTools.AddPersistentListener(sensitivityUp.onClick, panel.IncreaseMouseSensitivity);
            UnityEventTools.AddPersistentListener(closeSettings.onClick, panel.ToggleSettings);
            UnityEventTools.AddPersistentListener(closeHelp.onClick, panel.ToggleHelp);
            UnityEventTools.AddPersistentListener(retryResults.onClick, controller.RestartExperiment);
            UnityEventTools.AddPersistentListener(closeResults.onClick, panel.ToggleResults);
            settingsPanel.gameObject.SetActive(false);
            helpPanel.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            resultsPanel.gameObject.SetActive(false);
            ConfigureXrCanvas(canvasObject);
            return panel;
        }

        private static void ConfigureXrCanvas(GameObject canvasObject)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                SetLayerRecursively(canvasObject.transform, uiLayer);
            }

            var raycaster = canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.ignoreReversedGraphics = true;
                raycaster.checkFor3DOcclusion = true;
                raycaster.blockingMask = ~0;
            }
        }

        private static void ConfigureSkyEnvironment()
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.64f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.82f;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.fog = false;
        }

        private static CanvasGroup BuildFadeCanvas(Transform parent)
        {
            var canvas = CreateOverlayCanvas(parent, "FadeCanvas", 1000);
            var image = UiPanel(canvas.transform, "Fade", Vector2.zero, Color.black);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        private static void BuildStudentHud(Transform parent)
        {
            var canvas = CreateOverlayCanvas(parent, "StudentHUD", 5);
            var hint = UiPanel(canvas.transform, "ControlHint", new Vector2(660f, 38f), new Color(0.04f, 0.06f, 0.09f, 0.70f));
            SetAnchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(660f, 38f));
            UiText(hint.transform, "Hint", "WASD di chuyển theo hướng nhìn  •  Dùng tia tay cầm XR để chọn UI và thiết bị  •  Mọi bước được ghi từ thao tác vật lý thật", 14f, new Color(0.82f, 0.89f, 0.94f), TextAlignmentOptions.Center, new Vector2(760f, 30f), Vector2.zero);
        }

        private static Canvas CreateOverlayCanvas(Transform parent, string name, int sortingOrder)
        {
            var canvasObject = UiObject(parent, name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Image UiPanel(Transform parent, string name, Vector2 size, Color color)
        {
            var panel = UiObject(parent, name);
            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            var image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI UiText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size, Vector2 position)
        {
            var textObject = UiObject(parent, name);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSharedMaterial = uiText;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(12f, fontSize * 0.65f);
            label.fontSizeMax = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static void ConfigureBodyText(TextMeshProUGUI label)
        {
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static TextMeshProUGUI BuildScrollableResults(Transform parent)
        {
            var scrollRoot = UiObject(parent, "ResultsScroll");
            var rootRect = scrollRoot.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(470f, 300f);
            rootRect.anchoredPosition = new Vector2(0f, 10f);
            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = UiObject(scrollRoot.transform, "Viewport");
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-18f, 0f);
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();

            var content = UiObject(viewport.transform, "Content");
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var summary = UiText(content.transform, "ResultsSummary", "", 15f, new Color(0.86f, 0.93f, 0.98f), TextAlignmentOptions.TopLeft, new Vector2(430f, 0f), Vector2.zero);
            ConfigureBodyText(summary);
            summary.overflowMode = TextOverflowModes.Overflow;
            summary.gameObject.AddComponent<LayoutElement>().minHeight = 24f;

            var scrollbarObject = UiObject(scrollRoot.transform, "VerticalScrollbar");
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = Vector2.one;
            scrollbarRect.sizeDelta = new Vector2(12f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            var scrollbarBackground = scrollbarObject.AddComponent<Image>();
            scrollbarBackground.color = new Color(1f, 1f, 1f, 0.10f);
            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slidingArea = UiObject(scrollbarObject.transform, "SlidingArea");
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2f, 2f);
            slidingRect.offsetMax = new Vector2(-2f, -2f);
            var handle = UiObject(slidingArea.transform, "Handle");
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Hex("00B4D8");
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 6f;
            return summary;
        }

        private static Button UiButton(Transform parent, string name, string label, Vector2 size, Vector2 position, Color color)
        {
            var image = UiPanel(parent, name, size, color);
            image.rectTransform.anchoredPosition = position;
            var button = image.gameObject.AddComponent<Button>();
            image.raycastTarget = true;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Hex("00B4D8"), 0.45f);
            colors.pressedColor = Hex("06D6A0");
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            UiText(image.transform, "Label", label, 19f, Color.white, TextAlignmentOptions.Center, size - new Vector2(20f, 12f), Vector2.zero);
            return button;
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static GameObject InstantiatePrefab(string assetName, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{assetName}.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException($"Physics Lab prefab not found: {assetName}");
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        private static Bounds BoundsOf(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.zero);
            }
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static void PlaceBottomAt(GameObject target, float worldY)
        {
            var bounds = BoundsOf(target);
            target.transform.position += Vector3.up * (worldY - bounds.min.y);
        }

        private static void PlaceCenterAt(GameObject target, float worldY)
        {
            var bounds = BoundsOf(target);
            target.transform.position += Vector3.up * (worldY - bounds.center.y);
        }

        private static GameObject Primitive(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.DestroyImmediate(instance.GetComponent<Collider>());
            }
            return instance;
        }

        private static TextMeshPro WorldText(string text, Transform parent, Vector3 localPosition, Quaternion localRotation, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
        {
            var textObject = Child(parent, $"Text_{text}");
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = localRotation;
            var label = textObject.AddComponent<TextMeshPro>();
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSharedMaterial = uiText;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.rectTransform.sizeDelta = size;
            return label;
        }

        private static GameObject Root(string name) => new GameObject(name);

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var index = 0; index < root.childCount; index++)
            {
                SetLayerRecursively(root.GetChild(index), layer);
            }
        }

        private static GameObject UiObject(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"Failed to save scene: {path}");
            }
        }

        private static void ConfigureBuildSettings()
        {
            var paths = new List<string>
            {
                "Assets/Home.unity",
                ScenePath(PhysicsLabSceneNames.Base),
                ScenePath(PhysicsLabSceneNames.Hub),
                ScenePath(PhysicsLabSceneNames.Pendulum),
                ScenePath(PhysicsLabSceneNames.Projectile),
                ScenePath(PhysicsLabSceneNames.Friction),
                ScenePath(PhysicsLabSceneNames.PhotogateMotion),
                ScenePath(PhysicsLabSceneNames.Spring),
                ScenePath(PhysicsLabSceneNames.AirTrackMomentum),
            };
            EditorBuildSettings.scenes = paths.ConvertAll(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        private static string ScenePath(string sceneName) => $"{SceneRoot}/{sceneName}.unity";

        private static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString($"#{hex}", out var color))
            {
                throw new ArgumentException($"Invalid color: {hex}");
            }
            return color;
        }

        private readonly struct Apparatus
        {
            public Apparatus(string assetName, Vector3 offset, float explicitHeight, float yawDegrees = 0f, string instanceName = null)
            {
                AssetName = assetName;
                Offset = offset;
                ExplicitHeight = explicitHeight;
                YawDegrees = yawDegrees;
                InstanceName = instanceName;
            }

            public string AssetName { get; }
            public Vector3 Offset { get; }
            public float ExplicitHeight { get; }
            public float YawDegrees { get; }
            public string InstanceName { get; }
        }

        private sealed class StationDefinition
        {
            public StationDefinition(string id, string title, string sceneName, params Apparatus[] apparatus)
            {
                Id = id;
                Title = title;
                SceneName = sceneName;
                Apparatus = apparatus;
            }

            public string Id { get; }
            public string Title { get; }
            public string SceneName { get; }
            public IReadOnlyList<Apparatus> Apparatus { get; }
        }
    }
}
