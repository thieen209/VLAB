using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VLAB.PhysicsLab.AirTrack;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Measurement;
using VLAB.PhysicsLab.Mechanics;
using VLAB.PhysicsLab.Oscillation;
using VLAB.PhysicsLab.Projectile;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.PhysicsLab.Editor
{
    public static class PhysicsLabAssetPipeline
    {
        private const string Root = "Assets/VLAB/PhysicsLab";
        private const string Models = Root + "/Models";
        private const string Prefabs = Root + "/Prefabs";
        private const string Generated = Root + "/Generated";
        private const string ReportPath = Generated + "/PhysicsLabAssetReport.json";

        private static readonly AssetDefinition[] Definitions =
        {
            new AssetDefinition("Laboratory_Retort_Stand", false),
            new AssetDefinition("Adjustable_Clamp", false),
            new AssetDefinition("Experiment_Table", false),
            new AssetDefinition("Laboratory_Meter_Ruler", false),
            new AssetDefinition("Digital_Timer_MC964", false),
            new AssetDefinition("Physics_Photogate", false),
            new AssetDefinition("Photogate_Flag", false),
            new AssetDefinition("Spring_Force_Meter", false),
            new AssetDefinition("Pendulum_Bob", true),
            new AssetDefinition("Pendulum_String_Anchor", false),
            new AssetDefinition("Physics_Coil_Spring", false),
            new AssetDefinition("Mass_Set", false),
            new AssetDefinition("Physics_Projectile_Launcher", false),
            new AssetDefinition("Projectile_Steel_Ball", true),
            new AssetDefinition("Physics_Friction_Block", true),
            new AssetDefinition("Physics_Air_Track", false),
            new AssetDefinition("Air_Track_Glider_A", true),
            new AssetDefinition("Air_Track_Glider_B", true),
            new AssetDefinition("Collision_Bumper", false),
            new AssetDefinition("Inelastic_Collision_Attachment", false),
        };

        [MenuItem("Tools/VLAB/Physics Lab/Scan Assets")]
        public static void ScanAssets()
        {
            EnsureFolders();
            var report = CreateReport();
            foreach (var definition in Definitions)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(definition.Name));
                report.entries.Add(new AssetReportEntry
                {
                    assetName = definition.Name,
                    modelFound = model != null,
                    prefabFound = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(definition.Name)) != null,
                    status = model != null ? "MODEL_READY" : "MISSING_MODEL",
                });
            }
            SaveReport(report);
            Debug.Log($"[VLAB Physics Lab] Scanned {Definitions.Length} required assets. Report: {ReportPath}");
        }

        [MenuItem("Tools/VLAB/Physics Lab/Generate Prefabs")]
        public static void GeneratePrefabs()
        {
            EnsureFolders();
            foreach (var definition in Definitions)
            {
                GeneratePrefab(definition);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VLAB Physics Lab] Generated {Definitions.Length} reusable prefabs in {Prefabs}.");
        }

        [MenuItem("Tools/VLAB/Physics Lab/Validate Assets")]
        public static void ValidateAssets()
        {
            EnsureFolders();
            var report = CreateReport();
            foreach (var definition in Definitions)
            {
                report.entries.Add(Validate(definition, report.errors));
            }
            report.success = report.errors.Count == 0;
            SaveReport(report);
            if (!report.success)
            {
                throw new InvalidOperationException($"Physics Lab validation failed with {report.errors.Count} error(s). See {ReportPath}.");
            }
            Debug.Log($"[VLAB Physics Lab] Validation passed for {Definitions.Length} assets. Report: {ReportPath}");
        }

        [MenuItem("Tools/VLAB/Physics Lab/Generate All")]
        public static void GenerateAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ScanAssets();
            GeneratePrefabs();
            ValidateAssets();
        }

        public static void GenerateAllFromCommandLine()
        {
            GenerateAll();
        }

        private static void GeneratePrefab(AssetDefinition definition)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(definition.Name));
            if (model == null)
            {
                throw new FileNotFoundException($"Required model not found: {ModelPath(definition.Name)}");
            }

            var root = new GameObject(definition.Name);
            var visual = CreateChild(root.transform, "Visual");
            var physics = CreateChild(root.transform, "Physics");
            var interaction = CreateChild(root.transform, "Interaction");
            var attachments = CreateChild(root.transform, "AttachmentPoints");
            var sensors = CreateChild(root.transform, "Sensors");
            CreateChild(root.transform, "RuntimeVisuals");

            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelInstance.name = definition.Name + "_Model";
            modelInstance.transform.SetParent(visual.transform, false);
            AddBehavior(root, modelInstance.transform, attachments.transform, sensors.transform, definition.Name);
            ConfigureColliderAndBody(root, modelInstance, physics.transform, definition);
            ConfigureInteraction(root, definition.Name);
            AddStandardAnchors(definition.Name, attachments.transform);
            ConfigureNativeXriInteraction(root);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(definition.Name));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void AddBehavior(GameObject root, Transform model, Transform attachments, Transform sensors, string assetName)
        {
            switch (assetName)
            {
                case "Digital_Timer_MC964":
                {
                    var timer = root.AddComponent<DigitalTimerMC964>();
                    CreatePhysicalButton(attachments, "RESET_Button", new Vector3(-0.10f, 0.08f, -0.08f), InstrumentButtonAction.TimerReset, null, timer);
                    CreatePhysicalButton(attachments, "MODE_Button", new Vector3(0f, 0.08f, -0.08f), InstrumentButtonAction.TimerMode, null, timer);
                    CreatePhysicalButton(attachments, "RANGE_Button", new Vector3(0.10f, 0.08f, -0.08f), InstrumentButtonAction.TimerResolution, null, timer);
                    break;
                }
                case "Physics_Photogate":
                {
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "instrument");
                    var beam = CreateChild(sensors, "BeamTrigger");
                    beam.transform.localPosition = new Vector3(0f, 0.14f, 0f);
                    var trigger = beam.AddComponent<BoxCollider>();
                    trigger.isTrigger = true;
                    trigger.size = new Vector3(0.12f, 0.08f, 0.025f);
                    var gate = beam.AddComponent<PhysicsPhotogate>();
                    root.AddComponent<PhotogateVelocityMeter>().Configure(gate, 0.05f);
                    break;
                }
                case "Spring_Force_Meter":
                {
                    var meter = root.AddComponent<SpringForceMeter>();
                    meter.ConfigureIndicator(FindDeep(model, "ForceIndicator"));
                    AddAttachment(root, "instrument");
                    var hook = CreateChild(attachments, "ForceHook");
                    hook.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                    hook.AddComponent<LabAttachmentPoint>().Configure("force", true, false);
                    break;
                }
                case "Laboratory_Meter_Ruler":
                {
                    var ruler = root.AddComponent<LaboratoryMeterRuler>();
                    ruler.Configure(root.transform, Vector3.right);
                    break;
                }
                case "Physics_Coil_Spring":
                {
                    var top = CreateChild(attachments, "TopAnchor").transform;
                    top.localPosition = new Vector3(0f, 0.40f, 0f);
                    var bottom = CreateChild(attachments, "BottomAnchor").transform;
                    bottom.localPosition = new Vector3(0f, 0.03f, 0f);
                    bottom.gameObject.AddComponent<LabAttachmentPoint>().Configure("mass", true, false);
                    var spring = root.AddComponent<PhysicsCoilSpring>();
                    spring.Configure(top, bottom, null, FindDeep(model, "SpringCoil"), 18f, 0.37f, 0.8f);
                    AddAttachment(root, "instrument");
                    break;
                }
                case "Pendulum_Bob":
                    root.AddComponent<PendulumBob>();
                    AddAttachment(root, "pendulum-bob");
                    break;
                case "Physics_Projectile_Launcher":
                {
                    var pivot = FindDeep(model, "LauncherAnglePivot") ?? CreateChild(attachments, "LauncherAnglePivot").transform;
                    var origin = CreateChild(attachments, "LaunchOrigin").transform;
                    origin.localPosition = new Vector3(0.20f, 0.22f, 0f);
                    var socket = CreateChild(attachments, "ProjectileSocket").transform;
                    socket.localPosition = new Vector3(-0.12f, 0.22f, 0f);
                    socket.gameObject.AddComponent<LabAttachmentPoint>().Configure("projectile", true, true);
                    var launcher = root.AddComponent<ProjectileLauncher>();
                    launcher.Configure(pivot, origin, socket);
                    var angleHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    angleHandle.name = "LauncherAngleHandle";
                    angleHandle.transform.SetParent(attachments, false);
                    angleHandle.transform.localPosition = new Vector3(0f, 0.23f, 0f);
                    angleHandle.transform.localScale = new Vector3(0.22f, 0.10f, 0.16f);
                    var interactionLayer = LayerMask.NameToLayer("Interactable");
                    if (interactionLayer >= 0) angleHandle.layer = interactionLayer;
                    angleHandle.AddComponent<LabInteractable>();
                    angleHandle.AddComponent<LauncherAngleManipulator>().Configure(launcher);
                    CreatePhysicalButton(attachments, "LauncherTrigger", new Vector3(-0.16f, 0.08f, -0.10f), InstrumentButtonAction.ProjectileTrigger, launcher, null);
                    break;
                }
                case "Projectile_Steel_Ball":
                    root.AddComponent<ProjectileBall>();
                    AddAttachment(root, "projectile");
                    break;
                case "Physics_Friction_Block":
                    root.AddComponent<FrictionBlock>().SetParameters(0.5f, 0.45f, 0.30f);
                    AddAttachment(root, "force");
                    var frictionMassMount = CreateChild(attachments, "MassMount");
                    frictionMassMount.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                    frictionMassMount.AddComponent<LabAttachmentPoint>().Configure("mass", true, true);
                    break;
                case "Physics_Air_Track":
                    root.AddComponent<PhysicsAirTrack>().Configure(root.transform, 0.002f);
                    break;
                case "Air_Track_Glider_A":
                case "Air_Track_Glider_B":
                    root.AddComponent<AirTrackGlider>().Configure(null, 0.25f);
                    root.AddComponent<GliderCollisionAttachment>();
                    var flagMount = CreateChild(attachments, "PhotogateFlagMount");
                    flagMount.transform.localPosition = new Vector3(0f, 0.09f, 0f);
                    flagMount.AddComponent<LabAttachmentPoint>().Configure("photogate-flag", true, true);
                    var frontMount = CreateChild(attachments, "FrontCollisionMount");
                    frontMount.transform.localPosition = new Vector3(0.12f, 0f, 0f);
                    frontMount.AddComponent<LabAttachmentPoint>().Configure("glider-collision", true, true);
                    var rearMount = CreateChild(attachments, "RearCollisionMount");
                    rearMount.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
                    rearMount.AddComponent<LabAttachmentPoint>().Configure("glider-collision", true, true);
                    var gliderMassMount = CreateChild(attachments, "MassMount");
                    gliderMassMount.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                    gliderMassMount.AddComponent<LabAttachmentPoint>().Configure("mass", true, true);
                    break;
                case "Mass_Set":
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "mass");
                    root.AddComponent<PhysicalMass>().Configure(0.10f);
                    break;
                case "Adjustable_Clamp":
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "stand-clamp");
                    root.AddComponent<LabAttachmentPoint>().Configure("instrument", true, true);
                    break;
                case "Pendulum_String_Anchor":
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "instrument");
                    root.AddComponent<LabAttachmentPoint>().Configure("pendulum-bob", false, false);
                    break;
                case "Photogate_Flag":
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "photogate-flag");
                    break;
                case "Collision_Bumper":
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "glider-collision");
                    break;
                case "Inelastic_Collision_Attachment":
                    root.AddComponent<ExperimentObject>();
                    AddAttachment(root, "glider-collision");
                    break;
                default:
                    root.AddComponent<ExperimentObject>();
                    break;
            }
        }

        private static void ConfigureColliderAndBody(GameObject root, GameObject modelInstance, Transform physicsRoot, AssetDefinition definition)
        {
            var bounds = CalculateBounds(modelInstance);
            if (definition.Name == "Projectile_Steel_Ball" || definition.Name == "Pendulum_Bob")
            {
                var sphere = GetOrAddComponent<SphereCollider>(root);
                sphere.center = bounds.center;
                sphere.radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            }
            else
            {
                var box = GetOrAddComponent<BoxCollider>(root);
                box.center = bounds.center;
                box.size = bounds.size;
            }

            if (!definition.Dynamic)
            {
                return;
            }

            var body = GetOrAddComponent<Rigidbody>(root);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = definition.Name == "Projectile_Steel_Ball" ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.ContinuousSpeculative;
            if (definition.Name == "Projectile_Steel_Ball") body.mass = 0.065f;
            if (definition.Name == "Pendulum_Bob") body.mass = 0.20f;
            if (definition.Name == "Physics_Friction_Block") body.mass = 0.50f;
            if (definition.Name.StartsWith("Air_Track_Glider", StringComparison.Ordinal)) body.mass = 0.25f;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void ConfigureInteraction(GameObject root, string assetName)
        {
            if (!IsGrabbable(assetName))
            {
                return;
            }

            var interactionLayer = LayerMask.NameToLayer("Interactable");
            if (interactionLayer >= 0)
            {
                SetLayerRecursively(root.transform, interactionLayer);
            }

            var body = GetOrAddComponent<Rigidbody>(root);
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var releaseMode = assetName == "Air_Track_Glider_A" || assetName == "Air_Track_Glider_B"
                ? LabReleaseMode.DynamicNoGravity
                : assetName == "Pendulum_Bob" || assetName == "Projectile_Steel_Ball" || assetName == "Physics_Friction_Block" || assetName == "Mass_Set"
                    ? LabReleaseMode.Dynamic
                    : LabReleaseMode.RestoreStagedState;
            root.AddComponent<LabGrabbable>().Configure(true, true, releaseMode);
        }

        private static void ConfigureNativeXriInteraction(GameObject root)
        {
            foreach (var interactable in root.GetComponentsInChildren<LabInteractable>(true))
            {
                if (interactable.GetComponent<LabGrabbable>() != null)
                {
                    GetOrAddComponent<XRGrabInteractable>(interactable.gameObject);
                    GetOrAddComponent<XrGrabEventBridge>(interactable.gameObject);
                }
                else
                {
                    GetOrAddComponent<XRSimpleInteractable>(interactable.gameObject);
                    GetOrAddComponent<XrSimpleInteractableBridge>(interactable.gameObject);
                }
            }
        }

        private static bool IsGrabbable(string assetName)
        {
            switch (assetName)
            {
                case "Adjustable_Clamp":
                case "Laboratory_Meter_Ruler":
                case "Physics_Photogate":
                case "Photogate_Flag":
                case "Spring_Force_Meter":
                case "Pendulum_Bob":
                case "Mass_Set":
                case "Projectile_Steel_Ball":
                case "Physics_Friction_Block":
                case "Air_Track_Glider_A":
                case "Air_Track_Glider_B":
                case "Collision_Bumper":
                case "Inelastic_Collision_Attachment":
                    return true;
                default:
                    return false;
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private static void AddStandardAnchors(string assetName, Transform parent)
        {
            if (assetName == "Laboratory_Retort_Stand")
            {
                var top = CreateChild(parent, "StandTopAnchor");
                top.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                top.AddComponent<LabAttachmentPoint>().Configure("instrument", true, true);
                var rail = CreateChild(parent, "ClampRailAnchor");
                rail.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                rail.AddComponent<LabAttachmentPoint>().Configure("stand-clamp", true, true);
            }
            else if (assetName == "Physics_Air_Track")
            {
                foreach (var x in new[] { -0.65f, -0.25f, 0.25f, 0.65f })
                {
                    var mount = CreateChild(parent, $"PhotogateMount_{x:+0.00;-0.00}");
                    mount.transform.localPosition = new Vector3(x, 0.20f, 0f);
                    mount.AddComponent<LabAttachmentPoint>().Configure("instrument", true, true);
                }
            }
        }

        private static AssetReportEntry Validate(AssetDefinition definition, List<string> globalErrors)
        {
            var entry = new AssetReportEntry
            {
                assetName = definition.Name,
                modelFound = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(definition.Name)) != null,
                prefabFound = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(definition.Name)) != null,
                status = "VALID",
            };
            if (!entry.modelFound) entry.errors.Add("Model missing");
            if (!entry.prefabFound) entry.errors.Add("Prefab missing");
            if (entry.prefabFound)
            {
                var contents = PrefabUtility.LoadPrefabContents(PrefabPath(definition.Name));
                try
                {
                    foreach (var childName in new[] { "Visual", "Physics", "Interaction", "AttachmentPoints", "Sensors", "RuntimeVisuals" })
                    {
                        if (contents.transform.Find(childName) == null) entry.errors.Add($"Hierarchy node missing: {childName}");
                    }
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(contents) > 0) entry.errors.Add("Missing script");
                    foreach (var meshCollider in contents.GetComponentsInChildren<MeshCollider>(true))
                    {
                        if (meshCollider.attachedRigidbody != null && !meshCollider.convex) entry.errors.Add("Dynamic non-convex MeshCollider");
                    }
                    if (definition.Dynamic && contents.GetComponent<Rigidbody>() == null) entry.errors.Add("Dynamic asset has no Rigidbody");
                    if (contents.GetComponentsInChildren<Collider>(true).Length == 0) entry.errors.Add("No collider");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            if (entry.errors.Count > 0)
            {
                entry.status = "INVALID";
                globalErrors.AddRange(entry.errors.Select(error => $"{definition.Name}: {error}"));
            }
            return entry;
        }

        private static Bounds CalculateBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one * 0.1f);
            }
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void AddAttachment(GameObject root, string type)
        {
            root.AddComponent<LabAttachment>().Configure(type);
        }

        private static GameObject CreatePhysicalButton(Transform parent, string name, Vector3 localPosition, InstrumentButtonAction action, ProjectileLauncher launcher, DigitalTimerMC964 timer)
        {
            var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(parent, false);
            button.transform.localPosition = localPosition;
            button.transform.localScale = new Vector3(0.055f, 0.022f, 0.055f);
            var interactionLayer = LayerMask.NameToLayer("Interactable");
            if (interactionLayer >= 0) button.layer = interactionLayer;
            button.AddComponent<LabInteractable>();
            button.AddComponent<InstrumentPushButton>().Configure(action, launcher, timer);
            return button;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
        }

        private static AssetValidationReport CreateReport()
        {
            return new AssetValidationReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                requiredAssetCount = Definitions.Length,
            };
        }

        private static void SaveReport(AssetValidationReport report)
        {
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
        }

        private static string ModelPath(string name) => $"{Models}/{name}.fbx";
        private static string PrefabPath(string name) => $"{Prefabs}/{name}.prefab";

        private readonly struct AssetDefinition
        {
            public AssetDefinition(string name, bool dynamic)
            {
                Name = name;
                Dynamic = dynamic;
            }
            public string Name { get; }
            public bool Dynamic { get; }
        }

        [Serializable]
        private sealed class AssetValidationReport
        {
            public string generatedAtUtc;
            public string unityVersion;
            public int requiredAssetCount;
            public bool success;
            public List<AssetReportEntry> entries = new List<AssetReportEntry>();
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private sealed class AssetReportEntry
        {
            public string assetName;
            public bool modelFound;
            public bool prefabFound;
            public string status;
            public List<string> errors = new List<string>();
        }
    }
}
