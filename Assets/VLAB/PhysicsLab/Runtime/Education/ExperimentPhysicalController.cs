using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Measurement;
using VLAB.PhysicsLab.Oscillation;
using VLAB.PhysicsLab.SceneFlow;

namespace VLAB.PhysicsLab.Education
{
    public sealed class ExperimentPhysicalController : MonoBehaviour
    {
        [SerializeField] private string experimentId;
        [SerializeField] private PhysicsLabStationController stationController;
        [SerializeField] private ExperimentResetManager resetManager;
        [SerializeField] private ExperimentContextPanel contextPanel;
        [SerializeField, Min(1f)] private float firstHintDelay = 8f;
        [SerializeField, Min(1f)] private float strongHintDelay = 18f;

        private readonly List<Action> unsubscribeActions = new List<Action>();
        private float idleSeconds;
        private bool explicitHintRequested;
        private int incorrectPhysicalActions;

        public event Action Changed;
        public PhysicalExperimentProgress Progress { get; private set; }
        public PhysicalTrialRecorder Trials { get; private set; }
        public ExperimentLearningContent Content { get; private set; }
        public string ExperimentId => experimentId;
        public string CurrentCue => Progress?.CurrentCue ?? string.Empty;
        public bool ShowSubtleHint => explicitHintRequested || incorrectPhysicalActions >= 2 || idleSeconds >= firstHintDelay;
        public bool ShowStrongHint => incorrectPhysicalActions >= 3 || idleSeconds >= strongHintDelay;
        public string HintText => ShowStrongHint && Content?.GuidancePrompts != null && Content.GuidancePrompts.Count > 0
            ? Content.GuidancePrompts[Math.Min(Content.GuidancePrompts.Count - 1, 3)]
            : CurrentCue;
        public string MeasurementText => BuildMeasurementText();
        public bool HasEnoughTrials => Trials != null && Trials.HasEnoughTrials;
        public string ResultSheetText => BuildResultSheet();

        public void Configure(string id, PhysicsLabStationController station, ExperimentResetManager manager)
        {
            experimentId = id;
            stationController = station;
            resetManager = manager;
            InitializeState();
        }

        public void SetPanel(ExperimentContextPanel panel)
        {
            contextPanel = panel;
        }

        private void Awake()
        {
            InitializeState();
        }

        private void Start()
        {
            SubscribeToPhysicalApparatus();
            contextPanel?.Bind(this);
            Changed?.Invoke();
        }

        private void Update()
        {
            idleSeconds += Time.unscaledDeltaTime;
            if (Mathf.Abs(idleSeconds - firstHintDelay) < Time.unscaledDeltaTime
                || Mathf.Abs(idleSeconds - strongHintDelay) < Time.unscaledDeltaTime)
            {
                Changed?.Invoke();
            }
        }

        private void OnDestroy()
        {
            foreach (var unsubscribe in unsubscribeActions)
            {
                unsubscribe?.Invoke();
            }
            unsubscribeActions.Clear();
        }

        public void RequestHint()
        {
            explicitHintRequested = true;
            idleSeconds = Mathf.Max(idleSeconds, firstHintDelay);
            Changed?.Invoke();
        }

        public void ResetCurrentTrial()
        {
            resetManager?.ResetAll();
            Progress?.Reset();
            idleSeconds = 0f;
            explicitHintRequested = false;
            incorrectPhysicalActions = 0;
            Changed?.Invoke();
        }

        public void RestartExperiment()
        {
            Trials?.Clear();
            ResetCurrentTrial();
        }

        public void BackToHub() => stationController?.BackToHub();

        public bool PublishPhysicalAction(Component source, string objectId, PhysicalActionKind kind, double value = 0d, string unit = "")
        {
            if (Progress == null || source == null)
            {
                return false;
            }
            var advanced = Progress.Observe(new PhysicalLabEvent(source, objectId, kind, value, unit));
            idleSeconds = 0f;
            if (advanced)
            {
                explicitHintRequested = false;
                incorrectPhysicalActions = 0;
            }
            else if (!Progress.IsCoreComplete)
            {
                incorrectPhysicalActions++;
            }
            Changed?.Invoke();
            return advanced;
        }

