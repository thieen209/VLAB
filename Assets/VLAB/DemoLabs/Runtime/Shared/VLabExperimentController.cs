using UnityEngine;
using UnityEngine.SceneManagement;

namespace VLAB.DemoLabs
{
    public abstract class VLabExperimentController : MonoBehaviour
    {
        public VLabHud Hud;
        public VLabInteractionDriver Driver;
        public VLabFeedback FeedbackSounds;
        public LineRenderer GuidanceRing;
        public bool Started { get; protected set; }
        public bool Completed { get; protected set; }
        protected bool Resetting;
        private float lastActivity;
        private int mistakes;
        private IVLabResettable[] resettables;
        private VLabInteractable guidanceTarget;
        private float guidanceUntil;
        protected virtual VLabInteractable GuidanceTarget => null;
        protected abstract string Objective { get; }
        protected abstract string Concept { get; }
        protected abstract string AdaptiveHint { get; }
        protected virtual void Start()
        {
            var list = new System.Collections.Generic.List<IVLabResettable>();
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
                if (component is IVLabResettable resettable) list.Add(resettable);
            resettables = list.ToArray();
            Hud.ResetButton.onClick.AddListener(ResetExperiment);
            Hud.HomeButton.onClick.AddListener(() => SceneManager.LoadScene("Home"));
            Hud.CheckButton.onClick.AddListener(Check);
            ResetExperiment();
        }
        protected virtual void Update()
        {
            if (Started && !Completed && !Hud.ModalOpen && Time.unscaledTime - lastActivity > 35f)
            { Hud.Feedback(AdaptiveHint); ShowGuidance(); lastActivity = Time.unscaledTime; }
            if (GuidanceRing == null) return;
            var visible = guidanceTarget != null && Time.unscaledTime < guidanceUntil && Started && !Completed && !Driver.ViewLocked && !Hud.ModalOpen;
            GuidanceRing.enabled = visible;
            if (!visible) return;
            var collider = guidanceTarget.GetComponent<Collider>();
            var bounds = collider != null ? collider.bounds : new Bounds(guidanceTarget.transform.position, Vector3.one * .12f);
            var radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) + .045f, .08f, .28f);
            var center = bounds.center + Vector3.up * (bounds.extents.y + .012f);
            radius *= 1 + .05f * Mathf.Sin(Time.unscaledTime * 4);
            for (var i = 0; i < 32; i++)
            {
                var angle = i * Mathf.PI * 2 / 32;
                GuidanceRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
            }
        }
        private void ShowGuidance() { guidanceTarget = GuidanceTarget; guidanceUntil = Time.unscaledTime + 12; }
        public void MarkActivity() { lastActivity = Time.unscaledTime; guidanceUntil = 0; }
        public void Mistake(string reason)
        {
            mistakes++; MarkActivity();
            FeedbackSounds?.Warn();
            Hud.Feedback(reason + (mistakes >= 3 ? "\n" + AdaptiveHint : ""), true);
            if (mistakes >= 3) ShowGuidance();
        }
        public virtual void BeginExperiment() { Started = true; Hud.HideModal(); MarkActivity(); Refresh(); }
        public void ResetExperiment()
        {
            Resetting = true; Started = false; Completed = false; mistakes = 0;
            ExitInspection(); Driver.ReturnHeld();
            if (resettables != null) foreach (var item in resettables) item.ResetState();
            ResetModel(); Resetting = false;
            Driver.ResetView();
            Hud.Feedback(""); MarkActivity(); Refresh();
            Hud.ShowModal(Objective, Concept, "Bắt đầu", BeginExperiment);
        }
        protected void Finish(string title, string explanation)
        {
            Completed = true; Driver.ReturnHeld();
            Refresh();
            Hud.ShowModal(title, explanation, "Hoàn thành", () => Hud.Feedback("Đã hoàn thành. Chọn Đặt lại thí nghiệm để thực hành lại."));
        }
        public virtual void ExitInspection() { }
        public virtual void RotateInspection(float amount, bool coarse) { }
        protected abstract void ResetModel();
        public abstract void Check();
        protected abstract void Refresh();
    }
}
