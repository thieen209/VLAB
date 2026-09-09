using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VLAB.Core.Input;
using Object = UnityEngine.Object;

namespace VLAB.DemoLabs.Editor
{
    public static partial class DemoLabSceneBuilder
    {
        public const string Root = "Assets/VLAB/DemoLabs";
        public const string EngineeringPath = Root + "/Scenes/EngineeringLab.unity";
        public const string BiologyPath = Root + "/Scenes/BiologyLab.unity";
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static TMP_FontAsset font;
        private static Sprite circle;
        private static Transform sceneRoot;

        [MenuItem("Tools/VLAB/Demo Labs/Play Engineering _F6")]
        public static void PlayEngineering() => PlayScene(EngineeringPath);
        [MenuItem("Tools/VLAB/Demo Labs/Play Biology _F7")]
        public static void PlayBiology() => PlayScene(BiologyPath);
        private static void PlayScene(string path)
        {
            if (EditorApplication.isPlaying) SceneManager.LoadScene(Path.GetFileNameWithoutExtension(path));
            else
            {
                EditorSceneManager.OpenScene(path);
                EditorApplication.isPlaying = true;
            }
            var gameType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var game = EditorWindow.GetWindow(gameType); game.Show(); game.Focus();
        }

        [MenuItem("Tools/VLAB/Demo Labs/Build Engineering and Biology")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Art/Materials");
            Directory.CreateDirectory(Root + "/Art/UI"); Directory.CreateDirectory(Root + "/Prefabs");
            PrepareAssets();
            BuildEngineering(); BuildBiology();
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var path in new[] { EngineeringPath, BiologyPath })
                if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("VLAB_DEMOS_BUILT: EngineeringLab and BiologyLab. Existing scenes preserved.");
        }
        private static T BeginScene<T>(string name, bool biology) where T : VLabExperimentController
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            sceneRoot = new GameObject(name).transform;
            var experiment = sceneRoot.gameObject.AddComponent<T>();
            experiment.FeedbackSounds = sceneRoot.gameObject.AddComponent<VLabFeedback>();
            var guidance = new GameObject("ContextualHintRing", typeof(LineRenderer)); guidance.transform.SetParent(sceneRoot);
            experiment.GuidanceRing = guidance.GetComponent<LineRenderer>();
            experiment.GuidanceRing.sharedMaterial = guide; experiment.GuidanceRing.positionCount = 32;
            experiment.GuidanceRing.loop = true; experiment.GuidanceRing.useWorldSpace = true;
            experiment.GuidanceRing.startWidth = experiment.GuidanceRing.endWidth = .007f;
            experiment.GuidanceRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            experiment.GuidanceRing.enabled = false;
            BuildRoom(biology);
            var cameraObject = new GameObject("PlayerView", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(sceneRoot);
            cameraObject.transform.position = new Vector3(0, 2.65f, -3.3f);
            cameraObject.transform.LookAt(new Vector3(0, 1.18f, .12f));
            var camera = cameraObject.GetComponent<Camera>(); camera.fieldOfView = 48; camera.nearClipPlane = .03f; camera.farClipPlane = 55;
            camera.backgroundColor = new Color(.66f, .76f, .8f); camera.cullingMask &= ~(1 << 31);
            camera.allowHDR = false; camera.allowMSAA = true;
            var inputObject = new GameObject("DemoInput", typeof(VLabDemoInputProvider), typeof(InputManager));
            inputObject.transform.SetParent(sceneRoot);
            var provider = inputObject.GetComponent<VLabDemoInputProvider>();
            var input = inputObject.GetComponent<InputManager>(); input.SetProvider(provider);
            var driver = cameraObject.AddComponent<VLabInteractionDriver>();
            driver.Input = input; driver.PointerProvider = provider; driver.ViewCamera = camera; driver.Experiment = experiment;
            var events = new GameObject("EventSystem"); events.SetActive(false);
            events.transform.SetParent(sceneRoot);
            events.AddComponent<EventSystem>();
            var uiInput = events.AddComponent<InputSystemUIInputModule>();
            uiInput.scrollDeltaPerTick = 1;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Art/UI/DemoUIActions.asset");
            if (actions == null)
            {
                actions = ScriptableObject.CreateInstance<InputActionAsset>();
                var map = actions.AddActionMap("UI");
                map.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position").expectedControlType = "Vector2";
                map.AddAction("Click", InputActionType.PassThrough, "<Pointer>/press").expectedControlType = "Button";
                map.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll").expectedControlType = "Vector2";
                map.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
                map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
                AssetDatabase.CreateAsset(actions, Root + "/Art/UI/DemoUIActions.asset");
            }
            uiInput.actionsAsset = actions;
            InputActionReference Reference(string actionName)
            {
                var path = Root + "/Art/UI/UI_" + actionName + ".asset";
                var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
                if (reference == null) { reference = InputActionReference.Create(actions.FindAction("UI/" + actionName)); AssetDatabase.CreateAsset(reference, path); }
                return reference;
            }
            uiInput.point = Reference("Point"); uiInput.leftClick = Reference("Click"); uiInput.scrollWheel = Reference("Scroll");
            uiInput.submit = Reference("Submit"); uiInput.cancel = Reference("Cancel");
            events.SetActive(true);
            experiment.Driver = driver; experiment.Hud = BuildHud(biology); driver.Hud = experiment.Hud;
            return experiment;
        }
        private static void Save(VLabExperimentController experiment, string path)
        {
            experiment.Driver.Items = sceneRoot.GetComponentsInChildren<VLabGrabInteractable>(true);
            experiment.Driver.Zones = sceneRoot.GetComponentsInChildren<VLabSnapZone>(true);
            foreach (var renderer in sceneRoot.GetComponentsInChildren<Renderer>())
            {
                renderer.receiveShadows = true;
                var movingStage = experiment is BiologyExperiment biology && renderer.transform.IsChildOf(biology.StageMovingPart);
                if (!movingStage && !(renderer is LineRenderer) && renderer.GetComponentInParent<VLabInteractable>() == null && !renderer.name.Contains("Specimen"))
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        }
        private static void BuildEngineering()
        {
            var e = BeginScene<EngineeringExperiment>("EngineeringLab", false);
            Model("ENG_Breadboard", sceneRoot, new Vector3(.10f, 1.105f, .18f), 1.40f);
            var psu = Model("ENG_PowerSupply", sceneRoot, new Vector3(-1.35f, 1.105f, .28f), 1.15f);
            e.SupplyDisplay = WorldText("SupplyReadout", psu.transform, "5.00 V\nNGUỒN TẮT", new Vector3(0, .236f, -.188f), .35f, .104f, 2.7f, pale);
            e.PowerButton = PartAction(psu, "PowerSwitch", "Nguồn 5 V — chọn để bật / tắt", .09f);
            e.ResistorZone = Zone("ResistorDock", "ĐIỆN TRỞ — đặt linh kiện", "resistor", new Vector3(-.18f, 1.215f, .18f), new Vector3(.42f, .10f, .17f));
            e.ResistorZone.AllowReplacement = true;
            e.LedZone = Zone("LedDock", "LED — A: anode, K: cathode", "led", new Vector3(.48f, 1.215f, .18f), new Vector3(.20f, .12f, .19f));
            FlatLabel("ResistanceLabel", "ĐIỆN TRỞ", new Vector3(-.18f, 1.202f, .41f), .45f, .08f);
            FlatLabel("LEDLabel", "LED  A (+) · K (−)", new Vector3(.49f, 1.202f, .41f), .49f, .08f);
            Model("Lab_Tray", sceneRoot, new Vector3(-.65f, 1.11f, -.63f), 1.9f);
            var values = new[] { 100, 220, 1000 };
            for (var i = 0; i < values.Length; i++)
            {
                var item = GrabModel("ENG_Resistor" + values[i], "Điện trở " + (values[i] == 1000 ? "1 kΩ" : values[i] + " Ω"), "resistor", new Vector3(-1.08f + i * .42f, 1.17f, -.61f), new Vector3(.34f, .10f, .13f));
                item.Value = values[i];
                FlatLabel("R" + values[i], values[i] == 1000 ? "1 kΩ" : values[i] + " Ω", new Vector3(-1.08f + i * .42f, 1.179f, -.78f), .36f, .085f, pale);
            }
            var led = GrabModel("ENG_LED", "LED — cuộn chuột khi cầm để đảo cực", "led", new Vector3(.30f, 1.13f, -.70f), new Vector3(.16f, .18f, .16f));
            led.CanFlip = true;
            e.LedLens = FindPart(led.gameObject, "Dome").GetComponent<Renderer>();
            var light = new GameObject("LEDLight", typeof(Light)); light.transform.SetParent(led.transform, false); light.transform.localPosition = new Vector3(0, .14f, 0);
            e.LedLight = light.GetComponent<Light>(); e.LedLight.color = new Color(.2f, 1, .35f); e.LedLight.range = .35f; e.LedLight.intensity = 0; e.LedLight.shadows = LightShadows.None;
            FlatLabel("LedTrayLabel", "LED", new Vector3(.3f, 1.14f, -.85f), .20f, .08f);
            var positions = new[]
            {
                new Vector3(-1.53f, 1.16f, -.07f), new Vector3(-1.15f, 1.16f, -.07f),
                new Vector3(-.44f, 1.215f, -.03f), new Vector3(.01f, 1.215f, -.03f),
                new Vector3(.37f, 1.215f, -.03f), new Vector3(.66f, 1.215f, -.03f)
            };
            var labels = new[] { "+ 5 V", "− GND", "R.A", "R.B", "A (+)", "K (−)" };
            for (var i = 0; i < 6; i++)
            {
                var socket = Zone("Socket_" + (Terminal)i, "Cọc " + labels[i] + " — gắn một đầu dây", "wire", positions[i], new Vector3(.125f, .08f, .12f));
                socket.gameObject.AddComponent<VLabConnectionSocket>().Terminal = (Terminal)i;
                var socketBody = Model("ENG_ConnectionSocket", socket.transform, Vector3.zero);
                FindPart(socketBody, "Insulator").GetComponent<Renderer>().sharedMaterial = i == 0 ? red : i == 1 ? graphite : teal;
                FlatLabel("SocketLabel" + i, labels[i], positions[i] + new Vector3(0, .001f, -.12f), .22f, .065f);
            }
            e.Wires = new VLabWire[3];
            var wireMats = new[] { red, gold, graphite };
            for (var i = 0; i < 3; i++)
            {
                var wireRoot = new GameObject("Wire_" + i); wireRoot.transform.SetParent(sceneRoot);
                var wire = wireRoot.AddComponent<VLabWire>(); e.Wires[i] = wire;
                var z = -.61f + i * .29f;
                wire.EndA = Connector("Đầu dây " + (i + 1) + " · 1", wireRoot.transform, new Vector3(1.12f, 1.18f, z), wireMats[i]);
                wire.EndB = Connector("Đầu dây " + (i + 1) + " · 2", wireRoot.transform, new Vector3(1.67f, 1.18f, z), wireMats[i]);
                wire.Line = wireRoot.AddComponent<LineRenderer>(); wire.Line.sharedMaterial = wireMats[i]; wire.Line.startWidth = wire.Line.endWidth = .014f;
                wire.Line.numCapVertices = 4; wire.Line.useWorldSpace = true; wire.Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            FlatLabel("WireTrayLabel", "DÂY NỐI · GẮN TỪNG ĐẦU", new Vector3(1.39f, 1.12f, -.88f), 1.0f, .09f);
            e.ValidateButton = DeskButton("ValidateCircuit", "KIỂM TRA MẠCH", new Vector3(.88f, 1.16f, .65f), teal);
            var result = DeskButton("ShowResult", "KẾT QUẢ", new Vector3(1.60f, 1.16f, .65f), navy);
            e.ResultButton = result;
            e.ReadingDisplay = WorldText("CircuitReading", sceneRoot, "CHƯA KIỂM TRA", new Vector3(.15f, 1.57f, .849f), 1.25f, .23f, 4.8f, pale);
            Box("ReadoutPanel", sceneRoot, new Vector3(.15f, 1.57f, .88f), new Vector3(1.45f, .29f, .04f), navy, false);
            Save(e, EngineeringPath);
        }
        private static void BuildBiology()
        {
            var e = BeginScene<BiologyExperiment>("BiologyLab", true);
            var microscope = Model("BIO_Microscope", sceneRoot, new Vector3(.65f, 1.105f, .24f), 1.25f);
            PrefabUtility.UnpackPrefabInstance(microscope.transform.GetChild(0).gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            e.StageMovingPart = FindPart(microscope, "Stage");
            e.LeftClipPart = FindPart(microscope, "StageClip_L"); e.RightClipPart = FindPart(microscope, "StageClip_R");
            e.LeftClipPart.SetParent(e.StageMovingPart, true); e.RightClipPart.SetParent(e.StageMovingPart, true);
            FindPart(microscope, "ClipPin_L").SetParent(e.StageMovingPart, true);
            FindPart(microscope, "ClipPin_R").SetParent(e.StageMovingPart, true);
            e.ClipLeft = PartAction(microscope, "StageClip_L", "Kẹp trái — đóng / mở", .10f);
            e.ClipRight = PartAction(microscope, "StageClip_R", "Kẹp phải — đóng / mở", .10f);
            e.Eyepiece = PartAction(microscope, "Eyepiece", "Thị kính — chọn để quan sát", .12f);
            e.Nosepiece = PartDial(microscope, "Nosepiece", "Mâm vật kính — chuyển 10× / 40×", Vector3.up, .1f, true, .16f);
            e.Nosepiece.InitialValue = 1;
            e.CoarseKnob = PartDial(microscope, "CoarseFocusKnob", "Ốc sơ cấp — cuộn chuột để lấy nét thô", Vector3.right, .04f, false, .10f);
            e.FineKnob = PartDial(microscope, "FineFocusKnob", "Ốc vi cấp — cuộn chuột để lấy nét tinh", Vector3.right, .10f, false, .06f);
            e.FineKnob.InitialValue = .5f;
            e.LightKnob = PartDial(microscope, "LightControl", "Đèn kính — cuộn để chỉnh độ sáng", Vector3.right, .1f, false, .065f);
            e.LightKnob.InitialValue = .7f;
            // Separated hit handles avoid nested coarse/fine collider overlap.
            e.FineKnob.transform.position += (e.FineKnob.transform.position - e.CoarseKnob.transform.position).normalized * .05f;
            Model("Lab_Tray", sceneRoot, new Vector3(-.91f, 1.11f, -.57f), 1.75f);
            e.Slide = GrabModel("BIO_Slide", "Lam kính — cầm và đặt", "slide", new Vector3(-1.25f, 1.16f, -.63f), new Vector3(.30f, .075f, .13f));
            GrabModel("BIO_Dropper", "Ống nhỏ giọt nước", "liquid", new Vector3(-.67f, 1.17f, -.60f), new Vector3(.30f, .09f, .13f));
            var sample = Box("OnionSample", sceneRoot, new Vector3(-.98f, 1.17f, -.30f), new Vector3(.15f, .012f, .10f), sampleMaterial, false);
            var sampleGrab = sample.AddComponent<VLabGrabInteractable>(); sampleGrab.Kind = "sample"; sampleGrab.ContextLabel = "Mẫu biểu bì hành"; Collider(sample, new Vector3(.20f, .08f, .13f));
            GrabModel("BIO_Coverslip", "Lamen — tấm kính phủ mẫu", "cover", new Vector3(-.66f, 1.17f, -.30f), new Vector3(.15f, .07f, .13f));
            FlatLabel("SlideLabel", "LAM KÍNH", new Vector3(-1.25f, 1.18f, -.76f), .38f, .07f, pale);
            FlatLabel("WaterLabel", "NƯỚC", new Vector3(-.67f, 1.18f, -.76f), .30f, .07f, pale);
            FlatLabel("SampleLabel", "BIỂU BÌ HÀNH", new Vector3(-1.09f, 1.17f, -.42f), .43f, .06f, pale);
            FlatLabel("CoverLabel", "LAMEN", new Vector3(-.66f, 1.17f, -.42f), .27f, .06f, pale);
            e.PreparationZone = Zone("PreparationDock", "Đệm CHUẨN BỊ — đặt lam kính", "slide", new Vector3(-.94f, 1.16f, .31f), new Vector3(.43f, .08f, .20f));
            Box("PrepMat", sceneRoot, new Vector3(-.94f, 1.125f, .31f), new Vector3(.96f, .018f, .65f), teal, false);
            FlatLabel("PrepTitle", "CHUẨN BỊ TIÊU BẢN", new Vector3(-.94f, 1.146f, .60f), .82f, .075f);
            e.WaterZone = Zone("WaterTarget", "NHỎ NƯỚC — đưa ống nhỏ giọt tới đây", "liquid", new Vector3(-1.27f, 1.17f, .10f), new Vector3(.19f, .08f, .13f));
            e.SampleZone = Zone("SampleTarget", "MẪU — thêm biểu bì hành", "sample", new Vector3(-.94f, 1.17f, .10f), new Vector3(.19f, .08f, .13f));
            e.CoverZone = Zone("CoverTarget", "LAMEN — phủ mẫu", "cover", new Vector3(-.61f, 1.17f, .10f), new Vector3(.19f, .08f, .13f));
            foreach (var zone in new[] { e.WaterZone, e.SampleZone, e.CoverZone }) zone.ConsumeAndReturn = true;
            foreach (var zone in new[] { e.WaterZone, e.SampleZone, e.CoverZone })
            {
                zone.transform.SetParent(e.Slide.transform, false); zone.transform.localPosition = new Vector3(.025f, .045f, 0);
                zone.GetComponent<BoxCollider>().size = new Vector3(.29f, .06f, .17f);
            }
            FlatLabel("WaterTargetLabel", "NHỎ NƯỚC", new Vector3(-1.27f, 1.16f, -.02f), .29f, .055f);
            FlatLabel("SampleTargetLabel", "MẪU", new Vector3(-.94f, 1.16f, -.02f), .22f, .055f);
            FlatLabel("CoverTargetLabel", "LAMEN", new Vector3(-.61f, 1.16f, -.02f), .25f, .055f);
            var importedAnchor = FindPart(microscope, "SlideAnchor");
            var anchor = new GameObject("SlideMountAnchor").transform;
            anchor.SetPositionAndRotation(importedAnchor.position, Quaternion.identity);
            anchor.SetParent(e.StageMovingPart, true);
            // A visible handle at the accessible edge maps to the actual mechanical slide anchor.
            e.StageZone = Zone("StageDock", "BÀN KÍNH — gắn tiêu bản hoàn chỉnh", "slide", new Vector3(.27f, 1.49f, .16f), new Vector3(.17f, .10f, .19f));
            e.StageZone.Anchor = anchor;
            FlatLabel("StageLabel", "BÀN KÍNH", new Vector3(.16f, 1.15f, -.04f), .40f, .07f);
            e.WaterDrop = Box("WaterOnSlide", e.Slide.transform, new Vector3(.025f, .015f, 0), new Vector3(.06f, .004f, .045f), glass, false);
            e.SampleMark = Box("EpidermisOnSlide", e.Slide.transform, new Vector3(.025f, .018f, 0), new Vector3(.075f, .003f, .05f), sampleMaterial, false);
            e.CoverMark = Box("CoverslipOnSlide", e.Slide.transform, new Vector3(.025f, .022f, 0), new Vector3(.098f, .003f, .085f), glass, false);
            Bottle("Nước cất", new Vector3(-1.73f, 1.11f, .32f), glass);
            Bottle("Mẫu hành", new Vector3(-1.73f, 1.11f, .72f), gold);
            e.View = BuildMicroscopeView(e, anchor);
            var aim = new GameObject("EyepieceView").transform; aim.SetParent(microscope.transform);
            aim.position = FindPart(microscope, "Eyepiece").position + new Vector3(0, .12f, -.18f);
            aim.LookAt(FindPart(microscope, "Eyepiece").position); e.View.EyepieceView = aim;
            Save(e, BiologyPath);
        }
        private static VLabGrabInteractable GrabModel(string model, string label, string kind, Vector3 position, Vector3 colliderSize)
        {
            var obj = Model(model, sceneRoot, position); Collider(obj, colliderSize);
            var grab = obj.AddComponent<VLabGrabInteractable>(); grab.Kind = kind; grab.ContextLabel = label; return grab;
        }
        private static VLabGrabInteractable Connector(string label, Transform parent, Vector3 position, Material material)
        {
            var obj = Model("ENG_WireConnector", parent, position); obj.name = label;
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                if (renderer.sharedMaterial == teal) renderer.sharedMaterial = material;
            // Keep root scale at one, so snap transforms do not distort the endpoint.
            var grab = obj.AddComponent<VLabGrabInteractable>(); grab.Kind = "wire"; grab.ContextLabel = label;
            Collider(obj, new Vector3(.13f, .12f, .13f)); return grab;
        }
        private static VLabSnapZone Zone(string name, string label, string kind, Vector3 position, Vector3 size)
        {
            var obj = new GameObject(name); obj.transform.SetParent(sceneRoot); obj.transform.position = position;
            var zone = obj.AddComponent<VLabSnapZone>(); zone.ContextLabel = label; zone.AcceptedKind = kind;
            Collider(obj, size);
            zone.PreviewRenderer = Box("SnapGuide", obj.transform, new Vector3(0, -.024f, 0), new Vector3(size.x, .008f, size.z), guide, false).GetComponentInChildren<Renderer>();
            return zone;
        }
        private static VLabInteractable DeskButton(string name, string label, Vector3 position, Material material)
        {
            var obj = Box(name, sceneRoot, position, new Vector3(.58f, .055f, .20f), material, true);
            var action = obj.AddComponent<VLabInteractable>(); action.ContextLabel = label + " — chọn";
            FlatLabel(name + "Label", label, position + new Vector3(0, .031f, 0), .54f, .075f, pale); return action;
        }
        private static VLabInteractable PartAction(GameObject root, string partName, string label, float size)
        {
            var part = FindPart(root, partName);
            Collider(part.gameObject, Vector3.one * size);
            var action = part.gameObject.AddComponent<VLabInteractable>(); action.ContextLabel = label; return action;
        }
        private static VLabRotaryControl PartDial(GameObject root, string name, string label, Vector3 axis, float sensitivity, bool detented, float hitSize)
        {
            var part = FindPart(root, name); Collider(part.gameObject, Vector3.one * hitSize);
            var dial = part.gameObject.AddComponent<VLabRotaryControl>(); dial.ContextLabel = label; dial.MovingPart = part;
            dial.Axis = part.InverseTransformDirection(axis); dial.Sensitivity = sensitivity; dial.Detented = detented; return dial;
        }
        private static Transform FindPart(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        private static void Collider(GameObject obj, Vector3 size)
        {
            var c = obj.AddComponent<BoxCollider>();
            var scale = obj.transform.lossyScale;
            var minimumSize = new Vector3(size.x / Mathf.Abs(scale.x), size.y / Mathf.Abs(scale.y), size.z / Mathf.Abs(scale.z));
            if (obj.GetComponent<MeshFilter>() != null)
            {
                var padding = new Vector3(.04f / Mathf.Abs(scale.x), .04f / Mathf.Abs(scale.y), .04f / Mathf.Abs(scale.z));
                c.size = Vector3.Max(c.size + padding, minimumSize);
            }
            else { c.center = Vector3.zero; c.size = minimumSize; }
        }
    }
}