        public bool RecordLiveMeasurement(Component source, string objectId, string name, double value, string unit, string parameterSignature)
        {
            var labEvent = new PhysicalLabEvent(source, objectId, PhysicalActionKind.MeasurementRecorded, value, unit);
            if (Trials == null
                || Progress == null
                || (!Progress.IsCoreComplete && !Progress.CanObserve(labEvent))
                || !Trials.TryRecord(new LiveMeasurementSample(source, name, value, unit, parameterSignature)))
            {
                return false;
            }
            PublishPhysicalAction(source, objectId, PhysicalActionKind.MeasurementRecorded, value, unit);
            Changed?.Invoke();
            return true;
        }

        private void InitializeState()
        {
            if (string.IsNullOrWhiteSpace(experimentId) || Progress != null)
            {
                return;
            }
            var definition = PhysicalExperimentDefinitionCatalog.Get(experimentId);
            if (definition == null)
            {
                Debug.LogError($"Physical experiment definition missing for '{experimentId}'.", this);
                return;
            }
            Content = ExperimentContentCatalog.Get(experimentId);
            Progress = new PhysicalExperimentProgress(definition);
            Trials = new PhysicalTrialRecorder(3);
            Progress.StepAdvanced += _ => Changed?.Invoke();
        }

        private void SubscribeToPhysicalApparatus()
        {
            foreach (var grabbable in GetComponentsInChildren<Interaction.LabGrabbable>(true))
            {
                var captured = grabbable;
                captured.Grabbed += OnGrabbed;
                captured.Released += OnReleased;
                unsubscribeActions.Add(() => { captured.Grabbed -= OnGrabbed; captured.Released -= OnReleased; });
            }

            foreach (var point in GetComponentsInChildren<LabAttachmentPoint>(true))
            {
                var captured = point;
                captured.Attached += OnAttached;
                captured.Detached += OnDetached;
                unsubscribeActions.Add(() => { captured.Attached -= OnAttached; captured.Detached -= OnDetached; });
            }

            var pendulum = GetComponentInChildren<PendulumBob>(true);
            if (pendulum != null)
            {
                pendulum.PeriodMeasured += OnPendulumPeriod;
                unsubscribeActions.Add(() => pendulum.PeriodMeasured -= OnPendulumPeriod);
            }

            var spring = GetComponentInChildren<PhysicsCoilSpring>(true);
            if (spring != null)
            {
                spring.PeriodMeasured += OnSpringPeriod;
                unsubscribeActions.Add(() => spring.PeriodMeasured -= OnSpringPeriod);
            }

            var timer = GetComponentInChildren<DigitalTimerMC964>(true);
            if (timer != null)
            {
                timer.DisplayChanged += OnTimerDisplayChanged;
                unsubscribeActions.Add(() => timer.DisplayChanged -= OnTimerDisplayChanged);
            }
        }

        private void OnGrabbed(Interaction.LabGrabbable source) =>
            PublishPhysicalAction(source, source.name, PhysicalActionKind.Grabbed);

        private void OnReleased(Interaction.LabGrabbable source) =>
            PublishPhysicalAction(source, source.name, PhysicalActionKind.Released);

        private void OnAttached(LabAttachment attachment) =>
            PublishPhysicalAction(attachment, attachment.name, PhysicalActionKind.Snapped);

        private void OnDetached(LabAttachment attachment) =>
            PublishPhysicalAction(attachment, attachment.name, PhysicalActionKind.Detached);

        private void OnPendulumPeriod(double period) => RecordLiveMeasurement(
            GetComponentInChildren<PendulumBob>(true), "Pendulum_Bob", "Chu kỳ", period, "s", PendulumSignature());

        private void OnSpringPeriod(double period) => RecordLiveMeasurement(
            GetComponentInChildren<PhysicsCoilSpring>(true), "Physics_Coil_Spring", "Chu kỳ", period, "s", SpringSignature());

        private void OnTimerDisplayChanged(double time)
        {
            if (time > 0d)
            {
                RecordLiveMeasurement(GetComponentInChildren<DigitalTimerMC964>(true), "Digital_Timer", "Thời gian", time, "s", PhotogateSignature());
            }
        }

        private string PendulumSignature()
        {
            var bob = GetComponentInChildren<PendulumBob>(true);
            return bob != null ? $"L={bob.Length:0.###}m;m={bob.BobMass:0.###}kg" : "pendulum-live";
        }

        private string SpringSignature()
        {
            var spring = GetComponentInChildren<PhysicsCoilSpring>(true);
            return spring != null ? $"k={spring.SpringConstant:0.###}N/m;mass={spring.AttachedMassKilograms:0.###}kg" : "spring-live";
        }

