using UnityEngine;

namespace VLAB.PhysicsLab.SceneFlow
{
    public sealed class PhysicsLabHubController : MonoBehaviour
    {
        public void OpenPendulum() => Load(PhysicsLabSceneNames.Pendulum);
        public void OpenProjectile() => Load(PhysicsLabSceneNames.Projectile);
        public void OpenFriction() => Load(PhysicsLabSceneNames.Friction);
        public void OpenPhotogateMotion() => Load(PhysicsLabSceneNames.PhotogateMotion);
        public void OpenSpring() => Load(PhysicsLabSceneNames.Spring);
        public void OpenAirTrackMomentum() => Load(PhysicsLabSceneNames.AirTrackMomentum);
        public void BackToMainMenu() => PhysicsLabSceneFlow.Instance?.ReturnToMainMenu();

        private static void Load(string sceneName)
        {
            if (PhysicsLabSceneFlow.Instance == null)
            {
                Debug.LogError("PhysicsLabSceneFlow is not available. Load PhysicsLab_Base before hub content.");
                return;
            }
            PhysicsLabSceneFlow.Instance.LoadContent(sceneName);
        }
    }
}
