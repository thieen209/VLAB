using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace VLAB.DemoLabs.Editor
{
    public static partial class DemoLabSceneBuilder
    {
        private static Material ivory, navy, graphite, teal, metal, glass, gold, red, guide, sampleMaterial;
        private static readonly Color pale = new Color(.84f, .94f, .92f);
        private static Material Mat(string name, Color color, float metallic = 0)
        {
            var path = Root + "/Art/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(GraphicsSettings.currentRenderPipeline != null ? "Universal Render Pipeline/Lit" : "Standard");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor("_Color", color); material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", .35f); material.SetFloat("_Smoothness", .35f);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(material);
            materials[name] = material; return material;
        }
        private static void PrepareAssets()
        {
            ivory = Mat("Ivory", new Color(.82f, .85f, .82f)); navy = Mat("Navy", new Color(.035f, .075f, .10f));
            graphite = Mat("Graphite", new Color(.035f, .043f, .052f)); teal = Mat("Teal", new Color(.055f, .43f, .37f));
            metal = Mat("Metal", new Color(.48f, .54f, .58f), .7f); glass = Mat("Glass", new Color(.5f, .73f, .76f), .15f);
            glass.SetColor("_Color", new Color(.67f, .85f, .85f, .35f)); glass.SetColor("_BaseColor", new Color(.67f, .85f, .85f, .35f));
            glass.SetFloat("_Mode", 2); glass.SetFloat("_Surface", 1);
            glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); glass.SetInt("_ZWrite", 0);
            glass.EnableKeyword("_ALPHABLEND_ON"); glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.renderQueue = 3000;
            gold = Mat("Gold", new Color(.72f, .49f, .17f), .4f); red = Mat("Red", new Color(.66f, .075f, .045f));
            Mat("Brown", new Color(.25f, .10f, .035f));
            Mat("Lens", new Color(.1f, .65f, .22f)); guide = Mat("Guide", new Color(.23f, .54f, .49f));
            sampleMaterial = Mat("Sample", new Color(.69f, .48f, .56f));
            Mat("Floor", new Color(.50f, .58f, .60f)); Mat("Wall", new Color(.88f, .89f, .85f)); Mat("Wood", new Color(.54f, .36f, .21f));
            foreach (var file in Directory.GetFiles(Root + "/Art/Models", "*.fbx"))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            var fontPath = Root + "/Art/UI/VLAB_Vietnamese.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 7, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "VLAB Vietnamese";
                AssetDatabase.CreateAsset(font, fontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var tex in font.atlasTextures) AssetDatabase.AddObjectToAsset(tex, font);
                var chars = Enumerable.Range(32, 560).Concat(Enumerable.Range(0x1e00, 256)).Concat(new[] { 0x2192, 0x2190, 0x2212, 0x2248, 0x03a9, 0x2022 }).Select(c => (uint)c).ToArray();
                font.TryAddCharacters(chars, out _);
                EditorUtility.SetDirty(font);
            }
            var circlePath = Root + "/Art/UI/Circle.png";
            if (!File.Exists(circlePath) || AssetDatabase.LoadAssetAtPath<Texture2D>(circlePath)?.width != 512)
            {
                var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                var pixels = new Color32[512 * 512];
                for (var y = 0; y < 512; y++) for (var x = 0; x < 512; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(256, 256));
                    pixels[y * 512 + x] = new Color(1, 1, 1, Mathf.Clamp01(255.5f - d));
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(circlePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(circlePath);
            }
            var ti = (TextureImporter)AssetImporter.GetAtPath(circlePath);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.maxTextureSize = 512; ti.SaveAndReimport();
            circle = AssetDatabase.LoadAssetAtPath<Sprite>(circlePath);
            if (circle == null) throw new InvalidOperationException("Circular optical mask sprite was not imported.");
        }
        private static GameObject Model(string name, Transform parent, Vector3 position, float scale = 1)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.localPosition = position;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Models/" + name + ".fbx");
            if (asset == null) throw new InvalidOperationException("Missing model: " + name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, obj.transform);
            instance.transform.localPosition = Vector3.zero;
            // Preserve the FBX importer's metric scale and axis conversion on its root.
            instance.transform.localScale *= scale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                {
                    if (m == null) return ivory;
                    var key = m.name.Replace("Demo_", "").Split('.')[0];
                    return materials.TryGetValue(key, out var replacement) ? replacement : ivory;
                }).ToArray();
            }
            return obj;
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collide)
        {
            var root = new GameObject(name); root.transform.SetParent(parent); root.transform.localPosition = position;
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube); mesh.name = "Surface"; mesh.transform.SetParent(root.transform, false); mesh.transform.localScale = size;
            Object.DestroyImmediate(mesh.GetComponent<Collider>()); mesh.GetComponent<Renderer>().sharedMaterial = material;
            if (collide) Collider(root, size);
            return root;
        }
        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = name; obj.transform.SetParent(parent); obj.transform.localPosition = position; obj.transform.localScale = size;
            Object.DestroyImmediate(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static TMP_Text WorldText(string name, Transform parent, string content, Vector3 position, float width, float height, float size, Color color)
        {
            var obj = new GameObject(name, typeof(TextMeshPro)); obj.transform.SetParent(parent); obj.transform.localPosition = position;
            var text = obj.GetComponent<TextMeshPro>(); text.font = font; text.text = content; text.fontSize = size * .25f; text.color = color;
            text.enableAutoSizing = true; text.fontSizeMax = size * .25f; text.fontSizeMin = size * .18f;
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = new Vector2(width, height); text.raycastTarget = false;
            return text;
        }
        private static void FlatLabel(string name, string text, Vector3 position, float width, float height, Color? color = null)
        {
            var label = WorldText(name, sceneRoot, text, position, width, height, 2.2f, color ?? new Color(.045f, .1f, .12f));
            label.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        private static void BuildRoom(bool bio)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.56f, .63f, .68f);
            RenderSettings.ambientEquatorColor = new Color(.40f, .46f, .47f);
            RenderSettings.ambientGroundColor = new Color(.22f, .25f, .26f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/VLAB/PhysicsLab/Environment/Materials/VLAB_Qwantani_Skybox.mat"); RenderSettings.fog = false;
            Box("Floor", sceneRoot, new Vector3(0, -.08f, 0), new Vector3(10, .16f, 11), materials["Floor"], true);
            Box("BackWallLower", sceneRoot, new Vector3(0, .94f, 4.7f), new Vector3(10, 1.88f, .16f), materials["Wall"], true);
            Box("BackWallUpper", sceneRoot, new Vector3(0, 3.85f, 4.7f), new Vector3(10, .5f, .16f), materials["Wall"], true);
            foreach (var column in new[] { new Vector2(-4.6875f, .625f), new Vector2(-1.65f, 1.15f), new Vector2(1.65f, 1.15f), new Vector2(4.6875f, .625f) })
                Box("WindowPier", sceneRoot, new Vector3(column.x, 2.75f, 4.7f), new Vector3(column.y, 1.75f, .16f), materials["Wall"], true);
            Box("LeftWall", sceneRoot, new Vector3(-5, 2, 0), new Vector3(.16f, 4, 10), materials["Wall"], true);
            Box("RightWall", sceneRoot, new Vector3(5, 2, 0), new Vector3(.16f, 4, 10), materials["Wall"], true);
            var ceiling = Box("Ceiling", sceneRoot, new Vector3(0, 4.1f, 0), new Vector3(10, .16f, 10), ivory, false);
            ceiling.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box("BackWainscot", sceneRoot, new Vector3(0, .66f, 4.59f), new Vector3(10, 1.32f, .04f), navy, false);
            Box("WallTrim", sceneRoot, new Vector3(0, 1.34f, 4.56f), new Vector3(10, .035f, .03f), bio ? teal : gold, false);
            for (var x = -4; x <= 4; x += 2)
                Box("FloorJoint", sceneRoot, new Vector3(x, .004f, 0), new Vector3(.008f, .002f, 10), ivory, false);
            for (var z = -4; z <= 4; z += 2)
                Box("FloorJoint", sceneRoot, new Vector3(0, .004f, z), new Vector3(10, .002f, .008f), ivory, false);
            var sunlight = new GameObject("Daylight", typeof(Light)); sunlight.transform.SetParent(sceneRoot); sunlight.transform.rotation = Quaternion.Euler(45, -35, 0);
            var sun = sunlight.GetComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, .96f, .91f); sun.intensity = .85f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .60f;
            for (var x = -3; x <= 3; x += 3)
            {
                Box("CeilingFixture", sceneRoot, new Vector3(x, 4, 0), new Vector3(.13f, .025f, 4.3f), paleMaterial(), false);
                Box("CeilingRail", sceneRoot, new Vector3(x + .12f, 4.0f, 0), new Vector3(.045f, .03f, 4.3f), navy, false);
            }
            for (var i = 0; i < 3; i++)
            {
                var x = -3.3f + i * 3.3f;
                foreach (var sign in new[] { -1, 1 })
                {
                    Box("WindowUpright", sceneRoot, new Vector3(x + sign * 1.05f, 2.75f, 4.52f), new Vector3(.09f, 1.7f, .12f), navy, false);
                    Box("WindowCrossbar", sceneRoot, new Vector3(x, 2.75f + sign * .825f, 4.52f), new Vector3(2.15f, .09f, .12f), navy, false);
                }
                Box("WindowMullion", sceneRoot, new Vector3(x, 2.75f, 4.4f), new Vector3(.045f, 1.58f, .04f), ivory, false);
                Box("WindowSill", sceneRoot, new Vector3(x, 1.89f, 4.32f), new Vector3(2.25f, .05f, .34f), ivory, false);
            }
            // Existing asset supplies the workstation frame; a clean top gives the two rooms a shared scale.
            var tableAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLAB/PhysicsLab/Models/Experiment_Table.fbx");
            if (tableAsset != null)
            {
                var table = new GameObject("ReusedExperimentTable"); table.transform.SetParent(sceneRoot, false);
                PrefabUtility.InstantiatePrefab(tableAsset, table.transform);
                var bounds = new Bounds(); var initialized = false;
                foreach (var r in table.GetComponentsInChildren<Renderer>()) { if (!initialized) { bounds = r.bounds; initialized = true; } else bounds.Encapsulate(r.bounds); r.sharedMaterial = graphite; }
                if (initialized && bounds.size.y > .001f)
                {
                    table.transform.localScale = new Vector3(4.0f / bounds.size.x, 1.05f / bounds.size.y, 1.9f / bounds.size.z);
                    table.transform.position = new Vector3(-bounds.center.x * table.transform.localScale.x, -bounds.min.y * table.transform.localScale.y, -bounds.center.z * table.transform.localScale.z);
                }
            }
            Box("Worktop", sceneRoot, new Vector3(0, 1.065f, 0), new Vector3(4.2f, .075f, 2.02f), ivory, true);
            Box("WorktopEdge", sceneRoot, new Vector3(0, 1.027f, -1.015f), new Vector3(4.2f, .036f, .025f), bio ? teal : gold, false);
            WorldText("BenchBrand", sceneRoot, "VLAB  /  " + (bio ? "02 · SINH HỌC" : "01 · KỸ THUẬT"), new Vector3(0, .82f, -1.03f), 2.7f, .16f, 6, pale);
            for (var i = 0; i < 5; i++)
            {
                var x = -3.6f + i * 1.8f;
                Box("StorageCabinet", sceneRoot, new Vector3(x, .54f, 3.87f), new Vector3(1.7f, 1.08f, .93f), ivory, true);
                Box("SecondaryWorktop", sceneRoot, new Vector3(x, 1.115f, 3.85f), new Vector3(1.77f, .06f, 1.02f), graphite, false);
                for (var door = -1; door <= 1; door += 2)
                {
                    Box("CabinetDoor", sceneRoot, new Vector3(x + door * .41f, .55f, 3.39f), new Vector3(.80f, .96f, .018f), materials["Wall"], false);
                    Box("Handle", sceneRoot, new Vector3(x + door * .1f, .75f, 3.355f), new Vector3(.018f, .17f, .025f), metal, false);
                }
                if (bio && i % 2 == 0) Model("BIO_Microscope", sceneRoot, new Vector3(x, 1.15f, 3.82f), .8f);
                else if (!bio)
                {
                    if (i % 2 == 0)
                    {
                        var supply = Model("ENG_PowerSupply", sceneRoot, new Vector3(x, 1.15f, 3.82f), .95f);
                        WorldText("StandbyReadout", supply.transform, "5.00 V\nNGUỒN TẮT", new Vector3(0, .236f, -.188f), .35f, .104f, 2.7f, pale);
                    }
                    else
                    {
                        Model("ENG_Breadboard", sceneRoot, new Vector3(x, 1.15f, 3.82f), 1.1f);
                        Model("ENG_Resistor220", sceneRoot, new Vector3(x + .12f, 1.215f, 3.80f));
                    }
                }
                else { Bottle("Bình mẫu", new Vector3(x - .25f, 1.15f, 3.8f), glass); Bottle("Mẫu", new Vector3(x + .13f, 1.15f, 3.8f), sampleMaterial); }
            }
            // Side wall identity and a concise scientific reference stay behind the primary workspace.
            var poster = Box("ReferencePanel", sceneRoot, new Vector3(-3.45f, 2.2f, 2.7f), new Vector3(1.7f, 1.5f, .055f), navy, false);
            WorldText("ReferenceTitle", sceneRoot, bio ? "TẾ BÀO THỰC VẬT" : "THIẾT KẾ CÓ CƠ SỞ", new Vector3(-3.45f, 2.63f, 2.66f), 1.5f, .2f, 4.6f, pale);
            WorldText("ReferenceBody", sceneRoot, bio ? "Thành tế bào\nNhân · Tế bào chất\n\nQuan sát → So sánh\n→ Giải thích" : "U = I × R\n\nĐo lường trước\nKiểm tra mạch\nCấp nguồn sau", new Vector3(-3.45f, 2.06f, 2.66f), 1.45f, .85f, 4.5f, pale);
            WorldText("RoomIdentity", sceneRoot, bio ? "PHÒNG SINH HỌC" : "PHÒNG KỸ THUẬT", new Vector3(0, 3.87f, 4.40f), 5.5f, .32f, 12, new Color(.07f, .18f, .20f));
            WorldText("MissionLine", sceneRoot, "VLAB  ·  HỌC QUA THỰC HÀNH", new Vector3(0, 3.54f, 4.40f), 4.4f, .18f, 5.7f, new Color(.12f, .30f, .30f));
        }
        private static Material paleMaterial()
        {
            if (materials.TryGetValue("Luminaire", out var found)) return found;
            var m = Mat("Luminaire", Color.white); m.SetColor("_EmissionColor", new Color(.7f, .77f, .8f)); return m;
        }
        private static Material skyMaterial()
        {
            if (materials.TryGetValue("WindowSky", out var found)) return found;
            var m = Mat("WindowSky", new Color(.53f, .77f, .90f)); m.SetColor("_EmissionColor", new Color(.21f, .31f, .38f)); return m;
        }
        private static void Bottle(string label, Vector3 position, Material material)
        {
            Cylinder("Bottle_" + label, sceneRoot, position + Vector3.up * .11f, new Vector3(.15f, .11f, .15f), material);
            Cylinder("Cap", sceneRoot, position + Vector3.up * .235f, new Vector3(.10f, .025f, .10f), navy);
            WorldText("BottleLabel", sceneRoot, label, position + new Vector3(0, .12f, -.081f), .145f, .08f, 1.8f, Color.white);
        }
    }
}
