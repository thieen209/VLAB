using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VLAB.PhysicsLab.SceneFlow;

namespace VLAB.PhysicsLab.Editor
{
    public static class PhysicsLabEnvironmentVisualCapture
    {
        private const string SceneRoot = "Assets/VLAB/PhysicsLab/Scenes";
        private const string OutputRoot = "TestResults/EnvironmentVisuals";
        private const int Width = 1280;
        private const int Height = 720;

        [MenuItem("Tools/VLAB/Physics Lab/Capture Environment QA Views")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputRoot);
            var baseScene = EditorSceneManager.OpenScene(ScenePath(PhysicsLabSceneNames.Base), OpenSceneMode.Single);
            var camera = FindSingleComponent<Camera>(baseScene);
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;

            var skyPosition = new Vector3(0f, 18f, 0f);
            Capture(camera, "Sky_00_North", skyPosition, skyPosition + Vector3.forward);
            Capture(camera, "Sky_01_East", skyPosition, skyPosition + Vector3.right);
            Capture(camera, "Sky_02_South", skyPosition, skyPosition + Vector3.back);
            Capture(camera, "Sky_03_West", skyPosition, skyPosition + Vector3.left);

            var hub = EditorSceneManager.OpenScene(ScenePath(PhysicsLabSceneNames.Hub), OpenSceneMode.Additive);
            Capture(camera, "00_Hub_Entry", new Vector3(0f, 1.65f, -2.9f), new Vector3(0f, 1.75f, 2.35f));
            Capture(camera, "01_Windows_Central", new Vector3(0f, 1.65f, 0.15f), new Vector3(0f, 2.05f, 4.45f));
            Capture(camera, "02_Cabinet_West", new Vector3(-3.55f, 1.68f, 0.95f), new Vector3(-5.45f, 1.92f, 0.95f));
            Capture(camera, "03_Cabinet_East", new Vector3(3.55f, 1.68f, 0.95f), new Vector3(5.45f, 1.92f, 0.95f));
            EditorSceneManager.CloseScene(hub, true);

            var experimentScenes = new[]
            {
                PhysicsLabSceneNames.Pendulum,
                PhysicsLabSceneNames.Projectile,
                PhysicsLabSceneNames.Friction,
                PhysicsLabSceneNames.PhotogateMotion,
                PhysicsLabSceneNames.Spring,
                PhysicsLabSceneNames.AirTrackMomentum,
            };
            for (var index = 0; index < experimentScenes.Length; index++)
            {
                var content = EditorSceneManager.OpenScene(ScenePath(experimentScenes[index]), OpenSceneMode.Additive);
                Capture(camera, $"{index + 10:00}_{experimentScenes[index]}", new Vector3(0f, 1.70f, -1.85f), new Vector3(0f, 1.12f, 0.75f));
                EditorSceneManager.CloseScene(content, true);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VLAB Physics Lab] Captured 14 environment QA views in {OutputRoot}.");
        }

        public static void CaptureAllFromCommandLine() => CaptureAll();

        private static void Capture(Camera camera, string fileName, Vector3 position, Vector3 target)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
            var renderTexture = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var screenshot = new Texture2D(Width, Height, TextureFormat.RGB24, false, false);
                screenshot.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                screenshot.Apply(false, false);
                File.WriteAllBytes(Path.Combine(OutputRoot, fileName + ".png"), screenshot.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(screenshot);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static T FindSingleComponent<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }
            if (components.Count != 1)
            {
                throw new InvalidOperationException($"Expected one {typeof(T).Name} in {scene.name}, found {components.Count}.");
            }
            return components[0];
        }

        private static string ScenePath(string sceneName) => $"{SceneRoot}/{sceneName}.unity";
    }
}
