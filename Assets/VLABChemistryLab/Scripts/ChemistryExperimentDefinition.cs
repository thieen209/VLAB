using System.Collections.Generic;
using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Editable lesson brief. Teachers can change requirements and steps in the Inspector without code.</summary>
    [CreateAssetMenu(fileName = "ChemistryExperiment", menuName = "VLAB/Chemistry Experiment Definition")]
    public sealed class ChemistryExperimentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string experimentId;
        public string displayName;
        [TextArea] public string learningObjective;
        [TextArea] public string safetyNote;

        [Header("Teacher-configurable requirements")]
        public List<string> requirements = new List<string>();
        public List<string> procedureSteps = new List<string>();

        [Header("Expected result")]
        public string expectedObservation;
        public string resultLabel;
        public string targetResult;
    }
}
