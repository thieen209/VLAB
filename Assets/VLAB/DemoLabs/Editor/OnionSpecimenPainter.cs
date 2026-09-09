using System.IO;
using UnityEditor;
using UnityEngine;

namespace VLAB.DemoLabs.Editor
{
    public static class OnionSpecimenPainter
    {
        public static Texture2D Paint()
        {
            const int size = 2048;
            var path = DemoLabSceneBuilder.Root + "/Art/UI/OnionEpidermis.png";
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + .5f) / size, (y + .5f) / size);
                var p = OnionSpecimen.CellCoordinates(uv);
                var wallDistance = Mathf.Min(Mathf.Min(p.x, 1 - p.x), Mathf.Min(p.y, 1 - p.y) * .36f);
                var wall = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.003f, .013f, wallDistance));
                var membrane = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.012f, .027f, wallDistance));
                var vacuole = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.025f, .025f, OnionSpecimen.VacuoleDistance(uv)));
                var nucleusDistance = OnionSpecimen.NucleusDistance(uv);
                var nucleus = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.76f, 1.13f, nucleusDistance));
                var stain = OnionSpecimen.Variation(uv, 8);
                var cloud = Mathf.PerlinNoise(uv.x * 71, uv.y * 103);
                var granules = Mathf.PerlinNoise(uv.x * 950, uv.y * 1400);
                var color = Color.Lerp(new Color(.77f, .72f, .68f), new Color(.70f, .63f, .72f), stain * .45f);
                color = Color.Lerp(color, new Color(.91f, .89f, .79f), vacuole * .87f);
                color = Color.Lerp(color, new Color(.54f, .42f, .49f), membrane * .30f);
                color = Color.Lerp(color, new Color(.31f, .29f, .33f), wall * .83f);
                color = Color.Lerp(color, new Color(.46f + cloud * .05f, .29f + cloud * .08f, .52f + cloud * .09f), nucleus * .88f);
                var nucleolus = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.11f, .29f, nucleusDistance));
                color = Color.Lerp(color, new Color(.33f, .22f, .39f), nucleolus * .50f);
                var grain = (granules - .5f) * (nucleus > .5f ? .12f : .055f) + (cloud - .5f) * .045f;
                color += new Color(grain, grain, grain, 0);
                pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = size; importer.mipmapEnabled = true; importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Trilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
