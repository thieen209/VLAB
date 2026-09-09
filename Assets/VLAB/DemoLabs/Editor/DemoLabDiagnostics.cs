using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VLAB.DemoLabs.Editor
{
    public static class DemoLabDiagnostics
    {
        public static void InspectScenes()
        {
            var report = new StringBuilder();
            foreach (var path in new[] { DemoLabSceneBuilder.EngineeringPath, DemoLabSceneBuilder.BiologyPath })
            {
                var scene = EditorSceneManager.OpenScene(path); report.AppendLine(path);
                var root = scene.GetRootGameObjects().Single();
                foreach (Transform child in root.transform)
                {
                    if (!child.name.StartsWith("ENG_") && !child.name.StartsWith("BIO_") && child.name != "ReusedExperimentTable" && child.name != "BenchBrand") continue;
                    report.AppendLine($"{child.name} root: p={child.position} s={child.localScale} r={child.eulerAngles}");
                    foreach (var t in child.GetComponentsInChildren<Transform>().Take(5))
                        report.AppendLine($"  {t.name}: p={t.position.ToString("F4")} s={t.localScale.ToString("F4")} r={t.eulerAngles}");
                    var renderers = child.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                        report.AppendLine($"  bounds center={bounds.center.ToString("F4")} size={bounds.size.ToString("F4")}");
                    }
                    var text = child.GetComponent<TMP_Text>();
                    if (text != null) { text.ForceMeshUpdate(); report.AppendLine($"  fontSize={text.fontSize} auto={text.enableAutoSizing} textBounds={text.textBounds} preferred={text.GetPreferredValues()}"); }
                }
            }
            File.WriteAllText("TestResults/DemoLabs-SceneDiagnostics.txt", report.ToString());
            Debug.Log(report);
        }
    }
}
