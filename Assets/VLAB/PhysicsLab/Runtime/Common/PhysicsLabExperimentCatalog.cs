using System.Collections.Generic;

namespace VLAB.PhysicsLab.Common
{
    public sealed class PhysicsLabExperimentRequirement
    {
        public PhysicsLabExperimentRequirement(string id, string name, params string[] requiredAssets)
        {
            Id = id;
            Name = name;
            RequiredAssets = requiredAssets;
        }

        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<string> RequiredAssets { get; }
    }

    public static class PhysicsLabExperimentCatalog
    {
        public static IReadOnlyList<PhysicsLabExperimentRequirement> Experiments { get; } =
            new[]
            {
                new PhysicsLabExperimentRequirement("PHY_01", "Simple Pendulum",
                    "Experiment_Table", "Laboratory_Retort_Stand", "Adjustable_Clamp", "Pendulum_String_Anchor", "Pendulum_Bob", "Laboratory_Meter_Ruler"),
                new PhysicsLabExperimentRequirement("PHY_02", "Projectile Motion",
                    "Experiment_Table", "Physics_Projectile_Launcher", "Projectile_Steel_Ball", "Laboratory_Meter_Ruler"),
                new PhysicsLabExperimentRequirement("PHY_03", "Friction Experiment",
                    "Experiment_Table", "Physics_Friction_Block", "Mass_Set", "Spring_Force_Meter", "Laboratory_Meter_Ruler"),
                new PhysicsLabExperimentRequirement("PHY_04", "Photogate Motion Measurement",
                    "Experiment_Table", "Laboratory_Retort_Stand", "Adjustable_Clamp", "Digital_Timer_MC964", "Physics_Photogate", "Photogate_Flag", "Laboratory_Meter_Ruler"),
                new PhysicsLabExperimentRequirement("PHY_05", "Mass Spring Oscillation",
                    "Experiment_Table", "Laboratory_Retort_Stand", "Adjustable_Clamp", "Physics_Coil_Spring", "Mass_Set", "Spring_Force_Meter"),
                new PhysicsLabExperimentRequirement("PHY_06", "Momentum Collision on Air Track",
                    "Experiment_Table", "Physics_Air_Track", "Air_Track_Glider_A", "Air_Track_Glider_B", "Collision_Bumper", "Inelastic_Collision_Attachment", "Physics_Photogate", "Photogate_Flag", "Digital_Timer_MC964", "Mass_Set"),
            };
    }
}
