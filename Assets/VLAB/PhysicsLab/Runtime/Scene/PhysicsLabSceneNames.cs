using System.Collections.Generic;

namespace VLAB.PhysicsLab.SceneFlow
{
    public static class PhysicsLabSceneNames
    {
        public const string Home = "Home";
        public const string Base = "PhysicsLab_Base";
        public const string Hub = "PhysicsLab_Hub";
        public const string Pendulum = "Physics_Pendulum";
        public const string Projectile = "Physics_Projectile";
        public const string Friction = "Physics_Friction";
        public const string PhotogateMotion = "Physics_PhotogateMotion";
        public const string Spring = "Physics_Spring";
        public const string AirTrackMomentum = "Physics_AirTrackMomentum";

        public static IReadOnlyList<string> ContentScenes { get; } = new[]
        {
            Hub,
            Pendulum,
            Projectile,
            Friction,
            PhotogateMotion,
            Spring,
            AirTrackMomentum,
        };
    }
}
