using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Provides a single experiment selection point for desktop, XR and future teacher UI.</summary>
    public sealed class ChemistryLabLessonHub : MonoBehaviour
    {
        public enum Lesson { Titration, DaniellCell, CopperElectrolysis }
        [SerializeField] private TitrationLessonController titration;
        [SerializeField] private ConfigurableExperimentController daniellCell;
        [SerializeField] private ConfigurableExperimentController copperElectrolysis;
        [SerializeField] private Lesson activeLesson;

        public Lesson ActiveLesson => activeLesson;
        public TitrationLessonController Titration => titration;
        public ConfigurableExperimentController ActiveConfigurable => activeLesson == Lesson.DaniellCell ? daniellCell : activeLesson == Lesson.CopperElectrolysis ? copperElectrolysis : null;

        public void Configure(TitrationLessonController titrationController, ConfigurableExperimentController daniellController, ConfigurableExperimentController electrolysisController)
        {
            titration = titrationController;
            daniellCell = daniellController;
            copperElectrolysis = electrolysisController;
        }
        public void Select(int lessonIndex) => activeLesson = (Lesson)Mathf.Clamp(lessonIndex, 0, 2);
    }
}