        private string PhotogateSignature()
        {
            var gates = GetComponentsInChildren<PhysicsPhotogate>(true);
            return gates.Length >= 2 ? $"gate-distance={Vector3.Distance(gates[0].transform.position, gates[1].transform.position):0.###}m" : "photogate-live";
        }

        private string BuildMeasurementText()
        {
            if (Trials == null || Trials.Samples.Count == 0)
            {
                return "Chưa có số đo thực.";
            }
            var builder = new StringBuilder();
            foreach (var sample in Trials.Samples.TakeLast(4))
            {
                builder.AppendLine($"{sample.Name}: {sample.Value:0.###} {sample.Unit}");
            }
            builder.Append($"Lần đo hợp lệ: {Trials.TrialCount}/{Trials.RequiredTrials}");
            return builder.ToString();
        }

        private string BuildResultSheet()
        {
            if (!HasEnoughTrials)
            {
                return "Cần đủ ba phép đo vật lý hợp lệ để mở phiếu kết quả.";
            }

            var samples = Trials.Samples;
            var average = samples.Average(sample => sample.Value);
            var builder = new StringBuilder();
            builder.AppendLine(Content?.Title ?? experimentId);
            if (!string.IsNullOrWhiteSpace(Content?.Objective)) builder.AppendLine($"Mục tiêu: {Content.Objective}");
            builder.AppendLine("Bảng số liệu thực:");
            for (var index = 0; index < samples.Count; index++)
            {
                builder.AppendLine($"  {index + 1}. {samples[index].Value:0.###} {samples[index].Unit}");
            }
            builder.AppendLine($"Trung bình: {average:0.###} {samples[0].Unit}");
            builder.AppendLine($"Thông số: {samples[samples.Count - 1].ParameterSignature}");

            if (TryTheoryValue(average, samples[samples.Count - 1], out var theory, out var theoryLabel))
            {
                var error = Math.Abs(theory) > 0.000001d ? Math.Abs(average - theory) / Math.Abs(theory) * 100d : 0d;
                builder.AppendLine($"{theoryLabel}: {theory:0.###} {samples[0].Unit}");
                builder.AppendLine($"Độ lệch: {error:0.##}%");
            }
            else if (experimentId == "PHY_04" && TryParameter(samples[samples.Count - 1].ParameterSignature, "gate-distance", out var distance) && average > 0d)
            {
                builder.AppendLine($"Vận tốc từ khoảng cổng/thời gian: {distance / average:0.###} m/s");
            }

            builder.Append("Nhận xét: Kết quả được ghi trực tiếp từ trạng thái mô phỏng; hãy đổi thông số vật lý và lặp lại để so sánh.");
            return builder.ToString();
        }

        private bool TryTheoryValue(double average, LiveMeasurementSample sample, out double theory, out string label)
        {
            theory = 0d;
            label = "Giá trị lý thuyết";
            switch (experimentId)
            {
                case "PHY_01" when TryParameter(sample.ParameterSignature, "L", out var length):
                    theory = 2d * Math.PI * Math.Sqrt(length / 9.80665d);
                    return true;
                case "PHY_02" when TryParameter(sample.ParameterSignature, "angle", out var angle)
                                        && TryParameter(sample.ParameterSignature, "speed", out var speed):
                    theory = speed * speed * Math.Sin(2d * angle * Math.PI / 180d) / 9.80665d;
                    label = "Lý thuyết (điểm phóng và rơi cùng cao)";
                    return true;
                case "PHY_03" when TryParameter(sample.ParameterSignature, "mass", out var mass)
                                        && TryParameter(sample.ParameterSignature, "muK", out var muK):
                    theory = muK * mass * 9.80665d;
                    return true;
                case "PHY_05" when TryParameter(sample.ParameterSignature, "k", out var springConstant)
                                        && TryParameter(sample.ParameterSignature, "mass", out var springMass)
                                        && springConstant > 0d:
                    theory = 2d * Math.PI * Math.Sqrt(springMass / springConstant);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParameter(string signature, string key, out double value)
        {
            value = 0d;
            if (string.IsNullOrWhiteSpace(signature)) return false;
            foreach (var segment in signature.Split(';'))
            {
                var prefix = key + "=";
                if (!segment.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                var raw = segment.Trim().Substring(prefix.Length);
                var length = 0;
                while (length < raw.Length && (char.IsDigit(raw[length]) || raw[length] == '-' || raw[length] == '+' || raw[length] == '.' || raw[length] == 'e' || raw[length] == 'E')) length++;
                return double.TryParse(raw.Substring(0, length), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
            return false;
        }
    }
}
