using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VLAB.Core.Input;

namespace VLAB.Tests.Integration
{
    public sealed class PhysicsLabRebuildSceneTests
    {
        private const string ScenePath = "Assets/VLAB/PhysicsLab/Scenes/PhysicsLab_Base.unity";

        [Test]
        public void BaseScene_HasOneRigOneUiSystemAndNoMissingScripts()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Assert.That(FindRoot(scene, "_SYSTEMS"), Is.Not.Null);
            Assert.That(FindRoot(scene, "_PLAYER"), Is.Not.Null);
            Assert.That(FindRoot(scene, "_ENVIRONMENT"), Is.Not.Null);
            Assert.That(FindRoot(scene, "_UI"), Is.Not.Null);

            var inputManager = Object.FindAnyObjectByType<InputManager>();
            Assert.That(inputManager, Is.Not.Null);
            Assert.That(inputManager.HasProvider, Is.True);
            Assert.That(inputManager.ActiveProviderName, Is.EqualTo("Simulator / Keyboard"));
            Assert.That(Count<Camera>(scene), Is.EqualTo(1));
            Assert.That(Count<AudioListener>(scene), Is.EqualTo(1));
            Assert.That(Count<EventSystem>(scene), Is.EqualTo(1));

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, GetPath(transform));
                }
            }
        }

        private static int Count<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<T>(true).Length);

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);

        private static string GetPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }
    }
}
