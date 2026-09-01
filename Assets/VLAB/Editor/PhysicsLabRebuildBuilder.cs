using UnityEditor;
using VLAB.PhysicsLab.Editor;

namespace VLAB.Editor
{
    public static class PhysicsLabRebuildBuilder
    {
        [MenuItem("VLAB/Build PhysicsLab Rebuild Foundation")]
        public static void BuildScene() => PhysicsLabExperienceBuilder.BuildAll();

        public static void BuildFromCommandLine() => PhysicsLabExperienceBuilder.BuildAll();
    }
}
